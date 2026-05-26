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


    public interface IMenuUIController : IUIBase
    {
        #region Property

        // Component
        Root root { get; }

        // Reference
        IMenuManager menu { get; }

        #endregion
    }


    public class MenuUIController : UIBase, IMenuUIController
    {
        #region Definition

        public class Root : MenuBase
        {
            #region Definition

            public enum Type { Start, Load, Setting, Exit }

            #endregion


            #region Field

            public Dictionary<Type, SoundButton> buttons { get; }

            #endregion


            #region Constructor

            public Root(Transform transform) : base(transform)
            {
                buttons = new Dictionary<Type, SoundButton>();

                foreach (var button in content.GetComponentsInChildren<SoundButton>(true))
                {
                    string name = button.name.Replace(" ", string.Empty).Replace("Button", string.Empty);

                    if (Enum.TryParse(name, out Type type)) buttons.Add(type, button);
                }
            }

            #endregion
        }

        #endregion


        #region Field

        public Root         root { get; protected set; }
        public IMenuManager menu { get; protected set; }

        #endregion


        #region Method

        #region Initialization

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

        #endregion


        #region Option

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

        #endregion

        #endregion
    }
}
