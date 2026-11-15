using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

// Shared Tab order and explicit vertical navigation; sliders retain Left/Right adjustment.
public class MenuNavigation : MonoBehaviour
{
    Selectable[] controls;
    void OnEnable()
    {
        controls = GetComponentsInChildren<Selectable>();
        var enabledControls = new System.Collections.Generic.List<Selectable>();
        foreach (var control in controls) if (control.IsInteractable()) enabledControls.Add(control);
        controls = enabledControls.ToArray();
        // Position order also covers panels whose controls were constructed in a different order.
        System.Array.Sort(controls,(a,b) => b.transform.position.y.CompareTo(a.transform.position.y));
        for (int i=0;i<controls.Length;i++)
            controls[i].navigation = new Navigation { mode=Navigation.Mode.Explicit,
                selectOnUp=controls[(i+controls.Length-1)%controls.Length],
                selectOnDown=controls[(i+1)%controls.Length] };
        if (controls.Length>0) controls[0].Select();
    }
    void Update()
    {
        if (OverlayMenus.BlocksPause || !Input.GetKeyDown(KeyCode.Tab) || controls.Length==0 || !EventSystem.current) return;
        int index=System.Array.FindIndex(controls,c=>c.gameObject==EventSystem.current.currentSelectedGameObject);
        int direction=Input.GetKey(KeyCode.LeftShift)||Input.GetKey(KeyCode.RightShift)?-1:1;
        controls[(index+direction+controls.Length)%controls.Length].Select();
    }
}
