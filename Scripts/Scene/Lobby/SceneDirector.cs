// //////////////////////////////////////////////////////////////////////////////
// * 요약
//    - 로비 씬 디렉터 클래스
//
// * 목차
//    1. 인터페이스 ... Line 26
//    2. 클래스 ....... Line 37
//        1) 필드 ..... Line 42
//        2) 메서드 ... Line 49
//            1- 이벤트 함수 ....... Line 52
//            2- 초기화 ............ Line 57
//            3- 들어오기(Enter) ... Line 70
//            4- 나가기(Exit) ...... Line 96
//            5- 일시정지(Pause) ... Line 106
// //////////////////////////////////////////////////////////////////////////////
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

    // //////////////////////////////////////////////////////////////////////////////
    // 1. 인터페이스(ISceneBase 인터페이스 상속)
    // //////////////////////////////////////////////////////////////////////////////
    public interface ISceneDirector : ISceneBase
    {
        // 프로퍼티
        // Component
        new IBackGroundMusicController bgm  { get; }
        Menu                           menu { get; }

        // Reference
        Cameras           cameras { get; }
        IPlayerController player  { get; }
        IPlanetController planet  { get; }
        Stages            stages  { get; }
    }

    // //////////////////////////////////////////////////////////////////////////////
    // 2. 클래스(SceneBase 클래스 상속)
    // //////////////////////////////////////////////////////////////////////////////
    public class SceneDirector : SceneBase, ISceneDirector
    {
        // ==============================================================================
        // 1) 내부 타입
        // ==============================================================================
        // ------------------------------------------------------------------------------
        // 1-1) 내부 타입 -> 메뉴 리스트
        // ------------------------------------------------------------------------------
        public class Menu : Dictionary<MenuType, IMenuBase>
        {
            // 필드
            public Canvas                canvas    { get; }
            public IMainMenuManager      main      { get; }
            public ICharacterMenuManager character { get; }
            public IStageMenuManager     stage     { get; }

            // 생성자
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

            // 메서드
            public void Initialize() { foreach (var menu in Values) menu.Initialize(); }

            public void Pause(bool paused)
            {
                foreach (var menu in Values)
                {
                    IUIBase ui = menu.ui;

                    if (ui.gameObject.activeInHierarchy) ui.SetInteractables(!paused);
                }
            }
        }

        // ------------------------------------------------------------------------------
        // 1-2) 내부 타입 -> 카메라 리스트
        // ------------------------------------------------------------------------------
        public class Cameras : List<global::ICameraController>
        {
            // 필드
            public ICameraController              main          { get; }
            public ICharacterViewCameraController characterView { get; }

            public global::ICameraController current { get; protected set; }

            // 생성자
            public Cameras(GameObject gameObject) : base(gameObject.GetComponentsInChildren<global::ICameraController>(true))
            {
                main          = gameObject.GetComponentInChildren<ICameraController>(true);
                characterView = gameObject.GetComponentInChildren<ICharacterViewCameraController>(true);
            }

            // 메서드
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
        }

        // ------------------------------------------------------------------------------
        // 1-3) 내부 타입 -> 스테이지(오브젝트) 리스트
        // ------------------------------------------------------------------------------
        public class Stages : Dictionary<int, IStageController>
        {
            // 필드
            public IStageController current { get; protected set; }

            // 생성자
            public Stages(GameObject gameObject) : base()
            {
                foreach (var stage in gameObject.GetComponentsInChildren<IStageController>(true))
                {
                    int index = stage.transform.GetSiblingIndex() + 1;

                    Add(index, stage);
                }
            }

            // 메서드
            public void Initialize(StageData stageData)
            {
                current               = this[stageData.id];
                RenderSettings.skybox = current.skybox;

                foreach (var stage in Values) stage.gameObject.SetActive(false);
            }
        }

        // ==============================================================================
        // 2) 필드
        // ==============================================================================
        // Component & Reference
        public new IBackGroundMusicController bgm     { get; protected set; }
        public Menu                           menu    { get; protected set; }
        public Cameras                        cameras { get; protected set; }
        public IPlayerController              player  { get; protected set; }
        public IPlanetController              planet  { get; protected set; }
        public Stages                         stages  { get; protected set; }

        // ==============================================================================
        // 3) 메서드
        // ==============================================================================
        // ------------------------------------------------------------------------------
        // 3-1) 메서드 -> 초기화
        // ------------------------------------------------------------------------------
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

        // ------------------------------------------------------------------------------
        // 3-2) 메서드 -> 들어오기(Enter)
        //    - 오브젝트 초기화
        //    - 이전 씬에 따른 특정 메뉴 오픈
        // ------------------------------------------------------------------------------
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

        // ------------------------------------------------------------------------------
        // 3-3) 메서드 -> 일시정지(Pause)
        // ------------------------------------------------------------------------------
        public override void Pause(bool paused, params IAudioBase[] exceptions)
        {
            base.Pause(paused, exceptions);
            menu.Pause(paused);
        }
    }
}
