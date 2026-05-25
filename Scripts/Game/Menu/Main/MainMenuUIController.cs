// //////////////////////////////////////////////////////////////////////////////
// * 요약
//    - 게임의 메인 메뉴의 UI 클래스
//    - 세이브, 설정 메뉴의 연결 통로
//    - 씬의 이동 역할
//
// * 목차
//    1. 인터페이스 ... Line 33
//    2. 클래스 ....... Line 46
//        1) 내부 타입 ... Line 51
//        2) 필드 ........ Line 114
//        3) 메서드 ...... Line 121
//            1- 초기화 .......... Line 124
//            2- 셋(Set) ......... Line 147
//            3- 표시(Display) ... Line 163
//            4- 이벤트 .......... Line 186
// //////////////////////////////////////////////////////////////////////////////
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Game
{
    using Root              = MainMenuUIController.Root;
    using SlotType          = MainMenuUIController.Root.Main.Slot.Type;
    using ConfirmUIState    = ConfirmUIController.State;
    using SceneType         = SceneBase.Type;
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
    }

    // //////////////////////////////////////////////////////////////////////////////
    // 2. 클래스(UIBase 클래스 상속)
    // //////////////////////////////////////////////////////////////////////////////
    public class MainMenuUIController : UIBase, IMainMenuUIController
    {
        // ==============================================================================
        // 1) 내부 타입
        //    - UI의 구조에 대한 클래스
        // ==============================================================================
        public class Root : WindowBase
        {
            public class Main : MenuBase
            {
                public class Slot : SlotBase
                {
                    public enum Type { Setting, Scene, Exit }

                    public SoundButton button { get; }

                    public Slot(Transform transform) : base(transform)
                    {
                        button = content.GetComponentInChildren<SoundButton>(true);
                    }
                }

                public Dictionary<SlotType, Slot> slots { get; }

                public Main(Transform transform) : base(transform)
                {
                    slots = new Dictionary<SlotType, Slot>();

                    for (int i = 0; i < content.childCount; i++)
                    {
                        string name = content.GetChild(i).name.Replace(" ", string.Empty).Replace("Slot", string.Empty);

                        if (Enum.TryParse(name, out SlotType type)) slots.Add(type, new Slot(content.GetChild(i)));
                    }
                }

                public void SetContent(SceneType sceneType)
                {
                    var label = slots[SlotType.Scene].labelText;

                    switch (sceneType)
                    {
                        case SceneType.Lobby:                       label.text = "Save";       break;
                        case SceneType.Stage or SceneType.Tutorial: label.text = "Restart";    break;
                        default:                                    label.text = string.Empty; break;
                    }
                }
            }

            public Main      main        { get; }
            public KeyButton closeButton { get; }

            public Root(Transform transform) : base(transform)
            {
                main        = new Main(content.Find("Main"));
                closeButton = content.GetComponentInChildren<KeyButton>(true);
            }

            public void SetContent(SceneType sceneType, IControlSettingManager controlSetting)
            {
                main.SetContent(sceneType);
                closeButton.SetContent(controlSetting.GetKey(SystemControlType.Cancel));
            }
        }

        // ==============================================================================
        // 2) 필드
        // ==============================================================================
        // Component & Reference
        public Root             root { get; protected set; }
        public IMainMenuManager menu { get; protected set; }

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

            foreach (var item in root.main.slots)
            {
                var type = item.Key;
                var slot = item.Value;

                slot.button.onClick.AddListener(delegate { OnClickSlotButton(type); });
            }

            root.closeButton.onClick.AddListener(delegate { OnClickCloseButton(); });
        }

        protected override void ResetField() { _defaultDuration = 1f; }

        // ------------------------------------------------------------------------------
        // 3-2) 메서드 -> 셋(Set)
        //    - 설정이 변경되면 UI 갱신
        // ------------------------------------------------------------------------------
        public override void Set()
        {
            base.Set();

            IGameDirector          game           = menu.game;
            IControlSettingManager controlSetting = game.menu.setting.control;

            SceneType sceneType = game.scenes.current.Key;

            root.SetContent(sceneType, controlSetting);
        }

        // ------------------------------------------------------------------------------
        // 3-3) 메서드 -> 표시(Display)
        // ------------------------------------------------------------------------------
        protected override IEnumerator _Display(bool isActive, float duration)
        {
            root.closeButton.gameObject.SetActive(false);
            StartCoroutine(FadeBackGroundImage(root, isActive, duration));

            var           slots         = root.main.slots.Values;
            float         slotDuration  = duration / 2f;
            float         interval      = slotDuration / (slots.Count() - 1);
            FadeDirection fadeDirection = isActive ? FadeDirection.Right : FadeDirection.Left;

            yield return FadeSlots(slots, isActive, slotDuration, interval, fadeDirection);

            if (isActive && (duration > 0f)) lastSelectedSelectable = null;

            SetInteractables(true);

            if (isActive) root.closeButton.gameObject.SetActive(true);
            else          gameObject.SetActive(false);
        }

        // ------------------------------------------------------------------------------
        // 3-4) 메서드 -> 이벤트
        //    - 현재 씬에 따라 세이브 또는 세팅 메뉴 호출
        //    - 씬 이동(Exit)
        // ------------------------------------------------------------------------------
        protected virtual void OnClickSlotButton(SlotType type)
        {
            IGameDirector game  = menu.game;
            ISceneBase    scene = game.scenes.current.Value;

            switch (type)
            {
                case SlotType.Setting:
                    {
                        ISettingMenuManager settingMenu = game.menu.setting;

                        settingMenu.Open(true, menu);
                    }
                    break;

                case SlotType.Scene:
                    {
                        switch (scene.type)
                        {
                            case SceneType.Lobby:
                                {
                                    ISaveMenuManager saveMenu = game.menu.save;

                                    saveMenu.Open(true, menu);
                                }
                                break;

                            case SceneType.Stage or SceneType.Tutorial:
                                {
                                    StartCoroutine(TryExitScene(scene, scene.type));
                                }
                                break;
                        }
                    }
                    break;

                case SlotType.Exit:
                    {
                        SceneType nextSceneType = SceneType.None;

                        switch (scene.type)
                        {
                            case SceneType.Tutorial: nextSceneType = SceneType.Lobby; break;
                            case SceneType.Lobby:    nextSceneType = SceneType.Title; break;
                            case SceneType.Stage:    nextSceneType = SceneType.Lobby; break;
                        }

                        StartCoroutine(TryExitScene(scene, nextSceneType));
                    }
                    break;
            }
        }

        protected virtual IEnumerator TryExitScene(ISceneBase scene, SceneType nextSceneType)
        {
            IConfirmUIController confirmUI = menu.game.ui.confirm;

            yield return confirmUI.Display((scene.type == nextSceneType) ? "Restart" : "Exit", string.Empty, this);

            if (confirmUI.state == ConfirmUIState.Cancel) yield break;

            gameObject.SetActive(false);
            scene.Exit(nextSceneType);
        }

        protected virtual void OnClickCloseButton() { menu.Open(false); }
    }
}
