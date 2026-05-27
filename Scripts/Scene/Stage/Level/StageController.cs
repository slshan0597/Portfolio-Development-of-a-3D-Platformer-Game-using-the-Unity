// //////////////////////////////////////////////////////////////////////////////
// * 요약
//    - 스테이지 정보 클래스
//    - 하나의 스테이지 내에 다수의 레벨 정보 저장
//
// * 목차
//    1. 인터페이스 ... Line 25
//    2. 클래스 ....... Line 48
//        1) 내부 타입 ... Line 53
//        2) 필드 ........ Line 82
//        3) 메서드 ...... Line 96
// //////////////////////////////////////////////////////////////////////////////
using System.Collections.Generic;
using UnityEngine;

using Game;

namespace Stage
{
    using Levels    = StageController.Levels;
    using StageData = Game.DataManager.Stages.Stage;
    using LevelData = Game.DataManager.Stages.Stage.Levels.Level;

    // //////////////////////////////////////////////////////////////////////////////
    // 1. 인터페이스
    // //////////////////////////////////////////////////////////////////////////////
    public interface IStageController
    {
        // 프로퍼티
        // Component
        GameObject gameObject { get; }
        Transform  transform  { get; }
        Levels     levels     { get; }


        // Reference
        ISceneDirector scene { get; }

        // Setting
        Material  skybox { get; }
        AudioClip bgm    { get; }

        // 메서드
        void Initialize(StageData stageData);
    }

    // //////////////////////////////////////////////////////////////////////////////
    // 2. 클래스
    // //////////////////////////////////////////////////////////////////////////////
    public class StageController : MonoBehaviour, IStageController
    {
        // ==============================================================================
        // 1) 내부 타입
        // ==============================================================================
        public class Levels : Dictionary<int, ILevelController>
        {
            public ILevelController current { get; protected set; }

            public Levels(Transform transform) : base()
            {
                foreach (var level in transform.GetComponentsInChildren<ILevelController>(true))
                {
                    int index = level.transform.GetSiblingIndex() + 1;

                    Add(index, level);
                }
            }

            public void Initialize(LevelData levelData)
            {
                current = this[levelData.id];

                foreach (var level in Values)
                {
                    if (current == level) level.Initialize();
                    else                  level.gameObject.SetActive(false);
                }
            }
        }

        // ==============================================================================
        // 2) 필드
        // ==============================================================================
        // Component & Reference
        public Levels         levels { get; protected set; }
        public ISceneDirector scene  { get; protected set; }

        // Setting
        [SerializeField] protected Material  _skybox;
        [SerializeField] protected AudioClip _bgm;

        public Material  skybox { get { return _skybox; } }
        public AudioClip bgm    { get { return _bgm; } }

        // ==============================================================================
        // 3) 메서드
        // ==============================================================================
        protected virtual void Awake() { SetField(); }

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
    }
}
