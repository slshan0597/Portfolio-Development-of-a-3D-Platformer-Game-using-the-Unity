using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;


namespace Game
{
    using Root              = MainMenuUIController.Root;
    using SlotType          = MainMenuUIController.Root.Main.Slot.Type;
    using ConfirmUIState    = ConfirmUIController.State;
    using SceneType         = SceneBase.Type;
    using SystemControlType = ControlSettingManager.Data.SystemType;


    public interface IMainMenuUIController : IUIBase
    {
        #region Property

        // Component
        Root root { get; }

        // Reference
        IMainMenuManager menu { get; }

        #endregion
    }


    public class MainMenuUIController : UIBase, IMainMenuUIController
    {
        #region Definition

        public class Root : WindowBase
        {
            #region Definition

            public class Main : MenuBase
            {
                #region Definition

                public class Slot : SlotBase
                {
                    #region Definition

                    public enum Type { Setting, Scene, Exit }

                    #endregion


                    #region Field

                    public SoundButton button { get; }

                    #endregion


                    #region Constructor

                    public Slot(Transform transform) : base(transform)
                    {
                        button = content.GetComponentInChildren<SoundButton>(true);
                    }

                    #endregion
                }

                #endregion


                #region Field

                public Dictionary<SlotType, Slot> slots { get; }

                #endregion


                #region Constructor

                public Main(Transform transform) : base(transform)
                {
                    slots = new Dictionary<SlotType, Slot>();

                    for (int i = 0; i < content.childCount; i++)
                    {
                        string name = content.GetChild(i).name.Replace(" ", string.Empty).Replace("Slot", string.Empty);

                        if (Enum.TryParse(name, out SlotType type)) slots.Add(type, new Slot(content.GetChild(i)));
                    }
                }

                #endregion


                #region Method

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

                #endregion
            }

            #endregion


            #region Field

            public Main      main        { get; }
            public KeyButton closeButton { get; }

            #endregion


            #region Constructor

            public Root(Transform transform) : base(transform)
            {
                main        = new Main(content.Find("Main"));
                closeButton = content.GetComponentInChildren<KeyButton>(true);
            }

            #endregion


            #region Method

            public void SetContent(SceneType sceneType, IControlSettingManager controlSetting)
            {
                main.SetContent(sceneType);
                closeButton.SetContent(controlSetting.GetKey(SystemControlType.Cancel));
            }

            #endregion
        }

        #endregion


        #region Field

        public Root             root { get; protected set; }
        public IMainMenuManager menu { get; protected set; }

        #endregion


        #region Method

        #region Initialization

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

        #endregion


        #region Set

        public override void Set()
        {
            base.Set();

            IGameDirector          game           = menu.game;
            IControlSettingManager controlSetting = game.menu.setting.control;

            SceneType sceneType = game.scenes.current.Key;

            root.SetContent(sceneType, controlSetting);
        }

        #endregion


        #region Display

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

        #endregion


        #region Option

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

        #endregion

        #endregion
    }
}
