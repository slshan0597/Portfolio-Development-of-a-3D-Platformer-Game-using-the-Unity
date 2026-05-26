using System.Collections;
using UnityEngine;
using UnityEngine.UI;


namespace Game
{
    using State             = SaveMenuUIController.State;
    using Root              = SaveMenuUIController.Root;
    using SceneType         = SceneBase.Type;
    using SystemControlType = ControlSettingManager.Data.SystemType;


    public interface ISaveMenuUIController : IUIBase
    {
        #region Property

        // Component
        Root                  root { get; }
        ISaveListUIController list { get; }

        // Reference
        ISaveMenuManager menu { get; }

        // State
        State state { get; }

        #endregion
    }


    public class SaveMenuUIController : UIBase, ISaveMenuUIController
    {
        #region Definition

        public enum State { None, Load, Save }


        public class Root : WindowBase
        {
            #region Definition

            public class Main : WindowBase
            {
                #region Field

                public ScrollRect scroll { get; }

                #endregion


                #region Constructor

                public Main(Transform transform) : base(transform)
                {
                    scroll = content.GetComponentInChildren<ScrollRect>(true);
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
        }

        #endregion


        #region Field

        public Root                  root { get; protected set; }
        public ISaveListUIController list { get; protected set; }
        public ISaveMenuManager      menu { get; protected set; }

        public State state { get; protected set; }

        #endregion


        #region Method

        #region Initialization

        protected override void SetField()
        {
            base.SetField();

            root = new Root(transform);
            list = GetComponentInChildren<ISaveListUIController>(true);
            menu = GetComponentInParent<ISaveMenuManager>(true);
            root.closeButton.onClick.AddListener(delegate { OnClickCloseButton(); });
        }

        protected override void ResetField() { _defaultDuration = 0.5f; }

        #endregion


        #region Set

        public override void Set()
        {
            base.Set();

            IControlSettingManager controlSetting = menu.game.menu.setting.control;

            root.closeButton.SetContent(controlSetting.GetKey(SystemControlType.Cancel));
        }

        public override void SelectFirstSelectable() { }

        protected override void SetCurrent(bool isActive) { }

        #endregion


        #region Display

        public override Coroutine Display(bool isActive, bool animated = true)
        {
            if (isActive)
            {
                gameObject.SetActive(true);
                list.gameObject.SetActive(false);

                IGameDirector game = menu.game;

                SceneType sceneType = game.scenes.current.Key;

                switch (sceneType)
                {
                    case SceneType.Lobby: state = State.Save; break;
                    default:              state = State.Load; break;
                }
            }

            return base.Display(isActive, animated);
        }

        protected override IEnumerator _Display(bool isActive, float duration)
        {
            root.closeButton.gameObject.SetActive(false);

            yield return FadeWindow(root.main, isActive, duration);

            SetInteractables(true);

            if (isActive)
            {
                list.Display(true);
                root.closeButton.gameObject.SetActive(true);
            }
            else gameObject.SetActive(false);
        }

        #endregion


        protected virtual void OnClickCloseButton() { menu.Open(false); }

        #endregion
    }
}
