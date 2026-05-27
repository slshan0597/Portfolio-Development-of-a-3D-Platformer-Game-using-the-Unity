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


    public interface IStageMenuUIController : IUIBase
    {
        #region Property

        // Component
        Root                       root { get; }
        IStageMenuListUIController list { get; }

        // Reference
        IStageMenuManager menu { get; }

        #endregion
    }


    public class StageMenuUIController : UIBase, IStageMenuUIController, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        #region Definition

        public class Root : WindowBase
        {
            #region Definition

            public class Main : MenuBase
            {
                #region Definition

                public enum ButtonType { Previous, Next, Start, Select, Close }


                public class Level : WindowBase
                {
                    #region Field

                    public Text contentText { get; }

                    #endregion


                    #region Constructor

                    public Level(Transform transform) : base(transform)
                    {
                        contentText = content.Find("Content Text").GetComponent<Text>();
                    }

                    #endregion


                    #region Method

                    public void SetContent(LevelData levelData)
                    {
                        var           challengeDatas = levelData.challenges;
                        StringBuilder challenge      = new StringBuilder();

                        foreach (var challengeData in challengeDatas.Values)
                            challenge.Append(challengeData.cleared ? "★" : "☆");

                        string _challenge = levelData.cleared ? $"- {challenge} -" : "- Not Cleared -";

                        contentText.text = $"Name\t: {levelData.name}\n{_challenge}";
                    }

                    #endregion
                }

                #endregion


                #region Field

                public Text                                  labelText { get; }
                public Level                                 level     { get; }
                public Dictionary<MainButtonType, KeyButton> buttons   { get; }

                #endregion


                #region Constructor

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

                #endregion


                #region Method

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

                #endregion
            }


            public class Select : WindowBase
            {
                #region Field

                public ScrollRect list        { get; }
                public KeyButton  closeButton { get; }

                #endregion


                #region Constructor

                public Select(Transform transform) : base(transform)
                {
                    list        = content.GetComponentInChildren<ScrollRect>(true);
                    closeButton = content.GetComponentInChildren<KeyButton>(true);
                }

                #endregion
            }

            #endregion


            #region Field

            public Main   main   { get; }
            public Select select { get; }

            #endregion


            #region Constructor

            public Root(Transform transform) : base(transform)
            {
                main   = new Main(content.Find("Main"));
                select = new Select(content.Find("Select"));
            }

            #endregion


            #region Method

            public void SetContent(StageDatas stageDatas, IControlSettingManager controlSetting)
            {
                main.SetContent(stageDatas, controlSetting);
                select.closeButton.SetContent(controlSetting.GetKey(SystemControlType.Cancel));
            }

            #endregion
        }

        #endregion


        #region Field

        public Root                       root { get; protected set; }
        public IStageMenuListUIController list { get; protected set; }
        public IStageMenuManager          menu { get; protected set; }

        protected bool isDisplaying = false;

        #endregion


        #region Method

        #region Initialization

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

        #endregion


        #region Drag

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

        #endregion


        #region Set

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

        #endregion


        #region Main

        #region Display

        protected override IEnumerator _Display(bool isActive, float duration)
        {
            isDisplaying = true;

            root.select.gameObject.SetActive(false);

            yield return FadeContent(root.main, isActive, duration);

            SetInteractables(true);

            isDisplaying = false;

            if (!isActive) gameObject.SetActive(false);
        }

        #endregion


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

        #endregion


        #region Select

        protected virtual void OnClickSelectCloseButton() { menu.OpenSelectMenu(false); }

        #endregion

        #endregion
    }
}
