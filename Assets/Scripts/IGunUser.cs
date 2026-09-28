using UnityEngine;

public interface IGunUser
{
    bool WantsToFire { get; }
    Vector3 AimDirection { get; }
    bool WantsToReload { get; }
}
