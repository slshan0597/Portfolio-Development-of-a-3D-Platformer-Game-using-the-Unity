using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Resources   = GoombaController.Resources;
using State       = GoombaController.State;
using MainState   = GoombaController.State.Main;
using Setting     = GoombaController.Setting;
using SubState    = CharacterBase.State.Sub;
using MoveSetting = CharacterBase.CharacterSetting.Move;
using DamageType  = IDamageable.Type;


public interface IGoombaController : IEnemyBase
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

    Coroutine Walk();
    Coroutine Chase(IPlayerController player);
    Coroutine Brake();
    Coroutine Attack(IDamageable target);

    #endregion
}


public class GoombaController : EnemyBase, IGoombaController
{
    #region Definition

    public new class Resources : EnemyBase.Resources
    {
        #region Field

        public new IGoombaModelController      model   { get; }
        public new CharacterEffects<MainState> effects { get; }

        #endregion


        #region Constructor

        public Resources(Transform transform) : base(transform)
        {
            model   = transform.GetComponentInChildren<IGoombaModelController>(true);
            effects = new CharacterEffects<MainState>(transform.Find("Effects"));
        }

        #endregion


        #region Method

        public float Play(MainState type, SubState subType = SubState.None)
        {
            model.Play(type, subType);

            return effects.Play(type, subType);
        }

        #endregion
    }


    public new class State : EnemyBase.State
    {
        #region Definition

        public new enum Main 
        {
            None = 0, Idle = 1, Damage = 2, Find = 4, Die = 8, Walk = 16, Chase = 32, Brake = 64, Attack = 128
        }

        #endregion


        #region Field

        public new Main main;

        #endregion
    }


    [Serializable] public class Setting
    {
        #region Definition

        [Serializable] public class Idle
        {
            #region Field

            [SerializeField] protected float _duration;

            public float duration { get { return _duration; } }

            #endregion


            #region Constructor

            public Idle(float duration) { _duration = duration; }

            #endregion
        }


        [Serializable] public class Walk : SimpleData<SubState, Walk.Value>
        {
            #region Definition

            [Serializable] public class Value
            {
                #region Field

                [SerializeField] protected MoveSetting _move;

                public MoveSetting move { get { return _move; } }

                #endregion


                #region Constructor

                public Value(MoveSetting move) { _move = move; }

                #endregion
            }

            #endregion


            #region Field

            [SerializeField] protected float _duration;

            public float duration { get { return _duration; } }

            #endregion


            #region Constructor

            public Walk(List<Element> elements, float duration) : base(elements) {  _duration = duration; }

            #endregion
        }


        [Serializable] public class Chase
        {
            #region Field

            [SerializeField]                  protected MoveSetting _move;
            [SerializeField, Range(0f, 180f)] protected float       _angle;
            [SerializeField, Range(1f, 10f)]  protected float       _triggerRadiusRate;

            public MoveSetting move              { get { return _move; } }
            public float       angle             { get { return _angle; } }
            public float       triggerRadiusRate { get { return _triggerRadiusRate; } }

            #endregion


            #region Constructor

            public Chase(MoveSetting move, float angle, float triggerRadiusRate) 
            {
                _move              = move;
                _angle             = angle;
                _triggerRadiusRate = triggerRadiusRate;
            }

            #endregion
        }


        [Serializable] public class Brake
        {
            #region Field

            [SerializeField] protected MoveSetting _move;
            [SerializeField] protected float       _duration;

            public MoveSetting move     { get { return _move; } }
            public float       duration { get { return _duration; } }

            #endregion


            #region Constructor

            public Brake(MoveSetting move, float duration) 
            {
                _move     = move;
                _duration = duration;
            }

            #endregion
        }


        [Serializable] public class Attack
        {
            #region Field

            [SerializeField] protected float _duration;

            public float duration { get { return _duration; } }

            #endregion


            #region Constructor

            public Attack(float duration) { _duration = duration; }

            #endregion
        }

        #endregion


        #region Field

        [SerializeField] protected Idle   _idle;
        [SerializeField] protected Walk   _walk;
        [SerializeField] protected Chase  _chase;
        [SerializeField] protected Brake  _brake;
        [SerializeField] protected Attack _attack;

        public Idle   idle   { get { return _idle; } }
        public Walk   walk   { get { return _walk; } }
        public Chase  chase  { get { return _chase; } }
        public Brake  brake  { get { return _brake; } }
        public Attack attack { get { return _attack; } }

        #endregion


        #region Constructor

        public Setting(Idle idle, Walk walk, Chase chase, Brake brake, Attack attack)
        {
            _idle   = idle;
            _walk   = walk;
            _chase  = chase;
            _brake  = brake;
            _attack = attack;
        }

        #endregion
    }

    #endregion


    #region Field

    public new Resources resources { get; protected set; }

    public new State state { get; protected set; } = new State();

    [SerializeField] protected Setting _setting;

    public Setting setting { get { return _setting; } }

    #endregion


    #region Method

    #region Event

    protected override void OnCollisionStay(Collision collision)
    {
        var invalidType = MainState.Damage | MainState.Die;

        if ((state.main != MainState.None) && invalidType.HasFlag(state.main))  return;
        if (!collision.transform.TryGetComponent(out IPlayerController target)) return;

        if (state.main == MainState.Chase) Attack((IDamageable)target);
        else                               ((IDamageable)target).TryDamage(transform, DamageType.Normal);
    }

    #endregion


    #region Initialization

    protected override void SetField()
    {
        base.SetField();

        resources = new Resources(transform.Find("Resources"));
    }

    protected override void ResetField(Rigidbody rigidbody)
    {
        base.ResetField(rigidbody);

        _setting = new Setting(
            new Setting.Idle(5f),
            new Setting.Walk(
                new List<SimpleData<SubState, Setting.Walk.Value>.Element>()
                {
                    new SimpleData<SubState, Setting.Walk.Value>.Element(
                        SubState.Loop, new Setting.Walk.Value(new MoveSetting(1f, 5f))),
                    new SimpleData<SubState, Setting.Walk.Value>.Element(
                        SubState.End, new Setting.Walk.Value(new MoveSetting(0f, 5f)))
                },
                5f),
            new Setting.Chase(new MoveSetting(7.5f, 5f), 90f, 4f),
            new Setting.Brake(new MoveSetting(0f,   4f), 1f),
            new Setting.Attack(2f));
    }

    #endregion


    #region Action

    public override void StopAction()
    {
        base.StopAction();

        switch (state.main)
        {
            case MainState.Walk:   StopWalk();   break;
            case MainState.Chase:  StopChase();  break;
            case MainState.Brake:  StopBrake();  break;
            case MainState.Attack: StopAttack(); break;
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
        yield return new WaitForSeconds(setting.idle.duration);

        if (planet.enabled) Walk();
        else                StopIdle();
    }

    protected override void StopIdle()
    {
        base.StopIdle();

        state.main = MainState.None;
    }

    #endregion


    #region Damage

    public override Coroutine Damage(Transform attacker, DamageType type)
    {
        StopAction();

        state.main   = MainState.Damage;
        state.damage = type;

        return base.Damage(attacker, type);
    }

    protected override void StopDamage()
    {
        base.StopDamage();

        state.main   = MainState.None;
        state.damage = DamageType.None;
    }

    #endregion


    #region Find

    public override Coroutine Find(IPlayerController player)
    {
        StopAction();

        state.main = MainState.Find;

        return base.Find(player);
    }

    protected override IEnumerator _Find(IPlayerController player)
    {
        yield return base._Find(player);

        Chase(player);
    }

    protected override void StopFind()
    {
        base.StopFind();

        state.main = MainState.None;
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


    #region Walk

    public virtual Coroutine Walk()
    {
        StopAction();

        state.main                        = MainState.Walk;
        trigger.enabled                   = true;
        direction.transform.localRotation = Quaternion.Euler(Vector3.up * UnityEngine.Random.Range(0f, 360f));

        resources.Play(state.main);

        return action = StartCoroutine(_Walk());
    }

    protected virtual IEnumerator _Walk()
    {
        state.sub = SubState.Loop;

        var   setting     = this.setting.walk;
        float elapsedTime = 0f;

        while (elapsedTime < setting.duration)
        {
            Move(setting[state.sub].move);
            resources.model.SetMoveRate(moveSpeed / setting[SubState.Loop].move.speed);

            elapsedTime += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }

        state.sub = SubState.End;

        while (moveSpeed > 0f)
        {
            Move(setting[state.sub].move);
            resources.model.SetMoveRate(moveSpeed / setting[SubState.Loop].move.speed);

            yield return new WaitForFixedUpdate();
        }

        resources.model.PlayNext();
        Idle();
    }

    protected virtual void StopWalk()
    { 
        state.main      = MainState.None;
        state.sub       = SubState.None;
        trigger.enabled = false;
    }

    #endregion


    #region Chase

    public virtual Coroutine Chase(IPlayerController player)
    {
        StopAction();

        state.main = MainState.Chase;

        resources.Play(state.main);

        return action = StartCoroutine(_Chase(player));
    }

    protected virtual IEnumerator _Chase(IPlayerController player)
    {
        var   setting  = this.setting.chase;
        var   _player  = player.transform;
        float angle    = Vector3.Angle(direction.transform.forward, direction.GetDirection(_player));
        float distance = (_player.position - transform.position).magnitude;

        while ((angle <= setting.angle) && (distance <= (trigger.radius * setting.triggerRadiusRate)))
        {
            direction.LookAtSmooth(_player);
            Move(setting.move);
            resources.model.SetMoveRate(moveSpeed / setting.move.speed);

            yield return new WaitForFixedUpdate();

            angle    = Vector3.Angle(direction.transform.forward, direction.GetDirection(_player));
            distance = (_player.position - transform.position).magnitude;
        }

        Brake();
    }

    protected virtual void StopChase() { state.main = MainState.None; }

    #endregion


    #region Brake

    public Coroutine Brake()
    {
        StopAction();

        state.main = MainState.Brake;

        resources.Play(state.main);

        return action = StartCoroutine(_Brake());
    }

    protected virtual IEnumerator _Brake()
    {
        var setting = this.setting.brake;

        while (moveSpeed > 0f)
        {
            Move(setting.move);

            yield return new WaitForFixedUpdate();
        }

        resources.model.PlayNext();
        resources.effects[state.main].Stop(setting.duration);

        yield return new WaitForSeconds(setting.duration);

        Idle();
    }

    protected virtual void StopBrake()
    {
        state.main = MainState.None;

        resources.effects[MainState.Brake].Stop(setting.brake.duration);
    }

    #endregion


    #region Attack

    public virtual Coroutine Attack(IDamageable target)
    {
        StopAction();
        StopMove();

        state.main = MainState.Attack;

        SetFriction(true);
        direction.LookAt(target.transform);
        resources.Play(state.main);
        target.TryDamage(transform, DamageType.Normal);

        return action = StartCoroutine(_Attack());
    }

    protected virtual IEnumerator _Attack()
    {
        yield return new WaitForSeconds(setting.attack.duration);

        Idle();
    }

    protected virtual void StopAttack()
    { 
        state.main = MainState.None;

        SetFriction(false);
    }

    #endregion

    #endregion

    #endregion
}
