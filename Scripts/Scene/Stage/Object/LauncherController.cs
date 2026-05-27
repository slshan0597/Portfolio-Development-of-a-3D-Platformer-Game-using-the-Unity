using System.Collections;
using UnityEngine;


namespace Stage
{
    public interface ILauncherController : global::ILauncherController { }


    public class LauncherController : global::LauncherController, ILauncherController, IRewardable, ICheckPointable
    {
        #region Field

        public ICharacterTargetController characterTarget { get; protected set; }
        public IPlanetController          planet          { get; protected set; }
        public ILevelController           level           { get; protected set; }

        [SerializeField] protected bool _useCheckPoint;

        public bool useCheckPoint { get { return _useCheckPoint; } }

        #endregion


        #region Method

        #region Initialization

        protected override void SetField()
        {
            base.SetField();

            characterTarget = GetComponentInChildren<ICharacterTargetController>(true);
            planet          = GetComponentInParent<IPlanetController>(true);
            level           = GetComponentInParent<ILevelController>(true);
        }

        #endregion


        #region Transport

        protected override IEnumerator _Transport(global::IPlayerController player)
        {
            ISceneDirector scene = level.stage.scene;

            yield return base._Transport(player);

            if (useCheckPoint) scene.SaveLevel(this);
        }

        #endregion

        #endregion
    }
}
