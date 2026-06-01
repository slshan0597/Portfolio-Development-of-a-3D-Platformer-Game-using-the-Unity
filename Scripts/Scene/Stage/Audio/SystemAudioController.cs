// //////////////////////////////////////////////////////////////////////////////
// * 요약
//    - 스테이지 씬 전용 시스템 오디오 클래스
//    - 스테이지의 이벤트에 따른 오디오 재생
//
// * 목차
//    1. 인터페이스 ... Line 22
//    2. 클래스 ....... Line 38
//        1) 필드 ..... Line 45
//        2) 메서드 ... Line 56
//            1- 초기화 ....... Line 59
//            2- 재생(Play) ... Line 77
// //////////////////////////////////////////////////////////////////////////////
using UnityEngine;

namespace Stage
{
    using SoundType = SystemAudioController.SoundType;
    using AudioType = AudioBase.Type;

    // //////////////////////////////////////////////////////////////////////////////
    // 1. 인터페이스(IAudioBase 인터페이스 상속)
    // //////////////////////////////////////////////////////////////////////////////
    public interface ISystemAudioController : IAudioBase
    {
        // 프로퍼티
        // Reference
        ISceneDirector scene { get; }

        // Setting
        SimpleData<SoundType, AudioClip> sounds { get; }

        // 메서드
        float Play(SoundType type);
    }

    // //////////////////////////////////////////////////////////////////////////////
    // 2. 클래스(AudioBase 클래스 상속)
    // //////////////////////////////////////////////////////////////////////////////
    public class SystemAudioController : AudioBase, ISystemAudioController
    {
        public enum SoundType { None, PlanetClear, AppearBoss }

        // ==============================================================================
        // 1) 필드
        // ==============================================================================
        // Component & Reference
        public ISceneDirector scene { get; protected set; }

        // Setting
        [SerializeField] protected SimpleData<SoundType, AudioClip> _sounds;

        public SimpleData<SoundType, AudioClip> sounds { get { return _sounds; } }

        // ==============================================================================
        // 2) 메서드
        // ==============================================================================
        // ------------------------------------------------------------------------------
        // 2-1) 메서드 -> 초기화
        // ------------------------------------------------------------------------------
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

        // ------------------------------------------------------------------------------
        // 2-2) 메서드 -> 재생(Play)
        //    - 이벤트에 따른 오디오 재생
        // ------------------------------------------------------------------------------
        public virtual float Play(SoundType type)
        {
            audioSource.clip = sounds.ContainsKey(type) ? sounds[type] : null;

            return Play();
        }
    }
}
