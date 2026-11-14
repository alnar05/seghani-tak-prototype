using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(BoxCollider))]
public class SafeZoneTrigger : MonoBehaviour
{
    public Text objectiveText;
    public string objectiveMessage = "Objective: Go under the desk.";
    public string safeMessage = "Safe! You are under the desk.";
    public Color objectiveColor = Color.white;
    public Color safeColor = new Color(0.35f, 1f, 0.45f);

    public bool listening = true;

    public bool IsSafe { get; private set; }
    public bool PlayerInside { get; private set; }

    public event System.Action<bool> OnSafeChanged;

    BoxCollider box;

    void Awake()
    {
        box = GetComponent<BoxCollider>();
        box.isTrigger = true;
    }

    void Start() => Refresh();

    // Sample directly after movement and at impacts; do not depend on stale trigger callbacks.
    public void Evaluate(FirstPersonCrouchController player)
    {
        var controller = player.GetComponent<CharacterController>();
        // Require the complete capsule bounds to fit inside the leg-to-leg usable space.
        Bounds body = controller.bounds, bounds = box.bounds;
        PlayerInside = bounds.Contains(body.min + Vector3.one*.005f) &&
                       bounds.Contains(body.max - Vector3.one*.005f);
        SetSafe(listening && player.IsCrouching && PlayerInside);
    }

    public void Clear()
    {
        listening = false;
        PlayerInside = false;
        SetSafe(false);
        if (objectiveText) objectiveText.text = "";
    }

    void SetSafe(bool safe)
    {
        if (safe == IsSafe) return;
        IsSafe = safe;
        Refresh();
        OnSafeChanged?.Invoke(safe);
    }

    public void Refresh()
    {
        if (!objectiveText) return;
        objectiveText.text = IsSafe ? safeMessage : objectiveMessage;
        objectiveText.color = IsSafe ? safeColor : objectiveColor;
    }
}
