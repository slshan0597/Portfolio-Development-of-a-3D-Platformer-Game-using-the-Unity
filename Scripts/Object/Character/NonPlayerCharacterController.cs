using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Resources  = NonPlayerCharacterController.Resources;
using State      = NonPlayerCharacterController.State;
using MainState  = NonPlayerCharacterController.State.Main;
using Setting    = NonPlayerCharacterController.Setting;
using SubState   = CharacterBase.State.Sub;
using DamageType = IDamageable.Type;


public interface INonPlayerCharacterController : ICharacterBase
{
    #region Property

    // Components
    SphereCollider                             trigger   { get; }
    new INonPlayerCharacterDirectionController direction { get; }
    new Resources                              resources { get; }

    // State
    new State state { get; }

    // Setting
    Setting setting { get; }

    #endregion


    #region Method

    Coroutine Talk(IPlayerController player);
    Coroutine Farewell();

    #endregion
}


public class NonPlayerCharacterController : CharacterBase, INonPlayerCharacterController, IInteractable
{
    #region Definition

    public new class Resources : CharacterBase.Resources
    {
        #region Field

        public new INonPlayerCharacterModelController model { get; }
        public new INonPlayerCharacterVoiceController voice { get; }

        #endregion


        #region Constructor

        public Resources(Transform transform) : base(transform)
        {
            model = transform.GetComponentInChildren<INonPlayerCharacterModelController>(true);
            voice = transform.GetComponentInChildren<INonPlayerCharacterVoiceController>(true);
        }

        #endregion


        #region Method

        public float Play(MainState type, SubState subType = SubState.None)
        {
            model.Play(type, subType);

            return voice.Play(type, subType);
        }

        #endregion
    }


    public new class State : CharacterBase.State
    {
        #region Definition

        public new enum Main { None = 0, Idle = 1, Damage = 2, Talk = 4, Farewell = 8 }

        #endregion


        #region Field

        public new Main main;

        #endregion
    }


    [Serializable] public class Setting
    {
        #region Definition

        [Serializable] public class Talk
        {
            #region Field

            [SerializeField] protected List<string> _script;

            public List<string> script { get { return _script; } }

            #endregion


            #region Constructor

            public Talk(List<string> script) { _script = script; }

            #endregion
        }


        [Serializable] public class Farewell
        {
            #region Field

            [SerializeField] protected float _duration;

            public float duration { get { return _duration; } }

            #endregion


            #region Constructor

            public Farewell(float duration) { _duration = duration; }

            #endregion
        }

        #endregion


        #region Field

        [SerializeField] protected Talk     _talk;
        [SerializeField] protected Farewell _farewell;

        public Talk     talk     { get { return _talk; } }
        public Farewell farewell { get { return _farewell; } }

        #endregion


        #region Constructor

        public Setting(Talk talk, Farewell farewell)
        {
            _talk     = talk;
            _farewell = farewell;
        }

        #endregion
    }

    #endregion


    #region Field

    public SphereCollider                             trigger   { get; protected set; }
    public new INonPlayerCharacterDirectionController direction { get; protected set; }
    public new Resources                              resources { get; protected set; }

    public new State state { get; protected set; } = new State();

    [SerializeField] protected Setting _setting;

    public Setting setting { get { return _setting; } }

    #endregion


    #region Method

    #region Initialization

    protected override void SetField()
    {
        base.SetField();

        trigger   = GetComponent<SphereCollider>();
        direction = GetComponentInChildren<INonPlayerCharacterDirectionController>(true);
        resources = new Resources(transform.Find("Resources"));
    }

    protected override void ResetField(Rigidbody rigidbody)
    {
        base.ResetField(rigidbody);

        _characterSetting = new CharacterSetting(new CharacterSetting.Damage(0.5f, 0f, 0f));
        _setting          = new Setting(
            new Setting.Talk(
                new List<string>()
                {
                    "Printing Sample Line 01",
                    "Printing Sample Line 02",
                    "Printing Sample Line 03",
                    "Printing Sample Line 04",
                    "Printing Sample Line 05",
                }),
            new Setting.Farewell(0.5f));
    }

    #endregion


    #region Action

    public override void StopAction()
    {
        base.StopAction();

        switch (state.main)
        {
            case MainState.Talk:     StopTalk();     break;
            case MainState.Farewell: StopFarewell(); break;
        }
    }


    #region Idle

    public override Coroutine Idle(bool playAnimation = false)
    {
        StopAction();

        state.main      = MainState.Idle;
        trigger.enabled = true;

        return base.Idle(playAnimation);
    }

    protected override IEnumerator _Idle() 
    {
        while (planet.enabled) yield return new WaitForFixedUpdate();

        StopIdle();
    }

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
        MainState invalidType = MainState.Damage | MainState.Talk;

        if (invalidType.HasFlag(state.main)) return false;

        base.TryDamage(attacker, type);

        return type == DamageType.Foot;
    }

    public override Coroutine Damage(Transform attacker, DamageType type)
    {
        StopAction();

        state.main   = MainState.Damage;
        state.damage = type;

        SetFriction(true);

        return base.Damage(attacker, type);
    }

    protected override IEnumerator _Damage(Transform attacker, DamageType type)
    {
        rigidbody.isKinematic                = false;
        collider.enabled                     = true;
        resources.model.transform.localScale = Vector3.one;

        yield return base._Damage(attacker, type);

        Idle();
    }

    protected override void StopDamage()
    {
        base.StopDamage();

        state.main   = MainState.None;
        state.damage = DamageType.None;

        SetFriction(false);
    }

    #endregion


    #region Talk

    public virtual Coroutine Interact(IPlayerController player) { return Talk(player); }

    public virtual void StopInteract(IPlayerController player) { }

    public virtual Coroutine Talk(IPlayerController player)
    {
        ISceneBase                      scene = planet.scene;
        IPlayerConversationUIController ui    = player.ui.conversation;

        StopAction();

        state.main                                 = MainState.Talk;
        player.resources.model.animator.updateMode = AnimatorUpdateMode.UnscaledTime;

        SetFriction(true);
        scene.Pause(true, scene.bgm, player.resources.audios.bgm);
        LookAtUnscaledTime(player.transform, ui.defaultDuration);
        resources.Play(state.main);
        player.LookAtUnscaledTime(transform, ui.defaultDuration);

        return action = StartCoroutine(_Talk(player));
    }

    protected virtual IEnumerator _Talk(IPlayerController player)
    {
        ICameraController               camera = planet.scene.camera;
        IPlayerConversationUIController ui     = player.ui.conversation;

        state.sub = SubState.Start;

        camera.Set(direction.GetCameraTarget(player), ui.defaultDuration);

        yield return ui.Display(true);

        state.sub = SubState.Loop;

        resources.Play(state.main, state.sub);

        yield return ui.Print(setting.talk.script);

        state.sub = SubState.End;

        camera.Set(player.cameraTarget, ui.defaultDuration);

        yield return ui.Display(false);

        Farewell();
    }

    protected virtual void StopTalk()
    {
        ISceneBase scene = planet.scene;

        state.main = MainState.None;
        state.sub  = SubState.None;

        SetFriction(false);
        scene.Pause(false);
    }

    #endregion


    #region Farewell

    public virtual Coroutine Farewell()
    {
        StopAction();

        state.main = MainState.Farewell;

        SetFriction(true);
        resources.Play(state.main);

        return action = StartCoroutine(_Farewell());
    }

    protected virtual IEnumerator _Farewell()
    {
        yield return new WaitForSeconds(setting.farewell.duration);

        Idle();
    }

    protected virtual void StopFarewell()
    { 
        state.main = MainState.None;

        SetFriction(false);
    }

    #endregion

    #endregion

    #endregion
}
