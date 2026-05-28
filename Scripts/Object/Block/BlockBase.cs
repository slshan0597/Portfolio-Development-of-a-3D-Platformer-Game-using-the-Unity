// //////////////////////////////////////////////////////////////////////////////
// * 요약
//    - 블록 오브젝트에 대한 기반 클래스
//
// * 목차
//    1. 인터페이스 ... Line 26
//    2. 클래스 ....... Line 45
//        1) 필드 ..... Line 53
//        2) 메서드 ... Line 69
//            1- 이벤트 함수 ... Line 73
//            2- 초기화 ........ Line 80
//            3- 액션 .......... Line 90
//                1_ 대기(Idle) .... Line 93
//                2_ 파괴(Break) ... Line 104
// //////////////////////////////////////////////////////////////////////////////
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using State      = BlockBase.State;
using Resources  = BlockBase.Resources;
using DamageType = IDamageable.Type;

// //////////////////////////////////////////////////////////////////////////////
// 1. 인터페이스
// //////////////////////////////////////////////////////////////////////////////
public interface IBlockBase
{
    // 프로퍼티
    // Component
    BoxCollider collider  { get; }
    Resources   resources { get; }

    // State
    State state { get; }

    // 메서드
    // Action
    void      Idle();
    Coroutine Break(DamageType type);
}

// //////////////////////////////////////////////////////////////////////////////
// 2. 클래스
//    - Danageable 속성 -> 공격에 대한 피해를 입을 수 있음
// //////////////////////////////////////////////////////////////////////////////
public class BlockBase : MonoBehaviour, IBlockBase, IDamageable
{
    public enum State { None, Idle, Break }

    // ==============================================================================
    // 1) 필드
    // ==============================================================================
    // Component & Reference
    public new BoxCollider collider  { get; protected set; }
    public Resources       resources { get; protected set; }

    // State
    public State state { get; protected set; }

    // Setting
    [Header("Damage Setting")]
    [SerializeField] protected DamageType _mask;

    public DamageType mask { get { return _mask; } }

    // ==============================================================================
    // 2) 메서드
    //    - 클래스 확장을 위한 기반 기능만을 구현
    // ==============================================================================
    // ------------------------------------------------------------------------------
    // 2-1) 메서드 -> 이벤트 함수
    // ------------------------------------------------------------------------------
    protected virtual void Awake() { SetField(); }

    protected virtual void Start() { Idle(); }

    // ------------------------------------------------------------------------------
    // 2-2) 메서드 -> 초기화
    //    - 필드(컴포넌트 등) 초기화
    // ------------------------------------------------------------------------------
    protected virtual void SetField()
    {
        collider  = GetComponent<BoxCollider>();
        resources = new Resources(transform.Find("Resources"));
    }

    // ------------------------------------------------------------------------------
    // 2-3) 메서드 -> 액션
    // ------------------------------------------------------------------------------
    // ******************************************************************************
    // 2-3-1) 메서드 -> 액션 -> 대기(Idle)
    // ******************************************************************************
    public virtual void Idle()
    {
        state            = State.Idle;
        collider.enabled = true;

        resources.SetModel(state);
    }

    // ******************************************************************************
    // 2-3-2) 메서드 -> 액션 -> 파괴(Break)
    //    - 데미지를 입으면 오브젝트 파괴
    // ******************************************************************************
    public virtual bool TryDamage(Transform attacker, DamageType type)
    {
        if (!mask.HasFlag(type) || (state != State.Idle)) return false;

        Break(type);

        return true;
    }

    public virtual Coroutine Break(DamageType type) 
    {
        state = State.Break;

        return StartCoroutine(_Break(type, Time.timeScale <= 0f));
    }

    protected virtual IEnumerator _Break(DamageType type, bool useUnscaledTime) { yield break; }
}
