using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using LifeSpans  = BombController.LifeSpans;
using Resources  = BombController.Resources;
using DamageType = IDamageable.Type;


public interface IBombController : IThrowableBase
{
    #region Property

    // Component
    IBombUIController ui        { get; }
    new Resources     resources { get; }

    // Reference
    IPlanetController planet { get; }

    // Setting
    LifeSpans  lifeSpans { get; }
    GameObject explosion { get; }

    #endregion


    #region Method

    Coroutine SetTimer();
    void      StopSetTimer();

    #endregion
}


public class BombController : ThrowableBase, IBombController
{
    #region Definition

    public enum TimerState { None, Safety, Medium, Danger }


    public new class Resources : ThrowableBase.Resources
    {
        #region Definition

        public class Effects
        {
            #region Field

            public IBombTimerEffectController timer { get; }
            public IEffectController          fuse  { get; }

            #endregion


            #region Constructor

            public Effects(Transform transform)
            {
                timer = transform.GetComponentInChildren<IBombTimerEffectController>(true);
                fuse  = transform.Find("Fuse").GetComponent<IEffectController>();
            }

            #endregion
        }

        #endregion


        #region Field

        public GameObject gameObject { get; }
        public Effects    effects    { get; }

        #endregion


        #region Constructor

        public Resources(Transform transform) : base(transform)
        {
            gameObject = transform.gameObject;
            effects    = new Effects(transform.Find("Effects")); 
        }

        #endregion
    }


    [Serializable] public class LifeSpans : SimpleData<TimerState, float>
    {
        #region Field

        public float total
        {
            get
            {
                float sum = 0f;

                foreach (var element in this) sum += element.value;

                return sum;
            }
        }

        #endregion


        #region Constructor

        public LifeSpans(List<Element> elements) : base(elements) { }

        public LifeSpans(LifeSpans other) : base(other) { }

        #endregion
    }

    #endregion


    #region Field

    public IBombUIController ui        { get; protected set; }
    public new Resources     resources { get; protected set; }
    public IPlanetController planet    { get; protected set; }

    [SerializeField] protected LifeSpans  _lifeSpans;
    [SerializeField] protected GameObject _explosion;

    public LifeSpans  lifeSpans { get { return _lifeSpans; } }
    public GameObject explosion { get { return _explosion; } }

    protected Coroutine timerAction;

    #endregion


    #region Method

    #region Event

    protected override void Start()
    {
        if (transform.parent.TryGetComponent(out IBobombController enemy))
        {
            rigidbody.isKinematic = true;
            collider.enabled      = false;
            trigger.enabled       = false;

            ui.gameObject.SetActive(false);
            resources.model.gameObject.SetActive(false);

            return;
        }

        base.Start();
        SetTimer();
    }

    protected override void OnCollisionEnter(Collision collision)
    {
        if ((state != State.Throw) || collision.transform.TryGetComponent(out IPlayerController player)) return;

        Destroy();
    }

    #endregion


    #region Initialization

    protected override void SetField()
    {
        base.SetField();

        ui        = GetComponentInChildren<IBombUIController>(true);
        resources = new Resources(transform.Find("Resources"));
        planet    = GetComponentInParent<IPlanetController>(true);
    }

    protected override void ResetField(Rigidbody rigidbody)
    {
        base.ResetField(rigidbody);

        _mask      = DamageType.Explode;
        _lifeSpans = new LifeSpans(
            new List<SimpleData<TimerState, float>.Element>()
            {
                new SimpleData<TimerState, float>.Element(TimerState.Safety, 5f),
                new SimpleData<TimerState, float>.Element(TimerState.Medium, 3f),
                new SimpleData<TimerState, float>.Element(TimerState.Danger, 2f)
            });
    }

    #endregion


    #region Throw

    public override void Throw(IPlayerController player)
    {
        base.Throw(player);
        StopSetTimer();
    }

    #endregion


    #region Destroy

    protected override IEnumerator _Destroy(bool useUnscaledTime)
    {
        StopSetTimer();
        resources.gameObject.SetActive(false);

        var explosion = Instantiate(this.explosion, transform.position, transform.rotation, planet.objects)
            .GetComponent<IExplosionController>();

        yield return explosion.Explode();

        Destroy(gameObject);
    }

    #endregion


    #region Timer

    public virtual Coroutine SetTimer() { return timerAction = StartCoroutine(_SetTimer()); }

    protected virtual IEnumerator _SetTimer()
    {
        ui.Display(lifeSpans.total);
        resources.effects.fuse.Play();

        foreach (TimerState state in Enum.GetValues(typeof(TimerState)))
        {
            if (state == TimerState.None) continue;

            resources.effects.timer.Play(state);
            ui.ChangeColor(state);

            yield return new WaitForSeconds(lifeSpans[state]);
        }

        Destroy();
    }

    public virtual void StopSetTimer()
    {
        if (timerAction != null) StopCoroutine(timerAction);

        timerAction = null;

        ui.gameObject.SetActive(false);
        resources.effects.fuse.Stop();
        resources.effects.timer.Stop();
    }

    #endregion

    #endregion
}
