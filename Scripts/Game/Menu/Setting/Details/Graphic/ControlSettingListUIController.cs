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


    public interface IControlSettingListUIController : ISettingListUIBase
    {
        #region Property

        // Component
        new Root root { get; }

        #endregion
    }


    public class ControlSettingListUIController : SettingListUIBase, IControlSettingListUIController
    {
        #region Definition

        public new class Root
        {
            #region Definition

            public class Sensitivity : SlotBase
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

                    public void SetContent(float sensitivity)
                    {
                        text.text    = sensitivity.ToString();
                        slider.value = sensitivity;
                    }

                    #endregion
                }

                #endregion


                #region Field

                public Option option { get; }

                #endregion


                #region Constructor

                public Sensitivity(Transform transform) : base(transform)
                {
                    option = new Option(content.Find("Option"));
                }

                #endregion
            }


            public class Inversion : SlotBase
            {
                #region Definition

                public class Option : MenuBase
                {
                    #region Field

                    public Toggle toggle { get; }

                    #endregion


                    #region Constructor

                    public Option(Transform transform) : base(transform)
                    {
                        toggle = content.GetComponentInChildren<Toggle>(true);
                    }

                    #endregion


                    #region Method

                    public void SetContent(bool value) { toggle.isOn = value; }

                    #endregion
                }

                #endregion


                #region Field

                public Option option { get; }

                #endregion


                #region Constructor

                public Inversion(Transform transform) : base(transform)
                {
                    option = new Option(content.Find("Option"));
                }

                #endregion
            }


            public class Mapping : SlotBase
            {
                #region Definition

                public class Option : MenuBase
                {
                    #region Field

                    public Button button { get; }
                    public Text   text   { get; }

                    #endregion


                    #region Constructor

                    public Option(Transform transform) : base(transform)
                    {
                        button = content.GetComponentInChildren<Button>(true);
                        text   = button.GetComponentInChildren<Text>(true);
                    }

                    #endregion


                    #region Method

                    public void SetContent(bool interactable, KeyCode value)
                    {
                        button.interactable = interactable;
                        text.text           = (value != KeyCode.None) ? value.ToString() : string.Empty;
                    }

                    #endregion
                }

                #endregion


                #region Field

                public Option option { get; }

                #endregion


                #region Constructor

                public Mapping(Transform transform) : base(transform)
                {
                    option = new Option(content.Find("Option"));
                }

                #endregion
            }

            #endregion


            #region Field

            public Sensitivity                            sensitivity { get; }
            public Dictionary<InversionType,   Inversion> inversions  { get; }
            public Dictionary<PlayerMainState, Mapping>   mappings    { get; }

            #endregion


            #region Constructor

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

            #endregion


            #region Method

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

        #endregion


        #region Set

        public override void Set()
        {
            base.Set();

            IControlSettingManager controlSetting = ui.menu.control;

            root.SetContent(controlSetting);
        }

        #endregion


        #region Option

        protected virtual void OnValueChangedSensitivitySlider()
        {
            if (isDisplaying) return;

            IControlSettingManager controlSetting = ui.menu.control;

            var sensitivity = root.sensitivity;
            var cameraData  = controlSetting.data.camera;

            cameraData.sensitivity = (int)sensitivity.option.slider.value;

            //sensitivity.option.SetContent(cameraData.sensitivity);
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
            //StartCoroutine(TryChangeKey(type));

            IConfirmUIController confirmUI = ui.menu.game.ui.confirm;

            confirmUI.DisplayForControlSetting(type);
        }

        //protected virtual IEnumerator TryChangeKey(PlayerMainState type)
        //{
        //    IControlSettingManager controlSetting = ui.menu.control;

        //    var mapping           = root.mappings[type];
        //    var mappingData       = controlSetting.data.mappings[controlSetting.state];
        //    var characterKeyDatas = mappingData.characterKeys;
        //    var menuKeyDatas      = mappingData.menuKeys;

        //    mapping.option.SetContent(false, KeyCode.None);

        //    yield return WaitInput(type, characterKeyDatas, menuKeyDatas);

        //    //key.option.SetContent(true, characterKeyDatas[type]);
        //    controlSetting.Set();
        //}

        //protected virtual IEnumerator WaitInput(CharacterKeyType type, SimpleData<CharacterKeyType, KeyCode> characterKeyDatas,
        //    SimpleData<MenuKeyType, KeyCode> menuKeyDatas)
        //{
        //    float delay = 0.1f;

        //    yield return new WaitForSecondsRealtime(delay);

        //    while (!Input.anyKeyDown) yield return null;

        //    foreach (KeyCode value in Enum.GetValues(typeof(KeyCode)))
        //    {
        //        if (!Input.GetKeyDown(value)) continue;

        //        yield return new WaitForSecondsRealtime(delay);

        //        KeyCode closeKey = menuKeyDatas[MenuKeyType.Menu];
        //        KeyCode resetKey = menuKeyDatas[MenuKeyType.Other];

        //        if ((value == closeKey) || (value == resetKey)) break;

        //        foreach (var element in characterKeyDatas)
        //        {
        //            CharacterKeyType _type  = element.key;
        //            KeyCode          _value = element.value;

        //            if (_type  == type)  continue;
        //            if (_value == value) goto Skip;
        //        }

        //        characterKeyDatas[type] = value;

        //        Skip:
        //        break;
        //    }
        //}

        #endregion

        #endregion
    }
}
