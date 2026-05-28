// //////////////////////////////////////////////////////////////////////////////
// * 요약
//    - 게임의 래터박스 UI 클래스
//    - 위, 아래에 가릴 비율 만큼 검은 박스 이미지를 출력
//    - 필요 시 스킵 버튼 활성화 및 스킵 기능
//
// * 목차
//    1. 인터페이스 ... Line 35
//    2. 클래스 ....... Line 58
//        1) 내부 타입 ... Line 63
//        1) 필드 ........ Line 111
//        2) 메서드 ...... Line 126
//            1- 초기화 .......... Line 129
//            2- 셋(Set) ......... Line 152
//            3- 표시(Display) ... Line 165
//                1_ 일반 ........ Line 168
//                2_ 스킵 버튼 ... Line 230
// //////////////////////////////////////////////////////////////////////////////
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Game
{
    using State             = LetterboxUIController.State;
    using Root              = LetterboxUIController.Root;
    using ImageType         = LetterboxUIController.Root.ImageType;
    using ControlState      = ControlSettingManager.State;
    using SystemControlType = ControlSettingManager.Data.SystemType;

    // //////////////////////////////////////////////////////////////////////////////
    // 1. 인터페이스(IUIBase 인터페이스 상속)
    // //////////////////////////////////////////////////////////////////////////////
    public interface ILetterboxUIController : IUIBase
    {
        // 프로퍼티
        // Component
        Root root { get; }

        // Reference
        IGameDirector game { get; }

        // State
        State state { get; }

        // Setting
        SimpleData<State, float> stateRates { get; }

        // 메서드
        Coroutine Display(State type, bool animated = true);
        Coroutine DisplayWithSkipButton(float duration);
    }

    // //////////////////////////////////////////////////////////////////////////////
    // 2. 클래스(UIBase 클래스 상속)
    // //////////////////////////////////////////////////////////////////////////////
    public class LetterboxUIController : UIBase, ILetterboxUIController
    {
        // ==============================================================================
        // 1) 내부 타입
        // ==============================================================================
        // 가릴 비율
        public enum State { None, Normal, Closed, Open }

        public class Root : MenuBase
        {
            public enum ImageType { Top, Bottom }

            public class Skip : MenuBase
            {
                public SizeFitableText contentText { get; }

                public Skip(Transform transform) : base(transform)
                {
                    contentText = content.GetComponentInChildren<SizeFitableText>(true);
                }

                public void SetContent(IControlSettingManager controlSetting)
                {
                    string baseStr = "Skip : ";

                    switch (controlSetting.state)
                    {
                        case ControlState.Touch: contentText.text = $"{baseStr}Touch";                                            break;
                        default:                 contentText.text = $"{baseStr}{controlSetting.GetKey(SystemControlType.Pause)}"; break;
                    }
                }
            }

            public Dictionary<ImageType, Image> images { get; }
            public Skip                         skip   { get; }

            public Root(Transform transform) : base(transform)
            {
                images = new Dictionary<ImageType, Image>();
                skip   = new Skip(content.Find("Skip"));

                foreach (var image in content.GetComponentsInChildren<Image>(true))
                {
                    string name = image.name.Replace(" ", string.Empty).Replace("Image", string.Empty);

                    if (Enum.TryParse(name, out ImageType type)) images.Add(type, image);
                }
            }
        }

        // ==============================================================================
        // 2) 필드
        // ==============================================================================
        // Component & Reference
        public Root          root { get; protected set; }
        public IGameDirector game { get; protected set; }

        // State
        public State state { get; protected set; }

        // Setting
        [SerializeField] protected SimpleData<State, float> _stateRates;

        public SimpleData<State, float> stateRates { get { return _stateRates; } }

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
            game = GetComponentInParent<IGameDirector>(true);
        }

        protected override void ResetField() 
        { 
            _defaultDuration = 0.5f;
            _stateRates      = new SimpleData<State, float>(
                new List<SimpleData<State, float>.Element>()
                {
                    new SimpleData<State, float>.Element(State.Normal, 0.25f),
                    new SimpleData<State, float>.Element(State.Closed, 1f),
                    new SimpleData<State, float>.Element(State.Open,   0f)
                });
        }

        // ------------------------------------------------------------------------------
        // 3-2) 메서드 -> 셋(Set)
        //    - 컨트롤 설정에 따른 UI 갱신
        // ------------------------------------------------------------------------------
        public override void Set()
        {
            base.Set();

            IControlSettingManager controlSetting = GameDirector.instance.menu.setting.control;

            root.skip.SetContent(controlSetting);
        }

        // ------------------------------------------------------------------------------
        // 3-3) 메서드 -> 표시(Display)
        // ------------------------------------------------------------------------------
        // ******************************************************************************
        // 3-3-1) 메서드 -> 표시 -> 일반
        //    - 위, 아래 가려지는 부분의 비율(타입) 만큼 검은 박스 이미지 출력
        //    - Normal : 0.25 / Closed : 1 / Open : 0
        // ******************************************************************************
        public virtual Coroutine Display(State type, bool animated = true)
        {
            gameObject.SetActive(true);
            Set();

            state = type;

            return StartCoroutine(_Display(animated ? defaultDuration : 0f));
        }

        protected virtual IEnumerator _Display(float duration)
        {
            root.skip.gameObject.SetActive(false);

            var images = root.images;

            foreach (var element in images)
            {
                ImageType type  = element.Key;
                var       image = element.Value;

                if (image != images.Values.Last()) StartCoroutine(Fade(type, image.rectTransform, duration));
                else                               yield return Fade(type, image.rectTransform, duration);
            }

            if (state == State.Open) gameObject.SetActive(false);
        }

        protected virtual IEnumerator Fade(ImageType imageType, RectTransform image, float duration)
        {
            IAnimationCurvePreset curvePreset = GameDirector.instance.curvePreset;

            float width  = image.sizeDelta.x;
            float height = (canvas.transform as RectTransform).sizeDelta.y * 0.5f;

            image.sizeDelta = new Vector2(width, height);

            float   stateRate     = stateRates[state];
            float   sign          = (imageType == ImageType.Top) ? 1f : -1f;
            Vector2 startPosition = image.anchoredPosition;
            Vector2 endPosition   = Vector2.up * height * stateRate * sign;
            float   elapsedTime   = 0f;
            var     curveType     = curvePreset.types[1];

            while (elapsedTime < duration)
            {
                float rate = curveType.Evaluate(elapsedTime / duration);

                image.anchoredPosition = Vector2.Lerp(startPosition, endPosition, rate);

                elapsedTime += Time.unscaledDeltaTime;
                yield return null;
            }

            image.anchoredPosition = endPosition;
        }

        // ******************************************************************************
        // 3-3-2) 메서드 -> 표시 -> 스킵 버튼
        //    - 스킵 버튼 추가
        //    - 스킵 시 바로 Closed 상태로 넘어가서 화면을 전부 가림
        // ******************************************************************************
        public virtual Coroutine DisplayWithSkipButton(float duration)
        {
            gameObject.SetActive(true);
            Set();
            SetInteractables(false);

            state = State.Normal;

            return StartCoroutine(_DisplayWithSkipButton(duration));
        }

        protected virtual IEnumerator _DisplayWithSkipButton(float duration)
        {
            IControlSettingManager controlSetting = GameDirector.instance.menu.setting.control;

            yield return _Display(defaultDuration);

            root.skip.gameObject.SetActive(true);

            float elapsedTime  = 0f;
            float waitDuration = duration - (defaultDuration * 2f);

            while (elapsedTime < waitDuration)
            {
                switch (controlSetting.state)
                {
                    case ControlState.Touch: if (Input.anyKeyDown) { state = State.Closed; goto Skip; } break;
                    default:
                        {
                            KeyCode key = controlSetting.GetKey(SystemControlType.Pause);

                            if (Input.GetKeyDown(key))
                            {
                                state = State.Closed;

                                goto Skip;
                            }
                        }
                        break;
                }

                elapsedTime += Time.unscaledDeltaTime;
                yield return null;
            }

            Skip:

            root.skip.gameObject.SetActive(false);

            switch (state)
            {
                case State.Closed: yield return _Display(defaultDuration);                   break;
                default:           yield return new WaitForSecondsRealtime(defaultDuration); break;
            }
        }
    }
}
