// //////////////////////////////////////////////////////////////////////////////
// * 요약
//    - 컨트롤 설정(창)에 대한 UI 클래스
//    - UI 조작 시(이벤트 발생 시) Control Setting Manager 클래스를 호출하여 조작된 값으로 설정을 변경한 후 저장
//    - Control Setting Manager 클래스에서 다시 호출하여 현재 UI를 변경된 값으로 갱신
//
// * 목차
//    1. 인터페이스 ... Line 30
//    2. 클래스 ....... Line 50
//        1) 정의 ..... Line 56
//        2) 필드 ..... Line 81
//        3) 메서드 ... Line 100
//            1- 이벤트 함수 ..... Line 104
//            2- 초기화 .......... Line 127
//            3- 셋(Set) ......... Line 146
//            4- 표시(Display) ... Line 187
// //////////////////////////////////////////////////////////////////////////////
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Game
{
    using Root            = ControlSettingListUIController.Root;
    using SettingType     = SettingBase.Type;
    using State           = ControlSettingManager.State;
    using InversionType   = ControlSettingManager.Data.Camera.InversionType;
    using PlayerMainState = PlayerController.State.Main;

    // //////////////////////////////////////////////////////////////////////////////
    // 1. 인터페이스(ISettingListUIBase 인터페이스 상속)
    // //////////////////////////////////////////////////////////////////////////////
    public interface IControlSettingListUIController : ISettingListUIBase
    {
        // 프로퍼티
        // Component
        new Root root { get; }
    }

    // //////////////////////////////////////////////////////////////////////////////
    // 2. 클래스(SettingListUIBase 클래스 상속)
    // //////////////////////////////////////////////////////////////////////////////
    public class ControlSettingListUIController : SettingListUIBase, IControlSettingListUIController
    {
        // ==============================================================================
        // 1) 내부 추가 정의
        //    - UI의 구조에 대한 클래스
        // ==============================================================================
        public new class Root
        {
            // 내부 추가 정의
            // 카메라 민감도
            public class Sensitivity : SlotBase
            {
                // 조작
                public class Option : MenuBase
                {
                    public Text   text   { get; }
                    public Slider slider { get; }

                    public Option(Transform transform) : base(transform)
                    {
                        text   = content.Find("Text").GetComponentInChildren<Text>(true);
                        slider = content.GetComponentInChildren<Slider>(true);
                    }

                    public void SetContent(float sensitivity)
                    {
                        text.text    = sensitivity.ToString();
                        slider.value = sensitivity;
                    }
                }

                public Option option { get; }

                public Sensitivity(Transform transform) : base(transform)
                {
                    option = new Option(content.Find("Option"));
                }
            }

            // 카메라 반전
            public class Inversion : SlotBase
            {
                // 조작
                public class Option : MenuBase
                {
                    public Toggle toggle { get; }

                    public Option(Transform transform) : base(transform)
                    {
                        toggle = content.GetComponentInChildren<Toggle>(true);
                    }

                    public void SetContent(bool value) { toggle.isOn = value; }
                }

                public Option option { get; }

                public Inversion(Transform transform) : base(transform)
                {
                    option = new Option(content.Find("Option"));
                }
            }

            // 키 맵핑
            public class Mapping : SlotBase
            {
                // 조작
                public class Option : MenuBase
                {
                    public Button button { get; }
                    public Text   text   { get; }

                    public Option(Transform transform) : base(transform)
                    {
                        button = content.GetComponentInChildren<Button>(true);
                        text   = button.GetComponentInChildren<Text>(true);
                    }
                    
                    public void SetContent(bool interactable, KeyCode value)
                    {
                        button.interactable = interactable;
                        text.text           = (value != KeyCode.None) ? value.ToString() : string.Empty;
                    }
                }

                public Option option { get; }

                public Mapping(Transform transform) : base(transform)
                {
                    option = new Option(content.Find("Option"));
                }
            }

            // 필드
            public Sensitivity                            sensitivity { get; }
            public Dictionary<InversionType,   Inversion> inversions  { get; }
            public Dictionary<PlayerMainState, Mapping>   mappings    { get; }

            // 메서드 -> 생성자
            public Root(Transform transform)
            {
                var content = transform.GetComponentInChildren<LayoutGroup>(true).transform;

                inversions = new Dictionary<InversionType,   Inversion>();
                mappings   = new Dictionary<PlayerMainState, Mapping>();

                for (int i = 0; i < content.childCount; i++)
                {
                    var    slot = content.GetChild(i);
                    string name = slot.name.Replace(" ", string.Empty).Replace("Slot", string.Empty);

                    switch (name)
                    {
                        case "Sensitivity": sensitivity = new Sensitivity(slot); break;
                        default:
                            {
                                if      (Enum.TryParse(name, out InversionType   inversionType)) inversions.Add(inversionType, new Inversion(slot));
                                else if (Enum.TryParse(name, out PlayerMainState mappingType))   mappings.Add(mappingType,     new Mapping(slot));
                            }
                            break;
                    }
                }
            }

            // 메서드 -> 셋(Set)
            // 설정에서 변경된 값으로 UI 갱신
            public void SetContent(IControlSettingManager controlSetting)
            {
                var cameraData = controlSetting.data.camera;

                sensitivity.option.SetContent(cameraData.sensitivity);

                foreach (var element in inversions)
                {
                    InversionType type      = element.Key;
                    var           inversion = element.Value;

                    inversion.option.SetContent(cameraData.inversions[type]);
                }

                foreach (var element in mappings)
                {
                    PlayerMainState type         = element.Key;
                    var             key          = element.Value;
                    bool            interactable = controlSetting.state != State.Touch;

                    key.option.SetContent(interactable, controlSetting.GetKey(type));
                }
            }
        }

        // ==============================================================================
        // 2) 필드
        // ==============================================================================
        // Component & Reference
        public new Root root { get; protected set; }

        // ==============================================================================
        // 3) 메서드
        //    - 부모 클래스의 함수들을 재정의하여 확장
        // ==============================================================================
        // ------------------------------------------------------------------------------
        // 3-1) 메서드 -> 초기화
        //    - 필드(컴포넌트 등) 초기화
        //    - 조작 가능한 UI(Interactable)에 이벤트 연결
        // ------------------------------------------------------------------------------
        protected override void SetField()
        {
            base.SetField();

            root = new Root(transform);
            type = SettingType.Control;

            root.sensitivity.option.slider.onValueChanged.AddListener(delegate { OnValueChangedSensitivitySlider(); });

            foreach (var element in root.inversions)
            {
                InversionType type      = element.Key;
                var           inversion = element.Value;

                inversion.option.toggle.onValueChanged.AddListener(delegate { OnValueChangedInversionToggle(type); });
            }
            foreach (var slot in root.mappings)
            {
                PlayerMainState type    = slot.Key;
                var             mapping = slot.Value;

                mapping.option.button.onClick.AddListener(delegate { OnClickMappingButton(type); });
            }
        }

        // ------------------------------------------------------------------------------
        // 3-2) 메서드 -> 셋(Set)
        //    - 설정이 변경되면 UI 갱신
        // ------------------------------------------------------------------------------
        public override void Set()
        {
            base.Set();

            IControlSettingManager controlSetting = ui.menu.control;

            root.SetContent(controlSetting);
        }

        // ------------------------------------------------------------------------------
        // 3-3) 메서드 -> 이벤트
        //    - 조작 시 데이터 값을 변경 후 변경된 내용으로 설정 및 저장하기 위해 Control Setting Manager 호출
        // ------------------------------------------------------------------------------
        protected virtual void OnValueChangedSensitivitySlider()
        {
            if (isDisplaying) return;

            IControlSettingManager controlSetting = ui.menu.control;

            var sensitivity = root.sensitivity;
            var cameraData  = controlSetting.data.camera;

            cameraData.sensitivity = (int)sensitivity.option.slider.value;

            controlSetting.Set();
        }

        protected virtual void OnValueChangedInversionToggle(InversionType type)
        {
            if (isDisplaying) return;

            IControlSettingManager controlSetting = ui.menu.control;

            var inversion      = root.inversions[type];
            var inversionDatas = controlSetting.data.camera.inversions;

            inversionDatas[type] = inversion.option.toggle.isOn;

            controlSetting.Set();
        }

        protected virtual void OnClickMappingButton(PlayerMainState type)
        {
            IConfirmUIController confirmUI = ui.menu.game.ui.confirm;

            confirmUI.DisplayForControlSetting(type);
        }
    }
}
