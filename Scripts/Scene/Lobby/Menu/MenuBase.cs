using System.Collections;
using System.Linq;
using UnityEngine;


namespace Lobby
{
    using Type            = MenuBase.Type;
    using PlayerEmoteType = PlayerModelController.EmoteType;


    public interface IMenuBase
    {
        #region Property

        // Component
        GameObject              gameObject   { get; }
        IUIBase                 ui           { get; }
        ICameraTargetController cameraTarget { get; }

        // Reference
        ISceneDirector scene { get; }

        // Setting
        Type type { get; }

        #endregion


        #region Method

        void      Initialize();
        Coroutine Open(bool isActive, IMenuBase prev = null);

        #endregion
    }


    public class MenuBase : MonoBehaviour, IMenuBase
    {
        #region Definition

        public enum Type { None, Main, Character, Stage }

        #endregion


        #region Field

        public IUIBase                 ui           { get; protected set; }
        public ICameraTargetController cameraTarget { get; protected set; }
        public ISceneDirector          scene        { get; protected set; }

        public Type type { get; protected set; }

        #endregion


        #region Function

        #region Event

        protected virtual void Awake() { SetField(); }

        #endregion


        #region Initialization

        protected virtual void SetField()
        {
            ui    = transform.Find("UI").GetComponent<IUIBase>();
            scene = GetComponentInParent<ISceneDirector>(true);

            foreach (var target in GetComponentsInChildren<ICameraTargetController>(true))
            {
                string[] valid = new string[] { "Camera Target", "Main" };

                if (valid.Contains(target.transform.name)) cameraTarget = target;
            }
        }

        public virtual void Initialize() 
        {
            gameObject.SetActive(true);
            ui.gameObject.SetActive(false);
        }

        #endregion


        public virtual Coroutine Open(bool isActive, IMenuBase prev = null)
        { 
            if (isActive)
            {
                ICameraController camera = scene.cameras.main;
                IPlayerController player = scene.player;

                float duration = ui.defaultDuration + ((prev != null) ? prev.ui.defaultDuration : 0f);

                camera.Set(cameraTarget, duration);

                if (prev != null) player.resources.model.Play(PlayerEmoteType.None);
            }

            return StartCoroutine(_Open(isActive, prev));
        }

        protected virtual IEnumerator _Open(bool isActive, IMenuBase prev)
        {
            IBackGroundMusicController bgm = scene.bgm;

            if (isActive)
            {
                if (prev != null) yield return prev.Open(false);

                bgm.Play(type);
            }
            else bgm.Stop(ui.defaultDuration);

            yield return ui.Display(isActive);
        }

        #endregion
    }
}
