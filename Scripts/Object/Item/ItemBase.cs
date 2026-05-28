// //////////////////////////////////////////////////////////////////////////////
// * 요약
//    - 아이템 오브젝트에 대한 기반 클래스
//
// * 목차
//    1. 인터페이스 ... Line 27
//    2. 클래스 ....... Line 51
//        1) 필드 ..... Line 58
//        2) 메서드 ... Line 75
//            1- 이벤트 함수 ... Line 79
//            2- 초기화 ........ Line 94
//            3- 타이머 ........ Line 104
//            4- 액션 .......... Line 135
//                1_ 대기(Idle) .... Line 138
//                2_ 생성(Spawn) ... Line 163
//                3_ 획득(Get) ..... Line 201
// //////////////////////////////////////////////////////////////////////////////
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using State     = ItemBase.State;
using Resources = ItemBase.Resources;

// //////////////////////////////////////////////////////////////////////////////
// 1. 인터페이스
// //////////////////////////////////////////////////////////////////////////////
public interface IItemBase
{
    // 프로퍼티
    // Component
    SphereCollider trigger   { get; }
    Resources      resources { get; }

    // State
    State state { get; }

    // Setting
    float lifeSpan { get; }
    float modelRPS { get; }

    // 메서드
    // Action
    Coroutine Idle();
    Coroutine Spawn(Transform spawner = null);
    Coroutine Get(IPlayerController player);
}

// //////////////////////////////////////////////////////////////////////////////
// 2. 클래스
// //////////////////////////////////////////////////////////////////////////////
public class ItemBase : MonoBehaviour, IItemBase
{
    public enum State { None, Idle, Spawn, Get }

    // ==============================================================================
    // 1) 필드
    // ==============================================================================
    // Component & Reference
    public SphereCollider trigger   { get; protected set; }
    public Resources      resources { get; protected set; }

    // State
    public State state { get; protected set; }

    // Setting
    [SerializeField] protected float _modelRPS;
    [SerializeField] protected float _lifeSpan;

    public float modelRPS { get { return _modelRPS; } }
    public float lifeSpan { get { return _lifeSpan; } }

    // ==============================================================================
    // 2) 메서드
    //    - 클래스 확장을 위한 기반 기능만을 구현
    // ==============================================================================
    // ------------------------------------------------------------------------------
    // 2-1) 메서드 -> 이벤트 함수
    //    - 플레이어와 충돌 시 아이템 획득
    // ------------------------------------------------------------------------------
    protected virtual void Awake() { SetField(); }

    protected virtual void Start() { if (state != State.Spawn) Idle(); }

    protected virtual void OnTriggerEnter(Collider other)
    {
        if (other.isTrigger || !other.TryGetComponent(out IPlayerController player)) return;

        Get(player);
    }

    // ------------------------------------------------------------------------------
    // 2-2) 메서드 -> 초기화
    //    - 필드(컴포넌트 등) 초기화
    // ------------------------------------------------------------------------------
    protected virtual void SetField()
    {
        trigger   = GetComponent<SphereCollider>();
        resources = new Resources(transform.Find("Resources"));
    }

    // ------------------------------------------------------------------------------
    // 2-3) 메서드 -> 타이머
    //    - 지정된 시간이 지나면 오브젝트 파괴
    // ------------------------------------------------------------------------------
    protected virtual IEnumerator SetTimer()
    {
        float step = lifeSpan * 0.2f;

        yield return new WaitForSeconds(step * 3f);
        yield return BlinkModel(step, 4);
        yield return BlinkModel(step, 8);

        Destroy(gameObject);
    }

    protected virtual IEnumerator BlinkModel(float duration, int count)
    {
        float step = duration / (count * 2);

        for (int i = 0; i < count; i++)
        {
            resources.model.gameObject.SetActive(false);

            yield return new WaitForSeconds(step);

            resources.model.gameObject.SetActive(true);

            yield return new WaitForSeconds(step);
        }
    }

    // ------------------------------------------------------------------------------
    // 2-4) 메서드 -> 액션
    // ------------------------------------------------------------------------------
    // ******************************************************************************
    // 2-4-1) 메서드 -> 액션 -> 대기(Idle)
    // ******************************************************************************
    public virtual Coroutine Idle()
    {
        state           = State.Idle;
        trigger.enabled = true;

        return StartCoroutine(_Idle());
    }

    protected virtual IEnumerator _Idle()
    {
        if (modelRPS <= 0f) yield break;

        Vector3 rotation = Vector3.up * 360f * modelRPS;

        while (true)
        {
            resources.model.transform.Rotate(rotation * Time.deltaTime);

            yield return null;
        }
    }

    // ******************************************************************************
    // 2-4-2) 메서드 -> 액션 -> 생성(Spawn)
    //    - 적이 사망하거나 아이템 블록 피격 시 호출됨
    //    - 생성과 동시에 타이머 시작
    // ******************************************************************************
    public virtual Coroutine Spawn(Transform spawner = null) 
    {
        state           = State.Spawn;
        trigger.enabled = false;

        return StartCoroutine(_Spawn(spawner, Time.timeScale <= 0f));
    }

    protected virtual IEnumerator _Spawn(Transform spawner, bool useUnscaledTime)
    {
        float   height        = trigger.radius * 2f;
        Vector3 startPosition = Vector3.down * height;
        Vector3 endPosition   = Vector3.zero;
        float   duration      = resources.effects[state].Play();
        float   elapsedTime   = 0f;

        while (elapsedTime < duration)
        {
            float rate = elapsedTime / duration;

            resources.transform.localPosition = Vector3.Lerp(startPosition, endPosition, rate);

            elapsedTime += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            yield return null;
        }

        resources.transform.localPosition = endPosition;

        if (lifeSpan > 0f) StartCoroutine(SetTimer());

        Idle();
    }

    // ******************************************************************************
    // 2-4-3) 메서드 -> 액션 -> 획득(Get)
    //    - 플레이어와 충돌 시 파괴
    // ******************************************************************************
    public virtual Coroutine Get(IPlayerController player) 
    {
        StopAllCoroutines();

        state           = State.Get;
        trigger.enabled = false;

        return StartCoroutine(_Get(player, Time.timeScale <= 0f));
    }

    protected virtual IEnumerator _Get(IPlayerController player, bool useUnscaledTime) 
    {
        resources.model.gameObject.SetActive(false);

        float duration = resources.effects[state].Play();

        yield return useUnscaledTime ? new WaitForSecondsRealtime(duration) : new WaitForSeconds(duration);

        Destroy(gameObject);
    }
}
