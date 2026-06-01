// //////////////////////////////////////////////////////////////////////////////
// * 요약
//    - 스테이지 씬 전용 메인 카메라 클래스
//    - 플레이어 추격 등의 기능은 부모 클래스와 플레이어 타겟 클래스에서 이미 구현됨
//    - 특수 이벤트에 대한 카메라 이동 기능 추가
//
// * 목차
//    1. 인터페이스 ... Line 24
//    2. 클래스 ....... Line 38
//        1) 필드 ..... Line 43
//        2) 메서드 ... Line 52
//            1- 초기화 ............ Line 55
//            2- 셋(Set) ........... Line 65
//            3- 이동(Translate) ... Line 77
// //////////////////////////////////////////////////////////////////////////////
using System.Collections;
using UnityEngine;

using Game;

namespace Stage
{
    // //////////////////////////////////////////////////////////////////////////////
    // 1. 인터페이스(global::ICameraController 인터페이스 상속)
    // //////////////////////////////////////////////////////////////////////////////
    public interface ICameraController : global::ICameraController
    {
        // 프로퍼티
        // Reference
        new ISceneDirector scene { get; }

        // 메서드
        Coroutine Translate(ICameraTargetController target, float duration = -1f);
        void      StopTranslate();
    }

    // //////////////////////////////////////////////////////////////////////////////
    // 2. 클래스(global::CameraController 클래스 상속)
    // //////////////////////////////////////////////////////////////////////////////
    public class CameraController : global::CameraController, ICameraController
    {
        // ==============================================================================
        // 1) 필드
        // ==============================================================================
        // Component & Reference
        public new ISceneDirector scene { get; protected set; }

        // etc.
        protected Coroutine translateAction;

        // ==============================================================================
        // 2) 메서드
        // ==============================================================================
        // ------------------------------------------------------------------------------
        // 2-1) 메서드 -> 초기화
        // ------------------------------------------------------------------------------
        protected override void SetField()
        {
            base.SetField();

            scene = FindObjectOfType<SceneDirector>(true);
        }

        // ------------------------------------------------------------------------------
        // 2-2) 메서드 -> 셋(Set)
        //    - 카메라 배치 시 카메라 활성화 및 현재 카메라로 설정
        // ------------------------------------------------------------------------------
        public override Coroutine Set(ICameraTargetController target, float duration = -1)
        {
            gameObject.SetActive(true);
            scene.cameras.TrySetCurrent(this);

            return base.Set(target, duration);
        }

        // ------------------------------------------------------------------------------
        // 2-3) 메서드 -> 이동(Translate)
        //    - Set 기능과 유사
        //    - Set은 고정 타겟(좌표)에 대한 이동인 반면, Translate은 유동 타겟(좌표)에 대한 이동 기능
        // ------------------------------------------------------------------------------
        public virtual Coroutine Translate(ICameraTargetController target, float duration = -1f)
        {
            StopTranslate();

            duration = (duration < 0f) ? defaultDuration : duration;

            return translateAction = StartCoroutine(_Translate(target, duration));
        }

        protected virtual IEnumerator _Translate(ICameraTargetController target, float duration)
        {
            IAnimationCurvePreset curvePreset = GameDirector.instance.curvePreset;

            Vector3 startPosition = transform.position;
            float   elapsedTime   = 0f;
            var     curveType     = curvePreset.types[1];

            while (elapsedTime < duration)
            {
                float rate = curveType.Evaluate(elapsedTime / duration);

                transform.position = Vector3.Lerp(startPosition, target.endPoint.position, rate);

                elapsedTime += Time.fixedDeltaTime;
                yield return new WaitForFixedUpdate();
            }

            transform.position = target.endPoint.position;
            translateAction    = null;
        }

        public virtual void StopTranslate()
        {
            if (translateAction != null)
            {
                StopCoroutine(translateAction);

                translateAction = null;
            }
        }
    }
}
