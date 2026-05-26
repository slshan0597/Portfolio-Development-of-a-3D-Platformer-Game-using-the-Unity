using UnityEngine;
using UnityEngine.UI;

using Game;


namespace Tutorial
{
    using Root            = GuideUIController.Root;
    using ControlState    = ControlSettingManager.State;
    using PlayerMainState = PlayerController.State.Main;


    public interface IGuideUIController : IUIBase
    {
        #region Property

        // Component
        Root root { get; }

        // Reference
        ISceneDirector scene { get; }

        // State
        GuideType state { get; }

        #endregion


        #region Method

        Coroutine Display(GuideType type);

        #endregion
    }


    public class GuideUIController : UIBase, IGuideUIController
    {
        #region Definition

        public class Root : MenuBase
        {
            #region Definition

            public class Main : WindowBase
            {
                #region Field

                public Text contentText { get; }

                #endregion


                #region Constructor

                public Main(Transform transform) : base(transform)
                {
                    contentText = content.GetComponentInChildren<Text>(true);
                }

                #endregion


                #region Method

                public void SetContent(GuideType type, IControlSettingManager controlSetting)
                {
                    ControlState state       = controlSetting.state;
                    string       jumpKey     = (state == ControlState.Touch) ? "Jump"     : controlSetting.GetKey(PlayerMainState.Jump).ToString();
                    string       crouchKey   = (state == ControlState.Touch) ? "Crouch"   : controlSetting.GetKey(PlayerMainState.Crouch).ToString();
                    string       attackKey   = (state == ControlState.Touch) ? "Attack"   : controlSetting.GetKey(PlayerMainState.Attack).ToString();
                    string       interactKey = (state == ControlState.Touch) ? "Interact" : controlSetting.GetKey(PlayerMainState.Interact).ToString();

                    switch (type)
                    {
                        case GuideType.Move:
                            {
                                string moveKey = string.Empty;

                                switch (state)
                                {
                                    case ControlState.Keyboard: moveKey = "'W' 'A' 'S' 'D'"; break;
                                    case ControlState.Joystick: moveKey = "LS";              break;
                                }

                                contentText.text = $"Move\t\t: '{moveKey}'\n"
                                                 + $"Crouch\t: '{crouchKey}'\n"
                                                 + $"Interact\t: '{interactKey}'";
                            }
                            break;

                        case GuideType.Jump1:
                            {
                                contentText.text = $"Jump\t\t\t: '{jumpKey}'\n"
                                                 + $"High Jump\t: '{jumpKey}' -> '{jumpKey}' -> '{jumpKey}'\n"
                                                 + $"Back Jump\t: (Stop Move) -> '{crouchKey}' -> '{jumpKey}'";
                            }
                            break;

                        case GuideType.Jump2:
                            {
                                contentText.text = $"Long Jump\t: (Move) -> '{crouchKey}' -> '{jumpKey}'";
                            }
                            break;

                        case GuideType.HipDrop:
                            {
                                contentText.text = $"Hip Drop\t: '{jumpKey}' -> '{crouchKey}'";
                            }
                            break;

                        case GuideType.Attack:
                            {
                                contentText.text = $"Attack\t: '{attackKey}'\n"
                                                 + $"Dive\t\t: (Hip Drop) -> '{attackKey}'";
                            }
                            break;
                    }
                }

                #endregion
            }

            #endregion


            #region Field

            public Main main { get; }

            #endregion


            #region Constructor

            public Root(Transform transform) : base(transform) { main = new Main(content.Find("Main")); }

            #endregion
        }

        #endregion


        #region Field

        public Root           root  { get; protected set; }
        public ISceneDirector scene { get; protected set; }

        public GuideType state { get; protected set; }

        protected Coroutine displayAction;

        #endregion


        #region Method

        #region Initialization

        protected override void SetField()
        {
            base.SetField();

            root  = new Root(transform);
            scene = GetComponentInParent<ISceneDirector>(true);
        }

        protected override void ResetField() { _defaultDuration = 0.5f; }

        #endregion


        #region Set

        public override void Set()
        {
            base.Set();

            IControlSettingManager controlSetting = GameDirector.instance.menu.setting.control;

            root.main.SetContent(state, controlSetting);
        }

        #endregion


        #region Display

        public override Coroutine Display(bool isActive, bool animated = true)
        {
            if (displayAction != null)
            {
                StopCoroutine(displayAction);

                displayAction = null;
            }

            gameObject.SetActive(true);

            if (isActive) state = GuideType.Move;

            Set();

            return displayAction = StartCoroutine(FadeWindow(root.main, isActive, defaultDuration));
        }

        public virtual Coroutine Display(GuideType type)
        {
            if (displayAction != null)
            {
                StopCoroutine(displayAction);

                displayAction = null;
            }

            gameObject.SetActive(true);

            state = type;

            Set();

            return displayAction = StartCoroutine(FadeWindow(root.main, true, defaultDuration));
        }

        #endregion

        #endregion
    }
}
