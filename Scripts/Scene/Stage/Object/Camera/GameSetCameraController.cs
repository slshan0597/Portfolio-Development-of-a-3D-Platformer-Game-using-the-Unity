using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Game;


namespace Stage
{
    using LayerType = GameSetCameraController.LayerType;


    public interface IGameSetCameraController : global::ICameraController
    {
        #region Property

        // Component
        Dictionary<LayerType, Camera> inners { get; }

        // Reference
        new ISceneDirector scene { get; }

        #endregion


        #region Method

        Coroutine Rotate(Vector3 euler, float duration = -1f);

        #endregion
    }


    public class GameSetCameraController : global::CameraController, IGameSetCameraController
    {
        #region Definition

        public enum LayerType { None, Player, UI, Other }

        #endregion


        #region Field

        public Dictionary<LayerType, Camera> inners { get; protected set; }
        public new ISceneDirector            scene  { get; protected set; }

        protected Coroutine rotateAction;

        #endregion


        #region Method

        #region Initialization

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

        #endregion


        #region Set

        public override Coroutine Set(ICameraTargetController target, float duration = -1)
        {
            gameObject.SetActive(true);
            scene.cameras.TrySetCurrent(this);
            scene.ui.SetCanvas(inners[LayerType.UI]);

            return base.Set(target, duration);
        }

        #endregion


        #region Rotate

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

        #endregion

        #endregion
    }
}
