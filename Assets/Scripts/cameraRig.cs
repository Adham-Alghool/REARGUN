using UnityEngine;

/// <summary>
/// Camera rig that follows a target as if attached to it by a damped spring,
/// instead of a simple Lerp/SmoothDamp. Gives physically-based overshoot,
/// oscillation, and settle behavior that's easy to tune with two parameters.
/// </summary>
public class SpringCameraRig : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;

    [Header("Position Spring")]
    [Tooltip("Higher stiffness makes the spring more stiff. Duh.")]
    [SerializeField] private float stiffness = 120f;
    [Tooltip("Higher = less oscillation.")]
    [SerializeField] private float damping = 20f;

    [Header("Rotation Spring")]
    [SerializeField] private bool springRotation = true;
    [Tooltip("Higher stiffness makes the spring more stiff. Duh.")]
    [SerializeField] private float rotationStiffness = 150f;
    [Tooltip("Higher = less oscillation.")]
    [SerializeField] private float rotationDamping = 20f;

    [Header("Position Spring")]
    [SerializeField] private float maxDistance = 15f;

    [Header("Collision")]
    [SerializeField] private LayerMask collisionMask = ~0;
    [SerializeField] private float collisionRadius = 0.3f;
    [SerializeField] private float bounceRestitution = 0.4f; // 0 = just stops, 1 = full bounce
    [SerializeField] private float skinWidth = 0.05f;

    [Header("Add impulse vector for a shake.")]
    [Tooltip("Instantaneous velocity added to the spring, e.g. from an explosion or landing.")]
    private Vector3 pendingImpulse;

    private Vector3 velocity;
    private Vector3 angularVelocity;

    private void Reset()
    {
        if (target == null && Camera.main != null)
            target = Camera.main.transform.parent;
    }

    private void LateUpdate()
    {
        if (target == null)
        {
            Debug.LogError("cameraRig doesn't have a target!");
            return;
        }

        float dt = Time.deltaTime;
        if (dt <= 0f) return;

        AdjustPosition(dt);

        if (springRotation) AdjustRotation(dt);
        else transform.LookAt(target.forward);
    }

    private void AdjustPosition(float dt)
    {
        // Apply the requested impulse and prepare it for the next request.
        if (pendingImpulse != Vector3.zero)
        {
            velocity += pendingImpulse;
            pendingImpulse = Vector3.zero;
        }

        Vector3 displacement = transform.position - target.position;
        Vector3 springForce = -stiffness * displacement;
        Vector3 dampingForce = -damping * velocity;
        Vector3 acceleration = springForce + dampingForce;

        velocity += acceleration * dt;
        Vector3 nextPosition = transform.position + velocity * dt;

        nextPosition = ClampDistance(nextPosition);          // clamp the destination, not the current position
        transform.position = ResolveCollisions(transform.position, nextPosition);
    }

    private Vector3 ClampDistance(Vector3 candidatePosition)
    {
        Vector3 displacement = candidatePosition - target.position;
        if (displacement.magnitude <= maxDistance) return candidatePosition;

        Vector3 clampedDir = displacement.normalized;
        Vector3 clampedPosition = target.position + clampedDir * maxDistance;

        float outwardSpeed = Vector3.Dot(velocity, clampedDir);
        if (outwardSpeed > 0f) velocity -= clampedDir * outwardSpeed;

        return clampedPosition;
    }

    private Vector3 ResolveCollisions(Vector3 fromPos, Vector3 toPos)
    {
        Collider[] overlappingColliders = Physics.OverlapSphere(toPos, collisionRadius, collisionMask, QueryTriggerInteraction.Ignore);

        foreach (Collider collider in overlappingColliders)
        {
            // Point on the collider's surface nearest to where we're trying to go.
            Vector3 nearestSurfacePoint = collider.ClosestPoint(toPos);

            // Vector pointing from the surface out to the camera. Its length tells us how deep we're overlapping (if it's shorter than collisionRadius, we're inside).
            Vector3 awayFromSurface = toPos - nearestSurfacePoint;
            float penetrationDepth = collisionRadius - awayFromSurface.magnitude;

            bool isPenetrating = penetrationDepth > 0f;
            if (!isPenetrating) continue;

            // Edge case: if the camera's center is exactly ON or past the surface,
            // awayFromSurface has ~zero length and no usable direction.
            // Fall back to pushing opposite the camera's current velocity instead.
            Vector3 pushDirection = awayFromSurface.sqrMagnitude > 0.0001f
                ? awayFromSurface.normalized
                : -velocity.normalized;

            // Push the camera out to the surface, plus a small buffer (skinWidth)
            // so it doesn't sit exactly on it and re-penetrate again next frame.
            toPos += pushDirection * (penetrationDepth + skinWidth);

            // Only bounce velocity if it's still heading INTO the surface.
            // Without this check, the spring keeps re-accelerating the camera back
            // toward the target (which is inside the car) every frame, and we'd
            // reflect an already-outgoing velocity over and over — that's what
            // was causing the jitter instead of one clean bounce.
            bool movingIntoSurface = Vector3.Dot(velocity, pushDirection) < 0f;
            if (movingIntoSurface)
            {
                velocity = Vector3.Reflect(velocity, pushDirection) * bounceRestitution;
            }
        }

        return toPos;
    }

    // I have NO IDEA what this does. AI knows more math than me so we'll see if it works.
    private void AdjustRotation(float dt)
    {
        Quaternion desiredRotation = Quaternion.LookRotation(target.forward, target.up); 

        // Work in the tangent space of a quaternion via the angle-axis difference,
        // treated as a small rotation vector for the spring math.
        Quaternion delta = desiredRotation * Quaternion.Inverse(transform.rotation);
        delta.ToAngleAxis(out float angleDeg, out Vector3 axis);

        // Normalize angle to [-180, 180] so the spring doesn't fight the long way around.
        if (angleDeg > 180f) angleDeg -= 360f;
        Vector3 rotationError = -axis.normalized * angleDeg * Mathf.Deg2Rad;

        if (float.IsNaN(rotationError.x)) rotationError = Vector3.zero;

        Vector3 springTorque = -rotationStiffness * rotationError;
        Vector3 dampingTorque = -rotationDamping * angularVelocity;
        Vector3 angularAcceleration = springTorque + dampingTorque;

        angularVelocity += angularAcceleration * dt;
        transform.rotation = Quaternion.Euler(angularVelocity * dt * Mathf.Rad2Deg) * transform.rotation;
    }

    /// Call this to kick the camera. AddImpulse(hitDirection * force)
    public void AddImpulse(Vector3 impulse)
    {
        pendingImpulse += impulse;
    }

    /// Snap instantly to the target, killing all velocity (e.g. on level load).
    public void SnapToTarget()
    {
        if (target == null) return;
        transform.position = target.position;
        transform.rotation = Quaternion.LookRotation(target.position - transform.position, Vector3.up);
        velocity = Vector3.zero;
        angularVelocity = Vector3.zero;
    }
}