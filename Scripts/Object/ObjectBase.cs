// //////////////////////////////////////////////////////////////////////////////
// * 요약
//    - 오브젝트의 여러 속성(Gravityable, Interactable, Damageable)에 대한 인터페이스
//
// * 목차
//    1. 인터페이스 ... Line 15
//        1- 중력(Gravityable) ........ Line 18
//        2- 상호작용(Interactable) ... Line 59
//        3- 피격(Damageable) ......... Line 77
// //////////////////////////////////////////////////////////////////////////////
using System;
using UnityEngine;

// //////////////////////////////////////////////////////////////////////////////
// 1. 인터페이스(속성)
// //////////////////////////////////////////////////////////////////////////////
// ==============================================================================
// 1-1. 중력(Gravityable)
//    - 커스텀 중력의 영향을 받는 오브젝트 속성
//    - 필드(Planet)에 의해 생성된 중력을 오브젝트에 부여
// ==============================================================================
public interface IGravityable
{
    // 프로퍼티
    // Component
    Transform transform { get; }
    Rigidbody rigidbody { get; }

    // Setting
    public bool  useGravity    { get; }
    public float radius        { get; }
    public bool  fixRotation   { get; }
    public float rotationSpeed { get; }

    // State
    public Vector3 gravity { get; set; }

    // 메서드
    void TryApplyGravity(IPlanetController planet)
    {
        if (rigidbody.isKinematic || !useGravity) return;

        gravity = planet.GetGravity(this);

        if (GetFallSpeed(gravity) < gravity.magnitude) rigidbody.AddForce(gravity, ForceMode.Acceleration);
    }

    float GetFallSpeed(Vector3 gravity)
    {
        Vector3 velocity = rigidbody.velocity;

        if (Vector3.Angle(gravity, velocity) >= 90f) return 0f;

        return Vector3.Project(velocity, gravity).magnitude;
    }
}

// ==============================================================================
// 1-2. 상호작용(Interactable)
//    - 플레이어에 의해 상호작용할 수 있는 오브젝트 속성
//    - 오브젝트 탐색을 위한 트리거 기능
// ==============================================================================
public interface IInteractable
{
    // 프로퍼티
    // Component
    GameObject     gameObject { get; }
    Transform      transform  { get; }
    SphereCollider trigger    { get; }

    // 메서드
    Coroutine Interact(IPlayerController player);
    void      StopInteract(IPlayerController player);
}

// ==============================================================================
// 1-3. 피격(Damageable)
//    - 캐릭터의 체력 감소나 오브젝트의 파괴를 위한 속성
//    - 특정 타입에 의해서만 호출되기 위한 마스크 기능
// ==============================================================================
public interface IDamageable
{
    // 정의
    [Flags] public enum Type { None = 0, Normal = 1, PressDown = 2, Explode = 4, Foot = 8, Head = 16 }

    // 프로퍼티
    // Component
    GameObject gameObject { get; }
    Transform  transform  { get; }

    // Setting
    Type mask { get; }

    // 메서드
    bool TryDamage(Transform attacker, Type type);
}
