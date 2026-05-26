// //////////////////////////////////////////////////////////////////////////////
// * 요약
//    - 튜토리얼 씬 디렉터 클래스
//    - 컨트롤 가이드(UI) 표시
//
// * 목차
//    1. 인터페이스 ... Line 33
//    2. 클래스 ....... Line 50
//        1) 내부 타입 ... Line 55
//        2) 필드 ........ Line 84
//        3) 메서드 ...... Line 92
//            1- 이벤트 함수 ....... Line 95
//            2- 초기화 ............ Line 100
//            3- 들어오기(Enter) ... Line 114
//            4- 나가기(Exit) ...... Line 141
//            5- 일시정지(Pause) ... Line 151
//            6- 가이드 열기 ....... Line 164
// //////////////////////////////////////////////////////////////////////////////
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Game;
using UnityEngine.SceneManagement;

namespace Tutorial
{
    using UI = SceneDirector.UI;

    public enum GuideType { None, Move, Jump1, Jump2, HipDrop, Attack }

    // //////////////////////////////////////////////////////////////////////////////
    // 1. 인터페이스(ISceneBase 인터페이스 상속)
    // //////////////////////////////////////////////////////////////////////////////
    public interface ISceneDirector : ISceneBase 
    {
        // 프로퍼티
        // Component
        UI ui { get; }

        // Reference
        IPlanetController planet { get; }
        IPlayerController player { get; }

        // 메서드
        void OpenGuide(GuideType type);
    }

    // //////////////////////////////////////////////////////////////////////////////
    // 2. 클래스(SceneBase 클래스 상속)
    // //////////////////////////////////////////////////////////////////////////////
    public class SceneDirector : SceneBase, ISceneDirector
    {
        // ==============================================================================
        // 1) 내부 타입
        // ==============================================================================
        public class UI : List<IUIBase>
        {
            public IMainUIController  main  { get; }
            public IGuideUIController guide { get; }

            public UI(Transform transform) : base(transform.GetComponentsInChildren<IUIBase>(true))
            {
                main  = transform.GetComponentInChildren<IMainUIController>(true);
                guide = transform.GetComponentInChildren<IGuideUIController>(true);
            }

            public void Initialize() { foreach (var element in this) element.gameObject.SetActive(false); }

            public void Display(bool isActive)
            {
                main.Display(isActive);
                guide.Display(isActive);
            }

            public void Hide(bool paused)
            {
                main.root.content.gameObject.SetActive(!paused);
                guide.root.content.gameObject.SetActive(!paused);
            }
        }

        // ==============================================================================
        // 2) 필드
        // ==============================================================================
        // Component & Reference
        public UI                ui     { get; protected set; }
        public IPlanetController planet { get; protected set; }
        public IPlayerController player { get; protected set; }

        // ==============================================================================
        // 3) 메서드
        // ==============================================================================
        // ------------------------------------------------------------------------------
        // 3-1) 메서드 -> 이벤트 함수
        // ------------------------------------------------------------------------------
        protected virtual void Update() { player.state.hitPoint = player.setting.maxHitPoint; }

        // ------------------------------------------------------------------------------
        // 3-2) 메서드 -> 초기화
        // ------------------------------------------------------------------------------
        protected override void SetField()
        {
            base.SetField();

            ui     = new UI(transform.Find("UI"));
            planet = FindObjectOfType<PlanetController>(true);
            player = FindObjectOfType<PlayerController>(true);

            type = Type.Tutorial;
        }

        // ------------------------------------------------------------------------------
        // 3-3) 메서드 -> 들어오기(Enter)
        //    - 플레이어 초기화 및 배치
        // ------------------------------------------------------------------------------
        public override Coroutine Enter(Type prev = Type.None)
        {
            ui.Initialize();
            planet.Initialize();
            player.Set(planet.characterTarget);
            camera.Follow(player.cameraTarget);

            return base.Enter(prev);
        }

        protected override IEnumerator _Enter(Type prev)
        {
            IFadeUIController fadeUI = GameDirector.instance.ui.fade;

            yield return base._Enter(prev);
            yield return new WaitForSeconds(fadeUI.defaultDuration);

            ui.Display(true);
            player.Idle();

            if (Application.platform == RuntimePlatform.Android) player.ui.virtualJoystick.Display(true);
        }

        // ------------------------------------------------------------------------------
        // 3-4) 메서드 -> 나가기(Exit)
        // ------------------------------------------------------------------------------
        public override Coroutine Exit(Type next = Type.None)
        {
            next = (next == Type.None) ? Type.Lobby : next;

            return base.Exit(next);
        }

        // ------------------------------------------------------------------------------
        // 3-5) 메서드 -> 일시정지(Pause)
        // ------------------------------------------------------------------------------
        public override void Pause(bool paused, params IAudioBase[] exceptions)
        {
            base.Pause(paused, exceptions);
            ui.Hide(paused);
            player.ui.Hide(paused);

            if (paused) camera.StopFollow();
            else        camera.Follow(player.cameraTarget);
        }

        // ------------------------------------------------------------------------------
        // 3-6) 메서드 -> 가이드 열기
        //    - 현재 상황에 따른 컨트롤 가이드(UI) 표시
        // ------------------------------------------------------------------------------
        public virtual void OpenGuide(GuideType type)
        {
            ui.guide.Display(type);

            if (type == GuideType.Attack)
                foreach (var enemy in planet.enemies) enemy.Walk();
        }
    }
}
