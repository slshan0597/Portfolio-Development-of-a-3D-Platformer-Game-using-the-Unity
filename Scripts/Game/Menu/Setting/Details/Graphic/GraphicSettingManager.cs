// //////////////////////////////////////////////////////////////////////////////
// * 요약
//    - 게임의 창과 해상도 크기 조절
//    - 게임의 품질(프레임, 택스처 등) 설정
//    - 품질 프리셋 설정
//
// * 목차
//    1. 인터페이스 ... Line 44
//    2. 클래스 ....... Line 61
//        1) 내부 타입 ... Line 66
//            1- 데이터 ... Line 69
//                1_ 품질(Quality) ... Line 79
//            2- 프리셋(Preset) ... Line 209
//        2) 필드 ..... Line 229
//        3) 메서드 ... Line 244
//            1- 이벤트 함수 ... Line 247
//            2- 초기화 ........ Line 252
//            3- 셋(Set) ....... Line 285
//            4- 데이터 ........ Line 420
//                1_ 불러오기(Load) ... Line 424
//                2_ 저장하기(Save) ... Line 455
// //////////////////////////////////////////////////////////////////////////////
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Game
{
    using Data             = GraphicSettingManager.Data;
    using GraphicType      = GraphicSettingManager.Data.Type;
    using ScreenModeType   = GraphicSettingManager.Data.ScreenModeType;
    using ResolutionType   = GraphicSettingManager.Data.ResolutionType;
    using Quality          = GraphicSettingManager.Data.Quality;
    using FrameRateType    = GraphicSettingManager.Data.Quality.FrameRateType;
    using TextureType      = GraphicSettingManager.Data.Quality.TextureType;
    using ShadowType       = GraphicSettingManager.Data.Quality.ShadowType;
    using AntiAliasingType = GraphicSettingManager.Data.Quality.AntiAliasingType;
    using VSyncType        = GraphicSettingManager.Data.Quality.VSyncType;
    using Presets          = GraphicSettingManager.Presets;
    using PresetType       = GraphicSettingManager.Presets.Type;

    // //////////////////////////////////////////////////////////////////////////////
    // 1. 인터페이스(ISettingBase 인터페이스 상속)
    // //////////////////////////////////////////////////////////////////////////////
    public interface IGraphicSettingManager : ISettingBase
    {
        // 프로퍼티
        // Reference
        List<IUIBase> connectedUI { get; }

        // Data
        Data data { get; }

        // Setting
        Data    defaultData { get; }
        Presets presets     { get; }
    }

    // //////////////////////////////////////////////////////////////////////////////
    // 2. 클래스(SettingBase 클래스 상속)
    // //////////////////////////////////////////////////////////////////////////////
    public class GraphicSettingManager : SettingBase, IGraphicSettingManager
    {
        // ==============================================================================
        // 1) 내부 타입
        // ==============================================================================
        // ------------------------------------------------------------------------------
        // 1-1) 내부 타입 -> 데이터
        //    - 그래픽에 대한 설정값들을 저장
        // ------------------------------------------------------------------------------
        [Serializable] public class Data
        {
            public enum Type           { ScreenMode, Resolution, FrameRate, Texture, Shadow, AntiAliasing, VSync }
            public enum ScreenModeType { FullScreen, Window }
            public enum ResolutionType { _1080p, _900p, _720p, _768p, _480p }

            // ******************************************************************************
            // 1-1-1) 내부 타입 -> 데이터 -> 품질(Quality)
            // ******************************************************************************
            [Serializable] public class Quality
            {
                // 내부 타입
                public enum FrameRateType    { _60, _30 }
                public enum TextureType      { High, Medium, Low }
                public enum ShadowType       { High, Medium, Low }
                public enum AntiAliasingType { _4x, _2x, Disabled }
                public enum VSyncType        { On, Off }

                // 필드
                public FrameRateType    frameRate;
                public TextureType      texture;
                public ShadowType       shadow;
                public AntiAliasingType antiAliasing;
                public VSyncType        vSync;

                // 생성자
                public Quality(FrameRateType frameRate, TextureType texture, ShadowType shadow, 
                    AntiAliasingType antiAliasing, VSyncType vSync)
                {
                    this.frameRate    = frameRate;
                    this.texture      = texture;
                    this.shadow       = shadow;
                    this.antiAliasing = antiAliasing;
                    this.vSync        = vSync;
                }

                public Quality(Quality other)
                {
                    frameRate    = other.frameRate;
                    texture      = other.texture;
                    shadow       = other.shadow;
                    antiAliasing = other.antiAliasing;
                    vSync        = other.vSync;
                }

                // 메서드
                // Indexing
                public int this[Type type]
                {
                    get
                    {
                        switch (type)
                        {
                            case GraphicType.FrameRate:    return (int)frameRate;
                            case GraphicType.Texture:      return (int)texture;
                            case GraphicType.Shadow:       return (int)shadow;
                            case GraphicType.AntiAliasing: return (int)antiAliasing;
                            case GraphicType.VSync:        return (int)vSync;
                            default:                       return -1;
                        }
                    }
                    set
                    {
                        switch (type)
                        {
                            case GraphicType.FrameRate:    frameRate    = (FrameRateType)value;    break;
                            case GraphicType.Texture:      texture      = (TextureType)value;      break;
                            case GraphicType.Shadow:       shadow       = (ShadowType)value;       break;
                            case GraphicType.AntiAliasing: antiAliasing = (AntiAliasingType)value; break;
                            case GraphicType.VSync:        vSync        = (VSyncType)value;        break;
                        }
                    }
                }

                // Operator
                public static bool operator ==(Quality lhs, Quality rhs)
                {
                    return (lhs.frameRate    == rhs.frameRate)
                        && (lhs.texture      == rhs.texture)
                        && (lhs.shadow       == rhs.shadow)
                        && (lhs.antiAliasing == rhs.antiAliasing)
                        && (lhs.vSync        == rhs.vSync);
                }

                public static bool operator !=(Quality lhs, Quality rhs) { return !(lhs == rhs); }

                public override bool Equals(object obj) { return base.Equals(obj); }

                public override int GetHashCode() { return base.GetHashCode(); }
            }

            // 필드 - 데이터
            public ScreenModeType screenMode;
            public ResolutionType resolution;
            public Quality        quality;

            // 생성자 - 데이터
            public Data(ScreenModeType screenMode, ResolutionType resolution, Quality quality)
            {
                this.screenMode = screenMode;
                this.resolution = resolution;
                this.quality    = new Quality(quality);
            }

            public Data(Data other)
            {
                screenMode = other.screenMode;
                resolution = other.resolution;
                quality    = new Quality(other.quality);
            }

            // 메서드 - 데이터
            // Indexing
            public int this[Type type]
            {
                get
                {
                    switch (type)
                    {
                        case GraphicType.ScreenMode: return (int)screenMode;
                        case GraphicType.Resolution: return (int)resolution;
                        default:                     return quality[type];
                    }
                }
                set
                {
                    switch (type)
                    {
                        case GraphicType.ScreenMode: screenMode    = (ScreenModeType)value; break;
                        case GraphicType.Resolution: resolution    = (ResolutionType)value; break;
                        default:                     quality[type] = value;                 break;
                    }
                }
            }
        }

        // ------------------------------------------------------------------------------
        // 1-2) 내부 타입 -> 프리셋(Preset)
        //    - 그래픽 품질에 대한 기본 설정값
        // ------------------------------------------------------------------------------
        [Serializable] public class Presets : SimpleData<PresetType, Quality>
        {
            public enum Type { High, Medium, Low, Custom }

            public Presets(List<Element> elements) : base(elements) { }

            public Presets(Presets other) : base(other) { }

            public Type GetType(Quality quality)
            {
                var element = elements.Find(element => element.value == quality);

                return (element != null) ? element.key : PresetType.Custom;
            }
        }

        // ==============================================================================
        // 2) 필드
        // ==============================================================================
        // Component & Reference
        public List<IUIBase> connectedUI { get; protected set; }

        // Data
        public Data data { get; protected set; }

        [SerializeField] protected Data    _defaultData;
        [SerializeField] protected Presets _presets;

        public Data    defaultData { get { return _defaultData; } }
        public Presets presets     { get { return _presets; } }

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

            connectedUI = new List<IUIBase>();

            type = Type.Graphic;
        }

        protected virtual void ResetField()
        {
            _defaultData = new Data(
                ScreenModeType.FullScreen, ResolutionType._1080p, new Quality(
                    FrameRateType._60, TextureType.Medium, ShadowType.Medium, AntiAliasingType._2x, VSyncType.Off));

            _presets = new Presets(
                new List<SimpleData<PresetType, Quality>.Element>()
                {
                    new SimpleData<PresetType, Quality>.Element(
                        PresetType.High, new Quality(
                            FrameRateType._60, TextureType.High, ShadowType.High, AntiAliasingType._2x, VSyncType.Off)),
                    new SimpleData<PresetType, Quality>.Element(
                        PresetType.Medium, new Quality(
                            FrameRateType._60, TextureType.Medium, ShadowType.Medium, AntiAliasingType._2x, VSyncType.Off)),
                    new SimpleData<PresetType, Quality>.Element(
                        PresetType.Low, new Quality(
                            FrameRateType._30, TextureType.Low, ShadowType.Low, AntiAliasingType.Disabled, VSyncType.Off))
                });
        }

        // ------------------------------------------------------------------------------
        // 3-3) 메서드 -> 셋(Set)
        // ------------------------------------------------------------------------------
        public override void Set(bool reset = false)
        {
            if (reset) data = new Data(defaultData);

            _Set(data);

            foreach (var ui in connectedUI) ui.Set();

            base.Set(reset);
        }

        protected virtual void _Set(Data data)
        {
            ResolutionType resolutionType = data.resolution;
            string         resolutionStr  = Regex.Replace(resolutionType.ToString(), @"\D", "");
            int            height         = int.TryParse(resolutionStr, out int value) ? value : 1080;
            int            width          = 1920;
            bool           fullScreen     = true;

            switch (resolutionType)
            {
                case ResolutionType._1080p or ResolutionType._900p or ResolutionType._720p: width = height * 16 / 9; break;
                case ResolutionType._768p  or ResolutionType._480p:                         width = height * 4  / 3; break;
            }
            switch (data.screenMode)
            {
                case ScreenModeType.FullScreen: fullScreen = true;  break;
                case ScreenModeType.Window:     fullScreen = false; break;
            }

            Screen.SetResolution(width, height, fullScreen);
            __Set(data.quality);
        }

        protected virtual void __Set(Quality data)
        {
            ___Set(data.frameRate);
            ___Set(data.texture);
            ___Set(data.shadow);
            ___Set(data.antiAliasing);
            ___Set(data.vSync);
        }

        protected virtual void ___Set(FrameRateType data)
        {
            string str = Regex.Replace(data.ToString(), @"\D", "");

            Application.targetFrameRate = int.TryParse(str, out int value) ? value : 60;
        }

        protected virtual void ___Set(TextureType data)
        {
            switch (data)
            {
                case TextureType.High:
                    {
                        QualitySettings.masterTextureLimit   = 0;
                        QualitySettings.anisotropicFiltering = AnisotropicFiltering.ForceEnable;
                    }
                    break;

                case TextureType.Medium:
                    {
                        QualitySettings.masterTextureLimit   = 0;
                        QualitySettings.anisotropicFiltering = AnisotropicFiltering.Enable;
                    }
                    break;

                case TextureType.Low:
                    {
                        QualitySettings.masterTextureLimit   = 1;
                        QualitySettings.anisotropicFiltering = AnisotropicFiltering.Disable;
                    }
                    break;
            }
        }

        protected virtual void ___Set(ShadowType data)
        {
            switch (data)
            {
                case ShadowType.High:
                    {
                        QualitySettings.shadowmaskMode   = ShadowmaskMode.DistanceShadowmask;
                        QualitySettings.shadows          = ShadowQuality.All;
                        QualitySettings.shadowResolution = ShadowResolution.High;
                        QualitySettings.shadowDistance   = 150f;
                        QualitySettings.shadowCascades   = 4;
                    }
                    break;

                case ShadowType.Medium:
                    {
                        QualitySettings.shadowmaskMode   = ShadowmaskMode.DistanceShadowmask;
                        QualitySettings.shadows          = ShadowQuality.All;
                        QualitySettings.shadowResolution = ShadowResolution.Medium;
                        QualitySettings.shadowDistance   = 20f;
                        QualitySettings.shadowCascades   = 2;
                    }
                    break;

                case ShadowType.Low:
                    {
                        QualitySettings.shadowmaskMode   = ShadowmaskMode.Shadowmask;
                        QualitySettings.shadows          = ShadowQuality.HardOnly;
                        QualitySettings.shadowResolution = ShadowResolution.Low;
                        QualitySettings.shadowDistance   = 20f;
                        QualitySettings.shadowCascades   = 0;
                    }
                    break;
            }
        }

        protected virtual void ___Set(AntiAliasingType data)
        {
            switch (data)
            {
                case AntiAliasingType._4x:      QualitySettings.antiAliasing = 4; break;
                case AntiAliasingType._2x:      QualitySettings.antiAliasing = 2; break;
                case AntiAliasingType.Disabled: QualitySettings.antiAliasing = 0; break;
            }
        }

        protected virtual void ___Set(VSyncType data)
        {
            switch (data)
            {
                case VSyncType.On:  QualitySettings.vSyncCount = 1; break;
                case VSyncType.Off: QualitySettings.vSyncCount = 0; break;
            }
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
            ScreenModeType screenMode = __Load(GraphicType.ScreenMode, defaultData.screenMode);
            ResolutionType resolution = __Load(GraphicType.Resolution, defaultData.resolution);
            var            quality    = _Load(defaultData.quality);

            data = new Data(screenMode, resolution, quality);
        }

        protected virtual Quality _Load(Quality defaultData)
        {
            FrameRateType    frameRate    = __Load(GraphicType.FrameRate,    defaultData.frameRate);
            TextureType      texture      = __Load(GraphicType.Texture,      defaultData.texture);
            ShadowType       shadow       = __Load(GraphicType.Shadow,       defaultData.shadow);
            AntiAliasingType antiAliasing = __Load(GraphicType.AntiAliasing, defaultData.antiAliasing);
            VSyncType        vSync        = __Load(GraphicType.VSync,        defaultData.vSync);

            return new Quality(frameRate, texture, shadow, antiAliasing, vSync);
        }

        protected virtual TEnum __Load<TEnum>(GraphicType graphicType, TEnum defaultValue) 
            where TEnum : struct, Enum
        {
            string key = $"{type}_{graphicType}";

            return Enum.TryParse(PlayerPrefs.GetString(key), out TEnum value) ? value : defaultValue;
        }

        // ******************************************************************************
        // 3-4-2) 메서드 -> 데이터 -> 저장하기(Save)
        // ******************************************************************************
        public override void Save()
        {
            __Save(GraphicType.ScreenMode, data.screenMode);
            __Save(GraphicType.Resolution, data.resolution);
            _Save(data.quality);
        }

        protected virtual void _Save(Quality data)
        {
            __Save(GraphicType.FrameRate,    data.frameRate);
            __Save(GraphicType.Texture,      data.texture);
            __Save(GraphicType.Shadow,       data.shadow);
            __Save(GraphicType.AntiAliasing, data.antiAliasing);
            __Save(GraphicType.VSync,        data.vSync);
        }

        protected virtual void __Save<TEnum>(GraphicType graphicType, TEnum value) where TEnum : struct, Enum
        {
            string key = $"{type}_{graphicType}";

            PlayerPrefs.SetString(key, value.ToString());
        }
    }
}
