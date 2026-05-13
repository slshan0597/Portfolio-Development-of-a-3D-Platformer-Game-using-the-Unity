// //////////////////////////////////////////////////////////////////////////////
// * 목차
//    1. 인터페이스 ... Line 26
//    2. 클래스 ....... Line 54
//        1) 정의 ..... Line 64
//        2) 필드 ..... Line 90
//        3) 메서드 ... Line 126
//            1- 이벤트 함수 ... Line 130
//            2- 초기화 ........ Line 153
//            3- 액션 .......... Line 176
//                1_ 대기(Idle) ...... Line 179
//                2_ 나르기(Carry) ... Line 190
//                3_ 던지기(Throw) ... Line 225
//                4_ 파괴(Destroy) ... Line 247
// //////////////////////////////////////////////////////////////////////////////
using System;
using System.Collections;
using UnityEngine;

using State        = ThrowableBase.State;
using Resources    = ThrowableBase.Resources;
using ThrowSetting = ThrowableBase.ThrowSetting;
using DamageType   = IDamageable.Type;

// //////////////////////////////////////////////////////////////////////////////
// 1. 인터페이스
// //////////////////////////////////////////////////////////////////////////////
public interface IThrowableBase
{
    // 프로퍼티
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

    // 메서드
    // Action
    void      Idle();
    Coroutine Carry(IPlayerController player);
    void      Throw(IPlayerController player);
    Coroutine Destroy();
}

// //////////////////////////////////////////////////////////////////////////////
// 2. 클래스
//    - 플레이어가 들어올리고 던질 수 있는 오브젝트
//    - Gravityable 속성  -> 커스텀 중력의 영향을 받음
//    - Interactable 속성 -> 플레이어에 의해 상호작용될 수 있음
//    - Danageable 속성   -> 공격에 대한 피해를 입을 수 있음
// //////////////////////////////////////////////////////////////////////////////
[RequireComponent(typeof(Rigidbody))]
public class ThrowableBase : MonoBehaviour, IThrowableBase, IGravityable, IInteractable, IDamageable
{
    // ==============================================================================
    // 1) 정의
    // ==============================================================================
    public enum State { None, Idle, Carry, Throw, Destroy }

    [Serializable] public struct ThrowSetting
    {
        [SerializeField, Range(0f, 90f)] private float _angle;
        [SerializeField]                 private float _force;

        public float angle { get { return _angle; } }
        public float force { get { return _force; } }

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
    }

    // ==============================================================================
    // 2) 필드
    // ==============================================================================
    // Component & Reference
    public new Rigidbody  rigidbody { get; protected set; }
    public new Collider   collider  { get; protected set; }
    public Resources      resources { get; protected set; }
    public SphereCollider trigger   { get; protected set; }

    // State
    public Vector3 gravity { get; set; } = Vector3.zero;
    public State   state   { get; protected set; }

    // Setting
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

    // etc.
    protected Coroutine carryAction;

    // ==============================================================================
    // 3) 메서드
    //    - 클래스 확장을 위한 기반 기능만을 구현
    // ==============================================================================
    // ------------------------------------------------------------------------------
    // 3-1) 메서드 -> 이벤트 함수
    //    - 오브젝트 초기화
    //    - 오브젝트가 지면에 닿으면 기본 상태(Idle)로 초기화
    // ------------------------------------------------------------------------------
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

    // ------------------------------------------------------------------------------
    // 3-2) 메서드 -> 초기화
    //    - 필드(컴포넌트 등) 초기화
    // ------------------------------------------------------------------------------
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

    // ------------------------------------------------------------------------------
    // 3-3) 메서드 -> 액션
    // ------------------------------------------------------------------------------
    // ******************************************************************************
    // 3-3-1) 메서드 -> 액션 -> 대기(Idle)
    // ******************************************************************************
    public virtual void Idle()
    {
        state                 = State.Idle;
        rigidbody.isKinematic = false;
        collider.enabled      = true;
        trigger.enabled       = true;
    }

    // ******************************************************************************
    // 3-3-2) 메서드 -> 액션 -> 나르기(Carry)
    //    - 플레이어의 상호작용에 의해 호출됨
    //    - 플레이어가 물건을 들어올림
    //    - 플레이어가 물건을 들어올린 상태를 유지(플레이어의 Carry 함수 호출)
    // ******************************************************************************
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

    // ******************************************************************************
    // 3-3-3) 메서드 -> 액션 -> 던지기(Throw)
    //    - 플레이어의 공격에 의해 호출됨
    //    - 지정된 각도와 힘으로 오브젝트 발사
    // ******************************************************************************
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

    // ******************************************************************************
    // 3-3-4) 메서드 -> 액션 -> 파괴(Destroy)
    //    - 데미지를 입으면 오브젝트 파괴
    // ******************************************************************************
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
}
