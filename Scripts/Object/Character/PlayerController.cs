/////////////////////////////////////////////////////////////////////////////////
// * 목차
//    1. 인터페이스 정의 ... Line 36
//    2. 클래스 정의 ....... Line 80
/////////////////////////////////////////////////////////////////////////////////
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

/////////////////////////////////////////////////////////////////////////////////
// 1. 인터페이스 정의
//  - ICharacterBase 인터페이스 상속
/////////////////////////////////////////////////////////////////////////////////
public interface IPlayerController : ICharacterBase
{
    // ==============================================================================
    // 1) 프로퍼티
    // ==============================================================================
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
    
    // ==============================================================================
    // 2) 메서드
    // ==============================================================================
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

/////////////////////////////////////////////////////////////////////////////////
// 2. 클래스 정의
//  - CharacterBase 클래스 상속
/////////////////////////////////////////////////////////////////////////////////
public class PlayerController : CharacterBase, IPlayerController
{
    // ==============================================================================
    // 1) 사전 정의
    // ==============================================================================
    // ------------------------------------------------------------------------------
    // 1_ UI 참조
    //    - 상호작용 / 타이머 / 대사창 출력
    //    - Mobile(Android) 플랫폼 빌드 시 가상패드 출력
    // ------------------------------------------------------------------------------
    public class UI : List<IUIBase>
    {
        public Canvas                             canvas          { get; }
        public IPlayerInteractableUIController    interactable    { get; }
        public IPlayerTimerUIController           timer           { get; }
        public IPlayerConversationUIController    conversation    { get; }
        public IPlayerVirtualJoystickUIController virtualJoystick { get; }
        
        public UI(Transform transform) : base(transform.GetComponentsInChildren<IUIBase>(true))
        {
            canvas          = transform.GetComponent<Canvas>();
            interactable    = transform.GetComponentInChildren<IPlayerInteractableUIController>(true);
            timer           = transform.GetComponentInChildren<IPlayerTimerUIController>(true);
            conversation    = transform.GetComponentInChildren<IPlayerConversationUIController>(true);
            virtualJoystick = transform.GetComponentInChildren<IPlayerVirtualJoystickUIController>(true);
        }
        
        public void Initialize() { foreach (var element in this) element.gameObject.SetActive(false); }

        public void Hide(bool paused)
        {
            if (interactable.gameObject.activeInHierarchy)       interactable.root.main.gameObject.SetActive(!paused);
            if (timer.gameObject.activeInHierarchy)              timer.root.main.gameObject.SetActive(!paused);
            if (Application.platform == RuntimePlatform.Android) virtualJoystick.Display(!paused);
        }
    }

    // ------------------------------------------------------------------------------
    // 2_ 리소스 참조
    //    - CharacterBase.Resources 클래스를 상속하여 새로 정의하고, 부모 클래스를 대체
    // ------------------------------------------------------------------------------
    public new class Resources : CharacterBase.Resources
    {
        // Definition
        public class Effects : CharacterEffects<MainState>
        {
            public class HipDrop : Value
            {
                public IPlayerHipDropEffectController loop { get; }

                public HipDrop(Transform transform) : base(transform)
                {
                    loop = transform.GetComponentInChildren<IPlayerHipDropEffectController>(true);
                }
            }

            public class Attack : Details<AttackState>
            {
                public IPlayerSpinEffectController spin { get; }

                public Attack(Transform transform) : base(transform)
                {
                    spin = transform.GetComponentInChildren<IPlayerSpinEffectController>(true);
                }
            }

            public class Interact : Details<InteractState>
            {
                public class Transport : Details<TransporterType>
                {
                    public class Launcher : Details<LauncherState>
                    {
                        public IPlayerLauncherEffectController launch { get; }

                        public Launcher(Transform transform) : base(transform)
                        {
                            launch = transform.GetComponentInChildren<IPlayerLauncherEffectController>(true);
                        }
                    }

                    public Launcher launcher { get; }

                    public Transport(Transform transform) : base(transform)
                    {
                        launcher = new Launcher(transform.Find("Launcher"));
                    }
                }

                public Transport transport { get; }

                public Interact(Transform transform) : base(transform)
                {
                    transport = new Transport(transform.Find("Transport"));
                }
            }

            public class Goal : Value
            {
                #region Field

                public IPlayerGoalEffectController end { get; }

                public Goal(Transform transform) : base(transform)
                {
                    end = transform.GetComponentInChildren<IPlayerGoalEffectController>(true);
                }
            }

            public class Other : Dictionary<string, IEffectController>
            {
                public IPlayerFootEffectController  foot      { get; }
                public IPlayerSmokeEffectController smokeLand { get; }
                public IPlayerSmokeEffectController smokeJump { get; }

                public Other(Transform transform) : base()
                {
                    foreach (var effect in transform.GetComponentsInChildren<IEffectController>(true))
                        Add(effect.transform.name, effect);

                    foot      = transform.GetComponentInChildren<IPlayerFootEffectController>(true);
                    smokeLand = transform.Find("Smoke Land").GetComponent<IPlayerSmokeEffectController>();
                    smokeJump = transform.Find("Smoke Jump").GetComponent<IPlayerSmokeEffectController>();
                }
            }
            
            public IPlayerLandEffectController    land     { get; }
            public IPlayerJumpEffectController    jump     { get; }
            public HipDrop                        hipDrop  { get; }
            public Attack                         attack   { get; }
            public Interact                       interact { get; }
            public Details<DieState>              die      { get; }
            public Goal                           goal     { get; }
            public IPlayerPowerUpEffectController powerUp  { get; }
            public new Other                      other    { get; }

            public Effects(Transform transform) : base(transform)
            {
                land     = transform.GetComponentInChildren<IPlayerLandEffectController>(true);
                jump     = transform.GetComponentInChildren<IPlayerJumpEffectController>(true);
                hipDrop  = new HipDrop(transform.Find("Hip Drop"));
                attack   = new Attack(transform.Find("Attack"));         Remove(MainState.Attack);
                interact = new Interact(transform.Find("Interact"));     Remove(MainState.Interact);
                die      = new Details<DieState>(transform.Find("Die")); Remove(MainState.Die);
                goal     = new Goal(transform.Find("Goal"));
                powerUp  = transform.GetComponentInChildren<IPlayerPowerUpEffectController>(true);
                other    = new Other(transform.Find("Other"));
            }
        }

        public class Audios : List<IAudioBase>
        {
            public IPlayerBackGroundMusicController bgm    { get; }
            public IPlayerSystemAudioController     system { get; }

            public Audios(Transform transform) : base(transform.GetComponentsInChildren<IAudioBase>(true))
            {
                bgm    = transform.GetComponentInChildren<IPlayerBackGroundMusicController>(true);
                system = transform.GetComponentInChildren<IPlayerSystemAudioController>(true);
            }
        }
        
        // Field
        public new IPlayerModelController model   { get; }
        public new IPlayerVoiceController voice   { get; }
        public new Effects                effects { get; }
        public Audios                     audios  { get; }

        // Method
        public Resources(Transform transform) : base(transform)
        {
            model   = transform.GetComponentInChildren<IPlayerModelController>(true);
            voice   = transform.GetComponentInChildren<IPlayerVoiceController>(true);
            effects = new Effects(transform.Find("Effects"));
            audios  = new Audios(transform.Find("Audios"));
        }
        
        public float Play(MainState type, SubState subType = SubState.None)
        {
            model.Play(type, subType);

            return Mathf.Max(voice.Play(type, subType), effects.Play(type, subType));
        }

        public float Play(LandState type, SubState subType = SubState.None)
        {
            model.Play(type, subType);

            return Mathf.Max(voice.Play(MainState.Land, subType), effects.land.Play(type));
        }

        public float Play(JumpState type, SubState subType = SubState.None)
        {
            model.Play(type, subType);

            return Mathf.Max(voice.Play(type, subType), effects.jump.Play(type));
        }

        public float Play(AttackState type, SubState subType = SubState.None)
        {
            model.Play(type, subType);

            return Mathf.Max(voice.Play(MainState.Attack, subType), effects.attack.Play(type, subType));
        }

        public float Play(InteractState type, SubState subType = SubState.None)
        {
            model.Play(type, subType);

            return Mathf.Max(voice.Play(type, subType), effects.interact.Play(type, subType));
        }

        public float Play(TransporterType type, SubState subType = SubState.None)
        {
            model.Play(type, subType);

            return Mathf.Max(voice.Play(type, subType), effects.interact.transport.Play(type, subType));
        }

        public float Play(DieState type, SubState subType = SubState.None)
        {
            model.Play(type, subType);

            return Mathf.Max(voice.Play(type, subType), effects.die.Play(type, subType));
        }

        public float Play(GoalType type, SubState subType = SubState.None)
        {
            model.Play(type, subType);

            return Mathf.Max(voice.Play(type, subType), effects.goal.Play(subType));
        }
    }

    // ------------------------------------------------------------------------------
    // 3_ 캐릭터 상태
    //    - CharacterBase.State 클래스를 상속하여 새로 정의하고, 부모 클래스를 대체
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
    // 4_ 캐릭터 설정
    //    - 부모(CharacterBase.Setting) 클래스는 그대로 사용
    //    - 별도의 추가 클래스 정의
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

    // Action
    protected Dictionary<OverlapState, Coroutine> overlapActions;
    protected Coroutine                           jumpKeepAction;
    protected Coroutine                           interactableSetAction;

    protected IInteractable interactable, tempInteractable;

    protected Vector3 prevPosition, currentPosition;

    // ==============================================================================
    // 3) 메서드
    //    - 
    // ==============================================================================
    // ------------------------------------------------------------------------------
    // 1_ 이벤트 (Unity 호출 함수)
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
    // 2_ 초기화
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

    #endregion


    #region Set

    public override Coroutine Set(ICharacterTargetController target, bool resetDirection = false, bool setIdle = false, float duration = 0)
    {
        if (resetDirection) cameraTarget.Initialize();

        return base.Set(target, resetDirection, setIdle, duration);
    }

    public virtual void SetHitPoint(int amount)
    {
        state.hitPoint = Mathf.Clamp(state.hitPoint + amount, 0, setting.maxHitPoint);
    }

    #endregion


    #region Action

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


    #region Idle

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

    #endregion


    #region Damage

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

        int amount = ((type == DamageType.PressDown) ? setting.maxHitPoint : 1) * -1;

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
            Idle(type == DamageType.PressDown);
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

    #endregion


    #region Fall

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

    #endregion


    #region Land

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

    #endregion


    #region Run

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

            RotateAndMove(setting.move);
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

    #endregion


    #region Brake

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

    #endregion


    #region Crouch

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

    #endregion


    #region Jump

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

    #endregion


    #region Hip Drop

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

    #endregion


    #region Attack

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

    #endregion


    #region Interact

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

    #endregion


    #region Bump

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

    #endregion


    #region Die

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

    #endregion


    #region Goal

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

    #endregion

    #endregion


    #region Overlap

    #region Immunize

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

    #endregion


    #region Power Up

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

    #endregion


    #region Cool Down

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

    #endregion


    #region Carry

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

    #endregion

    #endregion


    //#region Climb Down Function

    //private void ClimbDown(Collision collision)
    //{
    //    var hitNormal = collision.contacts[0].normal;
    //    var angle = Vector3.Angle(transform.up, hitNormal);

    //    if ((angle >= 35f) && (angle <= 90f) && (!isGrounded || (isGrounded && !subIsGrounded)))
    //    {
    //        if (!isClimbingDown)
    //        {
    //            if (isFalling) StopFall();
    //            if (isMoving) StopMove();
    //            if (isJumping) StopJump();
    //            if (isDiving) StopDive();
    //            if (isSpinning) StopSpin();

    //            isClimbingDown = true;

    //            _rigidbody.velocity = Vector3.zero;

    //            mesh.SetAnimation("Climb Down");

    //            Quaternion lookRotation = Quaternion.LookRotation(hitNormal, transform.up);

    //            mesh.transform.rotation = lookRotation;
    //        }
    //        else
    //        {
    //            float forceRate = (angle >= 75f) ? 0.9f : 0f;

    //            _rigidbody.AddForce(-transform.up * forceRate, ForceMode.Acceleration);

    //            Quaternion lookRotation = Quaternion.LookRotation(hitNormal, transform.up);

    //            mesh.transform.rotation = lookRotation;
    //        }
    //    }
    //    else
    //    {
    //        if (isClimbingDown)
    //        {
    //            isClimbingDown = false;

    //            Vector3 _direction = Vector3.ProjectOnPlane(mesh.transform.forward, transform.up);

    //            Quaternion lookRotation = Quaternion.LookRotation(_direction, transform.up);

    //            mesh.transform.rotation = lookRotation;

    //            if (!isJumping) StartLand(LandType.Light);
    //        }
    //    }
    //}

    //private void StopClimbDown()
    //{
    //    if (isClimbingDown)
    //    {
    //        isClimbingDown = false;

    //        Vector3 _direction = Vector3.ProjectOnPlane(mesh.transform.forward, transform.up);
    //        Quaternion lookRotation = Quaternion.LookRotation(_direction, transform.up);

    //        mesh.transform.rotation = lookRotation;

    //        if (!isJumping) StartLand(LandType.Light);
    //    }
    //}

    //#endregion


    //#region Debug Function

    //private void OnGUI()
    //{
    //    PrintVelocity();
    //    //PrintState();
    //}

    //private void PrintVelocity()
    //{
    //    GUIStyle boxStyle = new GUIStyle(GUI.skin.box);
    //    GUIStyle contentStyle = new GUIStyle(GUI.skin.label);

    //    boxStyle.fontSize = 50;
    //    contentStyle.fontSize = 50;

    //    GUI.Box(new Rect(10, 10, 950, 420), "Velocity", boxStyle);
    //    GUI.Label(new Rect(10, 70, 950, 70), " World Velocity\t: " + worldVelocity, contentStyle);
    //    GUI.Label(new Rect(10, 140, 950, 70), " Local Velocity\t: " + localVelocity, contentStyle);
    //    GUI.Label(new Rect(10, 210, 950, 70), " Move Direction\t: " + localMoveDirection, contentStyle);
    //    GUI.Label(new Rect(10, 280, 950, 70), " Move Speed\t: " + localMoveSpeed, contentStyle);
    //    GUI.Label(new Rect(10, 350, 950, 70), " Fall Velocity\t: " + localFallVelocity, contentStyle);
    //    GUI.Label(new Rect(10f, 420, 950, 70), localMoveVelocity.ToString(), contentStyle);
    //}

    //private void PrintState()
    //{
    //    GUIStyle boxStyle = new GUIStyle(GUI.skin.box);
    //    GUIStyle contentStyle = new GUIStyle(GUI.skin.label);

    //    boxStyle.fontSize = 50;
    //    contentStyle.fontSize = 50;

    //    GUI.Box(new Rect(10, 0, 500, 960), "State", boxStyle);
    //    GUI.Label(new Rect(10, 60, 500, 50), "Move\t: " + isMoving, contentStyle);
    //    GUI.Label(new Rect(10, 120, 500, 50), "Carry\t: " + isCarring, contentStyle);
    //    GUI.Label(new Rect(10, 180, 500, 50), "PowerUp\t: " + isPowerUpping, contentStyle);
    //    //GUI.Label(new Rect(10, 240, 500, 50), "Change\t: " + isChangingTargetSpeed, contentStyle);
    //    GUI.Label(new Rect(10, 300, 500, 50), "Run\t: " + isRunning, contentStyle);
    //    GUI.Label(new Rect(10, 360, 500, 50), "Fall\t: " + isFalling, contentStyle);
    //    GUI.Label(new Rect(10, 420, 500, 50), "Land\t: " + isLanding, contentStyle);
    //    GUI.Label(new Rect(10, 480, 500, 50), "Brake\t: " + isBraking, contentStyle);
    //    GUI.Label(new Rect(10, 540, 500, 50), "Jump\t: " + isJumping, contentStyle);
    //    GUI.Label(new Rect(10, 600, 500, 50), "Crouch\t: " + isCrouching, contentStyle);
    //    GUI.Label(new Rect(10, 660, 500, 50), "HipDrop\t: " + isHipDropping, contentStyle);
    //    GUI.Label(new Rect(10, 720, 500, 50), "Attack\t: " + isAttacking, contentStyle);
    //    GUI.Label(new Rect(10, 780, 500, 50), "Interact\t: " + isInteracting, contentStyle);
    //    GUI.Label(new Rect(10, 840, 500, 50), "Damage\t: " + isGettingDamage, contentStyle);
    //    GUI.Label(new Rect(10, 900, 500, 50), "Die\t: " + isDying, contentStyle);
    //}

    //#endregion

    #endregion
}
