// //////////////////////////////////////////////////////////////////////////////
// - 출구 기능만을 가진 오브젝트
// - 해당 지점으로 이동 후 비활성화
//
// * 목차
//    1. 인터페이스 ... Line 33
//    2. 클래스 ....... Line 55
//        1) 정의 ..... Line 60
//        2) 필드 ..... Line 70
//        3) 메서드 ... Line 87
//            1- 이벤트 함수 ... Line 91
//            2- 초기화 ........ Line 107
//            3- 액션 .......... Line 133
//                1_ 대기(Idle) ............ Line 136
//                2_ 활성화(Appear) ........ Line 147
//                3_ 비활성화(Disappear) ... Line 159
//                4_ 전송(Transport) ....... Line 169
//                    1-> 준비(Ready) ... Line 193
//                    2-> 입장(Enter) ... Line 203
//                    3-> 대기(Wait) .... Line 216
//                    4-> 퇴장(Exit) .... Line 228
// //////////////////////////////////////////////////////////////////////////////
using System;
using System.Collections;
using UnityEngine;

using JumpSetting     = ExitPipeController.JumpSetting;
using PlayerLandState = PlayerController.State.Land;
using PlayerJumpState = PlayerController.State.Jump;

// //////////////////////////////////////////////////////////////////////////////
// 1. 인터페이스(IPipeController 인터페이스 상속)
// //////////////////////////////////////////////////////////////////////////////
public interface IExitPipeController : IPipeController
{
    // 프로퍼티
    // Setting
    JumpSetting jumpSetting { get; }
}

// //////////////////////////////////////////////////////////////////////////////
// 2. 클래스(PipeController 클래스 상속)
// //////////////////////////////////////////////////////////////////////////////
public class ExitPipeController : PipeController, IExitPipeController
{
    // ==============================================================================
    // 1) 정의
    // ==============================================================================
    [Serializable] public struct JumpSetting
    {
        [SerializeField]                 private float _force;
        [SerializeField, Range(0f, 90f)] private float _angle;

        public float force { get { return _force; } }
        public float angle { get { return _angle; } }
        
        public JumpSetting(float force, float angle)
        {
            _force = force;
            _angle = angle;
        }

        public Vector3 GetForce(IPlayerController player)
        {
            float   angle     = this.angle * Mathf.Deg2Rad;
            Vector3 direction = (Vector3.forward * Mathf.Cos(angle)) + (Vector3.up * Mathf.Sin(angle));
            Vector3 force     = Mathf.Sqrt(this.force) * direction;

            return player.transform.TransformDirection(force);
        }
    }

    // ==============================================================================
    // 2) 필드
    // ==============================================================================
    // Setting
    [SerializeField] protected JumpSetting _jumpSetting;

    public JumpSetting jumpSetting { get { return _jumpSetting; } }

    // etc.
    protected Coroutine jumpAction;

    // ==============================================================================
    // 3) 메서드
    //    - 부모 클래스의 함수들을 재정의하여 확장
    // ==============================================================================
    // ------------------------------------------------------------------------------
    // 3-1) 메서드 -> 이벤트 함수
    //    - 오브젝트 초기화
    // ------------------------------------------------------------------------------
    protected override void Start()
    {
        base.Start();
        gameObject.SetActive(false);
    }

    // ------------------------------------------------------------------------------
    // 3-2) 메서드 -> 초기화
    //    - 필드(컴포넌트 등) 초기화
    // ------------------------------------------------------------------------------
    protected override void ResetField()
    {
        base.ResetField();

        _jumpSetting = new JumpSetting(250f, 75f);
    }

    // ------------------------------------------------------------------------------
    // 3-3) 메서드 -> 액션
    // ------------------------------------------------------------------------------
    // ******************************************************************************
    // 3-3-1) 메서드 -> 액션 -> 비활성화(Disappear)
    // ******************************************************************************
    protected override IEnumerator _Disappear(float duration, bool useUnscaledTime)
    {
        yield return useUnscaledTime ? new WaitForSecondsRealtime(duration) : new WaitForSeconds(duration);
    }

    // ******************************************************************************
    // 3-3-2) 메서드 -> 액션 -> 퇴장(Exit)
    //    - 해당 지점으로 이동 후 오브젝트 비활성화(Disappear)
    // ******************************************************************************
    public override Coroutine Exit(IPlayerController player)
    {
        gameObject.SetActive(true);

        return base.Exit(player);
    }

    protected override IEnumerator _Exit(IPlayerController player)
    {
        yield return Appear();
        yield return base._Exit(player);
        yield return JumpAndLand(player);
        yield return Disappear();

        gameObject.SetActive(false);
    }

    protected virtual IEnumerator JumpAndLand(IPlayerController player)
    {
        Vector3 force = jumpSetting.GetForce(player);

        player.rigidbody.AddForce(force, ForceMode.VelocityChange);
        player.resources.Play(PlayerJumpState.Low);

        int count    = 0;
        int maxCount = 5;

        while (player.groundState.isGrounded && (count < maxCount))
        {
            yield return new WaitForFixedUpdate();
            count++;
        }

        while (!player.groundState.isGrounded) yield return new WaitForFixedUpdate();

        player.resources.Play(PlayerLandState.Light);
    }
}
