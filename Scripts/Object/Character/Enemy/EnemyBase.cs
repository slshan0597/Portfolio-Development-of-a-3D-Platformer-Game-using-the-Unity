using System;
using System.Collections;
using UnityEngine;

using Resources    = EnemyBase.Resources;
using State        = EnemyBase.State;
using MainState    = EnemyBase.State.Main;
using EnemySetting = EnemyBase.EnemySetting;
using SubState     = CharacterBase.State.Sub;
using DamageType   = IDamageable.Type;


public interface IEnemyBase : ICharacterBase
{
    #region Property

    // Component
    SphereCollider                trigger   { get; }
    new IEnemyDirectionController direction { get; }
    new Resources                 resources { get; }

    // Reference
    IEnemySpawner spawner { get; set; }

    // State
    new State state { get; }

    // Setting
    EnemySetting enemySetting { get; }

    #endregion


    #region Method

    Coroutine Find(IPlayerController player);
    Coroutine Die();

    #endregion
}


public class EnemyBase : CharacterBase, IEnemyBase
{
    #region Definition

    public new class Resources : CharacterBase.Resources
    {
        #region Field

        public new IEnemyModelBase             model   { get; }
        public new IEnemyVoiceBase             voice   { get; }
        public new CharacterEffects<MainState> effects { get; }

        #endregion


        #region Constructor

        public Resources(Transform transform) : base(transform)
        {
            model   = transform.GetComponentInChildren<IEnemyModelBase>(true);
            voice   = transform.GetComponentInChildren<IEnemyVoiceBase>(true);
            effects = new CharacterEffects<MainState>(transform.Find("Effects"));
        }

        #endregion


        #region Method

        public float Play(MainState type, SubState subType = SubState.None)
        {
            model.Play(type, subType);

            return Mathf.Max(voice.Play(type, subType), (effects != null) ? effects.Play(type, subType) : default);
        }

        #endregion
    }


    public new class State : CharacterBase.State
    {
        #region Definition

        public new enum Main { None = 0, Idle = 1, Damage = 2, Find = 4, Die = 8 }

        #endregion


        #region Field

        public new Main main;

        #endregion
    }


    [Serializable] public class EnemySetting
    {
        #region Definition

        [Serializable] public class Find
        {
            #region Field

            [SerializeField] protected float _duration;

            public float duration { get { return _duration; } }

            #endregion


            #region Constructor

            public Find(float duration) { _duration = duration; }

            #endregion
        }


        [Serializable] public class Die
        {
            #region Field

            [SerializeField] protected GameObject _drop;

            public GameObject drop { get { return _drop; } }

            #endregion
        }

        #endregion


        #region Field

        [SerializeField] protected Find _find;
        [SerializeField] protected Die  _die;

        public Find find { get { return _find; } }
        public Die  die  { get { return _die; } }

        #endregion


        #region Constructor

        public EnemySetting(Find find)
        { 
            _find = find;
            _die  = new Die();
        }

        #endregion
    }

    #endregion


    #region Field

    public SphereCollider                trigger   { get; protected set; }
    public new IEnemyDirectionController direction { get; protected set; }
    public new Resources                 resources { get; protected set; }
    public IEnemySpawner                 spawner   { get; set; }

    public new State state { get; protected set; } = new State();

    [SerializeField] protected EnemySetting _enemySetting;

    public EnemySetting enemySetting { get { return _enemySetting; } }

    #endregion


    #region Method

    #region Event

    protected virtual void OnCollisionStay(Collision collision)
    {
        var invalidType = MainState.Damage | MainState.Die;

        if ((state.main != MainState.None) &&invalidType.HasFlag(state.main))   return;
        if (!collision.transform.TryGetComponent(out IPlayerController target)) return;

        ((IDamageable)target).TryDamage(transform, DamageType.Normal);
    }

    protected virtual void OnTriggerStay(Collider other)
    {
        if (other.isTrigger || !other.TryGetComponent(out IPlayerController player)) return;

        if (state.main != MainState.Find) TryFind(player);
    }

    #endregion


    #region Initialization

    protected override void SetField()
    {
        base.SetField();

        trigger   = GetComponent<SphereCollider>();
        direction = GetComponentInChildren<IEnemyDirectionController>(true);
        resources = new Resources(transform.Find("Resources"));
    }

    protected override void ResetField(Rigidbody rigidbody)
    {
        base.ResetField(rigidbody);

        _enemySetting = new EnemySetting(new EnemySetting.Find(1f));
    }

    public override void Initialize()
    {
        base.Initialize();

        trigger.enabled = false;
    }

    #endregion


    #region Action

    //protected Vector3 GetMovement(Vector3 moveAmount, bool isFirst = true, Vector3? originMoveAmount = null,
    //    float originSign = 0f, int count = 0)
    //{
    //    Vector3 prediction = transform.position + moveAmount * Time.fixedDeltaTime;

    //    #region Test

    //    //// Stack over flow
    //    //if (count > 10) return transform.position;

    //    //Ray   upRay       = new Ray(prediction, transform.up);
    //    //float height      = _collider.height;
    //    //float radius      = _collider.radius;
    //    //float bodyLength  = height - (radius * 2f);
    //    //float offset      = 0.5f;
    //    //float maxDistance = bodyLength * (1f + offset);
    //    //int   layerMask   = 1 << LayerMask.NameToLayer("Planet Ground");

    //    //if (Physics.SphereCast(upRay, radius, out RaycastHit headHit, maxDistance, layerMask))
    //    //    return CorrectMovement_Head(moveAmount, headHit.normal, isFirst, originMoveAmount, originSign, count);

    //    //Vector3 downOrigin = prediction + transform.up * bodyLength;
    //    //Ray     downRay    = new Ray(downOrigin, -transform.up);

    //    //float temp = 10f;

    //    //if (Physics.SphereCast(downRay, radius, out RaycastHit sphereHit, maxDistance * temp, layerMask))
    //    //{
    //    //    if (Physics.Raycast(downRay, out RaycastHit rayHit, maxDistance * temp, layerMask))
    //    //    {
    //    //        Vector3 slopeDirection = (sphereHit.point - rayHit.point).normalized;
    //    //        float   angle          = (slopeDirection != Vector3.zero) ? 90f - (Vector3.Angle(transform.up, slopeDirection))
    //    //                                                                  : 0f;

    //    //        if (sphereHit.distance <= groundHit.distance && angle >= maxSlopeAngle)
    //    //            return CorrectMovement_UpHill(moveAmount, sphereHit.normal, isFirst, originMoveAmount, originSign, count);

    //    //        if (sphereHit.distance > groundHit.distance && angle >= maxSlopeAngle)
    //    //            return CorrectMovement_DownHill(moveAmount, sphereHit.normal, isFirst, originMoveAmount, originSign, count);

    //    //        return prediction;
    //    //    }

    //    //    return CorrectMovement_DownHill(moveAmount, sphereHit.normal, isFirst, originMoveAmount, originSign, count);
    //    //}

    //    #endregion

    //    return prediction;
    //}

    //private Vector3 CorrectMovement_Head(Vector3 moveAmount, Vector3 wallNormal, bool isFirst = true,
    //    Vector3? originMoveAmount = null, float originSign = 0f, int count = 0)
    //{
    //    Vector3 correctedNormal = Vector3.ProjectOnPlane(wallNormal, -projection.up).normalized;
    //    Vector3 correctedAmount = Vector3.ProjectOnPlane(moveAmount, -correctedNormal);

    //    if (correctedAmount == Vector3.zero) return transform.position;

    //    if (isFirst)
    //    {
    //        float originAngle = Vector3.SignedAngle(moveAmount, -correctedNormal, projection.up);

    //        return GetMovement(correctedAmount, false, moveAmount, Mathf.Sign(originAngle), ++count);
    //    }

    //    if (originMoveAmount is null) return transform.position;

    //    float signedAngle = Vector3.SignedAngle((Vector3)originMoveAmount, -correctedNormal, projection.up);

    //    if (Mathf.Sign(signedAngle) != originSign) return transform.position;

    //    return GetMovement(correctedAmount, false, (Vector3)originMoveAmount, originSign, ++count);
    //}

    //private Vector3 CorrectMovement_UpHill(Vector3 moveAmount, Vector3 groundNormal, bool isFirst = true,
    //    Vector3? originMoveAmount = null, float originSign = 0f, int count = 0)
    //{
    //    Vector3 correctedNormal = Vector3.ProjectOnPlane(groundNormal, projection.up).normalized;
    //    Vector3 correctedAmount = Vector3.ProjectOnPlane(moveAmount, -correctedNormal);

    //    if (correctedAmount == Vector3.zero) return transform.position;

    //    if (isFirst)
    //    {
    //        float originAngle = Vector3.SignedAngle(moveAmount, -correctedNormal, projection.up);

    //        return GetMovement(correctedAmount, false, moveAmount, Mathf.Sign(originAngle), ++count);
    //    }

    //    if (originMoveAmount is null) return transform.position;

    //    float signedAngle = Vector3.SignedAngle((Vector3)originMoveAmount, -correctedNormal, projection.up);

    //    if (Mathf.Sign(signedAngle) != originSign) return transform.position;

    //    return GetMovement(correctedAmount, false, (Vector3)originMoveAmount, originSign, ++count);
    //}

    //private Vector3 CorrectMovement_DownHill(Vector3 moveAmount, Vector3 groundNormal, bool isFirst = true,
    //    Vector3? originMoveAmount = null, float originSign = 0f, int count = 0)
    //{
    //    Vector3 correctedNormal = Vector3.ProjectOnPlane(groundNormal, projection.up).normalized;
    //    Vector3 correctedAmount = Vector3.ProjectOnPlane(moveAmount, correctedNormal);

    //    if (correctedAmount == Vector3.zero) return transform.position;

    //    if (isFirst)
    //    {
    //        float originAngle = Vector3.SignedAngle(moveAmount, correctedNormal, projection.up);

    //        return GetMovement(correctedAmount, false, moveAmount, Mathf.Sign(originAngle), ++count);
    //    }

    //    if (originMoveAmount is null) return transform.position;

    //    float signedAngle = Vector3.SignedAngle((Vector3)originMoveAmount, correctedNormal, projection.up);

    //    if (Mathf.Sign(signedAngle) != originSign) return transform.position;

    //    return GetMovement(correctedAmount, false, (Vector3)originMoveAmount, originSign, ++count);
    //}

    public override void StopAction()
    {
        base.StopAction();

        switch (state.main)
        {
            case MainState.Find: StopFind(); break;
            case MainState.Die:  StopDie();  break;
        }
    }

    protected virtual IEnumerator LookAt(IPlayerController player, float duration)
    {
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            direction.LookAt(player.transform);

            elapsedTime += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }

        direction.LookAt(player.transform);
    }


    #region Idle

    public override Coroutine Idle(bool playAnimation = false)
    {
        state.main      = MainState.Idle;
        trigger.enabled = true;

        return base.Idle(playAnimation);
    }

    protected override IEnumerator _Idle() { while (planet.enabled) yield return new WaitForFixedUpdate(); }

    protected override void StopIdle()
    {
        base.StopIdle();

        state.main      = MainState.None;
        trigger.enabled = false;
    }

    #endregion


    #region Damage

    public override bool TryDamage(Transform attacker, DamageType type)
    {
        MainState invalidType = MainState.Damage | MainState.Die;

        if ((state.main != MainState.None) && invalidType.HasFlag(state.main)) return false;

        return base.TryDamage(attacker, type);
    }

    public override Coroutine Damage(Transform attacker, DamageType type)
    {
        state.main   = MainState.Damage;
        state.damage = type;

        return base.Damage(attacker, type);
    }

    protected override IEnumerator _Damage(Transform attacker, DamageType type)
    {
        yield return base._Damage(attacker, type);

        Die();
    }

    protected override void StopDamage()
    {
        base.StopDamage();

        state.main   = MainState.None;
        state.damage = DamageType.None;
    }

    #endregion


    #region Find

    protected virtual void TryFind(IPlayerController player)
    {
        Vector3 startPosition           = transform.position;
        Vector3 endPosition             = player.transform.position;
        var     queryTriggerInteraction = QueryTriggerInteraction.Ignore;

        if (Physics.Linecast(startPosition, endPosition, out RaycastHit hit, -1, queryTriggerInteraction)
            && (hit.transform != player.transform)) return;

        Find(player);
    }

    public virtual Coroutine Find(IPlayerController player) 
    {
        state.main = MainState.Find;

        SetFriction(true);
        resources.Play(state.main);

        return action = StartCoroutine(_Find(player));
    }

    protected virtual IEnumerator _Find(IPlayerController player) 
    {
        yield return LookAt(player, enemySetting.find.duration);
    }

    protected virtual void StopFind()
    { 
        state.main = MainState.None;

        SetFriction(false);
    }

    #endregion


    #region Die

    public virtual Coroutine Die()
    {
        state.main            = MainState.Die;
        rigidbody.isKinematic = true;
        collider.enabled      = false;

        if (enemySetting.die.drop != null)
        {
            var drop = Instantiate(enemySetting.die.drop, transform.position, transform.rotation, planet.objects);

            if (drop.TryGetComponent(out IItemBase item)) item.Spawn();
        }
        if (spawner != null) spawner.SpawnEnemy();

        return action = StartCoroutine(_Die());
    }

    protected virtual IEnumerator _Die()
    {
        resources.model.gameObject.SetActive(false);

        yield return new WaitForSeconds(resources.effects[state.main].Play());

        Destroy(gameObject);
    }

    protected virtual void StopDie()
    {
        state.main            = MainState.None;
        rigidbody.isKinematic = false;
        collider.enabled      = true;
    }

    #endregion

    #endregion

    #endregion
}
