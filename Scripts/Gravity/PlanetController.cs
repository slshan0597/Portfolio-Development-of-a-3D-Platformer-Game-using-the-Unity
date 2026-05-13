using System;
using System.Collections.Generic;
using UnityEngine;

using Setting      = PlanetController.Setting;
using GravityType  = PlanetController.Setting.Gravity.Type;
using CoreType     = PlanetController.Setting.Core.Type;
using CoreLocation = PlanetController.Setting.Core.Location;
using CoreLineAxis = PlanetController.Setting.Core.LineAxis;


public interface IPlanetController
{
    #region Property

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

    #endregion


    #region Method

    void    SetEnable(bool enabled, IPlayerController player);
    Vector3 GetGravity(IGravityable target);

    #endregion
}


public class PlanetController : MonoBehaviour, IPlanetController
{
    #region Definition

    [Serializable] public class Setting
    {
        #region Definition

        [Serializable] public struct Gravity
        {
            #region Definition

            public enum Type { None, Core, Normal }

            #endregion


            #region Field

            [SerializeField] private Type    _type;
            [SerializeField] private Vector3 _defaultDirection;
            [SerializeField] private float   _force;

            public Type    type             { get { return _type; } }
            public Vector3 defaultDirection { get { return _defaultDirection; } }
            public float   force            { get { return _force; } }

            #endregion


            #region Constructor

            public Gravity(Type type, Vector3 defaultDirection, float force)
            {
                _type             = type;
                _defaultDirection = defaultDirection;
                _force            = force;
            }

            #endregion
        }


        [Serializable] public struct Core
        {
            #region Definition

            public enum Type     { None, Point, Line }
            public enum Location { None, Inside, Outside }
            public enum LineAxis { None, X_Axis, Y_Axis, Z_Axis }

            #endregion


            #region Field

            [SerializeField] private Type     _type;
            [SerializeField] private Location _location;
            [SerializeField] private LineAxis _lineAxis;

            public Type     type     { get { return _type; } }
            public Location location { get { return _location; } }
            public LineAxis lineAxis { get { return _lineAxis; } }

            #endregion


            #region Constructor

            public Core(Type type, Location location, LineAxis lineAxis)
            {
                _type     = type;
                _location = location;
                _lineAxis = lineAxis;
            }

            #endregion
        }

        #endregion


        #region Field

        [SerializeField] protected Gravity _gravity;
        [SerializeField] protected Core    _core;

        public Gravity gravity { get { return _gravity; } }
        public Core    core    { get { return _core; } }

        #endregion


        #region Constructor

        public Setting(Gravity gravity, Core core)
        {
            _gravity = gravity;
            _core    = core;
        }

        #endregion
    }

    #endregion


    #region Field

    public Collider                   area            { get; protected set; }
    public Transform                  core            { get; protected set; }
    public IModelController           ground          { get; protected set; }
    public ICharacterTargetController characterTarget { get; protected set; }
    public Transform                  objects         { get; protected set; }
    public List<ICharacterBase>       characters      { get; protected set; }
    public List<IModelController>     models          { get; protected set; }
    public ISceneBase                 scene           { get; protected set; }

    [SerializeField] protected Setting _setting;

    public Setting setting { get { return _setting; } }

    protected bool playerIsKinematic = false;

    #endregion


    #region Method

    #region Event

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

    #endregion


    #region Initialization

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

    #endregion


    #region Set

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

    #endregion


    #region Gravity

    public virtual Vector3 GetGravity(IGravityable target)
    {
        var     gravitySetting = setting.gravity;
        Vector3 direction      = gravitySetting.defaultDirection;

        switch (gravitySetting.type)
        {
            case GravityType.Core:   direction = GetCoreDirection(target);   break;
            //case GravityType.Normal: direction = GetNormalDirection(target); break;
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

    //protected virtual Vector3 GetNormalDirection(IGravityable target)
    //{
    //    float   radius    = target.radius;
    //    Vector3 origin    = target.transform.TransformPoint(Vector3.up * radius * 2f);
    //    Vector3 direction = -target.transform.up;
    //    Ray     ray       = new Ray(origin, direction);
    //    int     layerMask = 1 << LayerMask.NameToLayer("Planet Ground");

    //    if (Physics.SphereCast(ray, radius, out RaycastHit hit, float.MaxValue, layerMask)) return -hit.normal;
    //    else                                                                                return setting.defaultDirection;
    //}

    #endregion

    #endregion
}
