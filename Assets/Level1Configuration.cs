using UnityEngine;

[System.Serializable]
public class Level1Configuration
{
    public Vector3 roomSize = new Vector3(12, 3, 7);
    public float explorationSeconds = 2.5f;
    public float shakingSeconds = 35f;
    public int seed = 1701;
    public float warningSeconds = 3f;
    public float openingWarningSeconds = 2.75f;
    public float openingDelayAfterOnset = .75f;
    public bool allowJump = false;
    public float walkSpeed = 2.2f;
    public float crouchSpeed = 1.5f;
    public const string MovementHint = "WASD / arrows: move   Mouse: look";
    public float minimumInterval = 1f;
    public float maximumInterval = 2f;
    [Range(0f, 1f)] public float environmentalChance = .3f;

    public static readonly Vector3 SpawnPosition = new Vector3(-5,0,-1.65f);
    public static readonly Vector3 DeskPosition = new Vector3(5.1f,0,0);
    public float CrouchApproachX { get; private set; } = 4.1f;

    // Swept + resting envelopes. Never used to deal damage.
    public void AuthorHazards(FallingProp[] props)
    {
        props[0].predictedArea = new Bounds(new Vector3(-5.5f,1,2.7f),new Vector3(.8f,2,.8f));
        props[1].predictedArea = new Bounds(new Vector3(3,1,-2.7f),new Vector3(.8f,3,.8f));
        props[2].predictedArea = new Bounds(new Vector3(1f,1.1f,-1.6f),new Vector3(2f,2.4f,2.4f));
        props[3].predictedArea = new Bounds(new Vector3(-5.4f,1,-2.8f),new Vector3(1.4f,2,1.2f));
        props[4].predictedArea = new Bounds(new Vector3(-3.4f,1,3),new Vector3(1.6f,2,.6f));
        props[5].predictedArea = new Bounds(new Vector3(-5,1,-1.65f),new Vector3(1.1f,3,1.1f));
        props[6].predictedArea = new Bounds(new Vector3(1f,1.1f,1.6f),new Vector3(2f,2.4f,2.4f));
        props[7].predictedArea = new Bounds(new Vector3(0,1,-1.4f),new Vector3(1.4f,3,1.4f));
        props[8].predictedArea = new Bounds(new Vector3(0,1,1.4f),new Vector3(1.4f,3,1.4f));
    }

    public readonly Vector3[][] Routes = {
        new[] { SpawnPosition, new Vector3(-2.5f,0,-1.4f),new Vector3(2.5f,0,-1.4f),new Vector3(4,0,0),DeskPosition },
        new[] { SpawnPosition,new Vector3(-4.5f,0,1.65f),new Vector3(-2.5f,0,1.4f),new Vector3(2.5f,0,1.4f),new Vector3(4,0,0),DeskPosition }
    };

    public void SetDeskPosition(Vector3 position, float frontEdge)
    {
        foreach (var route in Routes) route[route.Length-1] = position;
        CrouchApproachX = frontEdge - .4f;
    }

    // A bit per route. Accumulate committed obstacles so at least one complete approach survives.
    public int BlockedRoutes(Bounds area)
    {
        area.Expand(new Vector3(.8f, 0, .8f));
        int blocked = 0;
        for (int r = 0; r < Routes.Length; r++)
        {
            var route = Routes[r];
            for (int i = 1; i < route.Length; i++)
                for (int step = 0; step <= 40; step++)
                {
                    var point = Vector3.Lerp(route[i - 1], route[i], step / 40f);
                    point.y = area.center.y;
                    if (area.Contains(point)) blocked |= 1 << r;
                }
        }
        return blocked;
    }

    // Test actual colliders, not only the predicted hazard boxes. Last approach requires crouching.
    public bool RouteIsClear(int routeIndex, FirstPersonCrouchController player, out string obstruction)
    {
        Physics.SyncTransforms();
        var controller = player.GetComponent<CharacterController>();
        var route = Routes[routeIndex];
        for (int i = 1; i < route.Length; i++)
        {
            int samples = Mathf.CeilToInt(Vector3.Distance(route[i-1],route[i]) / .08f);
            for (int step = 0; step <= samples; step++)
            {
                var feet = Vector3.Lerp(route[i-1],route[i],step/(float)samples);
                float height = feet.x > CrouchApproachX ? player.crouchHeight : player.standHeight;
                foreach (var hit in Physics.OverlapCapsule(feet + Vector3.up * .33f,
                    feet + Vector3.up * (height-.27f), .3f, ~0, QueryTriggerInteraction.Ignore))
                {
                    if (hit.gameObject == player.gameObject) continue;
                    // A settled, low fixture is traversable with the existing controller step offset.
                    var prop = hit.GetComponent<FallingProp>();
                    if (prop && prop.Settled && hit.bounds.max.y <= controller.stepOffset) continue;
                    obstruction = hit.name + " at " + feet;
                    return false;
                }
            }
        }
        obstruction = "";
        return true;
    }

    public void ValidateRoutes(FirstPersonCrouchController player)
    {
        for (int r=0; r<Routes.Length; r++)
            if (!RouteIsClear(r,player,out string obstruction))
                Debug.LogError("Initial Level 1 route " + r + " obstructed: " + obstruction);
    }

    public void ValidateRestingRoutes(FirstPersonCrouchController player)
    {
        bool shortClear = RouteIsClear(0,player,out string shortObstacle);
        bool alternateClear = RouteIsClear(1,player,out string alternateObstacle);
        if (!shortClear && !alternateClear)
            Debug.LogError("Both Level 1 approaches blocked after settlement: " + shortObstacle + "; " + alternateObstacle);
        else Debug.Log("Resting-layout check: short=" + shortClear + " alternate=" + alternateClear);
    }
}
