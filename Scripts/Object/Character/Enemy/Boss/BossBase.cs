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
//                5_ 등장(Appear) ... Line 
// //////////////////////////////////////////////////////////////////////////////
using System;
using System.Collections;
using UnityEngine;

using Resources   = BossBase.Resources;
using State       = BossBase.State;
using MainState   = BossBase.State.Main;
using BossSetting = BossBase.BossSetting;
using SubState    = CharacterBase.State.Sub;
using DamageType  = IDamageable.Type;

// //////////////////////////////////////////////////////////////////////////////
// 1. 인터페이스(IEnemyBase 인터페이스 상속)
// //////////////////////////////////////////////////////////////////////////////
public interface IBossBase : IEnemyBase
{
    // 프로퍼티
    // Component
    new Resources resources { get; }

    // State
    new State state { get; }

    // Setting
    BossSetting bossSetting { get; }

    // 메서드
    // Action
    Coroutine Appear();
}

// //////////////////////////////////////////////////////////////////////////////
// 2. 클래스(EnemyBase 클래스 상속)
// //////////////////////////////////////////////////////////////////////////////
public class BossBase : EnemyBase, IBossBase
{
    // ==============================================================================
    // 1) 정의
    // ==============================================================================
    // ------------------------------------------------------------------------------
    // 1-1) 정의 -> 캐릭터 상태
    //    - 부모 클래스(EnemyBase.State)를 대체하여 새로 정의
    //    - 캐릭터의 주 상태 저장
    //    - 캐릭터의 체력 저장
    // ------------------------------------------------------------------------------
    public new class State : EnemyBase.State
    {
        public new enum Main { None = 0, Idle = 1, Damage = 2, Find = 4, Die = 8, Appear = 16 }

        public new Main main;
        public int      hitPoint;

        public State() : base() { }

        public State(int hitPoint) : base() { this.hitPoint = hitPoint; }
    }

    // ------------------------------------------------------------------------------
    // 1-2) 정의 -> 캐릭터 설정
    //    - 캐릭터 상태에 대한 설정 프로퍼티 저장
    // ------------------------------------------------------------------------------
    [Serializable] public class BossSetting
    {
        // Definition
        [Serializable] public class Die
        {
            [SerializeField] protected float _duration;

            public float duration { get { return _duration; } }

            public Die(float duration) { _duration = duration; }
        }

        [Serializable] public class Appear
        {
            [SerializeField] protected float _duration;

            public float duration { get { return _duration; } }

            public Appear(float duration) { _duration = duration; }
        }

        // Field
        [SerializeField] protected Die    _die;
        [SerializeField] protected Appear _appear;
        [SerializeField] protected int    _maxHitPoint;

        public Die    die         { get { return _die; } }
        public Appear appear      { get { return _appear; } }
        public int    maxHitPoint { get { return _maxHitPoint; } }

        // Method
        public BossSetting(Die die, Appear appear, int maxHitPoint)
        {
            _die         = die;
            _appear      = appear;
            _maxHitPoint = maxHitPoint;
        }
    }

    // ==============================================================================
    // 2) 필드
    // ==============================================================================
    // Component & Reference
    public new Resources resources { get; protected set; }

    // State
    public new State state { get; protected set; }

    // Setting
    [SerializeField] protected BossSetting _bossSetting;

    public BossSetting bossSetting { get { return _bossSetting; } }

    // ==============================================================================
    // 3) 메서드
    //    - 부모 클래스의 함수들을 재정의하여 확장
    //    - 클래스 확장을 위한 기반 기능만을 구현
    // ==============================================================================
    // ------------------------------------------------------------------------------
    // 3-1) 메서드 -> 이벤트 함수
    //    - 오브젝트 초기화
    // ------------------------------------------------------------------------------
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

    // ------------------------------------------------------------------------------
    // 3-2) 메서드 -> 초기화
    //    - 필드(컴포넌트 등) 초기화
    // ------------------------------------------------------------------------------
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

    // ------------------------------------------------------------------------------
    // 3-3) 메서드 -> 액션(Common)
    //    - 모든 행동에 대한 정지
    // ------------------------------------------------------------------------------
    public override void StopAction()
    {
        base.StopAction();

        switch (state.main)
        {
            case MainState.Appear: StopAppear(); break;
        }
    }

    // ******************************************************************************
    // 3-3-1) 메서드 -> 액션 -> 대기(Idle)
    // ******************************************************************************
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

    // ******************************************************************************
    // 3-3-2) 메서드 -> 액션 -> 피격(Damage)
    //    - 체력 감소
    //    - 피격 이후 체력이 0이면 Die 함수 호출
    // ******************************************************************************
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

    // ******************************************************************************
    // 3-3-3) 메서드 -> 액션 -> 발견(Find)
    // ******************************************************************************
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

    // ******************************************************************************
    // 3-3-4) 메서드 -> 액션 -> 죽기(Die)
    // ******************************************************************************
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

    // ******************************************************************************
    // 3-3-5) 메서드 -> 액션 -> 등장(Appear)
    //    - 보스 캐릭터는 필드 등장 연출 추가
    // ******************************************************************************
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
}
