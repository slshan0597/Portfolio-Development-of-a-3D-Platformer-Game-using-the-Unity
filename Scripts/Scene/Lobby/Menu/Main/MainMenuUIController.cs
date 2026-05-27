// //////////////////////////////////////////////////////////////////////////////
// * 요약
//    - 게임의 로비의 메인 메뉴의 UI 클래스
//    - 캐릭터, 스테이지 메뉴 진입 통로
//
// * 목차
//    1. 인터페이스 ... Line 
//    2. 클래스 ....... Line 
//        1) 내부 타입 ... Line 
//        2) 필드 ........ Line 
//        3) 메서드 ...... Line 
//            1- 초기화 .......... Line 
//            2- 셋(Set) ......... Line 
//            3- 표시(Display) ... Line 
//                1_ 메인 ........... Line 
//                2_ 숨기기(Hide) ... Line 
//            4- 이벤트 .......... Line 
//                1_ 드래그(화면) ... Line 
//                2_ 클릭(버튼) ..... Line 
// //////////////////////////////////////////////////////////////////////////////
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

using Game;

namespace Lobby
{
    using Root              = MainMenuUIController.Root;
    using GeneralType       = MainMenuUIController.Root.General.Type;
    using Type              = MenuBase.Type;
    using MarkType          = MarkButton.MarkType;
    using PlayerEmoteType   = PlayerModelController.EmoteType;
    using SystemControlType = ControlSettingManager.Data.SystemType;

    // //////////////////////////////////////////////////////////////////////////////
    // 1. 인터페이스(IUIBase 인터페이스 상속)
    // //////////////////////////////////////////////////////////////////////////////
    public interface IMainMenuUIController : IUIBase
    {
        // 프로퍼티
        // Component
        Root root { get; }

        // Reference
        IMainMenuManager menu { get; }

        // 메서드
        Coroutine DisplayMain(bool isActive);
        Coroutine DisplayHide(bool isActive);
    }

    // //////////////////////////////////////////////////////////////////////////////
    // 2. 클래스(UIBase 클래스 상속)
    // //////////////////////////////////////////////////////////////////////////////
    public class MainMenuUIController : UIBase, IMainMenuUIController, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        // ==============================================================================
        // 1) 내부 타입 - 구조
        // ==============================================================================
        public class Root : WindowBase
        {
            public class General : MenuBase
            {
                public enum Type { Menu, Hide, Close }

                public Dictionary<Type, KeyButton> buttons { get; }

                public General(Transform transform) : base(transform)
                {
                    buttons = new Dictionary<Type, KeyButton>();

                    foreach (var button in content.GetComponentsInChildren<KeyButton>(true))
                    {
                        string name = button.name.Replace(" ", string.Empty).Replace("Button", string.Empty);

                        if (Enum.TryParse(name, out Type type)) buttons.Add(type, button);
                    }
                }
                
                public void SetContent(IControlSettingManager controlSetting)
                {
                    foreach (var element in buttons)
                    {
                        Type type   = element.Key;
                        var  button = element.Value;

                        switch (type)
                        {
                            case GeneralType.Menu:  button.SetContent(controlSetting.GetKey(SystemControlType.Pause));  break;
                            case GeneralType.Hide:  button.SetContent(controlSetting.GetKey(SystemControlType.Start));  break;
                            case GeneralType.Close: button.SetContent(controlSetting.GetKey(SystemControlType.Cancel)); break;
                        }
                    }
                }
            }

            public class Main : MenuBase
            {
                public class Option : MenuBase
                {
                    public Dictionary<Type, MarkButton> buttons { get; }

                    public Option(Transform transform) : base(transform)
                    {
                        buttons = new Dictionary<Type, MarkButton>();

                        foreach (var button in content.GetComponentsInChildren<MarkButton>(true))
                        {
                            string name = button.name.Replace(" ", string.Empty).Replace("Button", string.Empty);

                            if (Enum.TryParse(name, out Type type)) buttons.Add(type, button);
                        }
                    }
                    
                    public void SetContent(IDataManager data)
                    {
                        foreach (var element in buttons)
                        {
                            Type type   = element.Key;
                            var  button = element.Value;

                            switch (type)
                            {
                                case Type.Character:
                                    {
                                        int      money     = data.money.value;
                                        var      statDatas = data.characterStats.Values;
                                        MarkType mark
                                            = statDatas.Any(data => (data.value < data.maxCount) && (money >= data.cost))
                                            ? MarkType.Update : MarkType.None;

                                        button.SetContent(mark);
                                    }
                                    break;

                                case Type.Stage:
                                    {
                                        var      stageDatas = data.stages;
                                        var      levelDatas = stageDatas.Current.levels;
                                        MarkType mark
                                            = (stageDatas.Any(data => data.playable && !data.selected && !data.cleared)
                                            || levelDatas.Any(data => data.playable && !data.cleared))
                                            ? MarkType.Update : MarkType.None;

                                        button.SetContent(mark);
                                    }
                                    break;
                            }
                        }
                    }
                }

                public Option  option  { get; }
                public General general { get; }

                public Main(Transform transform) : base(transform)
                {
                    option  = new Option(content.Find("Option"));
                    general = new General(content.Find("General"));
                }

                public void SetContent(IDataManager data, IControlSettingManager controlSetting)
                {
                    option.SetContent(data);
                    general.SetContent(controlSetting);
                }
            }

            public class Hide : MenuBase
            {
                public General general { get; }

                public Hide(Transform transform) : base(transform) { general = new General(content.Find("General")); }
            }

            public Main main { get; }
            public Hide hide { get; }
            
            public Root(Transform transform) : base(transform)
            {
                main = new Main(content.Find("Main"));
                hide = new Hide(content.Find("Hide"));
            }
            
            public void SetContent(IDataManager data, IControlSettingManager controlSetting)
            {
                main.SetContent(data, controlSetting);
                hide.general.SetContent(controlSetting);
            }
        }

        // ==============================================================================
        // 2) 필드
        // ==============================================================================
        // Component & Reference
        public Root             root { get; protected set; }
        public IMainMenuManager menu { get; protected set; }

        // etc.
        protected bool isDisplaying = false;

        protected Coroutine timerAction;

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
            menu = GetComponentInParent<IMainMenuManager>(true);

            foreach (var element in root.main.option.buttons)
                element.Value.onClick.AddListener(delegate { OnClickMenuButton(element.Key); });

            foreach (var element in root.main.general.buttons)
                element.Value.onClick.AddListener(delegate { OnClickGeneralButton(element.Key); });

            foreach (var element in root.hide.general.buttons)
                element.Value.onClick.AddListener(delegate { OnClickGeneralButton(element.Key); });
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

            root.SetContent(data, controlSetting);
        }

        protected virtual IEnumerator SetTimer()
        {
            IPlayerController player = menu.scene.player;

            yield return new WaitForSeconds(10f);

            player.resources.model.Play(PlayerEmoteType.Tired);

            timerAction = null;
        }

        // ------------------------------------------------------------------------------
        // 3-3) 메서드 -> 표시(Display)
        // ------------------------------------------------------------------------------
        protected override IEnumerator _Display(bool isActive, float duration)
        {
            if (timerAction != null) StopCoroutine(timerAction);

            isDisplaying = true;

            root.hide.gameObject.SetActive(false);

            yield return _Display(root.main, isActive, duration);

            isDisplaying = false;

            if (isActive) timerAction = StartCoroutine(SetTimer());
            else          gameObject.SetActive(false);
        }

        // ******************************************************************************
        // 3-3-1) 메서드 -> 표시 -> 메인
        //    - 메인 메뉴의 표시
        // ******************************************************************************
        public virtual Coroutine DisplayMain(bool isActive)
        {
            SetInteractables(false);

            return StartCoroutine(_Display(root.main, isActive, defaultDuration)); 
        }

        protected virtual IEnumerator _Display(MenuBase menu, bool isActive, float duration)
        {
            yield return FadeContent(menu, isActive, duration);

            SetInteractables(true);
        }

        // ******************************************************************************
        // 3-3-2) 메서드 -> 표시 -> 숨기기(Hide)
        //    - 메인 메뉴의 숨기기
        //    - 숨김 메뉴의 표시
        // ******************************************************************************
        public virtual Coroutine DisplayHide(bool isActive)
        {
            var menu = root.hide;

            menu.gameObject.SetActive(true);
            Set();
            SetInteractables(false);

            return StartCoroutine(_Display(root.hide, isActive, defaultDuration));
        }

        // ------------------------------------------------------------------------------
        // 3-4) 메서드 -> 이벤트
        // ------------------------------------------------------------------------------
        // ******************************************************************************
        // 3-4-1) 메서드 -> 이벤트 -> 드래그(화면)
        //    - 화면을 드래그 시 일정 각도 내에 카메라 회전
        // ******************************************************************************
        public virtual void OnBeginDrag(PointerEventData eventData)
        {
            if (isDisplaying) return;

            ICameraController camera = menu.scene.cameras.main;

            camera.Rotate(eventData.position);
        }

        public virtual void OnDrag(PointerEventData eventData)
        {
            ICameraController camera = menu.scene.cameras.main;

            camera.SetAngles(eventData.position);
        }

        public virtual void OnEndDrag(PointerEventData eventData)
        {
            ICameraController camera = menu.scene.cameras.main;

            camera.Return();
        }

        // ******************************************************************************
        // 3-4-2) 메서드 -> 이벤트 -> 클릭(버튼)
        //    - 버튼 클릭 시 메뉴 전환
        // ******************************************************************************
        protected virtual void OnClickMenuButton(Type type)
        {
            IMenuBase menu = this.menu.scene.menu[type];

            menu.Open(true, this.menu);
        }

        protected virtual void OnClickGeneralButton(GeneralType type)
        {
            switch (type)
            {
                case GeneralType.Menu:
                    {
                        Game.IMainMenuManager gameMenu = GameDirector.instance.menu.main;

                        gameMenu.Open(true);
                    }
                    break;

                case GeneralType.Hide:  menu.HideMainMenu(true);  break;
                case GeneralType.Close: menu.HideMainMenu(false); break;
            }
        }
    }
}
