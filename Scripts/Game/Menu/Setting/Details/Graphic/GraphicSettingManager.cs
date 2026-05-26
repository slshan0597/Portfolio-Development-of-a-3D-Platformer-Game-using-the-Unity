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


    public interface IGraphicSettingManager : ISettingBase
    {
        #region Property

        // Reference
        List<IUIBase> connectedUI { get; }

        // Data
        Data data { get; }

        // Setting
        Data    defaultData { get; }
        Presets presets     { get; }

        #endregion
    }


    public class GraphicSettingManager : SettingBase, IGraphicSettingManager
    {
        #region Definition

        [Serializable] public class Data
        {
            #region Definition

            public enum Type           { ScreenMode, Resolution, FrameRate, Texture, Shadow, AntiAliasing, VSync }
            public enum ScreenModeType { FullScreen, Window }
            public enum ResolutionType { _1080p, _900p, _720p, _768p, _480p }


            [Serializable] public class Quality
            {
                #region Definition

                public enum FrameRateType    { _60, _30 }
                public enum TextureType      { High, Medium, Low }
                public enum ShadowType       { High, Medium, Low }
                public enum AntiAliasingType { _4x, _2x, Disabled }
                public enum VSyncType        { On, Off }

                #endregion


                #region Field

                public FrameRateType    frameRate;
                public TextureType      texture;
                public ShadowType       shadow;
                public AntiAliasingType antiAliasing;
                public VSyncType        vSync;

                #endregion


                #region Constructor

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

                #endregion


                #region indexing

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

                #endregion


                #region Operator

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

                #endregion
            }

            #endregion


            #region Field

            public ScreenModeType screenMode;
            public ResolutionType resolution;
            public Quality        quality;

            #endregion


            #region Constructor

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

            #endregion


            #region indexing

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

            #endregion
        }


        [Serializable] public class Presets : SimpleData<PresetType, Quality>
        {
            #region Definition

            public enum Type { High, Medium, Low, Custom }

            #endregion


            #region Constructor

            public Presets(List<Element> elements) : base(elements) { }

            public Presets(Presets other) : base(other) { }

            #endregion


            #region Method

            public Type GetType(Quality quality)
            {
                var element = elements.Find(element => element.value == quality);

                return (element != null) ? element.key : PresetType.Custom;
            }

            #endregion
        }

        #endregion


        #region Field

        public List<IUIBase> connectedUI { get; protected set; }

        public Data data { get; protected set; }

        [SerializeField] protected Data    _defaultData;
        [SerializeField] protected Presets _presets;

        public Data    defaultData { get { return _defaultData; } }
        public Presets presets     { get { return _presets; } }

        #endregion


        #region Method

        #region Event

        protected virtual void Reset() { ResetField(); }

        #endregion


        #region Initialization

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

        #endregion


        #region Data

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

        #endregion


        #region Set

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

        #endregion

        #endregion
    }
}
