/////////////////////////////////////////////////////////////////////////////////
// * 목차
//    1. 인터페이스 정의
//    2. 클래스 정의
//        1) 사전 정의
//            1_ 리소스 참조
//            2_ 지면(Ground) 상태
//            3_ 캐릭터 상태
//            4_ 캐릭터 설정
//        2) 필드
//        3) 메서드
//            1_ 이벤트(Unity 호출 함수)
//            2_ 초기화
//            3_ 셋(Set)
//            4_ 액션
//                - 공통(정지, 이동 등)
//                - 대기(Idle)
//                - 데미지(Damage)
/////////////////////////////////////////////////////////////////////////////////
using System;
using System.Collections;
using UnityEngine;

using Game;    // Game Director

using Resources        = CharacterBase.Resources;
using GroundState      = CharacterBase.GroundState;
using State            = CharacterBase.State;
using MainState        = CharacterBase.State.Main;
using SubState         = CharacterBase.State.Sub;
using CharacterSetting = CharacterBase.CharacterSetting;
using MoveSetting      = CharacterBase.CharacterSetting.Move;
using DamageSetting    = CharacterBase.CharacterSetting.Damage;
using DamageType       = IDamageable.Type;

/////////////////////////////////////////////////////////////////////////////////
// 1. 인터페이스 정의
/////////////////////////////////////////////////////////////////////////////////
public interface ICharacterBase
{
    // ==============================================================================
    // 1) 프로퍼티
    // ==============================================================================
    // Component
    GameObject              gameObject { get; }
    Transform               transform  { get; }
    Rigidbody               rigidbody  { get; }
    CapsuleCollider         collider   { get; }
    ICharacterDirectionBase direction  { get; }
    Resources               resources  { get; }

    // Reference
    IPlanetController planet { get; set; }

    // State
    GroundState groundState { get; }
    State       state       { get; }

    // Setting
    float            maxSlopeAngle    { get; }
    float            rotationSpeed    { get; }
    CharacterSetting characterSetting { get; }

    // ==============================================================================
    // 2) 메서드
    // ==============================================================================
    // Set
    void      Initialize();
    Coroutine Set(ICharacterTargetController target, bool resetDirection = false, bool setIdle = false, float duration = 0f);
    void      SetHeight(float rate = -1f);
    void      SetFriction(bool useFriction);
    Coroutine LookAtUnscaledTime(Transform target, float duration = 0f);

    // Action
    void      StopAction();
    Coroutine Idle(bool playAnimation = false);
    Coroutine Damage(Transform attacker, DamageType type);
}

/////////////////////////////////////////////////////////////////////////////////
// 2. 클래스 정의
/////////////////////////////////////////////////////////////////////////////////
[RequireComponent(typeof(Rigidbody))]
public class CharacterBase : MonoBehaviour, ICharacterBase, IGravityable, IDamageable
{
    // ==============================================================================
    // 1) 사전 정의
    // ==============================================================================
    // ------------------------------------------------------------------------------
    // 1_ 리소스 참조
    //    - 캐릭터 모델링의 회전
    //    - 캐릭터의 보이스, 이펙트 호출
    // ------------------------------------------------------------------------------
    public class Resources
    {
        public Transform                   transform { get; }
        public ICharacterModelBase         model     { get; }
        public ICharacterVoiceBase         voice     { get; }
        public CharacterEffects<MainState> effects   { get; }

        public Resources(Transform transform)
        {
            this.transform = transform;
            model          = transform.GetComponentInChildren<ICharacterModelBase>(true);
            voice          = transform.GetComponentInChildren<ICharacterVoiceBase>(true);
            effects        = new CharacterEffects<MainState>(transform.Find("Effects"));
        }

        public void Follow(ICharacterDirectionBase direction, float speed)
        {
            Quaternion rotation = direction.transform.localRotation;

            transform.localRotation
                = Quaternion.Slerp(transform.localRotation, rotation, speed * Time.fixedDeltaTime);
        }

        public float Play(MainState type, SubState subType = SubState.None)
        {
            model.Play(type, subType);

            return Mathf.Max(voice.Play(type, subType), (effects != null) ? effects.Play(type, subType) : default);
        }

        public float Play(DamageType type, SubState subType = SubState.None)
        {
            model.Play(type, subType);

            return Mathf.Max(voice.Play(type, subType), (effects != null) ? effects.Play(type, subType) : default);
        }

        public float Play(string type)
        {
            model.Play(type);

            return Mathf.Max(voice.Play(type), (effects != null) ? effects.Play(type) : default);
        }
    }

    // ------------------------------------------------------------------------------
    // 2_ 지면(Ground) 상태
    //    - 지면에 대한 충돌 또는 접지, 경사각 정보 갱신
    // ------------------------------------------------------------------------------
    public class GroundState
    {
        public RaycastHit groundHit;
        public bool       isGrounded;
        public float      slopeAngle;

        public GroundState()
        {
            groundHit  = new RaycastHit();
            isGrounded = false;
            slopeAngle = 0f;
        }

        public void Update(ICharacterBase character)
        {
            Transform       transform = character.transform;
            CapsuleCollider collider  = character.collider;

            float   height      = collider.height;
            float   radius      = collider.radius;
            float   maxDistance = (height - radius) * 1.1f;
            Vector3 origin      = transform.TransformPoint(Vector3.up * (height - radius));
            Vector3 upDirection = transform.up;
            Ray     ray         = new Ray(origin, -upDirection);
            int     layerMask   = (1 << LayerMask.NameToLayer("Character")) | (1 << LayerMask.NameToLayer("Coin"));
                    layerMask   = ~layerMask;
            var     qtr         = QueryTriggerInteraction.Ignore;

            isGrounded = Physics.SphereCast(ray, radius, out groundHit, maxDistance, layerMask, qtr);
            slopeAngle = isGrounded ? Vector3.Angle(upDirection, groundHit.normal) : 0f;

            if (slopeAngle > character.maxSlopeAngle)
            {
                layerMask  = 1 << LayerMask.NameToLayer("Planet Ground");
                isGrounded = Physics.SphereCast(ray, radius, out groundHit, maxDistance, layerMask, qtr);
                slopeAngle = isGrounded ? Vector3.Angle(upDirection, groundHit.normal) : 0f;

                if (slopeAngle > character.maxSlopeAngle) isGrounded = false;
            }
        }
    }

    // ------------------------------------------------------------------------------
    // 3_ 캐릭터 상태
    //    - 캐릭터의 메인 상태
    //    - 메인 상태의 진행도
    // ------------------------------------------------------------------------------
    public class State
    {
        public enum Main { None = 0, Idle = 1, Damage = 2 }
        public enum Sub  { None = 0, Start = 1, Loop = 2, End = 4 }

        public Main       main;
        public Sub        sub;
        public DamageType damage;
    }

    // ------------------------------------------------------------------------------
    // 4_ 캐릭터 설정
    //    - 각 상태에 대한 설정 프로퍼티
    // ------------------------------------------------------------------------------
    [Serializable] public class CharacterSetting
    {
        [Serializable] public struct Move
        {
            [SerializeField] private float _speed;
            [SerializeField] private float _acceleration;

            public float speed        { get { return _speed; } }
            public float acceleration { get { return _acceleration; } }

            private static readonly Move _one = new Move(1f, 1f);

            public Move(float speed, float acceleration)
            {
                _speed        = speed;
                _acceleration = acceleration;
            }
            
            public static Move one { get { return _one; } }

            public static Move MultiplySpeed(Move origin, float multiplier) 
                => new Move(origin.speed * multiplier, origin.acceleration);
        }

        [Serializable] public class Damage
        {
            [SerializeField]                 protected float _duration;
            [SerializeField]                 protected float _force;
            [SerializeField, Range(0f, 90f)] protected float _angle;

            public float duration { get { return _duration; } }
            public float force    { get { return _force; } }
            public float angle    { get { return _angle; } }

            public Damage(float duration, float force, float angle)
            {
                _duration = duration;
                _force    = force;
                _angle    = angle;
            }

            public Vector3 GetForce(Transform transform)
            {
                float   angle     = this.angle * Mathf.Deg2Rad;
                Vector3 direction = (Vector3.up * Mathf.Sin(angle)) - (Vector3.forward * Mathf.Cos(angle));
                Vector3 force     = Mathf.Sqrt(this.force) * direction;

                return transform.TransformDirection(force);
            }
        }

        [SerializeField] protected Damage _damage;

        public Damage damage { get { return _damage; } }

        public CharacterSetting(Damage damage) { _damage = damage; }
    }

    // ==============================================================================
    // 2) 필드
    // ==============================================================================
    // Component & Reference
    public new Rigidbody           rigidbody { get; protected set; }
    public new CapsuleCollider     collider  { get; protected set; }
    public ICharacterDirectionBase direction { get; protected set; }
    public Resources               resources { get; protected set; }
    public IPlanetController       planet    { get; set; }

    // State
    public Vector3     gravity     { get; set; }           = Vector3.zero;
    public GroundState groundState { get; protected set; } = new GroundState();
    public State       state       { get; protected set; } = new State();

    // Property
    [Header("Gravity Setting")]
    [SerializeField] protected bool  _useGravity;
    [SerializeField] protected float _radius;
    [SerializeField] protected bool  _fixRotation;
    [SerializeField] protected float _rotationSpeed;
    [Header("Damage Setting")]
    [SerializeField] protected DamageType _mask;
    [Header("Character Setting")]
    [SerializeField, Range(0f, 90f)] protected float            _maxSlopeAngle;
    [SerializeField]                 protected CharacterSetting _characterSetting;

    public bool             useGravity       { get { return _useGravity; } }
    public float            radius           { get { return _radius; } }
    public bool             fixRotation      { get { return _fixRotation; } }
    public float            rotationSpeed    { get { return _rotationSpeed; } }
    public DamageType       mask             { get { return _mask; } }
    public float            maxSlopeAngle    { get { return _maxSlopeAngle; } }
    public CharacterSetting characterSetting { get { return _characterSetting; } }

    // Action
    protected Coroutine setAction;
    protected Coroutine lookAtAction;
    protected Coroutine action;

    protected float   defaultColliderHeight;
    protected Vector3 defaultColliderCenter;
    protected float   moveSpeed;

    // ==============================================================================
    // 3) 메서드
    // ==============================================================================
    // ------------------------------------------------------------------------------
    // 1_ 이벤트 (Unity 호출 함수)
    // ------------------------------------------------------------------------------
    protected virtual void Awake() { SetField(); }

    protected virtual void Reset()
    {
        if (!TryGetComponent(out Rigidbody rigidbody)) return;

        ResetField(rigidbody);
    }

    protected virtual void Start()
    {
        Initialize();

        if ((planet == null) || planet.characters.Contains(this)) return;

        planet.characters.Add(this);

        if (planet.enabled) Idle();
    }

    protected virtual void FixedUpdate()
    {
        groundState.Update(this);
        resources.Follow(direction, rotationSpeed);
    }

    protected virtual void OnDestroy()
    {
        if ((planet == null) || !planet.characters.Contains(this)) return;

        planet.characters.Remove(this);
    }

    // ------------------------------------------------------------------------------
    // 2_ 초기화
    // ------------------------------------------------------------------------------
    protected virtual void SetField()
    {
        rigidbody = GetComponent<Rigidbody>();
        collider  = GetComponent<CapsuleCollider>();
        direction = GetComponentInChildren<ICharacterDirectionBase>(true);
        resources = new Resources(transform.Find("Resources"));
        planet    = GetComponentInParent<IPlanetController>(true);

        defaultColliderHeight = collider.height;
        defaultColliderCenter = collider.center;

        PhysicMaterial material = collider.material = new PhysicMaterial("Character");

        material.dynamicFriction = material.staticFriction = material.bounciness = 0f;
        material.frictionCombine = material.bounceCombine  = PhysicMaterialCombine.Minimum;
    }

    protected virtual void ResetField(Rigidbody rigidbody)
    {
        rigidbody.useGravity = false;
        _useGravity          = true;
        _radius              = TryGetComponent(out CapsuleCollider collider) ? collider.radius : 0f;
        _fixRotation         = true;
        _rotationSpeed       = 10f;
        _mask                = DamageType.Normal | DamageType.PressDown | DamageType.Explode | DamageType.Foot;
        _maxSlopeAngle       = 50f;
        _characterSetting    = new CharacterSetting(new DamageSetting(0.75f, 500f, 60f));
    }

    public virtual void Initialize()
    {
        rigidbody.isKinematic = false;
        collider.enabled      = true;
    }

    // ------------------------------------------------------------------------------
    // 3_ 셋(Set)
    //    - 캐릭터의 배치(Transform) 설정
    //    - 캐릭터의 형태(Collider) 설정
    // ------------------------------------------------------------------------------
    public virtual Coroutine Set(ICharacterTargetController target, bool resetDirection = false, 
        bool setIdle = false, float duration = 0f)
    {
        if (setAction != null)
        {
            StopCoroutine(setAction);
            setAction = null;
        }
        if (resetDirection)
        {
            direction.transform.localRotation = Quaternion.identity;
            resources.transform.localRotation = Quaternion.identity;
        }
        if (setIdle) Idle(true);

        return setAction = StartCoroutine(_Set(target, duration, Time.timeScale == 0f));
    }

    protected virtual IEnumerator _Set(ICharacterTargetController target, float duration, bool useUnscaledTime)
    {
        IAnimationCurvePreset curvePreset = GameDirector.instance.curvePreset;

        Vector3    startPosition = transform.position;
        Vector3    endPosition   = target.GetCenter(this);
        Quaternion startRotation = transform.rotation;
        Quaternion endRotation   = target.transform.rotation;
        float      elapsedTime   = 0f;
        var        curveType     = curvePreset.types[0];

        while (elapsedTime < duration)
        {
            float rate = curveType.Evaluate(elapsedTime / duration);

            transform.position = Vector3.Lerp(startPosition, endPosition, rate);
            transform.rotation = Quaternion.Slerp(startRotation, endRotation, rate);

            elapsedTime += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            yield return null;
        }

        transform.position = endPosition;
        transform.rotation = endRotation;
        setAction          = null;
    }

    public virtual void SetHeight(float rate = -1f)
    {
        float radius = collider.radius;

        collider.height = Mathf.Lerp(radius * 2f,    defaultColliderHeight, (rate < 0f) ? 1f : rate);
        collider.center = Vector3.Lerp(Vector3.zero, defaultColliderCenter, (rate < 0f) ? 1f : rate);
    }

    public virtual void SetFriction(bool useFriction)
    {
        PhysicMaterial material = collider.material;

        //material.dynamicFriction = material.staticFriction = useFriction ? 1f : 0f;
        //material.frictionCombine = useFriction ? PhysicMaterialCombine.Maximum : PhysicMaterialCombine.Minimum;

        material.dynamicFriction = material.staticFriction = useFriction ? 1.4f : 0f;
        material.frictionCombine = useFriction ? PhysicMaterialCombine.Average : PhysicMaterialCombine.Minimum;

        if (useFriction) rigidbody.velocity = Vector3.ProjectOnPlane(rigidbody.velocity, transform.up);
    }

    public virtual Coroutine LookAtUnscaledTime(Transform target, float duration = 0f)
    {
        if (lookAtAction != null)
        {
            StopCoroutine(lookAtAction);
            lookAtAction = null;
        }

        direction.LookAt(target);

        return lookAtAction = StartCoroutine(_LookAtUnscaledTime(duration));
    }

    protected virtual IEnumerator _LookAtUnscaledTime(float duration)
    {
        IAnimationCurvePreset curvePreset = GameDirector.instance.curvePreset;
        Transform             resources   = this.resources.transform;
        Transform             direction   = this.direction.transform;

        Quaternion startRotation = resources.localRotation;
        Quaternion endRotation   = direction.localRotation;
        float      elapsedTime   = 0f;
        var        curveType     = curvePreset.types[1];

        while (elapsedTime < duration)
        {
            float rate = curveType.Evaluate(elapsedTime / duration);

            resources.localRotation = Quaternion.Slerp(startRotation, endRotation, rate);

            elapsedTime += Time.unscaledDeltaTime;
            yield return null;
        }

        resources.localRotation = endRotation;
        lookAtAction            = null;
    }

    // ------------------------------------------------------------------------------
    // 4_ 액션 1. 공통
    //    - 이동 및 모든 행동에 대한 정지 기능
    //    - 지면(Ground)과의 충돌에 대한 지연 처리
    // ------------------------------------------------------------------------------
    public virtual void StopAction()
    {
        if (action != null) StopCoroutine(action);

        action = null;

        switch (state.main)
        {
            case MainState.Idle:   StopIdle();   break;
            case MainState.Damage: StopDamage(); break;
        }
    }

    protected virtual void Move(MoveSetting setting, bool isKinematic = false)
    {
        moveSpeed = Mathf.Lerp(moveSpeed, setting.speed, setting.acceleration * Time.fixedDeltaTime);
        moveSpeed = (Mathf.Abs(setting.speed - moveSpeed) < 0.1f) ? setting.speed : moveSpeed;

        Vector3 forward = direction.GetProjection(groundState);

        if (isKinematic)
        {
            Vector3 amount = forward * moveSpeed * Time.fixedDeltaTime;

            rigidbody.MovePosition(transform.position + amount);
        }
        else
        {
            Vector3 velocity = Vector3.ProjectOnPlane(rigidbody.velocity, transform.up);

            rigidbody.AddForce(forward * moveSpeed - velocity, ForceMode.VelocityChange);
        }
    }

    protected virtual void StopMove()
    {
        moveSpeed          = 0f;
        rigidbody.velocity = Vector3.zero;
    }

    protected virtual IEnumerator WaitUntilNotGrounded(int maxFrameCount = 3)
    {
        for (int count = 0; count < maxFrameCount; count++)
        {
            if (!groundState.isGrounded) break;

            yield return new WaitForFixedUpdate();
        }

        if (groundState.isGrounded) yield return new WaitForFixedUpdate();
    }

    protected virtual IEnumerator WaitUntilGrounded(bool useDelay = false)
    {
        if (useDelay) yield return WaitUntilNotGrounded();

        while (!groundState.isGrounded) yield return new WaitForFixedUpdate();
    }

    // ------------------------------------------------------------------------------
    // 4_ 액션 2. 대기(Idle)
    // ------------------------------------------------------------------------------
    public virtual Coroutine Idle(bool playAnimation = false) 
    {
        StopMove();

        state.main = MainState.Idle;

        SetFriction(true);

        if (playAnimation) resources.Play(state.main);

        return action = StartCoroutine(_Idle());
    }

    protected virtual IEnumerator _Idle() { while (true) yield return new WaitForFixedUpdate(); }

    protected virtual void StopIdle()
    { 
        state.main = MainState.None;

        SetFriction(false);
    }

    // ------------------------------------------------------------------------------
    // 4_ 액션 3. 대미지(Damage)
    //    - 시작 -> TryDamage(), Damage()
    //    - 반복 -> _Damage()
    //    - 종료 -> StopDamage()
    // ------------------------------------------------------------------------------
    public virtual bool TryDamage(Transform attacker, DamageType type)
    {
        if ((type == DamageType.None) || !mask.HasFlag(type)) return false;

        switch (type)
        {
            case DamageType.Explode: type = DamageType.Normal;    break;
            case DamageType.Foot:    type = DamageType.PressDown; break;
        }

        Damage(attacker, type);

        return true;
    }

    public virtual Coroutine Damage(Transform attacker, DamageType type)
    {
        StopMove();

        state.main   = MainState.Damage;
        state.damage = type;

        switch (type)
        {
            case DamageType.PressDown:
                {
                    rigidbody.isKinematic                = true;
                    collider.enabled                     = false;
                    resources.model.transform.localScale = new Vector3(1f, 0.1f, 1f);
                }
                break;

            default:
                {
                    direction.LookAt(attacker);

                    Vector3 force = characterSetting.damage.GetForce(direction.transform);

                    rigidbody.AddForce(force, ForceMode.VelocityChange);
                }
                break;
        }

        resources.Play(type);

        return action = StartCoroutine(_Damage(attacker, type));
    }

    protected virtual IEnumerator _Damage(Transform attacker, DamageType type)
    {
        yield return new WaitForSeconds(characterSetting.damage.duration);
    }

    protected virtual void StopDamage()
    {
        state.main                           = MainState.None;
        state.damage                         = DamageType.None;
        rigidbody.isKinematic                = false;
        collider.enabled                     = true;
        resources.model.transform.localScale = Vector3.one;

        SetHeight();
    }
}
