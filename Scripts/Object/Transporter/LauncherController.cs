using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

using Game;

using Points          = LauncherController.Points;
using Resources       = LauncherController.Resources;
using State           = LauncherController.State;
using TransportState  = LauncherController.State.Transport;
using Durations       = LauncherController.Durations;
using Duration        = LauncherController.Durations.Duration;
using MainState       = TransporterBase.State.Main;
using SubState        = TransporterBase.State.Sub;
using PlayerFaceType  = PlayerModelController.Meshes.FaceType;
using PlayerHandType  = PlayerModelController.Meshes.HandType;
using CameraShakeType = CameraController.ShakeSetting.Type;

// //////////////////////////////////////////////////////////////////////////////
// 1. 인터페이스(ITransporterBase 인터페이스 상속)
// //////////////////////////////////////////////////////////////////////////////
public interface ILauncherController : ITransporterBase
{
    // 프로퍼티
    // Component
    SplineCreator                   spline       { get; }
    Points                          points       { get; }
    ILauncherTrajectoryController[] trajectories { get; }
    new Resources                   resources    { get; }

    // State
    new State state { get; }

    // Setting
    Durations durations     { get; }
    float     rotationSpeed { get; }
}

// //////////////////////////////////////////////////////////////////////////////
// 2. 클래스(TransporterBase 클래스 상속)
// //////////////////////////////////////////////////////////////////////////////
public class LauncherController : TransporterBase, ILauncherController
{
    // ==============================================================================
    // 1) 정의
    // ==============================================================================
    public class Points
    {
        public Transform                  start { get; }
        public ICharacterTargetController end   { get; }

        public Points(Transform transform)
        {
            start = transform.Find("Start");
            end   = transform.GetComponentInChildren<ICharacterTargetController>(true);
        }
    }

    public new class State : TransporterBase.State
    {
        public enum Transport { None, Ready, Launch, Land }

        public Transport transport;
    }

    [Serializable] public class Durations : SimpleData<TransportState, Duration>
    {
        [Serializable] public class Duration : SimpleData<SubState, float>
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

            public Duration(List<Element> elements) : base(elements) { }

            public Duration(Duration other) : base(other) { }
        }

        public float total
        {
            get
            {
                float sum = 0f;

                foreach (var element in this) sum += element.value.total;

                return sum;
            }
        }

        public Durations(List<Element> elements) : base(elements) { }

        public Durations(Durations other) : base(other) { }
    }

    // ==============================================================================
    // 2) 필드
    // ==============================================================================
    // Component & Reference
    public SplineCreator                   spline       { get; protected set; }
    public Points                          points       { get; protected set; }
    public ILauncherTrajectoryController[] trajectories { get; protected set; }
    public new Resources                   resources    { get; protected set; }

    // State
    public new State state { get; protected set; } = new State();

    // Setting
    [SerializeField] protected Durations _durations;
    [SerializeField] protected float     _rotationSpeed;

    public Durations durations     { get { return _durations; } }
    public float     rotationSpeed { get { return _rotationSpeed; } }

    // ==============================================================================
    // 3) 메서드
    //    - 부모 클래스의 함수들을 재정의하여 확장
    // ==============================================================================
    // ------------------------------------------------------------------------------
    // 3-1) 메서드 -> 이벤트 함수
    //    - 오브젝트 초기화
    //    - 플레이어 접근 시 반응
    // ------------------------------------------------------------------------------
    protected virtual void Reset() { ResetField(); }

    protected virtual void OnTriggerEnter(Collider other)
    {
        if (other.isTrigger || !other.TryGetComponent(out IPlayerController player)) return;

        resources.effects.contact.Play();
    }

    protected virtual void OnTriggerExit(Collider other)
    {
        if (other.isTrigger || !other.TryGetComponent(out IPlayerController player)) return;

        resources.effects.contact.Stop(0.5f);
    }

    // ------------------------------------------------------------------------------
    // 3-2) 메서드 -> 초기화
    //    - 필드(컴포넌트 등) 초기화
    // ------------------------------------------------------------------------------
    protected override void SetField()
    {
        base.SetField();

        spline       = GetComponentInChildren<SplineCreator>(true);
        points       = new Points(transform.Find("Points"));
        trajectories = GetComponentsInChildren<ILauncherTrajectoryController>(true);
        resources    = new Resources(transform.Find("Resources"));

        type = Type.Launcher;
    }

    protected virtual void ResetField()
    {
        _rotationSpeed = 10f;
        _durations     = new Durations(
            new List<SimpleData<TransportState, Duration>.Element>()
            {
                new SimpleData<TransportState, Duration>.Element(
                    TransportState.Ready, new Duration(
                        new List<SimpleData<SubState, float>.Element>()
                        {
                            new SimpleData<SubState, float>.Element(SubState.Start, 0.1f),
                            new SimpleData<SubState, float>.Element(SubState.Loop,  0.75f),
                            new SimpleData<SubState, float>.Element(SubState.End,   0.5f)
                        })),
                new SimpleData<TransportState, Duration>.Element(
                    TransportState.Launch, new Duration(
                        new List<SimpleData<SubState, float>.Element>()
                        {
                            new SimpleData<SubState, float>.Element(SubState.Start, 2f),
                            new SimpleData<SubState, float>.Element(SubState.Loop,  4f),
                            new SimpleData<SubState, float>.Element(SubState.End,   0.75f)
                        })),
                new SimpleData<TransportState, Duration>.Element(
                    TransportState.Land, new Duration(
                        new List<SimpleData<SubState, float>.Element>()
                        {
                            new SimpleData<SubState, float>.Element(SubState.Start, 1.5f)
                        }))
            });
    }

    // ------------------------------------------------------------------------------
    // 3-3) 메서드 -> 액션
    // ------------------------------------------------------------------------------
    // ******************************************************************************
    // 3-3-1) 메서드 -> 액션 -> 대기(Idle)
    // ******************************************************************************
    public override void Idle()
    {
        base.Idle();

        state.main      = MainState.Idle;
        state.sub       = SubState.None;
        state.transport = TransportState.None;

        trajectories[1].gameObject.SetActive(false);
        trajectories[0].Draw(durations[TransportState.Launch].total, true);
        resources.model.gameObject.SetActive(true);
    }

    // ******************************************************************************
    // 3-3-2) 메서드 -> 액션 -> 활성화(Appear)
    // ******************************************************************************
    public override Coroutine Appear()
    {
        gameObject.SetActive(true);

        state.main = MainState.Appear;

        resources.model.gameObject.SetActive(true);

        return base.Appear();
    }

    protected override IEnumerator _Appear(float duration, bool useUnscaledTime)
    {
        trajectories[1].gameObject.SetActive(false);
        trajectories[0].Draw(duration);

        yield return base._Appear(duration, useUnscaledTime);
    }

    // ******************************************************************************
    // 3-3-3) 메서드 -> 액션 -> 비활성화(Disappear)
    // ******************************************************************************
    public override Coroutine Disappear()
    {
        state.main = MainState.Disappear;

        resources.model.gameObject.SetActive(false);
        resources.effects.contact.Stop(0.5f);

        return base.Disappear();
    }

    protected override IEnumerator _Disappear(float duration, bool useUnscaledTime)
    {
        trajectories[1].gameObject.SetActive(false);
        trajectories[0].Erase(duration);

        yield return base._Disappear(duration, useUnscaledTime);
    }

    // ******************************************************************************
    // 3-3-4) 메서드 -> 액션 -> 전송(Transport)
    //    - 플레이어를 지정된 경로에 따라 일정 시간에 걸쳐 이동
    //    - 루틴: 준비(Ready) -> 발사(Launch) -> 착지(Land)
    // ******************************************************************************
    public override Coroutine Transport(IPlayerController player)
    {
        state.main = MainState.Transport;

        resources.effects.contact.Stop(0.5f);

        return base.Transport(player);
    }

    protected override IEnumerator _Transport(IPlayerController player)
    {
        yield return Ready(player);
        yield return Launch(player);
        yield return Land(player);

        Idle();
    }

    // Ready
    protected virtual IEnumerator Ready(IPlayerController player)
    {
        state.sub       = SubState.Start;
        state.transport = TransportState.Ready;

        var duration = durations[state.transport];

        yield return Set_01(player, duration[state.sub]);

        state.sub = SubState.Loop;

        resources.Play(state.main);
        resources.effects.Play(state.transport);
        player.resources.model.PlayNext();

        yield return Set_02(player, duration[state.sub]);

        state.sub = SubState.End;

        player.resources.model.PlayNext();

        yield return new WaitForSeconds(duration[state.sub]);
    }

    protected virtual IEnumerator Set_01(IPlayerController player, float duration)
    {
        IAnimationCurvePreset curvePreset = GameDirector.instance.curvePreset;

        player.direction.transform.localRotation = Quaternion.identity;

        Transform character = player.transform;

        Vector3    startPosition = character.position;
        Quaternion startRotation = character.rotation;
        Vector3    endPosition   = points.start.position;
        Quaternion endRotation   = points.start.rotation;
        float      elapsedTime   = 0f;
        var        curveType     = curvePreset.types[0];

        while (elapsedTime < duration)
        {
            float rate = curveType.Evaluate(elapsedTime / duration);

            character.position = Vector3.Lerp(startPosition, endPosition, rate);
            character.rotation = Quaternion.Slerp(startRotation, endRotation, rate);

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        character.position = endPosition;
        character.rotation = endRotation;
    }

    protected virtual IEnumerator Set_02(IPlayerController player, float duration)
    {
        IAnimationCurvePreset curvePreset = GameDirector.instance.curvePreset;

        Transform character = player.transform;

        Vector3 startPosition = points.start.TransformPoint(Vector3.forward);
        Vector3 endPosition   = points.start.position;
        float   elapsedTime   = 0f;
        var     curveType     = curvePreset.types[0];

        while (elapsedTime < duration)
        {
            float rate = curveType.Evaluate(elapsedTime / duration);

            character.position = Vector3.Lerp(startPosition, endPosition, rate);

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        character.position = endPosition;
    }

    // Launch
    protected virtual IEnumerator Launch(IPlayerController player)
    {
        ICameraController camera = scene.camera;

        state.sub       = SubState.Start;
        state.transport = TransportState.Launch;

        var duration = durations[state.transport];

        trajectories[0].Erase(duration.total);
        trajectories[1].Draw(duration.total);

        resources.model.PlayNext();
        resources.effects.Play(state.transport);

        player.resources.model.PlayNext();
        player.resources.voice.Play(state.transport);
        player.resources.effects.interact.transport.launcher[state.transport].Play();
        camera.Shake(CameraShakeType.Hard);
        camera.Zoom(0.75f, camera.shakeSettings[CameraShakeType.Hard].duration);

        StartCoroutine(Move(player, duration));

        yield return new WaitForSeconds(duration[state.sub]);

        state.sub = SubState.Loop;

        player.resources.model.PlayNext();
        player.resources.model.SetHand(PlayerHandType.Open);

        yield return new WaitForSeconds(duration[state.sub]);

        state.sub = SubState.End;

        player.resources.model.PlayNext();
        player.resources.model.SetHand();
        player.resources.effects.interact.transport.launcher[state.transport].Stop(duration[state.sub]);
        
        yield return new WaitForSeconds(duration[state.sub]);

        player.resources.model.PlayNext();
    }

    protected virtual IEnumerator Move(IPlayerController player, Duration duration)
    {
        float totalDuration  = duration.total;
        float rotateDuration = duration[SubState.Start] + duration[SubState.Loop];
        float elapsedTime    = 0f;

        while (elapsedTime < totalDuration)
        {
            float      rate           = elapsedTime / totalDuration;
            float      nextRate       = (elapsedTime + Time.fixedDeltaTime) / totalDuration;
            Vector3    targetPosition = spline.GetPoint(rate);
            Vector3    nextPosition   = spline.GetPoint(nextRate);
            Vector3    direction      = (nextPosition - targetPosition).normalized;
            Quaternion targetRotation
                = (elapsedTime < rotateDuration) ? Quaternion.LookRotation(direction, player.transform.up)
                                                 : points.end.transform.rotation;

            player.transform.position = targetPosition;
            player.transform.rotation = Quaternion.Slerp(player.transform.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime);

            elapsedTime += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }

        player.transform.position = points.end.transform.position;
        player.transform.rotation = points.end.transform.rotation;
    }

    // Land
    protected virtual IEnumerator Land(IPlayerController player)
    {
        ICameraController camera = scene.camera;

        state.sub                    = SubState.None;
        state.transport              = TransportState.Land;
        player.rigidbody.isKinematic = false;

        points.end.Set(player);
        player.SetFriction(true);
        player.resources.voice.Play(state.transport);
        player.resources.effects.interact.transport.launcher[state.transport].Play();
        player.resources.model.SetFace(PlayerFaceType.Happy);
        player.resources.model.SetHand(PlayerHandType.Open);
        camera.Shake(CameraShakeType.Medium);
        camera.Zoom(1f, camera.shakeSettings[CameraShakeType.Medium].duration);

        yield return new WaitForSeconds(durations[state.transport].total);

        player.resources.model.SetFace();
        player.resources.model.SetHand();
    }
}

#if UNITY_EDITOR

[CustomEditor(typeof(LauncherController), true)]
public class LauncherControllerEditor : Editor
{
    // Field
    private LauncherController launcher;
    private Transform          transform;
    private Transform          startPoint;
    private Transform          endPoint;
    private SplineCreator      spline;
    private IModelController   model;

    #endregion


    #region Method

    #region Event

    protected void OnEnable()
    {
        launcher   = (LauncherController)target;
        transform  = launcher.GetComponent<Transform>();
        startPoint = transform.Find("Points").GetChild(0);
        endPoint   = transform.Find("Points").GetChild(1);
        spline     = launcher.GetComponentInChildren<SplineCreator>(true);
        model      = transform.GetComponentInChildren<IModelController>(true);
    }

    protected void OnSceneGUI() { SetPoints(); }

    #endregion


    private void SetPoints()
    {
        var nodes = spline.GetNodes();

        if (nodes.Length == 0) return;

        Vector3    firstPosition  = spline.GetPoint(0f);
        Vector3    secondPosition = spline.GetPoint(Time.fixedDeltaTime);
        Vector3    direction      = (secondPosition - firstPosition).normalized;
        Quaternion rotation       = Quaternion.LookRotation(direction, transform.up);

        startPoint.position       = nodes.First().startPosition;
        startPoint.rotation       = rotation;
        endPoint.position         = nodes.Last().endPosition;
        spline.transform.position = (startPoint.position + endPoint.position) * 0.5f;

        if (model == null) return;

        model.transform.position = startPoint.position;
        model.transform.rotation = startPoint.rotation;
    }

    #endregion
}

#endif
