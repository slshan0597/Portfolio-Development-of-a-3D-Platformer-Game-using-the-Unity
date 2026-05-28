using UnityEngine;


namespace Stage
{
    using SoundType          = BackGroundMusicController.SoundType;
    using PlayerOverlapState = global::PlayerController.State.Overlap.State;


    public interface IBackGroundMusicController : global::IBackGroundMusicController
    {
        #region Property

        // Reference
        new ISceneDirector scene { get; }

        // Setting
        SimpleData<SoundType, AudioClip> sounds { get; }

        #endregion


        #region Method

        void Play(SoundType type);
        void PlayMain();

        #endregion
    }


    public class BackGroundMusicController : global::BackGroundMusicController, IBackGroundMusicController
    {
        #region Definition

        public enum SoundType { None, Main, Underground, Boss, BeforeGoal }

        #endregion


        #region Field

        public new ISceneDirector scene { get; protected set; }

        [SerializeField] protected SimpleData<SoundType, AudioClip> _sounds;

        public SimpleData<SoundType, AudioClip> sounds { get { return _sounds; } }

        #endregion


        #region Method

        #region Initialization

        protected override void SetField()
        {
            base.SetField();

            scene = GetComponentInParent<ISceneDirector>(true);
        }

        #endregion


        #region Play

        public virtual void Play(SoundType type)
        {
            IPlayerController player = scene.player;

            var next = sounds.ContainsKey(type) ? sounds[type] : null;

            if ((next == null) || (next == audioSource.clip)) return;

            audioSource.clip = next;

            if (player.state.overlap[PlayerOverlapState.PowerUp]) return;

            Play();
        }

        public virtual void PlayMain()
        {
            IStageController stage = scene.stages.current;

            _sounds.Add(SoundType.Main, stage.bgm);

            audioSource.clip = stage.bgm;

            Play();
        }

        #endregion

        #endregion
    }
}
