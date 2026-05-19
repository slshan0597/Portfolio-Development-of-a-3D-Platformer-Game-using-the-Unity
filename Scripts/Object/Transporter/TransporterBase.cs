// //////////////////////////////////////////////////////////////////////////////
// * 요약
//    - 플레이어를 지정된 위치로 이동시키는 오브젝트에 대한 기반 클래스
//    - 필드(Planet)간 포탈 기능
//
// * 목차
//    1. 인터페이스 ... Line 31
//    2. 클래스 ....... Line 59
//        1) 내부 타입 ... Line 65
//        2) 필드 ........ Line 79
//        3) 메서드 ...... Line 91
//            1- 이벤트 함수 ... Line 95
//            2- 초기화 ........ Line 103
//            3- 액션 .......... Line 114
//                1_ 대기(Idle) ............ Line 117
//                2_ 활성화(Appear) ........ Line 130
//                3_ 비활성화(Disappear) ... Line 150
//                4_ 전송(Transport) ....... Line 170
// //////////////////////////////////////////////////////////////////////////////
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Resources = TransporterBase.Resources;
using State     = TransporterBase.State;
using MainState = TransporterBase.State.Main;
using Type      = TransporterBase.Type;

// //////////////////////////////////////////////////////////////////////////////
// 1. 인터페이스
// //////////////////////////////////////////////////////////////////////////////
public interface ITransporterBase
{
    // 프로퍼티
    // Component
    GameObject gameObject { get; }
    Transform  transform  { get; }
    Resources  resources  { get; }

    // Reference
    ISceneBase scene { get; }

    // State
    State state { get; }

    // Setting
    Type type { get; }

    // 메서드
    // Action
    void      Idle();
    Coroutine Appear();
    Coroutine Disappear();
    Coroutine Transport(IPlayerController player);
}

// //////////////////////////////////////////////////////////////////////////////
// 2. 클래스
//    - Interactable 속성 -> 플레이어에 의해 상호작용될 수 있음
// //////////////////////////////////////////////////////////////////////////////
public class TransporterBase : MonoBehaviour, ITransporterBase, IInteractable
{
    // ==============================================================================
    // 1) 내부 타입
    // ==============================================================================
    public enum Type { None, Launcher, Pipe }

    public class State
    {
        public enum Main { None, Idle, Appear, Disappear, Transport }
        public enum Sub  { None, Start, Loop, End }

        public Main main;
        public Sub  sub;
    }

    // ==============================================================================
    // 2) 필드
    // ==============================================================================
    // Component & Reference
    public SphereCollider trigger   { get; protected set; }
    public Resources      resources { get; protected set; }
    public ISceneBase     scene     { get; protected set; }

    public State state { get; protected set; } = new State();

    public Type type { get; protected set; }

    // ==============================================================================
    // 3) 메서드
    //    - 클래스 확장을 위한 기반 기능만을 구현
    // ==============================================================================
    // ------------------------------------------------------------------------------
    // 3-1) 메서드 -> 이벤트 함수
    //    - 오브젝트 초기화
    // ------------------------------------------------------------------------------
    protected virtual void Awake() { SetField(); }

    protected virtual void Start() { if (state.main != MainState.Appear) Idle(); }

    // ------------------------------------------------------------------------------
    // 3-2) 메서드 -> 초기화
    //    - 필드(컴포넌트 등) 초기화
    // ------------------------------------------------------------------------------
    protected virtual void SetField()
    {
        trigger   = Array.Find(GetComponents<SphereCollider>(), collider => collider.isTrigger);
        resources = new Resources(transform.Find("Resources"));
        scene     = FindObjectOfType<SceneBase>(true);
    }

    // ------------------------------------------------------------------------------
    // 3-3) 메서드 -> 액션
    // ------------------------------------------------------------------------------
    // ******************************************************************************
    // 3-3-1) 메서드 -> 액션 -> 대기(Idle)
    // ******************************************************************************
    public virtual void Idle() 
    {
        state.main      = MainState.Idle;
        trigger.enabled = true;

        resources.Play(state.main);

        resources.model.animator.updateMode = AnimatorUpdateMode.Normal;
    }

    // ******************************************************************************
    // 3-3-2) 메서드 -> 액션 -> 활성화(Appear)
    // ******************************************************************************
    public virtual Coroutine Appear()
    {
        state.main      = MainState.Appear;
        trigger.enabled = false;

        float duration = resources.Play(state.main);

        return StartCoroutine(_Appear(duration, Time.timeScale <= 0f));
    }

    protected virtual IEnumerator _Appear(float duration, bool useUnscaledTime)
    {
        yield return useUnscaledTime ? new WaitForSecondsRealtime(duration) : new WaitForSeconds(duration);

        Idle();
    }

    // ******************************************************************************
    // 3-3-3) 메서드 -> 액션 -> 비활성화(Disappear)
    // ******************************************************************************
    public virtual Coroutine Disappear()
    {
        state.main      = MainState.Disappear;
        trigger.enabled = false;

        float duration = resources.Play(state.main);

        return StartCoroutine(_Disappear(duration, Time.timeScale <= 0f));
    }

    protected virtual IEnumerator _Disappear(float duration, bool useUnscaledTime) 
    {
        yield return useUnscaledTime ? new WaitForSecondsRealtime(duration) : new WaitForSeconds(duration);

        gameObject.SetActive(false);
    }

    // ******************************************************************************
    // 3-3-4) 메서드 -> 액션 -> 전송(Transport)
    // ******************************************************************************
    public Coroutine Interact(IPlayerController player) { return Transport(player); }

    public void StopInteract(IPlayerController player) { }

    public virtual Coroutine Transport(IPlayerController player)
    {
        state.main                   = MainState.Transport;
        trigger.enabled              = false;
        player.rigidbody.isKinematic = true;

        player.resources.Play(type);

        return StartCoroutine(_Transport(player));
    }

    protected virtual IEnumerator _Transport(IPlayerController player) { yield break; }
}
