// //////////////////////////////////////////////////////////////////////////////
// * 요약
//    - 기반 클래스(TransporterBase)의 확장 클래스
//    - 플레이어를 연결된 다른 Pipe 오브젝트(Exit)로 바로 이동
//
// * 목차
//    1. 인터페이스 ... Line 34
//    2. 클래스 ....... Line 56
//        1) 정의 ..... Line 61
//        2) 필드 ..... Line 71
//        3) 메서드 ... Line 88
//            1- 이벤트 함수 ... Line 92
//            2- 초기화 ........ Line 108
//            3- 액션 .......... Line 134
//                1_ 대기(Idle) ............ Line 137
//                2_ 활성화(Appear) ........ Line 148
//                3_ 비활성화(Disappear) ... Line 160
//                4_ 전송(Transport) ....... Line 170
//                    1-> 준비(Ready) ... Line 194
//                    2-> 입장(Enter) ... Line 204
//                    3-> 대기(Wait) .... Line 217
//                    4-> 퇴장(Exit) .... Line 229
// //////////////////////////////////////////////////////////////////////////////
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using State          = PipeController.State;
using TransportState = PipeController.State.Transport;
using MainState      = TransporterBase.State.Main;

// //////////////////////////////////////////////////////////////////////////////
// 1. 인터페이스(ITransporterBase 인터페이스 상속)
// //////////////////////////////////////////////////////////////////////////////
public interface IPipeController : ITransporterBase
{
    // 프로퍼티
    // Component
    Collider                   collider        { get; }
    ICharacterTargetController characterTarget { get; }

    // State
    new State state { get; }

    // Setting
    IPipeController                   exitPipe  { get; }
    SimpleData<TransportState, float> durations { get; }

    // 메서드
    // Action
    Coroutine Exit(IPlayerController player);
}

// //////////////////////////////////////////////////////////////////////////////
// 2. 클래스(TransporterBase 클래스 상속)
// //////////////////////////////////////////////////////////////////////////////
public class PipeController : TransporterBase, IPipeController
{
    // ==============================================================================
    // 1) 정의
    // ==============================================================================
    public new class State : TransporterBase.State
    {
        public enum Transport { None, Ready, Enter, Wait, Exit }

        public Transport transport;
    }

    // ==============================================================================
    // 2) 필드
    // ==============================================================================
    // Component & Reference
    public new Collider               collider        { get; protected set; }
    public ICharacterTargetController characterTarget { get; protected set; }

    // State
    public new State state { get; protected set; } = new State();

    // Setting
    [SerializeField] protected PipeController                    _exitPipe;
    [SerializeField] protected SimpleData<TransportState, float> _durations;

    public IPipeController                   exitPipe  { get { return _exitPipe; } }
    public SimpleData<TransportState, float> durations { get { return _durations; } }

    // ==============================================================================
    // 3) 메서드
    //    - 부모 클래스의 함수들을 재정의하여 확장
    // ==============================================================================
    // ------------------------------------------------------------------------------
    // 3-1) 메서드 -> 이벤트 함수
    //    - 오브젝트 초기화
    //    - 연결된 다음 지점(Exit)간의 기즈모 생성
    // ------------------------------------------------------------------------------
    protected virtual void Reset() { ResetField(); }

    protected virtual void OnDrawGizmosSelected()
    {
        if (exitPipe == null) return;

        Gizmos.color = Color.white;

        Gizmos.DrawLine(transform.position, exitPipe.transform.position);
    }

    // ------------------------------------------------------------------------------
    // 3-2) 메서드 -> 초기화
    //    - 필드(컴포넌트 등) 초기화
    // ------------------------------------------------------------------------------
    protected override void SetField()
    {
        base.SetField();

        collider        = Array.Find(GetComponents<Collider>(), collider => !collider.isTrigger);
        characterTarget = GetComponentInChildren<ICharacterTargetController>(true);

        type = Type.Pipe;
    }

    protected virtual void ResetField()
    {
        _durations = new SimpleData<TransportState, float>(
            new List<SimpleData<TransportState, float>.Element>()
            {
                    new SimpleData<TransportState, float>.Element(TransportState.Ready, 0.25f),
                    new SimpleData<TransportState, float>.Element(TransportState.Enter, 1f),
                    new SimpleData<TransportState, float>.Element(TransportState.Wait,  1f),
                    new SimpleData<TransportState, float>.Element(TransportState.Exit,  1f)
            });
    }

    // ------------------------------------------------------------------------------
    // 3-3) 메서드 -> 액션
    // ------------------------------------------------------------------------------
    // ******************************************************************************
    // 3-3-1) 메서드 -> 액션 -> 대기(Idle)
    // ******************************************************************************
    public override void Idle()
    {
        base.Idle();

        state.main      = MainState.Idle;
        state.transport = TransportState.None;
    }

    // ******************************************************************************
    // 3-3-2) 메서드 -> 액션 -> 활성화(Appear)
    // ******************************************************************************
    public override Coroutine Appear()
    {
        gameObject.SetActive(true);

        state.main = MainState.Appear;

        return base.Appear();
    }

    // ******************************************************************************
    // 3-3-3) 메서드 -> 액션 -> 비활성화(Disappear)
    // ******************************************************************************
    public override Coroutine Disappear()
    {
        state.main = MainState.Disappear;

        return base.Disappear();
    }

    // ******************************************************************************
    // 3-3-4) 메서드 -> 액션 -> 전송(Transport)
    //    - 플레이어를 연결된 지점(Exit)으로 바로 이동
    //    - 루틴: 준비(Ready) -> 입장(Enter) -> 대기(Wait) -> 퇴장(Exit)
    //    - 퇴장 루틴(함수)은 연결된 지점 오브젝트로 넘어가서 호출됨
    // ******************************************************************************
    public override Coroutine Transport(IPlayerController player)
    {
        state.main = MainState.Transport;

        return base.Transport(player);
    }

    protected override IEnumerator _Transport(IPlayerController player)
    {
        yield return Ready(player);
        yield return Enter(player);
        yield return Wait(player);

        Idle();

        yield return exitPipe.Exit(player);    // 연결된 지점으로 넘어가서 호출
    }

    // ++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
    // 3-3-4-1) 메서드 -> 액션 -> 전송(Transport) -> 준비(Ready)
    // ++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
    protected virtual IEnumerator Ready(IPlayerController player)
    {
        state.transport = TransportState.Ready;

        yield return player.Set(characterTarget, false, false, durations[state.transport]);
    }

    // ++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
    // 3-3-4-2) 메서드 -> 액션 -> 전송(Transport) -> 입장(Enter)
    // ++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
    protected virtual IEnumerator Enter(IPlayerController player)
    {
        state.transport = TransportState.Enter;

        resources.effects.Play(state.main);
        player.resources.model.PlayNext();

        yield return new WaitForSeconds(durations[state.transport]);
    }

    // ++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
    // 3-3-4-3) 메서드 -> 액션 -> 전송(Transport) -> 대기(Wait)
    // ++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
    protected virtual IEnumerator Wait(IPlayerController player)
    {
        state.transport = TransportState.Wait;

        player.resources.model.meshes.gameObject.SetActive(false);

        yield return new WaitForSeconds(durations[state.transport]);
    }

    // ++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
    // 3-3-4-4) 메서드 -> 액션 -> 전송(Transport) -> 퇴장(Exit)
    //    - 출발 지점이 아닌 연결된 지점(Exit)에서 호출됨
    // ++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
    public virtual Coroutine Exit(IPlayerController player)
    {
        player.Set(characterTarget, true);

        return StartCoroutine(_Exit(player));
    }

    protected virtual IEnumerator _Exit(IPlayerController player)
    {
        state.main      = MainState.Transport;
        state.transport = TransportState.Exit;

        resources.effects.Play(state.main);
        player.resources.model.meshes.gameObject.SetActive(true);
        player.resources.model.PlayNext();

        yield return new WaitForSeconds(durations[state.transport]);

        player.rigidbody.isKinematic = false;

        player.resources.model.PlayNext();

        Idle();
    }
}
