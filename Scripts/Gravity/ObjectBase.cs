// //////////////////////////////////////////////////////////////////////////////
// * 요약
//    - 커스텀 중력 속성에 대한 인터페이스
//
// * 목차
//    1. 인터페이스 ... Line 13
//        1- 중력(Gravityable) ... Line 16
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
