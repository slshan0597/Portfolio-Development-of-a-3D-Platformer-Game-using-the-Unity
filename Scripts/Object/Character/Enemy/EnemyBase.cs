// //////////////////////////////////////////////////////////////////////////////
// * 목차
//    1. 인터페이스 ... Line 30
//    2. 클래스 ....... Line 56
//        1) 정의 ... Line 61
//            1- 캐릭터 상태 ... Line 64
//            2- 캐릭터 설정 ... Line 75
//        2) 필드 ..... Line 114
//        3) 메서드 ... Line 131
//            1- 이벤트 함수 ... Line 136
//            2- 초기화 ........ Line 156
//            3- 액션 .......... Line 182
//                1_ 대기(Idle) ..... Line 214
//                2_ 피격(Damage) ... Line 235
//                3_ 발견(Find) ..... Line 272
//                4_ 죽기(Die) ...... Line 310
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
    //    - 캐릭터의 주 상태 저장
    // ------------------------------------------------------------------------------
    public new class State : CharacterBase.State
    {
        public new enum Main { None = 0, Idle = 1, Damage = 2, Find = 4, Die = 8 }

        public new Main main;
    }

    // ------------------------------------------------------------------------------
    // 1-2) 정의 -> 캐릭터 설정
    //    - 캐릭터 상태에 대한 설정 프로퍼티 저장
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
    //    - 클래스 확장을 위한 기반 기능만을 구현
    // ==============================================================================
    // ------------------------------------------------------------------------------
    // 3-1) 메서드 -> 이벤트 함수
    //    - 범위 내에 플레이어 탐색
    //    - 플레이어와 충돌 시 공격
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
    //    - 필드(컴포넌트 등) 초기화
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
    //    - 플레이어 발견 시 플레이어를 향해 회전
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
    //    - 캐릭터와 플레이어 사이에 장애물이 없으면 플레이어를 발견
    //    - 플레이어를 향해 회전
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
    // 3-3-4) 메서드 -> 액션 -> 죽기(Die)
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
