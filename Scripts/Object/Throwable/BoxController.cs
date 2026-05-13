using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Resources  = BoxController.Resources;
using DamageType = IDamageable.Type;

// //////////////////////////////////////////////////////////////////////////////
// 1. 인터페이스(IThrowableBase 인터페이스 상속)
// //////////////////////////////////////////////////////////////////////////////
public interface IBoxController : IThrowableBase
{
    // 프로퍼티
    // Component
    new Resources resources { get; }
}

// //////////////////////////////////////////////////////////////////////////////
// 2. 클래스(ThrowableBase 클래스 상속)
// //////////////////////////////////////////////////////////////////////////////
public class BoxController : ThrowableBase, IBoxController
{
    // ==============================================================================
    // 1) 필드
    // ==============================================================================
    // Component & Reference
    public new Resources resources { get; protected set; }

    // ==============================================================================
    // 2) 메서드
    //    - 부모 클래스의 함수들을 재정의하여 확장
    // ==============================================================================
    // ------------------------------------------------------------------------------
    // 2-1) 메서드 -> 초기화
    //    - 필드(컴포넌트 등) 초기화
    // ------------------------------------------------------------------------------
    protected override void SetField()
    {
        base.SetField();

        resources = new Resources(transform.Find("Resources"));
    }

    protected override void ResetField(Rigidbody rigidbody)
    {
        base.ResetField(rigidbody);

        _mask = DamageType.Normal | DamageType.PressDown | DamageType.Explode;
    }

    // ------------------------------------------------------------------------------
    // 2-2) 메서드 -> 액션
    // ------------------------------------------------------------------------------
    // ******************************************************************************
    // 2-2-1) 메서드 -> 액션 -> 대기(Idle)
    // ******************************************************************************
    public override void Idle()
    {
        base.Idle();
        resources.models.Set(state);
    }

    // ******************************************************************************
    // 2-2-2) 메서드 -> 액션 -> 파괴(Destroy)
    // ******************************************************************************
    protected override IEnumerator _Destroy(bool useUnscaledTime)
    {
        resources.models.Set(state);

        float modelDuration  = resources.models.destroy.Play();
        float effectDuration = resources.effect.Play();
        float remainDuration = Mathf.Clamp(effectDuration - modelDuration, 0f, effectDuration);

        yield return useUnscaledTime ? new WaitForSecondsRealtime(modelDuration) : new WaitForSeconds(modelDuration);

        resources.models[state].gameObject.SetActive(false);

        yield return useUnscaledTime ? new WaitForSecondsRealtime(remainDuration) : new WaitForSeconds(remainDuration);

        Destroy(gameObject);
    }
}
