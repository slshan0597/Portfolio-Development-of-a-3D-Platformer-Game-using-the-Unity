// //////////////////////////////////////////////////////////////////////////////
// * 요약
//    - 레벨 내 필드에 대한 클래스, 하나의 레벨 내에 다수의 필드가 존재할 수 있음
//    - 커스텀 중력(Gravityable) 속성을 가진 오브젝트에게 위치에 따라 고유의 중력을 생성
//
// * 목차
//    1. 인터페이스 ... Line 27
//    2. 클래스 ....... Line 55
//        1) 정의 ..... Line 60
//        2) 필드 ..... Line 122
//        3) 메서드 ... Line 143
//            1- 이벤트 함수 ..... Line 146
//            2- 초기화 .......... Line 184
//            3- 중력(Gravity) ... Line 226
// //////////////////////////////////////////////////////////////////////////////
using System;
using System.Collections.Generic;
using UnityEngine;

using Setting      = PlanetController.Setting;
using GravityType  = PlanetController.Setting.Gravity.Type;
using CoreType     = PlanetController.Setting.Core.Type;
using CoreLocation = PlanetController.Setting.Core.Location;
using CoreLineAxis = PlanetController.Setting.Core.LineAxis;

// //////////////////////////////////////////////////////////////////////////////
// 1. 인터페이스
// //////////////////////////////////////////////////////////////////////////////
public interface IPlanetController
{
    // 프로퍼티
    // Component
    Transform                  transform       { get; }
    Collider                   area            { get; }
    Transform                  core            { get; }
    IModelController           ground          { get; }
    ICharacterTargetController characterTarget { get; }
    Transform                  objects         { get; }
    List<IModelController>     models          { get; }
    List<ICharacterBase>       characters      { get; }

    // Reference
    ISceneBase scene { get; }

    // Setting
    bool    enabled { get; }
    Setting setting { get; }

    // 메서드
    void    SetEnable(bool enabled, IPlayerController player);
    Vector3 GetGravity(IGravityable target);
}

// //////////////////////////////////////////////////////////////////////////////
// 2. 클래스
// //////////////////////////////////////////////////////////////////////////////
public class PlanetController : MonoBehaviour, IPlanetController
{
    // ==============================================================================
    // 1) 정의
    //    - 중력에 대한 설정값
    //    - 코어(Core) 타입일 경우에 대한 프로퍼티
    // ==============================================================================
    [Serializable] public class Setting
    {
        [Serializable] public struct Gravity
        {
            public enum Type { None, Core }

            [SerializeField] private Type    _type;
            [SerializeField] private Vector3 _defaultDirection;
            [SerializeField] private float   _force;

            public Type    type             { get { return _type; } }
            public Vector3 defaultDirection { get { return _defaultDirection; } }
            public float   force            { get { return _force; } }

            public Gravity(Type type, Vector3 defaultDirection, float force)
            {
                _type             = type;
                _defaultDirection = defaultDirection;
                _force            = force;
            }
        }

        [Serializable] public struct Core
        {
            public enum Type     { None, Point, Line }
            public enum Location { None, Inside, Outside }
            public enum LineAxis { None, X_Axis, Y_Axis, Z_Axis }

            [SerializeField] private Type     _type;
            [SerializeField] private Location _location;
            [SerializeField] private LineAxis _lineAxis;

            public Type     type     { get { return _type; } }
            public Location location { get { return _location; } }
            public LineAxis lineAxis { get { return _lineAxis; } }

            public Core(Type type, Location location, LineAxis lineAxis)
            {
                _type     = type;
                _location = location;
                _lineAxis = lineAxis;
            }
        }

        [SerializeField] protected Gravity _gravity;
        [SerializeField] protected Core    _core;

        public Gravity gravity { get { return _gravity; } }
        public Core    core    { get { return _core; } }

        public Setting(Gravity gravity, Core core)
        {
            _gravity = gravity;
            _core    = core;
        }
    }

    // ==============================================================================
    // 2) 필드
    // ==============================================================================
    // Component & Reference
    public Collider                   area            { get; protected set; }
    public Transform                  core            { get; protected set; }
    public IModelController           ground          { get; protected set; }
    public ICharacterTargetController characterTarget { get; protected set; }
    public Transform                  objects         { get; protected set; }
    public List<ICharacterBase>       characters      { get; protected set; }
    public List<IModelController>     models          { get; protected set; }
    public ISceneBase                 scene           { get; protected set; }

    // Setting
    [SerializeField] protected Setting _setting;

    public Setting setting { get { return _setting; } }

    // etc.
    protected bool playerIsKinematic = false;

    // ==============================================================================
    // 3) 메서드
    // ==============================================================================
    // ------------------------------------------------------------------------------
    // 3-1) 메서드 -> 이벤트 함수
    //    - 오브젝트 초기화
    //    - 필드 내에 중력의 영향을 받는 오브젝트에 중력을 부여
    //    - 플레이어가 필드 내에 있으면 활성화
    // ------------------------------------------------------------------------------
    protected virtual void Awake() { SetField(); }

    protected virtual void Reset() { ResetField(); }

    protected virtual void OnTriggerEnter(Collider other)
    {
        if (other.isTrigger || !other.TryGetComponent(out IPlayerController player)) return;
        if (enabled)                                                                 return;

        SetEnable(true, player);
    }

    protected virtual void OnTriggerStay(Collider other)
    {
        if (other.isTrigger || !other.TryGetComponent(out IGravityable target)) return;

        target.TryApplyGravity(this);
    }

    protected virtual void OnTriggerExit(Collider other)
    {
        if (other.isTrigger || !other.TryGetComponent(out IPlayerController player)) return;
        if (playerIsKinematic != player.rigidbody.isKinematic)
        {
            playerIsKinematic = player.rigidbody.isKinematic;

            return;
        }

        SetEnable(false, player);
    }

    // ------------------------------------------------------------------------------
    // 3-2) 메서드 -> 초기화
    //    - 필드(컴포넌트 등) 초기화
    // ------------------------------------------------------------------------------
    protected virtual void SetField()
    {
        area       = GetComponent<Collider>();
        core       = transform.Find("Core");
        ground     = transform.Find("Ground").GetComponent<IModelController>();
        objects    = transform.Find("Objects");
        models     = new List<IModelController>();
        characters = new List<ICharacterBase>();
        scene      = FindObjectOfType<SceneBase>(true);

        var _characterTarget = (transform.Find("Character Target") != null) ? transform.Find("Character Target") 
                                                                            : transform.Find("Character Targets");

        characterTarget = _characterTarget.GetComponentInChildren<ICharacterTargetController>(true);

        enabled = false;
    }

    protected virtual void ResetField()
    {
        _setting = new Setting(
            new Setting.Gravity(GravityType.Core, new Vector3(0f, -1f, 0f), 50f),
            new Setting.Core(CoreType.Point, CoreLocation.Inside, CoreLineAxis.None));
    }

    public virtual void SetEnable(bool enabled, IPlayerController player)
    {
        this.enabled      = enabled;
        player.planet     = enabled ? this : null;
        playerIsKinematic = player.rigidbody.isKinematic;

        foreach (var model in models) model.CastShadow(enabled);

        if (enabled)
            foreach (var character in characters)
                if (character.gameObject.activeInHierarchy) character.Idle();
    }

    // ------------------------------------------------------------------------------
    // 3-3) 메서드 -> 중력(Gravity)
    //    - 오브젝트와 코어 사이의 지면에 대한 법선을 기준으로 중력 생성
    //    - 코어 타입이 Line일 경우 코어에서 오브젝트 사이의 벡터를 지정된 축에 투영한 위치를 대체 코어(위치)로 지정
    //    - 방향 타입에 따라 중력의 방향을 반전
    // ------------------------------------------------------------------------------
    public virtual Vector3 GetGravity(IGravityable target)
    {
        var     gravitySetting = setting.gravity;
        Vector3 direction      = gravitySetting.defaultDirection;

        switch (gravitySetting.type)
        {
            case GravityType.Core: direction = GetCoreDirection(target); break;
        }

        return direction * gravitySetting.force;
    }

    protected virtual Vector3 GetCoreDirection(IGravityable target)
    {
        var coreSetting = setting.core;

        Vector3 targetPosition = target.transform.position;
        Vector3 corePosition   = (coreSetting.type     == CoreType.Point)      ? core.position  : GetCorePosition(targetPosition);
        Vector3 origin         = (coreSetting.location == CoreLocation.Inside) ? targetPosition : corePosition;
        Vector3 direction      = (coreSetting.location == CoreLocation.Inside) ? (corePosition - targetPosition).normalized
                                                                               : (targetPosition - corePosition).normalized;
        Ray     ray            = new Ray(origin, direction);
        float   radius         = target.radius * (core.localScale.x / ground.transform.localScale.x);
        int     layerMask      = 1 << LayerMask.NameToLayer("Planet Core");

        if (Physics.SphereCast(ray, radius, out RaycastHit hit, float.MaxValue, layerMask)) return -hit.normal;
        else                                                                                return setting.gravity.defaultDirection;
    }

    // 코어 타입이 Line일 경우 축에 투영된 위치를 대체 코어로 반환
    protected virtual Vector3 GetCorePosition(Vector3 targetPosition)
    {
        Vector3 displacement     = targetPosition - core.position;
        Vector3 axisDisplacement = Vector3.zero;

        switch (setting.core.lineAxis)
        {
            case CoreLineAxis.X_Axis: axisDisplacement = Vector3.right   * displacement.x; break;
            case CoreLineAxis.Y_Axis: axisDisplacement = Vector3.up      * displacement.y; break;
            case CoreLineAxis.Z_Axis: axisDisplacement = Vector3.forward * displacement.z; break;
        }

        return transform.TransformPoint(axisDisplacement);
    }
}
