// //////////////////////////////////////////////////////////////////////////////
// * 목차
//    1. 인터페이스 ... Line 
//    2. 클래스 ....... Line 
//        1) 필드 ..... Line 
//        2) 메서드 ... Line 
//            1- 초기화 ... Line 
//            2- 액션 ..... Line 
//                1_ 등장(Appear) ... Line 
//                2_ 죽기(Die) ...... Line 
// //////////////////////////////////////////////////////////////////////////////
using System.Collections;
using UnityEngine;

using Game;

namespace Stage
{
    using BossState        = BossBase.State.Main;
    using BGMType          = BackGroundMusicController.SoundType;
    using LetterboxUIState = LetterboxUIController.State;

    // //////////////////////////////////////////////////////////////////////////////
    // 1. 인터페이스(IBoomBoomController 인터페이스 상속)
    // //////////////////////////////////////////////////////////////////////////////
    public interface IBoomBoomController : global::IBoomBoomController
    {
        // 프로퍼티
        // Reference
        new IBossPlanetController planet { get; }
    }

    // //////////////////////////////////////////////////////////////////////////////
    // 2. 클래스(BoomBoomController 클래스 상속)
    // //////////////////////////////////////////////////////////////////////////////
    public class BoomBoomController : global::BoomBoomController
    {
        // ==============================================================================
        // 1) 필드
        // ==============================================================================
        // Component & Reference
        public new IBossPlanetController planet { get; protected set; }

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

            planet = GetComponentInParent<IBossPlanetController>(true);
        }

        // ------------------------------------------------------------------------------
        // 2-2) 메서드 -> 액션
        // ------------------------------------------------------------------------------
        // ******************************************************************************
        // 2-2-1) 메서드 -> 액션 -> 등장(Appear)
        //    - 필드(Planet) 초기화
        //    - Stage 씬의 UI 호출
        // ******************************************************************************
        public override Coroutine Appear()
        {
            gameObject.SetActive(true);
            planet.Set(BossState.Appear);

            resources.voice.audioSource.spatialBlend = 0f;

            foreach (var effect in resources.effects.elements) effect.audioSource.spatialBlend = 0f;

            return base.Appear();
        }

        protected override IEnumerator _Appear(bool useUnscaledTime)
        {
            ILetterboxUIController     letterboxUI = GameDirector.instance.ui.letterbox;
            ISceneDirector             scene       = planet.level.stage.scene;
            IBackGroundMusicController bgm         = scene.audios.bgm;

            yield return letterboxUI.DisplayWithSkipButton(bossSetting.appear.duration);

            scene.Pause(false);
            bgm.Play(BGMType.Boss);
            Idle(letterboxUI.state == LetterboxUIState.Closed);
            letterboxUI.Display(LetterboxUIState.Open, false);
        }

        protected override void StopAppear()
        {
            base.StopAppear();

            resources.voice.audioSource.spatialBlend = 1f;

            foreach (var effect in resources.effects.elements) effect.audioSource.spatialBlend = 1f;
        }

        // ******************************************************************************
        // 2-2-2) 메서드 -> 액션 -> 죽기(Die)
        //    - 필드(Planet) 클리어 트리거 기능 추가
        // ******************************************************************************
        public override Coroutine Die()
        {
            planet.Set(BossState.Die);

            resources.voice.audioSource.spatialBlend = 0f;

            foreach (var effect in resources.effects.elements) effect.audioSource.spatialBlend = 0f;

            return base.Die();
        }

        protected override IEnumerator _Die()
        {
            yield return base._Die();

            planet.Clear(false);
        }

        protected override void StopDie()
        {
            base.StopDie();

            resources.voice.audioSource.spatialBlend = 1f;

            foreach (var effect in resources.effects.elements) effect.audioSource.spatialBlend = 1f;
        }
    }
}
