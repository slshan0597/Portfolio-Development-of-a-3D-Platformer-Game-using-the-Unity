// //////////////////////////////////////////////////////////////////////////////
// * 요약
//    - 기반 클래스(BlockBase)의 확장 클래스
//    - 피격 시 조건부 아이템 드롭
//
// * 목차
//    1. 인터페이스 ... Line 25
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

using Resources  = ItemBlockController.Resources;
using DamageType = IDamageable.Type;

// //////////////////////////////////////////////////////////////////////////////
// 1. 인터페이스(IBlockBase 인터페이스 상속)
// //////////////////////////////////////////////////////////////////////////////
public interface IItemBlockController : IBlockBase
{
    // 프로퍼티
    // Component
    new Resources resources { get; }

    // Reference
    IPlanetController planet { get; }

    // Setting
    GameObject dropItem { get; }
    int        maxCount { get; }
    float      lifeSpan { get; }
}

// //////////////////////////////////////////////////////////////////////////////
// 2. 클래스(BlockBase 클래스 상속)
// //////////////////////////////////////////////////////////////////////////////
public class ItemBlockController : BlockBase, IItemBlockController
{
    // ==============================================================================
    // 1) 필드
    // ==============================================================================
    // Component & Reference
    public new Resources     resources { get; protected set; }
    public IPlanetController planet    { get; protected set; }

    // Setting
    [Header("Block Setting")]
    [SerializeField] protected GameObject _dropItem;
    [SerializeField] protected int        _maxCount;
    [SerializeField] protected float      _lifeSpan;

    public GameObject dropItem { get { return _dropItem; } }
    public int        maxCount { get { return _maxCount; } }
    public float      lifeSpan { get { return _lifeSpan; } }

    // etc.
    protected float elapsedTime;
    protected int   count;

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
        planet    = GetComponentInParent<IPlanetController>(true);
    }

    protected virtual void ResetField()
    {
        _mask     = DamageType.Normal | DamageType.PressDown | DamageType.Explode | DamageType.Head;
        _maxCount = 1;
        _lifeSpan = 10f;
    }

    // ------------------------------------------------------------------------------
    // 2-3) 메서드 -> 액션
    // ------------------------------------------------------------------------------
    // ******************************************************************************
    // 2-3-1) 메서드 -> 액션 -> 파괴(Break)
    //    - 피격 시 조건부 아이템 드롭, 하나 이상의 조건 종료 시 기능 정지
    //    - Max Count : 최대 아이템 드롭 횟수
    //    - Life Span : 첫 아이템 드롭부터 시작하여 드롭 가능한 시간
    // ******************************************************************************
    protected override IEnumerator _Break(DamageType type, bool useUnscaledTime)
    {
        if (count++ <= 0) StartCoroutine(SetTimer(lifeSpan));

        float duration = Mathf.Max(resources.models.idle.Play(), resources.effect.Play());

        yield return useUnscaledTime ? new WaitForSecondsRealtime(duration) : new WaitForSeconds(duration);

        Vector3 topPosition = transform.TransformPoint(Vector3.up * collider.size.y);
        var     item        = Instantiate(dropItem, topPosition, transform.rotation, planet.objects)
            .GetComponent<IItemBase>();

        item.Spawn(transform);

        if ((count < maxCount) && (elapsedTime < lifeSpan)) Idle();
        else                                                resources.SetModel(state);
    }

    protected virtual IEnumerator SetTimer(float duration)
    {
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            yield return null;
        }
    }
}
