using UnityEngine;

public class GunEffects : MonoBehaviour
{
    [Header("Toggles")]
    [SerializeField] bool Recoil = true;
    [SerializeField] bool HasMount = true;

    [Header("Recoil Parameters")]
    [SerializeField] Transform originalPosition;
    [SerializeField] float recoilStrength = 60f;
    [SerializeField] float camShakeStrength = 1f;
    [Tooltip("Higher stiffness makes the spring more stiff. Duh.")]
    [SerializeField] private float stiffness = 5000f;
    [Tooltip("Higher = less oscillation.")]
    [SerializeField] private float damping = 80f;
    [SerializeField] float maxVelocity = 3f;

    [Header("Turret Mount")]
    [SerializeField] Transform yawAxis;
    [SerializeField] Transform pitchAxis;

    [SerializeField] SpringCameraRig camRig;

    private Vector3 pendingImpulse;
    private Vector3 velocity;

    GunCore gun;

    void Start()
    {
        gun = GetComponent<GunCore>();
        gun.OnFired += AddKickback;
    }

    // Update is called once per frame
    void Update()
    {
        if (Recoil) UpdateGunRecoil();
        if (HasMount) UpdateMount();
    }

    void UpdateMount()
    {
        Vector3 gunLocRot = gun.gunBody.localEulerAngles;

        yawAxis.localRotation = Quaternion.Euler(0f, gunLocRot.y, 0f);
        pitchAxis.localRotation = Quaternion.Euler(gunLocRot.x, 0f, 0f);
    }

    void UpdateGunRecoil()
    {
        // Apply the requested impulse and prepare it for the next request.
        if (pendingImpulse != Vector3.zero)
        {
            velocity += pendingImpulse;
            pendingImpulse = Vector3.zero;
        }

        Vector3 displacement = gun.gunBody.position - originalPosition.position;
        Vector3 springForce = -stiffness * displacement;
        Vector3 dampingForce = -damping * velocity;
        Vector3 acceleration = springForce + dampingForce;

        velocity += acceleration * Time.deltaTime;

        velocity = Vector3.ClampMagnitude(velocity, maxVelocity);

        gun.gunBody.position = gun.gunBody.position + velocity * Time.deltaTime;
    }

    void AddKickback()
    {
        Vector3 impulse = -gun.gunBarrel.forward * recoilStrength;
        pendingImpulse += impulse;

        Vector3 camShakeDir = Random.onUnitSphere * camShakeStrength;
        camRig.AddImpulse(camShakeDir);
    }
}
