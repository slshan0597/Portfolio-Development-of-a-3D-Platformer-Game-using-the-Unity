// //////////////////////////////////////////////////////////////////////////////
// * 요약
//    - 게임 메뉴의 기반 클래스
//
// * 목차
//    1. 인터페이스 ... Line 25
//    2. 클래스 ....... Line 45
//        1) 내부 타입 ... Line 50
//        2) 필드 ........ Line 55
//        3) 메서드 ...... Line 68
//            1- 이벤트 함수 ... Line 71
//            2- 초기화 ........ Line 78
//            3- 열기(Open) .... Line 87
// //////////////////////////////////////////////////////////////////////////////
using System.Collections;
using UnityEngine;

namespace Game
{
    using Type                   = MenuBase.Type;
    using SceneType              = SceneBase.Type;
    using CursorVisibleEventType = GameDirector.CursorVisibleEventType;

    // //////////////////////////////////////////////////////////////////////////////
    // 1. 인터페이스
    // //////////////////////////////////////////////////////////////////////////////
    public interface IMenuBase
    {
        // 프로퍼티
        // Component
        GameObject gameObject { get; }
        IUIBase    ui         { get; }

        // Reference
        IGameDirector game { get; }

        // State
        Type type { get; }

        // 메서드
        Coroutine Open(bool isActive, IMenuBase prev = null);
    }

    // //////////////////////////////////////////////////////////////////////////////
    // 2. 클래스
    // //////////////////////////////////////////////////////////////////////////////
    public class MenuBase : MonoBehaviour, IMenuBase
    {
        // ==============================================================================
        // 1) 내부 타입
        // ==============================================================================
        public enum Type { None, Main, Save, Setting }

        // ==============================================================================
        // 2) 필드
        // ==============================================================================
        // Component & Reference
        public IUIBase       ui   { get; protected set; }
        public IGameDirector game { get; protected set; }

        // State
        public Type type { get; protected set; }

        // etc.
        protected IMenuBase prevMenu;

        // ==============================================================================
        // 3) 메서드
        // ==============================================================================
        // ------------------------------------------------------------------------------
        // 3-1) 메서드 -> 이벤트 함수
        // ------------------------------------------------------------------------------
        protected virtual void Awake() { SetField(); }

        protected virtual void Start() { ui.gameObject.SetActive(false); }

        // ------------------------------------------------------------------------------
        // 3-2) 메서드 -> 초기화
        // ------------------------------------------------------------------------------
        protected virtual void SetField()
        {
            ui   = transform.Find("UI").GetComponent<IUIBase>();
            game = GetComponentInParent<IGameDirector>(true);
        }

        // ------------------------------------------------------------------------------
        // 3-3) 메서드 -> 열기(Open)
        // ------------------------------------------------------------------------------
        public virtual Coroutine Open(bool isActive, IMenuBase prev = null)
        {
            IAudioController audio = game.audio;

            if (isActive)
            {
                if (prev != null)
                {
                    prevMenu = prev;

                    prevMenu.ui.gameObject.SetActive(false);
                }
                else PauseScene(true);
            }

            if      (prevMenu == null) audio.PlayMenuOpen(isActive);
            else if (!isActive)        audio.PlayButtonClick();

            return StartCoroutine(_Open(isActive));
        }

        protected virtual IEnumerator _Open(bool isActive)
        {
            yield return ui.Display(isActive);

            if (!isActive)
            {
                if (prevMenu != null)
                {
                    //prevMenu.ui.gameObject.SetActive(true);
                    prevMenu.ui.Display(true, false);

                    prevMenu = null;
                }
                else PauseScene(false);
            }
        }

        protected virtual void PauseScene(bool paused)
        {
            var        current = game.scenes.current;
            SceneType  type    = current.Key;
            ISceneBase scene   = current.Value;

            scene.Pause(paused);

            if ((type == SceneType.Stage) || (type == SceneType.Tutorial)) 
                game.SetCursorVisible(CursorVisibleEventType.MenuOpen, paused);
        }
    }
}
