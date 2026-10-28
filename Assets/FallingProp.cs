using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class FallingProp : MonoBehaviour
{
    public string label = "Falling object";
    public float impactDelay = 0.7f;

    [Header("Hazard zone (world space)")]
    public Vector3 hazardCenter;
    public Vector3 hazardSize = Vector3.one;

    [Header("Push applied when it starts to fall")]
    public Vector3 pushImpulse;
    public Vector3 pushPointLocal = new Vector3(0f, 0.5f, 0f);
    public Vector3 torqueImpulse;

    public float cameraKickOnImpact = 0.7f;
    Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
    }

    public void Trigger(EarthquakeDirector director) => StartCoroutine(Fall(director));

    IEnumerator Fall(EarthquakeDirector d)
    {
        rb.isKinematic = false;
        rb.AddForceAtPosition(pushImpulse, transform.TransformPoint(pushPointLocal), ForceMode.Impulse);
        rb.AddTorque(torqueImpulse, ForceMode.Impulse);

        yield return new WaitForSeconds(impactDelay);

        if (d.CurrentPhase == EarthquakeDirector.Phase.Ended) yield break;

        d.PlayCrash();
        if (d.shake) d.shake.Kick(cameraKickOnImpact);

        Vector3 p = d.player.transform.position + Vector3.up * 0.5f;
        var zone = new Bounds(hazardCenter, hazardSize);
        if (zone.Contains(p) && !d.shelter.PlayerInside)
            d.KillPlayer(label);
    }
}