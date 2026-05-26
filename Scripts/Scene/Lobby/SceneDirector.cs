using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Game;


namespace Lobby
{
    using Menu                   = SceneDirector.Menu;
    using Cameras                = SceneDirector.Cameras;
    using Stages                 = SceneDirector.Stages;
    using MenuType               = MenuBase.Type;
    using StageData              = DataManager.Stages.Stage;
    using CursorVisibleEventType = GameDirector.CursorVisibleEventType;


    public interface ISceneDirector : ISceneBase
    {
        #region Property

        // Component
        new IBackGroundMusicController bgm  { get; }
        Menu                           menu { get; }

        // Reference
        Cameras           cameras { get; }
        IPlayerController player  { get; }
        IPlanetController planet  { get; }
        Stages            stages  { get; }

        #endregion
    }


    public class SceneDirector : SceneBase, ISceneDirector
    {
        #region Definition

        public class Menu : Dictionary<MenuType, IMenuBase>
        {
            #region Field

            public Canvas                canvas    { get; }
            public IMainMenuManager      main      { get; }
            public ICharacterMenuManager character { get; }
            public IStageMenuManager     stage     { get; }

            #endregion


            #region Constructor

            public Menu(Transform transform) : base()
            {
                foreach (var menu in transform.GetComponentsInChildren<IMenuBase>(true))
                {
                    string name = menu.gameObject.name.Replace(" ", string.Empty).Replace("Menu", string.Empty);

                    if (Enum.TryParse(name, out MenuType type)) Add(type, menu);
                }

                canvas    = transform.GetComponent<Canvas>();
                main      = transform.GetComponentInChildren<IMainMenuManager>(true);
                character = transform.GetComponentInChildren<ICharacterMenuManager>(true);
                stage     = transform.GetComponentInChildren<IStageMenuManager>(true);
            }

            #endregion


            #region Method

            public void Initialize() { foreach (var menu in Values) menu.Initialize(); }

            public void Pause(bool paused)
            {
                foreach (var menu in Values)
                {
                    IUIBase ui = menu.ui;

                    if (ui.gameObject.activeInHierarchy) ui.SetInteractables(!paused);
                }
            }

            #endregion
        }


        public class Cameras : List<global::ICameraController>
        {
            #region Field

            public ICameraController              main          { get; }
            public ICharacterViewCameraController characterView { get; }

            public global::ICameraController current { get; protected set; }

            #endregion


            #region Constructor

            public Cameras(GameObject gameObject) : base(gameObject.GetComponentsInChildren<global::ICameraController>(true))
            {
                main          = gameObject.GetComponentInChildren<ICameraController>(true);
                characterView = gameObject.GetComponentInChildren<ICharacterViewCameraController>(true);
            }

            #endregion


            #region Method

            public void Initialize(ICameraTargetController cameraTarget)
            {
                foreach (var camera in this) camera.gameObject.SetActive(false);

                main.Set(cameraTarget, 0f);
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
                current               = this[stageData.id];
                RenderSettings.skybox = current.skybox;

                foreach (var stage in Values) stage.gameObject.SetActive(false);
            }

            #endregion
        }

        #endregion


        #region Field

        public new IBackGroundMusicController bgm     { get; protected set; }
        public Menu                           menu    { get; protected set; }
        public Cameras                        cameras { get; protected set; }
        public IPlayerController              player  { get; protected set; }
        public IPlanetController              planet  { get; protected set; }
        public Stages                         stages  { get; protected set; }

        #endregion


        #region Method

        #region Initialization

        protected override void SetField()
        {
            base.SetField();

            bgm     = GetComponentInChildren<IBackGroundMusicController>(true);
            menu    = new Menu(transform.Find("Menu"));
            cameras = new Cameras(GameObject.Find("Cameras"));
            player  = FindObjectOfType<PlayerController>(true);
            planet  = FindObjectOfType<PlanetController>(true);
            stages  = new Stages(GameObject.Find("Stages"));

            type = Type.Lobby;
        }

        #endregion


        #region Enter

        public override Coroutine Enter(Type prev)
        {
            IDataManager data = GameDirector.instance.data;

            menu.Initialize();
            cameras.Initialize(menu.stage.cameraTarget);
            planet.Initialize();
            player.Set(planet.characterTarget);
            stages.Initialize(data.stages.Current);

            return base.Enter(prev);
        }

        protected override IEnumerator _Enter(Type prev)
        {
            IIntroLauncherController launcher = planet.launchers.intro;
            IGameDirector            game     = GameDirector.instance;
            IFadeUIController        fadeUI   = game.ui.fade;

            yield return new WaitForSeconds(fadeUI.defaultDuration);

            fadeUI.Display(false);

            switch (prev)
            {
                case Type.Tutorial:
                    {
                        yield return launcher.Transport(player);

                        menu.main.Open(true);
                    }
                    break;

                case Type.Stage or Type.Lobby:
                    {
                        yield return launcher.Transport(player);

                        menu.stage.Open(true);
                    }
                    break;

                default:
                    {
                        yield return cameras.main.Set(menu.main.cameraTarget, fadeUI.defaultDuration * 2f);

                        menu.main.Open(true);
                    }
                    break;
            }

            game.SetCursorVisible(CursorVisibleEventType.MenuOpen, true);
        }

        #endregion


        #region Pause

        public override void Pause(bool paused, params IAudioBase[] exceptions)
        {
            base.Pause(paused, exceptions);
            menu.Pause(paused);
        }

        #endregion

        #endregion
    }
}
