// //////////////////////////////////////////////////////////////////////////////
// * 요약
//    - 타이틀 씬의 메뉴의 UI 클래스
//    - 시작 버튼 클릭 시 튜토리얼 씬으로 전환
//    - 로드 버튼 클릭 시 로드 메뉴 오픈
//    - 세팅 버튼 클릭 시 세팅 메뉴 오픈
//
// * 목차
//    1. 인터페이스 ... Line 
//    2. 클래스 ....... Line 
//        1) 필드 ..... Line 
//        2) 메서드 ... Line 
//            1- 이벤트 함수 ... Line 
//            2- 초기화 ........ Line 
//            3- 열기(Open) .... Line 
// //////////////////////////////////////////////////////////////////////////////
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Game;

namespace Title
{
    using Root           = MenuUIController.Root;
    using Type           = MenuUIController.Root.Type;
    using ConfirmUIState = ConfirmUIController.State;
    using SceneType      = SceneBase.Type;

    // //////////////////////////////////////////////////////////////////////////////
    // 1. 인터페이스(IUIBase 인터페이스 상속)
    // //////////////////////////////////////////////////////////////////////////////
    public interface IMenuUIController : IUIBase
    {
        // 프로퍼티
        // Component
        Root root { get; }

        // Reference
        IMenuManager menu { get; }
    }

    // //////////////////////////////////////////////////////////////////////////////
    // 2. 클래스(UIBase 클래스 상속)
    // //////////////////////////////////////////////////////////////////////////////
    public class MenuUIController : UIBase, IMenuUIController
    {
        // ==============================================================================
        // 1) 내부 타입
        // ==============================================================================
        public class Root : MenuBase
        {
            public enum Type { Start, Load, Setting, Exit }

            public Dictionary<Type, SoundButton> buttons { get; }

            public Root(Transform transform) : base(transform)
            {
                buttons = new Dictionary<Type, SoundButton>();

                foreach (var button in content.GetComponentsInChildren<SoundButton>(true))
                {
                    string name = button.name.Replace(" ", string.Empty).Replace("Button", string.Empty);

                    if (Enum.TryParse(name, out Type type)) buttons.Add(type, button);
                }
            }
        }

        // ==============================================================================
        // 2) 필드
        // ==============================================================================
        // Component & Reference
        public Root         root { get; protected set; }
        public IMenuManager menu { get; protected set; }

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
            menu = GetComponentInParent<IMenuManager>(true);

            foreach (var element in root.buttons)
            {
                var type   = element.Key;
                var button = element.Value;

                button.onClick.AddListener(delegate { OnClickButton(type); });
            }
        }

        // ------------------------------------------------------------------------------
        // 3-2) 메서드 -> 이벤트
        //    - 버튼 타입에 따라 기능 호출
        // ------------------------------------------------------------------------------
        protected virtual void OnClickButton(Type type)
        {
            IGameDirector       game        = GameDirector.instance;
            ISaveMenuManager    saveMenu    = game.menu.save;
            ISettingMenuManager settingMenu = game.menu.setting;

            switch (type)
            {
                case Type.Start:   saveMenu.Load(-1);         break;
                case Type.Load:    saveMenu.Open(true);       break;
                case Type.Setting: settingMenu.Open(true);    break;
                case Type.Exit:    StartCoroutine(TryExit()); break;
            }
        }

        protected virtual IEnumerator TryExit()
        {
            IConfirmUIController confirmUI = GameDirector.instance.ui.confirm;
            ISceneDirector       scene     = menu.scene;

            yield return confirmUI.Display("Exit", string.Empty, this);

            if (confirmUI.state == ConfirmUIState.Cancel) yield break;

            scene.Exit(SceneType.None);
        }
    }
}
