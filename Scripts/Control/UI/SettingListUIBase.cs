// //////////////////////////////////////////////////////////////////////////////
// * 요약
//    - 게임의 설정(그래픽, 오디오, 컨트롤)을 하기 위한 UI 기반 클래스
//    - 하나의 스크롤 안에 다수의 슬롯(Slot) 컨테이너 배치
//
// * 목차
//    1. 인터페이스 ... Line 
//    2. 클래스 ....... Line 
//        1) 정의 ..... Line 
//        2) 필드 ..... Line 
//        3) 메서드 ... Line 
//            1- 이벤트 함수 ..... Line 
//            2- 초기화 .......... Line 
//            3- 셋(Set) ......... Line 
//            4- 표시(Display) ... Line 
// //////////////////////////////////////////////////////////////////////////////
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game
{
    using Root         = SettingListUIBase.Root;
    using SettingType  = SettingBase.Type;
    using ControlState = ControlSettingManager.State;

    // //////////////////////////////////////////////////////////////////////////////
    // 1. 인터페이스(IUIBase 인터페이스 상속)
    // //////////////////////////////////////////////////////////////////////////////
    public interface ISettingListUIBase : IUIBase
    {
        // 프로퍼티
        // Component
        Root root { get; }

        // Reference
        ISettingMenuUIController ui { get; }

        // State
        bool isDisplaying { get; }

        // Setting
        SettingType type         { get; }
        float       intervalRate { get; }
    }

    // //////////////////////////////////////////////////////////////////////////////
    // 2. 클래스(UIBase 클래스 상속)
    //    - 클래스 확장을 위한 기반 기능만을 구현
    // //////////////////////////////////////////////////////////////////////////////
    public class SettingListUIBase : UIBase, ISettingListUIBase
    {
        // ==============================================================================
        // 1) 정의
        //    - UI의 구조에 대한 클래스
        // ==============================================================================
        public class Root
        {
            public RectTransform       rectTransform       { get; }
            public ScrollRect          scrollRect          { get; }
            public VerticalLayoutGroup verticalLayoutGroup { get; }
            public RectTransform       content             { get; }
            public List<SlotBase>      slots               { get; }

            public Root(Transform transform)
            {
                rectTransform       = transform as RectTransform;
                scrollRect          = transform.GetComponent<ScrollRect>();
                verticalLayoutGroup = transform.GetComponentInChildren<VerticalLayoutGroup>(true);
                content             = verticalLayoutGroup.transform as RectTransform;
                slots               = new List<SlotBase>();

                for (int i = 0; i < content.childCount; i++)
                    slots.Add(new SlotBase(content.GetChild(i)));
            }
        }

        // ==============================================================================
        // 2) 필드
        // ==============================================================================
        // Component & Reference
        public Root                     root { get; protected set; }
        public ISettingMenuUIController ui   { get; protected set; }

        // State
        public bool        isDisplaying { get; protected set; }
        public SettingType type         { get; protected set; }

        // Setting
        [SerializeField, Range(0f, 1f)] protected float _intervalRate;
        
        public float intervalRate { get { return _intervalRate; } }

        // etc.
        protected GameObject selectedObject;

        // ==============================================================================
        // 3) 메서드
        //    - 부모 클래스의 함수들을 재정의하여 확장
        // ==============================================================================
        // ------------------------------------------------------------------------------
        // 3-1) 메서드 -> 이벤트 함수
        //    - 입력 타입이 Joystick일 경우 Select된 오브젝트에 따라 스크롤 바 설정
        // ------------------------------------------------------------------------------
        protected virtual void Update()
        {
            IControlSettingManager controlSetting = GameDirector.instance.menu.setting.control;

            if (controlSetting.state               != ControlState.Joystick) return;
            if (controlSetting.connectedUI.current != (IUIBase)this)         return;

            SetScroll();
        }

        protected virtual void OnDisable()
        {
            selectedObject = null;

            SetCurrent(false);

            foreach (var selectable in selectables) selectable.interactable = true;
        }

        // ------------------------------------------------------------------------------
        // 3-2) 메서드 -> 초기화
        //    - 필드(컴포넌트 등) 초기화
        // ------------------------------------------------------------------------------
        protected override void SetField()
        {
            base.SetField();

            root = new Root(transform);
            ui   = GetComponentInParent<ISettingMenuUIController>(true);
        }

        protected override void ResetField()
        {
            base.ResetField();

            _intervalRate = 0.2f;
        }

        // ------------------------------------------------------------------------------
        // 3-3) 메서드 -> 셋(Set)
        //    - 스크롤 내에 선택(Select)된 슬롯의 위치에 따라 스크롤 바를 이동
        //    - 선택된 슬롯이 컨테이너(마스킹 된 부분) 범위를 벗어나 있는 경우, 컨테이너의 위치를 조절하여 슬롯을 컨테이너 안에 표시
        // ------------------------------------------------------------------------------
        protected virtual void SetScroll()
        {
            GameObject prevSelectedObject = selectedObject;
                       selectedObject     = EventSystem.current.currentSelectedGameObject;

            if ((selectedObject == null) || (selectedObject == prevSelectedObject)) return;

            var content = root.content;

            if (prevSelectedObject == null) content.anchoredPosition = Vector2.zero;    // 스크롤 초기화
            else
            {
                var slot = selectedObject.transform.parent.parent.parent.parent;

                if (!slot.name.Contains("Slot")) return;

                float   spacing           = root.verticalLayoutGroup.spacing;
                float   slotHeight        = root.slots[0].rectTransform.rect.height + spacing;    // 슬롯의 높이(여백 포함)
                int     slotIndex         = slot.GetSiblingIndex() + 1;
                float   targetHeight      = (slotHeight * slotIndex) + spacing;    // 첫 번째부터 선택된 슬롯 까지의 높이 + 최상단 여백
                Vector2 containerPosition = root.content.anchoredPosition;         // 현재 컨테이너의 위치
                float   containerHeight   = root.rectTransform.rect.height;        // 컨테이너(마스킹 된 부분)의 높이

                // 목표 높이가 현재 컨테이너의 위치 + 슬롯의 높이보다 작은 경우(현재 선택된 슬롯이 창에서 위로 벗어난 경우)
                if (targetHeight < (containerPosition.y + slotHeight))
                {
                    content.anchoredPosition = Vector2.up * (targetHeight - slotHeight - spacing);    // 컨테이너의 위치를 목표 높이에서 슬롯의 높이(최상단 여백 포함)를 뺀 값 만큼 이동
                }
                // 목표 높이가 현재 컨테이너의 위치 + 컨테이너의 높이보다 큰 경우(현재 선택된 슬롯이 창에서 아래로 벗어난 경우)
                else if (targetHeight > (containerPosition.y + containerHeight))
                {
                    content.anchoredPosition = Vector2.up * (targetHeight - containerHeight);    // 컨테이너의 위치를 목표 높이와 컨테이너의 높이의 차이만큼 이동
                }
            }
        }

        // ------------------------------------------------------------------------------
        // 3-4) 메서드 -> 표시(Display)
        //    - 컨테이너의 슬롯들을 일정 간격을 두고 좌-우로 이동하여 배치
        //    - Selectable 오브젝트의 활성화
        // ------------------------------------------------------------------------------
        public override Coroutine Display(bool isActive, bool animated = true)
        {
            isDisplaying = true;

            return base.Display(isActive, animated);
        }

        protected override IEnumerator _Display(bool isActive, float duration)
        {
            float interval = duration * intervalRate;

            yield return FadeSlots(root.slots, isActive, duration, interval, FadeDirection.Left);

            SetInteractables(true);

            isDisplaying = false;

            if (!isActive) gameObject.SetActive(false);
        }
    }
}
