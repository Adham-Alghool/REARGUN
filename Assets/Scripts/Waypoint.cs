using System;
using UnityEngine;

public class Waypoint : MonoBehaviour
{
    [HideInInspector] public Transform location;
    [SerializeField] float distanceThreshhold = 10f;
    [SerializeField] float speedLimit = 80f;

    bool passed;

    public static event Action WaypointPassed;

    private GameState gameState;
    private float distance = 0f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        location = transform.GetComponent<Transform>();
        passed = false;
        gameState = GameState.Instance;

        distance = Vector3.Distance(transform.position, gameState.Player.position);
    }

    private void Update()
    {
        distance = Vector3.Distance(transform.position, gameState.Player.position);
        if(distance <= distanceThreshhold)
        {
            passed = true;
            WaypointPassed?.Invoke();
            gameObject.SetActive(false);
        }
    }
}
