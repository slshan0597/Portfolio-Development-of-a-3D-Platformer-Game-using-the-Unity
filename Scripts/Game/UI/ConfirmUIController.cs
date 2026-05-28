// //////////////////////////////////////////////////////////////////////////////
// * 요약
//    - 게임의 확인 창 UI 클래스
//    - 이벤트 발생 시 실행 전 확인용 창 오픈, 정보 및 확인/취소 옵션 표시
//
// * 목차
//    1. 인터페이스 ... Line 33
//    2. 클래스 ....... Line 53
//        1) 내부 타입 ... Line 58
//        1) 필드 ........ Line 102
//        2) 메서드 ...... Line 112
//            1- 초기화 .......... Line 115
//            2- 셋(Set) ......... Line 133
//            3- 표시(Display) ... Line 149
//                1_ 공통 ..... Line 154
//                2_ 컨트롤 ... Line 196
//            4- 이벤트 ... Line 343
// //////////////////////////////////////////////////////////////////////////////
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Game
{
    using State             = ConfirmUIController.State;
    using Root              = ConfirmUIController.Root;
    using ControlState      = ControlSettingManager.State;
    using SystemControlType = ControlSettingManager.Data.SystemType;
    using PlayerMainState   = PlayerController.State.Main;

    // //////////////////////////////////////////////////////////////////////////////
    // 1. 인터페이스(IUIBase 인터페이스 상속)
    // //////////////////////////////////////////////////////////////////////////////
    public interface IConfirmUIController : IUIBase
    {
        // 프로퍼티
        // Component
        Root root { get; }

        // Reference
        IGameDirector game { get; }

        // State
        State state { get; }

        // 메서드
        Coroutine Display(string label, string content, params IUIBase[] exposedUI);
        Coroutine DisplayForControlSetting(PlayerMainState type);
    }

    // //////////////////////////////////////////////////////////////////////////////
    // 2. 클래스(UIBase 클래스 상속)
    // //////////////////////////////////////////////////////////////////////////////
    public class ConfirmUIController : UIBase, IConfirmUIController
    {
        // ==============================================================================
        // 1) 내부 타입
        // ==============================================================================
        public enum State { None, Waiting, Confirm, Cancel, WaitingForInput }

        public class Root : WindowBase
        {
            public class Main : WindowBase
            {
                public class Option : MenuBase
                {
                    public SoundButton confirmButton { get; }
                    public KeyButton   cancelButton  { get; }

                    public Option(Transform transform) : base(transform)
                    {
                        confirmButton = content.Find("Confirm Button").GetComponent<SoundButton>();
                        cancelButton  = content.GetComponentInChildren<KeyButton>(true);
                    }
                }

                public Text   labelText   { get; }
                public Text   contentText { get; }
                public Option option      { get; }

                public Main(Transform transform) : base(transform)
                {
                    labelText   = content.Find("Label Text").GetComponent<Text>();
                    contentText = content.Find("Content Text").GetComponent<Text>();
                    option      = new Option(content.Find("Option"));
                }

                public void SetContent(string label, string content)
                {
                    labelText.text   = label;
                    contentText.text = content;
                }
            }

            public Main main { get; }

            public Root(Transform transform) : base(transform) { main = new Main(content.Find("Main")); }
        }

        // ==============================================================================
        // 2) 필드
        // ==============================================================================
        // Component & Reference
        public Root          root { get; protected set; }
        public IGameDirector game { get; protected set; }

        // State
        public State state { get; protected set; }

        // ==============================================================================
        // 3) 메서드
        // ==============================================================================
        // ------------------------------------------------------------------------------
        // 3-1) 메서드 -> 초기화
        // ------------------------------------------------------------------------------
        protected override void SetField()
        {
            base.SetField();

            root = new Root(transform);
            game = GetComponentInParent<IGameDirector>(true);

            var option = root.main.option;

            option.confirmButton.onClick.AddListener(delegate { OnClickOptionButton(State.Confirm); });
            option.cancelButton.onClick.AddListener(delegate  { OnClickOptionButton(State.Cancel); });
        }

        protected override void ResetField() { _defaultDuration = 0.25f; }

        // ------------------------------------------------------------------------------
        // 3-2) 메서드 -> 셋(Set)
        //    - 컨트롤 설정에 따른 UI 갱신
        // ------------------------------------------------------------------------------
        public override void Set()
        {
            base.Set();

            IControlSettingManager controlSetting = game.menu.setting.control;

            var     button = root.main.option.cancelButton;
            KeyCode key    = controlSetting.GetKey(SystemControlType.Cancel);

            button.SetContent(key);
        }

        // ------------------------------------------------------------------------------
        // 3-3) 메서드 -> 표시(Display)
        //    - 확인용 정보(Content)를 전달받아 현재 UI 앞에 창(윈도우) 형태로 출력
        //    - 다음 입력(확인/취소)까지 대기
        // ------------------------------------------------------------------------------
        // ******************************************************************************
        // 3-3-1) 메서드 -> 표시 -> 공통
        // ******************************************************************************
        public virtual Coroutine Display(string label, string content, params IUIBase[] exposedUI)
        {
            gameObject.SetActive(true);
            StopAllCoroutines();
            Set();
            SetInteractables(false);
            root.main.SetContent(label, content);

            state = State.Waiting;

            foreach (var ui in exposedUI) ui.SetInteractables(false);

            return StartCoroutine(_Display(defaultDuration, exposedUI));
        }

        protected virtual IEnumerator _Display(float duration, IUIBase[] exposedUI)
        {
            yield return FadeWindow(root.main, true, duration);

            SetInteractables(true);

            while (state == State.Waiting) yield return null;

            SetCurrent(false);

            foreach (var ui in exposedUI) ui.SetInteractables(true);

            StartCoroutine(Close(duration));
        }

        protected virtual IEnumerator Close(float duration)
        {
            yield return FadeWindow(root.main, false, duration);

            state = State.None;

            gameObject.SetActive(false);
        }

        // ******************************************************************************
        // 3-3-2) 메서드 -> 표시 -> 컨트롤
        //    - 컨트롤 맵핑 설정 시 전용으로 출력
        //    - 변경할 키를 입력받은 후 조건을 거쳐 변경 또는 취소
        //    - ex) 현재 입력 상태가 키보드일 때, 조이스틱 입력을 받을 경우 변경 취소
        // ******************************************************************************
        public virtual Coroutine DisplayForControlSetting(PlayerMainState type)
        {
            if (gameObject.activeInHierarchy) return null;

            ISettingMenuManager             setting        = GameDirector.instance.menu.setting;
            IControlSettingManager          controlSetting = setting.control;
            ISettingMenuUIController        settingUi      = setting.ui;
            IControlSettingListUIController settingListUI  = setting.ui.lists.control;

            gameObject.SetActive(true);
            StopAllCoroutines();
            Set();
            root.main.SetContent("Input Key", $"Cancel : {controlSetting.GetKey(SystemControlType.Pause)}");
            root.main.option.gameObject.SetActive(false);
            settingUi.SetInteractables(false);
            settingListUI.SetInteractables(false);

            state = State.WaitingForInput;

            return StartCoroutine(_DisplayForControlSetting(type));
        }

        protected virtual IEnumerator _DisplayForControlSetting(PlayerMainState type)
        {
            yield return FadeWindow(root.main, true, defaultDuration);

            StartCoroutine(WaitInput(type));
        }

        protected virtual IEnumerator WaitInput(PlayerMainState type)
        {
            IControlSettingManager controlSetting = GameDirector.instance.menu.setting.control;

            while (!Input.anyKeyDown) yield return null;

            if (Input.GetKeyDown(controlSetting.GetKey(SystemControlType.Pause)))
            {
                StartCoroutine(CloseForControlSetting());

                yield break;
            }

            switch (Application.platform)
            {
                case RuntimePlatform.Android:
                    {
                        if (Input.touchCount > 0)
                        {
                            StartCoroutine(CloseForControlSetting());

                            yield break;
                        }

                        foreach (KeyCode key in Enum.GetValues(typeof(KeyCode)))
                        {
                            if (!Input.GetKeyDown(key) || !key.ToString().Contains("Joystick")) continue;

                            controlSetting.data.character[type][Application.platform][controlSetting.state] = key;

                            StartCoroutine(CloseForControlSetting());

                            yield break;
                        }
                    }
                    break;

                default:
                    {
                        KeyCode defaultExitKey 
                            = controlSetting.data.system[SystemControlType.Pause][Application.platform][ControlState.Keyboard];

                        if (Input.GetKeyDown(defaultExitKey))
                        {
                            StartCoroutine(CloseForControlSetting());

                            yield break;
                        }

                        foreach (KeyCode key in Enum.GetValues(typeof(KeyCode)))
                        {
                            if (!Input.GetKeyDown(key)) continue;

                            bool isJoystickKey = key.ToString().Contains("Joystick");

                            switch (controlSetting.state)
                            {
                                case ControlState.Keyboard:
                                    {
                                        if (!isJoystickKey)
                                        {
                                            controlSetting.data.character[type][Application.platform][controlSetting.state] = key;

                                            StartCoroutine(CloseForControlSetting());

                                            yield break;
                                        }
                                    }
                                    break;

                                case ControlState.Joystick:
                                    {
                                        if (isJoystickKey)
                                        {
                                            controlSetting.data.character[type][Application.platform][controlSetting.state] = key;

                                            StartCoroutine(CloseForControlSetting());

                                            yield break;
                                        }
                                    }
                                    break;
                            }
                        }
                    }
                    break;
            }

            yield return null;

            StartCoroutine(WaitInput(type));
        }

        protected virtual IEnumerator CloseForControlSetting()
        {
            ISettingMenuManager             setting        = GameDirector.instance.menu.setting;
            IControlSettingManager          controlSetting = setting.control;
            ISettingMenuUIController        settingUi      = setting.ui;
            IControlSettingListUIController settingListUI  = setting.ui.lists.control;

            controlSetting.Set();
            settingUi.SetInteractables(true);
            settingListUI.SetInteractables(true);

            yield return FadeWindow(root.main, false, defaultDuration);

            state = State.None;

            root.main.option.gameObject.SetActive(true);
            gameObject.SetActive(false);
        }

        // ------------------------------------------------------------------------------
        // 3-4) 메서드 -> 이벤트
        //    - 입력값에 따른 상태 변경
        // ------------------------------------------------------------------------------
        protected virtual void OnClickOptionButton(State state) { this.state = state; }
    }
}
