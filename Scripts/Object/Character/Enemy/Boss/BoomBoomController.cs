// //////////////////////////////////////////////////////////////////////////////
// * 요약
//    - 기반 클래스(BossBase)의 확장 클래스
//    - 플레이어 발견 시 일정 시간동안 플레이어를 향해 돌격
//    - 피격 시 형태(Model)를 변경하며, 다시 일정 시간동안 속도를 증가시켜 돌격
//
// * 목차
//    1. 인터페이스 ... Line 40
//    2. 클래스 ....... Line 61
//        1) 정의 ... Line 66
//            1- 캐릭터 상태 ... Line 69
//            2- 캐릭터 설정 ... Line 83
//        2) 필드 ..... Line 166
//        3) 메서드 ... Line 185
//            1- 초기화 ... Line 190
//            2- 액션 ..... Line 266
//                1_ 대기(Idle) ...... Line 281
//                2_ 피격(Damage) .... Line 307
//                3_ 발견(Find) ...... Line 373
//                4_ 죽기(Die) ....... Line 407
//                5_ 등장(Appear) .... Line 426
//                6_ 추격(Chase) ..... Line 446
//                7_ 멈추기(Brake) ... Line 527
// //////////////////////////////////////////////////////////////////////////////
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Resources   = BoomBoomController.Resources;
using ModelType   = BoomBoomController.Resources.Models.Type;
using State       = BoomBoomController.State;
using MainState   = BoomBoomController.State.Main;
using Setting     = BoomBoomController.Setting;
using SubState    = CharacterBase.State.Sub;
using MoveSetting = CharacterBase.CharacterSetting.Move;
using DamageType  = IDamageable.Type;

// //////////////////////////////////////////////////////////////////////////////
// 1. 인터페이스(IBossBase 인터페이스 상속)
// //////////////////////////////////////////////////////////////////////////////
public interface IBoomBoomController : IBossBase
{
    // 프로퍼티
    // Component
    new Resources resources { get; }

    // State
    new State state { get; }

    // Setting
    Setting setting { get; }

    // 메서드
    // Action
    Coroutine Chase(IPlayerController player, ModelType type);
    Coroutine Brake(ModelType type);
}

// //////////////////////////////////////////////////////////////////////////////
// 2. 클래스(BossBase 클래스 상속)
// //////////////////////////////////////////////////////////////////////////////
public class BoomBoomController : BossBase, IBoomBoomController
{
    // ==============================================================================
    // 1) 정의
    // ==============================================================================
    // ------------------------------------------------------------------------------
    // 1-1) 정의 -> 캐릭터 상태
    //    - 부모 클래스(BossBase.State)를 대체하여 새로 정의
    //    - 캐릭터의 주 상태 저장
    //    - 캐릭터의 형태 저장
    // ------------------------------------------------------------------------------
    public new class State : BossBase.State
    {
        public new enum Main { None = 0, Idle = 1, Damage = 2, Find = 4, Die = 8, Appear = 16, Chase = 32, Brake = 64 }

        public new Main  main;
        public ModelType model;
    }

    // ------------------------------------------------------------------------------
    // 1-2) 정의 -> 캐릭터 설정
    //    - 캐릭터 상태에 대한 설정 프로퍼티 저장
    // ------------------------------------------------------------------------------
    [Serializable] public class Setting
    {
        // Definition
        [Serializable] public class Find : SimpleData<ModelType, Find.Value>
        {
            [Serializable] public class Value
            {
                [SerializeField] protected float _duration;

                public float duration { get { return _duration; } }

                public Value(float duration) { _duration = duration; }

                #endregion
            }

            public Find(SimpleData<ModelType, Value> _base) : base(_base) { }
        }

        [Serializable] public class Chase : SimpleData<ModelType, Chase.Value>
        {
            [Serializable] public class Value
            {
                [SerializeField] protected SimpleData<SubState, MoveSetting> _move;

                public SimpleData<SubState, MoveSetting> move { get { return _move; } }

                public Value(SimpleData<SubState, MoveSetting> move) { _move = move; }
            }

            [SerializeField]                  protected float _duration;
            [SerializeField, Range(0f, 180f)] protected float _angle;

            public float duration { get { return _duration; } }
            public float angle    { get { return _angle; } }

            public Chase(SimpleData<ModelType, Value> _base, float duration, float angle) : base(_base)
            {
                _duration = duration;
                _angle    = angle;
            }
        }

        [Serializable] public class Brake : SimpleData<ModelType, Brake.Value>
        {
            [Serializable] public class Value
            {
                [SerializeField] protected SimpleData<SubState, float> _durations;

                public SimpleData<SubState, float> durations { get { return _durations; } }

                public Value(SimpleData<SubState, float> durations) { _durations = durations; }
            }

            [SerializeField] protected MoveSetting _move;

            public MoveSetting move { get { return _move; } }

            public Brake(SimpleData<ModelType, Value> _base, MoveSetting move) : base(_base) { _move = move; }
        }

        // Field
        [SerializeField] protected Find  _find;
        [SerializeField] protected Chase _chase;
        [SerializeField] protected Brake _brake;

        public Find  find  { get { return _find; } }
        public Chase chase { get { return _chase; } }
        public Brake brake { get { return _brake; } }

        // Method
        public Setting(Find find, Chase chase, Brake brake)
        {
            _find  = find;
            _chase = chase;
            _brake = brake;
        }
    }

    // ==============================================================================
    // 2) 필드
    // ==============================================================================
    // Component & Reference
    public new Resources resources { get; protected set; }

    // State
    public new State state { get; protected set; } = new State();

    // Setting
    [SerializeField] protected Setting _setting;

    public Setting setting { get { return _setting; } }

    // etc.
    protected IPlayerController player;

    protected Coroutine chaseAction;

    // ==============================================================================
    // 3) 메서드
    //    - 부모 클래스의 함수들을 재정의하여 확장
    //    - 클래스 확장을 위한 기반 기능만을 구현
    // ==============================================================================
    // ------------------------------------------------------------------------------
    // 3-1) 메서드 -> 초기화
    //    - 필드(컴포넌트 등) 초기화
    // ------------------------------------------------------------------------------
    protected override void SetField()
    {
        base.SetField();

        resources = new Resources(transform.Find("Resources"));
    }

    protected override void ResetField(Rigidbody rigidbody)
    {
        base.ResetField(rigidbody);

        _characterSetting = new CharacterSetting(new CharacterSetting.Damage(1.75f, 400f, 60f));
        _setting          = new Setting(
            new Setting.Find(
                new SimpleData<ModelType, Setting.Find.Value>(
                    new List<SimpleData<ModelType, Setting.Find.Value>.Element>()
                    {
                        new SimpleData<ModelType, Setting.Find.Value>.Element(
                            ModelType.Body, new Setting.Find.Value(1f)),
                        new SimpleData<ModelType, Setting.Find.Value>.Element(
                            ModelType.Shell, new Setting.Find.Value(2.25f))
                    })),
            new Setting.Chase(
                new SimpleData<ModelType, Setting.Chase.Value>(
                    new List<SimpleData<ModelType, Setting.Chase.Value>.Element>()
                    {
                        new SimpleData<ModelType, Setting.Chase.Value>.Element(
                            ModelType.Body, new Setting.Chase.Value(
                                new SimpleData<SubState, MoveSetting>(
                                    new List<SimpleData<SubState, MoveSetting>.Element>()
                                    {
                                        new SimpleData<SubState, MoveSetting>.Element(
                                            SubState.Loop, new MoveSetting(20f, 5f)),
                                        new SimpleData<SubState, MoveSetting>.Element(
                                            SubState.End, new MoveSetting(0f, 2.5f))
                                    }))),
                        new SimpleData<ModelType, Setting.Chase.Value>.Element(
                            ModelType.Shell, new Setting.Chase.Value(
                                new SimpleData<SubState, MoveSetting>(
                                    new List<SimpleData<SubState, MoveSetting>.Element>()
                                    {
                                        new SimpleData<SubState, MoveSetting>.Element(
                                            SubState.Loop, new MoveSetting(30f, 5f)),
                                        new SimpleData<SubState, MoveSetting>.Element(
                                            SubState.End, new MoveSetting(0f, 2.5f))
                                    })))
                    }),
                10f, 90f),
            new Setting.Brake(
                new SimpleData<ModelType, Setting.Brake.Value>(
                    new List<SimpleData<ModelType, Setting.Brake.Value>.Element>()
                    {
                        new SimpleData<ModelType, Setting.Brake.Value>.Element(
                            ModelType.Body, new Setting.Brake.Value(
                                new SimpleData<SubState, float>(
                                    new List<SimpleData<SubState, float>.Element>()
                                    {
                                        new SimpleData<SubState, float>.Element(SubState.Loop, 5f),
                                        new SimpleData<SubState, float>.Element(SubState.End,  2.25f)
                                    }))),
                        new SimpleData<ModelType, Setting.Brake.Value>.Element(
                            ModelType.Shell, new Setting.Brake.Value(
                                new SimpleData<SubState, float>(
                                    new List<SimpleData<SubState, float>.Element>()
                                    {
                                        new SimpleData<SubState, float>.Element(SubState.Loop, 0.5f),
                                        new SimpleData<SubState, float>.Element(SubState.End,  1.25f)
                                    })))
                    }),
                new MoveSetting(0f, 5f)));
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
            case MainState.Chase: StopChase(); break;
            case MainState.Brake: StopBrake(); break;
        }
    }

    // ******************************************************************************
    // 3-2-1) 메서드 -> 액션 -> 대기(Idle)
    // ******************************************************************************
    public override Coroutine Idle(bool playAnimation = false)
    {
        StopAction();

        state.main = MainState.Idle;

        return base.Idle(playAnimation);
    }

    protected override IEnumerator _Idle()
    {
        yield return base._Idle();

        StopIdle();
    }

    protected override void StopIdle()
    {
        base.StopIdle();

        state.main = MainState.None;
    }

    // ******************************************************************************
    // 3-2-2) 메서드 -> 액션 -> 피격(Damage)
    //    - 피격 이후 Find 함수 호출
    // ******************************************************************************
    public override bool TryDamage(Transform attacker, DamageType type)
    {
        MainState invalidType    = MainState.Damage | MainState.Die | MainState.Appear;
        ModelType validModelType = ModelType.Body;

        if (type == DamageType.Foot)                                                 return true;
        if (invalidType.HasFlag(state.main) || !validModelType.HasFlag(state.model)) return false;
        if ((state.main == MainState.Chase) && (type == DamageType.Normal))          return false;

        return base.TryDamage(attacker, type);
    }

    public override Coroutine Damage(Transform attacker, DamageType type)
    {
        StopAction();

        state.main   = MainState.Damage;
        state.damage = type;

        return base.Damage(attacker, type);
    }

    protected override IEnumerator _Damage(Transform attacker, DamageType type)
    {
        switch (type)
        {
            case DamageType.Normal:
                {
                    yield return WaitUntilGrounded(true);

                    StopMove();
                    resources.effects.other.land.Play();

                    Vector3 force = characterSetting.damage.GetForce(direction.transform) * 0.5f;

                    rigidbody.AddForce(force, ForceMode.VelocityChange);

                    yield return WaitUntilGrounded(true);

                    resources.effects.other.land.Play();
                }
                break;
        }

        SetFriction(true);

        yield return base._Damage(attacker, type);

        Find(null);    // 호출 전 상태가 Damage임을 확인하기 위해 null 값을 전달(임시로 클래스 내에 플레이어 정보를 별도로 저장)
    }

    protected override void StopDamage()
    {
        base.StopDamage();

        state.main       = MainState.None;
        state.damage     = DamageType.None;
        collider.enabled = true;

        SetFriction(false);
    }

    // ******************************************************************************
    // 3-2-3) 메서드 -> 액션 -> 발견(Find)
    //    - 직전 상태(Idle, Damage)에 의해 캐릭터의 형태(모델 타입)를 결정
    //    - 직전 상태의 유형은 전달받은 매개변수(플레이어)로 판단
    // ******************************************************************************
    public override Coroutine Find(IPlayerController player)
    {
        StopAction();

        state.main = MainState.Find;

        if (player != null) this.player = player;
        else                state.model = ModelType.Shell;

        resources.Play(state.main);

        return base.Find(this.player);
    }

    protected override IEnumerator _Find(IPlayerController player)
    {
        yield return LookAt(player, setting.find[state.model].duration);

        Chase(player, state.model);
    }

    protected override void StopFind()
    {
        base.StopFind();

        state.main  = MainState.None;
        state.model = ModelType.Body;
    }

    // ******************************************************************************
    // 3-2-4) 메서드 -> 액션 -> 죽기(Die)
    // ******************************************************************************
    public override Coroutine Die()
    {
        StopAction();

        state.main = MainState.Die;

        return base.Die();
    }

    protected override void StopDie()
    {
        base.StopDie();

        state.main = MainState.None;
    }

    // ******************************************************************************
    // 3-2-5) 메서드 -> 액션 -> 등장(Appear)
    //    - 필드 등장 연출 추가
    // ******************************************************************************
    public override Coroutine Appear()
    {
        gameObject.SetActive(true);

        state.main = MainState.Appear;

        return base.Appear();
    }

    protected override void StopAppear()
    {
        base.StopAppear();

        state.main = MainState.None;
    }

    // ******************************************************************************
    // 3-2-6) 메서드 -> 액션 -> 추격(Chase)
    //    - 시야각을 벗어날 때까지 플레이어를 향해 회전 및 이동
    //    - 지정 시간 내에 위의 루틴을 반복
    //    - 캐릭터 형태(모델 타입)에 따라 속도 조절
    // ******************************************************************************
    public virtual Coroutine Chase(IPlayerController player, ModelType type)
    {
        StopAction();

        state.main  = MainState.Chase;
        state.model = type;

        if (type == ModelType.Body) resources.Play(state.main);
        else
        {
            resources.models.Set(state.model);
            resources.effects[state.main].Play();
        }

        return action = StartCoroutine(_Chase(player, type));
    }

    protected virtual IEnumerator _Chase(IPlayerController player, ModelType type)
    {
        chaseAction = StartCoroutine(ChaseLoop(player.transform, type));

        yield return new WaitForSeconds(setting.chase.duration);

        Brake(type);
    }

    protected virtual IEnumerator ChaseLoop(Transform player, ModelType type)
    {
        state.sub = SubState.Loop;

        direction.LookAt(player);

        var   setting = this.setting.chase;
        float angle   = Vector3.Angle(direction.transform.forward, direction.GetDirection(player));

        while (angle <= setting.angle)
        {
            direction.LookAtSmooth(player);
            Move(setting[state.model].move[state.sub]);

            yield return new WaitForFixedUpdate();

            angle = Vector3.Angle(direction.transform.forward, direction.GetDirection(player));
        }

        state.sub = SubState.End;

        while (moveSpeed > 0f)
        {
            Move(setting[state.model].move[state.sub]);

            yield return new WaitForFixedUpdate();
        }

        yield return new WaitForFixedUpdate();

        chaseAction = StartCoroutine(ChaseLoop(player, type));
    }

    protected virtual void StopChase()
    {
        if (chaseAction != null)
        {
            StopCoroutine(chaseAction);
            chaseAction = null;
        }

        state.main  = MainState.None;
        state.sub   = SubState.None;
        state.model = ModelType.Body;

        resources.models.Set(ModelType.Body);
        resources.effects[MainState.Chase].Stop();
    }

    // ******************************************************************************
    // 3-2-7) 메서드 -> 액션 -> 멈추기(Brake)
    //    - 캐릭터의 급제동
    //    - 다음 행동까지의 인터벌
    // ******************************************************************************
    public virtual Coroutine Brake(ModelType type)
    {
        StopAction();

        state.main  = MainState.Brake;
        state.model = type;

        resources.Play(state.main);

        return action = StartCoroutine(_Brake(type));
    }

    protected virtual IEnumerator _Brake(ModelType type)
    {
        state.sub = SubState.Start;

        var setting = this.setting.brake;

        while (moveSpeed > 0f)
        {
            Move(setting.move);

            yield return new WaitForFixedUpdate();
        }

        state.sub = SubState.Loop;

        SetFriction(true);

        yield return new WaitForSeconds(setting[state.model].durations[state.sub]);

        state.sub = SubState.End;

        resources.Play(state.main, state.sub);

        yield return new WaitForSeconds(setting[state.model].durations[state.sub]);

        Idle();
    }

    protected virtual void StopBrake()
    {
        state.main  = MainState.None;
        state.sub   = SubState.None;
        state.model = ModelType.Body;

        SetFriction(false);
    }
}
