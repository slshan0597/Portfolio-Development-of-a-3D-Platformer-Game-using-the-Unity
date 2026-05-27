// //////////////////////////////////////////////////////////////////////////////
// * 요약
//    - 게임의 로비의 캐릭터 메뉴의 UI 클래스
//    - 재화 및 캐릭터 강화 리스트 표시(기능은 리스트 UI에서 구현)
//    - 뷰(View) 모드에서의 캐릭터 둘러보기 기능
//
// * 목차
//    1. 인터페이스 ... Line 
//    2. 클래스 ....... Line 
//        1) 내부 타입 ... Line 
//            1- 공통(General) ... Line 
//            2- 메인 ............ Line 
//            3- 뷰(View) ........ Line 
//        2) 필드 ..... Line 
//        3) 메서드 ... Line 
//            1- 초기화 .......... Line 
//            2- 셋(Set) ......... Line 
//            3- 표시(Display) ... Line 
//                1_ 메인 ....... Line 
//                2_ 뷰(View) ... Line 
//            4- 이벤트 ... Line 
//                1_ 드래그(화면) ... Line 
//                2_ 클릭(버튼) ..... Line 
// //////////////////////////////////////////////////////////////////////////////
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

using Game;

namespace Lobby
{
    using Root              = CharacterMenuUIController.Root;
    using GeneralType       = CharacterMenuUIController.Root.General.Type;
    using MoneyData         = DataManager.Money;
    using SystemControlType = ControlSettingManager.Data.SystemType;

    // //////////////////////////////////////////////////////////////////////////////
    // 1. 인터페이스(IUIBase 인터페이스 상속)
    // //////////////////////////////////////////////////////////////////////////////
    public interface ICharacterMenuUIController : IUIBase
    {
        // 프로퍼티
        // Component
        Root                           root { get; }
        ICharacterMenuListUIController list { get; }

        // Reference
        ICharacterMenuManager menu { get; }

        // 메서드
        Coroutine DisplayMain(bool isActive);
        Coroutine DisplayView(bool isActive);
    }

    // //////////////////////////////////////////////////////////////////////////////
    // 2. 클래스(UIBase 클래스 상속)
    // //////////////////////////////////////////////////////////////////////////////
    public class CharacterMenuUIController : UIBase, ICharacterMenuUIController, IDragHandler
    {
        // ==============================================================================
        // 1) 내부 타입 - 구조
        // ==============================================================================
        public class Root : WindowBase
        {
            // ------------------------------------------------------------------------------
            // 1-1) 내부 타입 - 구조 -> 공통(General)
            //    - 공통으로 사용되는 UI 구조
            // ------------------------------------------------------------------------------
            public class General : MenuBase
            {
                // 내부 타입 - 공통
                public enum Type { Close, View, Reset }

                // 필드 - 공통
                public Dictionary<Type, KeyButton> buttons { get; }

                // 생성자 - 공통
                public General(Transform transform) : base(transform)
                {
                    buttons = new Dictionary<Type, KeyButton>();

                    foreach (var button in content.GetComponentsInChildren<KeyButton>(true))
                    {
                        string name = button.name.Replace(" ", string.Empty).Replace("Button", string.Empty);

                        if (Enum.TryParse(name, out Type type)) buttons.Add(type, button);
                    }
                }

                // 메서드 - 공통
                public void SetContent(IControlSettingManager controlSetting)
                {
                    foreach (var element in buttons)
                    {
                        Type type   = element.Key;
                        var  button = element.Value;

                        switch (type)
                        {
                            case GeneralType.Close: button.SetContent(controlSetting.GetKey(SystemControlType.Cancel)); break;
                            case GeneralType.View:  button.SetContent(controlSetting.GetKey(SystemControlType.Start));  break;
                            case GeneralType.Reset: button.SetContent(controlSetting.GetKey(SystemControlType.Start));  break;
                        }
                    }
                }
            }

            // ------------------------------------------------------------------------------
            // 1-2) 내부 타입 - 구조 -> 메인
            //    - 캐릭터 강화 리스트 표시에 대한 UI 구조
            // ------------------------------------------------------------------------------
            public class Main : MenuBase
            {
                // 내부 타입 - 메인
                public class Stat : WindowBase
                {
                    public ScrollRect list { get; }

                    public Stat(Transform transform) : base(transform)
                    {
                        list = content.GetComponentInChildren<ScrollRect>(true);
                    }
                }

                public class Option : MenuBase
                {
                    public class Money : WindowBase
                    {
                        public Text contentText { get; }

                        public Money(Transform transform) : base(transform)
                        {
                            contentText = content.GetComponentInChildren<Text>(true);
                        }

                        public void SetContent(MoneyData moneyData)
                        {
                            int money = moneyData.value;

                            contentText.text = $"Money\t: {money.ToString("#,##0")}";
                        }
                    }

                    public Money   money   { get; }
                    public General general { get; }

                    public Option(Transform transform) : base(transform)
                    {
                        money   = new Money(content.Find("Money"));
                        general = new General(content.Find("General"));
                    }

                    public void SetContent(MoneyData moneyData, IControlSettingManager controlSetting)
                    {
                        money.SetContent(moneyData);
                        general.SetContent(controlSetting);
                    }
                }

                // 필드 - 메인
                public Stat   stat   { get; }
                public Option option { get; }

                // 생성자 - 메인
                public Main(Transform transform) : base(transform)
                {
                    stat   = new Stat(content.Find("Stat"));
                    option = new Option(content.Find("Option"));
                }
            }

            // ------------------------------------------------------------------------------
            // 1-3) 내부 타입 - 구조 -> 뷰(View)
            //    - 뷰 모드에 대한 UI 구조
            // ------------------------------------------------------------------------------
            public class View : MenuBase
            {
                // 필드 - 뷰
                public General general { get; }

                // 생성자 - 뷰
                public View(Transform transform) : base(transform) { general = new General(content.Find("General")); }
            }

            // 필드
            public Main main { get; }
            public View view { get; }

            // 생성자
            public Root(Transform transform)  : base(transform)
            {
                main = new Main(content.Find("Main"));
                view = new View(content.Find("View"));
            }

            // 메서
            public void SetContent(MoneyData moneyData, IControlSettingManager controlSetting)
            {
                main.option.SetContent(moneyData, controlSetting);
                view.general.SetContent(controlSetting);
            }
        }

        // ==============================================================================
        // 2) 필드
        // ==============================================================================
        // Component & Reference
        public Root                           root { get; protected set; }
        public ICharacterMenuListUIController list { get; protected set; }
        public ICharacterMenuManager          menu { get; protected set; }

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
            list = GetComponentInChildren<ICharacterMenuListUIController>(true);
            menu = GetComponentInParent<ICharacterMenuManager>(true);

            foreach (var element in root.main.option.general.buttons)
                element.Value.onClick.AddListener(delegate { OnClickGeneralButton(element.Key, false); });

            foreach (var element in root.view.general.buttons)
                element.Value.onClick.AddListener(delegate { OnClickGeneralButton(element.Key, true); });
        }

        // ------------------------------------------------------------------------------
        // 3-2) 메서드 -> 셋(Set)
        // ------------------------------------------------------------------------------
        public override void Set()
        {
            base.Set();

            IGameDirector          game           = GameDirector.instance;
            IDataManager           data           = game.data;
            IControlSettingManager controlSetting = game.menu.setting.control;

            root.SetContent(data.money, controlSetting);
        }

        public override void SelectFirstSelectable() { }

        protected override void SetCurrent(bool isActive) { }

        // ------------------------------------------------------------------------------
        // 3-3) 메서드 -> 표시(Display)
        // ------------------------------------------------------------------------------
        protected override IEnumerator _Display(bool isActive, float duration)
        {
            root.view.gameObject.SetActive(false);

            StartCoroutine(FadeBackGroundImage(root, isActive, duration));

            yield return _DisplayMain(isActive, duration);

            if (!isActive) gameObject.SetActive(false);
        }

        // ******************************************************************************
        // 3-3-1) 메서드 -> 표시 -> 메인
        //    - 메인(캐릭터 강화) 메뉴의 표시
        // ******************************************************************************
        public virtual Coroutine DisplayMain(bool isActive)
        {
            SetInteractables(false);

            return StartCoroutine(_DisplayMain(isActive, defaultDuration));
        }

        protected virtual IEnumerator _DisplayMain(bool isActive, float duration)
        {
            var main = root.main;

            if (isActive) main.gameObject.SetActive(true);

            list.gameObject.SetActive(false);

            StartCoroutine(FadeWindow(main.stat, isActive, duration));

            yield return FadeContent(main.option, isActive, duration);

            SetInteractables(true);

            if (isActive) list.Display(true);
            else          main.gameObject.SetActive(false);
        }

        // ******************************************************************************
        // 3-3-2) 메서드 -> 표시 -> 뷰(View)
        //    - 뷰 모드 메뉴의 표시
        // ******************************************************************************
        public virtual Coroutine DisplayView(bool isActive)
        {
            root.view.gameObject.SetActive(true);
            Set();
            SetInteractables(false);

            return StartCoroutine(_DisplayView(isActive, defaultDuration));
        }

        protected virtual IEnumerator _DisplayView(bool isActive, float duration)
        {
            yield return FadeContent(root.view, isActive, duration);

            SetInteractables(true);
        }

        // ------------------------------------------------------------------------------
        // 3-4) 메서드 -> 이벤트
        // ------------------------------------------------------------------------------
        // ******************************************************************************
        // 3-4-1) 메서드 -> 이벤트 -> 드래그(화면)
        //    - 화면을 드래그 시 카메라를 회전하여 캐릭터 둘러보
        // ******************************************************************************
        public virtual void OnDrag(PointerEventData eventData)
        {
            ICharacterViewCameraController camera = menu.scene.cameras.characterView;

            if (camera.gameObject.activeInHierarchy) camera.Rotate(eventData.delta);
        }

        // ******************************************************************************
        // 3-4-2) 메서드 -> 이벤트 -> 클릭(버튼)
        //    - 공통 버튼의 기능 수행
        // ******************************************************************************
        protected virtual void OnClickGeneralButton(GeneralType type, bool isViewMode)
        {
            switch (type)
            {
                case GeneralType.Close:
                    {
                        if (isViewMode) menu.OpenViewMenu(false);
                        else
                        {
                            IMainMenuManager mainMenu = menu.scene.menu.main;

                            mainMenu.Open(true, menu);
                        }
                    }
                    break;

                case GeneralType.View: menu.OpenViewMenu(true); break;
                case GeneralType.Reset:
                    {
                        ICharacterViewCameraController camera = menu.scene.cameras.characterView;

                        camera.Initialize();
                    }
                    break;
            }
        }
    }
}
