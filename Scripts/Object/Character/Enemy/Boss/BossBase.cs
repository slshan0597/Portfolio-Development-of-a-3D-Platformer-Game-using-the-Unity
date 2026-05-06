using System;
using System.Collections;
using UnityEngine;


using Resources   = BossBase.Resources;
using State       = BossBase.State;
using MainState   = BossBase.State.Main;
using BossSetting = BossBase.BossSetting;
using SubState    = CharacterBase.State.Sub;
using DamageType  = IDamageable.Type;


public interface IBossBase : IEnemyBase
{
    #region Property

    // Component
    new Resources resources { get; }

    // State
    new State state { get; }

    // Setting
    BossSetting bossSetting { get; }

    #endregion


    #region Method

    Coroutine Appear();

    #endregion
}


public class BossBase : EnemyBase, IBossBase
{
    #region Definition

    public new class Resources : EnemyBase.Resources
    {
        #region Field

        public new IBossModelBase              model   { get; }
        public new IBossVoiceBase              voice   { get; }
        public new CharacterEffects<MainState> effects { get; }

        #endregion


        #region Constructor

        public Resources(Transform transform) : base(transform)
        {
            model   = transform.GetComponentInChildren<IBossModelBase>(true);
            voice   = transform.GetComponentInChildren<IBossVoiceBase>(true);
            effects = new CharacterEffects<MainState>(transform.Find("Effects"));
        }

        #endregion


        #region Method

        public float Play(MainState type, SubState subType = SubState.None)
        {
            model.Play(type, subType);

            return Mathf.Max(voice.Play(type, subType), effects.Play(type, subType));
        }

        #endregion
    }


    public new class State : EnemyBase.State
    {
        #region Definition

        public new enum Main { None = 0, Idle = 1, Damage = 2, Find = 4, Die = 8, Appear = 16 }

        #endregion


        #region Field

        public new Main main;
        public int      hitPoint;

        #endregion


        #region Constructor

        public State() : base() { }

        public State(int hitPoint) : base() { this.hitPoint = hitPoint; }

        #endregion
    }


    [Serializable] public class BossSetting
    {
        #region Definition

        [Serializable] public class Die
        {
            #region Field

            [SerializeField] protected float _duration;

            public float duration { get { return _duration; } }

            #endregion


            #region Constructor

            public Die(float duration) { _duration = duration; }

            #endregion
        }


        [Serializable] public class Appear
        {
            #region Field

            [SerializeField] protected float _duration;

            public float duration { get { return _duration; } }

            #endregion


            #region Constructor

            public Appear(float duration) { _duration = duration; }

            #endregion
        }

        #endregion


        #region Field

        [SerializeField] protected Die    _die;
        [SerializeField] protected Appear _appear;
        [SerializeField] protected int    _maxHitPoint;

        public Die    die         { get { return _die; } }
        public Appear appear      { get { return _appear; } }
        public int    maxHitPoint { get { return _maxHitPoint; } }

        #endregion


        #region Constructor

        public BossSetting(Die die, Appear appear, int maxHitPoint)
        {
            _die         = die;
            _appear      = appear;
            _maxHitPoint = maxHitPoint;
        }

        #endregion
    }

    #endregion


    #region Field

    public new Resources resources { get; protected set; }

    public new State state { get; protected set; }

    [SerializeField] protected BossSetting _bossSetting;

    public BossSetting bossSetting { get { return _bossSetting; } }

    #endregion


    #region Method

    #region Event

    protected override void Start()
    {
        if ((planet == null) || planet.characters.Contains(this)) return;

        planet.characters.Add(this);

        if (planet.enabled && (state.main != MainState.Appear))
        {
            Initialize();
            Idle();
        }
    }

    protected override void OnCollisionStay(Collision collision)
    {
        var invalidType = MainState.Damage | MainState.Die | MainState.Appear;

        if ((state.main != MainState.None) && invalidType.HasFlag(state.main)) return;

        base.OnCollisionStay(collision);
    }

    #endregion


    #region Initialization

    protected override void SetField()
    {
        base.SetField();

        resources = new Resources(transform.Find("Resources"));

        state = new State(bossSetting.maxHitPoint);
    }

    protected override void ResetField(Rigidbody rigidbody)
    {
        base.ResetField(rigidbody);

        _bossSetting = new BossSetting(
            new BossSetting.Die(4.5f),
            new BossSetting.Appear(6f),
            3);
    }

    #endregion


    #region Action

    public override void StopAction()
    {
        base.StopAction();

        switch (state.main)
        {
            case MainState.Appear: StopAppear(); break;
        }
    }

    #endregion


    #region Idle

    public override Coroutine Idle(bool playAnimation = false)
    {
        state.main = MainState.Idle;

        return base.Idle(playAnimation);
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
        MainState invalidType = MainState.Damage | MainState.Die | MainState.Appear;

        if ((state.main != MainState.None) && invalidType.HasFlag(state.main)) return false;

        return base.TryDamage(attacker, type);
    }

    public override Coroutine Damage(Transform attacker, DamageType type)
    {
        state.main     =  MainState.Damage;
        state.damage   =  type;
        state.hitPoint -= 1;

        return base.Damage(attacker, type);
    }

    protected override IEnumerator _Damage(Transform attacker, DamageType type)
    {
        yield return new WaitForSeconds(characterSetting.damage.duration);

        if (state.hitPoint <= 0)
        {
            Die();
            yield break;
        }
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
        state.main = MainState.Find;

        return base.Find(player);
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
        state.main = MainState.Die;

        resources.Play(state.main);

        return base.Die();
    }

    protected override IEnumerator _Die()
    {
        state.sub = SubState.Start;

        bool  useUnscaledTime = Time.timeScale <= 0f;
        float duration        = bossSetting.die.duration;

        yield return useUnscaledTime ? new WaitForSecondsRealtime(duration) : new WaitForSeconds(duration);

        state.sub = SubState.End;

        resources.model.gameObject.SetActive(false);

        duration = resources.effects[state.main][state.sub].Play();

        yield return useUnscaledTime ? new WaitForSecondsRealtime(duration) : new WaitForSeconds(duration);

        Destroy(gameObject);
    }

    protected override void StopDie()
    {
        base.StopDie();

        state.main = MainState.None;
    }

    #endregion


    #region Appear

    public virtual Coroutine Appear()
    {
        gameObject.SetActive(true);

        state.main            = MainState.Appear;
        rigidbody.isKinematic = true;
        collider.enabled      = false;

        resources.Play(state.main);

        return action = StartCoroutine(_Appear(Time.timeScale <= 0f));
    }

    protected virtual IEnumerator _Appear(bool useUnscaledTime)
    {
        float duration = bossSetting.appear.duration;

        yield return useUnscaledTime ? new WaitForSecondsRealtime(duration) : new WaitForSeconds(duration);

        Idle(true);
    }

    protected virtual void StopAppear()
    {
        state.main            = MainState.None;
        rigidbody.isKinematic = false;
        collider.enabled      = true;
    }

    #endregion

    #endregion
}
