// //////////////////////////////////////////////////////////////////////////////
// * 요약
//    - 게임의 시스템 관련 오디오 클래스
//    - 버튼 클릭, 메뉴 오픈 등의 시스템 오디오 재생
//
// * 목차
//    1. 인터페이스 ... Line 
//    2. 클래스 ....... Line 
//        1) 내부 타입 ... Line 
//        1) 필드 ........ Line 
//        2) 메서드 ...... Line 
//            1- 초기화 ....... Line 
//            2- 재생(Play) ... Line 
// //////////////////////////////////////////////////////////////////////////////
using System;
using UnityEngine;

namespace Game
{
    using Sounds    = AudioController.Sounds;
    using AudioType = AudioBase.Type;

    // //////////////////////////////////////////////////////////////////////////////
    // 1. 인터페이스(IAudioBase 인터페이스 상속)
    // //////////////////////////////////////////////////////////////////////////////
    public interface IAudioController : IAudioBase
    {
        // 프로퍼티
        // Reference
        IGameDirector game { get; }

        // Setting
        Sounds sounds { get; }

        // 메서드
        float PlayGameStart();
        float PlayButtonClick();
        float PlayMenuOpen(bool isOn);
    }

    // //////////////////////////////////////////////////////////////////////////////
    // 2. 클래스(AudioBase 클래스 상속)
    // //////////////////////////////////////////////////////////////////////////////
    public class AudioController : AudioBase, IAudioController
    {
        // ==============================================================================
        // 1) 내부 타입
        // ==============================================================================
        [Serializable] public class Sounds
        {
            [SerializeField] protected AudioClip                   _gameStart;
            [SerializeField] protected AudioClip                   _buttonClick;
            [SerializeField] protected SimpleData<bool, AudioClip> _menuOpen;

            public AudioClip                   gameStart   { get { return _gameStart; } }
            public AudioClip                   buttonClick { get { return _buttonClick; } }
            public SimpleData<bool, AudioClip> menuOpen    { get { return _menuOpen; } }
        }

        // ==============================================================================
        // 2) 필드
        // ==============================================================================
        // Component & Reference
        public IGameDirector game { get; protected set; }

        // Setting
        [SerializeField] protected Sounds _sounds;

        public Sounds sounds { get { return _sounds; } }

        // ==============================================================================
        // 3) 메서드
        // ==============================================================================
        // ------------------------------------------------------------------------------
        // 3-1) 메서드 -> 초기화
        // ------------------------------------------------------------------------------
        protected override void SetField()
        {
            base.SetField();

            game = GetComponentInParent<IGameDirector>(true);
        }

        protected override void ResetField(AudioSource audioSource)
        {
            base.ResetField(audioSource);

            audioSource.spatialBlend = 0f;
            _audioType               = AudioType.System;
        }

        // ------------------------------------------------------------------------------
        // 3-2) 메서드 -> 재생(Play)
        //    - 시스템 이벤트에 따른 오디오 재생
        // ------------------------------------------------------------------------------
        public virtual float PlayGameStart()
        {
            audioSource.clip = sounds.gameStart;

            return Play();
        }

        public virtual float PlayButtonClick()
        {
            audioSource.clip = sounds.buttonClick;

            return Play();
        }

        public virtual float PlayMenuOpen(bool isOn)
        {
            var sounds = this.sounds.menuOpen;

            audioSource.clip = sounds.ContainsKey(isOn) ? sounds[isOn] : null;

            return Play();
        }
    }
}
