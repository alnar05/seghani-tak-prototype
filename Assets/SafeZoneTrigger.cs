using UnityEngine;
using UnityEngine.UI;

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

    public BoxCollider[] volumes;

    bool Fits(Bounds body)
    {
        // Every shelter is aligned with the building grid, including desks turned 90 degrees.
        foreach (var box in volumes)
        {
            Bounds bounds = box.bounds;
            if (bounds.Contains(body.min + Vector3.one * .005f) &&
                bounds.Contains(body.max - Vector3.one * .005f)) return true;
        }
        return false;
    }

    public bool FitsCrouchAt(Vector3 feet, FirstPersonCrouchController player)
    {
        float diameter = player.GetComponent<CharacterController>().radius * 2f;
        var size = new Vector3(diameter, player.crouchHeight, diameter);
        return Fits(new Bounds(feet + Vector3.up * (size.y * .5f), size));
    }

    void Start() => Refresh();

    // Sample directly after movement and at impacts; do not depend on stale trigger callbacks.
    public void Evaluate(FirstPersonCrouchController player)
    {
        var controller = player.GetComponent<CharacterController>();
        PlayerInside = Fits(controller.bounds);
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
