using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class GunCore : MonoBehaviour
{
    [Header("Basic Gun Values")]
    [SerializeField] float gunDamage = 9f;
    public float maxRange = 400f;
    [SerializeField] float fireRate = 0.09f;

    [Header("Visuals")]
    public Transform gunBarrel;
    public Transform gunBody;
    [SerializeField] float TurretRotationTime = 1f;

    [Header("Misc.")]
    public LayerMask IgnoreLayer;
    IGunUser gunUser;

    float fireRateTimer = 0f;
    bool canFire = false;

    public event Action OnFired;
    public event Action<GameObject> Hit; //PARAMTER: WHO WAS HIT

    private void Start()
    {
        gunUser = GetComponent<IGunUser>();
        if (gunUser == null) Debug.LogError($"No IGunUser found on {gameObject.name}");

        fireRateTimer = 0f;
    }

    private void Update()
    {
        fireRateTimer += Mathf.Clamp(Time.deltaTime, 0f, 0.08f);
        if (fireRateTimer >= fireRate) canFire = true;
    }

    // Update is called once per frame
    void LateUpdate()
    {
        TurretFollowAim();
        CheckForShots();
    }

    void CheckForShots()
    {
        bool isShooting = gunUser.WantsToFire;
        if(isShooting && canFire)
        {
            Shoot();
        }
    }

    void TurretFollowAim()
    {
        Vector3 origin = gunBarrel.position;
        Vector3 dir = gunUser.AimDirection;

        Vector3 pointToLookAt = Physics.Raycast(origin, dir, out RaycastHit hit, maxRange, ~IgnoreLayer) ? hit.point : origin + dir * maxRange;

        Vector3 toTarget = pointToLookAt - gunBody.position;
        if (toTarget.sqrMagnitude < 0.0001f) return; // avoid LookRotation zero-vector warning

        gunBody.rotation = Quaternion.Slerp(gunBody.rotation, Quaternion.LookRotation(toTarget), Time.deltaTime * TurretRotationTime);
    }

    public void Shoot()
    {
        OnFired?.Invoke();
        // Set firerate vars as needed
        fireRateTimer = 0f;
        canFire = false;

        RaycastHit hit;
        if(Physics.Raycast(gunBarrel.position, gunUser.AimDirection, out hit, maxRange, ~IgnoreLayer))
        {
            Hit?.Invoke(hit.transform.gameObject);
        }
    }
}
