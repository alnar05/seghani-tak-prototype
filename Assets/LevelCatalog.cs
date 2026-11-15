using UnityEngine;

public static class LevelCatalog
{
    public static readonly string[] Names =
    {
        "Living Room", "Home Office", "Kitchen & Dining", "Upstairs Study", "Two-Floor House"
    };

    public static readonly string[] Descriptions =
    {
        "Compare two passages from the living room to the study desk.",
        "Choose a short shelving aisle or the longer loop to the office desk.",
        "Cross the wide kitchen opening or take the side passage to the dining table.",
        "Find the study desk on the bedroom floor. A table also covers the downstairs sitting room.",
        "Learn both floors and find the nearest sturdy table or desk when the shaking starts."
    };

    public static int Count => Names.Length;

    public static Level1Configuration Create(int index)
    {
        index = Mathf.Clamp(index, 0, Count - 1);
        switch (index)
        {
            case 1:
                return new Level1Configuration
                {
                    roomSize = new Vector3(13f, 3f, 8.6f),
                    spawn = new Vector3(-5.55f, 0f, -1.8f),
                    explorationSeconds = 4f,
                    shakingSeconds = 18f,
                    seed = 2701,
                    coverInstruction = "Crouch fully under the office desk at 0 s.",
                    orientationTip = "Find the desk by either side of the central shelves."
                };
            case 2:
                return new Level1Configuration
                {
                    roomSize = new Vector3(13f, 3f, 8.6f),
                    spawn = new Vector3(-5.45f, 0f, -.25f),
                    explorationSeconds = 3f,
                    shakingSeconds = 20f,
                    seed = 3701,
                    coverInstruction = "Crouch fully under the dining table at 0 s.",
                    orientationTip = "Find the dining table through the wide opening or back passage."
                };
            case 3:
                return new Level1Configuration
                {
                    roomSize = new Vector3(13f, 3f, 9f),
                    spawn = new Vector3(-5.3f, 3.2f, -.2f),
                    explorationSeconds = 12f,
                    shakingSeconds = 22f,
                    seed = 4701,
                    coverInstruction = "Crouch fully under the study desk or sitting-room table at 0 s.",
                    orientationTip = "Look for the study on this floor before the shaking begins."
                };
            case 4:
                return new Level1Configuration
                {
                    roomSize = new Vector3(15f, 3f, 10f),
                    spawn = new Vector3(-6.25f, 0f, -1.3f),
                    explorationSeconds = 14f,
                    shakingSeconds = 26f,
                    seed = 5701,
                    coverInstruction = "Crouch fully under a sturdy table or desk at 0 s.",
                    orientationTip = "Find the dining table downstairs and the desk upstairs."
                };
            default:
                return new Level1Configuration();
        }
    }
}
