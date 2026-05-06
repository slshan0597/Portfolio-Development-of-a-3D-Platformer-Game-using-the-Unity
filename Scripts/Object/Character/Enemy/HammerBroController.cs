using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Resources  = HammerBroController.Resources;
using State      = HammerBroController.State;
using MainState  = HammerBroController.State.Main;
using Setting    = HammerBroController.Setting;
using SubState   = CharacterBase.State.Sub;
using DamageType = IDamageable.Type;


public interface IHammerBroController : IEnemyBase
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

    Coroutine Attack(IPlayerController player);
    Coroutine Jump(IPlayerController player);
    Coroutine Land(IPlayerController player);

    #endregion
}


public class HammerBroController : EnemyBase, IHammerBroController
{
    #region Definition

    public new class Resources : EnemyBase.Resources
    {
        #region Definition

        public class Models : List<IModelController>
        {
            #region Field

            public IHammerBroModelController       body   { get; }
            public IHammerBroWeaponModelController weapon { get; }

            #endregion


            #region Constructor

            public Models(Transform transform) : base(transform.GetComponentsInChildren<IModelController>(true))
            {
                body   = transform.GetComponentInChildren<IHammerBroModelController>(true);
                weapon = transform.GetComponentInChildren<IHammerBroWeaponModelController>(true);
            }

            #endregion
        }

        #endregion


        #region Field

        public Models                          models  { get; }
        public new CharacterEffects<MainState> effects { get; }

        #endregion


        #region Constructor

        public Resources(Transform transform) : base(transform)
        {
            models  = new Models(transform.Find("Models"));
            effects = new CharacterEffects<MainState>(transform.Find("Effects"));
        }

        #endregion


        #region Method

        public float Play(MainState type, SubState subType = SubState.None)
        {
            models.body.Play(type, subType);

            return effects.Play(type, subType);
        }

        #endregion
    }


    public new class State : EnemyBase.State
    {
        #region Definition

        public new enum Main { None = 0, Idle = 1, Damage = 2, Find = 4, Die = 8, Attack = 16, Jump = 32, Land = 64 }

        #endregion


        #region Field

        public new Main main;

        #endregion
    }


    [Serializable] public class Setting
    {
        #region Definition

        [Serializable] public class Attack
        {
            #region Variable

            [SerializeField] protected SimpleData<SubState, float> _durations;
            [SerializeField] protected int                         _repeatCount;
            [SerializeField] protected GameObject                  _projectile;

            public SimpleData<SubState, float> durations   { get { return _durations; } }
            public int                         repeatCount { get { return _repeatCount; } }
            public GameObject                  projectile  { get { return _projectile; } }

            #endregion


            #region Constructor

            public Attack(SimpleData<SubState, float> durations, int repeatCount)
            {
                _durations   = durations;
                _repeatCount = repeatCount;
            }

            #endregion
        }


        [Serializable] public class Jump
        {
            #region Field

            [SerializeField] protected float _force;

            public float force { get { return _force; } }

            #endregion


            #region Constructor

            public Jump(float force) { _force = force; }

            #endregion


            #region Method

            public Vector3 GetRandomForce(Transform transform)
            {
                Vector2 randomCircle         = UnityEngine.Random.insideUnitCircle * 0.1f;
                Vector3 randomLocalDirection = new Vector3(randomCircle.x, 1f, randomCircle.y).normalized;
                Vector3 force                = Mathf.Sqrt(this.force) * randomLocalDirection;

                return transform.TransformDirection(force);
            }

            #endregion
        }


        [Serializable] public class Land
        {
            #region Field

            [SerializeField] protected float _duration;

            public float duration { get { return _duration; } }

            #endregion


            #region Constructor

            public Land(float duration) { _duration = duration; }

            #endregion
        }

        #endregion


        #region Field

        [SerializeField] protected Attack _attack;
        [SerializeField] protected Jump   _jump;
        [SerializeField] protected Land   _land;

        public Attack attack { get { return _attack; } }
        public Jump   jump   { get { return _jump; } }
        public Land   land   { get { return _land; } }

        #endregion


        #region Constructor

        public Setting(Attack attack, Jump jump, Land land)
        {
            _attack = attack;
            _jump   = jump;
            _land   = land;
        }

        #endregion
    }

    #endregion


    #region Field

    public new Resources resources { get; protected set; }

    public new State state { get; protected set; } = new State();

    [SerializeField] protected Setting _setting;

    public Setting setting { get { return _setting; } }

    protected int attackCount;

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

        _setting = new Setting(
            new Setting.Attack(
                new SimpleData<SubState, float>(
                    new List<SimpleData<SubState, float>.Element>()
                    {
                        new SimpleData<SubState, float>.Element(SubState.Start, 0.5f),
                        new SimpleData<SubState, float>.Element(SubState.Loop,  0.5f),
                        new SimpleData<SubState, float>.Element(SubState.End,   0.5f)
                    }),
                2),
            new Setting.Jump(300f),
            new Setting.Land(0.5f));
    }

    #endregion


    #region Action

    public override void StopAction()
    {
        base.StopAction();

        switch (state.main)
        {
            case MainState.Attack: StopAttack(); break;
            case MainState.Jump:   StopJump();   break;
            case MainState.Land:   StopLand();   break;
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

    public override Coroutine Damage(Transform attacker, DamageType type)
    {
        StopAction();

        state.main   = MainState.Damage;
        state.damage = type;

        resources.models.weapon.gameObject.SetActive(false);

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

        Attack(player);
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


    #region Attack

    public virtual Coroutine Attack(IPlayerController player)
    {
        StopAction();

        state.main = MainState.Attack;

        SetFriction(true);
        resources.Play(state.main);

        return action = StartCoroutine(_Attack(player));
    }

    protected virtual IEnumerator _Attack(IPlayerController player)
    {
        state.sub = SubState.Start;

        var setting = this.setting.attack;

        yield return LookAt(player, setting.durations[state.sub]);

        state.sub = SubState.Loop;

        resources.Play(state.main, state.sub);
        resources.models.weapon.gameObject.SetActive(false);
        Throw(player.transform);

        yield return LookAt(player, setting.durations[state.sub]);

        resources.models.weapon.gameObject.SetActive(true);

        if (++attackCount < setting.repeatCount) Attack(player);
        else
        {
            state.sub   = SubState.End;
            attackCount = 0;

            yield return LookAt(player, setting.durations[state.sub]);

            Jump(player);
        }
    }

    protected virtual void Throw(Transform target)
    {
        Vector3    position  = resources.models.weapon.transform.position;
        Vector3    direction = (target.position - position).normalized;
        Vector3    project   = Vector3.ProjectOnPlane(direction, transform.up);
        Quaternion rotation  = Quaternion.LookRotation(project, transform.up);

        Instantiate(setting.attack.projectile, position, rotation, planet.objects)
            .GetComponent<IHammerController>()
            .Launch(this, target);
    }

    protected virtual void StopAttack()
    {
        state.main = MainState.None;
        state.sub  = SubState.None;

        SetFriction(false);
        resources.models.weapon.gameObject.SetActive(true);
    }

    #endregion


    #region Jump

    public virtual Coroutine Jump(IPlayerController player)
    {
        StopAction();

        state.main = MainState.Jump;

        resources.Play(state.main);
        rigidbody.AddForce(setting.jump.GetRandomForce(transform), ForceMode.VelocityChange);

        return action = StartCoroutine(_Jump(player));
    }

    protected virtual IEnumerator _Jump(IPlayerController player)
    {
        int maxCount = 3;

        for (int count = 0; count < maxCount; count++)
        {
            if (!groundState.isGrounded) break;

            direction.LookAt(player.transform);

            yield return new WaitForFixedUpdate();
        }
        while (!groundState.isGrounded)
        {
            direction.LookAt(player.transform);

            yield return new WaitForFixedUpdate();
        }

        Land(player);
    }

    protected virtual void StopJump() { state.main = MainState.None; }

    #endregion


    #region Land

    public virtual Coroutine Land(IPlayerController player)
    {
        StopAction();

        state.main = MainState.Land;

        SetFriction(true);
        resources.Play(state.main);

        return action = StartCoroutine(_Land(player));
    }

    protected virtual IEnumerator _Land(IPlayerController player)
    {
        yield return new WaitForSeconds(setting.land.duration);

        float distance = (player.transform.position - transform.position).magnitude;

        if (distance <= trigger.radius) Attack(player);
        else                            Idle();
    }

    protected virtual void StopLand()
    { 
        state.main = MainState.None;

        SetFriction(false);
    }

    #endregion

    #endregion

    #endregion
}
