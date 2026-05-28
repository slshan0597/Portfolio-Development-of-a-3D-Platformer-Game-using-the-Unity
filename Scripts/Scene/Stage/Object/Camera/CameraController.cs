using System.Collections;
using UnityEngine;

using Game;


namespace Stage
{
    public interface ICameraController : global::ICameraController
    {
        #region Property

        // Reference
        new ISceneDirector scene { get; }

        #endregion


        #region Method

        Coroutine Translate(ICameraTargetController target, float duration = -1f);
        void      StopTranslate();

        #endregion
    }


    public class CameraController : global::CameraController, ICameraController
    {
        #region Field

        public new ISceneDirector scene { get; protected set; }

        protected Coroutine translateAction;

        #endregion


        #region Method

        #region Initialization

        protected override void SetField()
        {
            base.SetField();

            scene = FindObjectOfType<SceneDirector>(true);
        }

        #endregion


        #region Set

        public override Coroutine Set(ICameraTargetController target, float duration = -1)
        {
            gameObject.SetActive(true);
            scene.cameras.TrySetCurrent(this);

            return base.Set(target, duration);
        }

        #endregion


        #region Translate

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

        #endregion

        #endregion
    }
}
