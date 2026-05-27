// //////////////////////////////////////////////////////////////////////////////
// * 요약
//    - 스테이지 씬 전용 확장 클래스
//    - 보상(Rewardable)과 체크포인트(CheckPointable) 속성 추가
//
// * 목차
//    1. 클래스
//        1) 필드 ..... Line 26
//        2) 메서드 ... Line 39
// //////////////////////////////////////////////////////////////////////////////
using System.Collections;
using UnityEngine;

namespace Stage
{
    public interface ILauncherController : global::ILauncherController { }

    // //////////////////////////////////////////////////////////////////////////////
    // 1. 클래스(global::LauncherController 클래스 상속)
    //    - Rewardable     : 필드(플래닛) 클리어 시 활성화 되는 속성
    //    - CheckPointable : 이벤트 발생 시 체크포인트 저장 속성
    // //////////////////////////////////////////////////////////////////////////////
    public class LauncherController : global::LauncherController, ILauncherController, IRewardable, ICheckPointable
    {
        // ==============================================================================
        // 1) 필드
        // ==============================================================================
        // Component & Reference
        public ICharacterTargetController characterTarget { get; protected set; }
        public IPlanetController          planet          { get; protected set; }
        public ILevelController           level           { get; protected set; }

        // Setting
        [SerializeField] protected bool _useCheckPoint;

        public bool useCheckPoint { get { return _useCheckPoint; } }

        // ==============================================================================
        // 2) 메서드
        //    - 플레이어 이동 후에 체크포인트 기능을 활성화하여 현재 레벨 정보 저장
        // ==============================================================================
        protected override void SetField()
        {
            base.SetField();

            characterTarget = GetComponentInChildren<ICharacterTargetController>(true);
            planet          = GetComponentInParent<IPlanetController>(true);
            level           = GetComponentInParent<ILevelController>(true);
        }

        protected override IEnumerator _Transport(global::IPlayerController player)
        {
            ISceneDirector scene = level.stage.scene;

            yield return base._Transport(player);

            if (useCheckPoint) scene.SaveLevel(this);
        }
    }
}
