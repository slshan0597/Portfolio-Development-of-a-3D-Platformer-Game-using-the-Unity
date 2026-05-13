using System;
using UnityEngine;


public interface IGravityable
{
    #region Property

    // Component
    Transform transform { get; }
    Rigidbody rigidbody { get; }

    // Setting
    public bool  useGravity    { get; }
    public float radius        { get; }
    public bool  fixRotation   { get; }
    public float rotationSpeed { get; }

    // State
    public Vector3 gravity { get; set; }

    #endregion


    #region Method

    void TryApplyGravity(IPlanetController planet)
    {
        if (rigidbody.isKinematic || !useGravity) return;

        gravity = planet.GetGravity(this);

        if (GetFallSpeed(gravity) < gravity.magnitude) rigidbody.AddForce(gravity, ForceMode.Acceleration);
        if (fixRotation)                               FixRotation(gravity);
    }

    float GetFallSpeed(Vector3 gravity)
    {
        Vector3 velocity = rigidbody.velocity;

        if (Vector3.Angle(gravity, velocity) >= 90f) return 0f;

        return Vector3.Project(velocity, gravity).magnitude;
    }

    void FixRotation(Vector3 gravity)
    {
        Quaternion amount   = Quaternion.FromToRotation(transform.up, -gravity.normalized);
        Quaternion rotation = amount * transform.rotation;
        //Quaternion rotation = target.transform.rotation * amount;

        transform.rotation = Quaternion.Slerp(transform.rotation, rotation, rotationSpeed * Time.fixedDeltaTime);
        transform.rotation = (Quaternion.Angle(transform.rotation, rotation) < 0.1f) ? rotation : transform.rotation;
    }

    #endregion
}


public interface IInteractable
{
    #region Property

    // Component
    GameObject     gameObject { get; }
    Transform      transform  { get; }
    SphereCollider trigger    { get; }

    #endregion


    #region Method

    Coroutine Interact(IPlayerController player);
    void      StopInteract(IPlayerController player);

    #endregion
}


public interface IDamageable
{
    #region Definition

    [Flags] public enum Type { None = 0, Normal = 1, PressDown = 2, Explode = 4, Foot = 8, Head = 16 }

    #endregion


    #region Field

    // Component
    GameObject gameObject { get; }
    Transform  transform  { get; }

    // Setting
    Type mask { get; }

    #endregion


    #region Method

    bool TryDamage(Transform attacker, Type type);

    #endregion
}
