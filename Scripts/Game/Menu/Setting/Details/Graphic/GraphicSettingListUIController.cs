// //////////////////////////////////////////////////////////////////////////////
// * 요약
//    - 그래픽 설정(창)에 대한 UI 클래스
//
// * 목차
//    1. 인터페이스 ... Line 30
//    2. 클래스 ....... Line 40
//        1) 내부 타입 ... Line 45
//        2) 필드 ........ Line 105
//        3) 메서드 ...... Line 111
//            1- 초기화 .... Line 114
//            2- 셋(Set) ... Line 135
//            3- 이벤트 .... Line 148
// //////////////////////////////////////////////////////////////////////////////
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Game
{
    using Root        = GraphicSettingListUIController.Root;
    using SettingType = SettingBase.Type;
    using Data        = GraphicSettingManager.Data;
    using GraphicType = GraphicSettingManager.Data.Type;
    using PresetsData = GraphicSettingManager.Presets;
    using PresetType  = GraphicSettingManager.Presets.Type;

    // //////////////////////////////////////////////////////////////////////////////
    // 1. 인터페이스(ISettingListUIBase 인터페이스 상속)
    // //////////////////////////////////////////////////////////////////////////////
    public interface IGraphicSettingListUIController : ISettingListUIBase
    {
        // 프로퍼티
        // Component
        new Root root { get; }
    }

    // //////////////////////////////////////////////////////////////////////////////
    // 2. 클래스(SettingListUIBase 클래스 상속)
    // //////////////////////////////////////////////////////////////////////////////
    public class GraphicSettingListUIController : SettingListUIBase, IGraphicSettingListUIController
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
                    public Dropdown dropdown { get; }

                    public Option(Transform transform) : base(transform)
                    {
                        dropdown = content.GetComponentInChildren<Dropdown>(true);
                    }

                    public void SetContent(int value) { dropdown.value = value; }
                }

                public Option option { get; }

                public Slot(Transform transform) : base(transform)
                {
                    option = new Option(content.Find("Option"));
                }
            }

            public Dictionary<GraphicType, Slot> graphics;
            public Slot                          preset;

            public Root(Transform transform)
            {
                var content = transform.GetComponentInChildren<LayoutGroup>(true).transform;

                graphics = new Dictionary<GraphicType, Slot>();

                for (int i = 0; i < content.childCount; i++)
                {
                    var    slot = content.GetChild(i);
                    string name = slot.name.Replace(" ", string.Empty).Replace("Slot", string.Empty);

                    if      (Enum.TryParse(name, out GraphicType type)) graphics.Add(type, new Slot(slot));
                    else if (name == "Preset")                          preset = new Slot(slot);
                }
            }
            
            public void SetContent(Data data, PresetsData presetsData)
            {
                foreach (var element in graphics)
                {
                    GraphicType type    = element.Key;
                    var         graphic = element.Value;

                    graphic.option.SetContent(data[type]);
                }

                preset.option.SetContent((int)presetsData.GetType(data.quality));
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
            type = SettingType.Graphic;

            foreach (var element in root.graphics)
            {
                GraphicType type    = element.Key;
                var         graphic = element.Value;

                graphic.option.dropdown.onValueChanged.AddListener(delegate { OnValueChangedGraphicDropdown(type); });
            }

            root.preset.option.dropdown.onValueChanged.AddListener(delegate { OnValueChangedPresetDropdown(); });
        }

        // ------------------------------------------------------------------------------
        // 3-2) 메서드 -> 셋(Set)
        //    - 설정이 변경되면 UI 갱신
        // ------------------------------------------------------------------------------
        public override void Set()
        {
            base.Set();

            IGraphicSettingManager graphicSetting = ui.menu.graphic;

            root.SetContent(graphicSetting.data, graphicSetting.presets);
        }

        // ------------------------------------------------------------------------------
        // 3-3) 메서드 -> 이벤트
        //    - 조작 시 데이터 값을 변경 후 변경된 내용으로 설정 및 저장하기 위해 Graphic Setting Manager 호출
        // ------------------------------------------------------------------------------
        protected virtual void OnValueChangedGraphicDropdown(GraphicType type) 
        {
            if (isDisplaying) return;

            IGraphicSettingManager graphicSetting = ui.menu.graphic;

            graphicSetting.data[type] = root.graphics[type].option.dropdown.value;

            graphicSetting.Set();
        }

        protected virtual void OnValueChangedPresetDropdown()
        {
            if (isDisplaying) return;

            IGraphicSettingManager graphicSetting = ui.menu.graphic;

            TryChangeQuality(graphicSetting.data, graphicSetting.presets);
        }

        protected virtual void TryChangeQuality(Data data, PresetsData presetsData)
        {
            var presetType = (PresetType)root.preset.option.dropdown.value;

            if (presetType == PresetType.Custom) return;

            foreach (var element in root.graphics)
            {
                GraphicType type    = element.Key;
                var         graphic = element.Value;
                int         value   = presetsData[presetType][type];

                if (value < 0) continue;

                data[type]                    = value;
                graphic.option.dropdown.value = data[type];
            }
        }
    }
}
