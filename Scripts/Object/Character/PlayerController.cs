// //////////////////////////////////////////////////////////////////////////////
// * 목차
//    1. 인터페이스 ... Line 66
//    2. 클래스 ....... Line 114
//        1) 정의 ... Line 119
//            1- 캐릭터 상태 ... Line 122
//            2- 캐릭터 설정 ... Line 187
//        2) 필드 ..... Line 440
//        3) 메서드 ... Line 469
//            1- 이벤트 함수 ... Line 473
//            2- 초기화 ........ Line 534
//            3- 셋(Set) ....... Line 652
//            4- 액션 .......... Line 668
//                1_  대기(Idle) .............. Line 731
//                2_  피격(Damage) ............ Line 761
//                3_  낙하(Fall) .............. Line 865
//                4_  착지(Land) .............. Line 904
//                5_  달리기(Run) ............. Line 966
//                6_  멈추기(Brake) ........... Line 1028
//                7_  웅크리기(Crouch) ........ Line 1063
//                8_  뛰기(Jump) .............. Line 1152
//                9_  엉덩이 찍기(Hip Drop) ... Line 1301
//                10_ 공격(Attack) ............ Line 1383
//                11_ 상호작용(Interact) ...... Line 1583
//                12_ 부딪치기(Bump) .......... Line 1674
//                13_ 죽기(Die) ............... Line 1769
//                14_ 도착(Goal) .............. Line 1823
//            5- 오버랩(Overlap) ... Line 1878
//                1_ 면역(Immunize) ....... Line 1883
//                2_ 파워업(Power Up) ..... Line 1918
//                3_ 쿨타임(Cool Down) .... Line 1983
//                4_ 물건 나르기(Carry) ... Line 2019
// //////////////////////////////////////////////////////////////////////////////
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UI                 = PlayerController.UI;
using Resources          = PlayerController.Resources;
using State              = PlayerController.State;
using MainState          = PlayerController.State.Main;
using LandState          = PlayerController.State.Land;
using JumpState          = PlayerController.State.Jump;
using AttackState        = PlayerController.State.Attack;
using InteractState      = PlayerController.State.Interact;
using DieState           = PlayerController.State.Die;
using OverlapState       = PlayerController.State.Overlap.State;
using Setting            = PlayerController.Setting;
using EyeType            = PlayerModelController.Meshes.EyeType;
using FaceType           = PlayerModelController.Meshes.FaceType;
using HandType           = PlayerModelController.Meshes.HandType;
using EmoteType          = PlayerModelController.EmoteType;
using PowerUpEffectState = PlayerPowerUpEffectController.State;
using SubState           = CharacterBase.State.Sub;
using MoveSetting        = CharacterBase.CharacterSetting.Move;
using DamageSetting      = CharacterBase.CharacterSetting.Damage;
using DamageType         = IDamageable.Type;
using TransporterType    = TransporterBase.Type;
using LauncherState      = LauncherController.State.Transport;
using GoalType           = GoalController.Type;
using PowerUpType        = PowerUpItemController.Type;
using CameraShakeType    = CameraController.ShakeSetting.Type;

// //////////////////////////////////////////////////////////////////////////////
// 1. 인터페이스(ICharacterBase 인터페이스 상속)
// //////////////////////////////////////////////////////////////////////////////
public interface IPlayerController : ICharacterBase
{
    // 프로퍼티
    // Component
    IPlayerInputManager                              input        { get; }
    new IPlayerDirectionController                   direction    { get; }
    Dictionary<DamageType, IPlayerTriggerController> triggers     { get; }
    IPlayerCameraTargetController                    cameraTarget { get; }
    UI                                               ui           { get; }
    new Resources                                    resources    { get; }

    // Reference
    ISceneBase scene { get; }

    // State
    new State state { get; }

    // Setting
    Setting setting { get; }

    // 메서드
    // Set
    void SetHitPoint(int amount);

    // Action
    Coroutine Fall();
    Coroutine Land(LandState type);
    Coroutine Run();
    Coroutine Brake();
    Coroutine Crouch();
    Coroutine Jump(JumpState type);
    Coroutine HipDrop();
    Coroutine Attack(AttackState type);
    Coroutine Interact(IInteractable interactable);
    Coroutine Bump(Vector3 normal);
    Coroutine Die(DieState type);
    Coroutine Goal(GoalType type);

    // Overlap
    Coroutine Immunize();
    Coroutine PowerUp(PowerUpType type);
    Coroutine CoolDown();
    Coroutine Carry(IThrowableBase throwable);
}

// //////////////////////////////////////////////////////////////////////////////
// 2. 클래스(CharacterBase 클래스 상속)
// //////////////////////////////////////////////////////////////////////////////
public class PlayerController : CharacterBase, IPlayerController
{
    // ==============================================================================
    // 1) 정의
    // ==============================================================================
    // ------------------------------------------------------------------------------
    // 1-1) 정의 -> 캐릭터 상태
    //    - 부모 클래스(CharacterBase.State)를 대체하여 새로 정의
    //    - 캐릭터의 주 상태(액션)에 대한 보조 상태 저장
    //    - 주 상태 외에 특수 상태(오버랩) 저장
    // ------------------------------------------------------------------------------
    public new class State : CharacterBase.State
    {
        // Definition
        public new enum Main
        {
            None   = 0,  Idle = 1,   Damage  = 2,   Fall   = 4,   Land     = 8,    Run  = 16,   Brake = 32,
            Crouch = 64, Jump = 128, HipDrop = 256, Attack = 512, Interact = 1024, Bump = 2048, Die   = 4096,
            Goal   = 8192
        }
        public enum Land { None = 0, Light = 1, Stunt = 2, Hard = 4 }
        public enum Jump
        {
            None  = 0, Low = 1, Middle = 2, High = 4, Turn = 8, Long = 16, Back = 32, Hip = 64, Bounce = 128,
            Super = 256
        }
        public enum Attack   { None = 0, Spin = 1, Fire = 2, Throw = 4, Dive = 8 }
        public enum Interact { None = 0, CarryUp = 1, Transport = 2, Talk = 4 }
        public enum Die      { None = 0, Normal = 1, Bungee = 2 }

        public class Overlap : Dictionary<OverlapState, bool>
        {
            public enum State { None = 0, Immunize = 1, PowerUp = 2, CoolDown = 4, Carry = 8 }

            public PowerUpType powerUp;

            public Overlap() : base()
            {
                foreach (State type in Enum.GetValues(typeof(State)))
                {
                    if (type == State.None) continue;

                    Add(type, false);
                }

                powerUp = PowerUpType.None;
            }
        }

        // Field
        public new Main       main;
        public Land           land;
        public Jump           jump;
        public Attack         attack;
        public Interact       interact;
        public Die            die;
        public GoalType       goal;
        public Overlap        overlap;
        public IInteractable  interactable;
        public IThrowableBase throwable;
        public int            hitPoint;

        // Method
        public State(int hitPoint) : base()
        {
            overlap       = new Overlap();
            this.hitPoint = hitPoint;
        }
    }

    // ------------------------------------------------------------------------------
    // 1-2) 정의 -> 캐릭터 설정
    //    - 캐릭터 상태에 대한 설정 프로퍼티 저장
    // ------------------------------------------------------------------------------
    [Serializable] public class Setting
    {
        // Definition
        [Serializable] public class Fall
        {
            [SerializeField] protected MoveSetting _move;

            public MoveSetting move { get { return _move; } }

            public Fall(MoveSetting move) { _move = move; }
        }

        [Serializable] public class Land : SimpleData<LandState, Land.Value>
        {
            [Serializable] public class Value
            {
                [SerializeField] protected float _duration;

                public float duration { get { return _duration; } }

                public Value(float duration) { _duration = duration; }
            }
            
            public Land(List<Element> elements) : base(elements) { }

            public Value GetOrDefault(LandState type) => this[ContainsKey(type) ? type : LandState.None];
        }

        [Serializable] public class Run : SimpleData<PowerUpType, Run.Value>
        {
            [Serializable] public class Value
            {
                [SerializeField] protected MoveSetting _move;

                public MoveSetting move { get { return _move; } }

                public Value(MoveSetting move) { _move = move; }
            }
            
            public Run(List<Element> elements) : base(elements) { }

            public Value GetOrDefault(PowerUpType type) => this[ContainsKey(type) ? type : PowerUpType.None];
        }

        [Serializable] public class Brake
        {
            [SerializeField] protected MoveSetting _move;

            public MoveSetting move { get { return _move; } }

            public Brake(MoveSetting move) { _move = move; }
        }

        [Serializable] public class Crouch : SimpleData<SubState, Crouch.Value>
        {
            [Serializable] public class Value
            {
                [SerializeField] protected float       _duration;
                [SerializeField] protected MoveSetting _move;

                public float       duration { get { return _duration; } }
                public MoveSetting move     { get { return _move; } }

                public Value(float duration, MoveSetting move)
                {
                    _duration = duration;
                    _move     = move;
                }
            }
            
            [SerializeField, Range(0f, 1f)] protected float _heightRate;

            public float heightRate { get { return _heightRate; } }

            public Crouch(List<Element> elements, float heightRate) : base(elements) { _heightRate = heightRate; }
        }

        [Serializable] public class Jump : SimpleData<JumpState, Jump.Value>
        {
            [Serializable] public class Value
            {
                [SerializeField, Range(0f, 2f)]   protected float _rate;
                [SerializeField, Range(0f, 180f)] protected float _angle;
                [SerializeField, Range(0f, 1f)]   protected float _heightRate;

                public float rate       { get { return _rate; } }
                public float angle      { get { return _angle; } }
                public float heightRate { get { return _heightRate; } }

                public Value(float rate, float angle, float heightRate)
                {
                    _rate       = rate;
                    _angle      = angle;
                    _heightRate = heightRate;
                }
                
                public Vector3 GetForce(Transform character, float force)
                {
                    float   _angle    = angle * Mathf.Deg2Rad;
                    Vector3 direction = (Vector3.forward * Mathf.Cos(_angle)) + (Vector3.up * Mathf.Sin(_angle));
                    Vector3 _force    = Mathf.Sqrt(force * rate) * direction;

                    return character.TransformDirection(_force);
                }
            }

            [SerializeField] protected float       _force;
            [SerializeField] protected float       _interval;
            [SerializeField] protected MoveSetting _move;

            public float       force    { get { return _force; } }
            public float       interval { get { return _interval; } }
            public MoveSetting move     { get { return _move; } }

            public Jump(List<Element> elements, float force, float interval, MoveSetting move) : base(elements)
            {
                _force    = force;
                _interval = interval;
                _move     = move;
            }
            
            public Value GetOrDefault(JumpState state) => this[ContainsKey(state) ? state : JumpState.None];
        }

        [Serializable] public class HipDrop
        {
            [SerializeField]                protected SimpleData<SubState, float> _durations;
            [SerializeField, Range(0f, 1f)] protected float                       _heightRate;

            public SimpleData<SubState, float> durations  { get { return _durations; } }
            public float                       heightRate { get { return _heightRate; } }

            public HipDrop(SimpleData<SubState, float> durations, float heightRate)
            {
                _durations  = durations;
                _heightRate = heightRate;
            }
        }

        [Serializable] public class Attack : SimpleData<AttackState, Attack.Value>
        {
            [Serializable] public class Value : Jump.Value
            {
                [SerializeField] protected float _duration;

                public float duration { get { return _duration; } }

                public Value(float rate, float angle, float heightRate, float duration) : base(rate, angle, heightRate)
                {
                    _duration = duration;
                }
            }

            [SerializeField] protected float                               _force;
            [SerializeField] protected SimpleData<PowerUpType, GameObject> _projectiles;
            [SerializeField] protected MoveSetting                         _move;

            public float                               force       { get { return _force; } }
            public SimpleData<PowerUpType, GameObject> projectiles { get { return _projectiles; } }
            public MoveSetting                         move        { get { return _move; } }

            public Attack(List<Element> elements, float force, SimpleData<PowerUpType, GameObject> projectiles, 
                MoveSetting move) : base(elements)
            {
                _force       = force;
                _projectiles = projectiles;
                _move        = move;
            }

            public Value GetOrDefault(AttackState state) => this[ContainsKey(state) ? state : AttackState.None];
        }

        [Serializable] public class Interact
        {
            [SerializeField] protected float _duration;

            public float duration { get { return _duration; } }

            public Interact(float duration) { _duration = duration; }
        }


        [Serializable] public class Bump : DamageSetting
        {
            [SerializeField, Range(0f, 1f)] protected float _heightRate;

            public float heightRate { get { return _heightRate; } }

            public Bump(float duration, float force, float angle, float heightRate) : base(duration, force, angle)
            {
                _heightRate = heightRate;
            }
        }

        [Serializable] public class Overlap
        {
            [SerializeField] protected float _duration;

            public float duration { get { return _duration; } }

            public Overlap(float duration) { _duration = duration; }
        }

        // Field
        [SerializeField] protected Fall                              _fall;
        [SerializeField] protected Land                              _land;
        [SerializeField] protected Run                               _run;
        [SerializeField] protected Brake                             _brake;
        [SerializeField] protected Crouch                            _crouch;
        [SerializeField] protected Jump                              _jump;
        [SerializeField] protected HipDrop                           _hipDrop;
        [SerializeField] protected Attack                            _attack;
        [SerializeField] protected Interact                          _interact;
        [SerializeField] protected Bump                              _bump;
        [SerializeField] protected SimpleData<OverlapState, Overlap> _overlap;
        [SerializeField] protected int                               _maxHitPoint;

        public Fall                               fall        { get { return _fall; } }
        public Land                               land        { get { return _land; } }
        public Run                                run         { get { return _run; } }
        public Brake                              brake       { get { return _brake; } }
        public Crouch                             crouch      { get { return _crouch; } }
        public Jump                               jump        { get { return _jump; } }
        public HipDrop                            hipDrop     { get { return _hipDrop; } }
        public Attack                             attack      { get { return _attack; } }
        public Interact                           interact    { get { return _interact; } }
        public Bump                               bump        { get { return _bump; } }
        public SimpleData<OverlapState, Overlap>  overlap     { get { return _overlap; } }
        public int                                maxHitPoint { get { return _maxHitPoint; } }

        // Method
        public Setting(Fall fall, Land land, Run run, Brake brake, Crouch crouch, Jump jump, HipDrop hipDrop,
            Attack attack, Interact interact, Bump bump, SimpleData<OverlapState, Overlap> overlap, int maxHitPoint)
        {
            _fall        = fall;
            _land        = land;
            _run         = run;
            _brake       = brake;
            _crouch      = crouch;
            _jump        = jump;
            _hipDrop     = hipDrop;
            _attack      = attack;
            _interact    = interact;
            _bump        = bump;
            _overlap     = overlap;
            _maxHitPoint = maxHitPoint;
        }
    }
    
    // ==============================================================================
    // 2) 필드
    // ==============================================================================
    // Component & Reference
    public IPlayerInputManager                              input        { get; protected set; }
    public new IPlayerDirectionController                   direction    { get; protected set; }
    public Dictionary<DamageType, IPlayerTriggerController> triggers     { get; protected set; }
    public IPlayerCameraTargetController                    cameraTarget { get; protected set; }
    public UI                                               ui           { get; protected set; }
    public new Resources                                    resources    { get; protected set; }
    public ISceneBase                                       scene        { get; protected set; }

    // State
    public new State state { get; protected set; }

    // Setting
    [SerializeField] protected Setting _setting;

    public Setting setting { get { return _setting; } }

    // etc.
    protected Dictionary<OverlapState, Coroutine> overlapActions;
    protected Coroutine                           jumpKeepAction;
    protected Coroutine                           interactableSetAction;

    protected IInteractable interactable, tempInteractable;

    protected Vector3 prevPosition, currentPosition;

    // ==============================================================================
    // 3) 메서드
    //    - 부모 클래스의 함수들을 재정의하여 확장
    // ==============================================================================
    // ------------------------------------------------------------------------------
    // 3-1) 메서드 -> 이벤트 함수
    //    - 캐릭터의 기본 정보 갱신
    //    - 액션에 대한 입력 대기
    //    - 범위 내에 Interactable 오브젝트 탐색
    // ------------------------------------------------------------------------------
    protected override void FixedUpdate()
    {
        prevPosition    = currentPosition;
        currentPosition = transform.position;

        base.FixedUpdate();

        if (state.main == MainState.None) return;

        tempInteractable = null;

        if ((state.main != MainState.Fall)   && !groundState.isGrounded)                TryFall();
        if ((state.main != MainState.Run)    && (input.moveDirection.magnitude > 0.1f)) TryRun();
        if ((state.main != MainState.Crouch) && input.crouchKeyPressed)                 TryCrouch();
    }

    protected virtual void OnTriggerStay(Collider other)
    {
        if (!other.TryGetComponent(out IInteractable interactable)) return;

        TrySetTempInteractable(interactable);
    }

    protected virtual void OnCollisionEnter(Collision collision)
    {
        if (!collision.transform.TryGetComponent(out IDamageable target) || !(target is IEnemyBase))
        {
            if (state.main != MainState.Bump) TryBump(collision);

            return;
        }
        if (state.overlap.powerUp == PowerUpType.SuperStar)
        {
            if (target.TryDamage(transform, DamageType.Explode))
            {
                ICameraController camera = scene.camera;

                camera.Shake(CameraShakeType.Medium);
            }
        }
    }

    protected virtual void Update()
    {
        if ((Time.timeScale == 0f) || (state.main == MainState.None)) return;

        SetInteractable();
        cameraTarget.Rotate(input.cameraAxis);

        if ((state.main != MainState.Jump)     && input.jumpKeyPressed)     TryJump();
        if ((state.main != MainState.HipDrop)  && input.hipDropKeyPressed)  TryHipDrop();
        if ((state.main != MainState.Attack)   && input.attackKeyPressed)   TryAttack();
        if ((state.main != MainState.Interact) && input.interactKeyPressed) TryInteract();
    }

    // ------------------------------------------------------------------------------
    // 3-2) 메서드 -> 초기화
    //    - 필드(컴포넌트 등) 초기화
    // ------------------------------------------------------------------------------
    protected override void SetField()
    {
        base.SetField();

        input        = GetComponentInChildren<IPlayerInputManager>(true);
        direction    = GetComponentInChildren<IPlayerDirectionController>(true);
        triggers     = new Dictionary<DamageType, IPlayerTriggerController>();
        cameraTarget = GetComponentInChildren<IPlayerCameraTargetController>(true);
        ui           = new UI(transform.Find("UI"));
        resources    = new Resources(transform.Find("Resources"));
        scene        = FindObjectOfType<SceneBase>(true);

        foreach (var trigger in transform.Find("Triggers").GetComponentsInChildren<IPlayerTriggerController>(true))
        {
            string name = trigger.gameObject.name.Replace(" ", string.Empty);

            if (Enum.TryParse(name, out DamageType type)) triggers.Add(type, trigger);
        }

        state = new State(setting.maxHitPoint);

        overlapActions = new Dictionary<OverlapState, Coroutine>();

        foreach (OverlapState type in Enum.GetValues(typeof(OverlapState))) overlapActions.Add(type, null);
    }

    protected override void ResetField(Rigidbody rigidbody)
    {
        base.ResetField(rigidbody);

        _characterSetting = new CharacterSetting(new DamageSetting(1.5f, 150f, 60f));
        _setting          = new Setting(
            new Setting.Fall(new MoveSetting(2.5f, 5f)),
            new Setting.Land(
                new List<SimpleData<LandState, Setting.Land.Value>.Element>()
                {
                    new SimpleData<LandState, Setting.Land.Value>.Element(LandState.None,  new Setting.Land.Value(1f)),
                    new SimpleData<LandState, Setting.Land.Value>.Element(LandState.Light, new Setting.Land.Value(0.5f))
                }),
            new Setting.Run(
                new List<SimpleData<PowerUpType, Setting.Run.Value>.Element>()
                {
                    new SimpleData<PowerUpType, Setting.Run.Value>.Element(
                        PowerUpType.None, new Setting.Run.Value(new MoveSetting(10f, 10f))),
                    new SimpleData<PowerUpType, Setting.Run.Value>.Element(
                        PowerUpType.SuperStar, new Setting.Run.Value(new MoveSetting(12.5f, 10f)))
                }),
            new Setting.Brake(new MoveSetting(0f, 10f)),
            new Setting.Crouch(
                new List<SimpleData<SubState, Setting.Crouch.Value>.Element>()
                {
                    new SimpleData<SubState, Setting.Crouch.Value>.Element(
                        SubState.Start, new Setting.Crouch.Value(0.25f, new MoveSetting(0f, 5f))),
                    new SimpleData<SubState, Setting.Crouch.Value>.Element(
                        SubState.Loop, new Setting.Crouch.Value(0f, new MoveSetting(2.5f, 10f))),
                    new SimpleData<SubState, Setting.Crouch.Value>.Element(
                        SubState.End, new Setting.Crouch.Value(0.25f, new MoveSetting(0f, 10f)))
                },
                0f),
            new Setting.Jump(
                new List<SimpleData<JumpState, Setting.Jump.Value>.Element>()
                {
                    new SimpleData<JumpState, Setting.Jump.Value>.Element(JumpState.None,   new Setting.Jump.Value(1f,    90f, 1f)),
                    new SimpleData<JumpState, Setting.Jump.Value>.Element(JumpState.Middle, new Setting.Jump.Value(1.25f, 90f, 1f)),
                    new SimpleData<JumpState, Setting.Jump.Value>.Element(JumpState.High,   new Setting.Jump.Value(1.5f,  90f, 0.5f)),
                    new SimpleData<JumpState, Setting.Jump.Value>.Element(JumpState.Turn,   new Setting.Jump.Value(1.5f,  85f, 1f)),
                    new SimpleData<JumpState, Setting.Jump.Value>.Element(JumpState.Long,   new Setting.Jump.Value(1f,    45f, 0.5f)),
                    new SimpleData<JumpState, Setting.Jump.Value>.Element(JumpState.Back,   new Setting.Jump.Value(1.5f,  95f, 0.5f)),
                    new SimpleData<JumpState, Setting.Jump.Value>.Element(JumpState.Hip,    new Setting.Jump.Value(1.5f,  90f, 1f)),
                    new SimpleData<JumpState, Setting.Jump.Value>.Element(JumpState.Bounce, new Setting.Jump.Value(0.75f, 90f, 1f)),
                    new SimpleData<JumpState, Setting.Jump.Value>.Element(JumpState.Super,  new Setting.Jump.Value(1.25f, 90f, 0.5f))
                },
                500f, 0.25f, new MoveSetting(5f, 7.5f)),
            new Setting.HipDrop(
                new SimpleData<SubState, float>(
                    new List<SimpleData<SubState, float>.Element>()
                    {
                        new SimpleData<SubState, float>.Element(SubState.Start, 0.5f),
                        new SimpleData<SubState, float>.Element(SubState.End,   0.5f)
                    }),
                0.5f),
            new Setting.Attack(
                new List<SimpleData<AttackState, Setting.Attack.Value>.Element>()
                {
                    new SimpleData<AttackState, Setting.Attack.Value>.Element(AttackState.None, new Setting.Attack.Value(1f, 90f, 1f,   0.5f)),
                    new SimpleData<AttackState, Setting.Attack.Value>.Element(AttackState.Spin, new Setting.Attack.Value(3f, 90f, 1f,   0.5f)),
                    new SimpleData<AttackState, Setting.Attack.Value>.Element(AttackState.Dive, new Setting.Attack.Value(4f, 45f, 0.5f, 0.4f))
                },
                125f, new SimpleData<PowerUpType, GameObject>(
                    new List<SimpleData<PowerUpType, GameObject>.Element>() { }),
                new MoveSetting(2.5f, 5f)),
            new Setting.Interact(0.5f),
            new Setting.Bump(1f, 50f, 45f, 0.5f),
            new SimpleData<OverlapState, Setting.Overlap>(
                new List<SimpleData<OverlapState, Setting.Overlap>.Element>()
                {
                    new SimpleData<OverlapState, Setting.Overlap>.Element(
                        OverlapState.Immunize, new Setting.Overlap(1f)),
                    new SimpleData<OverlapState, Setting.Overlap>.Element(
                        OverlapState.PowerUp, new Setting.Overlap(20f)),
                    new SimpleData<OverlapState, Setting.Overlap>.Element(
                        OverlapState.CoolDown, new Setting.Overlap(1f))
                }),
            3);
    }

    public override void Initialize()
    {
        base.Initialize();
        ui.Initialize();

        foreach (var trigger in triggers.Values) trigger.gameObject.SetActive(false);
    }

    // ------------------------------------------------------------------------------
    // 3-3) 메서드 -> 셋(Set)
    //    - 캐릭터의 배치, 형태, 체력 설정
    // ------------------------------------------------------------------------------
    public override Coroutine Set(ICharacterTargetController target, bool resetDirection = false, bool setIdle = false, float duration = 0)
    {
        if (resetDirection) cameraTarget.Initialize();

        return base.Set(target, resetDirection, setIdle, duration);
    }

    public virtual void SetHitPoint(int amount)
    {
        state.hitPoint = Mathf.Clamp(state.hitPoint + amount, 0, setting.maxHitPoint);
    }

    // ------------------------------------------------------------------------------
    // 3-4) 메서드 -> 액션(Common)
    //    - 모든 행동에 대한 정지
    //    - 캐릭터의 회전 및 이동
    //    - 지면과의 접지에 대한 지연 처리
    // ------------------------------------------------------------------------------
    public override void StopAction()
    {
        base.StopAction();

        switch (state.main)
        {
            case MainState.Fall:     StopFall();     break;
            case MainState.Land:     StopLand();     break;
            case MainState.Run:      StopRun();      break;
            case MainState.Brake:    StopBrake();    break;
            case MainState.Crouch:   StopCrouch();   break;
            case MainState.Jump:     StopJump();     break;
            case MainState.HipDrop:  StopHipDrop();  break;
            case MainState.Attack:   StopAttack();   break;
            case MainState.Interact: StopInteract(); break;
            case MainState.Bump:     StopBump();     break;
            case MainState.Die:      StopDie();      break;
            case MainState.Goal:     StopGoal();     break;
        }
    }

    protected virtual void RotateAndMove(MoveSetting setting, bool isKinematic = false)
    {
        Vector3 inputDirection = input.moveDirection;
        var     newSetting     = MoveSetting.MultiplySpeed(setting, inputDirection.magnitude);

        direction.Rotate(inputDirection);
        Move(newSetting, isKinematic);
    }

    protected virtual IEnumerator WaitUntilNotGrounded(MoveSetting setting, bool isKinematic = false,
        int maxFrameCount = 3)
    {
        for (int count = 0; count < maxFrameCount; count++)
        {
            if (!groundState.isGrounded) break;

            RotateAndMove(setting, isKinematic);

            yield return new WaitForFixedUpdate();
        }

        if (groundState.isGrounded) yield return new WaitForFixedUpdate();
    }

    protected virtual IEnumerator WaitUntilGrounded(MoveSetting setting, bool isKinematic = false, bool useDelay = false)
    {
        if (useDelay) yield return WaitUntilNotGrounded(setting, isKinematic);

        while (!groundState.isGrounded)
        {
            RotateAndMove(setting, isKinematic);

            yield return new WaitForFixedUpdate();
        }
    }

    // ******************************************************************************
    // 3-4-1) 메서드 -> 액션 -> 대기(Idle)
    // ******************************************************************************
    public override Coroutine Idle(bool playAnimation = false)
    {
        StopAction();

        state.main = MainState.Idle;

        return base.Idle(playAnimation);
    }

    protected override IEnumerator _Idle()
    {
        yield return new WaitForSeconds(10f);

        resources.model.Play(EmoteType.Tired);

        yield return base._Idle();
    }

    protected override void StopIdle()
    {
        base.StopIdle();

        state.main = MainState.None;

        resources.model.Play(EmoteType.None);
    }

    // ******************************************************************************
    // 3-4-2) 메서드 -> 액션 -> 피격(Damage)
    //    - 타입: Normal 고정
    //    - 피격 시 캐릭터 넉백 및 체력 감소
    //    - 체력 상태에 따라 Idle 또는 Die 함수 호출
    // ******************************************************************************
    public override bool TryDamage(Transform attacker, DamageType type)
    {
        MainState     invalidType       = MainState.Damage | MainState.Die | MainState.Goal;
        InteractState validInteractType = InteractState.CarryUp;
        PowerUpType   validPowerUpType  = PowerUpType.FireFlower;

        if (invalidType.HasFlag(state.main))                  return false;
        if (!validInteractType.HasFlag(state.interact))       return false;
        if (!validPowerUpType.HasFlag(state.overlap.powerUp)) return false;
        if (state.overlap[OverlapState.Immunize])             return false;

        return base.TryDamage(attacker, type);
    }

    public override Coroutine Damage(Transform attacker, DamageType type)
    {
        StopAction();

        if (state.overlap[OverlapState.Carry]) StopCarry(true);

        state.main   = MainState.Damage;
        state.damage = type;

        //int amount = ((type == DamageType.PressDown) ? setting.maxHitPoint : 1) * -1;
        int amount = -1;

        SetHitPoint(amount);
        SetCollision(false);
        resources.model.SetEye(EyeType.HalfClosed);
        resources.model.SetFace(FaceType.Painful);

        return base.Damage(attacker, type);
    }

    protected override IEnumerator _Damage(Transform attacker, DamageType type)
    {
        switch (type)
        {
            case DamageType.Normal:
                {
                    ICameraController camera = scene.camera;

                    state.sub = SubState.Start;

                    camera.Shake(CameraShakeType.Soft);

                    yield return WaitUntilGrounded(true);

                    state.sub = SubState.End;

                    resources.model.PlayNext();
                    resources.effects.land.Play(LandState.None);

                    if (state.hitPoint <= 0)
                    {
                        Die(DieState.Normal);
                        yield break;
                    }
                }
                break;
        }

        SetCollision(true);
        SetFriction(true);

        yield return base._Damage(attacker, type);

        if (state.hitPoint > 0)
        {
            Immunize();
            //Idle(type == DamageType.PressDown);
            Idle();
        }
        else Die(DieState.Normal);
    }

    protected override void StopDamage()
    {
        base.StopDamage();

        state.main   = MainState.None;
        state.sub    = SubState.None;
        state.damage = DamageType.None;

        SetCollision(true);
        SetFriction(false);
        resources.model.SetEye();
        resources.model.SetFace();
    }

    protected virtual void SetCollision(bool enabled)
    {
        int layer1 = LayerMask.NameToLayer("Player");
        int layer2 = LayerMask.NameToLayer("Character");

        Physics.IgnoreLayerCollision(layer1, layer2, !enabled);
    }

    // ******************************************************************************
    // 3-4-3) 메서드 -> 액션 -> 낙하(Fall)
    // ******************************************************************************
    protected virtual void TryFall()
    {
        MainState validType = MainState.Idle | MainState.Land | MainState.Run | MainState.Brake | MainState.Crouch;

        if ((state.main == MainState.None) || !validType.HasFlag(state.main)) return;

        Fall();
    }

    public virtual Coroutine Fall()
    {
        StopAction();

        state.main         = MainState.Fall;
        rigidbody.velocity = Vector3.ProjectOnPlane(rigidbody.velocity, transform.up);

        triggers[DamageType.Foot].gameObject.SetActive(true);
        resources.Play(state.main);

        return action = StartCoroutine(_Fall());
    }

    protected virtual IEnumerator _Fall()
    {
        yield return WaitUntilGrounded(setting.fall.move);

        Land(LandState.Light);
    }

    protected virtual void StopFall()
    {
        state.main = MainState.None;

        triggers[DamageType.Foot].gameObject.SetActive(false);
    }

    // ******************************************************************************
    // 3-4-4) 메서드 -> 액션 -> 착지(Land)
    //    - 타입: Light, Stunt, Hard
    //    - 특수 액션(3단 점프, 백 점프 등) 이후 Stunt 타입으로 착지
    //    - 낙하 속도가 기준값을 넘어서면 Hard 타입으로 착지
    // ******************************************************************************
    public virtual Coroutine Land(LandState type)
    {
        StopAction();

        Vector3 velocity  = rigidbody.velocity;
        float   fallSpeed = (Vector3.Angle(velocity, gravity) < 90f) ? Vector3.Project(velocity, gravity).magnitude 
                                                                     : 0f;

        state.main = MainState.Land;
        state.land = (fallSpeed < gravity.magnitude) ? type : LandState.Hard;

        if (input.moveDirection.magnitude <= 0f) StopMove();

        switch (type)
        {
            case LandState.Stunt:
                {
                    resources.model.SetFace(FaceType.Happy);
                    resources.model.SetHand(HandType.Open);
                }
                break;

            case LandState.Hard:
                {
                    ICameraController camera = scene.camera;

                    StopMove();
                    camera.Shake(CameraShakeType.Soft);
                }
                break;
        }

        SetFriction(true);
        resources.Play(type);

        return action = StartCoroutine(_Land(type));
    }

    protected virtual IEnumerator _Land(LandState type)
    {
        yield return new WaitForSeconds(setting.land.GetOrDefault(type).duration);

        Idle();
    }

    protected virtual void StopLand()
    {
        state.main = MainState.None;
        state.land = LandState.None;

        SetFriction(false);
        resources.model.SetEye();
        resources.model.SetFace();
        resources.model.SetHand();
    }

    // ******************************************************************************
    // 3-4-5) 메서드 -> 액션 -> 달리기(Run)
    //    - 입력값(벡터)과 캐릭터의 방향 사이 각도가 넓으면 Brake 함수 호출
    // ******************************************************************************
    protected virtual void TryRun()
    {
        MainState validType     = MainState.Idle  | MainState.Land;
        LandState validLandType = LandState.Light | LandState.Stunt;

        if ((state.main == MainState.None) || !validType.HasFlag(state.main)) return;
        if (!validLandType.HasFlag(state.land))                               return;

        Run();
    }

    public virtual Coroutine Run()
    {
        StopAction();

        state.main = MainState.Run;

        resources.Play(state.main);

        return action = StartCoroutine(_Run());
    }

    protected virtual IEnumerator _Run()
    {
        Vector3 inputDirection = input.moveDirection;
        Vector3 prevDirection  = Vector3.zero;
        var     settings       = setting.run;
        var     defaultSetting = settings[PowerUpType.None];

        do
        {
            float speedRate  = moveSpeed / defaultSetting.move.speed;
            float inputAngle = Vector3.Angle(inputDirection, prevDirection);

            if ((speedRate > 0.9f) && ((inputAngle > 90f) || (inputDirection.magnitude <= 0f)))
            {
                Brake();
                yield break;
            }

            var setting = settings.GetOrDefault(state.overlap.powerUp);

            RotateAndMove(setting.move);    // Move Method(CharacterBase)
            resources.model.SetMoveRate(speedRate);

            yield return new WaitForFixedUpdate();

            prevDirection  = inputDirection;
            inputDirection = input.moveDirection;
        }
        while ((inputDirection.magnitude > 0f) || (moveSpeed > 0f));

        resources.model.PlayNext();
        Idle();
    }

    protected virtual void StopRun() { state.main = MainState.None; }

    // ******************************************************************************
    // 3-4-6) 메서드 -> 액션 -> 멈추기(Brake)
    //    - 캐릭터의 급제동
    // ******************************************************************************
    public virtual Coroutine Brake()
    {
        StopAction();

        state.main = MainState.Brake;

        resources.Play(state.main);

        return action = StartCoroutine(_Brake());
    }

    protected virtual IEnumerator _Brake()
    {
        while (moveSpeed > 0f)
        {
            Move(setting.brake.move);

            yield return new WaitForFixedUpdate();
        }

        resources.model.PlayNext();
        Idle();
    }

    protected virtual void StopBrake()
    {
        state.main = MainState.None;

        resources.effects[MainState.Brake].Stop(0.75f);
    }

    // ******************************************************************************
    // 3-4-7) 메서드 -> 액션 -> 웅크리기(Crouch)
    //    - Crouch 상태에서의 캐릭터 이동
    //    - 실행 전에 캐릭터가 이동중이면 미끄러지면서 웅크림
    // ******************************************************************************
    protected virtual void TryCrouch()
    {
        MainState validType     = MainState.Idle  | MainState.Land | MainState.Run;
        LandState validLandType = LandState.Light | LandState.Stunt;

        if ((state.main == MainState.None) || !validType.HasFlag(state.main)) return;
        if (!validLandType.HasFlag(state.land))                               return;
        if (state.overlap[OverlapState.Carry])                                return;

        Crouch();
    }

    public virtual Coroutine Crouch()
    {
        StopAction();

        state.main = MainState.Crouch;

        SetHeight(setting.crouch.heightRate);
        resources.Play(state.main);

        return action = StartCoroutine(_Crouch());
    }

    protected virtual IEnumerator _Crouch()
    {
        state.sub = SubState.Start;

        if (moveSpeed > 0f) resources.effects[MainState.Brake].Play();

        var   setting     = this.setting.crouch;
        float elapsedTime = 0f;

        while ((moveSpeed > 0f) || (elapsedTime < setting[state.sub].duration))
        {
            Move(setting[state.sub].move);

            elapsedTime += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }

        resources.effects[MainState.Brake].Stop(0.75f);

        state.sub = SubState.Loop;

        resources.model.PlayNext();

        yield return new WaitForFixedUpdate();

        var moveSetting = setting[state.sub].move;

        while (input.crouchKeyPressed)
        {
            RotateAndMove(moveSetting);
            resources.model.SetMoveRate(moveSpeed / moveSetting.speed);

            yield return new WaitForFixedUpdate();
        }

        state.sub   = SubState.End;
        elapsedTime = 0f;

        resources.model.PlayNext();

        while ((moveSpeed > 0f) || (elapsedTime < setting[state.sub].duration))
        {
            Move(setting[state.sub].move);

            elapsedTime += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }

        Idle();
    }

    protected virtual void StopCrouch()
    {
        state.main = MainState.None;
        state.sub  = SubState.None;

        SetHeight();
        resources.effects[MainState.Brake].Stop(0.75f);
    }

    // ******************************************************************************
    // 3-4-8) 메서드 -> 액션 -> 뛰기(Jump)
    //    - 타입: Normal(Low, Middle, High), Turn, Long, Back, Hip, Bounce, Supper
    //    - Normal 타입은 3단 점프를 위해 상태값을 잠시 저장
    //    - Brake 상태이면                         -> Turn 타입으로 점프
    //    - Crouch 상태이면 Crouch 진행 상태에 따라 -> Long, Back 타입으로 점프
    //    - Hip Drop 상태 도중 지면에 닿으면        -> Hip 타입으로 점프
    //    - 적을 밟으면                            -> Bounch 타입으로 점프
    //    - Power Up(Supper Star) 상태이면         -> Super 타입으로 점프
    // ******************************************************************************
    protected virtual void TryJump()
    {
        MainState validType = MainState.Idle | MainState.Land | MainState.Run | MainState.Brake | MainState.Crouch 
            | MainState.HipDrop;
        LandState validLandType = LandState.Light | LandState.Stunt;

        if ((state.main == MainState.None) || !validType.HasFlag(state.main)) return;
        if (!validLandType.HasFlag(state.land))                               return;

        JumpState type = JumpState.Low;

        if (state.overlap[OverlapState.Carry])
        {
            Jump(type);

            return;
        }

        switch (state.main)
        {
            case MainState.Brake:
                {
                    Vector3 inputDirection = cameraTarget.transform.TransformDirection(input.moveDirection);
                    float   angle          = Vector3.Angle(direction.transform.forward, inputDirection);

                    type = (angle > 90f) ? JumpState.Turn : JumpState.Low;
                }
                break;

            case MainState.Crouch:
                {
                    switch (state.sub)
                    {
                        case SubState.Start: type = (moveSpeed > 0f) ? JumpState.Long : JumpState.Back; break;
                        default:             type = JumpState.Back;                                     break;
                    }
                }
                break;

            case MainState.HipDrop:
                {
                    switch (state.sub)
                    {
                        case SubState.End: type = JumpState.Hip; break;
                        default:                                 return;
                    }
                }
                break;

            default:
                {
                    if (state.overlap.powerUp == PowerUpType.SuperStar) type = JumpState.Super;
                    else
                    {
                        switch (state.jump)
                        {
                            case JumpState.Low:    type = JumpState.Middle; break;
                            case JumpState.Middle: type = JumpState.High;   break;
                            default:               type = JumpState.Low;    break;
                        }
                    }
                }
                break;
        }

        Jump(type);
    }

    public virtual Coroutine Jump(JumpState type)
    {
        StopAction();

        if ((type == JumpState.Bounce) && state.overlap[OverlapState.CoolDown]) StopCoolDown(true);
        if (jumpKeepAction != null)
        {
            StopCoroutine(jumpKeepAction);

            jumpKeepAction = null;
        }

        state.main = MainState.Jump;
        state.jump = type;

        switch (type)
        {
            case JumpState.Turn: direction.transform.Rotate(Vector3.up * 180f); break;
            case JumpState.Long: resources.model.SetHand(HandType.Open);        break;
        }

        var   setting = this.setting.jump.GetOrDefault(type);
        float force   = this.setting.jump.force;

        if (setting.angle != 90f) StopMove();
        else                      rigidbody.velocity = Vector3.ProjectOnPlane(rigidbody.velocity, transform.up);

        SetHeight(setting.heightRate);
        rigidbody.AddForce(setting.GetForce(direction.transform, force), ForceMode.VelocityChange);
        triggers[DamageType.Foot].gameObject.SetActive(true);
        triggers[DamageType.Head].gameObject.SetActive(true);
        resources.Play(state.jump);

        return action = StartCoroutine(_Jump(type));
    }

    protected virtual IEnumerator _Jump(JumpState type)
    {
        var  setting     = this.setting.jump;
        bool isKinematic = setting.GetOrDefault(type).angle != 90f;

        yield return WaitUntilGrounded(setting.move, isKinematic, true);

        jumpKeepAction = StartCoroutine(KeepJumpType());

        JumpState stuntType = JumpState.High | JumpState.Back;
        LandState landType  = ((type != JumpState.None) && stuntType.HasFlag(type)) ? LandState.Stunt : LandState.Light;

        Land(landType);
    }

    protected virtual void StopJump()
    {
        state.main = MainState.None;

        if (jumpKeepAction == null) state.jump = JumpState.None;

        SetHeight();
        resources.model.SetHand();
        triggers[DamageType.Foot].gameObject.SetActive(false);
        triggers[DamageType.Head].gameObject.SetActive(false);
    }

    protected virtual IEnumerator KeepJumpType()
    {
        yield return new WaitForSeconds(setting.jump.interval);

        jumpKeepAction = null;
        state.jump     = JumpState.None;
    }

    // ******************************************************************************
    // 3-4-9) 메서드 -> 액션 -> 엉덩이 찍기(Hip Drop)
    //    - 공중에서 최대 속력(Gravity)으로 낙하
    //    - 아래에 적이 있으면 Press Down Damage 타입으로 공격
    // ******************************************************************************
    protected virtual void TryHipDrop()
    {
        MainState   validType       = MainState.Fall | MainState.Jump | MainState.Attack;
        JumpState   invalidJumpType = JumpState.Long;
        AttackState validAttackType = AttackState.Spin | AttackState.Fire | AttackState.Throw;

        if ((state.main == MainState.None) || !validType.HasFlag(state.main))      return;
        if ((state.jump != JumpState.None) && invalidJumpType.HasFlag(state.jump)) return;
        if (!validAttackType.HasFlag(state.attack))                                return;
        if (state.overlap[OverlapState.Carry])                                     return;

        if ((state.main == MainState.Attack) && (state.sub != SubState.Loop)) return;

        HipDrop();
    }

    public virtual Coroutine HipDrop()
    {
        StopAction();
        StopMove();

        state.main = MainState.HipDrop;

        SetHeight(setting.hipDrop.heightRate);
        resources.Play(state.main);

        return action = StartCoroutine(_HipDrop());
    }

    protected virtual IEnumerator _HipDrop()
    {
        ICameraController camera = scene.camera;

        var setting = this.setting.hipDrop;

        state.sub             = SubState.Start;
        rigidbody.isKinematic = true;

        yield return new WaitForSeconds(setting.durations[state.sub]);

        rigidbody.isKinematic = false;
        state.sub             = SubState.Loop;

        rigidbody.AddForce(gravity, ForceMode.VelocityChange);
        triggers[DamageType.PressDown].gameObject.SetActive(true);
        resources.Play(state.main, state.sub);

        yield return WaitUntilGrounded();

        triggers[DamageType.PressDown].gameObject.SetActive(false);
        resources.effects.hipDrop[state.sub].Stop();

        state.sub = SubState.End;

        StopMove();
        SetFriction(true);
        resources.Play(state.main, state.sub);
        resources.effects.land.Play(LandState.None);
        camera.Shake(CameraShakeType.Soft);

        yield return new WaitForSeconds(setting.durations[state.sub]);

        Idle();
    }

    protected virtual void StopHipDrop()
    {
        state.main            = MainState.None;
        state.sub             = SubState.None;
        rigidbody.isKinematic = false;

        SetHeight();
        SetFriction(false);
        triggers[DamageType.PressDown].gameObject.SetActive(false);
        resources.effects.hipDrop[SubState.Loop].Stop();
    }

    // ******************************************************************************
    // 3-4-10) 메서드 -> 액션 -> 공격(Attack)
    //    - 타입: Spin, Fire, Throw, Dive
    //    - Spin 타입으로 공격 시 일정 범위의 트리거 활성화
    //    - Power Up(Fire Flower) 상태이면 Fire 타입으로 공격 -> 타입에 따라 지정된 Projectile 오브젝트 발사
    //    - Carry 상태이면 Throw 타입으로 공격                -> 들고 있는 Throwable 오브젝트 투척
    //    - Hip Drop 상태이면 Dive 타입으로 공격              -> 공중에서 앞으로 다이빙
    // ******************************************************************************
    protected virtual void TryAttack()
    {
        MainState validType = MainState.Idle | MainState.Fall | MainState.Land | MainState.Run | MainState.Brake 
            | MainState.Crouch | MainState.Jump | MainState.HipDrop;
        LandState validLandType = LandState.Light | LandState.Stunt;

        if ((state.main == MainState.None) || !validType.HasFlag(state.main)) return;
        if (!validLandType.HasFlag(state.land))                               return;
        if (state.overlap[OverlapState.CoolDown])                             return;

        if ((state.main == MainState.HipDrop) && (state.sub != SubState.Start)) return;

        var         setting = this.setting.attack.projectiles;
        AttackState type    = AttackState.Spin;

        if      (state.main == MainState.HipDrop)            type = AttackState.Dive;
        else if (state.overlap[OverlapState.Carry])          type = AttackState.Throw;
        else if (setting.ContainsKey(state.overlap.powerUp)) type = AttackState.Fire;

        Attack(type);
    }

    public virtual Coroutine Attack(AttackState type)
    {
        IThrowableBase throwable = state.throwable;

        StopAction();

        if (state.overlap[OverlapState.Carry]) StopCarry(true);

        state.main   = MainState.Attack;
        state.attack = type;

        var setting = this.setting.attack.GetOrDefault(type);

        if (!groundState.isGrounded)
        {
            StopMove();

            float force = this.setting.attack.force;

            rigidbody.AddForce(setting.GetForce(direction.transform, force), ForceMode.VelocityChange);
        }

        SetHeight(setting.heightRate);
        resources.Play(type);

        switch (type)
        {
            case AttackState.Dive:  return action = StartCoroutine(Dive());
            case AttackState.Throw: return action = StartCoroutine(Throw(throwable));
            case AttackState.Fire:  return action = StartCoroutine(Fire(state.overlap.powerUp));
            default:                return action = StartCoroutine(Spin());
        }
    }

    protected virtual IEnumerator Spin()
    {
        state.sub = SubState.Start;

        triggers[DamageType.Normal].gameObject.SetActive(true);

        foreach (var hand in resources.model.bones.hands) hand.localScale = Vector3.one * 2f;

        var   setting     = this.setting.attack;
        float elapsedTime = 0f;
        float duration    = setting.GetOrDefault(state.attack).duration;

        while (elapsedTime < duration)
        {
            RotateAndMove(setting.move);

            elapsedTime += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }

        triggers[DamageType.Normal].gameObject.SetActive(false);

        foreach (var hand in resources.model.bones.hands) hand.localScale = Vector3.one;

        yield return EndAttack();
    }

    protected virtual IEnumerator Fire(PowerUpType type)
    {
        state.sub = SubState.Start;

        if (groundState.isGrounded) StopMove();

        var        setting    = this.setting.attack;
        Vector3    position   = direction.transform.TransformPoint(
            (Vector3.up * (collider.height - (collider.radius * 2f))) + (Vector3.forward * collider.radius));
        Quaternion rotation   = direction.transform.rotation;
        var projectile = Instantiate(setting.projectiles[type], position, rotation, planet.objects)
            .GetComponent<IProjectileBase>();

        projectile.Launch(this);

        yield return new WaitForSeconds(setting.GetOrDefault(state.attack).duration);
        yield return EndAttack();
    }

    protected virtual IEnumerator Throw(IThrowableBase throwable)
    {
        state.sub = SubState.Start;

        if (groundState.isGrounded) StopMove();

        throwable.Throw(this);

        var setting = this.setting.attack;

        yield return new WaitForSeconds(setting.GetOrDefault(state.attack).duration);
        yield return EndAttack();
    }

    protected virtual IEnumerator EndAttack()
    {
        if (groundState.isGrounded)
        {
            CoolDown();
            Idle();

            yield break;
        }

        state.sub = SubState.Loop;

        triggers[DamageType.Foot].gameObject.SetActive(true);
        resources.model.Play(MainState.Fall);

        yield return WaitUntilGrounded(setting.fall.move);

        CoolDown();
        Land(LandState.Light);
    }

    protected virtual IEnumerator Dive()
    {
        state.sub = SubState.Start;

        triggers[DamageType.Normal].gameObject.SetActive(true);

        var setting = this.setting.attack;

        yield return WaitUntilGrounded(setting.move, true);

        Vector3 velocity  = rigidbody.velocity;
        float   fallSpeed = (Vector3.Angle(velocity, gravity) < 90f) ? Vector3.Project(velocity, gravity).magnitude
                                                                     : 0f;

        if ((fallSpeed >= gravity.magnitude) || (input.moveDirection.magnitude <= 0f))
        {
            CoolDown();
            StopMove();
            Land(LandState.Light);
            yield break;
        }

        state.sub = SubState.End;

        resources.model.PlayNext();
        resources.effects.land.Play(LandState.Light);

        float elapsedTime = 0f;
        float duration    = setting.GetOrDefault(state.attack).duration;

        while (elapsedTime < duration)
        {
            RotateAndMove(setting.move);

            elapsedTime += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }

        CoolDown();
        Idle();
    }

    protected virtual void StopAttack()
    {
        state.main   = MainState.None;
        state.sub    = SubState.None;
        state.attack = AttackState.None;

        SetHeight();
        triggers[DamageType.Normal].gameObject.SetActive(false);
        triggers[DamageType.Foot].gameObject.SetActive(false);

        foreach (var hand in resources.model.bones.hands) hand.localScale = Vector3.one;
    }

    // ******************************************************************************
    // 3-4-11) 메서드 -> 액션 -> 상호작용(Interact)
    //    - 타입: Carry Up, Transport, Talk
    //    - 탐색 범위 내에 Interactable 오브젝트가 존재하면 실행, 각 기능은 오브젝트 내에 구현
    //    - Throwable 오브젝트이면   -> 오브젝트를 들어올림
    //    - Transporter 오브젝트이면 -> 지정된 위치로 캐릭터를 강제 이동
    //    - NPC 오브젝트이면         -> NPC와 대화
    // ******************************************************************************
    protected virtual void TrySetTempInteractable(IInteractable interactable)
    {
        MainState validType     = MainState.Idle  | MainState.Land | MainState.Run | MainState.Brake;
        LandState validLandType = LandState.Light | LandState.Stunt;

        if ((state.main == MainState.None) || !validType.HasFlag(state.main)) return;
        if (!validLandType.HasFlag(state.land))                               return;
        if (tempInteractable != null)
        {
            float distance    = (tempInteractable.transform.position - transform.position).sqrMagnitude;
            float newDistance = (interactable.transform.position     - transform.position).sqrMagnitude;

            if (newDistance > distance) return;
        }

        tempInteractable = interactable;
    }

    protected virtual void SetInteractable()
    {
        var prevInteractable = interactable;
            interactable     = tempInteractable;

        if (prevInteractable != interactable) ui.interactable.Display(interactable);
    }

    public virtual void TryInteract()
    {
        MainState validType     = MainState.Idle  | MainState.Land | MainState.Run | MainState.Brake;
        LandState validLandType = LandState.Light | LandState.Stunt;

        if (interactable == null)                                             return;
        if ((state.main == MainState.None) || !validType.HasFlag(state.main)) return;
        if (!validLandType.HasFlag(state.land))                               return;

        Interact(interactable);
    }

    public Coroutine Interact(IInteractable interactable)
    {
        StopAction();
        StopMove();

        if (state.overlap[OverlapState.Carry]) StopCarry(true);

        state.main         = MainState.Interact;
        state.interactable = interactable;

        switch (interactable)
        {
            case IThrowableBase:                state.interact = InteractState.CarryUp;   break;
            case ITransporterBase:              state.interact = InteractState.Transport; break;
            case INonPlayerCharacterController: state.interact = InteractState.Talk;      break;
        }

        SetFriction(true);
        resources.Play(state.interact);

        return action = StartCoroutine(_Interact(interactable));
    }

    protected virtual IEnumerator _Interact(IInteractable interactable)
    {
        yield return interactable.Interact(this);

        Idle();
    }

    protected virtual void StopInteract()
    {
        state.main     = MainState.None;
        state.interact = InteractState.None;

        if (state.interactable != null)
        {
            state.interactable.StopInteract(this);

            state.interactable = null;
        }

        SetFriction(false);
    }

    // ******************************************************************************
    // 3-4-12) 메서드 -> 액션 -> 부딪치기(Bump)
    //    - 특정 오브젝트에 부딪치면 캐릭터 넉백
    //    - Long Jump, Dive Attack 상태 도중 벽에 부딪치면 실행
    //    - Hip Drop 상태 도중 NPC에 부딪치면 실행
    // ******************************************************************************
    protected virtual void TryBump(Collision collision)
    {
        MainState   validType       = MainState.Jump | MainState.HipDrop | MainState.Attack;
        JumpState   validJumpType   = JumpState.Long;
        AttackState validAttackType = AttackState.Dive;

        if ((state.main == MainState.None) || !validType.HasFlag(state.main)) return;
        if (!validJumpType.HasFlag(state.jump))                               return;
        if (!validAttackType.HasFlag(state.attack))                           return;

        if ((state.attack == AttackState.Dive) && (state.sub != SubState.Start)) return;

        switch (state.main)
        {
            case MainState.HipDrop:
                {
                    if (!collision.transform.TryGetComponent(out INonPlayerCharacterController npc)) return;

                    Vector3 normal = collision.GetContact(0).normal;

                    Bump(normal);
                }
                break;

            default:
                {
                    Vector3 normal    = collision.GetContact(0).normal;
                    Vector3 velocity  = currentPosition - prevPosition;
                    Vector3 direction = Vector3.ProjectOnPlane(velocity, transform.up).normalized;
                    float   angle     = Vector3.Angle(-normal, direction);

                    if (angle < (90f - maxSlopeAngle)) Bump(normal);
                }
                break;
        }
    }

    public virtual Coroutine Bump(Vector3 normal)
    {
        StopAction();
        StopMove();

        state.main = MainState.Bump;

        var setting = this.setting.bump;

        SetHeight(setting.heightRate);
        direction.LookRotation(-normal);
        rigidbody.AddForce(setting.GetForce(direction.transform), ForceMode.VelocityChange);
        resources.Play(DamageType.Normal);
        resources.model.SetEye(EyeType.HalfClosed);
        resources.model.SetFace(FaceType.Painful);

        return action = StartCoroutine(_Bump());
    }

    protected virtual IEnumerator _Bump()
    {
        ICameraController camera = scene.camera;

        state.sub = SubState.Start;

        camera.Shake(CameraShakeType.Soft);

        yield return WaitUntilGrounded(true);

        state.sub = SubState.End;

        SetFriction(true);
        resources.model.PlayNext();
        resources.effects.land.Play(LandState.None);

        yield return new WaitForSeconds(setting.bump.duration);

        Idle();
    }

    protected virtual void StopBump()
    {
        state.main       = MainState.None;
        state.sub        = SubState.None;
        collider.enabled = true;

        SetHeight();
        SetFriction(false);
        resources.model.SetEye();
        resources.model.SetFace();
    }

    // ******************************************************************************
    // 3-4-13) 메서드 -> 액션 -> 죽기(Die)
    //    - 타입: Noraml, Bungee
    //    - 기반 기능만을 구현, Stage.PlayerController 클래스에서 확장 및 호출
    // ******************************************************************************
    public virtual Coroutine Die(DieState type)
    {
        StopAction();

        if (state.overlap[OverlapState.Immunize]) StopImmunize(true);
        if (state.overlap[OverlapState.PowerUp])  StopPowerUp(true);
        if (state.overlap[OverlapState.CoolDown]) StopCoolDown(true);
        if (state.overlap[OverlapState.Carry])    StopCarry(true);

        state.main = MainState.Die;
        state.die  = type;

        scene.Pause(true, resources.effects.elements);

        switch (type)
        {
            case DieState.Normal:
                {
                    rigidbody.isKinematic = true;

                    LookAtUnscaledTime(cameraTarget.endPoint);
                }
                break;

            case DieState.Bungee: Time.timeScale = 1f; break;
        }

        resources.Play(type);
        resources.model.SetFace(FaceType.Surprised);

        return action = StartCoroutine(_Die(type));
    }

    protected virtual IEnumerator _Die(DieState type)
    {
        foreach (var effect in resources.effects.elements) effect.SetUnscaledTime(true);

        yield return new WaitForSecondsRealtime(resources.audios.system.Play(state.die));

        gameObject.SetActive(false);
    }

    protected virtual void StopDie()
    {
        state.main            = MainState.None;
        state.die             = DieState.None;
        rigidbody.isKinematic = false;
    }

    // ******************************************************************************
    // 3-4-14) 메서드 -> 액션 -> 도착하기(Goal)
    //    - 기반 기능만을 구현, Stage.PlayerController 클래스에서 확장 및 호출
    // ******************************************************************************
    public virtual Coroutine Goal(GoalType type)
    {
        ICameraController camera = scene.camera;

        StopAction();
        StopMove();

        if (state.overlap[OverlapState.Immunize]) StopImmunize(true);
        if (state.overlap[OverlapState.PowerUp])  StopPowerUp(true);
        if (state.overlap[OverlapState.CoolDown]) StopCoolDown(true);
        if (state.overlap[OverlapState.Carry])    StopCarry(true);

        state.main = MainState.Goal;
        state.goal = type;

        scene.Pause(true, resources.effects.elements);

        Time.timeScale = 1f;

        resources.Play(MainState.Fall);
        camera.Follow(cameraTarget);

        return action = StartCoroutine(_Goal(type));
    }

    protected virtual IEnumerator _Goal(GoalType type)
    {
        ICameraController camera = scene.camera;

        while (!groundState.isGrounded) yield return new WaitForFixedUpdate();

        Time.timeScale        = 0f;
        rigidbody.isKinematic = true;

        foreach (var effect in resources.effects.elements) effect.SetUnscaledTime(true);

        LookAtUnscaledTime(cameraTarget.endPoint);
        resources.Play(type);
        resources.audios.system.Play(type);
        camera.StopFollow();
    }

    protected virtual void StopGoal()
    {
        state.main            = MainState.None;
        state.goal            = GoalType.None;
        rigidbody.isKinematic = false;

        resources.model.SetFace();
    }

    // ------------------------------------------------------------------------------
    // 3-5) 메서드 -> 오버랩(Overlap)
    //    - 타입: Immunize, Power Up, Cool Down, Carry
    //    - 별도의 딕셔너리에 각 타입값을 저장하여 다중 상태 허용
    // ------------------------------------------------------------------------------
    // ******************************************************************************
    // 3-5-1) 메서드 -> 오버랩 -> 면역(Immunize)
    //    - Damage 상태 종료 후 일정 시간동안 실행
    // ******************************************************************************
    public virtual Coroutine Immunize()
    {
        OverlapState state = OverlapState.Immunize;

        if (this.state.overlap[state]) StopImmunize(true);

        this.state.overlap[state] = true;

        return overlapActions[state] = StartCoroutine(_Immunize());
    }

    protected virtual IEnumerator _Immunize()
    {
        OverlapState state    = OverlapState.Immunize;
        float        duration = setting.overlap[state].duration;

        yield return ui.timer.Display(state, duration);

        StopImmunize();
    }

    protected virtual void StopImmunize(bool stop = false)
    {
        OverlapState state = OverlapState.Immunize;

        if (stop) StopCoroutine(overlapActions[state]);

        overlapActions[state]     = null;
        this.state.overlap[state] = false;
    }

    // ******************************************************************************
    // 3-5-2) 메서드 -> 오버랩 -> 파워업(Power Up)
    //    - 타입: Fire Flower, Super Star
    //    - 각 타입에 대한 공격 기능은 이벤트 함수와 Attack 함수 내에 구현
    //    - Fire Flower 타입으로 파워업 시 -> 지정된 Projectile 오브젝트 발사
    //    - Super Star 타입으로 파워업 시  -> 피해 면역, 적과 충돌하면 자동 공격
    // ******************************************************************************
    public virtual Coroutine PowerUp(PowerUpType state)
    {
        IBackGroundMusicController sceneBGM = scene.bgm;

        OverlapState overlapState = OverlapState.PowerUp;
        PowerUpType  prevState    = this.state.overlap.powerUp;

        if (this.state.overlap[overlapState]) StopPowerUp(true, false);

        this.state.overlap[overlapState] = true;
        this.state.overlap.powerUp       = state;

        sceneBGM.Stop();
        resources.audios.bgm.Play(state);
        resources.model.SetBody(state);

        return overlapActions[overlapState] = StartCoroutine(_PowerUp(prevState, state));
    }

    protected virtual IEnumerator _PowerUp(PowerUpType prevType, PowerUpType type)
    {
        if (prevType != type)
        {
            scene.Pause(true);

            yield return new WaitForSecondsRealtime(resources.effects.powerUp.Play(PowerUpEffectState.On) * 0.5f);

            scene.Pause(false);
        }
        else resources.effects.powerUp.Play(PowerUpEffectState.On);

        OverlapState overlapState = OverlapState.PowerUp;
        float        duration     = setting.overlap[overlapState].duration;

        yield return ui.timer.Display(overlapState, duration);

        StopPowerUp();
    }

    protected virtual void StopPowerUp(bool stop = false, bool playEffect = true)
    {
        IBackGroundMusicController sceneBGM = scene.bgm;

        OverlapState overlapState = OverlapState.PowerUp;

        if (stop) StopCoroutine(overlapActions[overlapState]);

        overlapActions[overlapState] = null;
        state.overlap[overlapState]  = false;
        state.overlap.powerUp        = PowerUpType.None;

        resources.audios.bgm.Stop();
        resources.model.SetBody();
        sceneBGM.Play();

        if (playEffect) resources.effects.powerUp.Play(PowerUpEffectState.Off);
    }

    // ******************************************************************************
    // 3-5-3) 메서드 -> 오버랩 -> 쿨타임(Cool Down)
    //    - Spin Attack, Dive Attack 상태 종료 후 일정 시간동안 실행
    //    - 다음 공격에 대한 재사용 대기 시간
    // ******************************************************************************
    public virtual Coroutine CoolDown()
    {
        OverlapState state = OverlapState.CoolDown;

        if (this.state.overlap[state]) StopCoolDown(true);

        this.state.overlap[state] = true;

        return overlapActions[state] = StartCoroutine(_CoolDown());
    }

    protected virtual IEnumerator _CoolDown()
    {
        OverlapState state    = OverlapState.CoolDown;
        float        duration = setting.overlap[state].duration;

        yield return ui.timer.Display(state, duration);

        StopCoolDown();
    }

    protected virtual void StopCoolDown(bool stop = false)
    {
        OverlapState state = OverlapState.CoolDown;

        if (stop) StopCoroutine(overlapActions[state]);

        overlapActions[state]     = null;
        this.state.overlap[state] = false;
    }

    // ******************************************************************************
    // 3-5-4) 메서드 -> 오버랩 -> 물건 나르기(Carry)
    //    - Interact(Carry Up) 상태의 시작과 동시에 Throwable(외부)에서 호출
    // ******************************************************************************
    public virtual Coroutine Carry(IThrowableBase throwable)
    {
        OverlapState state = OverlapState.Carry;

        if (this.state.overlap[state]) StopCarry(true);

        this.state.overlap[state] = true;
        this.state.throwable      = throwable;

        resources.model.SetCarryLayer(true);

        return overlapActions[state] = StartCoroutine(_Carry(throwable));
    }

    protected virtual IEnumerator _Carry(IThrowableBase throwable)
    {
        var   hands  = resources.model.bones.palms;
        float radius = (throwable as IGravityable).radius;

        while ((throwable != null) && (throwable.state == ThrowableBase.State.Carry))
        {
            Vector3 handsCenter  = Vector3.Lerp(hands[0].position, hands[1].position, 0.5f);
            Vector3 radiusAmount = transform.TransformDirection(Vector3.up * radius);

            throwable.transform.position = handsCenter + radiusAmount;
            throwable.transform.rotation = resources.model.transform.rotation;

            yield return null;
        }

        StopCarry();
    }

    protected virtual void StopCarry(bool stop = false)
    {
        OverlapState state     = OverlapState.Carry;
        var          throwable = this.state.throwable;

        if (stop) StopCoroutine(overlapActions[state]);

        overlapActions[state]     = null;
        this.state.overlap[state] = false;
        this.state.throwable      = null;

        if (throwable != null) throwable.Idle();

        resources.model.SetCarryLayer(false);
    }
}
