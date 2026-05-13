using System.Collections;
using UnityEngine;


namespace Stage
{
    public interface IGoombaController : global::IGoombaController
    {
        #region Property

        // Reference
        new IPlanetController planet { get; }

        #endregion
    }


    public class GoombaController : global::GoombaController, ITriggerable
    {
        #region Field

        public new IPlanetController planet { get; protected set; }

        [Header("Trigger Setting")]
        [SerializeField] protected bool _useTrigger;

        public bool useTrigger { get { return _useTrigger; } }

        #endregion


        #region Method

        #region Initialization

        protected override void SetField()
        {
            base.SetField();

            planet = GetComponentInParent<IPlanetController>(true);
        }

        #endregion


        #region Action

        #region Die

        protected override IEnumerator _Die()
        {
            yield return base._Die();

            if (useTrigger) planet.TryClear();
        }

        #endregion

        #endregion

        #endregion
    }
}
