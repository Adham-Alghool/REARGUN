using System;
using UnityEngine;

public class Waypoint : MonoBehaviour
{
    [HideInInspector] public Transform location;
    public float distanceThreshhold { get; private set; } = 10f;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        location = transform.GetComponent<Transform>();
    }

    public float Distance(Vector3 pos)
    {
        return Vector3.Distance(transform.position, pos);
    }
}
