using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// The menu's vertical order also drives Tab and Shift+Tab.
public class MenuNavigation : MonoBehaviour
{
    Selectable[] controls;

    void OnEnable()
    {
        controls = Array.FindAll(GetComponentsInChildren<Selectable>(), control => control.IsInteractable());
        Array.Sort(controls, (first, second) => second.transform.position.y.CompareTo(first.transform.position.y));
        for (int i = 0; i < controls.Length; i++)
        {
            controls[i].navigation = new Navigation
            {
                mode = Navigation.Mode.Explicit,
                selectOnUp = controls[(i + controls.Length - 1) % controls.Length],
                selectOnDown = controls[(i + 1) % controls.Length]
            };
        }

        if (controls.Length > 0) controls[0].Select();
    }

    void Update()
    {
        if (!Input.GetKeyDown(KeyCode.Tab) || controls.Length == 0 || !EventSystem.current) return;

        int index = Array.FindIndex(controls, control =>
            control.gameObject == EventSystem.current.currentSelectedGameObject);
        bool backwards = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        int direction = backwards ? -1 : 1;
        if (index < 0) index = backwards ? 0 : -1;
        controls[(index + direction + controls.Length) % controls.Length].Select();
    }

    void LateUpdate()
    {
        if (controls.Length > 0 && EventSystem.current && !EventSystem.current.currentSelectedGameObject)
            controls[0].Select();
    }
}
