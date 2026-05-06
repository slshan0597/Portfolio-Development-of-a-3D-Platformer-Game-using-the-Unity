// //////////////////////////////////////////////////////////////////////////////
// * 목차
//    1. 인터페이스 ... Line 30
//    2. 클래스 ....... Line 53
//        1) 정의 ... Line 59
//            1- 캐릭터 상태 ... Line 62
//            2- 캐릭터 설정 ... Line 74
//        2) 필드 ..... Line 114
//        3) 메서드 ... Line 130
//            1- 초기화 ... Line 134
//            2- 액션 ..... Line 165
//                1_ 대기(Idle) ....... Line 180
//                2_ 피격(Damage) ..... Line 208
//                3_ 대화(Talk) ....... Line 256
//                4_ 작별(Farewell) ... Line 321
// //////////////////////////////////////////////////////////////////////////////
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

// //////////////////////////////////////////////////////////////////////////////
// 1. 인터페이스(ICharacterBase 인터페이스 상속)
// //////////////////////////////////////////////////////////////////////////////
public interface INonPlayerCharacterController : ICharacterBase
{
    // 프로퍼티
    // Component
    SphereCollider                             trigger   { get; }
    new INonPlayerCharacterDirectionController direction { get; }
    new Resources                              resources { get; }

    // State
    new State state { get; }

    // Setting
    Setting setting { get; }

    // 메서드
    // Action
    Coroutine Talk(IPlayerController player);
    Coroutine Farewell();
}

// //////////////////////////////////////////////////////////////////////////////
// 2. 클래스(CharacterBase 클래스 상속)
//    - Interactable 타입 -> 플레이어와 상호작용 가능
// //////////////////////////////////////////////////////////////////////////////
public class NonPlayerCharacterController : CharacterBase, INonPlayerCharacterController, IInteractable
{
    // ==============================================================================
    // 1) 정의
    // ==============================================================================
    // ------------------------------------------------------------------------------
    // 1-1) 정의 -> 캐릭터 상태
    //    - 부모 클래스(CharacterBase.State)를 대체하여 새로 정의
    //    - 캐릭터의 주 상태 저장
    // ------------------------------------------------------------------------------
    public new class State : CharacterBase.State
    {
        public new enum Main { None = 0, Idle = 1, Damage = 2, Talk = 4, Farewell = 8 }

        public new Main main;
    }

    // ------------------------------------------------------------------------------
    // 1-2) 정의 -> 캐릭터 설정
    //    - 캐릭터 상태에 대한 설정 프로퍼티 저장
    // ------------------------------------------------------------------------------
    [Serializable] public class Setting
    {
        // Definition
        [Serializable] public class Talk
        {
            [SerializeField] protected List<string> _script;

            public List<string> script { get { return _script; } }

            public Talk(List<string> script) { _script = script; }
        }

        [Serializable] public class Farewell
        {
            [SerializeField] protected float _duration;

            public float duration { get { return _duration; } }

            public Farewell(float duration) { _duration = duration; }
        }

        // Field
        [SerializeField] protected Talk     _talk;
        [SerializeField] protected Farewell _farewell;

        public Talk     talk     { get { return _talk; } }
        public Farewell farewell { get { return _farewell; } }

        // Method
        public Setting(Talk talk, Farewell farewell)
        {
            _talk     = talk;
            _farewell = farewell;
        }
    }

    // ==============================================================================
    // 2) 필드
    // ==============================================================================
    // Component & Reference
    public SphereCollider                             trigger   { get; protected set; }
    public new INonPlayerCharacterDirectionController direction { get; protected set; }
    public new Resources                              resources { get; protected set; }

    // State
    public new State state { get; protected set; } = new State();

    // Setting
    [SerializeField] protected Setting _setting;

    public Setting setting { get { return _setting; } }

    // ==============================================================================
    // 3) 메서드
    //    - 부모 클래스의 함수들을 재정의하여 확장
    // ==============================================================================
    // ------------------------------------------------------------------------------
    // 3-1) 메서드 -> 초기화
    //    - 필드(컴포넌트 등) 초기화
    // ------------------------------------------------------------------------------
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

    // ------------------------------------------------------------------------------
    // 3-2) 메서드 -> 액션(Common)
    //    - 모든 행동에 대한 정지
    // ------------------------------------------------------------------------------
    public override void StopAction()
    {
        base.StopAction();

        switch (state.main)
        {
            case MainState.Talk:     StopTalk();     break;
            case MainState.Farewell: StopFarewell(); break;
        }
    }

    // ******************************************************************************
    // 3-2-1) 메서드 -> 액션 -> 대기(Idle)
    // ******************************************************************************
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

    // ******************************************************************************
    // 3-2-2) 메서드 -> 액션 -> 피격(Damage)
    //    - 실질적으로 피해를 입지 않고 공격에 대한 단순 피드백
    // ******************************************************************************
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

    // ******************************************************************************
    // 3-2-3) 메서드 -> 액션 -> 대화(Talk)
    //    - 플레이어에 의해 Interactable 타입으로 호출됨
    //    - 대화창(UI)이 종료될 때까지 플레이어와 대화
    // ******************************************************************************
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

    // ******************************************************************************
    // 3-2-4) 메서드 -> 액션 -> 작별(Farewell)
    //    - 대화 종료에 대한 단순 피드백
    // ******************************************************************************
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
}
