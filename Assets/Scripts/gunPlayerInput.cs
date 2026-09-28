using UnityEngine;
using UnityEngine.InputSystem;

public class gunPlayerInput : MonoBehaviour, IGunUser
{
    [HideInInspector] public bool WantsToFire { get; private set;  } = false;
    [HideInInspector] public Vector3 AimDirection { get; private set; }
    [HideInInspector] public bool WantsToReload { get; private set; }

    GunCore gun;

    [SerializeField] private Camera cam;
    private LayerMask ignoreLayer;

    [SerializeField] Transform crosshair;
    [SerializeField] float cursorMoveSpeed = 10f;

    private float maxRange;

    InputActionAsset inputActions = InputSystem.actions;
    InputAction shoot;

    void Start()
    {
        gun = GetComponent<GunCore>();
        ignoreLayer = gun.IgnoreLayer;
        maxRange = gun.maxRange;
        shoot = inputActions.FindActionMap("Player").FindAction("Shoot");

        shoot.Enable();
        shoot.ReadValue<float>(); //This forces a flush on the 1st frame. To prevent immediate shooting input.
    }

    // Update is called once per frame
    void Update()
    {
        WantsToFire = GetShootInput();
        AimDirection = AimDir();

        AdjustCursorPos();
    }

    Vector3 AimDir()
    {
        return cam.transform.forward;
    }

    Vector3 AimPoint()
    {
        RaycastHit hit;
        if (Physics.Raycast(gun.gunBarrel.position, cam.transform.forward, out hit, maxRange, ~ignoreLayer)) return hit.point;
        else return gun.gunBarrel.position + (cam.transform.forward * maxRange);
    }

    bool GetShootInput()
    {
        return shoot.IsPressed();
    }

    void AdjustCursorPos()
    {
        RaycastHit hit;
        if (Physics.Raycast(gun.gunBarrel.position, gun.gunBarrel.forward, out hit, maxRange, ~ignoreLayer))
        {
            Vector3 hitPos = hit.point;
            crosshair.position = Vector3.Lerp(crosshair.position, cam.WorldToScreenPoint(hitPos), Time.deltaTime * cursorMoveSpeed);
        }
        else crosshair.position = cam.WorldToScreenPoint(AimPoint());
    }
}
