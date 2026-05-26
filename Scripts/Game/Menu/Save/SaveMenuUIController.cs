// //////////////////////////////////////////////////////////////////////////////
// * 요약
//    - 게임의 세이브 메뉴의 UI 클래스
//    - 세이브 메뉴의 일반 기능(Open 등)
//
// * 목차
//    1. 인터페이스 ... Line 
//    2. 클래스 ....... Line 
// //////////////////////////////////////////////////////////////////////////////
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Game
{
    using State             = SaveMenuUIController.State;
    using Root              = SaveMenuUIController.Root;
    using SceneType         = SceneBase.Type;
    using SystemControlType = ControlSettingManager.Data.SystemType;

    // //////////////////////////////////////////////////////////////////////////////
    // 1. 인터페이스(IUIBase 인터페이스 상속)
    // //////////////////////////////////////////////////////////////////////////////
    public interface ISaveMenuUIController : IUIBase
    {
        // 프로퍼티
        // Component
        Root                  root { get; }
        ISaveListUIController list { get; }    // 세이브 파일 목록

        // Reference
        ISaveMenuManager menu { get; }

        // State
        State state { get; }
    }

    // //////////////////////////////////////////////////////////////////////////////
    // 2. 클래스(UIBase 클래스 상속)
    // //////////////////////////////////////////////////////////////////////////////
    public class SaveMenuUIController : UIBase, ISaveMenuUIController
    {
        // ==============================================================================
        // 1) 내부 타입
        // ==============================================================================
        public enum State { None, Load, Save }

        public class Root : WindowBase
        {
            public class Main : WindowBase
            {
                public ScrollRect scroll { get; }

                public Main(Transform transform) : base(transform)
                {
                    scroll = content.GetComponentInChildren<ScrollRect>(true);
                }
            }
            
            public Main      main        { get; }
            public KeyButton closeButton { get; }
            
            public Root(Transform transform) : base(transform)
            {
                main        = new Main(content.Find("Main"));
                closeButton = content.GetComponentInChildren<KeyButton>(true);
            }
        }

        // ==============================================================================
        // 2) 필드
        // ==============================================================================
        // Component & Reference
        public Root                  root { get; protected set; }
        public ISaveListUIController list { get; protected set; }
        public ISaveMenuManager      menu { get; protected set; }

        // State
        public State state { get; protected set; }

        // ==============================================================================
        // 3) 메서드
        // ==============================================================================
        // ------------------------------------------------------------------------------
        // 3-1) 메서드 -> 초기화
        // ------------------------------------------------------------------------------
        protected override void SetField()
        {
            base.SetField();

            root = new Root(transform);
            list = GetComponentInChildren<ISaveListUIController>(true);
            menu = GetComponentInParent<ISaveMenuManager>(true);
            root.closeButton.onClick.AddListener(delegate { OnClickCloseButton(); });
        }

        protected override void ResetField() { _defaultDuration = 0.5f; }

        // ------------------------------------------------------------------------------
        // 3-2) 메서드 -> 셋(Set)
        //    - 설정이 변경되면 UI 갱신
        // ------------------------------------------------------------------------------
        public override void Set()
        {
            base.Set();

            IControlSettingManager controlSetting = menu.game.menu.setting.control;

            root.closeButton.SetContent(controlSetting.GetKey(SystemControlType.Cancel));
        }

        public override void SelectFirstSelectable() { }

        protected override void SetCurrent(bool isActive) { }

        // ------------------------------------------------------------------------------
        // 3-3) 메서드 -> 표시(Display)
        // ------------------------------------------------------------------------------
        public override Coroutine Display(bool isActive, bool animated = true)
        {
            if (isActive)
            {
                gameObject.SetActive(true);
                list.gameObject.SetActive(false);

                IGameDirector game = menu.game;

                SceneType sceneType = game.scenes.current.Key;

                switch (sceneType)
                {
                    case SceneType.Lobby: state = State.Save; break;
                    default:              state = State.Load; break;
                }
            }

            return base.Display(isActive, animated);
        }

        protected override IEnumerator _Display(bool isActive, float duration)
        {
            root.closeButton.gameObject.SetActive(false);

            yield return FadeWindow(root.main, isActive, duration);

            SetInteractables(true);

            if (isActive)
            {
                list.Display(true);
                root.closeButton.gameObject.SetActive(true);
            }
            else gameObject.SetActive(false);
        }

        // ------------------------------------------------------------------------------
        // 3-4) 메서드 -> 이벤트
        // ------------------------------------------------------------------------------
        protected virtual void OnClickCloseButton() { menu.Open(false); }
    }
}
