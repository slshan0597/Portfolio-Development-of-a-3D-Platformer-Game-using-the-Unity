using System;
using System.Collections;
using UnityEngine;

using State        = ThrowableBase.State;
using Resources    = ThrowableBase.Resources;
using ThrowSetting = ThrowableBase.ThrowSetting;
using DamageType   = IDamageable.Type;


public interface IThrowableBase
{
    #region Property

    // Component
    GameObject gameObject { get; }
    Transform  transform  { get; }
    Rigidbody  rigidbody  { get; }
    Collider   collider   { get; }
    Resources  resources  { get; }

    // State
    State state { get; }

    // Setting
    float        carryDuration { get; }
    ThrowSetting throwSetting  { get; }

    #endregion


    #region Method

    void      Idle();
    Coroutine Carry(IPlayerController player);
    void      Throw(IPlayerController player);
    Coroutine Destroy();

    #endregion
}


[RequireComponent(typeof(Rigidbody))]
public class ThrowableBase : MonoBehaviour, IThrowableBase, IGravityable, IInteractable, IDamageable
{
    #region Definition

    public enum State { None, Idle, Carry, Throw, Destroy }


    public class Resources
    {
        #region Field

        public IModelController model { get; }

        #endregion


        #region Constructor

        public Resources(Transform transform)
        {
            model = transform.GetComponentInChildren<IModelController>(true);
        }

        #endregion
    }


    [Serializable] public struct ThrowSetting
    {
        #region Field

        [SerializeField, Range(0f, 90f)] private float _angle;
        [SerializeField]                 private float _force;

        public float angle { get { return _angle; } }
        public float force { get { return _force; } }

        #endregion


        #region Constructor

        public ThrowSetting(float angle, float force)
        {
            _angle = angle;
            _force = force;
        }

        public ThrowSetting(ThrowSetting other)
        {
            _angle = other.angle;
            _force = other.force;
        }

        #endregion
    }

    #endregion


    #region Field

    public new Rigidbody  rigidbody { get; protected set; }
    public new Collider   collider  { get; protected set; }
    public Resources      resources { get; protected set; }
    public SphereCollider trigger   { get; protected set; }

    public Vector3 gravity { get; set; } = Vector3.zero;
    public State   state   { get; protected set; }

    [Header("Gravity Setting")]
    [SerializeField] protected bool  _useGravity;
    [SerializeField] protected float _radius;
    [SerializeField] protected bool  _fixRotation;
    [SerializeField] protected float _rotationSpeed;
    [Header("Damage Setting")]
    [SerializeField] protected DamageType _mask;
    [Header("Throwable Setting")]
    [SerializeField] protected float        _carryDuration;
    [SerializeField] protected ThrowSetting _throwSetting;

    public bool         useGravity    { get { return _useGravity; } }
    public float        radius        { get { return _radius; } }
    public bool         fixRotation   { get { return _fixRotation; } }
    public float        rotationSpeed { get { return _rotationSpeed; } }
    public DamageType   mask          { get { return _mask; } }
    public float        carryDuration { get { return _carryDuration; } }
    public ThrowSetting throwSetting  { get { return _throwSetting; } }

    protected Coroutine carryAction;

    #endregion


    #region Method

    #region Event

    protected virtual void Awake() { SetField(); }

    protected virtual void Reset()
    {
        if (!TryGetComponent(out Rigidbody rigidbody)) return;

        ResetField(rigidbody);
    }

    protected virtual void Start() { Idle(); }

    protected virtual void OnCollisionEnter(Collision collision)
    {
        if ((state != State.Throw) || collision.transform.TryGetComponent(out IPlayerController player)) return;

        Idle();
    }

    #endregion


    #region Initialization

    protected virtual void SetField()
    {
        rigidbody = GetComponent<Rigidbody>();
        collider  = Array.Find(GetComponents<Collider>(), collider => !collider.isTrigger);
        resources = new Resources(transform.Find("Resources"));
        trigger   = Array.Find(GetComponents<SphereCollider>(), collider => collider.isTrigger);
    }

    protected virtual void ResetField(Rigidbody rigidbody)
    {
        rigidbody.useGravity = false;
        _useGravity          = true;
        _radius              = TryGetComponent(out SphereCollider collider) ? collider.radius : 0f;
        _fixRotation         = false;
        _rotationSpeed       = 10f;
        _carryDuration       = 0.5f;
        _throwSetting        = new ThrowSetting(45f, 250f);
    }

    #endregion


    #region Idle

    public virtual void Idle()
    {
        state                 = State.Idle;
        rigidbody.isKinematic = false;
        collider.enabled      = true;
        trigger.enabled       = true;
    }

    #endregion


    #region Carry

    public virtual Coroutine Interact(IPlayerController player) { return Carry(player); }

    public virtual void StopInteract(IPlayerController player) 
    { 
        if (carryAction != null) StopCoroutine(carryAction);

        carryAction = null;
    }

    public virtual Coroutine Carry(IPlayerController player)
    {
        state                 = State.Carry;
        rigidbody.isKinematic = true;
        collider.enabled      = false;
        trigger.enabled       = false;

        //player.resources.Play(PlayerInteractState.CarryUp);
        player.Carry(this);

        return carryAction = StartCoroutine(_Carry(player)); 
    }

    protected virtual IEnumerator _Carry(IPlayerController player) 
    { 
        yield return new WaitForSeconds(carryDuration);

        carryAction = null;
    }

    #endregion


    #region Throw

    public virtual void Throw(IPlayerController player)
    {
        state                 = State.Throw;
        rigidbody.isKinematic = false;
        collider.enabled      = true;
        trigger.enabled       = false;
        transform.position    = player.transform.TransformPoint(Vector3.up * (player.collider.height - player.collider.radius + radius));
        transform.rotation    = player.direction.transform.rotation;

        float   angle          = throwSetting.angle * Mathf.Deg2Rad;
        float   force          = Mathf.Sqrt(throwSetting.force);
        Vector3 localDirection = (Vector3.forward * Mathf.Cos(angle)) + (Vector3.up * Mathf.Sin(angle));
        Vector3 direction      = transform.TransformDirection(localDirection);

        rigidbody.AddForce(force * direction, ForceMode.VelocityChange);
    }

    #endregion


    #region Destroy

    public virtual bool TryDamage(Transform attacker, DamageType type)
    {
        if (!mask.HasFlag(type)) return false;

        Destroy();

        return true;
    }

    public virtual Coroutine Destroy()
    {
        state                 = State.Destroy;
        rigidbody.isKinematic = true;
        collider.enabled      = false;
        trigger.enabled       = false;

        if (gravity.magnitude > 0)
        {
            Quaternion amount   = Quaternion.FromToRotation(transform.up, -gravity.normalized);
            Quaternion rotation = amount * transform.rotation;

            transform.rotation = rotation;
        }

        return StartCoroutine(_Destroy(Time.timeScale <= 0f));
    }

    protected virtual IEnumerator _Destroy(bool useUnscaledTime) { yield break; }

    #endregion

    #endregion
}
