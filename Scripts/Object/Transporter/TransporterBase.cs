using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Resources = TransporterBase.Resources;
using State     = TransporterBase.State;
using MainState = TransporterBase.State.Main;
using Type      = TransporterBase.Type;


public interface ITransporterBase
{
    #region Property

    // Component
    GameObject gameObject { get; }
    Transform  transform  { get; }
    Resources  resources  { get; }

    // Reference
    ISceneBase scene { get; }

    // State
    State state { get; }

    // Setting
    Type type { get; }

    #endregion


    #region Method

    void      Idle();
    Coroutine Appear();
    Coroutine Disappear();
    Coroutine Transport(IPlayerController player);

    #endregion
}


public class TransporterBase : MonoBehaviour, ITransporterBase, IInteractable
{
    #region Definition

    public enum Type { None, Launcher, Pipe }


    public class Resources
    {
        #region Definition

        public class Effects : Dictionary<MainState, IEffectController>
        {
            #region Constructor

            public Effects(Transform transform) : base()
            {
                foreach (var effect in transform.GetComponentsInChildren<IEffectController>(true))
                {
                    string name = effect.gameObject.name.Replace(" ", string.Empty);

                    if (Enum.TryParse(name, out MainState state)) Add(state, effect);
                }
            }

            #endregion


            #region Method

            public float Play(MainState state) => ContainsKey(state) ? this[state].Play() : default;

            #endregion
        }

        #endregion


        #region Field

        public IModelController model   { get; }
        public Effects          effects { get; }

        #endregion


        #region Constructor

        public Resources(Transform transform)
        {
            model   = transform.GetComponentInChildren<IModelController>(true);
            effects = new Effects(transform.Find("Effects"));
        }

        #endregion


        #region Method

        public float Play(MainState state)
        {
            model.Play(state.ToString());

            return effects.Play(state);
        }

        #endregion
    }


    public class State
    {
        #region Definition

        public enum Main { None, Idle, Appear, Disappear, Transport }
        public enum Sub  { None, Start, Loop, End }

        #endregion


        #region Field

        public Main main;
        public Sub  sub;

        #endregion
    }

    #endregion


    #region Field

    public SphereCollider trigger   { get; protected set; }
    public Resources      resources { get; protected set; }
    public ISceneBase     scene     { get; protected set; }

    public State state { get; protected set; } = new State();

    public Type type { get; protected set; }

    #endregion


    #region Method

    #region Event

    protected virtual void Awake() { SetField(); }

    protected virtual void Start() { if (state.main != MainState.Appear) Idle(); }

    #endregion


    #region Initialization

    protected virtual void SetField()
    {
        trigger   = Array.Find(GetComponents<SphereCollider>(), collider => collider.isTrigger);
        resources = new Resources(transform.Find("Resources"));
        scene     = FindObjectOfType<SceneBase>(true);
    }

    #endregion


    #region Idle

    public virtual void Idle() 
    {
        state.main      = MainState.Idle;
        trigger.enabled = true;

        resources.Play(state.main);

        resources.model.animator.updateMode = AnimatorUpdateMode.Normal;
    }

    #endregion


    #region Appear

    public virtual Coroutine Appear()
    {
        state.main      = MainState.Appear;
        trigger.enabled = false;

        float duration = resources.Play(state.main);

        return StartCoroutine(_Appear(duration, Time.timeScale <= 0f));
    }

    protected virtual IEnumerator _Appear(float duration, bool useUnscaledTime)
    {
        yield return useUnscaledTime ? new WaitForSecondsRealtime(duration) : new WaitForSeconds(duration);

        Idle();
    }

    #endregion


    #region Disappear

    public virtual Coroutine Disappear()
    {
        state.main      = MainState.Disappear;
        trigger.enabled = false;

        float duration = resources.Play(state.main);

        return StartCoroutine(_Disappear(duration, Time.timeScale <= 0f));
    }

    protected virtual IEnumerator _Disappear(float duration, bool useUnscaledTime) 
    {
        yield return useUnscaledTime ? new WaitForSecondsRealtime(duration) : new WaitForSeconds(duration);

        gameObject.SetActive(false);
    }

    #endregion


    #region Transport

    public Coroutine Interact(IPlayerController player) { return Transport(player); }

    public void StopInteract(IPlayerController player) { }

    public virtual Coroutine Transport(IPlayerController player)
    {
        state.main                   = MainState.Transport;
        trigger.enabled              = false;
        player.rigidbody.isKinematic = true;

        player.resources.Play(type);

        return StartCoroutine(_Transport(player));
    }

    protected virtual IEnumerator _Transport(IPlayerController player) { yield break; }

    #endregion

    #endregion
}
