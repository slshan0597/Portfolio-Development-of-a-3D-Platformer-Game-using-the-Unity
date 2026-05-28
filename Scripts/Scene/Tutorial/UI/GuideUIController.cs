// //////////////////////////////////////////////////////////////////////////////
// * 요약
//    - 튜토리얼 씬의 가이드 UI 클래스
//    - 현재 상황에 따른 컨트롤 가이드 표시
//
// * 목차
//    1. 인터페이스 ... Line 28
//    2. 클래스 ....... Line 47
//        1) 내부 타입 ... Line 52
//        2) 필드 ........ Line 127
//        3) 메서드 ...... Line 140
//            1- 초기화 .......... Line 143
//            2- 셋(Set) ......... Line 156
//            3- 표시(Display) ... Line 168
// //////////////////////////////////////////////////////////////////////////////
using UnityEngine;
using UnityEngine.UI;

using Game;

namespace Tutorial
{
    using Root            = GuideUIController.Root;
    using ControlState    = ControlSettingManager.State;
    using PlayerMainState = PlayerController.State.Main;

    // //////////////////////////////////////////////////////////////////////////////
    // 1. 인터페이스(IUIBase 인터페이스 상속)
    // //////////////////////////////////////////////////////////////////////////////
    public interface IGuideUIController : IUIBase
    {
        // 프로퍼티
        // Component
        Root root { get; }

        // Reference
        ISceneDirector scene { get; }

        // State
        GuideType state { get; }

        // 메서드
        Coroutine Display(GuideType type);
    }

    // //////////////////////////////////////////////////////////////////////////////
    // 2. 클래스(UIBase 클래스 상속)
    // //////////////////////////////////////////////////////////////////////////////
    public class GuideUIController : UIBase, IGuideUIController
    {
        // ==============================================================================
        // 1) 내부 타입
        // ==============================================================================
        public class Root : MenuBase
        {
            public class Main : WindowBase
            {
                public Text contentText { get; }

                public Main(Transform transform) : base(transform)
                {
                    contentText = content.GetComponentInChildren<Text>(true);
                }
                
                public void SetContent(GuideType type, IControlSettingManager controlSetting)
                {
                    ControlState state       = controlSetting.state;
                    string       jumpKey     = (state == ControlState.Touch) ? "Jump"     : controlSetting.GetKey(PlayerMainState.Jump).ToString();
                    string       crouchKey   = (state == ControlState.Touch) ? "Crouch"   : controlSetting.GetKey(PlayerMainState.Crouch).ToString();
                    string       attackKey   = (state == ControlState.Touch) ? "Attack"   : controlSetting.GetKey(PlayerMainState.Attack).ToString();
                    string       interactKey = (state == ControlState.Touch) ? "Interact" : controlSetting.GetKey(PlayerMainState.Interact).ToString();

                    switch (type)
                    {
                        case GuideType.Move:
                            {
                                string moveKey = string.Empty;

                                switch (state)
                                {
                                    case ControlState.Keyboard: moveKey = "'W' 'A' 'S' 'D'"; break;
                                    case ControlState.Joystick: moveKey = "LS";              break;
                                }

                                contentText.text = $"Move\t\t: '{moveKey}'\n"
                                                 + $"Crouch\t: '{crouchKey}'\n"
                                                 + $"Interact\t: '{interactKey}'";
                            }
                            break;

                        case GuideType.Jump1:
                            {
                                contentText.text = $"Jump\t\t\t: '{jumpKey}'\n"
                                                 + $"High Jump\t: '{jumpKey}' -> '{jumpKey}' -> '{jumpKey}'\n"
                                                 + $"Back Jump\t: (Stop Move) -> '{crouchKey}' -> '{jumpKey}'";
                            }
                            break;

                        case GuideType.Jump2:
                            {
                                contentText.text = $"Long Jump\t: (Move) -> '{crouchKey}' -> '{jumpKey}'";
                            }
                            break;

                        case GuideType.HipDrop:
                            {
                                contentText.text = $"Hip Drop\t: '{jumpKey}' -> '{crouchKey}'";
                            }
                            break;

                        case GuideType.Attack:
                            {
                                contentText.text = $"Attack\t: '{attackKey}'\n"
                                                 + $"Dive\t\t: (Hip Drop) -> '{attackKey}'";
                            }
                            break;
                    }
                }
            }

            public Main main { get; }

            public Root(Transform transform) : base(transform) { main = new Main(content.Find("Main")); }
        }

        // ==============================================================================
        // 2) 필드
        // ==============================================================================
        // Component & Reference
        public Root           root  { get; protected set; }
        public ISceneDirector scene { get; protected set; }

        // State
        public GuideType state { get; protected set; }

        // etc.
        protected Coroutine displayAction;

        // ==============================================================================
        // 3) 메서드
        // ==============================================================================
        // ------------------------------------------------------------------------------
        // 3-1) 메서드 -> 초기화
        // ------------------------------------------------------------------------------
        protected override void SetField()
        {
            base.SetField();

            root  = new Root(transform);
            scene = GetComponentInParent<ISceneDirector>(true);
        }

        protected override void ResetField() { _defaultDuration = 0.5f; }

        // ------------------------------------------------------------------------------
        // 3-2) 메서드 -> 셋(Set)
        // ------------------------------------------------------------------------------
        public override void Set()
        {
            base.Set();

            IControlSettingManager controlSetting = GameDirector.instance.menu.setting.control;

            root.main.SetContent(state, controlSetting);
        }

        // ------------------------------------------------------------------------------
        // 3-3) 메서드 -> 표시(Display)
        // ------------------------------------------------------------------------------
        public override Coroutine Display(bool isActive, bool animated = true)
        {
            if (displayAction != null)
            {
                StopCoroutine(displayAction);

                displayAction = null;
            }

            gameObject.SetActive(true);

            if (isActive) state = GuideType.Move;

            Set();

            return displayAction = StartCoroutine(FadeWindow(root.main, isActive, defaultDuration));
        }

        public virtual Coroutine Display(GuideType type)
        {
            if (displayAction != null)
            {
                StopCoroutine(displayAction);

                displayAction = null;
            }

            gameObject.SetActive(true);

            state = type;

            Set();

            return displayAction = StartCoroutine(FadeWindow(root.main, true, defaultDuration));
        }
    }
}
