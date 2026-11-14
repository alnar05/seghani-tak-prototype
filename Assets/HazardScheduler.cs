using UnityEngine;
using System.Collections.Generic;

// One committed warning/fall at a time. Selection is biased, never aimed after commitment.
public class HazardScheduler : MonoBehaviour
{
    public EarthquakeDirector director;
    public Level1Configuration configuration;
    public FallingProp[] props;
    System.Random random;
    float nextConsideration, settledAt = -1f;
    FallingProp active;
    int blockedRoutes;
    FallingProp opening, caution;
    bool openingPending = true, cautionPending = true;
    Vector3 previousPosition, travel;

    void Start()
    {
        random = new System.Random(configuration.seed);
        previousPosition = director.player.transform.position;
        int route = random.Next(2);
        opening = props[route == 0 ? 2 : 6];
        caution = props[route == 0 ? 8 : 7];
        Debug.Log("Level 1 seed=" + configuration.seed + " opening route=" + (route == 0 ? "south" : "north"));
        nextConsideration = director.quakeStart + configuration.openingDelayAfterOnset;
    }

    public void Tick(float now, float delta)
    {
        var position = director.player.transform.position;
        if (Time.timeScale <= 0 || director.CurrentPhase != EarthquakeDirector.Phase.Shaking)
        {
            previousPosition = position;
            travel = Vector3.zero;
            return;
        }
        travel = Vector3.Lerp(travel, (position - previousPosition) / Mathf.Max(delta, .001f), .2f);
        travel.y = 0;
        travel = Vector3.ClampMagnitude(travel, 2f);
        previousPosition = position;
        if (active)
        {
            active.Tick(now);
            if (!active.Settled) { settledAt = -1f; return; }
            if (settledAt < 0f) settledAt = now;
            if (now-settledAt < 1f) return;
            configuration.ValidateRestingRoutes(director.player);
            RecalculateBlockedRoutes();
            active = null;
            nextConsideration = now + configuration.minimumInterval +
                (float)random.NextDouble() * (configuration.maximumInterval - configuration.minimumInterval);
        }
        if (now < nextConsideration) return;
        nextConsideration = now + 1f;
        var candidates = new List<FallingProp>();
        var weights = new List<float>();
        float total = 0;
        bool environmental = random.NextDouble() < configuration.environmentalChance;
        foreach (var prop in props)
        {
            if (prop.Committed) continue;
            // Never drop the opposite bookcase: its passage must remain physically reachable.
            if ((prop == props[2] || prop == props[6]) && prop != opening) continue;
            if (openingPending && prop != opening) continue;
            if (!openingPending && cautionPending && prop != caution) continue;
            int blocked = prop.routeObstacle ? configuration.BlockedRoutes(prop.predictedArea) : 0;
            if ((blockedRoutes | blocked) == 3) continue;
            var area = prop.predictedArea;
            if (!HasEscape(area, position)) continue;
            float distance = Vector3.Distance(area.ClosestPoint(position + travel), position + travel);
            float weight = environmental ? 1f : 1f + 24f / (1f + distance * distance);
            candidates.Add(prop); weights.Add(weight); total += weight;
        }
        if (candidates.Count == 0) return;
        float pick = (float)random.NextDouble() * total;
        for (int i = 0; i < candidates.Count; i++)
        {
            pick -= weights[i];
            if (pick > 0 && i < candidates.Count - 1) continue;
            var selected=candidates[i];
            Commit(selected,Mathf.Max(2.5f,selected==opening?configuration.openingWarningSeconds:configuration.warningSeconds),now);
            if (selected==opening) openingPending=false;
            else if (selected==caution) cautionPending=false;
            break;
        }
    }
    void Commit(FallingProp prop, float warning, float now)
    {
        active=prop; settledAt=-1f;
        if (prop.routeObstacle) blockedRoutes |= configuration.BlockedRoutes(prop.predictedArea);
        Debug.Log("Hazard committed: " + prop.label + " at " + now.ToString("0.00"));
        prop.Warn(director,warning,now);
    }
    void RecalculateBlockedRoutes()
    {
        blockedRoutes = 0;
        float stepHeight = director.player.GetComponent<CharacterController>().stepOffset;
        foreach (var prop in props)
        {
            if (!prop.Committed || !prop.routeObstacle) continue;
            if (prop.Settled && prop.RestingBounds.max.y <= stepHeight) continue;
            blockedRoutes |= configuration.BlockedRoutes(prop.Settled ? prop.RestingBounds : prop.predictedArea);
        }
    }
    bool HasEscape(Bounds area, Vector3 feet)
    {
        area.Expand(new Vector3(.8f, 0, .8f));
        var sample = feet; sample.y = area.center.y;
        if (!area.Contains(sample)) return true;
        float distance = director.player.crouchSpeed * .6f * Mathf.Max(2.5f, configuration.warningSeconds);
        foreach (var direction in new[] { Vector3.left, Vector3.right, Vector3.forward, Vector3.back })
        {
            for (float retreat=.25f;retreat<=distance;retreat+=.25f)
            {
                if (area.Contains(sample+direction*retreat)) continue;
                if (!Physics.CapsuleCast(feet+Vector3.up*.33f,feet+Vector3.up*.6f,
                    .3f,direction,out _,retreat,~0,QueryTriggerInteraction.Ignore)) return true;
                break;
            }
        }
        return false;
    }

}
