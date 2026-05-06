using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Resources      = BobombController.Resources;
using State          = BobombController.State;
using MainState      = BobombController.State.Main;
using Setting        = BobombController.Setting;
using SubState       = CharacterBase.State.Sub;
using MoveSetting    = CharacterBase.CharacterSetting.Move;
using DamageType     = IDamageable.Type;
using ThrowableState = ThrowableBase.State;


public interface IBobombController : IEnemyBase
{
    #region Property

    // Component
    new Resources   resources { get; }
    IBombController bomb      { get; }

    // State
    new State state { get; }

    // Setting
    Setting setting { get; }

    #endregion


    #region Method

    Coroutine Walk();
    Coroutine Chase(IPlayerController player);

    #endregion
}


public class BobombController : EnemyBase, IBobombController
{
    #region Definition

    public new class Resources : EnemyBase.Resources
    {
        #region Field

        public new IBobombModelController      model   { get; }
        public new CharacterEffects<MainState> effects { get; }

        #endregion


        #region Constructor

        public Resources(Transform transform) : base(transform)
        {
            model   = transform.GetComponentInChildren<IBobombModelController>(true);
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

        public new enum Main { None = 0, Idle = 1, Damage = 2, Find = 4, Die = 8, Walk = 16, Chase = 32 }

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

            public Walk(List<Element> elements, float duration) : base(elements) { _duration = duration; }

            #endregion
        }


        [Serializable] public class Chase
        {
            #region Field

            [SerializeField] protected MoveSetting _move;

            public MoveSetting move { get { return _move; } }

            #endregion


            #region Constructor

            public Chase(MoveSetting move) { _move = move; }

            #endregion
        }

        #endregion


        #region Field

        [SerializeField] protected Idle  _idle;
        [SerializeField] protected Walk  _walk;
        [SerializeField] protected Chase _chase;

        public Idle  idle  { get { return _idle; } }
        public Walk  walk  { get { return _walk; } }
        public Chase chase { get { return _chase; } }

        #endregion


        #region Constructor

        public Setting(Idle idle, Walk walk, Chase chase)
        {
            _idle  = idle;
            _walk  = walk;
            _chase = chase;
        }

        #endregion
    }

    #endregion


    #region Field

    public new Resources   resources { get; protected set; }
    public IBombController bomb      { get; protected set; }

    public new State state { get; protected set; } = new State();

    [SerializeField] protected Setting _setting;

    public Setting setting { get { return _setting; } }

    #endregion


    #region Method

    #region Event

    protected override void OnCollisionStay(Collision collision) { }

    #endregion


    #region Initialization

    protected override void SetField()
    {
        base.SetField();

        resources = new Resources(transform.Find("Resources"));
        bomb      = Instantiate(enemySetting.die.drop, transform.position, transform.rotation, transform)
            .GetComponent<IBombController>();
    }

    protected override void ResetField(Rigidbody rigidbody)
    {
        base.ResetField(rigidbody);

        _mask             = DamageType.Normal | DamageType.Explode;
        _characterSetting = new CharacterSetting(new CharacterSetting.Damage(0.75f, 0f, 0f));
        _setting          = new Setting(
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
            new Setting.Chase(new MoveSetting(7.5f, 5f)));
    }

    #endregion


    #region Action

    public override void StopAction()
    {
        base.StopAction();

        switch (state.main)
        {
            case MainState.Walk:  StopWalk();  break;
            case MainState.Chase: StopChase(); break;
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

    public override bool TryDamage(Transform attacker, DamageType type)
    {
        if (type == DamageType.Explode) return Explode();

        return base.TryDamage(attacker, type);
    }

    public override Coroutine Damage(Transform attacker, DamageType type)
    {
        StopAction();

        state.main   = MainState.Damage;
        state.damage = type;

        SetFriction(true);
        bomb.StopSetTimer();

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

    protected virtual bool Explode()
    {
        if (bomb.state != ThrowableState.Destroy) bomb.Destroy();
        if (spawner    != null)                   spawner.SpawnEnemy();

        Destroy(gameObject);

        return true;
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
        bomb.SetTimer();

        return action = StartCoroutine(_Chase(player));
    }

    protected virtual IEnumerator _Chase(IPlayerController player)
    {
        var setting = this.setting.chase;

        while (true)
        {
            direction.LookAtSmooth(player.transform);
            Move(setting.move);
            resources.model.SetMoveRate(moveSpeed / setting.move.speed);

            yield return new WaitForFixedUpdate();
        }
    }

    protected virtual void StopChase() { state.main = MainState.None; }

    #endregion

    #endregion

    #endregion
}
