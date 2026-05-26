// //////////////////////////////////////////////////////////////////////////////
// * 요약
//    - 타이틀 씬의 UI 클래스
//    - 타이틀 표시 및 메인 메뉴 호출
//
// * 목차
//    1. 인터페이스 ... Line 
//    2. 클래스 ....... Line 
//        1) 내부 타입 ... Line 
//        1) 필드 ........ Line 
//        2) 메서드 ...... Line 
//            1- 이벤트 함수 ....... Line 
//            2- 초기화 ............ Line 
//            3- 들어오기(Enter) ... Line 
//            4- 나가기(Exit) ...... Line 
//            5- 일시정지(Pause) ... Line 
// //////////////////////////////////////////////////////////////////////////////
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

using Game;

namespace Title
{
    using Root         = UIController.Root;
    using ControlState = ControlSettingManager.State;

    // //////////////////////////////////////////////////////////////////////////////
    // 1. 인터페이스(IUIBase 인터페이스 상속)
    // //////////////////////////////////////////////////////////////////////////////
    public interface IUIController : IUIBase
    {
        // 프로퍼티
        // Component
        public Root root { get; }

        // Reference
        ISceneDirector scene { get; }
    }

    // //////////////////////////////////////////////////////////////////////////////
    // 2. 클래스(UIBase 클래스 상속)
    // //////////////////////////////////////////////////////////////////////////////
    public class UIController : UIBase, IUIController
    {
        // ==============================================================================
        // 1) 내부 타입
        // ==============================================================================
        public class Root : MenuBase
        {
            public class Window : MenuBase
            {
                public Text text { get; }

                public Window(Transform transform) : base(transform)
                {
                    text = content.GetComponentInChildren<Text>(true);
                }
            }
            
            public Window label { get; }
            public Window input { get; }

            public Root(Transform transform) : base(transform)
            {
                label = new Window(content.Find("Label"));
                input = new Window(content.Find("Input"));
            }
        }

        // ==============================================================================
        // 2) 필드
        // ==============================================================================
        // Component & Reference
        public Root           root  { get; protected set; }
        public ISceneDirector scene { get; protected set; }

        // ==============================================================================
        // 3) 메서드
        // ==============================================================================
        // ------------------------------------------------------------------------------
        // 3-1) 메서드 -> 이벤트 함수
        // ------------------------------------------------------------------------------
        protected override void SetField()
        {
            base.SetField();

            root  = new Root(transform);
            scene = GetComponentInParent<ISceneDirector>(true);
        }

        protected override void ResetField() { _defaultDuration = 1f; }

        // ------------------------------------------------------------------------------
        // 3-2) 메서드 -> 셋(Set)
        // ------------------------------------------------------------------------------
        public override void Set()
        {
            base.Set();

            IControlSettingManager controlSetting = GameDirector.instance.menu.setting.control;

            switch (controlSetting.state)
            {
                case ControlState.Touch: root.input.text.text = "- Touch Screen -";     break;
                default:                 root.input.text.text = "- Press Any Button -"; break;
            }
        }

        // ------------------------------------------------------------------------------
        // 3-3) 메서드 -> 표시(Display)
        // ------------------------------------------------------------------------------
        protected override IEnumerator _Display(bool isActive, float duration)
        {
            if (!isActive) gameObject.SetActive(false);

            IGameDirector          game           = GameDirector.instance;
            IAudioController       audio          = game.audio;
            IControlSettingManager controlSetting = game.menu.setting.control;

            root.label.gameObject.SetActive(true);
            root.input.gameObject.SetActive(false);

            yield return FadeGraphics(root.label, true, duration);

            root.input.gameObject.SetActive(true);

            yield return FadeGraphics(root.input, true, duration);

            while (true)
            {
                switch (controlSetting.state)
                {
                    case ControlState.Touch: if (Input.touchCount > 0) goto End; break;
                    default:                 if (Input.anyKeyDown)     goto End; break;
                }

                yield return null;
            }

            End:

            StartCoroutine(FadeContent(root.label, false, duration));
            audio.PlayGameStart();

            yield return FadeContent(root.input, false, duration);

            gameObject.SetActive(false);
        }
    }
}
