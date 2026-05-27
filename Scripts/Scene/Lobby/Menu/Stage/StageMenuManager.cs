// //////////////////////////////////////////////////////////////////////////////
// * 요약
//    - 게임의 로비의 스테이지(레벨) 메뉴 클래스
//    - 스테이지-레벨의 2차원 배열 구조
//    - 스테이지 및 레벨의 선택, 변경 기능
//    - 레벨 선택 시 스테이지 씬 진입
//
// * 목차
//    1. 인터페이스 ... Line 29
//    2. 클래스 ....... Line 44
//        1) 필드 ..... Line 52
//        2) 메서드 ... Line 59
//            1- 초기화 ............... Line 62
//            2- 뷰(View) 모드 열기 ... Line 82
//            3- 캐릭터 강화 .......... Line 108
// //////////////////////////////////////////////////////////////////////////////
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using Game;

namespace Lobby
{
    using LevelData = DataManager.Stages.Stage.Levels.Level;
    using SceneType = SceneBase.Type;

    // //////////////////////////////////////////////////////////////////////////////
    // 1. 인터페이스(IMenuBase 인터페이스 상속)
    // //////////////////////////////////////////////////////////////////////////////
    public interface IStageMenuManager : IMenuBase
    {
        // 프로퍼티
        // Component
        new IStageMenuUIController ui { get; }

        // 메서드
        Coroutine ChangeLevel(bool isNext);
        void      StartLevel();
        void      Select(int index);
        void      OpenSelectMenu(bool isActive);
    }

    // //////////////////////////////////////////////////////////////////////////////
    // 2. 클래스(MenuBase 클래스 상속)
    // //////////////////////////////////////////////////////////////////////////////
    public class StageMenuManager : MenuBase, IStageMenuManager
    {
        // ==============================================================================
        // 1) 필드
        // ==============================================================================
        // Component & Reference
        public new IStageMenuUIController ui { get; protected set; }

        // ==============================================================================
        // 2) 메서드
        // ==============================================================================
        // ------------------------------------------------------------------------------
        // 2-1) 메서드 -> 초기화
        // ------------------------------------------------------------------------------
        protected override void SetField()
        {
            base.SetField();

            ui = GetComponentInChildren<IStageMenuUIController>(true);

            type = Type.Stage;
        }

        // ------------------------------------------------------------------------------
        // 2-2) 메서드 -> 열기(Open)
        // ------------------------------------------------------------------------------
        public override Coroutine Open(bool isActive, IMenuBase prev = null)
        {
            IStageController         stage    = scene.stages.current;
            IOutroLauncherController launcher = scene.planet.launchers.outro;

            stage.Display(isActive, ui.defaultDuration);

            if (isActive) launcher.Appear();
            else          launcher.Disappear();

            return base.Open(isActive, prev);
        }

        // ------------------------------------------------------------------------------
        // 2-3) 메서드 -> 레벨
        //    - 좌 또는 우로 현재의 레벨을 변경
        //    - 레벨 시작 시 스테이지 씬 진입
        // ------------------------------------------------------------------------------
        public virtual Coroutine ChangeLevel(bool isNext) { return StartCoroutine(_ChangeLevel(isNext)); }

        protected virtual IEnumerator _ChangeLevel(bool isNext)
        {
            IOutroLauncherController launcher = scene.planet.launchers.outro;
            IStageController         stage    = scene.stages.current;
            IDataManager             data     = GameDirector.instance.data;

            var levelDatas    = data.stages.Current.levels;
            var levelData     = levelDatas.Current;
            var nextLevelData = isNext ? (levelDatas.TryGetNext(out LevelData next)         ? next     : levelDatas.First())
                                       : (levelDatas.TryGetPrevious(out LevelData previous) ? previous : levelDatas.Last());

            levelData.selected     = false;
            nextLevelData.selected = true;

            ui.SetInteractables(false);
            launcher.trajectories[0].Draw(stage.defaultDuration);

            yield return stage.Rotate(isNext);

            ui.SetInteractables(true);
            ui.Set();
        }

        public virtual void StartLevel()
        {
            IAudioController         audio    = GameDirector.instance.audio;
            IPlayerController        player   = scene.player;
            IOutroLauncherController launcher = scene.planet.launchers.outro;

            ui.Display(false);
            audio.PlayGameStart();
            launcher.Transport(player);    // 해당 클래스에서 일정 시간 뒤 스테이지 디렉터를 통해 스테이지 씬 진입 호출
        }

        // ------------------------------------------------------------------------------
        // 2-3) 메서드 -> 스테이지
        //    - 스테이지 리스트의 UI 표시
        //    - 스테이지 선택 시 현재 스테이지 정보를 갱신 후 로비 씬 재진입
        // ------------------------------------------------------------------------------
        public virtual void OpenSelectMenu(bool isActive)
        {
            var root = ui.root;

            root.main.gameObject.SetActive(!isActive);
            root.select.gameObject.SetActive(isActive);
            ui.Set();

            if (isActive) ui.list.Display(true);
        }
        
        public virtual void Select(int index)
        {
            IDataManager data = GameDirector.instance.data;

            var stageDatas    = data.stages;
            var stageData     = stageDatas.Current;
            var nextStageData = stageDatas[index];

            stageData.selected     = false;
            nextStageData.selected = true;

            ui.SetInteractables(false);
            ui.list.SetInteractables(false);
            scene.Exit(SceneType.Lobby);
        }
    }
}
