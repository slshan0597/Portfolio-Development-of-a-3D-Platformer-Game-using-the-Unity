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


    public interface IGraphicSettingListUIController : ISettingListUIBase
    {
        #region Property

        // Component
        new Root root { get; }

        #endregion
    }


    public class GraphicSettingListUIController : SettingListUIBase, IGraphicSettingListUIController
    {
        #region Definition

        public new class Root
        {
            #region Definition

            public class Slot : SlotBase
            {
                #region Definition

                public class Option : MenuBase
                {
                    #region Field

                    public Dropdown dropdown { get; }

                    #endregion


                    #region Constructor

                    public Option(Transform transform) : base(transform)
                    {
                        dropdown = content.GetComponentInChildren<Dropdown>(true);
                    }

                    #endregion


                    #region Method

                    public void SetContent(int value) { dropdown.value = value; }

                    #endregion
                }

                #endregion


                #region Field

                public Option option { get; }

                #endregion


                #region Constructor

                public Slot(Transform transform) : base(transform)
                {
                    option = new Option(content.Find("Option"));
                }

                #endregion
            }

            #endregion


            #region Field

            public Dictionary<GraphicType, Slot> graphics;
            public Slot                          preset;

            #endregion


            #region Constructor

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

            #endregion


            #region Method

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

            #endregion
        }

        #endregion


        #region Field

        public new Root root { get; protected set; }

        #endregion


        #region Method

        #region Initialization

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

        #endregion


        #region Set

        public override void Set()
        {
            base.Set();

            IGraphicSettingManager graphicSetting = ui.menu.graphic;

            root.SetContent(graphicSetting.data, graphicSetting.presets);
        }

        #endregion


        #region Option

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

        #endregion

        #endregion
    }
}
