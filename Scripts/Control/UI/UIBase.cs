// //////////////////////////////////////////////////////////////////////////////
// * 요약
//    - 게임 상의 모든 UI에 대한 기반 클래스
//    - 창(Window)이나 메뉴 등 컨테이너 정의
//    - UI의 표시(Display)와 내용(Content) 수정
//    - 버튼 등 Selectable 오브젝트의 설정
//
// * 목차
//    1. 인터페이스 ... Line 40
//    2. 클래스 ....... Line 64
//        1) 내부 타입 ... Line 70
//            1- 메뉴 ......... Line 75
//            2- 창(Window) ... Line 146
//            3- 슬롯(Slot) ... Line 169
//        2) 필드 ..... Line 187
//        3) 메서드 ... Line 204
//            1- 이벤트 함수 ..... Line 208
//            2- 초기화 .......... Line 237
//            3- 셋(Set) ......... Line 251
//            4- 표시(Display) ... Line 325
//                1_ 메뉴 ......... Line 351
//                2_ 창(Window) ... Line 435
//                3_ 슬롯(Slot) ... Line 498
// //////////////////////////////////////////////////////////////////////////////
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

// //////////////////////////////////////////////////////////////////////////////
// 1. 인터페이스
// //////////////////////////////////////////////////////////////////////////////
public interface IUIBase
{
    // 프로퍼티
    // Component
    GameObject   gameObject  { get; }
    Selectable[] selectables { get; }

    // Reference
    Canvas       canvas       { get; }
    CanvasScaler canvasScaler { get; }

    // Setting
    float defaultDuration { get; }

    // 메서드
    void      Set();
    void      SetInteractables(bool interactable);
    void      SelectFirstSelectable();
    Coroutine Display(bool isActive, bool animated = true);
}

// //////////////////////////////////////////////////////////////////////////////
// 2. 클래스
//    - 클래스 확장을 위한 기반 기능만을 구현
// //////////////////////////////////////////////////////////////////////////////
public class UIBase : MonoBehaviour, IUIBase
{
    // ==============================================================================
    // 1) 내부 타입
    // ==============================================================================
    public enum FadeDirection { None, Up, Down, Left, Right }

    // ------------------------------------------------------------------------------
    // 1-1) 내부 타입 -> 메뉴
    //    - 대부분의 컨테이너의 기반이 되는 클래스
    //    - 컨테이너의 크기와 그래픽에 대한 정의
    // ------------------------------------------------------------------------------
    public class MenuBase
    {
        // 내부 타입
        public class InitialSettings
        {
            public class RectTransformSetting
            {
                public Vector2 anchoredPosition   { get; }
                public Vector3 anchoredPosition3D { get; }
                public Vector2 sizeDelta          { get; }
                public Vector3 localScale         { get; }

                public RectTransformSetting(RectTransform rectTransform)
                {
                    anchoredPosition   = rectTransform.anchoredPosition;
                    anchoredPosition3D = rectTransform.anchoredPosition3D;
                    sizeDelta          = rectTransform.sizeDelta;
                    localScale         = rectTransform.localScale;
                }
            }

            public Dictionary<RectTransform, RectTransformSetting> rectTransforms { get; }
            public Dictionary<MaskableGraphic, Color>              graphics       { get; }

            public InitialSettings(IEnumerable<RectTransform> rectTransforms, IEnumerable<MaskableGraphic> graphics)
            {
                this.rectTransforms = new Dictionary<RectTransform, RectTransformSetting>();
                this.graphics       = new Dictionary<MaskableGraphic, Color>();

                foreach (var rectTransform in rectTransforms)
                    this.rectTransforms.Add(rectTransform, new RectTransformSetting(rectTransform));
                foreach (var graphic in graphics)
                    this.graphics.Add(graphic, graphic.color);
            }
        }

        // 필드
        public Transform         transform     { get; }
        public GameObject        gameObject    { get; }
        public RectTransform     rectTransform { get; }
        public RectTransform     content       { get; }
        public RectTransform[]   items         { get; }
        public MaskableGraphic[] graphics      { get; }

        public InitialSettings initialSettings { get; protected set; }

        // 메서드
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
    }

    // ------------------------------------------------------------------------------
    // 1-2) 내부 타입 -> 창(Window)
    //    - 메뉴(컨테이너)에서 확장된 클래스
    //    - 배경 이미지 추가
    // ------------------------------------------------------------------------------
    public class WindowBase : MenuBase
    {
        // 필드
        public Image backGroundImage { get; }

        // 메서드
        public WindowBase(Transform transform) : base(transform)
        {
            backGroundImage = transform.Find("Back Ground Image").GetComponent<Image>();

            var rectTransforms = new List<RectTransform>() { rectTransform, content, backGroundImage.rectTransform };

            rectTransforms.AddRange(items);

            initialSettings = new InitialSettings(rectTransforms, graphics);
        }
    }

    // ------------------------------------------------------------------------------
    // 1-3) 내부 타입 -> 슬롯(Slot)
    //    - 메뉴(컨테이너)에서 확장된 클래스
    //    - 이름 텍스트 추가
    //    - 게임의 설정(그래픽 등) 항목 등 연속적인 컨테이너가 필요할 때 사용
    // ------------------------------------------------------------------------------
    public class SlotBase : MenuBase
    {
        // 필드
        public Text labelText { get; }

        // 메서드
        public SlotBase(Transform transform) : base(transform)
        {
            labelText = content.Find("Label Text").GetComponent<Text>();
        }
    }

    // ==============================================================================
    // 2) 필드
    // ==============================================================================
    // Component & Reference
    public Selectable[] selectables  { get; protected set; }
    public Canvas       canvas       { get; protected set; }
    public CanvasScaler canvasScaler { get; protected set; }

    // Selectables
    protected Selectable[] validSelectables;
    protected Selectable   lastSelectedSelectable;

    // Setting
    [SerializeField] protected float _defaultDuration;

    public float defaultDuration { get { return _defaultDuration; } }

    // ==============================================================================
    // 3) 메서드
    //    - 클래스 확장을 위한 기반 기능만을 구현
    // ==============================================================================
    // ------------------------------------------------------------------------------
    // 3-1) 메서드 -> 이벤트 함수
    //    - 오브젝트 초기화
    //    - 한 번이라도 활성화 시 세팅 클래스 내 현재 활성화된 UI 리스트에 연결
    // ------------------------------------------------------------------------------
    protected virtual void Awake() { SetField(); }

    protected virtual void Reset() { ResetField(); }

    protected virtual void Start()
    {
        ISettingMenuManager    settingMenu    = GameDirector.instance.menu.setting;
        IGraphicSettingManager graphicSetting = settingMenu.graphic;
        IControlSettingManager controlSetting = settingMenu.control;

        graphicSetting.connectedUI.Add(this);
        controlSetting.connectedUI.Add(this);
    }

    protected virtual void OnDestroy()
    {
        ISettingMenuManager    settingMenu    = GameDirector.instance.menu.setting;
        IGraphicSettingManager graphicSetting = settingMenu.graphic;
        IControlSettingManager controlSetting = settingMenu.control;

        graphicSetting.connectedUI.Remove(this);
        controlSetting.connectedUI.Remove(this);
    }

    // ------------------------------------------------------------------------------
    // 3-2) 메서드 -> 초기화
    //    - 필드(컴포넌트 등) 초기화
    // ------------------------------------------------------------------------------
    protected virtual void SetField()
    {
        selectables  = Array.FindAll(GetComponentsInChildren<Selectable>(true),
            selectable => selectable.GetComponentInParent<UIBase>(true) == this);
        canvas       = GetComponentInParent<Canvas>(true);
        canvasScaler = GetComponentInParent<CanvasScaler>(true);
    }

    protected virtual void ResetField() { _defaultDuration = 0.5f; }

    // ------------------------------------------------------------------------------
    // 3-3) 메서드 -> 셋(Set)
    //    - 그래픽(해상도)에 따른 컨테이너 크기 조절
    //    - UI 내 상호작용 가능한 모든 오브젝트 활성화
    //    - 입력 타입이 Joystick일 경우(포인터가 없는 경우), 활성화 시 첫 Selectable 설정
    // ------------------------------------------------------------------------------
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
            if (controlSetting.state == ControlState.Joystick) SelectFirstSelectable();    // 입력 타입이 Joystick일 경우에만 Select 기능 수행

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

    // ------------------------------------------------------------------------------
    // 3-4) 메서드 -> 표시(Display)
    //    - UI의 활성화 및 비활성화
    // ------------------------------------------------------------------------------
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

    // ******************************************************************************
    // 3-4-1) 메서드 -> 표시 -> 메뉴
    //    - 메뉴의 모든 오브젝트에 대한 페이드 인-아웃 기능
    //    - 투명-불투명 간의 그라데이션 효과
    //    - 메뉴의 가운데-외곽 사이로 아이템을 이동하여 배치
    // ******************************************************************************
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

    // ******************************************************************************
    // 3-4-2) 메서드 -> 표시 -> 창(Window)
    //    - 창의 모든 오브젝트에 대한 페이드 인-아웃 기능
    //    - 배경 이미지의 투명-불투명 그라데이션 효과
    //    - 창 사이즈의 높이를 조절하여 배치(두루마리)
    // ******************************************************************************
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

    // ******************************************************************************
    // 3-4-3) 메서드 -> 표시 -> 슬롯(Slot)
    //    - 슬롯 리스트에 대한 페이드 인-아웃 기능
    //    - 슬롯마다 간격(인터벌)을 두고 좌-우로 하나씩 이동하여 배치
    // ******************************************************************************
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
}
