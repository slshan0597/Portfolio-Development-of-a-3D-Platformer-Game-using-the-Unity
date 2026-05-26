// //////////////////////////////////////////////////////////////////////////////
// * 요약
//    - 게임의 세팅 메뉴의 UI 클래스
//    - 세팅 메뉴의 일반 기능(Open, Reset 등)
//    - 세부 사항(그래픽, 오디오, 컨트롤) 표시 및 전환
//
// * 목차
//    1. 인터페이스 ... Line 29
//    2. 클래스 ....... Line 46
// //////////////////////////////////////////////////////////////////////////////
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Game
{
    using Root              = SettingMenuUIController.Root;
    using GeneralOptionType = SettingMenuUIController.Root.Option.General.Type;
    using Lists             = SettingMenuUIController.Lists;
    using SettingType       = SettingBase.Type;
    using ConfirmUIState    = ConfirmUIController.State;
    using SystemControlType = ControlSettingManager.Data.SystemType;

    // //////////////////////////////////////////////////////////////////////////////
    // 1. 인터페이스(IUIBase 인터페이스 상속)
    // //////////////////////////////////////////////////////////////////////////////
    public interface ISettingMenuUIController : IUIBase
    {
        // 프로퍼티
        // Component
        Root  root  { get; }
        Lists lists { get; }    // 세부 사항

        // Reference
        ISettingMenuManager menu { get; }

        // State
        SettingType state { get; }
    }

    // //////////////////////////////////////////////////////////////////////////////
    // 2. 클래스(UIBase 클래스 상속)
    // //////////////////////////////////////////////////////////////////////////////
    public class SettingMenuUIController : UIBase, ISettingMenuUIController
    {
        // ==============================================================================
        // 1) 내부 타입
        // ==============================================================================
        // ------------------------------------------------------------------------------
        // 1-1) 내부 타입 -> 구조(Root)
        // ------------------------------------------------------------------------------
        public class Root : WindowBase
        {
            public class Main : MenuBase
            {
                public Dictionary<SettingType, ScrollRect> scrolls { get; }

                public Main(Transform transform) : base(transform)
                {
                    scrolls = new Dictionary<SettingType, ScrollRect>();

                    foreach (var scroll in content.GetComponentsInChildren<ScrollRect>(true))
                    {
                        string name = scroll.name;

                        if (Enum.TryParse(name, out SettingType type)) scrolls.Add(type, scroll);
                    }
                }
            }

            public class Option : WindowBase
            {
                public class Setting : MenuBase
                {
                    public Dictionary<SettingType, SoundButton> buttons { get; }

                    public Setting(Transform transform) : base(transform)
                    {
                        buttons = new Dictionary<SettingType, SoundButton>();

                        foreach (var button in content.GetComponentsInChildren<SoundButton>(true))
                        {
                            string name = button.name.Replace(" ", string.Empty).Replace("Button", string.Empty);

                            if (Enum.TryParse(name, out SettingType type)) buttons.Add(type, button);
                        }
                    }

                    public void SetContent(SettingType type)
                    {
                        foreach (var element in buttons)
                        {
                            SettingType _type  = element.Key;
                            var         button = element.Value;

                            button.interactable = _type != type;
                        }
                    }
                }

                public class General : MenuBase
                {
                    public enum Type { Reset, Close }

                    public Dictionary<Type, KeyButton> buttons { get; }

                    public General(Transform transform) : base(transform)
                    {
                        buttons = new Dictionary<Type, KeyButton>();

                        foreach (var button in content.GetComponentsInChildren<KeyButton>(true))
                        {
                            string name = button.name.Replace(" ", string.Empty).Replace("Button", string.Empty);

                            if (Enum.TryParse(name, out Type type)) buttons.Add(type, button);
                        }
                    }

                    public void SetContent(IControlSettingManager controlSetting)
                    {
                        foreach (var element in buttons)
                        {
                            Type _type  = element.Key;
                            var  button = element.Value;

                            switch (_type)
                            {
                                case GeneralOptionType.Reset: button.SetContent(controlSetting.GetKey(SystemControlType.Start));  break;
                                case GeneralOptionType.Close: button.SetContent(controlSetting.GetKey(SystemControlType.Cancel)); break;
                            }
                        }
                    }
                }

                public WindowBase title   { get; }
                public Setting    setting { get; }
                public General    general { get; }

                public Option(Transform transform) : base(transform)
                {
                    title   = new WindowBase(content.Find("Title"));
                    setting = new Setting(content.Find("Setting"));
                    general = new General(content.Find("General"));
                }

                public void SetContent(SettingType settingType, IControlSettingManager controlSetting)
                {
                    setting.SetContent(settingType);
                    general.SetContent(controlSetting);
                }
            }

            public Main   main   { get; }
            public Option option { get; }

            public Root(Transform transform) : base(transform)
            {
                main   = new Main(content.Find("Main"));
                option = new Option(content.Find("Option"));
            }
        }

        // ------------------------------------------------------------------------------
        // 1-2) 내부 타입 -> 세부 사항
        //    - 세팅 메뉴 내의 세부 사항에 대한 리스트
        // ------------------------------------------------------------------------------
        public class Lists : Dictionary<SettingType, ISettingListUIBase>
        {
            // 필드
            public IGraphicSettingListUIController graphic { get; }
            public IAudioSettingListUIController   audio   { get; }
            public IControlSettingListUIController control { get; }

            // 생성자
            public Lists(Transform transform) : base()
            {
                foreach (var list in transform.GetComponentsInChildren<ISettingListUIBase>(true))
                {
                    string name = list.gameObject.name.Replace(" ", string.Empty).Replace("List", string.Empty);

                    if (Enum.TryParse(name, out SettingType type)) Add(type, list);
                }

                graphic = transform.GetComponentInChildren<IGraphicSettingListUIController>(true);
                audio   = transform.GetComponentInChildren<IAudioSettingListUIController>(true);
                control = transform.GetComponentInChildren<IControlSettingListUIController>(true);
            }
        }

        // ==============================================================================
        // 2) 필드
        // ==============================================================================
        // Component & Reference
        public Root                root  { get; protected set; }
        public Lists               lists { get; protected set; }
        public ISettingMenuManager menu  { get; protected set; }

        // State
        public SettingType state { get; protected set; }

        // ==============================================================================
        // 3) 메서드
        // ==============================================================================
        // ------------------------------------------------------------------------------
        // 3-1) 메서드 -> 초기화
        // ------------------------------------------------------------------------------
        protected override void SetField()
        {
            base.SetField();

            root  = new Root(transform);
            lists = new Lists(transform);
            menu  = GetComponentInParent<ISettingMenuManager>(true);

            var option = root.option;

            foreach (var element in option.setting.buttons)
            {
                SettingType type   = element.Key;
                var         button = element.Value;

                button.onClick.AddListener(delegate { OnClickSettingButton(type); });
            }
            foreach (var element in option.general.buttons)
            {
                GeneralOptionType type   = element.Key;
                var               button = element.Value;

                button.onClick.AddListener(delegate { OnClickGeneralButton(type); });
            }
        }

        protected override void ResetField() { _defaultDuration = 0.5f; }

        // ------------------------------------------------------------------------------
        // 3-2) 메서드 -> 셋(Set)
        //    - 설정이 변경되면 UI 갱신
        // ------------------------------------------------------------------------------
        public override void Set()
        {
            base.Set();

            IControlSettingManager controlSetting = menu.control;

            root.option.SetContent(state, controlSetting);
        }

        public override void SelectFirstSelectable() { }

        protected override void SetCurrent(bool isActive) { }

        // ------------------------------------------------------------------------------
        // 3-3) 메서드 -> 표시(Display)
        // ------------------------------------------------------------------------------
        public override Coroutine Display(bool isActive, bool animated = true)
        {
            if (isActive) state = SettingType.Graphic;

            return base.Display(isActive, animated);
        }

        protected override IEnumerator _Display(bool isActive, float duration)
        {
            var main = root.main;

            main.gameObject.SetActive(false);

            foreach (var list in lists.Values) list.gameObject.SetActive(false);

            yield return StartCoroutine(FadeOption(isActive, duration));

            SetInteractables(true);

            if (isActive)
            {
                main.gameObject.SetActive(true);
                lists[state].Display(true);
            }
            else gameObject.SetActive(false);
        }

        protected virtual IEnumerator FadeOption(bool isFadeIn, float duration)
        {
            IAnimationCurvePreset curvePreset = menu.game.curvePreset;

            var           option        = root.option;
            RectTransform rectTransform = option.rectTransform;
            Transform     content       = option.content;

            if (isFadeIn) option.gameObject.SetActive(true);

            content.gameObject.SetActive(false);

            var     initialSetting      = option.initialSettings.rectTransforms[rectTransform];
            Vector2 originPosition      = initialSetting.anchoredPosition;
            float   height              = (canvas.transform as RectTransform).sizeDelta.y;
            Vector2 transparentPosition = originPosition + (Vector2.up * height);
            Vector2 startPosition       = isFadeIn ? transparentPosition : originPosition;
            Vector2 endPosition         = isFadeIn ? originPosition      : transparentPosition;
            float   elapsedTime         = 0f;
            var     curveType           = curvePreset.types[isFadeIn ? 1 : 2];

            while (elapsedTime < duration)
            {
                float rate = curveType.Evaluate(elapsedTime / duration);

                rectTransform.anchoredPosition = Vector2.Lerp(startPosition, endPosition, rate);

                elapsedTime += Time.unscaledDeltaTime;
                yield return null;
            }

            rectTransform.anchoredPosition = endPosition;

            if (isFadeIn) content.gameObject.SetActive(true);
            else          option.gameObject.SetActive(false);
        }

        protected virtual void DisplayList(SettingType type)
        {
            lists[state].gameObject.SetActive(false);
            lists[state = type].Display(true);
            Set();
        }

        // ------------------------------------------------------------------------------
        // 3-4) 메서드 -> 이벤트
        // ------------------------------------------------------------------------------
        protected virtual void OnClickSettingButton(SettingType type) { DisplayList(type); }

        protected virtual void OnClickGeneralButton(GeneralOptionType type)
        {
            switch (type)
            {
                case GeneralOptionType.Reset: StartCoroutine(TryReset()); break;
                case GeneralOptionType.Close: menu.Open(false);           break;
            }
        }

        protected virtual IEnumerator TryReset()
        {
            IConfirmUIController confirmUI = menu.game.ui.confirm;

            yield return confirmUI.Display("Reset", string.Empty, this, lists[state]);

            if (confirmUI.state == ConfirmUIState.Cancel) yield break;

            menu[state].Set(true);
        }
    }
}
