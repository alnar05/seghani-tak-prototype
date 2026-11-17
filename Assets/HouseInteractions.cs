using UnityEngine;
using UnityEngine.UI;

public sealed class HouseInteractions : MonoBehaviour
{
    public Transform key, shortcutHinge, jammedDoor;
    FirstPersonCrouchController player;
    EarthquakeDirector director;
    Text prompt;
    bool hasKey, shortcutOpen;
    float feedbackUntil;

    public void Initialize(FirstPersonCrouchController controller, EarthquakeDirector round, Text label)
    {
        player = controller;
        director = round;
        prompt = label;
    }

    void Update()
    {
        if (!director || director.CurrentPhase == EarthquakeDirector.Phase.Ended || Time.timeScale <= 0f) return;
        if (Time.time < feedbackUntil) return;

        bool orienting = director.CurrentPhase == EarthquakeDirector.Phase.Calm;
        bool nearKey = orienting && !hasKey && Nearby(key, Vector3.zero);
        bool nearJammedDoor = Nearby(jammedDoor, Vector3.up + Vector3.forward * .51f);
        bool nearShortcut = !shortcutOpen && Nearby(shortcutHinge, Vector3.up + Vector3.forward * .51f);
        prompt.text = orienting && !hasKey ? "Optional shortcut key on the entry wall" : "";
        if (nearKey) prompt.text = "E  Pick up the brass key";
        else if (nearJammedDoor) prompt.text = "E  Check the damaged door";
        else if (nearShortcut) prompt.text = hasKey ? "E  Unlock the shortcut" : "E  Check the locked shortcut";

        if (!Input.GetKeyDown(KeyCode.E)) return;
        if (nearKey)
        {
            hasKey = true;
            key.gameObject.SetActive(false);
            Show("Brass key collected");
        }
        else if (nearJammedDoor) Show("The door is jammed. Use either side passage.");
        else if (nearShortcut)
        {
            if (!hasKey) Show("Locked. The brass key is on the entry wall during orientation.");
            else if (WouldSwingIntoPlayer()) Show("Step back to give the door room to open.");
            else
            {
                shortcutOpen = true;
                shortcutHinge.localRotation = Quaternion.Euler(0f, 90f, 0f);
                Show("Shortcut unlocked");
            }
        }
    }

    bool Nearby(Transform target, Vector3 offset)
    {
        Vector3 look = player.cameraPivot.position;
        Vector3 point = target.TransformPoint(offset);
        Vector3 delta = point - look;
        if (delta.sqrMagnitude > 1.65f * 1.65f ||
            Vector3.Dot(player.cameraPivot.forward, delta.normalized) < -.15f) return false;

        if (!Physics.Raycast(look, delta.normalized, out var hit, delta.magnitude, ~0, QueryTriggerInteraction.Ignore))
            return true;
        return hit.transform == target || hit.transform.IsChildOf(target);
    }

    bool WouldSwingIntoPlayer()
    {
        Vector3 delta = player.transform.position - shortcutHinge.position;
        return delta.x > -.25f && delta.x < 1.37f && delta.z > -.38f && delta.z < 1.38f;
    }

    void Show(string text)
    {
        prompt.text = text;
        feedbackUntil = Time.time + 1.6f;
    }
}
