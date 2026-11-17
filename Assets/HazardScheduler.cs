using System.Collections.Generic;
using UnityEngine;

// Each cue identifies a preplaced object and fixes when and where it can first warn.
public sealed class HazardCue
{
    public readonly FallingProp Prop;
    public readonly float Earliest, Latest, Warning;
    public readonly Bounds? Approach;
    public bool Finished { get; set; }

    public HazardCue(FallingProp prop, float earliest, float latest, float warning, Bounds? approach)
    {
        Prop = prop;
        Earliest = earliest;
        Latest = latest;
        Warning = warning;
        Approach = approach;
    }
}

public class HazardScheduler : MonoBehaviour
{
    public EarthquakeDirector director;
    public Level1Configuration configuration;
    public FallingProp[] props;
    public List<HazardCue> cues;
    public float[] floorHeights;

    const float Step = .25f, ReactionReserve = .35f;
    static readonly Vector2Int[] Directions =
        { Vector2Int.left, Vector2Int.right, Vector2Int.up, Vector2Int.down };

    readonly Collider[] overlaps = new Collider[24];
    float nextCheck;
    FallingProp active;
    float activeUntil;
    bool cancelled;

    public void Tick(float now, float delta)
    {
        if (cancelled || director.CurrentPhase != EarthquakeDirector.Phase.Shaking) return;
        if (now >= director.quakeEnd) { CancelPending(); return; }

        if (active)
        {
            active.Tick(now);
            if (!active.Settled && now < activeUntil) return;
            active = null;
        }

        if (now < nextCheck) return;
        nextCheck = now + .25f;
        float shaking = now - director.quakeStart;
        Vector3 feet = director.player.transform.position;

        foreach (var cue in cues)
        {
            var prop = cue.Prop;
            if (cue.Finished || prop.Committed) continue;
            if (shaking > cue.Latest || now + cue.Warning + 1.5f > director.quakeEnd)
            {
                cue.Finished = true;
                continue;
            }

            if (shaking < cue.Earliest ||
                (cue.Approach.HasValue && !cue.Approach.Value.Contains(feet)) ||
                !HasResponse(prop, cue.Warning, feet)) continue;

            prop.Warn(director, cue.Warning, now);
            cue.Finished = true;
            active = prop;
            // A body stuck moving against a collider must not silence the rest of the round.
            activeUntil = now + cue.Warning + 2.5f;
            nextCheck = now + cue.Warning + .7f;
            return;
        }
    }

    public void CancelPending()
    {
        cancelled = true;
        foreach (var prop in props) prop.ClearWarning();
    }

    bool HasResponse(FallingProp prop, float warning, Vector3 start)
    {
        Bounds danger = prop.predictedArea;
        danger.Expand(new Vector3(.5f, 0f, .5f));
        float floor = ClosestFloor(start.y);
        // The two-storey layouts have independent safe approaches and no fixtures on the stairs.
        if (Mathf.Abs(start.y - floor) > .35f) return false;
        if (danger.min.y > floor + director.player.standHeight || danger.max.y < floor)
            return !prop.routeObstacle;

        Physics.SyncTransforms();
        Vector3 origin = new Vector3(start.x, floor, start.z);
        float refugeDistance = Mathf.Max(0f, warning - ReactionReserve) * director.player.crouchSpeed;
        // The search admits crouch-only clearance, so budget the same speed for the full route.
        float deadlineDistance = Mathf.Max(0f, director.quakeEnd - director.RoundTime - .8f) *
            director.player.crouchSpeed;

        var pending = new Queue<(Vector2Int cell, float distance)>();
        var visited = new HashSet<Vector2Int> { Vector2Int.zero };
        pending.Enqueue((Vector2Int.zero, 0f));
        bool startsInDanger = OnFootprint(danger, origin, floor);

        while (pending.Count > 0)
        {
            var (cell, distance) = pending.Dequeue();
            Vector3 feet = origin + new Vector3(cell.x * Step, 0f, cell.y * Step);
            bool threatened = OnFootprint(danger, feet, floor);
            if (!threatened && (!prop.routeObstacle || director.shelter.FitsCrouchAt(feet, director.player)))
                return true;

            foreach (var offset in Directions)
            {
                var next = cell + offset;
                if (visited.Contains(next)) continue;
                Vector3 point = origin + new Vector3(next.x * Step, 0f, next.y * Step);
                float nextDistance = distance + Step;
                if (nextDistance > (prop.routeObstacle ? deadlineDistance : refugeDistance)) continue;

                bool nextThreatened = OnFootprint(danger, point, floor);
                // Transit out of a fall path must fit the warning. Never plan a return through it.
                if (threatened && nextDistance > refugeDistance) continue;
                if (prop.routeObstacle && nextThreatened && (!startsInDanger || !threatened)) continue;
                if (!OtherMotionClear(point, floor, prop) || !Walkable(point)) continue;

                visited.Add(next);
                pending.Enqueue((next, nextDistance));
            }
        }
        return false;
    }

    bool OtherMotionClear(Vector3 feet, float floor, FallingProp candidate)
    {
        foreach (var other in props)
        {
            if (other == candidate || !other.Committed || other.Settled) continue;
            var area = other.predictedArea;
            area.Expand(new Vector3(.5f, 0f, .5f));
            if (OnFootprint(area, feet, floor)) return false;
        }
        return true;
    }

    bool OnFootprint(Bounds area, Vector3 feet, float floor) =>
        area.min.y <= floor + director.player.standHeight && area.max.y >= floor &&
        area.Contains(new Vector3(feet.x, area.center.y, feet.z));

    float ClosestFloor(float y)
    {
        float closest = floorHeights[0];
        foreach (float height in floorHeights)
            if (Mathf.Abs(y - height) < Mathf.Abs(y - closest)) closest = height;
        return closest;
    }

    bool Walkable(Vector3 feet)
    {
        if (Mathf.Abs(feet.x) > configuration.roomSize.x * .5f - .26f ||
            Mathf.Abs(feet.z) > configuration.roomSize.z * .5f - .26f) return false;

        var controller = director.player.GetComponent<CharacterController>();
        float radius = controller.radius;
        int count = Physics.OverlapCapsuleNonAlloc(
            feet + Vector3.up * (radius + .04f),
            feet + Vector3.up * (director.player.crouchHeight - radius + .04f),
            radius, overlaps, ~0, QueryTriggerInteraction.Ignore);
        if (count == overlaps.Length) return false;

        for (int i = 0; i < count; i++)
        {
            Collider hit = overlaps[i];
            if (hit == controller) continue;
            var fallen = hit.GetComponentInParent<FallingProp>();
            if (fallen && fallen.Settled && fallen.RestingBounds.max.y <= feet.y + controller.stepOffset)
                continue;
            return false;
        }
        // The floor opening over a staircase is not a path.
        return Physics.Raycast(feet + Vector3.up * .11f, Vector3.down, .2f, ~0,
            QueryTriggerInteraction.Ignore);
    }
}
