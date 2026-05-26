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


    public interface ISaveListUIController : IUIBase
    {
        #region Property

        // Component
        Root root { get; }

        // Reference
        ISaveMenuUIController ui { get; }

        // Setting
        int maxCount { get; }

        #endregion
    }


    public class SaveListUIController : UIBase, ISaveListUIController
    {
        #region Definition

        public class Root
        {
            #region Definition

            public class Slot : SlotBase
            {
                #region Field

                public SoundButton button      { get; }
                public Text        contentText { get; }

                #endregion


                #region Constructor

                public Slot(Transform transform) : base(transform)
                {
                    button      = content.GetComponent<SoundButton>();
                    contentText = content.Find("Content Text").GetComponent<Text>();
                }

                #endregion


                #region Method

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

                #endregion
            }

            #endregion


            #region Field

            public RectTransform       rectTransform       { get; }
            public ScrollRect          scrollRect          { get; }
            public VerticalLayoutGroup verticalLayoutGroup { get; }
            public RectTransform       content             { get; }
            public List<Slot>          slots               { get; }
            public List<SlotBase>      boundarySlots       { get; }

            #endregion


            #region Constructor

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

            #endregion


            #region Method

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

            #endregion
        }

        #endregion


        #region Field

        public Root                  root { get; protected set; }
        public ISaveMenuUIController ui   { get; protected set; }

        [SerializeField] protected int _maxCount;

        public int maxCount { get { return _maxCount; } }

        protected GameObject selectedObject;

        #endregion


        #region Method

        #region Event

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

        #endregion


        #region Initialization

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

        #endregion


        #region Set

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

            if (prevSelectedObject == null) content.anchoredPosition = Vector2.zero;
            else
            {
                float   spacing           = root.verticalLayoutGroup.spacing;
                float   slotHeight        = root.slots[0].rectTransform.rect.height + spacing;
                int     slotIndex         = selectedObject.transform.parent.GetSiblingIndex() + ((ui.state == State.Load) ? 1 : -1);
                float   targetHeight      = (slotHeight * slotIndex) + spacing;
                Vector2 containerPosition = root.content.anchoredPosition;
                float   containerHeight   = root.rectTransform.rect.height;

                if (targetHeight < (containerPosition.y + slotHeight))
                {
                    content.anchoredPosition = Vector2.up * (targetHeight - slotHeight - spacing);
                }
                else if (targetHeight > (containerPosition.y + containerHeight))
                {
                    content.anchoredPosition = Vector2.up * (targetHeight - containerHeight);
                }
            }
        }

        #endregion


        #region Slot

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

        #endregion

        #endregion
    }
}
