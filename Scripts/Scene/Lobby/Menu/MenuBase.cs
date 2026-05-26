// //////////////////////////////////////////////////////////////////////////////
// * 요약
//    - 로비 씬의 메뉴의 기반 클래스
//
// * 목차
//    1. 인터페이스 ... Line 25
//    2. 클래스 ....... Line 47
//        1) 내부 타입 ... Line 52
//        2) 필드 ........ Line 57
//        3) 메서드 ...... Line 68
//            1- 이벤트 함수 ... Line 71
//            2- 초기화 ........ Line 76
//            3- 열기(Open) .... Line 98
// //////////////////////////////////////////////////////////////////////////////
using System.Collections;
using System.Linq;
using UnityEngine;

namespace Lobby
{
    using Type            = MenuBase.Type;
    using PlayerEmoteType = PlayerModelController.EmoteType;

    // //////////////////////////////////////////////////////////////////////////////
    // 1. 인터페이스
    // //////////////////////////////////////////////////////////////////////////////
    public interface IMenuBase
    {
        // 프로퍼티
        // Component
        GameObject              gameObject   { get; }
        IUIBase                 ui           { get; }
        ICameraTargetController cameraTarget { get; }

        // Reference
        ISceneDirector scene { get; }

        // State
        Type type { get; }

        // 메서드
        void      Initialize();
        Coroutine Open(bool isActive, IMenuBase prev = null);
    }

    // //////////////////////////////////////////////////////////////////////////////
    // 2. 클래스
    // //////////////////////////////////////////////////////////////////////////////
    public class MenuBase : MonoBehaviour, IMenuBase
    {
        // ==============================================================================
        // 1) 내부 타입
        // ==============================================================================
        public enum Type { None, Main, Character, Stage }

        // ==============================================================================
        // 2) 필드
        // ==============================================================================
        // Component & Reference
        public IUIBase                 ui           { get; protected set; }
        public ICameraTargetController cameraTarget { get; protected set; }
        public ISceneDirector          scene        { get; protected set; }

        // State
        public Type type { get; protected set; }

        // ==============================================================================
        // 3) 메서드
        // ==============================================================================
        // ------------------------------------------------------------------------------
        // 3-1) 메서드 -> 이벤트 함수
        // ------------------------------------------------------------------------------
        protected virtual void Awake() { SetField(); }

        // ------------------------------------------------------------------------------
        // 3-2) 메서드 -> 초기화
        // ------------------------------------------------------------------------------
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

        // ------------------------------------------------------------------------------
        // 3-3) 메서드 -> 열기(Open)
        //    - 캐릭터, 카메라 등의 오브젝트 조정
        // ------------------------------------------------------------------------------
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
    }
}
