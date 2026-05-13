using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

using Game;

using InitialSettings = UIBase.MenuBase.InitialSettings;
using ResolutionType  = Game.GraphicSettingManager.Data.ResolutionType;
using ControlState    = Game.ControlSettingManager.State;


public interface IUIBase
{
    #region Property

    // Component
    GameObject   gameObject  { get; }
    Selectable[] selectables { get; }

    // Reference
    Canvas       canvas       { get; }
    CanvasScaler canvasScaler { get; }

    // Setting
    float defaultDuration { get; }

    #endregion


    #region Method

    void      Set();
    void      SetInteractables(bool interactable);
    void      SelectFirstSelectable();
    Coroutine Display(bool isActive, bool animated = true);

    #endregion
}


public class UIBase : MonoBehaviour, IUIBase
{
    #region Definition

    public enum FadeDirection { None, Up, Down, Left, Right }


    public class MenuBase
    {
        #region Definition

        public class InitialSettings
        {
            #region Definition

            public class RectTransformSetting
            {
                #region Field

                public Vector2 anchoredPosition   { get; }
                public Vector3 anchoredPosition3D { get; }
                public Vector2 sizeDelta          { get; }
                public Vector3 localScale         { get; }

                #endregion


                #region Constructor

                public RectTransformSetting(RectTransform rectTransform)
                {
                    anchoredPosition   = rectTransform.anchoredPosition;
                    anchoredPosition3D = rectTransform.anchoredPosition3D;
                    sizeDelta          = rectTransform.sizeDelta;
                    localScale         = rectTransform.localScale;
                }

                #endregion
            }

            #endregion


            #region Field

            public Dictionary<RectTransform, RectTransformSetting> rectTransforms { get; }
            public Dictionary<MaskableGraphic, Color>              graphics       { get; }

            #endregion


            #region Constructor

            public InitialSettings(IEnumerable<RectTransform> rectTransforms, IEnumerable<MaskableGraphic> graphics)
            {
                this.rectTransforms = new Dictionary<RectTransform, RectTransformSetting>();
                this.graphics       = new Dictionary<MaskableGraphic, Color>();

                foreach (var rectTransform in rectTransforms)
                    this.rectTransforms.Add(rectTransform, new RectTransformSetting(rectTransform));
                foreach (var graphic in graphics)
                    this.graphics.Add(graphic, graphic.color);
            }

            #endregion
        }

        #endregion


        #region Field

        public Transform         transform     { get; }
        public GameObject        gameObject    { get; }
        public RectTransform     rectTransform { get; }
        public RectTransform     content       { get; }
        public RectTransform[]   items         { get; }
        public MaskableGraphic[] graphics      { get; }

        public InitialSettings initialSettings { get; protected set; }

        #endregion


        #region Constructor

        public MenuBase(Transform transform)
        {
            this.transform = transform;
            gameObject     = transform.gameObject;
            rectTransform  = transform as RectTransform;
            content        = transform.Find("Content") as RectTransform;
            items          = new RectTransform[content.childCount];
            graphics       = transform.GetComponentsInChildren<MaskableGraphic>(true);

            for (int i = 0; i < items.Length; i++) items[i] = content.GetChild(i) as RectTransform;

            var rectTransforms = new List<RectTransform>() { rectTransform, content };

            rectTransforms.AddRange(items);

            initialSettings = new InitialSettings(rectTransforms, graphics);
        }

        #endregion
    }


    public class WindowBase : MenuBase
    {
        #region Field

        public Image backGroundImage { get; }

        #endregion


        #region Constructor

        public WindowBase(Transform transform) : base(transform)
        {
            backGroundImage = transform.Find("Back Ground Image").GetComponent<Image>();

            var rectTransforms = new List<RectTransform>() { rectTransform, content, backGroundImage.rectTransform };

            rectTransforms.AddRange(items);

            initialSettings = new InitialSettings(rectTransforms, graphics);
        }

        #endregion
    }


    public class SlotBase : MenuBase
    {
        #region Field

        public Text labelText { get; }

        #endregion


        #region Constructor

        public SlotBase(Transform transform) : base(transform)
        {
            labelText = content.Find("Label Text").GetComponent<Text>();
        }

        #endregion
    }

    #endregion


    #region Field

    public Selectable[] selectables  { get; protected set; }
    public Canvas       canvas       { get; protected set; }
    public CanvasScaler canvasScaler { get; protected set; }

    protected Selectable[] validSelectables;
    protected Selectable   lastSelectedSelectable;

    [SerializeField] protected float _defaultDuration;

    public float defaultDuration { get { return _defaultDuration; } }

    #endregion


    #region Method

    #region Event

    protected virtual void Awake() { SetField(); }

    //protected virtual void OnEnable()
    //{
    //    if (GameDirector.instance == null) return;

    //    IControlSettingManager controlSetting = GameDirector.instance.menu.setting.control;

    //    controlSetting.connectedUI.current = this;
    //}

    protected virtual void Reset() { ResetField(); }

    protected virtual void Start()
    {
        ISettingMenuManager    settingMenu    = GameDirector.instance.menu.setting;
        IGraphicSettingManager graphicSetting = settingMenu.graphic;
        IControlSettingManager controlSetting = settingMenu.control;

        graphicSetting.connectedUI.Add(this);
        controlSetting.connectedUI.Add(this);
    }

    //protected virtual void OnDisable()
    //{
    //    if (GameDirector.instance == null) return;

    //    IControlSettingManager controlSetting = GameDirector.instance.menu.setting.control;

    //    if (controlSetting.connectedUI.current == (IUIBase)this) controlSetting.connectedUI.current = null;
    //}

    protected virtual void OnDestroy()
    {
        ISettingMenuManager    settingMenu    = GameDirector.instance.menu.setting;
        IGraphicSettingManager graphicSetting = settingMenu.graphic;
        IControlSettingManager controlSetting = settingMenu.control;

        graphicSetting.connectedUI.Remove(this);
        controlSetting.connectedUI.Remove(this);
    }

    #endregion


    #region Initialization

    protected virtual void SetField()
    {
        selectables  = Array.FindAll(GetComponentsInChildren<Selectable>(true),
            selectable => selectable.GetComponentInParent<UIBase>(true) == this);
        canvas       = GetComponentInParent<Canvas>(true);
        canvasScaler = GetComponentInParent<CanvasScaler>(true);
    }

    protected virtual void ResetField() { _defaultDuration = 0.5f; }

    #endregion


    #region Set

    public virtual void Set()
    {
        IGraphicSettingManager graphicSetting = GameDirector.instance.menu.setting.graphic;

        Vector2 canvasResolution = canvasScaler.referenceResolution;
        float   canvasRate       = canvasResolution.x / canvasResolution.y;
        float   screenRate       = 16                 / (float)9;

        switch (graphicSetting.data.resolution)
        {
            case ResolutionType._1080p or ResolutionType._900p or ResolutionType._720p: screenRate = 16 / (float)9; break;
            case ResolutionType._768p  or ResolutionType._480p:                         screenRate = 4  / (float)3; break;
        }

        canvasScaler.matchWidthOrHeight = (screenRate >= canvasRate) ? 1f : 0f;
    }

    public virtual void SetInteractables(bool interacatable)
    {
        IControlSettingManager controlSetting = GameDirector.instance.menu.setting.control;

        if (selectables == null) return;

        if (!interacatable)
        {
            validSelectables = Array.FindAll(selectables, selectable => selectable.interactable);

            var currentSelectable = EventSystem.current.currentSelectedGameObject;

            lastSelectedSelectable = (currentSelectable != null)
                ? validSelectables.FirstOrDefault(selectable => selectable.gameObject == currentSelectable) : null;

            SetCurrent(false);
        }

        foreach (var selectable in validSelectables) selectable.interactable = interacatable;

        if (interacatable)
        {
            if (controlSetting.state == ControlState.Joystick) SelectFirstSelectable();

            SetCurrent(true);
        }
    }

    public virtual void SelectFirstSelectable()
    {
        if (selectables == null) return;

        if (lastSelectedSelectable != null) lastSelectedSelectable.Select();
        else
        {
            var first = selectables.FirstOrDefault(selectable => selectable.interactable);

            if (first != null) first.Select();
        }
    }

    protected virtual void SetCurrent(bool isActive)
    {
        IControlSettingManager controlSetting = GameDirector.instance.menu.setting.control;

        var connectedUI = controlSetting.connectedUI;

        if      (isActive)                             connectedUI.current = this;
        else if (connectedUI.current == (IUIBase)this) connectedUI.current = null;
    }

    #endregion


    #region Display

    public virtual Coroutine Display(bool isActive, bool animated = true)
    {
        if (isActive)
        {
            gameObject.SetActive(true);
            Set();
        }

        SetInteractables(false);

        return StartCoroutine(_Display(isActive, animated ? defaultDuration : 0f));
    }

    protected virtual IEnumerator _Display(bool isActive, float duration)
    {
        SetInteractables(true);

        if (!isActive) gameObject.SetActive(false);

        yield break;
    }


    #region Base

    protected virtual IEnumerator FadeContent(MenuBase menu, bool isFadeIn, float duration)
    {
        RectTransform[] items = menu.items;

        if (isFadeIn) menu.gameObject.SetActive(true);

        StartCoroutine(FadeGraphics(menu, isFadeIn, duration));

        for (int i = 0; i < items.Length; i++)
        {
            if (i != items.Length - 1) StartCoroutine(FadeItem(items[i], menu.initialSettings, isFadeIn, duration));
            else                       yield return FadeItem(items[i], menu.initialSettings, isFadeIn, duration);
        }

        if (!isFadeIn) menu.gameObject.SetActive(false);
    }

    protected virtual IEnumerator FadeGraphics(MenuBase menu, bool isFadeIn, float duration)
    {
        IAnimationCurvePreset curvePreset = GameDirector.instance.curvePreset;

        MaskableGraphic[] graphics = menu.graphics;

        Color[] originColors      = new Color[graphics.Length];
        Color[] transparentColors = new Color[graphics.Length];
        Color[] startColors       = new Color[graphics.Length];
        Color[] endColors         = new Color[graphics.Length];

        for (int i = 0; i < graphics.Length; i++)
        {
            originColors[i]      = menu.initialSettings.graphics[graphics[i]];
            transparentColors[i] = Vector4.Scale(originColors[i], new Vector4(1f, 1f, 1f, 0f));
            startColors[i]       = isFadeIn ? transparentColors[i] : originColors[i];
            endColors[i]         = isFadeIn ? originColors[i]      : transparentColors[i];
        }

        float elapsedTime = 0f;
        var   curveType   = curvePreset.types[isFadeIn ? 1 : 2];

        while (elapsedTime < duration)
        {
            float rate = curveType.Evaluate(elapsedTime / duration);

            for (int i = 0; i < graphics.Length; i++)
                graphics[i].color = Color.Lerp(startColors[i], endColors[i], rate);

            elapsedTime += Time.unscaledDeltaTime;
            yield return null;
        }

        for (int i = 0; i < graphics.Length; i++) graphics[i].color = endColors[i];
    }

    protected virtual IEnumerator FadeItem(RectTransform item, InitialSettings initialSettings,
        bool isFadeIn, float duration)
    {
        IAnimationCurvePreset curvePreset = GameDirector.instance.curvePreset;

        Vector2 originPosition      = initialSettings.rectTransforms[item].anchoredPosition;
        Vector2 transparentPosition = -originPosition;
        Vector2 startPosition       = isFadeIn ? transparentPosition : originPosition;
        Vector2 endPosition         = isFadeIn ? originPosition      : transparentPosition;
        float   elapsedTime         = 0f;
        var     curveType           = curvePreset.types[isFadeIn ? 1 : 2];

        while (elapsedTime < duration)
        {
            float rate = curveType.Evaluate(elapsedTime / duration);

            item.anchoredPosition = Vector2.Lerp(startPosition, endPosition, rate);

            elapsedTime += Time.unscaledDeltaTime;
            yield return null;
        }

        item.anchoredPosition = endPosition;
    }

    #endregion


    #region Window

    protected virtual IEnumerator FadeBackGroundImage(WindowBase window, bool isFadeIn, float duration)
    {
        Image image = window.backGroundImage;

        Color originColor      = window.initialSettings.graphics[image];
        Color transparentColor = Vector4.Scale(originColor, new Vector4(1f, 1f, 1f, 0f));
        Color startColor       = isFadeIn ? transparentColor : originColor;
        Color endColor         = isFadeIn ? originColor      : transparentColor;
        float elapsedTime      = 0f;

        while (elapsedTime < duration)
        {
            float rate = elapsedTime / duration;

            image.color = Color.Lerp(startColor, endColor, rate);

            elapsedTime += Time.unscaledDeltaTime;
            yield return null;
        }

        image.color = endColor;
    }

    protected virtual IEnumerator FadeWindow(WindowBase window, bool isFadeIn, float duration)
    {
        IAnimationCurvePreset curvePreset = GameDirector.instance.curvePreset;

        RectTransform rectTransform = window.rectTransform;
        RectTransform content       = window.content;

        if (isFadeIn) window.gameObject.SetActive(true);

        content.gameObject.SetActive(false);

        Vector2 originSize      = window.initialSettings.rectTransforms[rectTransform].sizeDelta;
        Vector2 transparentSize = Vector2.Scale(originSize, Vector2.right);
        Vector2 startSize       = isFadeIn ? transparentSize : originSize;
        Vector2 endSize         = isFadeIn ? originSize      : transparentSize;
        float   elapsedTime     = 0f;
        var curveType = curvePreset.types[1];

        while (elapsedTime < duration)
        {
            float rate = curveType.Evaluate(elapsedTime / duration);

            rectTransform.sizeDelta = Vector2.Lerp(startSize, endSize, rate);

            elapsedTime += Time.unscaledDeltaTime;
            yield return null;
        }

        rectTransform.sizeDelta = endSize;

        if (isFadeIn) content.gameObject.SetActive(true);
        else          window.gameObject.SetActive(false);
    }

    #endregion


    #region List

    protected virtual IEnumerator FadeSlots(IEnumerable<SlotBase> slots, bool isFadeIn, float slotDuration,
        float interval, FadeDirection fadeDirection)
    {
        if (isFadeIn)
            foreach (var slot in slots) slot.gameObject.SetActive(false);

        foreach (var slot in slots)
        {
            if (slot != slots.Last())
            {
                StartCoroutine(FadeSlot(slot, isFadeIn, slotDuration, fadeDirection));

                yield return new WaitForSecondsRealtime(interval);
            }
            else yield return FadeSlot(slot, isFadeIn, slotDuration, fadeDirection);
        }

        if (!isFadeIn)
            foreach (var slot in slots) slot.gameObject.SetActive(false);
    }

    protected virtual IEnumerator FadeSlot(SlotBase slot, bool isFadeIn, float duration,
        FadeDirection fadeDirection)
    {
        IAnimationCurvePreset curvePreset = GameDirector.instance.curvePreset;

        RectTransform content = slot.content;

        //if (isFadeIn) slot.gameObject.SetActive(true);
        slot.gameObject.SetActive(true);

        StartCoroutine(FadeGraphics(slot, isFadeIn, duration));

        Vector2 originPosition      = slot.initialSettings.rectTransforms[content].anchoredPosition;
        Vector2 transparentPosition = originPosition + GetTransparentAmount(slot, fadeDirection);
        Vector2 startPosition       = isFadeIn ? transparentPosition : originPosition;
        Vector2 endPosition         = isFadeIn ? originPosition      : transparentPosition;
        float   elapsedTime         = 0f;
        var     curveType           = curvePreset.types[isFadeIn ? 1 : 2];

        while (elapsedTime < duration)
        {
            float rate = curveType.Evaluate(elapsedTime / duration);

            content.anchoredPosition = Vector2.Lerp(startPosition, endPosition, rate);

            elapsedTime += Time.unscaledDeltaTime;
            yield return null;
        }

        content.anchoredPosition = endPosition;

        //if (!isFadeIn) slot.gameObject.SetActive(false);
    }

    private Vector2 GetTransparentAmount(SlotBase slot, FadeDirection fadeDirection)
    {
        Vector2 size   = slot.rectTransform.sizeDelta;
        float   width  = size.x;
        float   height = size.y;

        switch (fadeDirection)
        {
            case FadeDirection.Up:    return Vector2.down  * (height / 2f);
            case FadeDirection.Down:  return Vector2.up    * (height / 2f);
            case FadeDirection.Right: return Vector2.left  * (width  / 2f);
            case FadeDirection.Left:  return Vector2.right * (width  / 2f);
            default:                  return Vector2.zero;
        }
    }

    #endregion

    #endregion

    #endregion
}
