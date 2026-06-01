// //////////////////////////////////////////////////////////////////////////////
// * 요약
//    - 스테이지 씬 전용 게임 셋 카메라 클래스
//    - 스테이지 클리어 또는 실패에 대한 카메라 연출 기능
//    - UI, 배경(필드), 캐릭터(플레이어) 3가지 레이어(마스크)에 따른 내부 다중 카메라 배치
//
// * 목차
//    1. 인터페이스 ... Line 28
//    2. 클래스 ....... Line 44
//        1) 필드 ..... Line 51
//        2) 메서드 ... Line 61
//            1- 초기화 ......... Line 64
//            2- 셋(Set) ........ Line 82
//            3- 회전(Rotate) ... Line 96
// //////////////////////////////////////////////////////////////////////////////
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Game;

namespace Stage
{
    using LayerType = GameSetCameraController.LayerType;

    // //////////////////////////////////////////////////////////////////////////////
    // 1. 인터페이스(global::ICameraController 인터페이스 상속)
    // //////////////////////////////////////////////////////////////////////////////
    public interface IGameSetCameraController : global::ICameraController
    {
        // 프로퍼티
        // Component
        Dictionary<LayerType, Camera> inners { get; }

        // Reference
        new ISceneDirector scene { get; }

        // 메서드
        Coroutine Rotate(Vector3 euler, float duration = -1f);
    }

    // //////////////////////////////////////////////////////////////////////////////
    // 2. 클래스(global::CameraController 클래스 상속)
    // //////////////////////////////////////////////////////////////////////////////
    public class GameSetCameraController : global::CameraController, IGameSetCameraController
    {
        public enum LayerType { None, Player, UI, Other }

        // ==============================================================================
        // 1) 필드
        // ==============================================================================
        // Component & Reference
        public Dictionary<LayerType, Camera> inners { get; protected set; }
        public new ISceneDirector            scene  { get; protected set; }

        // etc.
        protected Coroutine rotateAction;

        // ==============================================================================
        // 2) 메서드
        // ==============================================================================
        // ------------------------------------------------------------------------------
        // 2-1) 메서드 -> 초기화
        // ------------------------------------------------------------------------------
        protected override void SetField()
        {
            base.SetField();

            inners = new Dictionary<LayerType, Camera>();
            scene  = FindObjectOfType<SceneDirector>(true);

            foreach (var camera in GetComponentsInChildren<Camera>(true))
            {
                string name = camera.name.Replace(" ", string.Empty);

                if (Enum.TryParse(name, out LayerType type)) inners.Add(type, camera);
            }
        }

        // ------------------------------------------------------------------------------
        // 2-2) 메서드 -> 셋(Set)
        //    - 카메라 배치 시 카메라 활성화 및 현재 카메라로 설정
        //    - UI 켄버스의 렌더 모드 변경(Overlay -> World Space)
        // ------------------------------------------------------------------------------
        public override Coroutine Set(ICameraTargetController target, float duration = -1)
        {
            gameObject.SetActive(true);
            scene.cameras.TrySetCurrent(this);
            scene.ui.SetCanvas(inners[LayerType.UI]);

            return base.Set(target, duration);
        }

        // ------------------------------------------------------------------------------
        // 2-3) 메서드 -> 회전(Rotate)
        // ------------------------------------------------------------------------------
        public virtual Coroutine Rotate(Vector3 euler, float duration = -1f)
        {
            if (rotateAction != null)
            {
                StopCoroutine(rotateAction);
                rotateAction = null;
            }

            duration = (duration < 0f) ? defaultDuration : duration;

            return rotateAction = StartCoroutine(_Rotate(euler, duration, Time.timeScale == 0f));
        }

        protected virtual IEnumerator _Rotate(Vector3 euler, float duration, bool useUnscaledTime)
        {
            IAnimationCurvePreset curvePreset = GameDirector.instance.curvePreset;

            Quaternion startRotation = transform.rotation;
            Quaternion endRotation   = transform.rotation * Quaternion.Euler(euler);
            float      elapsedTime   = 0f;
            var        curveType     = curvePreset.types[1];

            while (elapsedTime < duration)
            {
                float rate = curveType.Evaluate(elapsedTime / duration);

                transform.rotation = Quaternion.Slerp(startRotation, endRotation, rate);

                elapsedTime += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                yield return null;
            }

            transform.rotation = endRotation;
            rotateAction       = null;
        }
    }
}
