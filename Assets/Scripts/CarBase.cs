using System.Collections.Generic;
using UnityEngine;

public class CarBase : MonoBehaviour
{
    [Header("Waypoint Related Info")]
    [SerializeField] List<Waypoint> waypoints = new List<Waypoint>();
    [Tooltip("How close the car can get before switching waypoints")]
    [SerializeField] float wpThreshhold = 10f;

    [Header("Wheel Refs (Respective)")]
    [Tooltip("Set rear wheels to be of index 3, 4 for correct steering/motor behavior")]
    [SerializeField] List<GameObject> wheelModels;
    public List<WheelCollider> wheels;
    [SerializeField] Vector3[] meshRotationOffset;

    [Header("Engine Performance")]
    [SerializeField] float maxSpeed = 300f;
    public float torque = 20f;
    private float originalTorque;

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
        // 1. Move as fast as your speed limit allows you.
        // 2. Steer towards the target
        // 3. Slow down if off-target and time to correct is low.
        // 4. That's it (so far).

        MatchWheelMeshes();
    }

    private void FixedUpdate()
    {
        FloorIt();
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

    void FloorIt()
    {
        float speed = Vector3.Dot(rb.linearVelocity, transform.forward); //Get our car's speed on the plane it's actually driving on.
        if (speed > maxSpeed) torque = 0;
        else 
        {
            torque = originalTorque;
            transmission.Accelerate();
        }
    }

}
