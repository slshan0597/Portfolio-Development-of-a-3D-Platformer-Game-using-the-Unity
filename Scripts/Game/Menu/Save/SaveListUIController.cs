// //////////////////////////////////////////////////////////////////////////////
// * 요약
//    - 세이브 메뉴의 세이브 리스트 목록 UI 클래스
//    - 저장된 세이브 리스트 표시 및 조작
//
// * 목차
//    1. 인터페이스 ... Line 33
//    2. 클래스 ....... Line 49
//        1) 내부 타입 ... Line 54
//        2) 필드 ........ Line 148
//        3) 메서드 ...... Line 163
//            1- 이벤트 함수 ... Line 166
//            2- 초기화 ........ Line 187
//            3- 셋(Set) ....... Line 211
//            4- 이벤트 ........ Line 260
// //////////////////////////////////////////////////////////////////////////////
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game
{
    using Root           = SaveListUIController.Root;
    using State          = SaveMenuUIController.State;
    using SaveData       = SaveMenuManager.Data;
    using ConfirmUIState = ConfirmUIController.State;
    using ControlState   = ControlSettingManager.State;

    // //////////////////////////////////////////////////////////////////////////////
    // 1. 인터페이스(IUIBase 인터페이스 상속)
    // //////////////////////////////////////////////////////////////////////////////
    public interface ISaveListUIController : IUIBase
    {
        // 프로퍼티
        // Component
        Root root { get; }

        // Reference
        ISaveMenuUIController ui { get; }

        // Setting
        int maxCount { get; }
    }

    // //////////////////////////////////////////////////////////////////////////////
    // 2. 클래스(UIBase 클래스 상속)
    // //////////////////////////////////////////////////////////////////////////////
    public class SaveListUIController : UIBase, ISaveListUIController
    {
        // ==============================================================================
        // 1) 내부 타입
        // ==============================================================================
        public class Root
        {
            // 내부 타입 - 루트
            public class Slot : SlotBase
            {
                // 필드 - 슬롯
                public SoundButton button      { get; }
                public Text        contentText { get; }

                // 생성자 - 슬롯
                public Slot(Transform transform) : base(transform)
                {
                    button      = content.GetComponent<SoundButton>();
                    contentText = content.Find("Content Text").GetComponent<Text>();
                }

                // 메서드 - 슬롯
                public void SetContent(int index, SaveData saveData)
                {
                    int levelCount = 0;
                    int clearCount = 0;

                    foreach (var stageSaveData in saveData.stages)
                    {
                        var levelSaveDatas = stageSaveData.levels;

                        levelCount += levelSaveDatas.Count();
                        clearCount += levelSaveDatas.Count(levelSaveData => levelSaveData.cleared);
                    }

                    var recordSaveData = saveData.record;

                    button.interactable = true;
                    labelText.text      = $"Data {index.ToString("00")}";
                    contentText.text    = $"Date Saved\t: {recordSaveData.dateSaved}\n" +
                                          $"Run Time\t: {recordSaveData.runTime}\n" +
                                          $"Progress\t\t: {(clearCount / (float)levelCount).ToString("0%")}";
                }

                public void SetContent(State state)
                {
                    button.interactable = (state == State.Save);
                    labelText.text      = "Empty";
                    contentText.text    = string.Empty;
                }
            }

            // 필드 - 루트
            public RectTransform       rectTransform       { get; }
            public ScrollRect          scrollRect          { get; }
            public VerticalLayoutGroup verticalLayoutGroup { get; }
            public RectTransform       content             { get; }
            public List<Slot>          slots               { get; }
            public List<SlotBase>      boundarySlots       { get; }

            // 생성자 - 루트
            public Root(Transform transform, int count)
            {
                rectTransform       = transform as RectTransform;
                scrollRect          = transform.GetComponent<ScrollRect>();
                verticalLayoutGroup = transform.GetComponentInChildren<VerticalLayoutGroup>(true);
                content             = verticalLayoutGroup.transform as RectTransform;

                var slot = content.Find("Slot");

                slots         = new List<Slot>() { new Slot(slot) };
                boundarySlots = new List<SlotBase>();

                for (int i = 0; i < content.childCount; i++)
                    if (content.GetChild(i).name.Split(" ")[0] == "Boundary")
                        boundarySlots.Add(new SlotBase(content.GetChild(i)));

                for (int i = 0; i < count; i++) slots.Add(new Slot(Instantiate(slot, content)));
            }

            // 메서드 - 루트
            public void SetContent(State state, Dictionary<int, SaveData> saveDatas)
            {
                for (int index = 0; index < slots.Count; index++)
                {
                    if (saveDatas.ContainsKey(index)) slots[index].SetContent(index, saveDatas[index]);
                    else                              slots[index].SetContent(state);
                }

                slots[0].gameObject.SetActive(state == State.Load);
                boundarySlots[0].gameObject.SetActive(state == State.Load);

                if (!slots[0].gameObject.activeInHierarchy) slots[0].button.interactable = false;
            }
        }

        // ==============================================================================
        // 2) 필드
        // ==============================================================================
        // Component & Reference
        public Root                  root { get; protected set; }
        public ISaveMenuUIController ui   { get; protected set; }

        // Setting
        [SerializeField] protected int _maxCount;

        public int maxCount { get { return _maxCount; } }

        // etc.
        protected GameObject selectedObject;

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

            if (controlSetting.state               != ControlState.Joystick) return;
            if (controlSetting.connectedUI.current != (IUIBase)this)         return;

            SetScroll();
        }

        protected virtual void OnDisable()
        { 
            selectedObject = null;

            SetCurrent(false);
        }

        // ------------------------------------------------------------------------------
        // 3-2) 메서드 -> 초기화
        // ------------------------------------------------------------------------------
        protected override void SetField()
        {
            ui   = GetComponentInParent<ISaveMenuUIController>(true);
            root = new Root(transform, maxCount);

            base.SetField();

            for (int i = 0; i < root.slots.Count; i++)
            {
                int index = i;

                root.slots[index].button.onClick.AddListener(delegate { OnClickButton(index); });
            }
        }

        protected override void ResetField()
        { 
            _defaultDuration = 0f;
            _maxCount        = 20;
        }

        // ------------------------------------------------------------------------------
        // 3-3) 메서드 -> 셋(Set)
        //    - 세이브 메뉴에서 불러온 데이터 리스트 표시
        //    - 스크롤 내에 선택(Select)된 슬롯의 위치에 따라 스크롤 바를 이동
        //    - 선택된 슬롯이 컨테이너(마스킹 된 부분) 범위를 벗어나 있는 경우, 컨테이너의 위치를 조절하여 슬롯을 컨테이너 안에 표시
        // ------------------------------------------------------------------------------
        public override void Set()
        {
            base.Set();

            ISaveMenuManager saveMenu = ui.menu;

            root.SetContent(ui.state, saveMenu.datas);
        }

        protected virtual void SetScroll()
        {
            GameObject prevSelectedObject = selectedObject;
                       selectedObject     = EventSystem.current.currentSelectedGameObject;

            if ((selectedObject == null) || (selectedObject == prevSelectedObject)) return;

            var content = root.content;

            if (prevSelectedObject == null) content.anchoredPosition = Vector2.zero;    // 스크롤 초기화
            else
            {
                float   spacing           = root.verticalLayoutGroup.spacing;
                float   slotHeight        = root.slots[0].rectTransform.rect.height + spacing;    // 슬롯의 높이(여백 포함)
                int     slotIndex         = selectedObject.transform.parent.GetSiblingIndex() + ((ui.state == State.Load) ? 1 : -1);
                float   targetHeight      = (slotHeight * slotIndex) + spacing;    // 첫 번째부터 선택된 슬롯 까지의 높이 + 최상단 여백
                Vector2 containerPosition = root.content.anchoredPosition;         // 현재 컨테이너의 위치
                float   containerHeight   = root.rectTransform.rect.height;        // 컨테이너(마스킹 된 부분)의 높이

                // 목표 높이가 현재 컨테이너의 위치 + 슬롯의 높이보다 작은 경우(현재 선택된 슬롯이 창에서 위로 벗어난 경우)
                if (targetHeight < (containerPosition.y + slotHeight))
                {
                    // 컨테이너의 위치를 목표 높이에서 슬롯의 높이(최상단 여백 포함)를 뺀 값 만큼 이동
                    content.anchoredPosition = Vector2.up * (targetHeight - slotHeight - spacing);
                }
                // 목표 높이가 현재 컨테이너의 위치 + 컨테이너의 높이보다 큰 경우(현재 선택된 슬롯이 창에서 아래로 벗어난 경우)
                else if (targetHeight > (containerPosition.y + containerHeight))
                {
                    // 컨테이너의 위치를 목표 높이와 컨테이너의 높이의 차이만큼 이동
                    content.anchoredPosition = Vector2.up * (targetHeight - containerHeight);
                }
            }
        }

        // ------------------------------------------------------------------------------
        // 3-4) 메서드 -> 이벤트
        //    - 세이브 리스트의 조작 및 업데이트
        // ------------------------------------------------------------------------------
        protected virtual void OnClickButton(int index) { StartCoroutine(TryUpdateData(index)); }

        protected virtual IEnumerator TryUpdateData(int index)
        {
            ISaveMenuManager     menu      = ui.menu;
            IConfirmUIController confirmUI = menu.game.ui.confirm;

            yield return confirmUI.Display(GetConfirmText(index, ui.state), string.Empty, this, ui);

            if (confirmUI.state == ConfirmUIState.Cancel) yield break;

            switch (ui.state)
            {
                case State.Load:
                    {
                        menu.Load(index);
                        menu.ui.gameObject.SetActive(false);
                    }
                    break;

                case State.Save: menu.Save(index); break;
            }
        }

        protected virtual string GetConfirmText(int index, State state)
        {
            ISaveMenuManager menu = ui.menu;

            string label = string.Empty;

            switch (state)
            {
                case State.Load: label = "Load";                                              break;
                case State.Save: label = menu.datas.ContainsKey(index) ? "Override" : "Save"; break;
            }

            return label;
        }
    }
}
