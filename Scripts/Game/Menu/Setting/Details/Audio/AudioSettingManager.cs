using System;
using System.Collections.Generic;
using UnityEngine;


namespace Game
{
    using Data      = AudioSettingManager.Data;
    using AudioType = AudioBase.Type;


    public interface IAudioSettingManager : ISettingBase
    {
        #region Property

        // Reference
        List<IAudioBase> connectedAudios { get; }

        // Data
        Data data { get; }

        // Setting
        Data defaultData { get; }

        #endregion
    }


    public class AudioSettingManager : SettingBase, IAudioSettingManager
    {
        #region Definition

        [Serializable] public class Data
        {
            #region Field

            public int                        master;
            public SimpleData<AudioType, int> details;

            #endregion


            #region Constructor

            public Data(int master, SimpleData<AudioType, int> details)
            {
                this.master  = master;
                this.details = new SimpleData<AudioType, int>(details);
            }

            public Data(Data other)
            {
                master  = other.master;
                details = new SimpleData<AudioType, int>(other.details);
            }

            #endregion
        }

        #endregion


        #region Field

        public List<IAudioBase> connectedAudios { get; protected set; }

        public Data data { get; protected set; }

        [SerializeField] protected Data _defaultData;

        public Data defaultData { get { return _defaultData; } }

        #endregion


        #region Method

        #region Event

        protected virtual void Reset() { ResetField(); }

        #endregion


        #region Initialization

        protected override void SetField()
        {
            base.SetField();

            connectedAudios = new List<IAudioBase>();

            type = Type.Audio;
        }

        protected virtual void ResetField()
        {
            _defaultData = new Data(
                100, new SimpleData<AudioType, int>(
                    new List<SimpleData<AudioType, int>.Element>()
                    {
                        new SimpleData<AudioType, int>.Element(AudioType.BGM,    10),
                        new SimpleData<AudioType, int>.Element(AudioType.Voice,  10),
                        new SimpleData<AudioType, int>.Element(AudioType.Effect, 10),
                        new SimpleData<AudioType, int>.Element(AudioType.System, 10)
                    }));
        }

        #endregion


        #region Data

        public override void Load()
        {
            int master = __Load("Master", defaultData.master);
            var details = _Load(defaultData.details);

            data = new Data(master, details);
        }

        protected virtual SimpleData<AudioType, int> _Load(SimpleData<AudioType, int> defaultData)
        {
            var details = new List<SimpleData<AudioType, int>.Element>();

            foreach (var element in defaultData)
            {
                AudioType type   = element.key;
                int       volume = __Load(type.ToString(), element.value);

                details.Add(new SimpleData<AudioType, int>.Element(type, volume));
            }

            return new SimpleData<AudioType, int>(details);
        }

        protected virtual int __Load(string baseKey, int defaultValue)
        {
            string key = $"{type}_{baseKey}";

            return PlayerPrefs.GetInt(key, defaultValue);
        }

        public override void Save()
        {
            __Save("Master", data.master);
            _Save(data.details);
        }

        protected virtual void _Save(SimpleData<AudioType, int> data)
        {
            foreach (var element in data)
            {
                AudioType type = element.key;

                __Save(type.ToString(), element.value);
            }
        }

        protected virtual void __Save(string baseKey, int value)
        {
            string key = $"{type}_{baseKey}";

            PlayerPrefs.SetInt(key, value);
        }

        #endregion


        #region Set

        public override void Set(bool reset = false)
        {
            if (reset) data = new Data(defaultData);

            foreach (var audio in connectedAudios) audio.Set(data);

            base.Set(reset);
        }

        #endregion

        #endregion
    }
}
