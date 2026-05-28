// //////////////////////////////////////////////////////////////////////////////
// * 요약
//    - 기반 클래스(BlockBase)의 확장 클래스
//
// * 목차
//    1. 인터페이스 ... Line 
//    2. 클래스 ....... Line 
//        1) 필드 ..... Line 
//        2) 메서드 ... Line 
//            1- 이벤트 함수 ... Line 
//            2- 초기화 ........ Line 
//            3- 액션 .......... Line 
//                1_ 대기(Idle) .... Line 
//                2_ 생성(Spawn) ... Line 
//                3_ 획득(Get) ..... Line 
// //////////////////////////////////////////////////////////////////////////////
using System.Collections;
using UnityEngine;

// //////////////////////////////////////////////////////////////////////////////
// 1. 인터페이스(IItemBase 인터페이스 상속)
// //////////////////////////////////////////////////////////////////////////////
public interface ICoinController : IItemBase
{
    // 프로퍼티
    // Component
    Rigidbody rigidbody { get; }

    // Setting
    float force         { get; }
    float spawnDuration { get; }
}

// //////////////////////////////////////////////////////////////////////////////
// 2. 클래스(ItemBase 클래스 상속)
//    - Gravityable : 중력의 영향을 받을 수 있음
// //////////////////////////////////////////////////////////////////////////////
[RequireComponent(typeof(Rigidbody))]
public class CoinController : ItemBase, ICoinController, IGravityable
{
    // ==============================================================================
    // 1) 필드
    // ==============================================================================
    // Component & Reference
    public new Rigidbody rigidbody { get; protected set; }

    // State
    public Vector3 gravity { get; set; } = Vector3.zero;

    // Setting
    [Header("Gravity Setting")]
    [SerializeField] protected bool  _useGravity;
    [SerializeField] protected float _radius;
    [SerializeField] protected bool  _fixRotation;
    [SerializeField] protected float _rotationSpeed;
    [Header("Item Setting")]
    [SerializeField] protected float _force;
    [SerializeField] protected float _spawnDuration;

    public bool  useGravity    { get { return _useGravity; } }
    public float radius        { get { return _radius; } }
    public bool  fixRotation   { get { return _fixRotation; } }
    public float rotationSpeed { get { return _rotationSpeed; } }
    public float force         { get { return _force; } }
    public float spawnDuration { get { return _spawnDuration; } }

    // ==============================================================================
    // 2) 메서드
    //    - 부모 클래스의 함수들을 재정의하여 확장
    // ==============================================================================
    // ------------------------------------------------------------------------------
    // 2-1) 메서드 -> 이벤트 함수
    // ------------------------------------------------------------------------------
    protected virtual void Reset() 
    {
        if (!TryGetComponent(out Rigidbody rigidbody)) return;

        ResetField(rigidbody);
    }

    // ------------------------------------------------------------------------------
    // 2-2) 메서드 -> 초기화
    // ------------------------------------------------------------------------------
    protected override void SetField()
    {
        base.SetField();

        rigidbody = GetComponent<Rigidbody>();
    }

    protected virtual void ResetField(Rigidbody rigidbody)
    {
        rigidbody.useGravity     = false;
        rigidbody.freezeRotation = true;
        _useGravity              = true;
        _radius                  = TryGetComponent(out SphereCollider collider) ? collider.radius : 0f;
        _fixRotation             = true;
        _rotationSpeed           = 10f;
        _force                   = 300f;
        _spawnDuration           = 0.5f;
        _lifeSpan                = 10f;
        _modelRPS                = 0.5f;
    }

    // ------------------------------------------------------------------------------
    // 2-3) 메서드 -> 액션
    // ------------------------------------------------------------------------------
    // ******************************************************************************
    // 2-3-1) 메서드 -> 액션 -> 대기(Idle)
    //    - 중력의 영향을 받기 위해 Rigidbody 컴포넌트가 존재해야 하며, 플레이어와 필드를 제외한 오브젝트와의 충돌은 제외해야 함
    //    - Rigidbody 컴포넌트의 존재에 따라 기존의 트리거 이벤트 대신 별도의 충돌(Overlap) 기능 추가
    // ******************************************************************************
    protected override IEnumerator _Idle()
    {
        int     layerMask = 1 << LayerMask.NameToLayer("Player");
        var     qtr       = QueryTriggerInteraction.Ignore;
        var     colliders = new Collider[1];
        Vector3 rotation  = Vector3.up * 360f * modelRPS;

        while (Physics.OverlapSphereNonAlloc(transform.position, trigger.radius, colliders, layerMask, qtr) <= 0)
        {
            resources.model.transform.Rotate(rotation * Time.deltaTime);

            yield return null;
        }

        Get();
    }

    // ******************************************************************************
    // 2-3-2) 메서드 -> 액션 -> 생성(Spawn)
    // ******************************************************************************
    public override Coroutine Spawn(Transform spawner = null)
    {
        rigidbody.AddForce(transform.up * Mathf.Sqrt(force), ForceMode.VelocityChange);

        return base.Spawn(spawner);
    }

    protected override IEnumerator _Spawn(Transform spawner, bool useUnscaledTime)
    {
        trigger.enabled = true;

        resources.effects[state].Play();

        yield return useUnscaledTime ? new WaitForSecondsRealtime(spawnDuration) : new WaitForSeconds(spawnDuration); yield return null;

        if (spawner != null) Get();
        else
        {
            if (lifeSpan > 0f) StartCoroutine(SetTimer());

            Idle();
        }
    }

    // ******************************************************************************
    // 2-3-3) 메서드 -> 액션 -> 획득(Get)
    // ******************************************************************************
    public override Coroutine Get(IPlayerController player = null)
    {
        rigidbody.isKinematic = true;

        return base.Get(player);
    }
}
