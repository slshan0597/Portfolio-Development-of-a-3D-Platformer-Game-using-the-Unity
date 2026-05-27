using System.Collections.Generic;
using UnityEngine;

using Game;


namespace Stage
{
    using Levels    = StageController.Levels;
    using StageData = Game.DataManager.Stages.Stage;
    using LevelData = Game.DataManager.Stages.Stage.Levels.Level;


    public interface IStageController
    {
        #region Property

        // Component
        GameObject gameObject { get; }
        Transform  transform  { get; }
        Levels     levels     { get; }


        // Reference
        ISceneDirector scene { get; }

        // Setting
        Material  skybox { get; }
        AudioClip bgm    { get; }

        #endregion


        #region Method

        void Initialize(StageData stageData);

        #endregion
    }


    public class StageController : MonoBehaviour, IStageController
    {
        #region Definition

        public class Levels : Dictionary<int, ILevelController>
        {
            #region Field

            public ILevelController current { get; protected set; }

            #endregion


            #region Constructor

            public Levels(Transform transform) : base()
            {
                foreach (var level in transform.GetComponentsInChildren<ILevelController>(true))
                {
                    int index = level.transform.GetSiblingIndex() + 1;

                    Add(index, level);
                }
            }

            #endregion


            #region Method

            public void Initialize(LevelData levelData)
            {
                current = this[levelData.id];

                foreach (var level in Values)
                {
                    if (current == level) level.Initialize();
                    else                  level.gameObject.SetActive(false);
                }
            }

            #endregion
        }

        #endregion


        #region Field

        public Levels         levels { get; protected set; }
        public ISceneDirector scene  { get; protected set; }

        [SerializeField] protected Material  _skybox;
        [SerializeField] protected AudioClip _bgm;

        public Material  skybox { get { return _skybox; } }
        public AudioClip bgm    { get { return _bgm; } }

        #endregion


        #region Method

        #region Event

        protected virtual void Awake() { SetField(); }

        #endregion


        #region Initialization

        protected virtual void SetField()
        {
            levels = new Levels(transform);
            scene  = FindObjectOfType<SceneDirector>(true);
        }

        public virtual void Initialize(StageData stageData)
        {
            gameObject.SetActive(true);

            RenderSettings.skybox = skybox;

            levels.Initialize(stageData.levels.Current);
        }

        #endregion

        #endregion
    }
}
