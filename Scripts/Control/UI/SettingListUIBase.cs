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


    public interface ISettingListUIBase : IUIBase
    {
        #region Property

        // Component
        Root root { get; }

        // Reference
        ISettingMenuUIController ui { get; }

        // State
        bool isDisplaying { get; }

        // Setting
        SettingType type         { get; }
        float       intervalRate { get; }

        #endregion
    }


    public class SettingListUIBase : UIBase, ISettingListUIBase
    {
        #region Definition

        public class Root
        {
            #region Field

            public RectTransform       rectTransform       { get; }
            public ScrollRect          scrollRect          { get; }
            public VerticalLayoutGroup verticalLayoutGroup { get; }
            public RectTransform       content             { get; }
            public List<SlotBase>      slots               { get; }

            #endregion


            #region Constructor

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

            #endregion
        }

        #endregion


        #region Field

        public Root                     root { get; protected set; }
        public ISettingMenuUIController ui   { get; protected set; }

        public bool        isDisplaying { get; protected set; }
        public SettingType type         { get; protected set; }

        [SerializeField, Range(0f, 1f)] protected float _intervalRate;
        
        public float intervalRate { get { return _intervalRate; } }

        protected GameObject selectedObject;

        #endregion


        #region Function

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

            foreach (var selectable in selectables) selectable.interactable = true;
        }

        #endregion


        #region Initialization

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

        #endregion


        #region Set

        protected virtual void SetScroll()
        {
            GameObject prevSelectedObject = selectedObject;
                       selectedObject     = EventSystem.current.currentSelectedGameObject;

            if ((selectedObject == null) || (selectedObject == prevSelectedObject)) return;

            var content = root.content;

            if (prevSelectedObject == null) content.anchoredPosition = Vector2.zero;
            else
            {
                var slot = selectedObject.transform.parent.parent.parent.parent;

                if (!slot.name.Contains("Slot")) return;

                Debug.Log(slot.name);

                float   spacing           = root.verticalLayoutGroup.spacing;
                float   slotHeight        = root.slots[0].rectTransform.rect.height + spacing;
                int     slotIndex         = slot.GetSiblingIndex() + 1;
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


        #region Display

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

        #endregion

        #endregion
    }
}
