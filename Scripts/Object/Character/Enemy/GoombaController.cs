// //////////////////////////////////////////////////////////////////////////////
// * 목차
//    1. 인터페이스 ... Line 
//    2. 클래스 ....... Line 
//        1) 정의 ... Line 
//            1- 캐릭터 상태 ... Line 
//            2- 캐릭터 설정 ... Line 
//        2) 필드 ..... Line 
//        3) 메서드 ... Line 
//            1- 이벤트 함수 ... Line 
//            2- 초기화 ........ Line 
//            3- 액션 .......... Line 
//                1_ 대기(Idle) ..... Line 
//                2_ 피격(Damage) ... Line 
//                3_ 발견(Find) ..... Line 
//                4_ 죽기(Die) ...... Line 
// //////////////////////////////////////////////////////////////////////////////
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

// //////////////////////////////////////////////////////////////////////////////
// 1. 인터페이스(IEnemyBase 인터페이스 상속)
// //////////////////////////////////////////////////////////////////////////////
public interface IGoombaController : IEnemyBase
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
    Coroutine Walk();
    Coroutine Chase(IPlayerController player);
    Coroutine Brake();
    Coroutine Attack(IDamageable target);
}

// //////////////////////////////////////////////////////////////////////////////
// 2. 클래스(EnemyBase 클래스 상속)
// //////////////////////////////////////////////////////////////////////////////
public class GoombaController : EnemyBase, IGoombaController
{
    // ==============================================================================
    // 1) 정의
    // ==============================================================================
    // ------------------------------------------------------------------------------
    // 1-1) 정의 -> 캐릭터 상태
    //    - 부모 클래스(EnemyBase.State)를 대체하여 새로 정의
    // ------------------------------------------------------------------------------
    public new class State : EnemyBase.State
    {
        public new enum Main 
        {
            None = 0, Idle = 1, Damage = 2, Find = 4, Die = 8, Walk = 16, Chase = 32, Brake = 64, Attack = 128
        }
        
        public new Main main;
    }

    // ------------------------------------------------------------------------------
    // 1-2) 정의 -> 캐릭터 설정
    //    - 부모(EnemyBase.Setting) 클래스는 그대로 사용, 별도의 추가 클래스 정의
    //    - 각 상태에 대한 설정 프로퍼티
    // ------------------------------------------------------------------------------
    [Serializable] public class Setting
    {
        // Definition
        [Serializable] public class Idle
        {
            [SerializeField] protected float _duration;

            public float duration { get { return _duration; } }

            public Idle(float duration) { _duration = duration; }
        }

        [Serializable] public class Walk : SimpleData<SubState, Walk.Value>
        {
            [Serializable] public class Value
            {
                [SerializeField] protected MoveSetting _move;

                public MoveSetting move { get { return _move; } }

                public Value(MoveSetting move) { _move = move; }
            }

            [SerializeField] protected float _duration;

            public float duration { get { return _duration; } }

            public Walk(List<Element> elements, float duration) : base(elements) {  _duration = duration; }
        }

        [Serializable] public class Chase
        {
            [SerializeField]                  protected MoveSetting _move;
            [SerializeField, Range(0f, 180f)] protected float       _angle;
            [SerializeField, Range(1f, 10f)]  protected float       _triggerRadiusRate;

            public MoveSetting move              { get { return _move; } }
            public float       angle             { get { return _angle; } }
            public float       triggerRadiusRate { get { return _triggerRadiusRate; } }

            public Chase(MoveSetting move, float angle, float triggerRadiusRate) 
            {
                _move              = move;
                _angle             = angle;
                _triggerRadiusRate = triggerRadiusRate;
            }
        }

        [Serializable] public class Brake
        {
            [SerializeField] protected MoveSetting _move;
            [SerializeField] protected float       _duration;

            public MoveSetting move     { get { return _move; } }
            public float       duration { get { return _duration; } }

            public Brake(MoveSetting move, float duration) 
            {
                _move     = move;
                _duration = duration;
            }
        }

        [Serializable] public class Attack
        {
            [SerializeField] protected float _duration;

            public float duration { get { return _duration; } }

            public Attack(float duration) { _duration = duration; }
        }

        // Field
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

        // Method
        public Setting(Idle idle, Walk walk, Chase chase, Brake brake, Attack attack)
        {
            _idle   = idle;
            _walk   = walk;
            _chase  = chase;
            _brake  = brake;
            _attack = attack;
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

    // ==============================================================================
    // 3) 메서드
    //    - 부모 클래스의 함수들을 재정의하여 확장
    // ==============================================================================
    // ------------------------------------------------------------------------------
    // 3-1) 메서드 -> 이벤트 함수
    //    - 플레이어와 충돌하면 Attack 호출
    // ------------------------------------------------------------------------------
    protected override void OnCollisionStay(Collision collision)
    {
        var invalidType = MainState.Damage | MainState.Die;

        if ((state.main != MainState.None) && invalidType.HasFlag(state.main))  return;
        if (!collision.transform.TryGetComponent(out IPlayerController target)) return;

        if (state.main == MainState.Chase) Attack((IDamageable)target);
        else                               ((IDamageable)target).TryDamage(transform, DamageType.Normal);
    }

    // ------------------------------------------------------------------------------
    // 3-2) 메서드 -> 초기화
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

    // ------------------------------------------------------------------------------
    // 3-3) 메서드 -> 액션(Common)
    //    - 모든 행동에 대한 정지
    //    - 루틴: 시작(TryMethod, Method) -> 반복(_Method) -> 종료(StopMethod)
    // ------------------------------------------------------------------------------
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

    // ******************************************************************************
    // 3-3-1) 메서드 -> 액션 -> 대기(Idle)
    //    - Walk 상태와 사이클 반복
    // ******************************************************************************
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

    // ******************************************************************************
    // 3-3-2) 메서드 -> 액션 -> 피격(Damage)
    //    - 부모 클래스 내 함수에 상태값 변환 기능만 추가
    // ******************************************************************************
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

    // ******************************************************************************
    // 3-3-3) 메서드 -> 액션 -> 발견(Find)
    //    - 부모 클래스 내 함수에 상태값 변환 기능만 추가
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

        Chase(player);
    }

    protected override void StopFind()
    {
        base.StopFind();

        state.main = MainState.None;
    }

    // ******************************************************************************
    // 3-3-4) 메서드 -> 액션 -> 죽기(Die)
    //    - 부모 클래스 내 함수에 상태값 변환 기능만 추가
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
    // 3-3-5) 메서드 -> 액션 -> 걷기(Walk)
    //    - Idle 상태와 사이클 반복
    // ******************************************************************************
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
            Move(setting[state.sub].move);    // Move Method(CharacterBase)
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

    // ******************************************************************************
    // 3-3-6) 메서드 -> 액션 -> 추격(Chase)
    //    - Find 상태 종료 후 호출
    //    - 플레이어를 향해 이동
    // ******************************************************************************
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
            Move(setting.move);    // Move Method(CharacterBase)
            resources.model.SetMoveRate(moveSpeed / setting.move.speed);

            yield return new WaitForFixedUpdate();

            angle    = Vector3.Angle(direction.transform.forward, direction.GetDirection(_player));
            distance = (_player.position - transform.position).magnitude;
        }

        Brake();
    }

    protected virtual void StopChase() { state.main = MainState.None; }

    // ******************************************************************************
    // 3-3-7) 메서드 -> 액션 -> 멈추기(Brake)
    //    - Chase 상태 종료 후 호출
    //    - 캐릭터의 급제동
    // ******************************************************************************
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

    // ******************************************************************************
    // 3-3-8) 메서드 -> 액션 -> 공격(Attack)
    //    - 플레이어와 충돌하면 호출
    //    - 플레이어에게 Damage 호출
    // ******************************************************************************
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
}
