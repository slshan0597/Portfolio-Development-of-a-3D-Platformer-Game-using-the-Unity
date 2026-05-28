// //////////////////////////////////////////////////////////////////////////////
// * 요약
//    - 기반 클래스(ProjectileBase)의 확장 클래스
//    - 중력의 영향을 받으며, 발사 후 지면과 충돌 시 튀어 오름
//
// * 목차
//    1. 인터페이스 ... Line 26
//    2. 클래스 ....... Line 36
//        1) 필드 ..... Line 42
//        2) 메서드 ... Line 63
//            1- 이벤트 함수 ... Line 66
//            2- 초기화 ........ Line 80
//            3- 액션 .......... Line 101
//                1_ 발사(Launch) .... Line 104
//                2_ 파괴(Destroy) ... Line 141
// //////////////////////////////////////////////////////////////////////////////
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Resources  = FireBallController.Resources;
using DamageType = IDamageable.Type;

// //////////////////////////////////////////////////////////////////////////////
// 1. 인터페이스(IProjectileBase 인터페이스 상속)
// //////////////////////////////////////////////////////////////////////////////
public interface IFireBallController : IProjectileBase
{
    // 프로퍼티
    // Component
    new Resources resources { get; }
}

// //////////////////////////////////////////////////////////////////////////////
// 2. 클래스(ProjectileBase 클래스 상속)
//    - Gravityable : 중력의 영향을 받을 수 있음
// //////////////////////////////////////////////////////////////////////////////
public class FireBallController : ProjectileBase, IFireBallController, IGravityable
{
    // ==============================================================================
    // 1) 필드
    // ==============================================================================
    // Component & Reference
    public new Resources resources { get; protected set; }

    // State
    public Vector3 gravity { get; set; } = Vector3.zero;

    // Setting
    [Header("Gravity Setting")]
    [SerializeField] protected bool  _useGravity;
    [SerializeField] protected float _radius;
    [SerializeField] protected bool  _fixRotation;
    [SerializeField] protected float _rotationSpeed;

    public bool  useGravity    { get { return _useGravity; } }
    public float radius        { get { return _radius; } }
    public bool  fixRotation   { get { return _fixRotation; } }
    public float rotationSpeed { get { return _rotationSpeed; } }

    // ==============================================================================
    // 2) 메서드
    // ==============================================================================
    // ------------------------------------------------------------------------------
    // 2-1) 메서드 -> 이벤트 함수
    //    - 충돌한 오브젝트를 식별하여 기능 수행
    // ------------------------------------------------------------------------------
    protected override void OnCollisionEnter(Collision collision)
    {
        if (collision.transform.TryGetComponent(out ICharacterBase character) && (character != launcher))
        {
            ((IDamageable)character).TryDamage(transform, DamageType.Normal);
            Destroy();
        }
        else resources.effects.bounce.Play();
    }

    // ------------------------------------------------------------------------------
    // 2-2) 메서드 -> 초기화
    // ------------------------------------------------------------------------------
    protected override void SetField()
    {
        base.SetField();

        resources = new Resources(transform.Find("Resources"));
    }

    protected override void ResetField(Rigidbody rigidbody)
    {
        base.ResetField(rigidbody);

        _launchSetting = new LaunchSetting(0f, 150f, 0f);
        _useGravity    = true;
        _radius        = TryGetComponent(out SphereCollider collider) ? collider.radius : 0f;
        _fixRotation   = false;
        _rotationSpeed = 10f;
    }

    // ------------------------------------------------------------------------------
    // 2-3) 메서드 -> 액션
    // ------------------------------------------------------------------------------
    // ******************************************************************************
    // 2-3-1) 메서드 -> 액션 -> 발사(Launch)
    //    - 지정된 방향(각도)를 향해 오브젝트에 힘을 가함
    //    - 지정된 시간이 지나면 오브젝트 파괴
    // ******************************************************************************
    protected override IEnumerator _Launch(Transform target)
    {
        rigidbody.isKinematic = false;

        float   angle          = launchSetting.angle * Mathf.Deg2Rad;
        Vector3 localDirection = (Vector3.forward * Mathf.Cos(angle)) + (Vector3.up * Mathf.Sin(angle));
        Vector3 direction      = transform.TransformDirection(localDirection);
        float   force          = Mathf.Sqrt(launchSetting.force);

        rigidbody.AddForce(direction * force, ForceMode.VelocityChange);

        Transform effect      = resources.effects[state].transform;
        float     elapsedTime = 0f;

        while (elapsedTime < lifeSpan)
        {
            Vector3 velocity = rigidbody.velocity;

            if (velocity.magnitude > 0f)
            {
                Quaternion rotation = Quaternion.LookRotation(velocity, Vector3.up);

                effect.rotation = Quaternion.Slerp(effect.rotation, rotation, rotationSpeed * Time.deltaTime);
            }

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        Destroy();
    }

    // ******************************************************************************
    // 2-3-2) 메서드 -> 액션 -> 파괴(Destroy)
    // ******************************************************************************
    protected override IEnumerator _Destroy(bool useUnscaledTime)
    {
        resources.model.gameObject.SetActive(false);

        float duration = resources.effects[State.Launch].Stop();

        yield return useUnscaledTime ? new WaitForSecondsRealtime(duration) : new WaitForSeconds(duration);

        Destroy(gameObject);
    }
}
