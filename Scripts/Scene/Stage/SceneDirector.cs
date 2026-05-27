// //////////////////////////////////////////////////////////////////////////////
// * 요약
//    - 스테이지 씬 디렉터 클래스
//    - 레벨 시작, 성공, 실패, 체크포인트(진행도 등) 저장 기능
//
// * 목차
//    1. 인터페이스 ... Line 43
//    2. 클래스 ....... Line 68
//        1) 내부 타입 ... Line 73
//            1- 스테이지 리스트 ... Line 148
//        2) 필드 ..... Line 178
//        3) 메서드 ... Line 191
//            1- 초기화 ............ Line 194
//            2- 들어오기(Enter) ... Line 212
//            3- 나가기(Exit) ...... Line 253
//            4- 일시정지(Pause) ... Line 265
//            5- 레벨 .............. Line 292
//                1_ 시작(Start) ... Line 295
//                2_ 실패(Fail) .... Line 337
//                3_ 성공(Clear) ... Line 360
//                4_ 저장(Save) .... Line 381
// //////////////////////////////////////////////////////////////////////////////
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Game;

namespace Stage
{
    using UI                     = SceneDirector.UI;
    using Audios                 = SceneDirector.Audios;
    using Cameras                = SceneDirector.Cameras;
    using Stages                 = SceneDirector.Stages;
    using LauncherState          = global::LauncherController.State.Transport;
    using PlayerDieState         = global::PlayerController.State.Die;
    using GoalType               = global::GoalController.Type;
    using StageData              = Game.DataManager.Stages.Stage;
    using LetterboxUIState       = LetterboxUIController.State;
    using CursorVisibleEventType = GameDirector.CursorVisibleEventType;

    // //////////////////////////////////////////////////////////////////////////////
    // 1. 인터페이스(ISceneBase 인터페이스 상속)
    // //////////////////////////////////////////////////////////////////////////////
    public interface ISceneDirector : ISceneBase
    {
        // 프로퍼티
        // Component
        UI            ui     { get; }
        Audios        audios { get; }
        IScoreManager score  { get; }

        // Reference
        Cameras           cameras { get; }
        IPlayerController player  { get; }
        ILightController  light   { get; }
        Stages            stages  { get; }
        IDataManager      data    { get; }

        // 메서드
        Coroutine StartLevel(bool isFirst);
        Coroutine FailLevel(PlayerDieState type);
        Coroutine ClearLevel(GoalType type);
        void      SaveLevel(ICheckPointable checkPoint);
    }

    // //////////////////////////////////////////////////////////////////////////////
    // 2. 클래스(SceneBase 클래스 상속)
    // //////////////////////////////////////////////////////////////////////////////
    public class SceneDirector : SceneBase, ISceneDirector
    {
        // ==============================================================================
        // 1) 내부 타입
        //    - 같은 타입의 오브젝트 리스트 정의
        // ==============================================================================
        public class UI : List<IUIBase>
        {
            public Canvas             canvas { get; }
            public IMainUIController  main   { get; }
            public IStartUIController start  { get; }
            public IFailUIController  fail   { get; }
            public IClearUIController clear  { get; }

            public UI(Transform transform) : base(transform.GetComponentsInChildren<IUIBase>(true))
            {
                canvas = transform.GetComponent<Canvas>();
                main   = transform.GetComponentInChildren<IMainUIController>(true);
                start  = transform.GetComponentInChildren<IStartUIController>(true);
                fail   = transform.GetComponentInChildren<IFailUIController>(true);
                clear  = transform.GetComponentInChildren<IClearUIController>(true);
            }

            public void Initialize() { foreach (var ui in this) ui.gameObject.SetActive(false); }

            public void SetCanvas(Camera camera)
            {
                canvas.renderMode    = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera   = camera;
                canvas.planeDistance = 1f;
            }
        }

        public class Audios : List<IAudioBase>
        {
            public IBackGroundMusicController bgm    { get; }
            public ISystemAudioController     system { get; }

            public Audios(Transform transform) : base(transform.GetComponentsInChildren<IAudioBase>(true))
            {
                bgm    = transform.GetComponentInChildren<IBackGroundMusicController>(true);
                system = transform.GetComponentInChildren<ISystemAudioController>(true);
            }
        }

        public class Cameras : List<global::ICameraController>
        {
            public ICameraController        main    { get; }
            public IGameSetCameraController gameSet { get; }

            public global::ICameraController current { get; protected set; }

            public Cameras(GameObject gameObject) : base(gameObject.GetComponentsInChildren< global::ICameraController> (true))
            {
                main    = gameObject.GetComponentInChildren<ICameraController>(true);
                gameSet = gameObject.GetComponentInChildren<IGameSetCameraController>(true);
            }

            public void Initialize()
            {
                main.gameObject.SetActive(true);
                gameSet.gameObject.SetActive(false);
            }

            public void TrySetCurrent(global::ICameraController camera)
            {
                if (current != null)
                {
                    if (current == camera) return;

                    current.gameObject.SetActive(false);
                }

                current = camera;
            }
        }

        // ------------------------------------------------------------------------------
        // 1-1) 내부 타입 -> 스테이지
        //    - 현재 선택된 스테이지의 초기화 및 활성화
        // ------------------------------------------------------------------------------
        public class Stages : Dictionary<int, IStageController>
        {
            public IStageController current { get; protected set; }

            public Stages(GameObject gameObject) : base()
            {
                foreach (var stage in gameObject.GetComponentsInChildren<IStageController>(true))
                {
                    int index = stage.transform.GetSiblingIndex() + 1;

                    Add(index, stage);
                }
            }

            public void Initialize(StageData stageData)
            {
                current = this[stageData.id];

                foreach (var stage in Values)
                {
                    if (current == stage) stage.Initialize(stageData);
                    else                  stage.gameObject.SetActive(false);
                }
            }
        }

        // ==============================================================================
        // 2) 필드
        // ==============================================================================
        // Component & Reference
        public UI                   ui      { get; protected set; }
        public Audios               audios  { get; protected set; }
        public IScoreManager        score   { get; protected set; }
        public Cameras              cameras { get; protected set; }
        public IPlayerController    player  { get; protected set; }
        public new ILightController light   { get; protected set; }
        public Stages               stages  { get; protected set; }
        public IDataManager         data => DataManager.instance;

        // ==============================================================================
        // 3) 메서드
        // ==============================================================================
        // ------------------------------------------------------------------------------
        // 3-1) 메서드 -> 초기화
        // ------------------------------------------------------------------------------
        protected override void SetField()
        {
            base.SetField();

            ui      = new UI(transform.Find("UI"));
            audios  = new Audios(transform.Find("Audios"));
            score   = GetComponentInChildren<IScoreManager>(true);
            cameras = new Cameras(GameObject.Find("Cameras"));
            player  = FindObjectOfType<PlayerController>(true);
            light   = FindObjectOfType<LightController>(true);
            stages  = new Stages(GameObject.Find("Stages"));

            type = Type.Stage;
        }

        // ------------------------------------------------------------------------------
        // 3-2) 메서드 -> 들어오기(Enter)
        //    - 오브젝트 초기화
        //    - 현재 스테이지(레벨) 초기화 및 레벨 시작
        // ------------------------------------------------------------------------------
        public override Coroutine Enter(Type prev)
        {
            IGameDirector          game        = GameDirector.instance;
            Game.IDataManager      data        = game.data;
            ILetterboxUIController letterboxUI = game.ui.letterbox;

            ui.Initialize();
            score.Load();
            cameras.Initialize();
            stages.Initialize(data.stages.Current);
            //player.gameObject.SetActive(false);
            light.Follow(player.transform);
            letterboxUI.Display(LetterboxUIState.Closed, false);

            return base.Enter(prev);
        }

        protected override IEnumerator _Enter(Type prev)
        {
            IFadeUIController        fadeUI   = GameDirector.instance.ui.fade;
            IIntroLauncherController launcher = stages.current.levels.current.launcher;

            yield return base._Enter(prev);
            yield return new WaitForSeconds(fadeUI.defaultDuration);

            //player.gameObject.SetActive(true);

            player.input.enabled = false;

            switch (prev)
            {
                case Type.Stage: StartLevel(false);          break;
                default:         launcher.Transport(player); break;
            }
        }

        // ------------------------------------------------------------------------------
        // 3-3) 메서드 -> 나가기(Exit)
        //    - 이벤트(타입)에 따른 씬 변
        // ------------------------------------------------------------------------------
        public override Coroutine Exit(Type next)
        {
            if (next != Type.None) data.Destroy();
            else                   next = Type.Stage;

            return base.Exit(next);
        }

        // ------------------------------------------------------------------------------
        // 3-4) 메서드 -> 일시정지(Pause)
        // ------------------------------------------------------------------------------
        public override void Pause(bool paused, params IAudioBase[] exceptions)
        {
            ICameraController camera = cameras.main;

            base.Pause(paused, exceptions);

            ui.main.Display(!paused);
            player.ui.Hide(paused);

            if (paused)
            {
                score.stopwatch.Stop();
                camera.StopFollow();
                light.StopFollow();
            }
            else
            {
                score.stopwatch.Start();
                camera.StopLookAt();
                camera.Follow(player.cameraTarget);
                light.Follow(player.transform);
            }
        }

        // ------------------------------------------------------------------------------
        // 3-5) 메서드 -> 레벨
        // ------------------------------------------------------------------------------
        // ******************************************************************************
        // 3-5-1) 메서드 -> 레벨 -> 시작(Start)
        //    - 저장된 체크포인트 정보에 따라 오브젝트의 시작 위치나 진행도(Score) 정보를 변경
        // ******************************************************************************
        public virtual Coroutine StartLevel(bool isFirst)
        {
            ILevelController         level       = stages.current.levels.current;
            IIntroLauncherController launcher    = level.launcher;
            ILetterboxUIController   letterboxUI = GameDirector.instance.ui.letterbox;

            letterboxUI.Display(LetterboxUIState.Open);
            audios.bgm.PlayMain();

            if (isFirst) ui.start.Display(true);
            else
            {
                ICheckPointable            checkPoint      = level.checkPoints.current;
                ICharacterTargetController characterTarget = (checkPoint != null) ? checkPoint.characterTarget
                                                                                  : launcher.points.end;

                player.Set(characterTarget);
                cameras.main.Set(player.cameraTarget, 0f);
            }

            return StartCoroutine(_StartLevel(launcher));
        }

        protected virtual IEnumerator _StartLevel(IIntroLauncherController launcher)
        {
            yield return new WaitForSeconds(launcher.durations[LauncherState.Land].total);

            ui.start.gameObject.SetActive(false);
            ui.main.Display(true);
            score.stopwatch.Start();
            cameras.main.Follow(player.cameraTarget);
            player.Idle();

            if (Application.platform == RuntimePlatform.Android) player.ui.virtualJoystick.Display(true);

            player.input.enabled = true;
        }

        // ******************************************************************************
        // 3-5-2) 메서드 -> 레벨 -> 실패(Fail)
        //    - 플레이어 사망 시 호출됨
        //    - 씬을 멈춘 후 실패 메뉴(UI) 호출
        // ******************************************************************************
        public virtual Coroutine FailLevel(PlayerDieState type)
        {
            if (type == PlayerDieState.Bungee) Time.timeScale = 1f;

            return StartCoroutine(_FailLevel(type));
        }

        protected virtual IEnumerator _FailLevel(PlayerDieState type)
        {
            IGameDirector game = GameDirector.instance;

            yield return ui.fail.Display(true);

            Time.timeScale = 0f;

            game.SetCursorVisible(CursorVisibleEventType.MenuOpen, true);
        }

        // ******************************************************************************
        // 3-5-3) 메서드 -> 레벨 -> 성공(Clear)
        //    - 플레이어가 골에 도달 시 호출됨
        //    - 씬을 멈춘 후 스코어 정보가 포함된 성공 UI 호출
        // ******************************************************************************
        public virtual Coroutine ClearLevel(GoalType type)
        {
            score.Write();

            return StartCoroutine(_ClearLevel());
        }

        public virtual IEnumerator _ClearLevel()
        {
            IGameDirector game = GameDirector.instance;

            yield return ui.clear.Display(true);

            game.SetCursorVisible(CursorVisibleEventType.MenuOpen, true);
        }

        // ******************************************************************************
        // 3-5-4) 메서드 -> 레벨 -> 저장(Save)
        //    - 체크포인트 도달 시 현재 레벨의 진행도 저장
        //    - 체크포인트는 물리적인 오브젝트가 아닌 이벤트 형식으로 여러 오브젝트에서 재정의하여 호출
        //    - ex) Planet(Field) 클리어 시 체크포인트 저장 등
        // ******************************************************************************
        public void SaveLevel(ICheckPointable checkPoint)
        {
            score.Save();
            stages.current.levels.current.Save(checkPoint);
        }
    }
}
