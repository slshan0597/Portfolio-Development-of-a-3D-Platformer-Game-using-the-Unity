// //////////////////////////////////////////////////////////////////////////////
// * 목차
//    1. 인터페이스 ... Line 00
//    2. 클래스 ....... Line 00
//        1) 정의 ... Line 00
//            1- 지면(Ground) 상태 ... Line 00
//            2- 캐릭터 상태 ......... Line 00
//            3- 캐릭터 설정 ......... Line 00
//        2) 필드 ..... Line 00
//        3) 메서드 ... Line 00
//            1- 이벤트 함수(Unity 호출) ... Line 00
//            2- 초기화 ................... Line 00
//            3- 셋(Set) .................. Line 00
//            4- 액션 ..................... Line 00
//                1_ 대기(Idle) ..... Line 00
//                2_ 피격(Damage) ... Line 00
// //////////////////////////////////////////////////////////////////////////////
using System;
using System.Collections;
using UnityEngine;

using Resources    = EnemyBase.Resources;
using State        = EnemyBase.State;
using MainState    = EnemyBase.State.Main;
using EnemySetting = EnemyBase.EnemySetting;
using SubState     = CharacterBase.State.Sub;
using DamageType   = IDamageable.Type;

// //////////////////////////////////////////////////////////////////////////////
// 1. 인터페이스(ICharacterBase 인터페이스 상속)
// //////////////////////////////////////////////////////////////////////////////
public interface IEnemyBase : ICharacterBase
{
    // 프로퍼티
    // Component
    SphereCollider                trigger   { get; }
    new IEnemyDirectionController direction { get; }
    new Resources                 resources { get; }

    // Reference
    IEnemySpawner spawner { get; set; }

    // State
    new State state { get; }

    // Setting
    EnemySetting enemySetting { get; }

    // 메서드
    // Action
    Coroutine Find(IPlayerController player);
    Coroutine Die();
}

// //////////////////////////////////////////////////////////////////////////////
// 2. 클래스(CharacterBase 클래스 상속)
// //////////////////////////////////////////////////////////////////////////////
public class EnemyBase : CharacterBase, IEnemyBase
{
    // ==============================================================================
    // 1) 정의
    // ==============================================================================
    // ------------------------------------------------------------------------------
    // 1-1) 정의 -> 캐릭터 상태
    //    - 부모 클래스(CharacterBase.State)를 대체하여 새로 정의
    // ------------------------------------------------------------------------------
    public new class State : CharacterBase.State
    {
        public new enum Main { None = 0, Idle = 1, Damage = 2, Find = 4, Die = 8 }

        public new Main main;
    }

    // ------------------------------------------------------------------------------
    // 1-2) 정의 -> 캐릭터 설정
    //    - 부모(CharacterBase.Setting) 클래스는 그대로 사용, 별도의 추가 클래스 정의
    //    - 각 상태에 대한 설정 프로퍼티
    // ------------------------------------------------------------------------------
    [Serializable] public class EnemySetting
    {
        // Definition
        [Serializable] public class Find
        {
            [SerializeField] protected float _duration;

            public float duration { get { return _duration; } }

            public Find(float duration) { _duration = duration; }
        }

        [Serializable] public class Die
        {
            [SerializeField] protected GameObject _drop;

            public GameObject drop { get { return _drop; } }
        }

        // Field
        [SerializeField] protected Find _find;
        [SerializeField] protected Die  _die;

        public Find find { get { return _find; } }
        public Die  die  { get { return _die; } }

        // Method
        public EnemySetting(Find find)
        { 
            _find = find;
            _die  = new Die();
        }
    }

    // ==============================================================================
    // 2) 필드
    // ==============================================================================
    // Component & Reference
    public SphereCollider                trigger   { get; protected set; }
    public new IEnemyDirectionController direction { get; protected set; }
    public new Resources                 resources { get; protected set; }
    public IEnemySpawner                 spawner   { get; set; }

    // State
    public new State state { get; protected set; } = new State();

    // Setting
    [SerializeField] protected EnemySetting _enemySetting;

    public EnemySetting enemySetting { get { return _enemySetting; } }

    // ==============================================================================
    // 3) 메서드
    //    - 부모 클래스의 함수들을 재정의하여 확장
    //    - Enemy 클래스 확장을 위한 기반 기능만을 구현
    // ==============================================================================
    // ------------------------------------------------------------------------------
    // 3-1) 메서드 -> 이벤트 함수(Unity 호출)
    // ------------------------------------------------------------------------------
    protected virtual void OnCollisionStay(Collision collision)
    {
        var invalidType = MainState.Damage | MainState.Die;

        if ((state.main != MainState.None) &&invalidType.HasFlag(state.main))   return;
        if (!collision.transform.TryGetComponent(out IPlayerController target)) return;

        ((IDamageable)target).TryDamage(transform, DamageType.Normal);
    }

    protected virtual void OnTriggerStay(Collider other)
    {
        if (other.isTrigger || !other.TryGetComponent(out IPlayerController player)) return;

        if (state.main != MainState.Find) TryFind(player);
    }

    // ------------------------------------------------------------------------------
    // 3-2) 메서드 -> 초기화
    // ------------------------------------------------------------------------------
    protected override void SetField()
    {
        base.SetField();

        trigger   = GetComponent<SphereCollider>();
        direction = GetComponentInChildren<IEnemyDirectionController>(true);
        resources = new Resources(transform.Find("Resources"));
    }

    protected override void ResetField(Rigidbody rigidbody)
    {
        base.ResetField(rigidbody);

        _enemySetting = new EnemySetting(new EnemySetting.Find(1f));
    }

    public override void Initialize()
    {
        base.Initialize();

        trigger.enabled = false;
    }

    // ------------------------------------------------------------------------------
    // 3-3) 메서드 -> 액션(Common)
    //    - 모든 행동에 대한 정지
    //    - 플레이어에 대한 록온(Look At) 기능
    //    - 루틴: 시작(TryMethod, Method) -> 반복(_Method) -> 종료(StopMethod)
    // ------------------------------------------------------------------------------
    public override void StopAction()
    {
        base.StopAction();

        switch (state.main)
        {
            case MainState.Find: StopFind(); break;
            case MainState.Die:  StopDie();  break;
        }
    }

    protected virtual IEnumerator LookAt(IPlayerController player, float duration)
    {
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            direction.LookAt(player.transform);

            elapsedTime += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }

        direction.LookAt(player.transform);
    }

    // ******************************************************************************
    // 3-3-1) 메서드 -> 액션 -> 대기(Idle)
    // ******************************************************************************
    public override Coroutine Idle(bool playAnimation = false)
    {
        state.main      = MainState.Idle;
        trigger.enabled = true;

        return base.Idle(playAnimation);
    }

    protected override IEnumerator _Idle() { while (planet.enabled) yield return new WaitForFixedUpdate(); }

    protected override void StopIdle()
    {
        base.StopIdle();

        state.main      = MainState.None;
        trigger.enabled = false;
    }

    // ******************************************************************************
    // 3-3-2) 메서드 -> 액션 -> 피격(Damage)
    //    - 부모 클래스 내 함수에 상태값 변환 기능만 추가
    //    - 상태 종료 다음에 Die 호출
    // ******************************************************************************
    public override bool TryDamage(Transform attacker, DamageType type)
    {
        MainState invalidType = MainState.Damage | MainState.Die;

        if ((state.main != MainState.None) && invalidType.HasFlag(state.main)) return false;

        return base.TryDamage(attacker, type);
    }

    public override Coroutine Damage(Transform attacker, DamageType type)
    {
        state.main   = MainState.Damage;
        state.damage = type;

        return base.Damage(attacker, type);
    }

    protected override IEnumerator _Damage(Transform attacker, DamageType type)
    {
        yield return base._Damage(attacker, type);

        Die();
    }

    protected override void StopDamage()
    {
        base.StopDamage();

        state.main   = MainState.None;
        state.damage = DamageType.None;
    }

    // ******************************************************************************
    // 3-3-3) 메서드 -> 액션 -> 발견(Find)
    //    - 영역 내에 플레이어가 감지되면 록온(Look At)
    // ******************************************************************************
    protected virtual void TryFind(IPlayerController player)
    {
        Vector3 startPosition           = transform.position;
        Vector3 endPosition             = player.transform.position;
        var     queryTriggerInteraction = QueryTriggerInteraction.Ignore;

        if (Physics.Linecast(startPosition, endPosition, out RaycastHit hit, -1, queryTriggerInteraction)
            && (hit.transform != player.transform)) return;

        Find(player);
    }

    public virtual Coroutine Find(IPlayerController player) 
    {
        state.main = MainState.Find;

        SetFriction(true);
        resources.Play(state.main);

        return action = StartCoroutine(_Find(player));
    }

    protected virtual IEnumerator _Find(IPlayerController player) 
    {
        yield return LookAt(player, enemySetting.find.duration);
    }

    protected virtual void StopFind()
    { 
        state.main = MainState.None;

        SetFriction(false);
    }

    // ******************************************************************************
    // 3-3-3) 메서드 -> 액션 -> 죽기(Die)
    //    - 오브젝트 제거와 동시에 아이템 드롭
    //    - 지정된 스포너가 존재할 경우 리스폰 호출
    // ******************************************************************************
    public virtual Coroutine Die()
    {
        state.main            = MainState.Die;
        rigidbody.isKinematic = true;
        collider.enabled      = false;

        if (enemySetting.die.drop != null)
        {
            var drop = Instantiate(enemySetting.die.drop, transform.position, transform.rotation, planet.objects);

            if (drop.TryGetComponent(out IItemBase item)) item.Spawn();
        }
        if (spawner != null) spawner.SpawnEnemy();

        return action = StartCoroutine(_Die());
    }

    protected virtual IEnumerator _Die()
    {
        resources.model.gameObject.SetActive(false);

        yield return new WaitForSeconds(resources.effects[state.main].Play());

        Destroy(gameObject);
    }

    protected virtual void StopDie()
    {
        state.main            = MainState.None;
        rigidbody.isKinematic = false;
        collider.enabled      = true;
    }
}
