// //////////////////////////////////////////////////////////////////////////////
// * 요약
//    - 로비 씬 전용 BGM 오디오 클래스
//    - 로비의 메뉴마다 개별 BGM 적용
//
// * 목차
//    1. 인터페이스 ... Line 21
//    2. 클래스 ....... Line 37
//        1) 필드 ..... Line 42
//        2) 메서드 ... Line 53
//            1- 초기화 ....... Line 56
//            2- 재생(Play) ... Line 66
// //////////////////////////////////////////////////////////////////////////////
using UnityEngine;

namespace Lobby
{
    using MenuType = MenuBase.Type;

    // //////////////////////////////////////////////////////////////////////////////
    // 1. 인터페이스(global::IBackGroundMusicController 인터페이스 상속)
    // //////////////////////////////////////////////////////////////////////////////
    public interface IBackGroundMusicController : global::IBackGroundMusicController
    {
        // 프로퍼티
        // Reference
        new ISceneDirector scene { get; }

        // Setting
        SimpleData<MenuType, AudioClip> sounds { get; }

        // 메서드
        float Play(MenuType type);
    }

    // //////////////////////////////////////////////////////////////////////////////
    // 2. 클래스(global::BackGroundMusicController 클래스 상속)
    // //////////////////////////////////////////////////////////////////////////////
    public class BackGroundMusicController : global::BackGroundMusicController, IBackGroundMusicController
    {
        // ==============================================================================
        // 1) 필드
        // ==============================================================================
        // Component & Reference
        public new ISceneDirector scene { get; protected set; }

        // Setting
        [SerializeField] protected SimpleData<MenuType, AudioClip> _sounds;

        public SimpleData<MenuType, AudioClip> sounds { get { return _sounds; } }

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
        //    - 메뉴 타입에 따른 개별 BGM 재생
        // ------------------------------------------------------------------------------
        public virtual float Play(MenuType type)
        {
            audioSource.clip = sounds.ContainsKey(type) ? sounds[type] : null;

            return Play();
        }
    }
}
