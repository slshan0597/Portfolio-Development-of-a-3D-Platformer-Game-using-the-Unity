using System.Collections;
using System.Linq;
using UnityEngine;

using Game;


namespace Stage
{
    using Type             = PlanetController.Type;
    using LetterboxUIState = LetterboxUIController.State;
    using SoundType        = SystemAudioController.SoundType;
    using BGMType          = BackGroundMusicController.SoundType;


    public interface IPlanetController : global::IPlanetController
    {
        #region Property

        // Component
        new IPlanetCoreController core         { get; }
        ICameraTargetController   cameraTarget { get; }
        IRewardable               reward       { get; }

        // Reference
        ILevelController level { get; }

        // Setting
        Type type { get; }

        #endregion


        #region Method

        void      Initialize(int index);
        void      Save(int index);
        bool      CheckClearable();
        void      TryClear();
        Coroutine Clear(bool animated = true);

        #endregion
    }


    public class PlanetController : global::PlanetController, IPlanetController, ICheckPointable
    {
        #region Definition

        public enum Type { Normal, Underground }

        #endregion


        #region Field

        public new IPlanetCoreController core         { get; protected set; }
        public ICameraTargetController   cameraTarget { get; protected set; }
        public IRewardable               reward       { get; protected set; }
        public ILevelController          level        { get; protected set; }

        [SerializeField] protected Type _type;
        [SerializeField] protected bool _useCheckPoint;

        public Type type          { get { return _type; } }
        public bool useCheckPoint { get { return _useCheckPoint; } }

        protected int triggeredCount, maxTriggeredCount;

        #endregion


        #region Method

        #region Initialization

        protected override void SetField()
        {
            base.SetField();

            core         = GetComponentInChildren<IPlanetCoreController>(true);
            cameraTarget = GetComponentInChildren<ICameraTargetController>(true);
            reward       = GetComponentInChildren<IRewardable>(true);
            level        = GetComponentInParent<ILevelController>(true);

            maxTriggeredCount = GetComponentsInChildren<ITriggerable>(true).Count(trigger => trigger.useTrigger);
        }

        public virtual void Initialize(int index)
        {
            gameObject.SetActive(true);

            if (reward == null) return;

            IDataManager data = level.stage.scene.data;

            reward.gameObject.SetActive(data.level.planetsCleared[index]);
        }

        #endregion


        #region Set

        public override void SetEnable(bool enabled, global::IPlayerController player)
        {
            IBackGroundMusicController bgm = level.stage.scene.audios.bgm;

            base.SetEnable(enabled, player);

            if (enabled)
            {
                switch (type)
                {
                    case Type.Underground: bgm.Play(BGMType.Underground); break;
                    default:               bgm.Play(BGMType.Main);        break;
                }
            }
        }

        #endregion


        #region Data

        public virtual void Save(int index)
        {
            if (reward == null) return;

            IDataManager data = level.stage.scene.data;

            data.level.planetsCleared[index] = reward.gameObject.activeInHierarchy;
        }

        #endregion


        #region Clear

        public virtual bool CheckClearable()
        {
            if (reward == null)                      return false;
            if (reward.gameObject.activeInHierarchy) return false;

            return (triggeredCount + 1) == maxTriggeredCount;
        }

        public virtual void TryClear()
        {
            if (reward == null)                       return;
            if (reward.gameObject.activeInHierarchy)  return;
            if (++triggeredCount < maxTriggeredCount) return;

            Clear();
        }

        public virtual Coroutine Clear(bool animated = true)
        {
            if (reward == null) return null;

            ISceneDirector scene = level.stage.scene;

            scene.Pause(true);

            return StartCoroutine(_Clear(animated));
        }

        protected virtual IEnumerator _Clear(bool animated)
        {
            ISceneDirector         scene       = level.stage.scene;
            ISystemAudioController audio       = scene.audios.system;
            ICameraController      camera      = scene.cameras.main;
            ILightController       light       = scene.light;
            ILetterboxUIController letterboxUI = GameDirector.instance.ui.letterbox;

            float waitDuration = letterboxUI.defaultDuration;

            letterboxUI.Display(LetterboxUIState.Normal, animated);

            if (animated)
                yield return new WaitForSecondsRealtime(audio.Play(SoundType.PlanetClear) + waitDuration);

            camera.Set(cameraTarget, 0f);
            camera.LookAt(reward.transform);
            light.Follow(reward.transform);

            yield return reward.Appear();
            yield return new WaitForSecondsRealtime(waitDuration);

            camera.StopLookAt();
            light.StopFollow();
            letterboxUI.Display(LetterboxUIState.Open, false);
            scene.Pause(false);

            if (useCheckPoint) scene.SaveLevel(this);
        }

        #endregion

        #endregion
    }
}
