using System.Collections.Generic;
using UnityEngine;

public class CarBase : MonoBehaviour
{
    [Header("Waypoint Related Info")]
    [SerializeField] List<Waypoint> waypoints = new List<Waypoint>();
    [Tooltip("How close the car can get before switching waypoints")]
    [SerializeField] float wpThreshhold = 10f;
    [SerializeField] float blendingThreshhold = 40f;
    int currentWaypoint = 0;

    [Header("Wheel Refs (Respective)")]
    [Tooltip("Set rear wheels to be of index 3, 4 for correct steering/motor behavior")]
    [SerializeField] List<GameObject> wheelModels;
    public List<WheelCollider> wheels;
    [SerializeField] Vector3[] meshRotationOffset;

    [Header("Engine Performance")]
    [SerializeField] float maxSpeed = 300f;
    public float torque = 20f;
    private float originalTorque;

    [Header("Steering")]
    [Tooltip("The maximum steering angle at low speeds.")]
    [SerializeField] float maxSteerAngleLo = 60f;
    [Tooltip("The maximum steering angle at top speeds.")]
    [SerializeField] float maxSteerAngleHi = 10f;
    [Tooltip("Measured in degress / sec")]
    [SerializeField] float steerSpeed = 120f;

    private CarTransmission transmission;

    [Header("Center of Mass")]
    float CMY_offset = -1f;
    float CMX_offset = 0f;

    Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        transmission = GetComponent<CarTransmission>();
        originalTorque = torque;

        // Setup CM
        Vector3 centerOfMass = rb.centerOfMass;
        centerOfMass.y += CMY_offset;
        centerOfMass.x += CMX_offset;
        rb.centerOfMass = centerOfMass;
    }

    // Update is called once per frame
    void Update()
    {
        // Steps:
        // 1. Move as fast as your speed limit allows you. (DONE)
        // 2. Steer towards the target
        // 3. Slow down if off-target and time to correct is low.
        // 4. That's it (so far).

        MatchWheelMeshes();
        HandleNavigation();
    }

    private void FixedUpdate()
    {
        FloorIt();
        Steer();
    }

    void MatchWheelMeshes()
    {
        for (int i = 0; i < wheelModels.Count; i++)
        {
            wheels[i].GetWorldPose(out Vector3 pos, out Quaternion rot);
            wheelModels[i].transform.position = pos;
            wheelModels[i].transform.rotation = rot * Quaternion.Euler(meshRotationOffset[i]);
        }
    }

    float GetSpeed() //Get our car's speed on the plane it's actually driving on.
    {
        return Vector3.Dot(rb.linearVelocity, transform.forward);
    }

    
    float GetAppropriateTorque()
    {
        Vector3 toTarget = (GetSteerTarget() - transform.position).normalized;
        float alignment = Vector3.Dot(transform.forward, toTarget); // -1..1, not distance-scaled
        float factor = Mathf.Clamp01(alignment);                    // 0 when perpendicular/behind, 1 when aligned
        return Mathf.Lerp(torque * 0.4f, torque, factor);            // keep some minimum drive even mid-turn
    }

    void FloorIt()
    {
        if (GetSpeed() > maxSpeed)
        {
            torque = 0;
            transmission.ReleaseGas();
        }
        else
        {
            torque = originalTorque;
            transmission.Accelerate(GetAppropriateTorque());
        }
    }

    void Steer()
    {
        float steerLimit = Mathf.Lerp(maxSteerAngleLo, maxSteerAngleHi, GetSpeed() / maxSpeed);

        float angleToTarget = Vector3.SignedAngle(transform.forward, GetSteerTarget() - transform.position, transform.up);

        float targetSteer = Mathf.Clamp(angleToTarget, -steerLimit, steerLimit);

        // ease the current wheel angle toward the target instead of snapping
        float newSteer = Mathf.MoveTowards(wheels[0].steerAngle, targetSteer, steerSpeed * Time.fixedDeltaTime);

        wheels[0].steerAngle = newSteer;
        wheels[1].steerAngle = newSteer;
    }

    Vector3 GetSteerTarget()
    {
        Vector3 current = waypoints[currentWaypoint].location.position;
        if (currentWaypoint < waypoints.Count - 1)
        {
            Vector3 next = waypoints[currentWaypoint + 1].location.position;
            float dist = Vector3.Distance(transform.position, current);
            float blend = Mathf.Clamp01(1f - (dist / blendingThreshhold)); // ramps up as you approach
            return Vector3.Lerp(current, next, blend);
        }
        return current;
    }

    void HandleNavigation()
    {
        print(waypoints[currentWaypoint].Distance(transform.position));
        if (waypoints[currentWaypoint].Distance(transform.position) <= wpThreshhold)
        {
            if (waypoints.Count-1 > currentWaypoint)
            {
                currentWaypoint++;
            }
        }

    }
}
