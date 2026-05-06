using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


using Resources   = BoomBoomController.Resources;
using ModelType   = BoomBoomController.Resources.Models.Type;
using State       = BoomBoomController.State;
using MainState   = BoomBoomController.State.Main;
using Setting     = BoomBoomController.Setting;
using SubState    = CharacterBase.State.Sub;
using MoveSetting = CharacterBase.CharacterSetting.Move;
using DamageType  = IDamageable.Type;


public interface IBoomBoomController : IBossBase
{
    #region Property

    // Component
    new Resources resources { get; }

    // State
    new State state { get; }

    // Setting
    Setting setting { get; }

    #endregion


    #region Method

    Coroutine Chase(IPlayerController player, ModelType type);
    Coroutine Brake(ModelType type);

    #endregion
}


public class BoomBoomController : BossBase, IBoomBoomController
{
    #region Definition

    public new class Resources : BossBase.Resources
    {
        #region Definition

        public class Models : Dictionary<ModelType, IBossModelBase>
        {
            #region Definition

            public enum Type { Body, Shell }

            #endregion


            #region Field

            public IBoomBoomModelController      body  { get; }
            public IBoomBoomShellModelController shell { get; }

            #endregion


            #region Constructor

            public Models(Transform transform) : base()
            {
                foreach (var model in transform.GetComponentsInChildren<IBossModelBase>(true))
                {
                    string name = model.gameObject.name.Replace(" ", string.Empty);

                    if (Enum.TryParse(name, out Type type)) Add(type, model);
                }

                body  = transform.GetComponentInChildren<IBoomBoomModelController>(true);
                shell = transform.GetComponentInChildren<IBoomBoomShellModelController>(true);
            }

            #endregion


            #region Method

            public void Set(ModelType type)
            {
                foreach (var element in this)
                {
                    ModelType _type = element.Key;
                    var       model = element.Value;

                    model.gameObject.SetActive(type == _type);
                }
            }

            #endregion
        }


        public class Effects : CharacterEffects<MainState>
        {
            #region Definition

            public class Other : Dictionary<string, IEffectController>
            {
                #region Field

                public IBoomBoomJumpEffectController jump { get; }
                public IBoomBoomLandEffectController land { get; }

                #endregion


                #region Constructor

                public Other(Transform transform) : base()
                {
                    foreach (var effect in transform.GetComponentsInChildren<IEffectController>(true))
                        Add(effect.transform.name, effect);

                    jump = transform.GetComponentInChildren<IBoomBoomJumpEffectController>(true);
                    land = transform.GetComponentInChildren<IBoomBoomLandEffectController>(true);
                }

                #endregion
            }

            #endregion


            #region Field

            public new Other other { get; }

            #endregion


            #region Constructor

            public Effects(Transform transform) : base(transform)
            {
                other = new Other(transform.Find("Other"));
            }

            #endregion
        }

        #endregion


        #region Field

        public Models                       models  { get; }
        public new IBoomBoomVoiceController voice   { get; }
        public new Effects                  effects { get; }

        #endregion


        #region Constructor

        public Resources(Transform transform) : base(transform)
        {
            models  = new Models(transform.Find("Models"));
            voice   = transform.GetComponentInChildren<IBoomBoomVoiceController>(true);
            effects = new Effects(transform.Find("Effects"));
        }

        #endregion


        #region Method

        public float Play(MainState type, SubState subType = SubState.None)
        {
            models.body.Play(type, subType);

            return Mathf.Max(voice.Play(type, subType), effects.Play(type, subType));
        }

        #endregion
    }


    public new class State : BossBase.State
    {
        #region Definition

        public new enum Main { None = 0, Idle = 1, Damage = 2, Find = 4, Die = 8, Appear = 16, Chase = 32, Brake = 64 }

        #endregion


        #region Field

        public new Main  main;
        public ModelType model;

        #endregion
    }


    [Serializable] public class Setting
    {
        #region Definition

        [Serializable] public class Find : SimpleData<ModelType, Find.Value>
        {
            #region Definition

            [Serializable] public class Value
            {
                #region Field

                [SerializeField] protected float _duration;

                public float duration { get { return _duration; } }

                #endregion


                #region Constructor

                public Value(float duration) { _duration = duration; }

                #endregion
            }

            #endregion


            #region Constructor

            public Find(SimpleData<ModelType, Value> _base) : base(_base) { }

            #endregion
        }


        [Serializable] public class Chase : SimpleData<ModelType, Chase.Value>
        {
            #region Definition

            [Serializable] public class Value
            {
                #region Field

                [SerializeField] protected SimpleData<SubState, MoveSetting> _move;

                public SimpleData<SubState, MoveSetting> move { get { return _move; } }

                #endregion


                #region Constructor

                public Value(SimpleData<SubState, MoveSetting> move) { _move = move; }

                #endregion
            }

            #endregion


            #region Field

            [SerializeField]                  protected float _duration;
            [SerializeField, Range(0f, 180f)] protected float _angle;

            public float duration { get { return _duration; } }
            public float angle    { get { return _angle; } }

            #endregion


            #region Constructor

            public Chase(SimpleData<ModelType, Value> _base, float duration, float angle) : base(_base)
            {
                _duration = duration;
                _angle    = angle;
            }

            #endregion
        }


        [Serializable] public class Brake : SimpleData<ModelType, Brake.Value>
        {
            #region Definition

            [Serializable] public class Value
            {
                #region Field

                [SerializeField] protected SimpleData<SubState, float> _durations;

                public SimpleData<SubState, float> durations { get { return _durations; } }

                #endregion


                #region Constructor

                public Value(SimpleData<SubState, float> durations) { _durations = durations; }

                #endregion
            }

            #endregion


            #region Field

            [SerializeField] protected MoveSetting _move;

            public MoveSetting move { get { return _move; } }

            #endregion


            #region Constructor

            public Brake(SimpleData<ModelType, Value> _base, MoveSetting move) : base(_base) { _move = move; }

            #endregion
        }

        #endregion


        #region Field

        [SerializeField] protected Find  _find;
        [SerializeField] protected Chase _chase;
        [SerializeField] protected Brake _brake;

        public Find  find  { get { return _find; } }
        public Chase chase { get { return _chase; } }
        public Brake brake { get { return _brake; } }

        #endregion


        #region Constructor

        public Setting(Find find, Chase chase, Brake brake)
        {
            _find  = find;
            _chase = chase;
            _brake = brake;
        }

        #endregion
    }

    #endregion


    #region Field

    public new Resources resources { get; protected set; }

    public new State state { get; protected set; } = new State();

    [SerializeField] protected Setting _setting;

    public Setting setting { get { return _setting; } }

    protected IPlayerController player;

    protected Coroutine chaseAction;

    #endregion


    #region Method

    #region Initialization

    protected override void SetField()
    {
        base.SetField();

        resources = new Resources(transform.Find("Resources"));
    }

    protected override void ResetField(Rigidbody rigidbody)
    {
        base.ResetField(rigidbody);

        _characterSetting = new CharacterSetting(new CharacterSetting.Damage(1.75f, 400f, 60f));
        _setting          = new Setting(
            new Setting.Find(
                new SimpleData<ModelType, Setting.Find.Value>(
                    new List<SimpleData<ModelType, Setting.Find.Value>.Element>()
                    {
                        new SimpleData<ModelType, Setting.Find.Value>.Element(
                            ModelType.Body, new Setting.Find.Value(1f)),
                        new SimpleData<ModelType, Setting.Find.Value>.Element(
                            ModelType.Shell, new Setting.Find.Value(2.25f))
                    })),
            new Setting.Chase(
                new SimpleData<ModelType, Setting.Chase.Value>(
                    new List<SimpleData<ModelType, Setting.Chase.Value>.Element>()
                    {
                        new SimpleData<ModelType, Setting.Chase.Value>.Element(
                            ModelType.Body, new Setting.Chase.Value(
                                new SimpleData<SubState, MoveSetting>(
                                    new List<SimpleData<SubState, MoveSetting>.Element>()
                                    {
                                        new SimpleData<SubState, MoveSetting>.Element(
                                            SubState.Loop, new MoveSetting(20f, 5f)),
                                        new SimpleData<SubState, MoveSetting>.Element(
                                            SubState.End, new MoveSetting(0f, 2.5f))
                                    }))),
                        new SimpleData<ModelType, Setting.Chase.Value>.Element(
                            ModelType.Shell, new Setting.Chase.Value(
                                new SimpleData<SubState, MoveSetting>(
                                    new List<SimpleData<SubState, MoveSetting>.Element>()
                                    {
                                        new SimpleData<SubState, MoveSetting>.Element(
                                            SubState.Loop, new MoveSetting(30f, 5f)),
                                        new SimpleData<SubState, MoveSetting>.Element(
                                            SubState.End, new MoveSetting(0f, 2.5f))
                                    })))
                    }),
                10f, 90f),
            new Setting.Brake(
                new SimpleData<ModelType, Setting.Brake.Value>(
                    new List<SimpleData<ModelType, Setting.Brake.Value>.Element>()
                    {
                        new SimpleData<ModelType, Setting.Brake.Value>.Element(
                            ModelType.Body, new Setting.Brake.Value(
                                new SimpleData<SubState, float>(
                                    new List<SimpleData<SubState, float>.Element>()
                                    {
                                        new SimpleData<SubState, float>.Element(SubState.Loop, 5f),
                                        new SimpleData<SubState, float>.Element(SubState.End,  2.25f)
                                    }))),
                        new SimpleData<ModelType, Setting.Brake.Value>.Element(
                            ModelType.Shell, new Setting.Brake.Value(
                                new SimpleData<SubState, float>(
                                    new List<SimpleData<SubState, float>.Element>()
                                    {
                                        new SimpleData<SubState, float>.Element(SubState.Loop, 0.5f),
                                        new SimpleData<SubState, float>.Element(SubState.End,  1.25f)
                                    })))
                    }),
                new MoveSetting(0f, 5f)));
    }

    #endregion


    #region Action

    public override void StopAction()
    {
        base.StopAction();

        switch (state.main)
        {
            case MainState.Chase: StopChase(); break;
            case MainState.Brake: StopBrake(); break;
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
        yield return base._Idle();

        StopIdle();
    }

    protected override void StopIdle()
    {
        base.StopIdle();

        state.main = MainState.None;
    }

    #endregion


    #region Damage

    public override bool TryDamage(Transform attacker, DamageType type)
    {
        MainState invalidType    = MainState.Damage | MainState.Die | MainState.Appear;
        ModelType validModelType = ModelType.Body;

        if (type == DamageType.Foot)                                                 return true;
        if (invalidType.HasFlag(state.main) || !validModelType.HasFlag(state.model)) return false;
        if ((state.main == MainState.Chase) && (type == DamageType.Normal))          return false;

        return base.TryDamage(attacker, type);
    }

    public override Coroutine Damage(Transform attacker, DamageType type)
    {
        StopAction();

        state.main   = MainState.Damage;
        state.damage = type;

        return base.Damage(attacker, type);
    }

    protected override IEnumerator _Damage(Transform attacker, DamageType type)
    {
        switch (type)
        {
            case DamageType.Normal:
                {
                    yield return WaitUntilGrounded(true);

                    StopMove();
                    resources.effects.other.land.Play();

                    Vector3 force = characterSetting.damage.GetForce(direction.transform) * 0.5f;

                    rigidbody.AddForce(force, ForceMode.VelocityChange);

                    yield return WaitUntilGrounded(true);

                    resources.effects.other.land.Play();
                }
                break;
        }

        SetFriction(true);

        yield return base._Damage(attacker, type);

        Find(null);
    }

    protected override void StopDamage()
    {
        base.StopDamage();

        state.main       = MainState.None;
        state.damage     = DamageType.None;
        collider.enabled = true;

        SetFriction(false);
    }

    #endregion


    #region Find

    public override Coroutine Find(IPlayerController player)
    {
        StopAction();

        state.main = MainState.Find;

        if (player != null) this.player = player;
        else                state.model = ModelType.Shell;

        resources.Play(state.main);

        return base.Find(this.player);
    }

    protected override IEnumerator _Find(IPlayerController player)
    {
        yield return LookAt(player, setting.find[state.model].duration);

        Chase(player, state.model);
    }

    protected override void StopFind()
    {
        base.StopFind();

        state.main  = MainState.None;
        state.model = ModelType.Body;
    }

    #endregion


    #region Die

    public override Coroutine Die()
    {
        StopAction();

        state.main = MainState.Die;

        return base.Die();
    }

    protected override void StopDie()
    {
        base.StopDie();

        state.main = MainState.None;
    }

    #endregion


    #region Appear

    public override Coroutine Appear()
    {
        gameObject.SetActive(true);

        state.main = MainState.Appear;

        return base.Appear();
    }

    protected override void StopAppear()
    {
        base.StopAppear();

        state.main = MainState.None;
    }

    #endregion


    #region Chase

    public virtual Coroutine Chase(IPlayerController player, ModelType type)
    {
        StopAction();

        state.main  = MainState.Chase;
        state.model = type;

        if (type == ModelType.Body) resources.Play(state.main);
        else
        {
            resources.models.Set(state.model);
            resources.effects[state.main].Play();
        }

        return action = StartCoroutine(_Chase(player, type));
    }

    protected virtual IEnumerator _Chase(IPlayerController player, ModelType type)
    {
        chaseAction = StartCoroutine(ChaseLoop(player.transform, type));

        yield return new WaitForSeconds(setting.chase.duration);

        Brake(type);
    }

    protected virtual IEnumerator ChaseLoop(Transform player, ModelType type)
    {
        state.sub = SubState.Loop;

        direction.LookAt(player);

        var   setting = this.setting.chase;
        float angle   = Vector3.Angle(direction.transform.forward, direction.GetDirection(player));

        while (angle <= setting.angle)
        {
            direction.LookAtSmooth(player);
            Move(setting[state.model].move[state.sub]);

            yield return new WaitForFixedUpdate();

            angle = Vector3.Angle(direction.transform.forward, direction.GetDirection(player));
        }

        state.sub = SubState.End;

        while (moveSpeed > 0f)
        {
            Move(setting[state.model].move[state.sub]);

            yield return new WaitForFixedUpdate();
        }

        yield return new WaitForFixedUpdate();

        chaseAction = StartCoroutine(ChaseLoop(player, type));
    }

    protected virtual void StopChase()
    {
        if (chaseAction != null)
        {
            StopCoroutine(chaseAction);
            chaseAction = null;
        }

        state.main  = MainState.None;
        state.sub   = SubState.None;
        state.model = ModelType.Body;

        resources.models.Set(ModelType.Body);
        resources.effects[MainState.Chase].Stop();
    }

    #endregion


    #region Brake

    public virtual Coroutine Brake(ModelType type)
    {
        StopAction();

        state.main  = MainState.Brake;
        state.model = type;

        resources.Play(state.main);

        return action = StartCoroutine(_Brake(type));
    }

    protected virtual IEnumerator _Brake(ModelType type)
    {
        state.sub = SubState.Start;

        var setting = this.setting.brake;

        while (moveSpeed > 0f)
        {
            Move(setting.move);

            yield return new WaitForFixedUpdate();
        }

        state.sub = SubState.Loop;

        SetFriction(true);

        yield return new WaitForSeconds(setting[state.model].durations[state.sub]);

        state.sub = SubState.End;

        resources.Play(state.main, state.sub);

        yield return new WaitForSeconds(setting[state.model].durations[state.sub]);

        Idle();
    }

    protected virtual void StopBrake()
    {
        state.main  = MainState.None;
        state.sub   = SubState.None;
        state.model = ModelType.Body;

        SetFriction(false);
    }

    #endregion

    #endregion

    #endregion
}
