// //////////////////////////////////////////////////////////////////////////////
// * 요약
//    - 기반 클래스(BlockBase)의 확장 클래스
//    - 충돌 시 플레이어 체력 회복
//
// * 목차
//    1. 인터페이스 ... Line 22
//    2. 클래스 ....... Line 37
//        1) 필드 ..... Line 44
//        2) 메서드 ... Line 72
//            1- 이벤트 함수 ... Line 75
//            2- 초기화 ........ Line 91
//            3- 액션 .......... Line 115
//                1_ 대기(Idle) .... Line 118
//                2_ 생성(Spawn) ... Line 148
//                3_ 획득(Get) ..... Line 183
// //////////////////////////////////////////////////////////////////////////////
using System.Collections;
using UnityEngine;

// //////////////////////////////////////////////////////////////////////////////
// 1. 인터페이스(IItemBase 인터페이스 상속)
// //////////////////////////////////////////////////////////////////////////////
public interface IHitPointItemController : IItemBase
{
    // 프로퍼티
    // Component
    Rigidbody rigidbody { get; }
    Transform direction { get; }

    // Setting
    int   amount    { get; }
    float moveSpeed { get; }
}

// //////////////////////////////////////////////////////////////////////////////
// 2. 클래스(ItemBase 클래스 상속)
//    - Gravityable : 중력의 영향을 받을 수 있음
// //////////////////////////////////////////////////////////////////////////////
[RequireComponent(typeof(Rigidbody))]
public class HitPointItemController : ItemBase, IHitPointItemController, IGravityable
{
    // ==============================================================================
    // 1) 필드
    // ==============================================================================
    // Component & Reference
    public new Rigidbody rigidbody { get; protected set; }
    public Transform     direction { get; protected set; }

    // State
    public Vector3 gravity { get; set; } = Vector3.zero;

    // Setting
    [Header("Gravity Setting")]
    [SerializeField] protected bool  _useGravity;
    [SerializeField] protected float _radius;
    [SerializeField] protected bool  _fixRotation;
    [SerializeField] protected float _rotationSpeed;

    [Header("Item Setting")]
    [SerializeField] protected int   _amount;
    [SerializeField] protected float _moveSpeed;

    public bool  useGravity    { get { return _useGravity; } }
    public float radius        { get { return _radius; } }
    public bool  fixRotation   { get { return _fixRotation; } }
    public float rotationSpeed { get { return _rotationSpeed; } }
    public int   amount        { get { return _amount; } }
    public float moveSpeed     { get { return _moveSpeed; } }

    // ==============================================================================
    // 2) 메서드
    // ==============================================================================
    // ------------------------------------------------------------------------------
    // 2-1) 메서드 -> 이벤트 함수
    // ------------------------------------------------------------------------------
    protected virtual void Reset()
    {
        if (!TryGetComponent(out Rigidbody rigidbody)) return;

        ResetField(rigidbody);
    }

    protected virtual void FixedUpdate()
    {
        resources.transform.localRotation
            = Quaternion.Slerp(resources.transform.localRotation, direction.localRotation, rotationSpeed * Time.fixedDeltaTime);
    }

    // ------------------------------------------------------------------------------
    // 2-2) 메서드 -> 초기화
    // ------------------------------------------------------------------------------
    protected override void SetField()
    {
        base.SetField();

        rigidbody = GetComponent<Rigidbody>();
        direction = transform.Find("Direction");
    }

    protected virtual void ResetField(Rigidbody rigidbody)
    {
        rigidbody.useGravity     = false;
        rigidbody.freezeRotation = true;
        _useGravity              = true;
        _radius                  = TryGetComponent(out SphereCollider collider) ? collider.radius : 0f;
        _fixRotation             = true;
        _rotationSpeed           = 10f;
        _lifeSpan                = 10f;
        _amount                  = 1;
        _moveSpeed               = 2.5f;
    }

    // ------------------------------------------------------------------------------
    // 2-3) 메서드 -> 액션
    // ------------------------------------------------------------------------------
    // ******************************************************************************
    // 2-3-1) 메서드 -> 액션 -> 대기(Idle)
    //    - 중력의 영향을 받기 위해 Rigidbody 컴포넌트가 존재해야 하며, 플레이어와 필드를 제외한 오브젝트와의 충돌은 제외해야 함
    //    - Rigidbody 컴포넌트의 존재에 따라 기존의 트리거 이벤트 대신 별도의 충돌(Overlap) 기능 추가
    // ******************************************************************************
    public override Coroutine Idle()
    {
        trigger.enabled       = true;
        rigidbody.isKinematic = false;

        return base.Idle();
    }

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

        Get(colliders[0].GetComponent<IPlayerController>());
    }

    // ******************************************************************************
    // 2-3-2) 메서드 -> 액션 -> 생성(Spawn)
    //    - 생성된 후 지정된 방향으로 이동
    // ******************************************************************************
    public override Coroutine Spawn(Transform spawner = null)
    {
        rigidbody.isKinematic = true;

        transform.Translate(Vector3.up * radius);

        return base.Spawn(spawner);
    }

    protected override IEnumerator _Spawn(Transform spawner, bool useUnscaledTime)
    {
        yield return base._Spawn(spawner, useUnscaledTime);

        StartCoroutine(Move());
    }

    protected virtual IEnumerator Move()
    {
        direction.localRotation = Quaternion.Euler(Vector3.up * Random.Range(0f, 360f));

        while (true)
        {
            Vector3 forward  = direction.forward;
            Vector3 velocity = Vector3.ProjectOnPlane(rigidbody.velocity, transform.up);

            rigidbody.AddForce(forward * moveSpeed - velocity, ForceMode.VelocityChange);

            yield return null;
        }
    }

    // ******************************************************************************
    // 2-3-3) 메서드 -> 액션 -> 획득(Get)
    //    - 획득 시 플레이어 체력 회복
    // ******************************************************************************
    public override Coroutine Get(IPlayerController player)
    {
        rigidbody.isKinematic = true;

        player.SetHitPoint(amount);

        return base.Get(player);
    }
}
