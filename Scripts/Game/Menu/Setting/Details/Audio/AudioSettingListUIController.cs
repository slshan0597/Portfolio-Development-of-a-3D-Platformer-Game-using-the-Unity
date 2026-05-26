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


    public interface IAudioSettingListUIController : ISettingListUIBase
    {
        #region Property

        // Component
        new Root root { get; }

        #endregion
    }


    public class AudioSettingListUIController : SettingListUIBase, IAudioSettingListUIController
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

                    public Text   text   { get; }
                    public Slider slider { get; }

                    #endregion


                    #region Constructor

                    public Option(Transform transform) : base(transform)
                    {
                        text   = content.Find("Text").GetComponentInChildren<Text>(true);
                        slider = content.GetComponentInChildren<Slider>(true);
                    }

                    #endregion


                    #region Method

                    public void SetContent(int value)
                    {
                        text.text    = value.ToString();
                        slider.value = value;
                    }

                    #endregion
                }

                #endregion


                #region Field

                public Option option { get; }

                #endregion


                #region Constructor

                public Slot(Transform transform) : base(transform) { option = new Option(content.Find("Option")); }

                #endregion
            }

            #endregion


            #region Field

            public Slot                        master  { get; }
            public Dictionary<AudioType, Slot> details { get; }

            #endregion


            #region Constructor

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

            #endregion


            #region Method

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
            type = SettingType.Audio;

            root.master.option.slider.onValueChanged.AddListener(delegate { OnValueChangedSlider(null); });

            foreach (var element in root.details)
            {
                AudioType type   = element.Key;
                var       volume = element.Value;

                volume.option.slider.onValueChanged.AddListener(delegate { OnValueChangedSlider(type); });
            }
        }

        #endregion


        #region Set

        public override void Set()
        {
            base.Set();

            IAudioSettingManager audioSetting = ui.menu.audio;

            root.SetContent(audioSetting.data);
        }

        #endregion


        #region Option

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

        //protected virtual void OnValueChangedToggle(AudioType? type)
        //{
        //    if (isDisplaying) return;

        //    IAudioSettingManager audioSetting = ui.menu.audio;

        //    var data       = audioSetting.data;
        //    var volumeData = (type != null) ? data.details[(AudioType)type] : data.full;
        //    var volume     = (type != null) ? root.details[(AudioType)type] : root.full;

        //    volumeData.mute = volume.option.toggle.isOn;

        //    //volume.option.SetContent(volumeData);
        //    audioSetting.Set();
        //}

        #endregion

        #endregion
    }
}
