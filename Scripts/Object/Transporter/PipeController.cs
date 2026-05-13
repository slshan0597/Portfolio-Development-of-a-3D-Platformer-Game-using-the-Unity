using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using State          = PipeController.State;
using TransportState = PipeController.State.Transport;
using MainState      = TransporterBase.State.Main;


public interface IPipeController : ITransporterBase
{
    #region Property

    // Component
    Collider                   collider        { get; }
    ICharacterTargetController characterTarget { get; }

    // State
    new State state { get; }

    // Setting
    IPipeController                   exitPipe  { get; }
    SimpleData<TransportState, float> durations { get; }

    #endregion


    #region Method

    Coroutine Exit(IPlayerController player);

    #endregion
}


public class PipeController : TransporterBase, IPipeController
{
    #region Definition

    public new class State : TransporterBase.State
    {
        #region Definition

        public enum Transport { None, Ready, Enter, Wait, Exit }

        #endregion


        #region Field

        public Transport transport;

        #endregion
    }

    #endregion


    #region Field

    public new Collider               collider        { get; protected set; }
    public ICharacterTargetController characterTarget { get; protected set; }

    public new State state { get; protected set; } = new State();

    [SerializeField] protected PipeController                    _exitPipe;
    [SerializeField] protected SimpleData<TransportState, float> _durations;

    public IPipeController                   exitPipe  { get { return _exitPipe; } }
    public SimpleData<TransportState, float> durations { get { return _durations; } }

    #endregion


    #region Method

    #region Event

    protected virtual void Reset() { ResetField(); }

    protected virtual void OnDrawGizmosSelected()
    {
        if (exitPipe == null) return;

        Gizmos.color = Color.white;

        Gizmos.DrawLine(transform.position, exitPipe.transform.position);
    }

    #endregion


    #region Initialization

    protected override void SetField()
    {
        base.SetField();

        collider        = Array.Find(GetComponents<Collider>(), collider => !collider.isTrigger);
        characterTarget = GetComponentInChildren<ICharacterTargetController>(true);

        type = Type.Pipe;
    }

    protected virtual void ResetField()
    {
        _durations = new SimpleData<TransportState, float>(
            new List<SimpleData<TransportState, float>.Element>()
            {
                    new SimpleData<TransportState, float>.Element(TransportState.Ready, 0.25f),
                    new SimpleData<TransportState, float>.Element(TransportState.Enter, 1f),
                    new SimpleData<TransportState, float>.Element(TransportState.Wait,  1f),
                    new SimpleData<TransportState, float>.Element(TransportState.Exit,  1f)
            });
    }

    #endregion


    #region Idle

    public override void Idle()
    {
        base.Idle();

        state.main      = MainState.Idle;
        state.transport = TransportState.None;
    }

    #endregion


    #region Appear

    public override Coroutine Appear()
    {
        gameObject.SetActive(true);

        state.main = MainState.Appear;

        return base.Appear();
    }

    #endregion


    #region Disappear

    public override Coroutine Disappear()
    {
        state.main = MainState.Disappear;

        return base.Disappear();
    }

    #endregion


    #region Transport

    public override Coroutine Transport(IPlayerController player)
    {
        state.main = MainState.Transport;

        return base.Transport(player);
    }

    protected override IEnumerator _Transport(IPlayerController player)
    {
        yield return Ready(player);
        yield return Enter(player);
        yield return Wait(player);

        Idle();

        yield return exitPipe.Exit(player);
    }

    protected virtual IEnumerator Ready(IPlayerController player)
    {
        state.transport = TransportState.Ready;

        yield return player.Set(characterTarget, false, false, durations[state.transport]);
    }

    protected virtual IEnumerator Enter(IPlayerController player)
    {
        state.transport = TransportState.Enter;

        resources.effects.Play(state.main);
        player.resources.model.PlayNext();

        yield return new WaitForSeconds(durations[state.transport]);
    }

    protected virtual IEnumerator Wait(IPlayerController player)
    {
        state.transport = TransportState.Wait;

        player.resources.model.meshes.gameObject.SetActive(false);

        yield return new WaitForSeconds(durations[state.transport]);
    }


    #region Exit

    public virtual Coroutine Exit(IPlayerController player)
    {
        player.Set(characterTarget, true);

        return StartCoroutine(_Exit(player));
    }

    protected virtual IEnumerator _Exit(IPlayerController player)
    {
        state.main      = MainState.Transport;
        state.transport = TransportState.Exit;

        resources.effects.Play(state.main);
        player.resources.model.meshes.gameObject.SetActive(true);
        player.resources.model.PlayNext();

        yield return new WaitForSeconds(durations[state.transport]);

        player.rigidbody.isKinematic = false;

        player.resources.model.PlayNext();

        Idle();
    }

    #endregion


    //public override void StopTransport(IPlayerController player)
    //{
    //    StopAllCoroutines();

    //    if (player.state.interactable == (IInteractable)this)
    //    {
    //        player.rigidbody.isKinematic = false;

    //        player.resources.model.meshes.gameObject.SetActive(true);

    //        if (exitPipe.gameObject.activeInHierarchy) exitPipe.StopTransport(player);
    //    }

    //    Idle();
    //}

    #endregion

    #endregion
}
