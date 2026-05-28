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
//                1_ 파괴(Break) ... Line 
// //////////////////////////////////////////////////////////////////////////////
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Resources  = BlockController.Resources;
using DamageType = IDamageable.Type;

// //////////////////////////////////////////////////////////////////////////////
// 1. 인터페이스(IBlockBase 인터페이스 상속)
// //////////////////////////////////////////////////////////////////////////////
public interface IBlockController : IBlockBase 
{
    // 프로퍼티
    // Component
    new Resources resources { get; }

    // Setting
    float breakDelay { get; }
}

// //////////////////////////////////////////////////////////////////////////////
// 2. 클래스(BlockBase 클래스 상속)
// //////////////////////////////////////////////////////////////////////////////
public class BlockController : BlockBase, IBlockController
{
    // ==============================================================================
    // 1) 필드
    // ==============================================================================
    // Component & Reference
    public new Resources resources { get; protected set; }

    // Setting
    [Header("Block Setting")]
    [SerializeField] protected float _breakDelay;

    public float breakDelay { get { return _breakDelay; } }

    // ==============================================================================
    // 2) 메서드
    //    - 부모 클래스의 함수들을 재정의하여 확장
    // ==============================================================================
    // ------------------------------------------------------------------------------
    // 2-1) 메서드 -> 이벤트 함수
    // ------------------------------------------------------------------------------
    protected virtual void Reset() { ResetField(); }

    // ------------------------------------------------------------------------------
    // 2-2) 메서드 -> 초기화
    // ------------------------------------------------------------------------------
    protected override void SetField()
    {
        base.SetField();

        resources = new Resources(transform.Find("Resources"));
    }

    protected virtual void ResetField() { _breakDelay = 0.1f; }

    // ------------------------------------------------------------------------------
    // 2-2) 메서드 -> 액션
    // ------------------------------------------------------------------------------
    // ******************************************************************************
    // 2-2-1) 메서드 -> 액션 -> 파괴(Break)
    // ******************************************************************************
    protected override IEnumerator _Break(DamageType type, bool useUnscaledTime)
    {
        if (type != DamageType.PressDown)
            yield return useUnscaledTime ? new WaitForSecondsRealtime(breakDelay) : new WaitForSeconds(breakDelay);

        collider.enabled = false;

        resources.SetModel(state);

        float modelDuration  = resources.models._break.Play();
        float effectDuration = resources.effect.Play();
        float remainDuration = Mathf.Clamp(effectDuration - modelDuration, 0f, effectDuration);

        yield return useUnscaledTime ? new WaitForSecondsRealtime(modelDuration) : new WaitForSeconds(modelDuration);

        resources.models[state].gameObject.SetActive(false);

        yield return useUnscaledTime ? new WaitForSecondsRealtime(remainDuration) : new WaitForSeconds(remainDuration);

        Destroy(gameObject);
    }
}
