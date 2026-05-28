// //////////////////////////////////////////////////////////////////////////////
// * 요약
//    - 기반 클래스(BlockBase)의 확장 클래스
//    - 충돌 시 플레이어 클래스를 경유해 스테이지(레벨)의 클리어 기능 호출
//
// * 목차
//    1. 인터페이스 ... Line 
//    2. 클래스 ....... Line 
//        1) 필드 ..... Line 
//        2) 메서드 ... Line 
//            1- 이벤트 함수 ... Line 
//            2- 초기화 ........ Line 
//            3- 액션 .......... Line 
//                1_ 대기(Idle) .... Line 
//                2_ 생성(Spawn) ... Line 
//                3_ 획득(Get) ..... Line 
// //////////////////////////////////////////////////////////////////////////////
using System.Collections;
using UnityEngine;

using Game;

using Resources = GoalController.Resources;
using Type      = GoalController.Type;

// //////////////////////////////////////////////////////////////////////////////
// 1. 인터페이스(IItemBase 인터페이스 상속)
// //////////////////////////////////////////////////////////////////////////////
public interface IGoalController : IItemBase
{
    // 프로퍼티
    // Component
    new Resources resources  { get; }
    Transform     spawnPoint { get; }

    // Setting
    Type type { get; }
}

// //////////////////////////////////////////////////////////////////////////////
// 2. 클래스(ItemBase 클래스 상속)
// //////////////////////////////////////////////////////////////////////////////
public class GoalController : ItemBase, IGoalController
{
    public enum Type { None, Normal, Boss }

    // ==============================================================================
    // 1) 필드
    // ==============================================================================
    // Component & Reference
    public new Resources resources  { get; protected set; }
    public Transform     spawnPoint { get; protected set; }

    // Setting
    [SerializeField] protected Type _type;

    public Type type { get { return _type; } }

    // ==============================================================================
    // 2) 메서드
    // ==============================================================================
    // ------------------------------------------------------------------------------
    // 2-1) 메서드 -> 이벤트 함수
    // ------------------------------------------------------------------------------
    protected virtual void Update()
    {
        foreach (var element in resources.effects)
            if (element.Key != State.Get) element.Value.transform.position = resources.center.position;
    }

    protected virtual void OnDrawGizmosSelected()
    {
        Transform _spawnPoint = transform.Find("Spawn Point");

        Gizmos.color = Color.white;

        Gizmos.DrawLine(_spawnPoint.position, transform.position);
    }

    // ------------------------------------------------------------------------------
    // 2-2) 메서드 -> 초기화
    // ------------------------------------------------------------------------------
    protected override void SetField()
    {
        base.SetField();

        resources  = new Resources(transform.Find("Resources"));
        spawnPoint = transform.Find("Spawn Point");
    }

    // ------------------------------------------------------------------------------
    // 2-3) 메서드 -> 액션
    // ------------------------------------------------------------------------------
    // ******************************************************************************
    // 2-3-1) 메서드 -> 액션 -> 대기(Idle)
    // ******************************************************************************
    public override Coroutine Idle()
    {
        resources.Play(State.Idle);

        return base.Idle();
    }

    // ******************************************************************************
    // 2-3-2) 메서드 -> 액션 -> 생성(Spawn)
    //    - 생성된 후 스폰 지점에서 지정된 지점으로 이동
    // ******************************************************************************
    public override Coroutine Spawn(Transform spawner = null)
    {
        gameObject.SetActive(true);
        resources.Play(State.Spawn);

        return base.Spawn(spawner);
    }

    protected override IEnumerator _Spawn(Transform spawner, bool useUnscaledTime)
    {
        IAnimationCurvePreset curvePreset = GameDirector.instance.curvePreset;

        Vector3    startPosition   = spawnPoint.position;
        Quaternion startRotation   = spawnPoint.rotation;
        Vector3    endPosition     = transform.position;
        Quaternion endRotation     = transform.rotation;
        float      duration        = resources.effects[State.Spawn].Play();
        float      quarterDuration = duration * 0.25f;

        transform.position = startPosition;
        transform.rotation = startRotation;

        yield return useUnscaledTime ? new WaitForSecondsRealtime(quarterDuration * 3f) 
                                     : new WaitForSeconds(quarterDuration * 3f);

        float elapsedTime = 0f;
        var   curveType   = curvePreset.types[1];

        while (elapsedTime < quarterDuration)
        {
            float rate = curveType.Evaluate(elapsedTime / quarterDuration);

            transform.position = Vector3.Lerp(startPosition, endPosition, rate);
            transform.rotation = Quaternion.Slerp(startRotation, endRotation, rate);

            elapsedTime += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            yield return null;
        }

        Idle();
    }

    // ******************************************************************************
    // 2-3-3) 메서드 -> 액션 -> 획득(Get)
    //    - 획득 시 스테이지(레벨) 클리어
    // ******************************************************************************
    public override Coroutine Get(IPlayerController player)
    {
        player.Goal(type);    // 플레이어를 경유해 호출
        resources.model.Play(State.Get.ToString());
        resources.effects[State.Idle].Stop();

        return base.Get(player);
    }

    protected override IEnumerator _Get(IPlayerController player, bool useUnscaledTime)
    {
        float duration = resources.effects[State.Get].Play();

        resources.model.animator.updateMode = AnimatorUpdateMode.UnscaledTime;

        foreach (var effect in resources.effects.Values) effect.SetUnscaledTime(true);

        yield return new WaitForSecondsRealtime(duration);

        gameObject.SetActive(false);
    }
}
