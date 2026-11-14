using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class FallingProp : MonoBehaviour
{
    public string label = "Falling object";
    public Vector3 pushImpulse;
    public Vector3 pushPointLocal = new Vector3(0f, 0.5f, 0f);
    public Vector3 torqueImpulse;
    public float minimumImpactSpeed = 1f;
    public float cameraKickOnImpact = 0.7f;

    public Bounds predictedArea;
    public bool routeObstacle = true;
    public bool Committed { get; private set; }
    public bool Settled => triggered && (rb.IsSleeping() ||
        (rb.velocity.sqrMagnitude < .01f && rb.angularVelocity.sqrMagnitude < .01f));
    public Bounds RestingBounds
    {
        get
        {
            Bounds bounds = colliders[0].bounds;
            foreach (var collider in colliders) bounds.Encapsulate(collider.bounds);
            return bounds;
        }
    }
    public Transform warningVisual;
    public bool IsWarning { get; private set; }
    public bool HasReleased => triggered;
    public bool HasImpacted => playedImpact;
    Collider[] colliders;
    float warningStart, releaseAt;
    Material warningMaterial;
    Vector3 committedPush, committedTorque;
    GameObject warningMarker;
    AudioSource warningAudio;

    public void Warn(EarthquakeDirector owner, float seconds, float now)
    {
        if (Committed || owner.CurrentPhase != EarthquakeDirector.Phase.Shaking) return;
        Committed = IsWarning = true; director=owner;
        committedPush = pushImpulse; committedTorque = torqueImpulse;
        warningStart=now; releaseAt=now+seconds;
        warningMarker = GameObject.CreatePrimitive(PrimitiveType.Cube);
        warningMarker.name = label + " fall warning";
        var markerCollider=warningMarker.GetComponent<Collider>();
        markerCollider.enabled=false; Destroy(markerCollider);
        warningMarker.transform.position = new Vector3(predictedArea.center.x,.015f,predictedArea.center.z);
        warningMarker.transform.localScale = new Vector3(predictedArea.size.x,.02f,predictedArea.size.z);
        warningMaterial=warningMarker.GetComponent<Renderer>().material;
        warningAudio=gameObject.AddComponent<AudioSource>();
        warningAudio.playOnAwake=false;
        warningAudio.spatialBlend=.55f; warningAudio.minDistance=4f; warningAudio.maxDistance=18f;
        warningAudio.rolloffMode=AudioRolloffMode.Linear;
        warningAudio.clip=SafeFeedback.MakeTone("Furniture creak",new[]{260f,340f},.1f,.5f,.35f,4f);
        GameAudio.Bind(warningAudio,GameAudio.Bus.Warning);
        warningAudio.loop=true; warningAudio.Play();
    }

    // Called only by the scheduler on the director's round timeline, never by a separate timer.
    public void Tick(float now)
    {
        if (!IsWarning) return;
        if (director.CurrentPhase != EarthquakeDirector.Phase.Shaking) { ClearWarning(); return; }
        if (now >= releaseAt) { ClearWarning(); Trigger(director); return; }
        float t=now-warningStart;
        warningMaterial.color=Color.Lerp(new Color(.4f,.15f,0),Color.yellow,.5f+.5f*Mathf.Sin(t*8));
        if (warningVisual)
        {
            float envelope=Mathf.Clamp01(t/.25f)*Mathf.Clamp01((releaseAt-now)/.25f);
            warningVisual.localRotation=Quaternion.Euler(.6f*Mathf.Sin(t*14)*envelope,0,0);
        }
    }

    public void ClearWarning()
    {
        IsWarning=false;
        if (warningVisual) warningVisual.localRotation=Quaternion.identity;
        if (warningAudio) GameAudio.FadeOut(warningAudio);
        if (warningMarker) Destroy(warningMarker);
        if (warningMaterial) Destroy(warningMaterial);
    }

    Rigidbody rb;
    EarthquakeDirector director;
    Vector3 velocityBeforeStep, angularVelocityBeforeStep;
    bool triggered, playedImpact;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        colliders=GetComponentsInChildren<Collider>();
        rb.isKinematic = true;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
    }

    public void Trigger(EarthquakeDirector owner)
    {
        if (triggered || owner.CurrentPhase != EarthquakeDirector.Phase.Shaking) return;
        if (!Committed) { committedPush = pushImpulse; committedTorque = torqueImpulse; Committed = true; }
        triggered = true;
        director = owner;
        rb.isKinematic = false;
        rb.AddForceAtPosition(committedPush, transform.TransformPoint(pushPointLocal), ForceMode.Impulse);
        rb.AddTorque(committedTorque, ForceMode.Impulse);
    }

    void FixedUpdate()
    {
        // Preserve incoming motion: collision resolution can stop the body before callbacks.
        velocityBeforeStep = rb.velocity;
        angularVelocityBeforeStep = rb.angularVelocity;
    }

    Vector3 IncomingVelocity(Vector3 point) => velocityBeforeStep +
        Vector3.Cross(angularVelocityBeforeStep, point - rb.worldCenterOfMass);

    bool Active => triggered && director && Time.timeScale > 0f &&
                   director.CurrentPhase == EarthquakeDirector.Phase.Shaking;

    void OnCollisionEnter(Collision collision)
    {
        if (!Active) return;
        foreach (ContactPoint contact in collision.contacts)
        {
            // Collision normals point toward this prop; negate for motion into the other body.
            float speed = Vector3.Dot(IncomingVelocity(contact.point), -contact.normal);
            if (speed < minimumImpactSpeed) continue;
            if (!playedImpact)
            {
                playedImpact = true;
                director.PlayCrash();
                director.shake.Kick(cameraKickOnImpact);
            }
            if (collision.gameObject.GetComponent<FirstPersonCrouchController>())
                TryDamage(speed);
        }
    }

    // CharacterController movement uses its own contact callback rather than rigidbody collisions.
    public void HitController(ControllerColliderHit hit)
    {
        if (!Active) return;
        // Use current motion here: the controller may walk into an already settled prop.
        TryDamage(Vector3.Dot(rb.GetPointVelocity(hit.point), hit.normal));
    }

    void TryDamage(float incomingSpeed)
    {
        if (incomingSpeed < minimumImpactSpeed) return;
        director.shelter.Evaluate(director.player);
        if (!director.shelter.IsSafe) director.KillPlayer(label + " struck you.");
    }
}
