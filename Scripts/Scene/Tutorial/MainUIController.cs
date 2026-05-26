// //////////////////////////////////////////////////////////////////////////////
// * 요약
//    - 튜토리얼 씬의 메인 UI 클래스
//
// * 목차
//    1. 인터페이스 ... Line 
//    2. 클래스 ....... Line 
//        1) 내부 타입 ... Line 
//        2) 필드 ........ Line 
//        3) 메서드 ...... Line 
//            1- 초기화 .... Line 
//            2- 셋(Set) ... Line 
//            3- 이벤트 .... Line 
// //////////////////////////////////////////////////////////////////////////////
using UnityEngine;

using Game;

namespace Tutorial
{
    using Root              = MainUIController.Root;
    using SystemControlType = ControlSettingManager.Data.SystemType;

    // //////////////////////////////////////////////////////////////////////////////
    // 1. 인터페이스(IUIBase 인터페이스 상속)
    // //////////////////////////////////////////////////////////////////////////////
    public interface IMainUIController : IUIBase
    {
        // 프로퍼티
        // Component
        Root root { get; }

        // Reference
        ISceneDirector scene { get; }
    }

    // //////////////////////////////////////////////////////////////////////////////
    // 2. 클래스(UIBase 클래스 상속)
    // //////////////////////////////////////////////////////////////////////////////
    public class MainUIController : UIBase, IMainUIController
    {
        // ==============================================================================
        // 1) 내부 타입
        // ==============================================================================
        public class Root : MenuBase
        {
            public KeyButton menuButton { get; }

            public Root(Transform transform) : base(transform)
            {
                menuButton = content.GetComponentInChildren<KeyButton>(true);
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
        // 3-1) 메서드 -> 초기화
        // ------------------------------------------------------------------------------
        protected override void SetField()
        {
            base.SetField();

            root  = new Root(transform);
            scene = GetComponentInParent<ISceneDirector>(true);

            root.menuButton.onClick.AddListener(OnClickButton);
        }

        // ------------------------------------------------------------------------------
        // 3-2) 메서드 -> 셋(Set)
        // ------------------------------------------------------------------------------
        public override void Set()
        {
            base.Set();

            IControlSettingManager controlSetting = GameDirector.instance.menu.setting.control;

            root.menuButton.SetContent(controlSetting.GetKey(SystemControlType.Pause));
        }

        public override void SelectFirstSelectable() { }

        protected override void SetCurrent(bool isActive) { }

        // ------------------------------------------------------------------------------
        // 3-3) 메서드 -> 이벤트
        // ------------------------------------------------------------------------------
        protected virtual void OnClickButton()
        {
            IMainMenuManager gameMenu = GameDirector.instance.menu.main;

            gameMenu.Open(true);
        }
    }
}
