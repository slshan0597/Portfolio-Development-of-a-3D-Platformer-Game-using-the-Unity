// //////////////////////////////////////////////////////////////////////////////
// * 요약
//    - Rewardable 속성 추가
//    - 활성화 시 전용 BGM 재생
//
// * 목차
//    1. 인터페이스 ... Line 23
//    2. 클래스 ....... Line 32
//        1) 필드 ..... Line 38
//        2) 메서드 ... Line 45
//            1- 초기화 ........ Line 48
//            2- 대기(Idle) .... Line 59
//            3- 생성(Spawn) ... Line 71
// //////////////////////////////////////////////////////////////////////////////
using System.Collections;
using UnityEngine;

namespace Stage
{
    using BGMType = BackGroundMusicController.SoundType;

    // //////////////////////////////////////////////////////////////////////////////
    // 1. 인터페이스(global::IGoalController 인터페이스 상속)
    // //////////////////////////////////////////////////////////////////////////////
    public interface IGoalController : global::IGoalController
    {
        // 프로퍼티
        ISceneDirector scene { get; }
    }

    // //////////////////////////////////////////////////////////////////////////////
    // 2. 클래스(global::GoalController 클래스 상속)
    //    - Rewardable : 필드(플래닛) 클리어 시 활성화 되는 속성
    // //////////////////////////////////////////////////////////////////////////////
    public class GoalController : global::GoalController, IGoalController, IRewardable
    {
        // ==============================================================================
        // 1) 필드
        // ==============================================================================
        // Component & Reference
        public ISceneDirector    scene  { get; protected set; }
        public IPlanetController planet { get; protected set; }

        // ==============================================================================
        // 2) 메서드
        // ==============================================================================
        // ------------------------------------------------------------------------------
        // 2-1) 메서드 -> 초기화
        // ------------------------------------------------------------------------------
        protected override void SetField()
        {
            base.SetField();

            scene  = FindObjectOfType<SceneDirector>(true);
            planet = GetComponentInParent<IPlanetController>(true);
        }

        // ------------------------------------------------------------------------------
        // 2-2) 메서드 -> 대기(Idle)
        // ------------------------------------------------------------------------------
        public override Coroutine Idle()
        {
            resources.effects[State.Idle].SetUnscaledTime(false);

            resources.model.animator.updateMode = AnimatorUpdateMode.Normal;

            return base.Idle();
        }

        // ------------------------------------------------------------------------------
        // 2-3) 메서드 -> 생성(Spawn)
        //    - 활성화 시 메인 BGM을 중지하고 전용 BGM 재생
        // ------------------------------------------------------------------------------
        public virtual Coroutine Appear() { return Spawn(); }

        protected override IEnumerator _Spawn(Transform spawner, bool useUnscaledTime)
        {
            IBackGroundMusicController bgm = scene.audios.bgm;

            yield return base._Spawn(spawner, useUnscaledTime);

            bgm.Play(BGMType.BeforeGoal);
        }
    }
}
