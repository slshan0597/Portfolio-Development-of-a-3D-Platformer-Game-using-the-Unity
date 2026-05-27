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


    public interface IStageMenuListUIController : IUIBase
    {
        #region Property

        // Component
        Root root { get; }

        // Reference
        IStageMenuUIController ui { get; }

        #endregion
    }


    public class StageMenuListUIController : UIBase, IStageMenuListUIController
    {
        #region Definition

        public class Root
        {
            #region Definition

            public class Slot : SlotBase
            {
                #region Field

                public MarkButton button { get; }

                #endregion


                #region Constructor

                public Slot(Transform transform) : base(transform) { button = content.GetComponentInChildren<MarkButton>(true); }

                #endregion


                #region Method

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

                #endregion
            }

            #endregion


            #region Field

            public RectTransform         rectTransform         { get; }
            public ScrollRect            scrollRect            { get; }
            public HorizontalLayoutGroup horizontalLayoutGroup { get; }
            public RectTransform         content               { get; }
            public Dictionary<int, Slot> slots                 { get; }

            #endregion


            #region Constructor

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

            #endregion


            #region Method

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

            #endregion
        }

        #endregion


        #region Field

        public Root                   root { get; protected set; }
        public IStageMenuUIController ui   { get; protected set; }

        protected GameObject selectedObject;

        #endregion


        #region Function

        #region Event

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

        #endregion


        #region Initialization

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

        #endregion


        #region Set

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

        #endregion


        #region Display

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

        #endregion


        #region Option

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

        #endregion

        #endregion
    }
}
