// //////////////////////////////////////////////////////////////////////////////
// * 요약
//    - 게임의 로비의 스테이지(레벨) 메뉴의 스테이지 리스트 UI 클래스
//    - 각 스테이지의 정보를 리스트로 표시
//
// * 목차
//    1. 인터페이스 ... Line 
//    2. 클래스 ....... Line 
//        1) 내부 타입 ... Line 
//        2) 필드 ........ Line 
//        3) 메서드 ...... Line 
//            1- 이벤트 함수 ..... Line 
//            2- 초기화 .......... Line 
//            3- 셋(Set) ......... Line 
//            4- 표시(Display) ... Line 
//            3- 이벤트 .......... Line 
// //////////////////////////////////////////////////////////////////////////////
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

using Game;

namespace Lobby
{
    using Root         = StageMenuListUIController.Root;
    using StageDatas   = DataManager.Stages;
    using StageData    = DataManager.Stages.Stage;
    using MarkType     = MarkButton.MarkType;
    using ControlState = ControlSettingManager.State;

    // //////////////////////////////////////////////////////////////////////////////
    // 1. 인터페이스(IUIBase 인터페이스 상속)
    // //////////////////////////////////////////////////////////////////////////////
    public interface IStageMenuListUIController : IUIBase
    {
        // 프로퍼티
        // Component
        Root root { get; }

        // Reference
        IStageMenuUIController ui { get; }
    }

    // //////////////////////////////////////////////////////////////////////////////
    // 2. 클래스(UIBase 클래스 상속)
    // //////////////////////////////////////////////////////////////////////////////
    public class StageMenuListUIController : UIBase, IStageMenuListUIController
    {
        // ==============================================================================
        // 1) 내부 타입 - 구조
        // ==============================================================================
        public class Root
        {
            // 내부 타입
            public class Slot : SlotBase
            {
                public MarkButton button { get; }

                public Slot(Transform transform) : base(transform) { button = content.GetComponentInChildren<MarkButton>(true); }

                public void SetContent(StageData stageData)
                {
                    string   state    = stageData.cleared  ? "Cleared"   : (stageData.playable ? string.Empty : "Locked");
                    string   selected = stageData.selected ? "(Current)" : string.Empty;
                    MarkType mark     = (stageData.playable && !stageData.selected && !stageData.cleared)
                        ? MarkType.Update : MarkType.None;

                    labelText.text        = stageData.name;
                    button.interactable   = true;
                    button.labelText.text = $"{state}\n{selected}";

                    button.SetContent(mark);
                }
            }

            // 필드
            public RectTransform         rectTransform         { get; }
            public ScrollRect            scrollRect            { get; }
            public HorizontalLayoutGroup horizontalLayoutGroup { get; }
            public RectTransform         content               { get; }
            public Dictionary<int, Slot> slots                 { get; }

            // 생성자
            public Root(Transform transform, StageDatas stageDatas)
            {
                rectTransform         = transform as RectTransform;
                scrollRect            = transform.GetComponent<ScrollRect>();
                horizontalLayoutGroup = transform.GetComponentInChildren<HorizontalLayoutGroup>(true);
                content               = horizontalLayoutGroup.transform as RectTransform;

                var slot = content.Find("Slot");

                slots = new Dictionary<int, Slot>();

                foreach (var stageData in stageDatas)
                {
                    int index = stageData.id;

                    slots.Add(index, new Slot(Instantiate(slot, content)));
                }

                DestroyImmediate(slot.gameObject);
            }

            // 메서드
            public void SetContent(StageDatas stageDatas)
            {
                foreach (var element in slots)
                {
                    int index     = element.Key;
                    var slot      = element.Value;
                    var stageData = stageDatas[index];

                    slot.SetContent(stageData);
                }
            }
        }

        // ==============================================================================
        // 2) 필드
        // ==============================================================================
        // Component & Reference
        public Root                   root { get; protected set; }
        public IStageMenuUIController ui   { get; protected set; }

        // etc.
        protected GameObject selectedObject;

        #endregion


        // ==============================================================================
        // 3) 메서드
        // ==============================================================================
        // ------------------------------------------------------------------------------
        // 3-1) 메서드 -> 이벤트 함수
        //    - 입력 타입이 Joystick일 경우 리스트의 스크롤 조작
        // ------------------------------------------------------------------------------
        protected virtual void Update()
        {
            IControlSettingManager controlSetting = GameDirector.instance.menu.setting.control;

            if (controlSetting.state != ControlState.Joystick)       return;
            if (controlSetting.connectedUI.current != (IUIBase)this) return;

            SetScroll();
        }

        protected virtual void OnDisable()
        {
            selectedObject = null;

            SetCurrent(false);
            EventSystem.current.SetSelectedGameObject(null);
        }

        // ------------------------------------------------------------------------------
        // 3-2) 메서드 -> 초기화
        // ------------------------------------------------------------------------------
        protected override void SetField()
        {
            IDataManager data = GameDirector.instance.data;

            root = new Root(transform, data.stages);
            ui   = GetComponentInParent<IStageMenuUIController>(true);

            base.SetField();

            foreach (var element in root.slots)
            {
                int index = element.Key;
                var slot  = element.Value;

                slot.button.onClick.AddListener(delegate { OnClickButton(index); });
            }
        }

        // ------------------------------------------------------------------------------
        // 3-3) 메서드 -> 셋(Set)
        //    - 현재 스테이지의 항목을 먼저 선택(Select)
        //    - 다른 스테이지 선택 시 스테이지 슬롯이 화면 가운데에 오도록 스크롤 바 이동
        // ------------------------------------------------------------------------------
        public override void Set()
        {
            base.Set();

            IDataManager data = GameDirector.instance.data;

            root.SetContent(data.stages);
        }

        public override void SelectFirstSelectable()
        {
            IGameDirector game = GameDirector.instance;
            IDataManager  data = game.data;
            
            int index = data.stages.Current.id;

            if (lastSelectedSelectable != null) lastSelectedSelectable.Select();
            else                                root.slots[index].button.Select();
        }

        protected virtual void SetScroll()
        {
            GameObject prevSelectedObject = selectedObject;
                       selectedObject     = EventSystem.current.currentSelectedGameObject;

            if ((selectedObject == null) || (selectedObject == prevSelectedObject)) return;

            var   content   = root.content;
            float slotWidth = root.slots.First().Value.rectTransform.rect.width;
            int   slotIndex = selectedObject.transform.parent.parent.GetSiblingIndex();

            content.anchoredPosition = Vector2.right * (slotWidth * slotIndex * -1f);
        }

        // ------------------------------------------------------------------------------
        // 3-4) 메서드 -> 표시(Display)
        //    - 리스트 표시와 동시에 현재 선택된 스테이지로 스크롤 바 이동
        // ------------------------------------------------------------------------------
        protected override IEnumerator _Display(bool isActive, float duration)
        {
            float interval = duration * 0.5f;

            root.content.anchoredPosition = Vector2.zero;

            yield return FadeSlots(root.slots.Values, isActive, duration, interval, FadeDirection.Up);

            SetInteractables(true);
            SelectFirstSelectable();
            SetScroll();

            if (!isActive) gameObject.SetActive(false);
        }

        // ------------------------------------------------------------------------------
        // 3-5) 메서드 -> 이벤트
        //    - 스테이지 선택 및 확인 창 표시
        // ------------------------------------------------------------------------------
        protected virtual void OnClickButton(int index) 
        {
            IGameDirector       game     = GameDirector.instance;
            IDataManager        data     = game.data;
            INoticeUIController noticeUI = game.ui.notice;

            var stageData = data.stages[index];

            if (stageData.selected) return;
            if (stageData.playable) StartCoroutine(TrySelectStage(index));
            else                    noticeUI.Display("Locked", this, ui);
        }

        protected virtual IEnumerator TrySelectStage(int index)
        {
            IConfirmUIController confirm = GameDirector.instance.ui.confirm;
            IStageMenuManager    menu    = ui.menu;

            yield return confirm.Display("Select", GetConfirmText(index), ui, this);

            if (confirm.state == ConfirmUIController.State.Cancel) yield break;

            menu.Select(index);
        }

        protected virtual string GetConfirmText(int index)
        {
            IDataManager data = GameDirector.instance.data;

            var stageData = data.stages[index];

            return $"Name\t\t: {stageData.name}\n" + 
                   $"Cleared\t: {stageData.cleared}";
        }
    }
}
