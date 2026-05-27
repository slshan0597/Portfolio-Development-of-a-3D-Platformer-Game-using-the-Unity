// //////////////////////////////////////////////////////////////////////////////
// * 요약
//    - 레벨 정보 클래스
//    - 필드(플레닛), 체크포인트 등 정보 저장
//
// * 목차
//    1. 인터페이스 ... Line 
//    2. 클래스 ....... Line 
//        1) 내부 타입 ... Line 
//        2) 필드 ........ Line 
//        3) 메서드 ...... Line 
//            1- 초기화 ........... Line 
//            2- 체크포인트 저장 ... Line 
// //////////////////////////////////////////////////////////////////////////////
using System.Collections.Generic;
using UnityEngine;

namespace Stage
{
    using CheckPoints = LevelController.CheckPoints;

    // //////////////////////////////////////////////////////////////////////////////
    // 1. 인터페이스
    // //////////////////////////////////////////////////////////////////////////////
    public interface ILevelController
    {
        // 프로퍼티
        // Component
        GameObject               gameObject  { get; }
        Transform                transform   { get; }
        IIntroLauncherController launcher    { get; }
        IPlanetController[]      planets     { get; }
        CheckPoints              checkPoints { get; }

        // Reference
        IStageController stage { get; }

        // 메서드
        void Initialize();
        void Save(ICheckPointable checkPoint);
    }

    // //////////////////////////////////////////////////////////////////////////////
    // 2. 클래스
    // //////////////////////////////////////////////////////////////////////////////
    public class LevelController : MonoBehaviour, ILevelController
    {
        // ==============================================================================
        // 1) 내부 타입
        // ==============================================================================
        public class CheckPoints : List<ICheckPointable>
        {
            public ICheckPointable current;

            public CheckPoints(Transform transform) : base(transform.GetComponentsInChildren<ICheckPointable>(true)) { }
        }

        // ==============================================================================
        // 2) 필드
        // ==============================================================================
        // Component & Reference
        public IIntroLauncherController launcher    { get; protected set; }
        public IPlanetController[]      planets     { get; protected set; }
        public CheckPoints              checkPoints { get; protected set; }
        public IStageController         stage       { get; protected set; }

        // ==============================================================================
        // 3) 메서드
        // ==============================================================================
        protected virtual void Awake() { SetField(); }

        // ------------------------------------------------------------------------------
        // 3-1) 메서드 -> 초기화
        // ------------------------------------------------------------------------------
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

        // ------------------------------------------------------------------------------
        // 3-2) 메서드 -> 체크포인트 저장
        //    - 현재 도달한 체크포인트 식별 번호와 필드(플래닛)의 정보를 저장
        // ------------------------------------------------------------------------------
        public virtual void Save(ICheckPointable checkPoint)
        {
            IDataManager data = stage.scene.data;

            int index = checkPoints.FindIndex(_checkPoint => _checkPoint == checkPoint);

            checkPoints.current        = checkPoint;
            data.level.checkPointIndex = index;

            for (int i = 0; i < planets.Length; i++) planets[i].Save(i);
        }
    }
}
