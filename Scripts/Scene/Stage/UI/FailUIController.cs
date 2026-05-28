// //////////////////////////////////////////////////////////////////////////////
// * 요약
//    - 스테이지 씬의 실패 UI 클래스
//    - 게임 오버 표시
//    - 재시작, 체크포인트 시작, 나가기 옵션 표시
//
// * 목차
//    1. 인터페이스 ... Line 
//    2. 클래스 ....... Line 
//        1) 내부 타입 ... Line 
//        2) 필드 ........ Line 
//        3) 메서드 ...... Line 
//            1- 초기화 .......... Line 
//            2- 셋(Set) ......... Line 
//            3- 표시(Display) ... Line 
//            4- 이벤트 .......... Line 
// //////////////////////////////////////////////////////////////////////////////
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using Game;

namespace Stage
{
    using Root      = FailUIController.Root;
    using MenuType  = FailUIController.Root.Menu.Type;
    using SceneType = SceneBase.Type;

    // //////////////////////////////////////////////////////////////////////////////
    // 1. 인터페이스(IUIBase 인터페이스 상속)
    // //////////////////////////////////////////////////////////////////////////////
    public interface IFailUIController : IUIBase
    {
        // 프로퍼티
        // Component
        Root root { get; }

        // Reference
        ISceneDirector scene { get; }
    }

    // //////////////////////////////////////////////////////////////////////////////
    // 2. 클래스(UIBase 클래스 상속)
    // //////////////////////////////////////////////////////////////////////////////
    public class FailUIController : UIBase, IFailUIController
    {
        // ==============================================================================
        // 1) 내부 타입 - 구조
        // ==============================================================================
        public class Root : WindowBase
        {
            public class Menu : MenuBase
            {
                public enum Type { CheckPoint, Restart, Exit }

                public Dictionary<Type, Button> buttons { get; }

                public Menu(Transform transform) : base(transform)
                {
                    buttons = new Dictionary<Type, Button>();

                    foreach (var button in content.GetComponentsInChildren<Button>(true))
                    {
                        string name = button.name.Replace(" ", string.Empty).Replace("Button", string.Empty);

                        if (Enum.TryParse(name, out Type type)) buttons.Add(type, button);
                    }
                }

                public void SetContent(ICheckPointable checkPoint)
                {
                    buttons[MenuType.CheckPoint].interactable = checkPoint != null;
                }
            }

            public SlotBase label { get; }
            public Menu     menu  { get; }

            public Root(Transform transform) : base(transform)
            {
                label = new SlotBase(content.Find("Label"));
                menu  = new Menu(content.Find("Menu"));
            }
        }

        // ==============================================================================
        // 2) 필드
        // ==============================================================================
        // Component & Reference
        public Root           root  { get; protected set; }
        public ISceneDirector scene { get; protected set; }

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
            scene = GetComponentInParent<ISceneDirector>(true);

            foreach (var element in root.menu.buttons)
            {
                MenuType type   = element.Key;
                var      button = element.Value;

                button.onClick.AddListener(delegate { OnClickMenuButton(type); });
            }
        }

        protected override void ResetField() { _defaultDuration = 2f; }

        // ------------------------------------------------------------------------------
        // 3-2) 메서드 -> 셋(Set)
        //    - 체크포인트 존재 시 체크포인트 옵션 표시
        // ------------------------------------------------------------------------------
        public override void Set()
        {
            ICheckPointable checkPoint = scene.stages.current.levels.current.checkPoints.current;

            root.menu.SetContent(checkPoint);
        }

        // ------------------------------------------------------------------------------
        // 3-3) 메서드 -> 표시(Display)
        // ------------------------------------------------------------------------------
        protected override IEnumerator _Display(bool isActive, float duration)
        {
            if (!isActive) yield return base._Display(isActive, duration);
            else
            {
                root.menu.gameObject.SetActive(false);
                StartCoroutine(FadeSlot(root.label, true, duration, FadeDirection.Down));

                yield return FadeBackGroundImage(root, true, duration);

                root.menu.gameObject.SetActive(true);

                yield return FadeGraphics(root.menu, true, duration * 0.25f);

                SetInteractables(true);
            }
        }

        protected override IEnumerator FadeBackGroundImage(WindowBase window, bool isFadeIn, float duration)
        {
            RectTransform image = window.backGroundImage.rectTransform;

            image.gameObject.SetActive(true);

            Vector2 originMaskSize      = window.initialSettings.rectTransforms[image].sizeDelta;
            Vector2 transparentMaskSize = Vector2.zero;
            Vector2 startMaskSize       = isFadeIn ? originMaskSize      : transparentMaskSize;
            Vector2 endMaskSize         = isFadeIn ? transparentMaskSize : originMaskSize;
            float   elapsedTime         = 0f;

            while (elapsedTime < duration)
            {
                float rate = elapsedTime / duration;

                image.sizeDelta = Vector2.Lerp(startMaskSize, endMaskSize, rate);

                elapsedTime += Time.unscaledDeltaTime;
                yield return null;
            }

            image.sizeDelta = endMaskSize;
        }

        // ------------------------------------------------------------------------------
        // 3-4) 메서드 -> 이벤트
        //    - 버튼 타입에 따른 이벤트 수행
        // ------------------------------------------------------------------------------
        protected virtual void OnClickMenuButton(MenuType type)
        {
            switch (type)
            {
                case MenuType.CheckPoint:
                    {
                        SetInteractables(false);
                        scene.Exit(SceneType.None);
                    }
                    break;

                case MenuType.Restart:
                    {
                        SetInteractables(false);
                        scene.Exit(SceneType.Stage);
                    }
                    break;

                case MenuType.Exit: StartCoroutine(TryExit(scene)); break;
            }
        }

        protected virtual IEnumerator TryExit(ISceneDirector scene)
        {
            {
                IConfirmUIController confirmUI = GameDirector.instance.ui.confirm;

                yield return confirmUI.Display("Exit", string.Empty, this);

                if (confirmUI.state == ConfirmUIController.State.Cancel) yield break;
            }

            SetInteractables(false);
            scene.Exit(SceneType.Lobby);
        }
    }
}
