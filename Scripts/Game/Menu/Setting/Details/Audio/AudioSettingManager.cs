// //////////////////////////////////////////////////////////////////////////////
// * 요약
//    - 게임의 오디오 크기 조절
//
// * 목차
//    1. 인터페이스 ... Line 28
//    2. 클래스 ....... Line 44
//        1) 내부 타입 ... Line 49
//        2) 필드 ..... Line 73
//        3) 메서드 ... Line 86
//            1- 이벤트 함수 ... Line 89
//            2- 초기화 ........ Line 94
//            3- 셋(Set) ....... Line 119
//            4- 데이터 ........ Line 131
//                1_ 불러오기(Load) ... Line 135
//                2_ 저장하기(Save) ... Line 168
// //////////////////////////////////////////////////////////////////////////////
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game
{
    using Data      = AudioSettingManager.Data;
    using AudioType = AudioBase.Type;

    // //////////////////////////////////////////////////////////////////////////////
    // 1. 인터페이스(ISettingBase 인터페이스 상속)
    // //////////////////////////////////////////////////////////////////////////////
    public interface IAudioSettingManager : ISettingBase
    {
        // 프로퍼티
        // Reference
        List<IAudioBase> connectedAudios { get; }

        // Data
        Data data { get; }

        // Setting
        Data defaultData { get; }
    }

    // //////////////////////////////////////////////////////////////////////////////
    // 2. 클래스(SettingBase 클래스 상속)
    // //////////////////////////////////////////////////////////////////////////////
    public class AudioSettingManager : SettingBase, IAudioSettingManager
    {
        // ==============================================================================
        // 1) 내부 타입
        //    - 오디오에 대한 데이터 타입
        // ==============================================================================
        [Serializable] public class Data
        {
            // 필드
            public int                        master;
            public SimpleData<AudioType, int> details;

            // 생성자
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
        }

        // ==============================================================================
        // 2) 필드
        // ==============================================================================
        // Component & Reference
        public List<IAudioBase> connectedAudios { get; protected set; }

        // Data
        public Data data { get; protected set; }

        [SerializeField] protected Data _defaultData;

        public Data defaultData { get { return _defaultData; } }

        // ==============================================================================
        // 3) 메서드
        // ==============================================================================
        // ------------------------------------------------------------------------------
        // 3-1) 메서드 -> 이벤트 함수
        // ------------------------------------------------------------------------------
        protected virtual void Reset() { ResetField(); }

        // ------------------------------------------------------------------------------
        // 3-2) 메서드 -> 초기화
        // ------------------------------------------------------------------------------
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

        // ------------------------------------------------------------------------------
        // 3-3) 메서드 -> 셋(Set)
        // ------------------------------------------------------------------------------
        public override void Set(bool reset = false)
        {
            if (reset) data = new Data(defaultData);

            foreach (var audio in connectedAudios) audio.Set(data);

            base.Set(reset);
        }
        
        // ------------------------------------------------------------------------------
        // 3-4) 메서드 -> 데이터
        //    - 데이터 저장 및 불러오기
        // ------------------------------------------------------------------------------
        // ******************************************************************************
        // 3-4-1) 메서드 -> 데이터 -> 불러오기(Load)
        // ******************************************************************************
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

        // ******************************************************************************
        // 3-4-2) 메서드 -> 데이터 -> 저장하기(Save)
        // ******************************************************************************
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
    }
}
