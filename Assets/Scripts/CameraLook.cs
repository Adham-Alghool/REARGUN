using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class CameraLook : MonoBehaviour
{
    [HideInInspector] public bool NeedsOverride = false; // An exposed bool if another script needs to manipulate the camera.

    [Header("Gimbal Limits")]
    [SerializeField] float xClampMax = 80f;
    [SerializeField] float xClampMin = -80f;
    [SerializeField] float yClampMax = 80f;
    [SerializeField] float yClampMin = -80f;

    [Header("Sensitivity")]
    [SerializeField] float xSensitivity = 0.3f;
    [SerializeField] float ySensitivity = 0.3f;

    [Header("Zoom Settings")]
    [SerializeField] float maxFOV = 75f;
    [SerializeField] float minFOV = 30f;
    [SerializeField] float zoomSpeed = 3f;


    private Camera cam;
    private Vector2 lookOffset; // How much we're changing our "look" input every frame.

    InputActionAsset actions = InputSystem.actions;

    //Input actions
    InputAction look;
    InputAction zoom;

    //Values we need to keep things in check.
    float pitch = 0f;
    float yaw = 0f;

    bool isZooming = false;
    private Coroutine zoomCoroutine; // Know which coroutine in memory we are playing at all times.

    void Start()
    {
        foreach (var map in actions.actionMaps) map.Disable();

        actions.FindActionMap("Player").Enable();

        look = actions.FindActionMap("Player").FindAction("Look");
        zoom = actions.FindActionMap("Player").FindAction("Zoom");


        cam = GetComponentInChildren<Camera>();

        Cursor.lockState = CursorLockMode.Locked;
    }

    // Update is called once per frame
    void Update()
    {
        if (!NeedsOverride)
        {
            ReadLookInfo();
            ReadZoomInfo();
        }
    }

    void ReadZoomInfo()
    {
        float zoomValue = zoom.ReadValue<float>();
        bool heldDown = (zoomValue == 1) ? true : false;
        if (heldDown) ZoomIn();
        else ZoomOut();
    }
    void ReadLookInfo()
    {
        lookOffset = look.ReadValue<Vector2>();
        float deltaX = lookOffset.x;
        float deltaY = lookOffset.y;

        yaw += deltaX * xSensitivity; // horizontal input -> yaw
        pitch += deltaY * ySensitivity; // vertical input -> pitch

        yaw = Mathf.Clamp(yaw, xClampMin, xClampMax);
        pitch = Mathf.Clamp(pitch, yClampMin, yClampMax);

        transform.localRotation = Quaternion.Euler(-pitch, yaw, 0);
    }
    void ZoomIn()
    {
        if (cam.fieldOfView != minFOV)
        {
            if (isZooming)
            {
                if (zoomCoroutine != null) StopCoroutine(zoomCoroutine);
            }
            zoomCoroutine = StartCoroutine(zoomToggle(true));
        }
    }

    void ZoomOut()
    {
        if (cam.fieldOfView != maxFOV)
        {
            if (isZooming)
            {
                if (zoomCoroutine != null) StopCoroutine(zoomCoroutine);
            }
            zoomCoroutine = StartCoroutine(zoomToggle(false));
        }
    }   

    IEnumerator zoomToggle(bool In)
    {
        isZooming = false;
        if (In)
        {
            while (cam.fieldOfView > minFOV)
            {
                cam.fieldOfView -= Time.deltaTime * zoomSpeed;
                yield return new WaitForEndOfFrame();
            }
            cam.fieldOfView = minFOV;
        }
        else
        {
            while (cam.fieldOfView < maxFOV)
            {
                cam.fieldOfView += Time.deltaTime * zoomSpeed;
                yield return new WaitForEndOfFrame();
            }
            cam.fieldOfView = maxFOV;
        }
        isZooming = false;
    }
}
