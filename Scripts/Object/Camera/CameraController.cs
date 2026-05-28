// //////////////////////////////////////////////////////////////////////////////
// * 요약
//    - 카메라 타겟을 통해 카메라 배치(Set)
//    - 카메라 이동, 회전, 흔들기 등의 액션
//
// * 목차
//    1. 인터페이스 ... Line 
//    2. 클래스 ....... Line 
//        1) 필드 ..... Line 
//        2) 메서드 ... Line 
//            1- 이벤트 함수 ... Line 
//            2- 초기화 ........ Line 
// //////////////////////////////////////////////////////////////////////////////
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Game;

using ShakeSetting = CameraController.ShakeSetting;
using ShakeType    = CameraController.ShakeSetting.Type;

// //////////////////////////////////////////////////////////////////////////////
// 1. 인터페이스
// //////////////////////////////////////////////////////////////////////////////
public interface ICameraController
{
    // 프로퍼티
    // Component
    GameObject gameObject { get; }
    Transform  transform  { get; }
    Camera     inner      { get; }

    // Reference
    ISceneBase scene { get; }    // 각 씬을 통해 카메라 참조

    // Setting
    float                               defaultDuration { get; }
    float                               defaultFOV      { get; }
    SimpleData<ShakeType, ShakeSetting> shakeSettings   { get; }

    // 메서드
    void      Initialize();
    Coroutine Set(ICameraTargetController target, float duration = -1f);

    // Action
    Coroutine Shake(ShakeType type);
    Coroutine Zoom(float magnification, float duration = -1f);
    Coroutine LookAt(Transform target);
    void      StopLookAt();
    Coroutine Follow(ICameraTargetController target);
    void      StopFollow();
}

// //////////////////////////////////////////////////////////////////////////////
// 2. 클래스
// //////////////////////////////////////////////////////////////////////////////
public class CameraController : MonoBehaviour, ICameraController
{
    // ==============================================================================
    // 1) 내부 타입
    // ==============================================================================
    [Serializable] public class ShakeSetting
    {
        // 내부 타입
        public enum Type { Soft, Medium, Hard }

        // 필드
        [SerializeField] protected float _duration;
        [SerializeField] protected float _amount;

        public float duration { get { return _duration; } }
        public float amount   { get { return _amount; } }

        // 생성자
        public ShakeSetting(float duration, float amount)
        {
            _duration = duration;
            _amount   = amount;
        }
    }

    // ==============================================================================
    // 2) 필드
    // ==============================================================================
    // Component & Reference
    public Camera     inner { get; protected set; }
    public ISceneBase scene { get; protected set; }

    // Setting
    [SerializeField] protected float                               _defaultDuration;
    [SerializeField] protected float                               _defaultFOV;
    [SerializeField] protected SimpleData<ShakeType, ShakeSetting> _shakeSettings;

    public float                               defaultDuration { get { return _defaultDuration; } }
    public float                               defaultFOV      { get { return _defaultFOV; } }
    public SimpleData<ShakeType, ShakeSetting> shakeSettings   { get { return _shakeSettings; } }

    // etc.
    protected Coroutine setAction;
    protected Coroutine shakeAction;
    protected Coroutine zoomAction;
    protected Coroutine lookAtAction;
    protected Coroutine followAction;

    // ==============================================================================
    // 3) 메서드
    // ==============================================================================
    // ------------------------------------------------------------------------------
    // 3-1) 메서드 -> 이벤트 함수
    //    - 카메라 초기화
    // ------------------------------------------------------------------------------
    protected virtual void Awake() { SetField(); }

    protected virtual void Reset() { ResetField(); }

    protected virtual void OnDisable() { Initialize(); }

    // ------------------------------------------------------------------------------
    // 3-2) 메서드 -> 초기화
    // ------------------------------------------------------------------------------
    protected virtual void SetField()
    {
        inner = GetComponentInChildren<Camera>(true);
        scene = FindObjectOfType<SceneBase>(true);
    }

    protected virtual void ResetField()
    {
        _defaultDuration = 1f;
        _defaultFOV      = 60f;
        _shakeSettings   = new SimpleData<ShakeType, ShakeSetting>(
            new List<SimpleData<ShakeType, ShakeSetting>.Element>()
            {
                    new SimpleData<ShakeType, ShakeSetting>.Element(ShakeType.Soft,   new ShakeSetting(0.25f, 0.5f)),
                    new SimpleData<ShakeType, ShakeSetting>.Element(ShakeType.Medium, new ShakeSetting(0.5f,  0.5f)),
                    new SimpleData<ShakeType, ShakeSetting>.Element(ShakeType.Hard,   new ShakeSetting(0.75f, 1f))
            });
    }

    public virtual void Initialize()
    {
        inner.transform.localPosition = Vector3.zero;
        inner.transform.localRotation = Quaternion.identity;
        inner.fieldOfView             = defaultFOV;
    }

    // ------------------------------------------------------------------------------
    // 3-3) 메서드 -> 셋(Set)
    //    - 카메라 타겟을 통한 배치
    //    - 지정된 시간동안 이동
    // ------------------------------------------------------------------------------
    public virtual Coroutine Set(ICameraTargetController target, float duration = -1f)
    {
        if (setAction != null) 
        { 
            StopCoroutine(setAction);

            setAction = null; 
        }

        duration = (duration < 0f) ? defaultDuration : duration;

        return setAction = StartCoroutine(_Set(target, duration, Time.timeScale == 0f));
    }

    protected virtual IEnumerator _Set(ICameraTargetController target, float duration, bool useUnscaledTime)
    {
        IAnimationCurvePreset curvePreset = GameDirector.instance.curvePreset;

        Vector3    startPosition = transform.position;
        Quaternion startRotation = transform.rotation;
        float      elapsedTime   = 0f;
        var        curveType     = curvePreset.types[0];

        while (elapsedTime < duration)
        {
            Vector3    endPosition = target.endPoint.position;
            Quaternion endRotation = target.endPoint.rotation;
            float      rate        = curveType.Evaluate(elapsedTime / duration);

            transform.position = Vector3.Lerp(startPosition, endPosition, rate);
            transform.rotation = Quaternion.Slerp(startRotation, endRotation, rate);

            elapsedTime += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            yield return null;
        }

        transform.position = target.endPoint.position;
        transform.rotation = target.endPoint.rotation;
        setAction          = null;
    }

    // ------------------------------------------------------------------------------
    // 3-4) 메서드 -> 액션
    // ------------------------------------------------------------------------------
    // ******************************************************************************
    // 3-4-1) 메서드 -> 액션 -> 흔들기(Shake)
    //    - 플레이어 피격 등의 특정 이벤트 발생 시 카메라(내부)를 위-아래로 흔듦
    // ******************************************************************************
    public virtual Coroutine Shake(ShakeType type)
    {
        if (shakeAction != null)
        {
            StopCoroutine(shakeAction);
            //Gamepad.current.SetMotorSpeeds(0f, 0f);

            shakeAction = null;
        }

        return shakeAction = StartCoroutine(_Shake(shakeSettings[type], Time.timeScale == 0f));
    }

    protected virtual IEnumerator _Shake(ShakeSetting setting, bool useUnscaledTime)
    {
        IAnimationCurvePreset curvePreset = GameDirector.instance.curvePreset;

        Transform camera = inner.transform;

        float duration    = setting.duration;
        float elapsedTime = 0f;
        var   curveType   = curvePreset.types[3];

        while (elapsedTime < duration)
        {
            float timeRate     = elapsedTime / duration;
            float rate         = curveType.Evaluate(timeRate);
            float rumbleAmount = Mathf.Lerp(setting.amount, 0f, timeRate);
            
            camera.localPosition = Vector3.up * setting.amount * rate;

            elapsedTime += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            yield return null;
        }
        
        camera.localPosition = Vector3.zero;
        shakeAction          = null;
    }

    // ******************************************************************************
    // 3-4-2) 메서드 -> 액션 -> 줌(Zoom)
    //    - 카메라 시야각 조절
    // ******************************************************************************
    public virtual Coroutine Zoom(float magnification, float duration = -1f)
    {
        if (zoomAction != null)
        {
            StopCoroutine(zoomAction);

            zoomAction = null;
        }

        duration = (duration < 0f) ? defaultDuration : duration;

        return zoomAction = StartCoroutine(_Zoom(magnification, duration, Time.timeScale == 0f));
    }

    protected virtual IEnumerator _Zoom(float magnification, float duration, bool useUnscaledTime)
    {
        IAnimationCurvePreset curvePreset = GameDirector.instance.curvePreset;

        float startFOV    = inner.fieldOfView;
        float endFOV      = defaultFOV * (1f / magnification);
        float elapsedTime = 0f;
        var   curveType   = curvePreset.types[1];

        while (elapsedTime < duration)
        {
            float rate = curveType.Evaluate(elapsedTime / duration);

            inner.fieldOfView = Mathf.Lerp(startFOV, endFOV, rate);

            elapsedTime += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            yield return null;
        }

        inner.fieldOfView = endFOV;
        zoomAction        = null;
    }

    // ******************************************************************************
    // 3-4-3) 메서드 -> 액션 -> 바라보기(Look At)
    //    - 특정 오브젝트가 중앙에 오도록 카메라 회전
    // ******************************************************************************
    public virtual Coroutine LookAt(Transform target)
    {
        StopLookAt();

        return lookAtAction = StartCoroutine(_LookAt(target));
    }

    protected virtual IEnumerator _LookAt(Transform target)
    {
        Vector3 worldUp = transform.up;

        while (true)
        {
            transform.LookAt(target, worldUp);

            yield return null;
        }
    }

    public virtual void StopLookAt() 
    {
        if (lookAtAction != null)
        {
            StopCoroutine(lookAtAction);

            lookAtAction = null;
        }
    }

    // ******************************************************************************
    // 3-4-4) 메서드 -> 액션 -> 따라가기(Follow)
    //    - 카메라 타겟을 계속해서 따라감
    // ******************************************************************************
    public virtual Coroutine Follow(ICameraTargetController target)
    {
        StopFollow();

        return followAction = StartCoroutine(_Follow(target));
    }

    protected virtual IEnumerator _Follow(ICameraTargetController target)
    {
        while (true)
        {
            transform.position = target.endPoint.position;
            transform.rotation = target.endPoint.rotation;

            yield return null;
            //yield return new WaitForEndOfFrame();
            //yield return new WaitForFixedUpdate();
        }
    }

    public virtual void StopFollow()
    { 
        if (followAction != null)
        {
            StopCoroutine(followAction);

            followAction = null;
        }
    }
}
