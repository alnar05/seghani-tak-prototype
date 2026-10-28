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

    void OnTriggerStay(Collider other)
    {
        var player = other.GetComponent<FirstPersonCrouchController>();
        if (!player) return;

        Vector3 p = player.transform.position;
        p.y = box.bounds.center.y;
        bool inside = box.bounds.Contains(p);

        PlayerInside = inside;
        if (!listening) return;
        SetSafe(player.IsCrouching && inside);
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.GetComponent<FirstPersonCrouchController>()) return;
        PlayerInside = false;
        if (listening) SetSafe(false);
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
