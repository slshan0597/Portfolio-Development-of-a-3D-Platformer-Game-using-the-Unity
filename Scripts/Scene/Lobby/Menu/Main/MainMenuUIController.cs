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


    public interface IMainMenuUIController : IUIBase
    {
        #region Property

        // Component
        Root root { get; }

        // Reference
        IMainMenuManager menu { get; }

        #endregion


        #region Method

        Coroutine DisplayMain(bool isActive);
        Coroutine DisplayHide(bool isActive);

        #endregion
    }


    public class MainMenuUIController : UIBase, IMainMenuUIController, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        #region Definition

        public class Root : WindowBase
        {
            #region Definition

            public class General : MenuBase
            {
                #region Definition

                public enum Type { Menu, Hide, Close }

                #endregion


                #region Field

                public Dictionary<Type, KeyButton> buttons { get; }

                #endregion


                #region Constructor

                public General(Transform transform) : base(transform)
                {
                    buttons = new Dictionary<Type, KeyButton>();

                    foreach (var button in content.GetComponentsInChildren<KeyButton>(true))
                    {
                        string name = button.name.Replace(" ", string.Empty).Replace("Button", string.Empty);

                        if (Enum.TryParse(name, out Type type)) buttons.Add(type, button);
                    }
                }

                #endregion


                #region Method

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

                #endregion
            }


            public class Main : MenuBase
            {
                #region Definition

                public class Option : MenuBase
                {
                    #region Field

                    public Dictionary<Type, MarkButton> buttons { get; }

                    #endregion


                    #region Constructor

                    public Option(Transform transform) : base(transform)
                    {
                        buttons = new Dictionary<Type, MarkButton>();

                        foreach (var button in content.GetComponentsInChildren<MarkButton>(true))
                        {
                            string name = button.name.Replace(" ", string.Empty).Replace("Button", string.Empty);

                            if (Enum.TryParse(name, out Type type)) buttons.Add(type, button);
                        }
                    }

                    #endregion


                    #region Method

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

                    #endregion
                }

                #endregion


                #region Field

                public Option  option  { get; }
                public General general { get; }

                #endregion


                #region Constructor

                public Main(Transform transform) : base(transform)
                {
                    option  = new Option(content.Find("Option"));
                    general = new General(content.Find("General"));
                }

                #endregion


                #region Method

                public void SetContent(IDataManager data, IControlSettingManager controlSetting)
                {
                    option.SetContent(data);
                    general.SetContent(controlSetting);
                }

                #endregion
            }


            public class Hide : MenuBase
            {
                #region Field

                public General general { get; }

                #endregion


                #region Constructor

                public Hide(Transform transform) : base(transform) { general = new General(content.Find("General")); }

                #endregion
            }

            #endregion


            #region Field

            public Main main { get; }
            public Hide hide { get; }

            #endregion


            #region Cunstructor

            public Root(Transform transform) : base(transform)
            {
                main = new Main(content.Find("Main"));
                hide = new Hide(content.Find("Hide"));
            }

            #endregion


            #region Method

            public void SetContent(IDataManager data, IControlSettingManager controlSetting)
            {
                main.SetContent(data, controlSetting);
                hide.general.SetContent(controlSetting);
            }

            #endregion
        }

        #endregion


        #region Field

        public Root             root { get; protected set; }
        public IMainMenuManager menu { get; protected set; }

        protected bool isDisplaying = false;

        protected Coroutine timerAction;

        #endregion


        #region Method

        protected void Update()
        {
            if (Input.GetKeyDown(KeyCode.Z)) Cursor.lockState = CursorLockMode.Locked;
            if (Input.GetKeyDown(KeyCode.X)) Cursor.lockState = CursorLockMode.None;
            if (Input.GetKeyDown(KeyCode.C)) Cursor.visible = true;
            if (Input.GetKeyDown(KeyCode.V)) Cursor.visible = false;
        }

        #region Initialization

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

            root.SetContent(data, controlSetting);
        }

        #endregion


        #region Main

        #region Display

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

        public virtual Coroutine DisplayMain(bool isActive)
        {
            SetInteractables(false);

            return StartCoroutine(_Display(root.main, isActive, defaultDuration)); 
        }

        #endregion


        protected virtual IEnumerator SetTimer()
        {
            IPlayerController player = menu.scene.player;

            yield return new WaitForSeconds(10f);

            player.resources.model.Play(PlayerEmoteType.Tired);

            timerAction = null;
        }

        protected virtual void OnClickMenuButton(Type type)
        {
            IMenuBase menu = this.menu.scene.menu[type];

            menu.Open(true, this.menu);
        }

        #endregion


        #region Hide

        #region Display

        public virtual Coroutine DisplayHide(bool isActive)
        {
            var menu = root.hide;

            menu.gameObject.SetActive(true);
            Set();
            SetInteractables(false);

            return StartCoroutine(_Display(root.hide, isActive, defaultDuration));
        }

        #endregion

        #endregion


        #region General

        #region Display

        protected virtual IEnumerator _Display(MenuBase menu, bool isActive, float duration)
        {
            yield return FadeContent(menu, isActive, duration);

            SetInteractables(true);
        }

        #endregion


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

        #endregion

        #endregion
    }
}
