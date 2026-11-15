using UnityEngine;
using System.Collections.Generic;

// Preplaced fixtures and seeded route selection. A warning freezes that event's forces and deadline.
public class HazardScheduler : MonoBehaviour
{
    public EarthquakeDirector director;
    public Level1Configuration configuration;
    public FallingProp[] props;
    FallingProp opening;
    float earliestWarning, nextEnvironment;
    System.Random random;
    bool cancelled;

    void Start()
    {
        random = new System.Random(configuration.seed);
        int route = random.Next(2);
        nextEnvironment = director.quakeStart + 6f;
        opening = props[route == 0 ? 2 : 6];
        earliestWarning = director.quakeStart + configuration.openingDelayAfterOnset;
        Debug.Log("Level 1 seed=" + configuration.seed + " opening route=" + (route == 0 ? "south" : "north"));
    }

    public void Tick(float now, float delta)
    {
        if (cancelled || Time.timeScale <= 0 || director.CurrentPhase != EarthquakeDirector.Phase.Shaking) return;
        if (now >= director.quakeEnd) { CancelPending(); return; }
        foreach (var prop in props) prop.Tick(now);
        if (now < earliestWarning) return;
        TryCommit(opening, configuration.openingWarningSeconds, now);
        Vector3 feet = director.player.transform.position;
        // Each exit has its own installed fixture. Approaching either opening starts its warning;
        // choosing the other corridor does not bypass the second encounter.
        for (int i = 7; i <= 8; i++)
        {
            var prop = props[i];
            if (feet.x >= configuration.passageTriggerX && feet.x < prop.predictedArea.min.x &&
                Mathf.Abs(feet.z - prop.predictedArea.center.z) < 1.2f)
                TryCommit(prop, configuration.passageWarningSeconds, now);
        }
        if (feet.x >= configuration.approachTriggerX && feet.x < props[9].predictedArea.min.x)
            TryCommit(props[9], configuration.approachWarningSeconds, now);
        bool activeHazard = false;
        foreach (var prop in props)
            activeHazard |= prop.IsWarning || (prop.HasReleased && !prop.Settled);
        if (now >= nextEnvironment && !activeHazard)
        {
            nextEnvironment = now + configuration.minimumInterval +
                (float)random.NextDouble() * (configuration.maximumInterval-configuration.minimumInterval);
            if (random.NextDouble() < configuration.environmentalChance)
            {
                int index = random.Next(5);
                if (index >= 2) index++; // Existing room hazards, excluding both corridor bookcases.
                TryCommit(props[index], configuration.warningSeconds, now);
            }
        }
    }

    void TryCommit(FallingProp prop, float warning, float now)
    {
        // Keep the full reaction interval and leave time for the physical fall before results.
        if (cancelled || prop.Committed || now + warning + 2f > director.quakeEnd ||
            !HasResponse(prop, warning)) return;
        prop.Warn(director, warning, now);
        Debug.Log("Hazard committed: " + prop.label + " at " + now.ToString("0.00"));
    }

    public void CancelPending()
    {
        cancelled = true;
        foreach (var prop in props) prop.ClearWarning();
    }

    bool HasResponse(FallingProp prop, float warning)
    {
        Physics.SyncTransforms();
        Vector3 start = director.player.transform.position;
        Bounds danger = prop.predictedArea;
        danger.Expand(new Vector3(.8f, 0, .8f));
        // Flood the actual current walkable floor, including compound fallen-object colliders.
        // A route-blocking shelf needs a complete remaining path to shelter. Temporary flat
        // fixtures need a reachable refuge outside their footprint before release.
        const float step = .25f;
        var queue = new Queue<Vector2Int>();
        var visited = new HashSet<Vector2Int>();
        var distances = new Dictionary<Vector2Int,float>();
        var origin = new Vector2(start.x, start.z);
        queue.Enqueue(Vector2Int.zero); visited.Add(Vector2Int.zero); distances[Vector2Int.zero]=0f;
        float reach = Mathf.Max(0, warning - configuration.reactionReserve) * director.player.crouchSpeed;
        while (queue.Count > 0)
        {
            Vector2Int cell = queue.Dequeue();
            Vector3 feet = new Vector3(origin.x + cell.x*step, .03f, origin.y + cell.y*step);
            bool threatened = danger.Contains(new Vector3(feet.x,danger.center.y,feet.z));
            if (!threatened && OtherWarningsClear(feet,prop) && (!prop.routeObstacle || Vector3.Distance(feet,configuration.Routes[0][4]) < .4f)) return true;
            foreach (var offset in Directions)
            {
                var next = cell + offset;
                if (!visited.Add(next)) continue;
                Vector3 point = new Vector3(origin.x+next.x*step,.03f,origin.y+next.y*step);
                if (Mathf.Abs(point.x) > 5.65f || Mathf.Abs(point.z) > 3.15f) continue;
                if (!prop.routeObstacle && distances[cell]+step > reach) continue;
                // Shelves permanently occupy their envelope; don't commit one that cuts off this player.
                if (prop.routeObstacle && danger.Contains(new Vector3(point.x,danger.center.y,point.z))) continue;
                if (OtherWarningsClear(point,prop) && Walkable(point)) { distances[next]=distances[cell]+step; queue.Enqueue(next); }
            }
        }
        return false;
    }
    static readonly Vector2Int[] Directions = { Vector2Int.left,Vector2Int.right,Vector2Int.up,Vector2Int.down };
    bool OtherWarningsClear(Vector3 feet, FallingProp candidate)
    {
        foreach (var other in props)
        {
            if (other == candidate || !other.Committed || other.Settled) continue;
            var area = other.predictedArea;
            area.Expand(new Vector3(.8f,0,.8f));
            if (area.Contains(new Vector3(feet.x,area.center.y,feet.z))) return false;
        }
        return true;
    }
    bool Walkable(Vector3 feet)
    {
        foreach (var hit in Physics.OverlapCapsule(feet+Vector3.up*.33f,
            feet+Vector3.up*(director.player.crouchHeight-.3f),.3f,~0,QueryTriggerInteraction.Ignore))
        {
            if (hit.gameObject == director.player.gameObject) continue;
            var fallen = hit.GetComponentInParent<FallingProp>();
            if (fallen && fallen.Settled && fallen.RestingBounds.max.y <= director.player.GetComponent<CharacterController>().stepOffset) continue;
            return false;
        }
        return true;
    }
}
