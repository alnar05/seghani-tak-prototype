using UnityEngine;

[System.Serializable]
public class Level1Configuration
{
    public Vector3 roomSize = new Vector3(12f, 3f, 7f);
    public Vector3 spawn = new Vector3(-5f, 0f, -1.65f);
    public float explorationSeconds = 3f;
    public float shakingSeconds = 15f;
    public int seed = 1701;
    public float openingDelayAfterOnset = .18f;
    public float openingWarningSeconds = 2.7f;
    public bool allowJump;
    public float walkSpeed = 2.2f;
    public float crouchSpeed = 1.5f;
    public string coverInstruction = "Crouch fully under the study desk at 0 s.";
    public string orientationTip = "Find the study desk through either passage.";
    public const string MovementHint = "WASD / arrows: move   Mouse: look";
    public const float UpperFloor = 3.2f;

    // Reference paths used by the Level 1 authoring tools.
    public static readonly Vector3 SpawnPosition = new Vector3(-5f, 0f, -1.65f);
    public static readonly Vector3 DeskPosition = new Vector3(5.1f, 0f, 0f);
    public float CrouchApproachX { get; private set; } = 4.1f;
    public readonly Vector3[][] Routes =
    {
        new[] { SpawnPosition, new Vector3(-2.5f, 0f, -1.4f), new Vector3(2.5f, 0f, -1.4f), new Vector3(4f, 0f, 0f), DeskPosition },
        new[] { SpawnPosition, new Vector3(-4.5f, 0f, 1.65f), new Vector3(-2.5f, 0f, 1.4f), new Vector3(2.5f, 0f, 1.4f), new Vector3(4f, 0f, 0f), DeskPosition }
    };

    public void SetDeskPosition(Vector3 position, float frontEdge)
    {
        foreach (var route in Routes) route[route.Length - 1] = position;
        CrouchApproachX = frontEdge - .4f;
    }
}
