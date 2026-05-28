// //////////////////////////////////////////////////////////////////////////////
// * 요약
//    - 로비 씬 전용 메인 카메라 클래스
//    - 화면 드래그 시 카메라를 일정 각도 회전 기능 추가
//
// * 목차
//    1. 인터페이스 ... Line 27
//    2. 클래스 ....... Line 45
//        1) 내부 타입 ... Line 50
//        2) 필드 ........ Line 71
//        3) 메서드 ...... Line 89
//            1- 초기화 .... Line 92
//            2- 셋(Set) ... Line 109
//            3- 액션 ...... Line 121
//                1_ 회전(Rotate) ....... Line 124
//                2_ 되돌리기(Return) ... Line 171
// //////////////////////////////////////////////////////////////////////////////
using System;
using System.Collections;
using UnityEngine;

namespace Lobby
{
    using RotationSetting = CameraController.RotationSetting;

    // //////////////////////////////////////////////////////////////////////////////
    // 1. 인터페이스(global::ICameraController 인터페이스 상속)
    // //////////////////////////////////////////////////////////////////////////////
    public interface ICameraController : global::ICameraController
    {
        // 프로퍼티
        // Reference
        new ISceneDirector scene { get; }

        // Setting
        RotationSetting rotationSetting { get; }

        // 메서드
        Coroutine Rotate(Vector2 startPoint);
        void      SetAngles(Vector2 currentPoint);
        Coroutine Return();
    }

    // //////////////////////////////////////////////////////////////////////////////
    // 2. 클래스(global::CameraController 클래스 상속)
    // //////////////////////////////////////////////////////////////////////////////
    public class CameraController : global::CameraController, ICameraController
    {
        // ==============================================================================
        // 1) 내부 타입
        // ==============================================================================
        [Serializable] public class RotationSetting
        {
            [SerializeField] private Vector2 _maxAngles;
            [SerializeField] private float   _amount;
            [SerializeField] private float   _speed;

            public Vector2 maxAngles { get { return _maxAngles; } }
            public float   amount    { get { return _amount; } }
            public float   speed     { get { return _speed; } }

            public RotationSetting(Vector2 maxAngles, float amount, float speed)
            {
                _maxAngles = maxAngles;
                _amount    = amount;
                _speed     = speed;
            }
        }

        // ==============================================================================
        // 2) 필드
        // ==============================================================================
        // Component & Reference
        public new ISceneDirector scene { get; protected set; }

        // Setting
        [SerializeField] protected RotationSetting _rotationSetting;

        public RotationSetting rotationSetting { get { return _rotationSetting; } }

        // etc.
        protected Coroutine action;

        protected Vector2 originPoint    = Vector2.zero;
        protected Vector3 originAngles   = Vector3.zero;
        protected Vector3 rotationAngles = Vector3.zero;

        // ==============================================================================
        // 3) 메서드
        // ==============================================================================
        // ------------------------------------------------------------------------------
        // 3-1) 메서드 -> 초기화
        // ------------------------------------------------------------------------------
        protected override void SetField()
        {
            base.SetField();

            scene = FindObjectOfType<SceneDirector>(true);
        }

        protected override void ResetField()
        {
            base.ResetField();

            _rotationSetting = new RotationSetting(new Vector2(10f, 10f), 40f, 5f);
        }

        // ------------------------------------------------------------------------------
        // 3-2) 메서드 -> 셋(Set)
        //    - 로비 씬에는 하나 이상의 카메라가 존재하며, 특정 카메라 배치 시 해당 카메라만 활성화하여 배치
        // ------------------------------------------------------------------------------
        public override Coroutine Set(ICameraTargetController target, float duration = -1)
        {
            gameObject.SetActive(true);
            scene.cameras.TrySetCurrent(this);    // 현재 카메라를 메인 카메라로 세팅(나머지 전부 off)

            return base.Set(target, duration);
        }

        // ------------------------------------------------------------------------------
        // 3-3) 메서드 -> 액션
        // ------------------------------------------------------------------------------
        // ******************************************************************************
        // 3-3-1) 메서드 -> 액션 -> 회전(Rotate)
        //    - 화면 드래그 시(해당 UI의 이벤트로 호출됨) 입력값을 통해 카메라 내부를 일정 각도로 회전
        // ******************************************************************************
        public virtual Coroutine Rotate(Vector2 startPoint)
        {
            if (action != null) StopCoroutine(action);

            originPoint    = startPoint;
            originAngles   = inner.transform.localRotation.eulerAngles;
            rotationAngles = Vector3.zero;

            return action = StartCoroutine(_Rotate());
        }

        protected virtual IEnumerator _Rotate()
        {
            Transform inner = this.inner.transform;

            while (true)
            {
                Quaternion targetRotation = Quaternion.Euler(rotationAngles);

                inner.localRotation = Quaternion.Slerp(inner.localRotation, targetRotation, Time.deltaTime * rotationSetting.speed);

                yield return null;
            }
        }

        public virtual void SetAngles(Vector2 currentPoint)
        {
            Resolution resolution    = Screen.currentResolution;
            Vector2    denominator   = new Vector2(1f / resolution.width, 1f / resolution.height);
            Vector2    displacement  = currentPoint - originPoint;
            Vector2    rate          = Vector2.Scale(displacement, denominator);
            float      originXAngle  = (originAngles.x > 180f) ? originAngles.x - 360f : originAngles.x;
            float      originYAngle  = (originAngles.y > 180f) ? originAngles.y - 360f : originAngles.y;
            float      xAngle        = originXAngle - (rate.y * rotationSetting.amount);
            float      yAngle        = originYAngle + (rate.x * rotationSetting.amount);
            float      maxXAngle     = rotationSetting.maxAngles.x;
            float      maxYAngle     = rotationSetting.maxAngles.y;
            float      clampedXAngle = Mathf.Clamp(xAngle, -maxXAngle, maxXAngle);
            float      clampedYAngle = Mathf.Clamp(yAngle, -maxYAngle, maxYAngle);

            rotationAngles = new Vector3(clampedXAngle, clampedYAngle, 0f);
        }

        // ******************************************************************************
        // 3-3-2) 메서드 -> 액션 -> 되돌리기(Return)
        //    - 화면 드래그 종료 시(해당 UI의 이벤트로 호출됨) 카메라 내부 회전을 멈추고 제자리로 되돌림
        // ******************************************************************************
        public virtual Coroutine Return()
        {
            if (action != null) StopCoroutine(action);

            return action = StartCoroutine(_Return());
        }

        protected virtual IEnumerator _Return()
        {
            Transform inner = this.inner.transform;

            while (Quaternion.Angle(inner.localRotation, Quaternion.identity) > 0.001f)
            {
                inner.localRotation = Quaternion.Slerp(inner.localRotation, Quaternion.identity, Time.deltaTime * rotationSetting.speed);

                yield return null;
            }

            inner.localRotation = Quaternion.identity;
            action              = null;
        }
    }
}
