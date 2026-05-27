// //////////////////////////////////////////////////////////////////////////////
// * 요약
//    - 기존 클래스를 스테이지 씬 전용으로 확장(상속)
//    - 필드 클리어 기능 추가 및 클리어 시 보상(Transporter or Goal) 활성화
//
// * 목차
//    1. 인터페이스 ... Line 
//    2. 클래스 ....... Line 
//        1) 필드 ..... Line 
//        2) 메서드 ... Line 
//            1- 초기화 ........ Line 
//            2- 셋(Set) ....... Line 
//            3- 저장(Save) .... Line 
//            4- 성공(Clear) ... Line 
// //////////////////////////////////////////////////////////////////////////////
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

    // //////////////////////////////////////////////////////////////////////////////
    // 1. 인터페이스(global::IPlanetController 클래스 상속)
    // //////////////////////////////////////////////////////////////////////////////
    public interface IPlanetController : global::IPlanetController
    {
        // 프로퍼티
        // Component
        new IPlanetCoreController core         { get; }
        ICameraTargetController   cameraTarget { get; }
        IRewardable               reward       { get; }

        // Reference
        ILevelController level { get; }

        // Setting
        Type type { get; }

        // 메서드
        void      Initialize(int index);
        void      Save(int index);
        bool      CheckClearable();
        void      TryClear();
        Coroutine Clear(bool animated = true);
    }

    // //////////////////////////////////////////////////////////////////////////////
    // 2. 클래스(global::PlanetController 클래스 상속)
    //    - CheckPointable : 필드 클리어 시 체크포인트 기능 활성화
    // //////////////////////////////////////////////////////////////////////////////
    public class PlanetController : global::PlanetController, IPlanetController, ICheckPointable
    {
        public enum Type { Normal, Underground }

        // ==============================================================================
        // 1) 필드
        // ==============================================================================
        // Component & Reference
        public new IPlanetCoreController core         { get; protected set; }
        public ICameraTargetController   cameraTarget { get; protected set; }
        public IRewardable               reward       { get; protected set; }
        public ILevelController          level        { get; protected set; }

        // Setting
        [SerializeField] protected Type _type;
        [SerializeField] protected bool _useCheckPoint;

        public Type type          { get { return _type; } }
        public bool useCheckPoint { get { return _useCheckPoint; } }

        // etc.
        protected int triggeredCount, maxTriggeredCount;

        // ==============================================================================
        // 2) 메서드
        // ==============================================================================
        // ------------------------------------------------------------------------------
        // 2-1) 메서드 -> 초기화
        // ------------------------------------------------------------------------------
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

        // ------------------------------------------------------------------------------
        // 2-2) 메서드 -> 셋(Set)
        //    - 플레이어가 필드 상에 존재 시 활성화
        // ------------------------------------------------------------------------------
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

        // ------------------------------------------------------------------------------
        // 2-3) 메서드 -> 저장(Save)
        //    - 체크포인트 활성화 시 현재 필드의 진행 정보 저장
        // ------------------------------------------------------------------------------
        public virtual void Save(int index)
        {
            if (reward == null) return;

            IDataManager data = level.stage.scene.data;

            data.level.planetsCleared[index] = reward.gameObject.activeInHierarchy;
        }

        // ------------------------------------------------------------------------------
        // 2-4) 메서드 -> 성공(Clear)
        //    - 필드 상의 클리어 트리거 발동 시 호출됨
        //    - 클리어에 대한 보상(Transporter or Goal) 활성화
        // ------------------------------------------------------------------------------
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
    }
}
