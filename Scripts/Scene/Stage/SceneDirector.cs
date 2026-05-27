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


    public interface ISceneDirector : ISceneBase
    {
        #region Property

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

        #endregion


        #region Method

        Coroutine StartLevel(bool isFirst);
        Coroutine FailLevel(PlayerDieState type);
        Coroutine ClearLevel(GoalType type);
        void      SaveLevel(ICheckPointable checkPoint);

        #endregion
    }


    public class SceneDirector : SceneBase, ISceneDirector
    {
        #region Definition

        public class UI : List<IUIBase>
        {
            #region Field

            public Canvas             canvas { get; }
            public IMainUIController  main   { get; }
            public IStartUIController start  { get; }
            public IFailUIController  fail   { get; }
            public IClearUIController clear  { get; }

            #endregion


            #region Constructor

            public UI(Transform transform) : base(transform.GetComponentsInChildren<IUIBase>(true))
            {
                canvas = transform.GetComponent<Canvas>();
                main   = transform.GetComponentInChildren<IMainUIController>(true);
                start  = transform.GetComponentInChildren<IStartUIController>(true);
                fail   = transform.GetComponentInChildren<IFailUIController>(true);
                clear  = transform.GetComponentInChildren<IClearUIController>(true);
            }

            #endregion


            #region Method

            public void Initialize() { foreach (var ui in this) ui.gameObject.SetActive(false); }

            public void SetCanvas(Camera camera)
            {
                canvas.renderMode    = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera   = camera;
                canvas.planeDistance = 1f;
            }

            #endregion
        }


        public class Audios : List<IAudioBase>
        {
            #region Field

            public IBackGroundMusicController bgm    { get; }
            public ISystemAudioController     system { get; }

            #endregion


            #region Constructor

            public Audios(Transform transform) : base(transform.GetComponentsInChildren<IAudioBase>(true))
            {
                bgm    = transform.GetComponentInChildren<IBackGroundMusicController>(true);
                system = transform.GetComponentInChildren<ISystemAudioController>(true);
            }

            #endregion
        }


        public class Cameras : List<global::ICameraController>
        {
            #region Field

            public ICameraController        main    { get; }
            public IGameSetCameraController gameSet { get; }

            public global::ICameraController current { get; protected set; }

            #endregion


            #region Constructor

            public Cameras(GameObject gameObject) : base(gameObject.GetComponentsInChildren< global::ICameraController> (true))
            {
                main    = gameObject.GetComponentInChildren<ICameraController>(true);
                gameSet = gameObject.GetComponentInChildren<IGameSetCameraController>(true);
            }

            #endregion


            #region Method

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

            #endregion
        }


        public class Stages : Dictionary<int, IStageController>
        {
            #region Field

            public IStageController current { get; protected set; }

            #endregion


            #region Constructor

            public Stages(GameObject gameObject) : base()
            {
                foreach (var stage in gameObject.GetComponentsInChildren<IStageController>(true))
                {
                    int index = stage.transform.GetSiblingIndex() + 1;

                    Add(index, stage);
                }
            }

            #endregion


            #region Method

            public void Initialize(StageData stageData)
            {
                current = this[stageData.id];

                foreach (var stage in Values)
                {
                    if (current == stage) stage.Initialize(stageData);
                    else                  stage.gameObject.SetActive(false);
                }
            }

            #endregion
        }

        #endregion


        #region Field

        public UI                   ui      { get; protected set; }
        public Audios               audios  { get; protected set; }
        public IScoreManager        score   { get; protected set; }
        public Cameras              cameras { get; protected set; }
        public IPlayerController    player  { get; protected set; }
        public new ILightController light   { get; protected set; }
        public Stages               stages  { get; protected set; }
        public IDataManager         data => DataManager.instance;

        #endregion


        #region Method

        #region Initialization

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

        #endregion


        #region Enter

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

        #endregion


        #region Exit

        public override Coroutine Exit(Type next)
        {
            if (next != Type.None) data.Destroy();
            else                   next = Type.Stage;

            return base.Exit(next);
        }

        #endregion


        #region Pause

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

        #endregion


        #region Start

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

        #endregion


        #region Fail

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

        #endregion


        #region Clear

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

        #endregion


        #region Save

        public void SaveLevel(ICheckPointable checkPoint)
        {
            score.Save();
            stages.current.levels.current.Save(checkPoint);
        }

        #endregion

        #endregion
    }
}
