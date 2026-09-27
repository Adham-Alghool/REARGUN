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
    [Tooltip("Strengthens calculated steering curve")]
    [SerializeField] float steeringStrength = 2f;
    [SerializeField] float maxTraction = 3f;

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
            transmission.Accelerate(torque);
        }
    }

    Waypoint getWaypoint()
    {
        return waypoints[currentWaypoint];
    }

    void HandleNavigation()
    {
        float distance = getWaypoint().Distance(transform.position);

        if (distance <= wpThreshhold)
        {
            if (waypoints.Count-1 > currentWaypoint)
            {
                currentWaypoint++;
            }
        }

    }

    void Steer()
    {
        wheels[0].steerAngle = CalculateRequiredSteeringAngle();
        wheels[1].steerAngle = CalculateRequiredSteeringAngle();

        WheelFrictionCurve newFriction = wheels[0].sidewaysFriction;

        newFriction.stiffness = CalculateRequiredTraction();

        foreach(var wheel in wheels)
        {
            wheel.sidewaysFriction = newFriction;
        }
    }

    float CalculateRequiredSteeringAngle()
    {
        float xDisplacement = transform.InverseTransformPoint(getWaypoint().location.position).x;
        float yDisplacement = transform.InverseTransformPoint(getWaypoint().location.position).z;

        float wheelbase = wheels[0].transform.localPosition.z - wheels[2].transform.localPosition.z;

        float curvature = (2 * xDisplacement) / (Mathf.Pow(xDisplacement, 2) + Mathf.Pow(yDisplacement, 2)); // Found this formula online.
        float angle = Mathf.Rad2Deg * Mathf.Atan(wheelbase * curvature);
        print($"ANGLE: {angle}");
        return angle;
    }

    float CalculateRequiredTraction()
    {
        Vector3 directionToWaypoint = getWaypoint().location.position - transform.position;

        float headingError = Vector3.Angle(transform.forward, directionToWaypoint);

        float heading = 1f - headingError / 180f;

        return heading * maxTraction;
    }
}
