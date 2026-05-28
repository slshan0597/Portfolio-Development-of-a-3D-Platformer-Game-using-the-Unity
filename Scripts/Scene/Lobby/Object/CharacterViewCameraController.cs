// //////////////////////////////////////////////////////////////////////////////
// * 요약
//    - 로비 씬 전용 캐릭터 뷰(View) 카메라 클래스
//    - 캐릭터 메뉴에서 캐릭터 뷰 모드 수행 시 호출됨
//    - 화면 드래그 시 캐릭터를 중심으로 카메라 회전
//
// * 목차
//    1. 인터페이스 ... Line 
//    2. 클래스 ....... Line 
//        1) 내부 타입 ... Line 
//        2) 필드 ........ Line 
//        3) 메서드 ...... Line 
//            1- 이벤트 함수 ... Line 
//            1- 초기화 ........ Line 
//            2- 셋(Set) ....... Line 
//            3- 액션 .......... Line 
//                1_ 회전(Rotate) ... Line 
// //////////////////////////////////////////////////////////////////////////////
using System;
using UnityEngine;

namespace Lobby
{
    using RotationSetting = CharacterViewCameraController.RotationSetting;

    // //////////////////////////////////////////////////////////////////////////////
    // 1. 인터페이스(global::ICameraController 인터페이스 상속)
    // //////////////////////////////////////////////////////////////////////////////
    public interface ICharacterViewCameraController : global::ICameraController
    {
        // 프로퍼티
        // reference
        new ISceneDirector scene { get; }

        // setting
        RotationSetting rotationSetting { get; }

        // 메서드
        void Rotate(Vector2 point);
    }

    // //////////////////////////////////////////////////////////////////////////////
    // 2. 클래스(global::CameraController 클래스 상속)
    // //////////////////////////////////////////////////////////////////////////////
    public class CharacterViewCameraController : global::CameraController, ICharacterViewCameraController
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
        protected Vector3 angles;

        // ==============================================================================
        // 3) 메서드
        // ==============================================================================
        // ------------------------------------------------------------------------------
        // 3-1) 메서드 -> 이벤트 함수
        // ------------------------------------------------------------------------------
        protected virtual void OnEnable() { Initialize(); }

        protected virtual void Update() { FollowAngles(); }

        // ------------------------------------------------------------------------------
        // 3-2) 메서드 -> 초기화
        // ------------------------------------------------------------------------------
        protected override void SetField()
        {
            base.SetField();

            scene = FindObjectOfType<SceneDirector>(true);
        }

        protected override void ResetField()
        {
            base.ResetField();

            _rotationSetting = new RotationSetting(new Vector2(60f, 0f), 0.25f, 10f);
        }

        public override void Initialize()
        {
            angles                  = Vector3.zero;
            transform.localRotation = Quaternion.identity;
        }

        // ------------------------------------------------------------------------------
        // 3-2) 메서드 -> 셋(Set)
        //    - 로비 씬에는 하나 이상의 카메라가 존재하며, 특정 카메라 배치 시 해당 카메라만 활성화하여 배치
        // ------------------------------------------------------------------------------
        public override Coroutine Set(ICameraTargetController target, float duration = -1)
        {
            gameObject.SetActive(true);
            scene.cameras.TrySetCurrent(this);    // 현재 카메라를 메인 카메라로 세팅(나머지 전부 off)

            transform.position            = target.transform.position;
            transform.rotation            = target.transform.rotation;
            inner.transform.localPosition = target.endPoint.localPosition;
            inner.transform.localRotation = target.endPoint.localRotation;

            return null;
        }

        // ------------------------------------------------------------------------------
        // 3-3) 메서드 -> 액션
        // ------------------------------------------------------------------------------
        // ******************************************************************************
        // 3-3-1) 메서드 -> 액션 -> 회전(Rotate)
        //    - 화면 드래그 시(해당 UI의 이벤트로 호출됨) 입력값을 통해 캐릭터를 중심으로 카메라 회전
        // ******************************************************************************
        public virtual void Rotate(Vector2 point)
        {
            float maxHorizontalAngle = rotationSetting.maxAngles.x;

            angles.x += point.y * rotationSetting.amount;
            angles.y -= point.x * rotationSetting.amount;
            angles.x =  Mathf.Clamp(angles.x, -maxHorizontalAngle, maxHorizontalAngle);
        }

        protected virtual void FollowAngles()
        {
            Quaternion targetRotation = Quaternion.Euler(angles);

            transform.localRotation = Quaternion.Slerp(transform.localRotation, targetRotation,
                Time.deltaTime * rotationSetting.speed);
        }
    }
}
