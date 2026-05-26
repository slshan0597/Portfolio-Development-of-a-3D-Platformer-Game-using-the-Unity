// //////////////////////////////////////////////////////////////////////////////
// * 요약
//    - 기반 클래스(SettingBase)의 확장 클래스
//    - 입력 타입(Keyboard, Joystick, Touch) 변경 시 연결된 UI의 내용 변경
//    - 설정 창(UI)에서 맵핑(Mapping)값 변경 시 연결된 UI의 내용 변경
//    - 키-값 쌍의 데이터 형태로 저장 및 불러오기
//
// * 목차
//    1. 인터페이스 ... Line 44
//    2. 클래스 ....... Line 83
//        1) 내부 타입 ... Line 88
//            1- 데이터 ... Line 94
//                1_ 카메라 .......... Line 100
//                2_ 맵핑(Mapping) ... Line 128
//        2) 필드 ..... Line 245
//        3) 메서드 ... Line 263
//            1- 이벤트 함수 .... Line 267
//            2- 초기화 ......... Line 287
//            3- 조이스틱 감지 ... Line 552
//            4- 데이터 ......... Line 631
//                1_ 불러오기(Load) ... Line 645
//                2_ 저장(Save) ....... Line 727
// //////////////////////////////////////////////////////////////////////////////
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Game
{
    using State                  = ControlSettingManager.State;
    using Data                   = ControlSettingManager.Data;
    using Camera                 = ControlSettingManager.Data.Camera;
    using InversionType          = ControlSettingManager.Data.Camera.InversionType;
    using SystemType             = ControlSettingManager.Data.SystemType;
    using ConnectedUI            = ControlSettingManager.ConnectedUI;
    using PlayerMainState        = PlayerController.State.Main;
    using Mappings               = ControlSettingManager.Data.Mappings;
    using Mapping                = ControlSettingManager.Data.Mappings.Mapping;
    using CursorVisibleEventType = GameDirector.CursorVisibleEventType;

    // //////////////////////////////////////////////////////////////////////////////
    // 1. 인터페이스(ISettingBase 인터페이스 상속)
    // //////////////////////////////////////////////////////////////////////////////
    public interface IControlSettingManager : ISettingBase
    {
        // 프로퍼티
        // Reference
        ConnectedUI                     connectedUI          { get; }
        IControlSettingListUIController controlSettingListUI { get; }

        // Data
        Data data { get; }

        // State
        State state { get; }

        // Setting
        Data defaultData { get; }

        // 메서드
        public KeyCode GetKey(PlayerMainState type)
        {
            switch (state)
            {
                case State.Touch: return KeyCode.None;
                default:          return data.GetKey(type, Application.platform, state);
            }
        }

        public KeyCode GetKey(SystemType type)
        {
            switch (state)
            {
                case State.Touch: return KeyCode.None;
                default:          return data.GetKey(type, Application.platform, state);
            }
        }
    }

    // //////////////////////////////////////////////////////////////////////////////
    // 2. 클래스(SettingBase 클래스 상속)
    // //////////////////////////////////////////////////////////////////////////////
    public class ControlSettingManager : SettingBase, IControlSettingManager
    {
        // ==============================================================================
        // 1) 내부 타입
        // ==============================================================================
        // 입력 타입(플랫폼 타입이 아님)
        public enum State { Keyboard, Joystick, Touch }

        // ------------------------------------------------------------------------------
        // 1-1) 내부 타입 -> 데이터
        //    - 컨트롤에 대한 설정값들을 저장
        // ------------------------------------------------------------------------------
        [Serializable] public class Data
        {
            // ******************************************************************************
            // 1-1-1) 내부 타입 -> 데이터 -> 카메라
            //    - 카메라 민감도
            //    - 카메라 반전(상-하, 좌-우)
            // ******************************************************************************
            [Serializable] public class Camera
            {
                public enum InversionType { Horizontal, Vertical }

                [Range(0f, 100f)] public int                             sensitivity;
                                  public SimpleData<InversionType, bool> inversions;

                public Camera(int sensitivity, SimpleData<InversionType, bool> inversions)
                {
                    this.sensitivity = sensitivity;
                    this.inversions  = new SimpleData<InversionType, bool>(inversions);
                }

                public Camera(Camera other)
                {
                    sensitivity = other.sensitivity;
                    inversions  = new SimpleData<InversionType, bool>(other.inversions);
                }
            }

            // 시스템 단축키 타입
            public enum SystemType { Pause, Start, Submit, Cancel }

            // ******************************************************************************
            // 1-1-2) 내부 타입 -> 데이터 -> 맵핑(Mapping)
            //    - 플랫폼(PC, Android)과 입력(Keyboard, Joystick)에 대응하는 입력값들을 저장(키-값 쌍)
            //    - SimpleData<PlatformType, SimpleData<InputType, Key>> 형태(SimpleData = Dictionary)
            // ******************************************************************************
            [Serializable] public class Mappings : SimpleData<RuntimePlatform, Mapping>
            {
                [Serializable] public class Mapping : SimpleData<State, KeyCode>
                {
                    public Mapping(List<Element> elements) : base(elements) { }

                    public Mapping(Mapping other) : base(other) { }

                    public KeyCode GetKey(State type) => ContainsKey(type) ? this[type] : KeyCode.None;
                }

                public Mappings(List<Element> elements)
                {
                    this.elements = new List<Element>();

                    foreach (var element in elements)
                    {
                        RuntimePlatform type    = element.key;
                        var             mapping = new Mapping(element.value);

                        this.elements.Add(new Element(type, mapping));
                    }
                }

                public Mappings(Mappings other) : base(other)
                {
                    elements = new List<Element>();

                    foreach (var element in other.elements)
                    {
                        RuntimePlatform type    = element.key;
                        var             mapping = new Mapping(element.value);

                        elements.Add(new Element(type, mapping));
                    }
                }

                public KeyCode GetKey(RuntimePlatform type, State subType)
                    => ContainsKey(type) ? this[type].GetKey(subType) : KeyCode.None;
            }

            // 필드
            public Camera                                camera;
            public SimpleData<PlayerMainState, Mappings> character;
            public SimpleData<SystemType,      Mappings> system;

            // 메서드
            // Constructor
            public Data(Camera camera, SimpleData<PlayerMainState, Mappings> character,
                SimpleData<SystemType, Mappings> system)
            {
                var _character = new List<SimpleData<PlayerMainState, Mappings>.Element>();
                var _system    = new List<SimpleData<SystemType,      Mappings>.Element>();

                foreach (var element in character)
                {
                    PlayerMainState type     = element.key;
                    var             mappings = new Mappings(element.value);

                    _character.Add(new SimpleData<PlayerMainState, Mappings>.Element(type, mappings));
                }
                foreach (var element in system)
                {
                    SystemType type     = element.key;
                    var        mappings = new Mappings(element.value);

                    _system.Add(new SimpleData<SystemType, Mappings>.Element(type, mappings));
                }

                this.camera    = new Camera(camera);
                this.character = new SimpleData<PlayerMainState, Mappings>(_character);
                this.system    = new SimpleData<SystemType,      Mappings>(_system);
            }

            public Data(Data other)
            {
                var _character = new List<SimpleData<PlayerMainState, Mappings>.Element>();
                var _system    = new List<SimpleData<SystemType,      Mappings>.Element>();

                foreach (var element in other.character)
                {
                    PlayerMainState type     = element.key;
                    var             mappings = new Mappings(element.value);

                    _character.Add(new SimpleData<PlayerMainState, Mappings>.Element(type, mappings));
                }
                foreach (var element in other.system)
                {
                    SystemType type     = element.key;
                    var        mappings = new Mappings(element.value);

                    _system.Add(new SimpleData<SystemType, Mappings>.Element(type, mappings));
                }

                camera    = new Camera(other.camera);
                character = new SimpleData<PlayerMainState, Mappings>(_character);
                system    = new SimpleData<SystemType,      Mappings>(_system);
            }

            // Get
            public KeyCode GetKey(PlayerMainState type, RuntimePlatform secondType, State thirdType)
                => character.ContainsKey(type) ? character[type].GetKey(secondType, thirdType) : KeyCode.None;

            public KeyCode GetKey(SystemType type, RuntimePlatform secondType, State thirdType)
                => system.ContainsKey(type) ? system[type].GetKey(secondType, thirdType) : KeyCode.None;
        }

        public class ConnectedUI : List<IUIBase>
        {
            public IUIBase current { get; set; }
        }

        // ==============================================================================
        // 2) 필드
        // ==============================================================================
        // Component & Reference
        public ConnectedUI                     connectedUI          { get; protected set; }
        public IControlSettingListUIController controlSettingListUI { get; protected set; }

        // Data
        public Data data { get; protected set; }

        // Setting
        [SerializeField] protected Data _defaultData;

        public Data defaultData { get { return _defaultData; } }

        // State
        public State state { get; protected set; }

        // ==============================================================================
        // 3) 메서드
        //    - 부모 클래스의 함수들을 재정의하여 확장
        // ==============================================================================
        // ------------------------------------------------------------------------------
        // 3-1) 메서드 -> 이벤트 함수
        //    - 오브젝트 초기화
        //    - 조이스틱 감지
        // ------------------------------------------------------------------------------
        protected virtual void Reset() { ResetField(); }

        protected override void Start()
        { 
            base.Start();

            switch (Application.platform)
            {
                case RuntimePlatform.Android: SetState(State.Touch);    break;
                default:                      SetState(State.Keyboard); break;
            }

            StartCoroutine(DetectJoystick());
        }

        // ------------------------------------------------------------------------------
        // 3-2) 메서드 -> 초기화
        //    - 필드(컴포넌트 등) 초기화
        // ------------------------------------------------------------------------------
        protected override void SetField()
        {
            base.SetField();

            connectedUI          = new ConnectedUI();
            controlSettingListUI = transform.parent.GetComponentInChildren<IControlSettingListUIController>(true);

            type = Type.Control;
        }

        protected virtual void ResetField()
        {
            _defaultData = new Data(
                new Camera(
                    50,
                    new SimpleData<InversionType, bool>(
                        new List<SimpleData<InversionType, bool>.Element>()
                        {
                            new SimpleData<InversionType, bool>.Element(InversionType.Horizontal, false),
                            new SimpleData<InversionType, bool>.Element(InversionType.Vertical,   false)
                        })),
                new SimpleData<PlayerMainState, Mappings>(
                    new List<SimpleData<PlayerMainState, Mappings>.Element>()
                    {
                        new SimpleData<PlayerMainState, Mappings>.Element(
                            PlayerMainState.Jump,
                            new Mappings(
                                new List<Mappings.Element>()
                                {
                                    new Mappings.Element(
                                        RuntimePlatform.WindowsPlayer,
                                        new Mapping(
                                            new List<Mapping.Element>()
                                            {
                                                new Mapping.Element(State.Keyboard, KeyCode.Space),
                                                new Mapping.Element(State.Joystick, KeyCode.JoystickButton0)
                                            })),
                                    new Mappings.Element(
                                        RuntimePlatform.WindowsEditor,
                                        new Mapping(
                                            new List<Mapping.Element>()
                                            {
                                                new Mapping.Element(State.Keyboard, KeyCode.Space),
                                                new Mapping.Element(State.Joystick, KeyCode.JoystickButton0)
                                            })),
                                    new Mappings.Element(
                                        RuntimePlatform.Android,
                                        new Mapping(
                                            new List<Mapping.Element>()
                                            {
                                                new Mapping.Element(State.Joystick, KeyCode.JoystickButton0)
                                            }))
                                })),
                        new SimpleData<PlayerMainState, Mappings>.Element(
                            PlayerMainState.Crouch,
                            new Mappings(
                                new List<Mappings.Element>()
                                {
                                    new Mappings.Element(
                                        RuntimePlatform.WindowsPlayer,
                                        new Mapping(
                                            new List<Mapping.Element>()
                                            {
                                                new Mapping.Element(State.Keyboard, KeyCode.C),
                                                new Mapping.Element(State.Joystick, KeyCode.JoystickButton4)
                                            })),
                                    new Mappings.Element(
                                        RuntimePlatform.WindowsEditor,
                                        new Mapping(
                                            new List<Mapping.Element>()
                                            {
                                                new Mapping.Element(State.Keyboard, KeyCode.C),
                                                new Mapping.Element(State.Joystick, KeyCode.JoystickButton4)
                                            })),
                                    new Mappings.Element(
                                        RuntimePlatform.Android,
                                        new Mapping(
                                            new List<Mapping.Element>()
                                            {
                                                new Mapping.Element(State.Joystick, KeyCode.JoystickButton4)
                                            }))
                                })),
                        new SimpleData<PlayerMainState, Mappings>.Element(
                            PlayerMainState.Attack,
                            new Mappings(
                                new List<Mappings.Element>()
                                {
                                    new Mappings.Element(
                                        RuntimePlatform.WindowsPlayer,
                                        new Mapping(
                                            new List<Mapping.Element>()
                                            {
                                                new Mapping.Element(State.Keyboard, KeyCode.Mouse0),
                                                new Mapping.Element(State.Joystick, KeyCode.JoystickButton2)
                                            })),
                                    new Mappings.Element(
                                        RuntimePlatform.WindowsEditor,
                                        new Mapping(
                                            new List<Mapping.Element>()
                                            {
                                                new Mapping.Element(State.Keyboard, KeyCode.Mouse0),
                                                new Mapping.Element(State.Joystick, KeyCode.JoystickButton2)
                                            })),
                                    new Mappings.Element(
                                        RuntimePlatform.Android,
                                        new Mapping(
                                            new List<Mapping.Element>()
                                            {
                                                new Mapping.Element(State.Joystick, KeyCode.JoystickButton2)
                                            }))
                                })),
                        new SimpleData<PlayerMainState, Mappings>.Element(
                            PlayerMainState.Interact,
                            new Mappings(
                                new List<Mappings.Element>()
                                {
                                    new Mappings.Element(
                                        RuntimePlatform.WindowsPlayer,
                                        new Mapping(
                                            new List<Mapping.Element>()
                                            {
                                                new Mapping.Element(State.Keyboard, KeyCode.F),
                                                new Mapping.Element(State.Joystick, KeyCode.JoystickButton3)
                                            })),
                                    new Mappings.Element(
                                        RuntimePlatform.WindowsEditor,
                                        new Mapping(
                                            new List<Mapping.Element>()
                                            {
                                                new Mapping.Element(State.Keyboard, KeyCode.F),
                                                new Mapping.Element(State.Joystick, KeyCode.JoystickButton3)
                                            })),
                                    new Mappings.Element(
                                        RuntimePlatform.Android,
                                        new Mapping(
                                            new List<Mapping.Element>()
                                            {
                                                new Mapping.Element(State.Joystick, KeyCode.JoystickButton3)
                                            }))
                                }))
                    }),
                new SimpleData<SystemType, Mappings>(
                    new List<SimpleData<SystemType, Mappings>.Element>()
                    {
                        new SimpleData<SystemType, Mappings>.Element(
                            SystemType.Pause,
                            new Mappings(
                                new List<Mappings.Element>()
                                {
                                    new Mappings.Element(
                                        RuntimePlatform.WindowsPlayer,
                                        new Mapping(
                                            new List<Mapping.Element>()
                                            {
                                                new Mapping.Element(State.Keyboard, KeyCode.Escape),
                                                new Mapping.Element(State.Joystick, KeyCode.JoystickButton7)
                                            })),
                                    new Mappings.Element(
                                        RuntimePlatform.WindowsEditor,
                                        new Mapping(
                                            new List<Mapping.Element>()
                                            {
                                                new Mapping.Element(State.Keyboard, KeyCode.Escape),
                                                new Mapping.Element(State.Joystick, KeyCode.JoystickButton7)
                                            })),
                                    new Mappings.Element(
                                        RuntimePlatform.Android,
                                        new Mapping(
                                            new List<Mapping.Element>()
                                            {
                                                new Mapping.Element(State.Joystick, KeyCode.JoystickButton10)
                                            }))
                                })),
                        new SimpleData<SystemType, Mappings>.Element(
                            SystemType.Start,
                            new Mappings(
                                new List<Mappings.Element>()
                                {
                                    new Mappings.Element(
                                        RuntimePlatform.WindowsPlayer,
                                        new Mapping(
                                            new List<Mapping.Element>()
                                            {
                                                new Mapping.Element(State.Joystick, KeyCode.JoystickButton6)
                                            })),
                                    new Mappings.Element(
                                        RuntimePlatform.WindowsEditor,
                                        new Mapping(
                                            new List<Mapping.Element>()
                                            {
                                                new Mapping.Element(State.Joystick, KeyCode.JoystickButton6)
                                            })),
                                    new Mappings.Element(
                                        RuntimePlatform.Android,
                                        new Mapping(
                                            new List<Mapping.Element>()
                                            {
                                                new Mapping.Element(State.Joystick, KeyCode.JoystickButton11)
                                            }))
                                })),
                        new SimpleData<SystemType, Mappings>.Element(
                            SystemType.Submit,
                            new Mappings(
                                new List<Mappings.Element>()
                                {
                                    new Mappings.Element(
                                        RuntimePlatform.WindowsPlayer,
                                        new Mapping(
                                            new List<Mapping.Element>()
                                            {
                                                new Mapping.Element(State.Keyboard, KeyCode.Space),
                                                new Mapping.Element(State.Joystick, KeyCode.JoystickButton0)
                                            })),
                                    new Mappings.Element(
                                        RuntimePlatform.WindowsEditor,
                                        new Mapping(
                                            new List<Mapping.Element>()
                                            {
                                                new Mapping.Element(State.Keyboard, KeyCode.Space),
                                                new Mapping.Element(State.Joystick, KeyCode.JoystickButton0)
                                            })),
                                    new Mappings.Element(
                                        RuntimePlatform.Android,
                                        new Mapping(
                                            new List<Mapping.Element>()
                                            {
                                                new Mapping.Element(State.Joystick, KeyCode.JoystickButton0)
                                            }))
                                })),
                        new SimpleData<SystemType, Mappings>.Element(
                            SystemType.Cancel,
                            new Mappings(
                                new List<Mappings.Element>()
                                {
                                    new Mappings.Element(
                                        RuntimePlatform.WindowsPlayer,
                                        new Mapping(
                                            new List<Mapping.Element>()
                                            {
                                                new Mapping.Element(State.Keyboard, KeyCode.Escape),
                                                new Mapping.Element(State.Joystick, KeyCode.JoystickButton1)
                                            })),
                                    new Mappings.Element(
                                        RuntimePlatform.WindowsEditor,
                                        new Mapping(
                                            new List<Mapping.Element>()
                                            {
                                                new Mapping.Element(State.Keyboard, KeyCode.Escape),
                                                new Mapping.Element(State.Joystick, KeyCode.JoystickButton1)
                                            })),
                                    new Mappings.Element(
                                        RuntimePlatform.Android,
                                        new Mapping(
                                            new List<Mapping.Element>()
                                            {
                                                new Mapping.Element(State.Joystick, KeyCode.JoystickButton1)
                                            }))
                                }))
                    }));
        }

        // ------------------------------------------------------------------------------
        // 3-3) 메서드 -> 조이스틱 감지
        //    - 매 순간 입력의 타입(Keyboard or Joystick)을 검색
        //    - 입력 타입이 현재 입력 상태와 다른 경우, 입력 상태를 변경(연결된 모든 UI의 내용 변경 등)
        //    - 게임상의 모든 조작은 맵핑(Mapping)에서 현재 입력 상태에 대응하는 키를 검색한 후 사용
        // ------------------------------------------------------------------------------
        protected virtual IEnumerator DetectJoystick()
        {
            while (!Input.anyKeyDown) yield return null;

            IConfirmUIController confirmUI = menu.game.ui.confirm;

            // 설정 창(UI)에서 키 설정 중에 타 입력 방식이 감지될 경우, UI 등의 혼선 방지를 위해 스킵
            if (confirmUI.state == ConfirmUIController.State.WaitingForInput) goto Skip;

            State type = state;

            switch (Application.platform)
            {
                case RuntimePlatform.Android:
                    {
                        if (Input.touchCount > 0) type = State.Touch;
                        else
                        {
                            foreach (KeyCode key in Enum.GetValues(typeof(KeyCode)))
                            {
                                if (Input.GetKeyDown(key) && key.ToString().Contains("Joystick"))
                                {
                                    type = State.Joystick;

                                    break;
                                }
                            }
                        }
                    }
                    break;

                default:
                    {
                        foreach (KeyCode key in Enum.GetValues(typeof(KeyCode)))
                        {
                            if (Input.GetKeyDown(key))
                            {
                                type = key.ToString().Contains("Joystick") ? State.Joystick : State.Keyboard;

                                break;
                            }
                        }
                    }
                    break;
            }

            if (state != type) SetState(type);

            Skip:

            yield return null;

            StartCoroutine(DetectJoystick());
        }

        // 현재 입력 상태와 연결된 모든 UI의 내용을 변경
        protected virtual void SetState(State type)
        {
            IGameDirector game = menu.game;

            state = type;

            game.SetCursorVisible(CursorVisibleEventType.ControllerChange, type != State.Joystick);

            foreach (var ui in connectedUI) ui.Set();

            switch (state)
            {
                case State.Joystick: if (connectedUI.current != null) connectedUI.current.SelectFirstSelectable(); break;
                default:             EventSystem.current.SetSelectedGameObject(null);                              break;
            }
        }

        // ------------------------------------------------------------------------------
        // 3-4) 메서드 -> 데이터
        //    - 데이터를 키-값 쌍의 형태로 저장 및 불러오기
        //    - 맵핑 변경 등의 데이터 변경(또는 리셋) 시 연결된 UI의 내용 등을 변경
        // ------------------------------------------------------------------------------
        public override void Set(bool reset = false)
        {
            if (reset) data = new Data(defaultData);

            foreach (var ui in connectedUI) ui.Set();

            base.Set(reset);
        }

        // ******************************************************************************
        // 3-4-1) 메서드 -> 데이터 -> 불러오기(Load)
        // ******************************************************************************
        public override void Load()
        {
            var camera    = _Load(defaultData.camera);
            var character = _Load(defaultData.character);

            data = new Data(camera, character, defaultData.system);
        }

        protected virtual Camera _Load(Camera defaultData)
        {
            string key         = $"{type}_Camera_Sensitivity";
            int    sensitivity = PlayerPrefs.GetInt(key, defaultData.sensitivity);
            var    inversions  = __Load(defaultData.inversions);

            return new Camera(sensitivity, inversions);
        }

        protected virtual SimpleData<InversionType, bool> __Load(SimpleData<InversionType, bool> defaultData)
        {
            var inversions = new List<SimpleData<InversionType, bool>.Element>();

            foreach (var element in defaultData)
            {
                InversionType type      = element.key;
                string        key       = $"{this.type}_Camera_Inversion_{type}";
                bool          inversion = bool.TryParse(PlayerPrefs.GetString(key), out bool value) ? value : element.value;

                inversions.Add(new SimpleData<InversionType, bool>.Element(type, inversion));
            }

            return new SimpleData<InversionType, bool>(inversions);
        }

        protected virtual SimpleData<PlayerMainState, Mappings> _Load(SimpleData<PlayerMainState, Mappings> defaultData)
        {
            var character = new List<SimpleData<PlayerMainState, Mappings>.Element>();

            foreach (var element in defaultData)
            {
                PlayerMainState type     = element.key;
                var             mappings = __Load(type, element.value);

                character.Add(new SimpleData<PlayerMainState, Mappings>.Element(type, mappings));
            }

            return new SimpleData<PlayerMainState, Mappings>(character);
        }

        protected virtual Mappings __Load(PlayerMainState mappingsType, Mappings defaultData)
        {
            var mappings = new List<Mappings.Element>();

            foreach (var element in defaultData)
            {
                RuntimePlatform type    = element.key;
                var             mapping = ___Load(mappingsType, type, element.value);

                mappings.Add(new Mappings.Element(type, mapping));
            }

            return new Mappings(mappings);
        }

        protected virtual Mapping ___Load(PlayerMainState mappingsType, RuntimePlatform mappingType, Mapping defaultData)
        {
            var mapping = new List<Mapping.Element>();

            foreach (var element in defaultData)
            {
                State   type  = element.key;
                string  key   = $"{this.type}_Character_{mappingsType}_{mappingType}_{type}";
                KeyCode value = Enum.TryParse(PlayerPrefs.GetString(key), out KeyCode result) ? result : element.value;

                mapping.Add(new Mapping.Element(type, value));
            }

            return new Mapping(mapping);
        }

        // ******************************************************************************
        // 3-4-2) 메서드 -> 데이터 -> 저장(Save)
        // ******************************************************************************
        public override void Save()
        {
            _Save(data.camera);
            _Save(data.character);
        }

        protected virtual void _Save(Camera data)
        {
            string key = $"{type}_Camera_Sensitivity";

            PlayerPrefs.SetInt(key, data.sensitivity);
            __Save(data.inversions);
        }

        protected virtual void __Save(SimpleData<InversionType, bool> data)
        {
            foreach (var element in data)
            {
                InversionType type = element.key;
                string        key  = $"{this.type}_Camera_Inversion_{type}";

                PlayerPrefs.SetString(key, element.value.ToString());
            }
        }

        protected virtual void _Save(SimpleData<PlayerMainState, Mappings> data)
        {
            foreach (var element in data)
            {
                PlayerMainState type = element.key;

                __Save(type, element.value);
            }
        }

        protected virtual void __Save(PlayerMainState mappingsType, Mappings data)
        {
            foreach (var element in data)
            {
                RuntimePlatform type = element.key;

                ___Save(mappingsType, type, element.value);
            }
        }

        protected virtual void ___Save(PlayerMainState mappingsType, RuntimePlatform mappingType, Mapping data)
        {
            foreach (var element in data)
            {
                State  type = element.key;
                string key  = $"{this.type}_Character_{mappingsType}_{mappingType}_{type}";

                PlayerPrefs.SetString(key, element.value.ToString());
            }
        }
    }
}
