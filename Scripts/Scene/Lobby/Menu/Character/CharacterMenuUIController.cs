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


    public interface ICharacterMenuUIController : IUIBase
    {
        #region Property

        // Component
        Root                           root { get; }
        ICharacterMenuListUIController list { get; }

        // Reference
        ICharacterMenuManager menu { get; }

        #endregion


        #region Method

        Coroutine DisplayMain(bool isActive);
        Coroutine DisplayView(bool isActive);

        #endregion
    }


    public class CharacterMenuUIController : UIBase, ICharacterMenuUIController, IDragHandler
    {
        #region Definition

        public class Root : WindowBase
        {
            #region Definition

            public class General : MenuBase
            {
                #region Definition

                public enum Type { Close, View, Reset }

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
                            case GeneralType.Close: button.SetContent(controlSetting.GetKey(SystemControlType.Cancel)); break;
                            case GeneralType.View:  button.SetContent(controlSetting.GetKey(SystemControlType.Start));  break;
                            case GeneralType.Reset: button.SetContent(controlSetting.GetKey(SystemControlType.Start));  break;
                        }
                    }
                }

                #endregion
            }


            public class Main : MenuBase
            {
                #region Definition

                public class Stat : WindowBase
                {
                    #region Field

                    public ScrollRect list { get; }

                    #endregion


                    #region Constructor

                    public Stat(Transform transform) : base(transform)
                    {
                        list = content.GetComponentInChildren<ScrollRect>(true);
                    }

                    #endregion
                }


                public class Option : MenuBase
                {
                    #region Definition

                    public class Money : WindowBase
                    {
                        #region Field

                        public Text contentText { get; }

                        #endregion


                        #region Constructor

                        public Money(Transform transform) : base(transform)
                        {
                            contentText = content.GetComponentInChildren<Text>(true);
                        }

                        #endregion


                        #region Method

                        public void SetContent(MoneyData moneyData)
                        {
                            int money = moneyData.value;

                            contentText.text = $"Money\t: {money.ToString("#,##0")}";
                        }

                        #endregion
                    }

                    #endregion


                    #region Field

                    public Money   money   { get; }
                    public General general { get; }

                    #endregion


                    #region Constructor

                    public Option(Transform transform) : base(transform)
                    {
                        money   = new Money(content.Find("Money"));
                        general = new General(content.Find("General"));
                    }

                    #endregion


                    #region Method

                    public void SetContent(MoneyData moneyData, IControlSettingManager controlSetting)
                    {
                        money.SetContent(moneyData);
                        general.SetContent(controlSetting);
                    }

                    #endregion
                }

                #endregion


                #region Field

                public Stat   stat   { get; }
                public Option option { get; }

                #endregion


                #region Constructor

                public Main(Transform transform) : base(transform)
                {
                    stat   = new Stat(content.Find("Stat"));
                    option = new Option(content.Find("Option"));
                }

                #endregion
            }


            public class View : MenuBase
            {
                #region Field

                public General general { get; }

                #endregion


                #region Constructor

                public View(Transform transform) : base(transform) { general = new General(content.Find("General")); }

                #endregion
            }

            #endregion


            #region Field

            public Main main { get; }
            public View view { get; }

            #endregion


            #region Constructor

            public Root(Transform transform)  : base(transform)
            {
                main = new Main(content.Find("Main"));
                view = new View(content.Find("View"));
            }

            #endregion


            #region Method

            public void SetContent(MoneyData moneyData, IControlSettingManager controlSetting)
            {
                main.option.SetContent(moneyData, controlSetting);
                view.general.SetContent(controlSetting);
            }

            #endregion
        }

        #endregion


        #region Field

        public Root                           root { get; protected set; }
        public ICharacterMenuListUIController list { get; protected set; }
        public ICharacterMenuManager          menu { get; protected set; }

        #endregion


        #region Function

        #region Initialization

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

        #endregion


        #region Set

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

        #endregion


        #region Main

        #region Display

        protected override IEnumerator _Display(bool isActive, float duration)
        {
            root.view.gameObject.SetActive(false);

            StartCoroutine(FadeBackGroundImage(root, isActive, duration));

            yield return _DisplayMain(isActive, duration);

            if (!isActive) gameObject.SetActive(false);
        }

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

        #endregion

        #endregion


        #region View

        #region Display

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

        #endregion


        #region Drag

        public virtual void OnDrag(PointerEventData eventData)
        {
            ICharacterViewCameraController camera = menu.scene.cameras.characterView;

            if (camera.gameObject.activeInHierarchy) camera.Rotate(eventData.delta);
        }

        #endregion

        #endregion


        #region General

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

        #endregion

        #endregion
    }
}
