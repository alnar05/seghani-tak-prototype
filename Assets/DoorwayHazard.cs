using UnityEngine;

public class DoorwayHazard : MonoBehaviour
{
    public EarthquakeDirector director;
    public Vector3 returnPosition;

    bool penalized;

    void OnTriggerEnter(Collider other)
    {
        var p = other.GetComponent<FirstPersonCrouchController>();
        if (!p || director.CurrentPhase != EarthquakeDirector.Phase.Shaking) return;

        var cc = other.GetComponent<CharacterController>();
        cc.enabled = false;
        p.transform.position = returnPosition;
        cc.enabled = true;

        if (!penalized)
        {
            penalized = true;
            director.doorwayHit = true;
            director.KillPlayer("A doorframe collapsed on you!");
        }
    }
}