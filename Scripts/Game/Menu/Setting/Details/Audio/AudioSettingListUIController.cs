// //////////////////////////////////////////////////////////////////////////////
// * 요약
//    - 오디오 설정(창)에 대한 UI 클래스
//
// * 목차
//    1. 인터페이스 ... Line 28
//    2. 클래스 ....... Line 38
//        1) 내부 타입 ... Line 43
//        2) 필드 ........ Line 106
//        3) 메서드 ...... Line 112
//            1- 초기화 .... Line 115
//            2- 셋(Set) ... Line 136
//            3- 이벤트 .... Line 149
// //////////////////////////////////////////////////////////////////////////////
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Game
{
    using Root        = AudioSettingListUIController.Root;
    using SettingType = SettingBase.Type;
    using Data        = AudioSettingManager.Data;
    using AudioType   = AudioBase.Type;

    // //////////////////////////////////////////////////////////////////////////////
    // 1. 인터페이스(ISettingListUIBase 인터페이스 상속)
    // //////////////////////////////////////////////////////////////////////////////
    public interface IAudioSettingListUIController : ISettingListUIBase
    {
        // 프로퍼티
        // Component
        new Root root { get; }
    }

    // //////////////////////////////////////////////////////////////////////////////
    // 2. 클래스(SettingListUIBase 클래스 상속)
    // //////////////////////////////////////////////////////////////////////////////
    public class AudioSettingListUIController : SettingListUIBase, IAudioSettingListUIController
    {
        // ==============================================================================
        // 1) 내부 타입
        // ==============================================================================
        public new class Root
        {
            public class Slot : SlotBase
            {
                public class Option : MenuBase
                {
                    public Text   text   { get; }
                    public Slider slider { get; }

                    public Option(Transform transform) : base(transform)
                    {
                        text   = content.Find("Text").GetComponentInChildren<Text>(true);
                        slider = content.GetComponentInChildren<Slider>(true);
                    }

                    public void SetContent(int value)
                    {
                        text.text    = value.ToString();
                        slider.value = value;
                    }
                }

                public Option option { get; }

                public Slot(Transform transform) : base(transform) { option = new Option(content.Find("Option")); }
            }

            public Slot                        master  { get; }
            public Dictionary<AudioType, Slot> details { get; }

            public Root(Transform transform)
            {
                details = new Dictionary<AudioType, Slot>();

                var content = transform.GetComponentInChildren<LayoutGroup>(true).transform;

                for (int i = 0; i < content.childCount; i++)
                {
                    var    slot = content.GetChild(i);
                    string name = slot.name.Replace(" ", string.Empty).Replace("Slot", string.Empty);

                    if (name == "Master")                        master = new Slot(slot);
                    if (Enum.TryParse(name, out AudioType type)) details.Add(type, new Slot(slot));
                }
            }

            public void SetContent(Data data)
            {
                master.option.SetContent(data.master);

                foreach (var element in details)
                {
                    AudioType type   = element.Key;
                    var       volume = element.Value;

                    volume.option.SetContent(data.details[type]);
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
        // ==============================================================================
        // ------------------------------------------------------------------------------
        // 3-1) 메서드 -> 초기화
        // ------------------------------------------------------------------------------
        protected override void SetField()
        {
            base.SetField();

            root = new Root(transform);
            type = SettingType.Audio;

            root.master.option.slider.onValueChanged.AddListener(delegate { OnValueChangedSlider(null); });

            foreach (var element in root.details)
            {
                AudioType type   = element.Key;
                var       volume = element.Value;

                volume.option.slider.onValueChanged.AddListener(delegate { OnValueChangedSlider(type); });
            }
        }

        // ------------------------------------------------------------------------------
        // 3-2) 메서드 -> 셋(Set)
        //    - 설정이 변경되면 UI 갱신
        // ------------------------------------------------------------------------------
        public override void Set()
        {
            base.Set();

            IAudioSettingManager audioSetting = ui.menu.audio;

            root.SetContent(audioSetting.data);
        }

        // ------------------------------------------------------------------------------
        // 3-3) 메서드 -> 이벤트
        //    - 조작 시 데이터 값을 변경 후 변경된 내용으로 설정 및 저장하기 위해 Audio Setting Manager 호출
        // ------------------------------------------------------------------------------
        protected virtual void OnValueChangedSlider(AudioType? type)
        {
            if (isDisplaying) return;

            IAudioSettingManager audioSetting = ui.menu.audio;

            var data  = audioSetting.data;
            var slot  = (type != null) ? root.details[(AudioType)type] : root.master;
            int value = (int)slot.option.slider.value;

            if (type != null) data.details[(AudioType)type] = value;
            else              data.master                   = value;

            slot.option.SetContent(value);
            audioSetting.Set();
        }
    }
}
