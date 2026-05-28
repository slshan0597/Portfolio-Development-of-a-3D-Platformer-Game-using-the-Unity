using UnityEngine;


namespace Stage
{
    using SoundType = SystemAudioController.SoundType;
    using AudioType = AudioBase.Type;


    public interface ISystemAudioController : IAudioBase
    {
        #region Property

        // Reference
        ISceneDirector scene { get; }

        // Setting
        SimpleData<SoundType, AudioClip> sounds { get; }

        #endregion


        #region Method

        float Play(SoundType type);

        #endregion
    }


    public class SystemAudioController : AudioBase, ISystemAudioController
    {
        #region Definition

        public enum SoundType { None, PlanetClear, AppearBoss }

        #endregion


        #region Field

        public ISceneDirector scene { get; protected set; }

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

        protected override void ResetField(AudioSource audioSource)
        {
            base.ResetField(audioSource);

            audioSource.spatialBlend = 0f;
            _audioType               = AudioType.System;
        }

        #endregion


        #region Play

        public virtual float Play(SoundType type)
        {
            audioSource.clip = sounds.ContainsKey(type) ? sounds[type] : null;

            return Play();
        }

        #endregion

        #endregion
    }
}
