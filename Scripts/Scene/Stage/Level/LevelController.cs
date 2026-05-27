using System.Collections.Generic;
using UnityEngine;


namespace Stage
{
    using CheckPoints = LevelController.CheckPoints;


    public interface ILevelController
    {
        #region Property

        // Component
        GameObject               gameObject  { get; }
        Transform                transform   { get; }
        IIntroLauncherController launcher    { get; }
        IPlanetController[]      planets     { get; }
        CheckPoints              checkPoints { get; }

        // Reference
        IStageController stage { get; }

        #endregion


        #region Method

        void Initialize();
        void Save(ICheckPointable checkPoint);

        #endregion
    }


    public class LevelController : MonoBehaviour, ILevelController
    {
        #region Definition

        public class CheckPoints : List<ICheckPointable>
        {
            #region Field

            public ICheckPointable current;

            #endregion


            #region Constructor

            public CheckPoints(Transform transform) : base(transform.GetComponentsInChildren<ICheckPointable>(true)) { }

            #endregion
        }

        #endregion


        #region Field

        public IIntroLauncherController launcher    { get; protected set; }
        public IPlanetController[]      planets     { get; protected set; }
        public CheckPoints              checkPoints { get; protected set; }
        public IStageController         stage       { get; protected set; }

        #endregion


        #region Method

        #region Event

        protected virtual void Awake() { SetField(); }

        #endregion


        #region Initialization

        protected virtual void SetField()
        {
            launcher    = GetComponentInChildren<IIntroLauncherController>(true);
            planets     = GetComponentsInChildren<IPlanetController>(true);
            checkPoints = new CheckPoints(transform);
            stage       = GetComponentInParent<IStageController>(true);
        }

        public virtual void Initialize()
        {
            gameObject.SetActive(true);

            IDataManager data = stage.scene.data;

            launcher.gameObject.SetActive(false);

            int index = data.level.checkPointIndex;

            checkPoints.current = (index >= 0) ? checkPoints[index] : null;

            for (int i = 0; i < planets.Length; i++) planets[i].Initialize(i);
        }

        #endregion


        #region Data

        public virtual void Save(ICheckPointable checkPoint)
        {
            IDataManager data = stage.scene.data;

            int index = checkPoints.FindIndex(_checkPoint => _checkPoint == checkPoint);

            checkPoints.current        = checkPoint;
            data.level.checkPointIndex = index;

            for (int i = 0; i < planets.Length; i++) planets[i].Save(i);
        }

        #endregion

        #endregion
    }
}
