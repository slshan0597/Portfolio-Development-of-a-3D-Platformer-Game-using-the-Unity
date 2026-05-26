using UnityEngine;

using Game;


namespace Tutorial
{
    using Root              = MainUIController.Root;
    using SystemControlType = ControlSettingManager.Data.SystemType;


    public interface IMainUIController : IUIBase
    {
        #region Property

        // Component
        Root root { get; }

        // Reference
        ISceneDirector scene { get; }

        #endregion
    }


    public class MainUIController : UIBase, IMainUIController
    {
        #region Definition

        public class Root : MenuBase
        {
            #region Field

            public KeyButton menuButton { get; }

            #endregion


            #region Constructor

            public Root(Transform transform) : base(transform)
            {
                menuButton = content.GetComponentInChildren<KeyButton>(true);
            }

            #endregion
        }

        #endregion


        #region Field

        public Root           root  { get; protected set; }
        public ISceneDirector scene { get; protected set; }

        #endregion


        #region Method

        #region Initialization

        protected override void SetField()
        {
            base.SetField();

            root  = new Root(transform);
            scene = GetComponentInParent<ISceneDirector>(true);

            root.menuButton.onClick.AddListener(OnClickButton);
        }

        #endregion


        #region Set

        public override void Set()
        {
            base.Set();

            IControlSettingManager controlSetting = GameDirector.instance.menu.setting.control;

            root.menuButton.SetContent(controlSetting.GetKey(SystemControlType.Pause));
        }

        public override void SelectFirstSelectable() { }

        protected override void SetCurrent(bool isActive) { }

        #endregion


        protected virtual void OnClickButton()
        {
            IMainMenuManager gameMenu = GameDirector.instance.menu.main;

            gameMenu.Open(true);
        }

        #endregion
    }
}
