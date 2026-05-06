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

// //////////////////////////////////////////////////////////////////////////////
// 1. 인터페이스(IEnemyBase 인터페이스 상속)
// //////////////////////////////////////////////////////////////////////////////
public interface IBobombController : IEnemyBase
{
    // 프로퍼티
    // Component
    new Resources   resources { get; }
    IBombController bomb      { get; }

    // State
    new State state { get; }

    // Setting
    Setting setting { get; }

    // 메서드
    // Action
    Coroutine Walk();
    Coroutine Chase(IPlayerController player);
}

// //////////////////////////////////////////////////////////////////////////////
// 2. 클래스(EnemyBase 클래스 상속)
// //////////////////////////////////////////////////////////////////////////////
public class BobombController : EnemyBase, IBobombController
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
        public new enum Main { None = 0, Idle = 1, Damage = 2, Find = 4, Die = 8, Walk = 16, Chase = 32 }

        public new Main main;
    }

    // ------------------------------------------------------------------------------
    // 1-2) 정의 -> 캐릭터 설정
    //    - 캐릭터 상태에 대한 설정 프로퍼티 저장
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

            public Walk(List<Element> elements, float duration) : base(elements) { _duration = duration; }
        }

        [Serializable] public class Chase
        {
            [SerializeField] protected MoveSetting _move;

            public MoveSetting move { get { return _move; } }

            public Chase(MoveSetting move) { _move = move; }
        }

        // Field
        [SerializeField] protected Idle  _idle;
        [SerializeField] protected Walk  _walk;
        [SerializeField] protected Chase _chase;

        public Idle  idle  { get { return _idle; } }
        public Walk  walk  { get { return _walk; } }
        public Chase chase { get { return _chase; } }

        // Method
        public Setting(Idle idle, Walk walk, Chase chase)
        {
            _idle  = idle;
            _walk  = walk;
            _chase = chase;
        }
    }

    // ==============================================================================
    // 2) 필드
    // ==============================================================================
    // Component & Reference
    public new Resources   resources { get; protected set; }
    public IBombController bomb      { get; protected set; }

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
    //    - 부모 클래스의 충돌 기능(Attack)을 제거하기 위한 임시 함수
    // ------------------------------------------------------------------------------
    protected override void OnCollisionStay(Collision collision) { }

    // ------------------------------------------------------------------------------
    // 3-2) 메서드 -> 초기화
    //    - 필드(컴포넌트 등) 초기화
    // ------------------------------------------------------------------------------
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

    // ------------------------------------------------------------------------------
    // 3-3) 메서드 -> 액션(Common)
    //    - 모든 행동에 대한 정지
    // ------------------------------------------------------------------------------
    public override void StopAction()
    {
        base.StopAction();

        switch (state.main)
        {
            case MainState.Walk:  StopWalk();  break;
            case MainState.Chase: StopChase(); break;
        }
    }
    
    // ******************************************************************************
    // 3-3-1) 메서드 -> 액션 -> 대기(Idle)
    //    - Walk 함수와 사이클 반복
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
    //    - Damage 타입이 Explode일 경우, 내장된 Bomb 오브젝트(Throwable)를 바로 폭발시키며 소멸
    // ******************************************************************************
    public override bool TryDamage(Transform attacker, DamageType type)
    {
        if (type == DamageType.Explode) return Explode();

        return base.TryDamage(attacker, type);
    }

    protected virtual bool Explode()
    {
        if (bomb.state != ThrowableState.Destroy) bomb.Destroy();    // 내장된 Bomb가 이미 폭발중이면 건너뜀(재귀 방지)
        if (spawner    != null)                   spawner.SpawnEnemy();

        Destroy(gameObject);

        return true;
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

    // ******************************************************************************
    // 3-3-3) 메서드 -> 액션 -> 발견(Find)
    //    - 실행 후 Chase 함수 호출
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
    //    - 아이템 대신 Bomb 오브젝트(Throwable)를 드롭
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
    //    - Idle 함수와 사이클 반복
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

    // ******************************************************************************
    // 3-3-6) 메서드 -> 액션 -> 추격(Chase)
    //    - 플레이어를 향해 회전 및 이동
    //    - 실행 시 내장된 Bomb 오브젝트(Throwable)의 타이머를 실행, 타이머 종료 시 내장된 Bomb 오브젝트 폭발
    // ******************************************************************************
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
}
