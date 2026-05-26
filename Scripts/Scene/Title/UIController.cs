using System.Collections;
using UnityEngine;
using UnityEngine.UI;

using Game;


namespace Title
{
    using Root         = UIController.Root;
    using ControlState = ControlSettingManager.State;


    public interface IUIController : IUIBase
    {
        #region Property

        // Component
        public Root root { get; }

        // Reference
        ISceneDirector scene { get; }

        #endregion
    }


    public class UIController : UIBase, IUIController
    {
        #region Definition

        public class Root : MenuBase
        {
            #region Definition

            public class Window : MenuBase
            {
                #region Field

                public Text text { get; }

                #endregion


                #region Constructor

                public Window(Transform transform) : base(transform)
                {
                    text = content.GetComponentInChildren<Text>(true);
                }

                #endregion
            }

            #endregion


            #region Field

            public Window label { get; }
            public Window input { get; }

            #endregion


            #region Constructor

            public Root(Transform transform) : base(transform)
            {
                label = new Window(content.Find("Label"));
                input = new Window(content.Find("Input"));
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
        }

        protected override void ResetField() { _defaultDuration = 1f; }

        #endregion


        #region Set

        public override void Set()
        {
            base.Set();

            IControlSettingManager controlSetting = GameDirector.instance.menu.setting.control;

            switch (controlSetting.state)
            {
                case ControlState.Touch: root.input.text.text = "- Touch Screen -";     break;
                default:                 root.input.text.text = "- Press Any Button -"; break;
            }
        }

        #endregion


        #region Display

        protected override IEnumerator _Display(bool isActive, float duration)
        {
            if (!isActive) gameObject.SetActive(false);

            IGameDirector          game           = GameDirector.instance;
            IAudioController       audio          = game.audio;
            IControlSettingManager controlSetting = game.menu.setting.control;

            root.label.gameObject.SetActive(true);
            root.input.gameObject.SetActive(false);

            yield return FadeGraphics(root.label, true, duration);

            root.input.gameObject.SetActive(true);

            yield return FadeGraphics(root.input, true, duration);

            while (true)
            {
                switch (controlSetting.state)
                {
                    case ControlState.Touch: if (Input.touchCount > 0) goto End; break;
                    default:                 if (Input.anyKeyDown)     goto End; break;
                }

                yield return null;
            }

            End:

            StartCoroutine(FadeContent(root.label, false, duration));
            audio.PlayGameStart();

            yield return FadeContent(root.input, false, duration);

            gameObject.SetActive(false);
        }

        #endregion

        #endregion
    }
}
