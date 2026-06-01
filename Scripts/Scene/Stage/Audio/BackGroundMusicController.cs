// //////////////////////////////////////////////////////////////////////////////
// * 요약
//    - 스테이지 씬 전용 BGM 오디오 클래스
//    - 스테이지의 상황마다 개별 BGM 적용
//
// * 목차
//    1. 인터페이스 ... Line 22
//    2. 클래스 ....... Line 39
//        1) 필드 ..... Line 46
//        2) 메서드 ... Line 57
//            1- 초기화 ....... Line 60
//            2- 재생(Play) ... Line 70
// //////////////////////////////////////////////////////////////////////////////
using UnityEngine;

namespace Stage
{
    using SoundType          = BackGroundMusicController.SoundType;
    using PlayerOverlapState = global::PlayerController.State.Overlap.State;

    // //////////////////////////////////////////////////////////////////////////////
    // 1. 인터페이스(global::IBackGroundMusicController 인터페이스 상속)
    // //////////////////////////////////////////////////////////////////////////////
    public interface IBackGroundMusicController : global::IBackGroundMusicController
    {
        // 프로퍼티
        // Reference
        new ISceneDirector scene { get; }

        // Setting
        SimpleData<SoundType, AudioClip> sounds { get; }

        // 메서드
        void Play(SoundType type);
        void PlayMain();
    }

    // //////////////////////////////////////////////////////////////////////////////
    // 2. 클래스(global::BackGroundMusicController 클래스 상속)
    // //////////////////////////////////////////////////////////////////////////////
    public class BackGroundMusicController : global::BackGroundMusicController, IBackGroundMusicController
    {
        public enum SoundType { None, Main, Underground, Boss, BeforeGoal }

        // ==============================================================================
        // 1) 필드
        // ==============================================================================
        // Component & Reference
        public new ISceneDirector scene { get; protected set; }

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

        // ------------------------------------------------------------------------------
        // 2-2) 메서드 -> 재생(Play)
        //    - 상황(타입)에 따른 개별 BGM 재생
        // ------------------------------------------------------------------------------
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
    }
}
