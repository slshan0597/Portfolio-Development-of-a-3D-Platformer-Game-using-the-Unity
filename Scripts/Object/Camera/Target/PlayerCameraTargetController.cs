// //////////////////////////////////////////////////////////////////////////////
// * 요약
//    - 플레이어 전용의 카메라 타겟 클래스(기존 카메라 타겟 클래스 확장)
//    - 플레이어의 입력을 통해 카메라(타겟)을 회전하여 카메라가 타겟을 따라감(Follow)
//    - 카메라(타겟)과 플레이어 사이의 내부 거리를 조절하여 벽 등의 오브젝트에 가려지는 현상 방지
//
// * 목차
//    1. 인터페이스 ... Line 22
//    2. 클래스 ....... Line 44
//        1) 필드 ..... Line 49
//        2) 메서드 ... Line 67
//            1- 이벤트 함수 ... Line 70
//            2- 초기화 ........ Line 82
//            3- 액션 .......... Line 106
//                1_ 회전(Rotate) ................ Line 109
//                2_ 거리 조절(Move End Point) ... Line 121
// //////////////////////////////////////////////////////////////////////////////
using System;
using UnityEngine;

// //////////////////////////////////////////////////////////////////////////////
// 1. 인터페이스(ICameraTargetController 인터페이스 상속)
// //////////////////////////////////////////////////////////////////////////////
public interface IPlayerCameraTargetController : ICameraTargetController
{
    // 프로퍼티
    // Reference
    IPlayerController player { get; }

    // Setting
    float maxAngle    { get; }
    float maxDistance { get; }
    float moveSpeed   { get; }

    // 메서드
    void Initialize();

    // Action
    void Rotate(Vector2 angles);
    void MoveEndPoint();
}

// //////////////////////////////////////////////////////////////////////////////
// 2. 클래스(CameraTargetController 클래스 상속)
// //////////////////////////////////////////////////////////////////////////////
public class PlayerCameraTargetController : CameraTargetController, IPlayerCameraTargetController
{
    // ==============================================================================
    // 1) 필드
    // ==============================================================================
    // Component & Reference
    public IPlayerController player { get; protected set; }

    // Setting
    [SerializeField, Range(0f, 90f)] protected float _maxAngle;
    [SerializeField]                 protected float _maxDistance;
    [SerializeField]                 protected float _moveSpeed;

    public float maxAngle    { get { return _maxAngle; } }
    public float maxDistance { get { return _maxDistance; } }
    public float moveSpeed   { get { return _moveSpeed; } }

    // etc.
    protected Vector2 angles;

    // ==============================================================================
    // 2) 메서드
    // ==============================================================================
    // ------------------------------------------------------------------------------
    // 2-1) 메서드 -> 이벤트 함수
    // ------------------------------------------------------------------------------
    protected virtual void Reset() { ResetField(); }

    protected virtual void Update()
    {
        if (Time.timeScale == 0f) return;

        MoveEndPoint();
    }

    // ------------------------------------------------------------------------------
    // 2-2) 메서드 -> 초기화
    // ------------------------------------------------------------------------------
    protected override void SetField()
    {
        base.SetField();

        player = GetComponentInParent<IPlayerController>(true);
    }

    protected virtual void ResetField()
    {
        _maxAngle    = 75f;
        _maxDistance = 8f;
        _moveSpeed   = 10f;
    }

    public virtual void Initialize()
    {
        transform.localRotation = Quaternion.identity;
        endPoint.localPosition  = Vector3.back * maxDistance;
        angles                  = Vector3.zero;
    }

    // ------------------------------------------------------------------------------
    // 2-3) 메서드 -> 액션
    // ------------------------------------------------------------------------------
    // ******************************************************************************
    // 2-3-1) 메서드 -> 액션 -> 회전(Rotate)
    //    - 플레이어를 통해 호출되며, 전달받은 입력값을 통해 카메라(타겟)을 회전
    // ******************************************************************************
    public virtual void Rotate(Vector2 angles)
    {
        this.angles.x           += angles.x;
        this.angles.y           -= angles.y;
        this.angles.y           =  Mathf.Clamp(this.angles.y, -maxAngle, maxAngle);
        transform.localRotation =  Quaternion.Euler((Vector3.up * this.angles.x) + (Vector3.right * this.angles.y));
    }

    // ******************************************************************************
    // 2-3-2) 메서드 -> 액션 -> 거리 조절(Move End Point)
    //    - 플레이어와 카메라(타겟) 사이에 오브젝트가 존재하면 타겟의 내부 거리값을 조절하여 가림 현상 방지
    // ******************************************************************************
    public virtual void MoveEndPoint()
    {
        Vector3 origin    = transform.position;
        Vector3 direction = -transform.forward;
        int     layerMask = 1 << LayerMask.NameToLayer("Planet Ground");
        var     qtr       = QueryTriggerInteraction.Ignore;
        bool    isHit     = Physics.Raycast(origin, direction, out RaycastHit hit, maxDistance, layerMask, qtr);
        Vector3 position  = Vector3.back * (isHit ? Mathf.Clamp(hit.distance, 0f, maxDistance) : maxDistance);

        endPoint.localPosition = Vector3.Lerp(endPoint.localPosition, position, moveSpeed * Time.deltaTime);
    }
}
