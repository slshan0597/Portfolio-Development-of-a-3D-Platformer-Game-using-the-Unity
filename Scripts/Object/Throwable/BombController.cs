// //////////////////////////////////////////////////////////////////////////////
// * 목차
//    1. 인터페이스 ... Line 25
//    2. 클래스 ....... Line 47
//        1) 정의 ..... Line 52
//        2) 필드 ..... Line 76
//        3) 메서드 ... Line 94
//            1- 이벤트 함수 ... Line 98
//            2- 초기화 ........ Line 129
//            3- 타이머 ........ Line 156
//            4- 액션 .......... Line 191
//                1_ 던지기(Throw) ... Line 194
//                2_ 파괴(Destroy) ... Line 205
// //////////////////////////////////////////////////////////////////////////////
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using LifeSpans  = BombController.LifeSpans;
using Resources  = BombController.Resources;
using DamageType = IDamageable.Type;

// //////////////////////////////////////////////////////////////////////////////
// 1. 인터페이스(IThrowableBase 인터페이스 상속)
// //////////////////////////////////////////////////////////////////////////////
public interface IBombController : IThrowableBase
{
    // 프로퍼티
    // Component
    IBombUIController ui        { get; }
    new Resources     resources { get; }

    // Reference
    IPlanetController planet { get; }

    // Setting
    LifeSpans  lifeSpans { get; }
    GameObject explosion { get; }

    // 메서드
    Coroutine SetTimer();
    void      StopSetTimer();
}

// //////////////////////////////////////////////////////////////////////////////
// 2. 클래스(ThrowableBase 클래스 상속)
// //////////////////////////////////////////////////////////////////////////////
public class BombController : ThrowableBase, IBombController
{
    // ==============================================================================
    // 1) 정의
    // ==============================================================================
    public enum TimerState { None, Safety, Medium, Danger }

    [Serializable] public class LifeSpans : SimpleData<TimerState, float>
    {
        public float total
        {
            get
            {
                float sum = 0f;

                foreach (var element in this) sum += element.value;

                return sum;
            }
        }

        public LifeSpans(List<Element> elements) : base(elements) { }

        public LifeSpans(LifeSpans other) : base(other) { }
    }

    // ==============================================================================
    // 2) 필드
    // ==============================================================================
    // Component & Reference
    public IBombUIController ui        { get; protected set; }
    public new Resources     resources { get; protected set; }
    public IPlanetController planet    { get; protected set; }

    // Setting
    [SerializeField] protected LifeSpans  _lifeSpans;
    [SerializeField] protected GameObject _explosion;

    public LifeSpans  lifeSpans { get { return _lifeSpans; } }
    public GameObject explosion { get { return _explosion; } }

    // etc.
    protected Coroutine timerAction;

    // ==============================================================================
    // 3) 메서드
    //    - 부모 클래스의 함수들을 재정의하여 확장
    // ==============================================================================
    // ------------------------------------------------------------------------------
    // 3-1) 메서드 -> 이벤트 함수
    //    - 오브젝트 초기화
    //    - 오브젝트 타이머 셋
    //    - 충돌 시 오브젝트 파괴
    // ------------------------------------------------------------------------------
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

    // ------------------------------------------------------------------------------
    // 3-2) 메서드 -> 초기화
    //    - 필드(컴포넌트 등) 초기화
    // ------------------------------------------------------------------------------
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

    // ------------------------------------------------------------------------------
    // 3-3) 메서드 -> 타이머
    //    - 타이머 종료 시 오브젝트 파괴
    // ------------------------------------------------------------------------------
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

    // ------------------------------------------------------------------------------
    // 3-4) 메서드 -> 액션
    // ------------------------------------------------------------------------------
    // ******************************************************************************
    // 3-4-1) 메서드 -> 액션 -> 던지기(Throw)
    //    - 던짐과 동시에 타이머 정지
    // ******************************************************************************
    public override void Throw(IPlayerController player)
    {
        base.Throw(player);
        StopSetTimer();
    }

    // ******************************************************************************
    // 3-4-2) 메서드 -> 액션 -> 파괴(Destroy)
    //    - 오브젝트 파괴와 동시에 폭발(데미지 트리거 오브젝트 생성)
    // ******************************************************************************
    protected override IEnumerator _Destroy(bool useUnscaledTime)
    {
        StopSetTimer();
        resources.gameObject.SetActive(false);

        var explosion = Instantiate(this.explosion, transform.position, transform.rotation, planet.objects)
            .GetComponent<IExplosionController>();

        yield return explosion.Explode();

        Destroy(gameObject);
    }
}
