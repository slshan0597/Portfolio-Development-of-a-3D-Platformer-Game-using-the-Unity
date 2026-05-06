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

// //////////////////////////////////////////////////////////////////////////////
// 1. 인터페이스(IEnemyBase 인터페이스 상속)
// //////////////////////////////////////////////////////////////////////////////
public interface IHammerBroController : IEnemyBase
{
    // 프로퍼티
    // Component
    new Resources resources { get; }

    // State
    new State state { get; }

    // Setting
    Setting setting { get; }

    // 메서드
    // Action
    Coroutine Attack(IPlayerController player);
    Coroutine Jump(IPlayerController player);
    Coroutine Land(IPlayerController player);
}

// //////////////////////////////////////////////////////////////////////////////
// 2. 클래스(EnemyBase 클래스 상속)
// //////////////////////////////////////////////////////////////////////////////
public class HammerBroController : EnemyBase, IHammerBroController
{
    // ==============================================================================
    // 1) 정의
    // ==============================================================================
    // ------------------------------------------------------------------------------
    // 1-1) 정의 -> 캐릭터 상태
    //    - 부모 클래스(EnemyBase.State)를 대체하여 새로 정의
    //    - 캐릭터의 주 상태 저장
    // ------------------------------------------------------------------------------
    public new class State : EnemyBase.State
    {
        public new enum Main { None = 0, Idle = 1, Damage = 2, Find = 4, Die = 8, Attack = 16, Jump = 32, Land = 64 }

        public new Main main;
    }

    // ------------------------------------------------------------------------------
    // 1-2) 정의 -> 캐릭터 설정
    //    - 캐릭터 상태에 대한 설정 프로퍼티 저장
    // ------------------------------------------------------------------------------
    [Serializable] public class Setting
    {
        // Definition
        [Serializable] public class Attack
        {
            [SerializeField] protected SimpleData<SubState, float> _durations;
            [SerializeField] protected int                         _repeatCount;
            [SerializeField] protected GameObject                  _projectile;

            public SimpleData<SubState, float> durations   { get { return _durations; } }
            public int                         repeatCount { get { return _repeatCount; } }
            public GameObject                  projectile  { get { return _projectile; } }

            public Attack(SimpleData<SubState, float> durations, int repeatCount)
            {
                _durations   = durations;
                _repeatCount = repeatCount;
            }
        }

        [Serializable] public class Jump
        {
            [SerializeField] protected float _force;

            public float force { get { return _force; } }

            public Jump(float force) { _force = force; }

            public Vector3 GetRandomForce(Transform transform)
            {
                Vector2 randomCircle         = UnityEngine.Random.insideUnitCircle * 0.1f;
                Vector3 randomLocalDirection = new Vector3(randomCircle.x, 1f, randomCircle.y).normalized;
                Vector3 force                = Mathf.Sqrt(this.force) * randomLocalDirection;

                return transform.TransformDirection(force);
            }
        }

        [Serializable] public class Land
        {
            [SerializeField] protected float _duration;

            public float duration { get { return _duration; } }

            public Land(float duration) { _duration = duration; }
        }

        // Field
        [SerializeField] protected Attack _attack;
        [SerializeField] protected Jump   _jump;
        [SerializeField] protected Land   _land;

        public Attack attack { get { return _attack; } }
        public Jump   jump   { get { return _jump; } }
        public Land   land   { get { return _land; } }

        // Method
        public Setting(Attack attack, Jump jump, Land land)
        {
            _attack = attack;
            _jump   = jump;
            _land   = land;
        }
    }

    // ==============================================================================
    // 2) 필드
    // ==============================================================================
    // Component & Reference
    public new Resources resources { get; protected set; }

    // State
    public new State state { get; protected set; } = new State();

    // Setting
    [SerializeField] protected Setting _setting;

    public Setting setting { get { return _setting; } }

    // etc.
    protected int attackCount;

    // ==============================================================================
    // 3) 메서드
    //    - 부모 클래스의 함수들을 재정의하여 확장
    // ==============================================================================
    // ------------------------------------------------------------------------------
    // 3-1) 메서드 -> 초기화
    //    - 필드(컴포넌트 등) 초기화
    // ------------------------------------------------------------------------------
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

    // ------------------------------------------------------------------------------
    // 3-2) 메서드 -> 액션(Common)
    //    - 모든 행동에 대한 정지
    // ------------------------------------------------------------------------------
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

    // ******************************************************************************
    // 3-2-1) 메서드 -> 액션 -> 대기(Idle)
    // ******************************************************************************
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

    // ******************************************************************************
    // 3-2-2) 메서드 -> 액션 -> 피격(Damage)
    // ******************************************************************************
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

    // ******************************************************************************
    // 3-2-3) 메서드 -> 액션 -> 발견(Find)
    //    - 실행 후 Attack 함수 호출
    // ******************************************************************************
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

    // ******************************************************************************
    // 3-2-4) 메서드 -> 액션 -> 죽기(Die)
    // ******************************************************************************
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

    // ******************************************************************************
    // 3-2-5) 메서드 -> 액션 -> 공격(Attack)
    //    - 
    // ******************************************************************************
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
