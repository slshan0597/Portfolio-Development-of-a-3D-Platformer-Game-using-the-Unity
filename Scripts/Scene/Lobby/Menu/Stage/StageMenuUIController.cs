// //////////////////////////////////////////////////////////////////////////////
// * 요약
//    - 게임의 로비의 스테이지(레벨) 메뉴의 UI 클래스
//    - 현재 선택된 레벨의 정보 표시
//    - 레벨의 변경 기능 및 스테이지 변경 메뉴 호출 기능
//
// * 목차
//    1. 인터페이스 ... Line 42
//    2. 클래스 ....... Line 60
//        1) 내부 타입 ... Line 65
//            1- 공통(General) ... Line 70
//            2- 메인 ............ Line 113
//            3- 뷰(View) ........ Line 177
//        2) 필드 ..... Line 209
//        3) 메서드 ... Line 217
//            1- 초기화 .......... Line 220
//            2- 셋(Set) ......... Line 238
//            3- 표시(Display) ... Line 256
//                1_ 메인 ....... Line 270
//                2_ 뷰(View) ... Line 299
//            4- 이벤트 ... Line 319
//                1_ 드래그(화면) ... Line 322
//                2_ 클릭(버튼) ..... Line 333
// //////////////////////////////////////////////////////////////////////////////
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

using Game;

namespace Lobby
{
    using Root              = StageMenuUIController.Root;
    using MainButtonType    = StageMenuUIController.Root.Main.ButtonType;
    using StageDatas        = DataManager.Stages;
    using LevelData         = DataManager.Stages.Stage.Levels.Level;
    using MarkType          = MarkButton.MarkType;
    using ControlState      = ControlSettingManager.State;
    using SystemControlType = ControlSettingManager.Data.SystemType;

    // //////////////////////////////////////////////////////////////////////////////
    // 1. 인터페이스(IUIBase 인터페이스 상속)
    // //////////////////////////////////////////////////////////////////////////////
    public interface IStageMenuUIController : IUIBase
    {
        // 프로퍼티
        // Component
        Root                       root { get; }
        IStageMenuListUIController list { get; }

        // Reference
        IStageMenuManager menu { get; }
    }

    // //////////////////////////////////////////////////////////////////////////////
    // 2. 클래스(UIBase 클래스 상속)
    // //////////////////////////////////////////////////////////////////////////////
    public class StageMenuUIController : UIBase, IStageMenuUIController, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        // ==============================================================================
        // 1) 내부 타입 - 구조
        // ==============================================================================
        public class Root : WindowBase
        {
            // ------------------------------------------------------------------------------
            // 1-1) 내부 타입 - 구조 -> 메인(레벨)
            //    - 레벨에 대한 UI 구조
            // ------------------------------------------------------------------------------
            public class Main : MenuBase
            {
                // 내부 타입 - 메인
                public enum ButtonType { Previous, Next, Start, Select, Close }

                public class Level : WindowBase
                {
                    public Text contentText { get; }

                    public Level(Transform transform) : base(transform)
                    {
                        contentText = content.Find("Content Text").GetComponent<Text>();
                    }

                    public void SetContent(LevelData levelData)
                    {
                        var           challengeDatas = levelData.challenges;
                        StringBuilder challenge      = new StringBuilder();

                        foreach (var challengeData in challengeDatas.Values)
                            challenge.Append(challengeData.cleared ? "★" : "☆");

                        string _challenge = levelData.cleared ? $"- {challenge} -" : "- Not Cleared -";

                        contentText.text = $"Name\t: {levelData.name}\n{_challenge}";
                    }
                }

                // 필드 - 메인
                public Text                                  labelText { get; }
                public Level                                 level     { get; }
                public Dictionary<MainButtonType, KeyButton> buttons   { get; }

                // 생성자 - 메인
                public Main(Transform transform) : base(transform)
                {
                    labelText = content.Find("Label Text").GetComponent<Text>();
                    level     = new Level(content.Find("Level"));
                    buttons   = new Dictionary<MainButtonType, KeyButton>();

                    foreach (var button in content.GetComponentsInChildren<KeyButton>(true))
                    {
                        string name = button.name.Replace(" ", string.Empty).Replace("Button", string.Empty);

                        if (Enum.TryParse(name, out MainButtonType type)) buttons.Add(type, button);
                    }
                }

                // 메서드 - 메인
                public void SetContent(StageDatas stageDatas, IControlSettingManager controlSetting)
                {
                    var stageData = stageDatas.Current;
                    var levelData = stageData.levels.Current;

                    labelText.text = stageData.name;

                    level.SetContent(levelData);

                    foreach (var element in buttons)
                    {
                        MainButtonType type   = element.Key;
                        var            button = element.Value;

                        switch (type)
                        {
                            case MainButtonType.Previous:
                                {
                                    switch (controlSetting.state)
                                    {
                                        case ControlState.Keyboard: button.SetContent(KeyCode.Q);               break;
                                        case ControlState.Joystick: button.SetContent(KeyCode.JoystickButton4); break;
                                        default:                    button.SetContent(KeyCode.None);            break;
                                    }
                                }
                                break;

                            case MainButtonType.Next:
                                {
                                    MarkType mark = (!stageData.cleared && (!levelData.playable || levelData.cleared)) 
                                        ? MarkType.Update : MarkType.None;

                                    switch (controlSetting.state)
                                    {
                                        case ControlState.Keyboard: button.SetContent(KeyCode.E,               mark); break;
                                        case ControlState.Joystick: button.SetContent(KeyCode.JoystickButton5, mark); break;
                                        default:                    button.SetContent(KeyCode.None,            mark); break;
                                    }
                                }
                                break;

                            case MainButtonType.Start:
                                {
                                    MarkType mark = (levelData.playable && !levelData.cleared) ? MarkType.Update : MarkType.None;

                                    button.interactable = levelData.playable;

                                    button.SetContent(controlSetting.GetKey(SystemControlType.Start), mark);
                                }
                                break;

                            case MainButtonType.Select:
                                {
                                    MarkType mark = stageDatas.Any(data => data.playable && !data.selected && !data.cleared)
                                        ? MarkType.Update : MarkType.None;

                                    switch (controlSetting.state)
                                    {
                                        case ControlState.Keyboard: button.SetContent(KeyCode.Tab,             mark); break;
                                        case ControlState.Joystick: button.SetContent(KeyCode.JoystickButton2, mark); break;
                                        default:                    button.SetContent(KeyCode.None,            mark); break;
                                    }
                                }
                                break;

                            case MainButtonType.Close: button.SetContent(controlSetting.GetKey(SystemControlType.Cancel)); break;
                        }
                    }
                }
            }

            // ------------------------------------------------------------------------------
            // 1-2) 내부 타입 - 구조 -> 셀렉트(스테이지)
            //    - 스테이지 리스트에 대한 UI 구조
            // ------------------------------------------------------------------------------
            public class Select : WindowBase
            {
                // 필드 - 셀렉트
                public ScrollRect list        { get; }
                public KeyButton  closeButton { get; }

                // 생성자 - 셀렉트
                public Select(Transform transform) : base(transform)
                {
                    list        = content.GetComponentInChildren<ScrollRect>(true);
                    closeButton = content.GetComponentInChildren<KeyButton>(true);
                }
            }

            // 필드
            public Main   main   { get; }
            public Select select { get; }

            // 생성자
            public Root(Transform transform) : base(transform)
            {
                main   = new Main(content.Find("Main"));
                select = new Select(content.Find("Select"));
            }

            // 메서드
            public void SetContent(StageDatas stageDatas, IControlSettingManager controlSetting)
            {
                main.SetContent(stageDatas, controlSetting);
                select.closeButton.SetContent(controlSetting.GetKey(SystemControlType.Cancel));
            }
        }

        // ==============================================================================
        // 2) 필드
        // ==============================================================================
        // Component & Reference
        public Root                       root { get; protected set; }
        public IStageMenuListUIController list { get; protected set; }
        public IStageMenuManager          menu { get; protected set; }

        // etc.
        protected bool isDisplaying = false;

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
            list = GetComponentInChildren<IStageMenuListUIController>(true);
            menu = GetComponentInParent<IStageMenuManager>(true);

            foreach (var element in root.main.buttons)
                element.Value.onClick.AddListener(delegate { OnClickMainButton(element.Key); });

            root.select.closeButton.onClick.AddListener(delegate { OnClickSelectCloseButton(); });
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

            var stageDatas = data.stages;

            root.SetContent(stageDatas, controlSetting);
        }

        public override void SelectFirstSelectable() { }

        protected override void SetCurrent(bool isActive) { }

        // ------------------------------------------------------------------------------
        // 3-3) 메서드 -> 표시(Display)
        // ------------------------------------------------------------------------------
        protected override IEnumerator _Display(bool isActive, float duration)
        {
            isDisplaying = true;

            root.select.gameObject.SetActive(false);

            yield return FadeContent(root.main, isActive, duration);

            SetInteractables(true);

            isDisplaying = false;

            if (!isActive) gameObject.SetActive(false);
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
        //    - 레벨 전환 및 시작
        //    - 스테이지 메뉴 오픈
        // ******************************************************************************
        protected virtual void OnClickMainButton(MainButtonType type)
        {
            switch (type)
            {
                case MainButtonType.Previous: menu.ChangeLevel(false);   break;
                case MainButtonType.Next:     menu.ChangeLevel(true);    break;
                case MainButtonType.Start:    menu.StartLevel();         break;
                case MainButtonType.Select:   menu.OpenSelectMenu(true); break;
                case MainButtonType.Close:
                    {
                        IMainMenuManager mainMenu = menu.scene.menu.main;

                        mainMenu.Open(true, menu);
                    }
                    break;
            }
        }

        protected virtual void OnClickSelectCloseButton() { menu.OpenSelectMenu(false); }
    }
}
