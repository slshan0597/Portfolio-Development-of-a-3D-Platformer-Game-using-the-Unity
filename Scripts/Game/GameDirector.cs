// //////////////////////////////////////////////////////////////////////////////
// * 요약
//    - 게임 시스템 관리자
//    - 각 씬과 서로 참조하여 데이터를 주고 받는 최상단 버스 역할
//
// * 목차
//    1. 인터페이스 ... Line 33
//    2. 클래스 ....... Line 55
//        1) 내부 타입 ... Line 60
//        2) 필드 ..... Line 132
//        3) 메서드 ... Line 148
// //////////////////////////////////////////////////////////////////////////////
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace Game
{
    using Menu                   = GameDirector.Menu;
    using UI                     = GameDirector.UI;
    using Scenes                 = GameDirector.Scenes;
    using CursorVisibleEventType = GameDirector.CursorVisibleEventType;
    using LetterboxUIState       = LetterboxUIController.State;
    using MenuType               = MenuBase.Type;
    using SceneType              = SceneBase.Type;
    using AudioType              = AudioBase.Type;
    using ControlState           = ControlSettingManager.State;

    // //////////////////////////////////////////////////////////////////////////////
    // 1. 인터페이스
    // //////////////////////////////////////////////////////////////////////////////
    public interface IGameDirector
    {
        // 프로퍼티
        // Component
        GameObject            gameObject  { get; }
        Menu                  menu        { get; }
        UI                    ui          { get; }
        IAudioController      audio       { get; }
        IDataManager          data        { get; }
        IAnimationCurvePreset curvePreset { get; }

        // Reference
        Scenes scenes { get; }

        // 메서드
        void SetCursorVisible(CursorVisibleEventType type, bool visible);
        void PauseAudios(bool paused, params IAudioBase[] exceptions);
    }

    // //////////////////////////////////////////////////////////////////////////////
    // 2. 클래스
    // //////////////////////////////////////////////////////////////////////////////
    public class GameDirector : MonoBehaviour, IGameDirector
    {
        // ==============================================================================
        // 1) 내부 타입
        // ==============================================================================
        public class Menu : Dictionary<MenuType, IMenuBase>
        {
            public Canvas              canvas  { get; }
            public IMainMenuManager    main    { get; }
            public ISaveMenuManager    save    { get; }
            public ISettingMenuManager setting { get; }

            public Menu(Transform transform) : base()
            {
                foreach (var menu in transform.GetComponentsInChildren<IMenuBase>(true))
                {
                    string name = menu.gameObject.name.Replace(" ", string.Empty).Replace("Menu", string.Empty);

                    if (Enum.TryParse(name, out MenuType type)) Add(type, menu);
                }

                canvas  = transform.GetComponent<Canvas>();
                main    = transform.GetComponentInChildren<IMainMenuManager>(true);
                save    = transform.GetComponentInChildren<ISaveMenuManager>(true);
                setting = transform.GetComponentInChildren<ISettingMenuManager>(true);
            }
        }

        public class UI : List<IUIBase>
        {
            public Canvas                 canvas    { get; }
            public IConfirmUIController   confirm   { get; }
            public INoticeUIController    notice    { get; }
            public ILetterboxUIController letterbox { get; }
            public IFadeUIController      fade      { get; }

            public UI(Transform transform) : base(transform.GetComponentsInChildren<IUIBase>(true))
            {
                canvas    = transform.GetComponent<Canvas>();
                confirm   = transform.GetComponentInChildren<IConfirmUIController>(true);
                notice    = transform.GetComponentInChildren<INoticeUIController>(true);
                letterbox = transform.GetComponentInChildren<ILetterboxUIController>(true);
                fade      = transform.GetComponentInChildren<IFadeUIController>(true);
            }

            public void Initialize()
            {
                foreach (var ui in this) ui.gameObject.SetActive(false);

                letterbox.Display(LetterboxUIState.Open, false);
                fade.gameObject.SetActive(true);
            }
        }

        public class Scenes
        {
            public KeyValuePair<SceneType, ISceneBase> previous { get; private set; }
            public KeyValuePair<SceneType, ISceneBase> current  { get; private set; }

            public Scenes()
            {
                previous = new KeyValuePair<SceneType, ISceneBase>(SceneType.None, null);
                current  = new KeyValuePair<SceneType, ISceneBase>(SceneType.None, null);
            }

            public void Set(ISceneBase scene)
            {
                previous = new KeyValuePair<SceneType, ISceneBase>(current.Key, null);
                current  = new KeyValuePair<SceneType, ISceneBase>(scene.type, scene);
            }
        }

        public enum CursorVisibleEventType { MenuOpen, ControllerChange }

        // ==============================================================================
        // 2) 필드
        // ==============================================================================
        public static IGameDirector instance;

        // Component & Reference
        public Menu                  menu        { get; protected set; }
        public UI                    ui          { get; protected set; }
        public new IAudioController  audio       { get; protected set; }
        public IDataManager          data        { get; protected set; }
        public IAnimationCurvePreset curvePreset { get; protected set; }
        public Scenes                scenes      { get; protected set; }

        // etc.
        protected Dictionary<CursorVisibleEventType, bool> cursorVisibles;

        // ==============================================================================
        // 3) 메서드
        // ==============================================================================
        // ------------------------------------------------------------------------------
        // 3-1) 메서드 -> 이벤트 함수
        // ------------------------------------------------------------------------------
        protected virtual void Awake()
        {
            CreateSingleton();
            SetField();
        }

        protected virtual void Start()
        {
            ui.Initialize();
            SceneManager.LoadScene("Title");
        }

        // ------------------------------------------------------------------------------
        // 3-2) 메서드 -> 초기화
        // ------------------------------------------------------------------------------
        protected virtual void CreateSingleton()
        {
            if (instance == null)
            {
                instance = this;

                DontDestroyOnLoad(gameObject);
                DontDestroyOnLoad(EventSystem.current.gameObject);
            }
            else if (gameObject != instance.gameObject) Destroy(gameObject);
        }

        protected virtual void SetField()
        {
            menu        = new Menu(transform.Find("Menu"));
            ui          = new UI(transform.Find("UI"));
            audio       = GetComponentInChildren<IAudioController>(true);
            data        = GetComponentInChildren<IDataManager>(true);
            curvePreset = GetComponentInChildren<IAnimationCurvePreset>(true);
            scenes      = new Scenes();

            cursorVisibles = new Dictionary<CursorVisibleEventType, bool>();

            foreach (CursorVisibleEventType type in Enum.GetValues(typeof(CursorVisibleEventType))) 
                cursorVisibles.Add(type, false);
        }

        // ------------------------------------------------------------------------------
        // 3-3) 메서드 -> 유틸(커서, 오디오)
        // ------------------------------------------------------------------------------
        public virtual void SetCursorVisible(CursorVisibleEventType type, bool visible)
        {
            IControlSettingManager controlSetting = menu.setting.control;

                 cursorVisibles[type] = visible;
            bool result               = true;

            foreach (bool value in cursorVisibles.Values) result &= value;

            Cursor.visible = result;

            switch (controlSetting.state)
            {
                case ControlState.Keyboard: Cursor.lockState = result ? CursorLockMode.None : CursorLockMode.Locked; break;
                default:                    Cursor.lockState = CursorLockMode.None;                                  break;
            }
        }

        public virtual void PauseAudios(bool paused, params IAudioBase[] exceptions)
        {
            IAudioSettingManager audioSetting = menu.setting.audio;

            foreach (var audio in audioSetting.connectedAudios)
            {
                if (exceptions.Contains(audio) || (audio.audioType == AudioType.System)) continue;

                audio.Pause(paused);
            }
        }
    }
}
