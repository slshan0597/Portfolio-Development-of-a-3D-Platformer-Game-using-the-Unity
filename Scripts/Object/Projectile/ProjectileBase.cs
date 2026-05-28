// //////////////////////////////////////////////////////////////////////////////
// * 요약
//    - 캐릭터(플레이어 또는 적)가 발사할 수 있는 오브젝트의 기반 클래스
//    - 발사된 오브젝트와 충돌 시 공격하여 피해를 입힘
//
// * 목차
//    1. 인터페이스 ... Line 
//    2. 클래스 ....... Line 
//        1) 내부 타입 ... Line 
//        1) 필드 ........ Line 
//        2) 메서드 ...... Line 
//            1- 이벤트 함수 ... Line 
//            2- 초기화 ........ Line 
//            3- 액션 .......... Line 
//                1_ 발사(Launch) .... Line 
//                2_ 파괴(Destroy) ... Line 
// //////////////////////////////////////////////////////////////////////////////
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using State         = ProjectileBase.State;
using Resources     = ProjectileBase.Resources;
using LaunchSetting = ProjectileBase.LaunchSetting;
using DamageType    = IDamageable.Type;

// //////////////////////////////////////////////////////////////////////////////
// 1. 인터페이스
// //////////////////////////////////////////////////////////////////////////////
public interface IProjectileBase
{
    // 프로퍼티
    // Component
    Transform      transform { get; }
    Rigidbody      rigidbody { get; }
    SphereCollider collider  { get; }
    Resources      resources { get; }

    // Reference
    ICharacterBase launcher { get; }

    // State
    State state { get; }

    // Setting
    float         lifeSpan      { get; }
    LaunchSetting launchSetting { get; }

    // 메서드
    // Action
    Coroutine Launch(ICharacterBase launcher, Transform target = null);
    Coroutine Destroy();
}

// //////////////////////////////////////////////////////////////////////////////
// 2. 클래스
// //////////////////////////////////////////////////////////////////////////////
[RequireComponent(typeof(Rigidbody))]
public class ProjectileBase : MonoBehaviour, IProjectileBase
{
    // ==============================================================================
    // 1) 내부 타입
    // ==============================================================================
    public enum State { None, Launch, Destroy }

    [Serializable] public struct LaunchSetting
    {
        [SerializeField, Range(0f, 90f)] private float _angle;
        [SerializeField]                 private float _force;
        [SerializeField]                 private float _speed;

        public float angle { get { return _angle; } }
        public float force { get { return _force; } }
        public float speed { get { return _speed; } }

        public LaunchSetting(float angle, float force, float speed)
        {
            _angle = angle;
            _force = force;
            _speed = speed;
        }

        public LaunchSetting(LaunchSetting other)
        {
            _angle = other.angle;
            _force = other.force;
            _speed = other.speed;
        }
    }

    // ==============================================================================
    // 2) 필드
    // ==============================================================================
    // Component & Reference
    public new Rigidbody      rigidbody { get; protected set; }
    public new SphereCollider collider  { get; protected set; }
    public Resources          resources { get; protected set; }
    public ICharacterBase     launcher  { get; protected set; }

    // State
    public State state { get; protected set; }

     Setting
    [SerializeField] protected float         _lifeSpan;
    [SerializeField] protected LaunchSetting _launchSetting;

    public float         lifeSpan      { get { return _lifeSpan; } }
    public LaunchSetting launchSetting { get { return _launchSetting; } }

    // ==============================================================================
    // 3) 메서드
    // ==============================================================================
    // ------------------------------------------------------------------------------
    // 3-1) 메서드 -> 이벤트 함수
    //    - 피격 가능한(Damageable) 오브젝트와 충돌 시 피격 기능 호출 및 오브젝트 파괴
    // ------------------------------------------------------------------------------
    protected virtual void Awake() { SetField(); }

    protected virtual void Reset()
    {
        if (!TryGetComponent(out Rigidbody rigidbody)) return;

        ResetField(rigidbody);
    }

    protected virtual void OnCollisionEnter(Collision collision)
    {
        if (collision.transform.TryGetComponent(out IDamageable target))
        {
            if (target != launcher)
            {
                target.TryDamage(transform, DamageType.Normal);
                Destroy();
            }
        }
        else Destroy();
    }

    // ------------------------------------------------------------------------------
    // 3-2) 메서드 -> 초기화
    // ------------------------------------------------------------------------------
    protected virtual void SetField()
    {
        rigidbody = GetComponent<Rigidbody>();
        collider  = GetComponent<SphereCollider>();
        resources = new Resources(transform.Find("Resources"));
    }

    protected virtual void ResetField(Rigidbody rigidbody) 
    {
        rigidbody.useGravity = false;
        _lifeSpan            = 5f;
    }

    // ------------------------------------------------------------------------------
    // 3-3) 메서드 -> 액션
    // ------------------------------------------------------------------------------
    // ******************************************************************************
    // 3-3-1) 메서드 -> 액션 -> 발사(Launch)
    //    - 실질적인 발사 기능은 확장된 오브젝트에서 구현
    // ******************************************************************************
    public virtual Coroutine Launch(ICharacterBase launcher, Transform target = null) 
    {
        state            = State.Launch;
        this.launcher    = launcher;
        collider.enabled = true;

        resources.PlayEffect(state);

        return StartCoroutine(_Launch(target));
    }

    protected virtual IEnumerator _Launch(Transform target)
    {
        yield return new WaitForSeconds(lifeSpan);

        Destroy();
    }

    // ******************************************************************************
    // 3-3-2) 메서드 -> 액션 -> 파괴(Destroy)
    // ******************************************************************************
    public virtual Coroutine Destroy()
    {
        StopAllCoroutines();

        state                 = State.Destroy;
        rigidbody.isKinematic = true;
        collider.enabled      = false;

        return StartCoroutine(_Destroy(Time.timeScale <= 0f));
    }

    protected virtual IEnumerator _Destroy(bool useUnscaledTime) { Destroy(gameObject); yield break; }
}
