// //////////////////////////////////////////////////////////////////////////////
// * 요약
//    - 타이틀 씬 디렉터 클래스
//
// * 목차
//    1. 인터페이스 ... Line 26
//    2. 클래스 ....... Line 37
//        1) 필드 ..... Line 42
//        2) 메서드 ... Line 49
//            1- 이벤트 함수 ....... Line 52
//            2- 초기화 ............ Line 57
//            3- 들어오기(Enter) ... Line 70
//            4- 나가기(Exit) ...... Line 96
//            5- 일시정지(Pause) ... Line 106
// //////////////////////////////////////////////////////////////////////////////
using System.Collections;
using UnityEngine;

using Game;

namespace Title
{
    using CursorVisibleEventType = GameDirector.CursorVisibleEventType;

    // //////////////////////////////////////////////////////////////////////////////
    // 1. 인터페이스(ISceneBase 인터페이스 상속)
    // //////////////////////////////////////////////////////////////////////////////
    public interface ISceneDirector : ISceneBase
    {
        // 프로퍼티
        // Component
        IMenuManager  menu { get; }
        IUIController ui   { get; }
    }

    // //////////////////////////////////////////////////////////////////////////////
    // 2. 클래스(SceneBase 클래스 상속)
    // //////////////////////////////////////////////////////////////////////////////
    public class SceneDirector : SceneBase, ISceneDirector
    {
        // ==============================================================================
        // 1) 필드
        // ==============================================================================
        // Component & Reference
        public IMenuManager  menu { get; protected set; }
        public IUIController ui   { get; protected set; }

        // ==============================================================================
        // 2) 메서드
        // ==============================================================================
        // ------------------------------------------------------------------------------
        // 2-1) 메서드 -> 이벤트 함수
        // ------------------------------------------------------------------------------
        protected virtual void Update() { camera.transform.Rotate(Vector3.up, 5f * Time.deltaTime); }

        // ------------------------------------------------------------------------------
        // 2-2) 메서드 -> 초기화
        // ------------------------------------------------------------------------------
        protected override void SetField()
        {
            base.SetField();

            menu = GetComponentInChildren<IMenuManager>(true);
            ui   = GetComponentInChildren<IUIController>(true);

            type = Type.Title;
        }

        // ------------------------------------------------------------------------------
        // 2-3) 메서드 -> 들어오기(Enter)
        //    - 타이틀(UI) 표시
        //    - 메인 메뉴 오픈
        // ------------------------------------------------------------------------------
        public override Coroutine Enter(Type prev)
        {
            menu.Initialize();
            ui.gameObject.SetActive(false);

            return base.Enter(prev);
        }

        protected override IEnumerator _Enter(Type prev) 
        {
            IGameDirector game = GameDirector.instance;

            yield return base._Enter(prev);

            game.SetCursorVisible(CursorVisibleEventType.MenuOpen, true);

            yield return ui.Display(true);

            menu.Open(true);
        }

        // ------------------------------------------------------------------------------
        // 2-4) 메서드 -> 나가기(Exit)
        // ------------------------------------------------------------------------------
        public override Coroutine Exit(Type next)
        {
            menu.Open(false);

            return base.Exit(next);
        }

        // ------------------------------------------------------------------------------
        // 2-5) 메서드 -> 일시정지(Pause)
        // ------------------------------------------------------------------------------
        public override void Pause(bool paused, params IAudioBase[] exceptions)
        {
            base.Pause(paused, exceptions);
            menu.ui.SetInteractables(!paused);
        }
    }
}
