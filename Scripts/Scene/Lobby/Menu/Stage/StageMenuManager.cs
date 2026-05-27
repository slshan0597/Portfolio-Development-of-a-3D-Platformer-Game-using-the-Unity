using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using Game;


namespace Lobby
{
    using LevelData = DataManager.Stages.Stage.Levels.Level;
    using SceneType = SceneBase.Type;


    public interface IStageMenuManager : IMenuBase
    {
        #region Property

        // Component
        new IStageMenuUIController ui { get; }

        #endregion


        #region Method

        Coroutine ChangeLevel(bool isNext);
        void      StartLevel();
        void      Select(int index);
        void      OpenSelectMenu(bool isActive);

        #endregion
    }


    public class StageMenuManager : MenuBase, IStageMenuManager
    {
        #region Field

        public new IStageMenuUIController ui { get; protected set; }

        #endregion


        #region Method

        #region Initialization

        protected override void SetField()
        {
            base.SetField();

            ui = GetComponentInChildren<IStageMenuUIController>(true);

            type = Type.Stage;
        }

        #endregion


        public override Coroutine Open(bool isActive, IMenuBase prev = null)
        {
            IStageController         stage    = scene.stages.current;
            IOutroLauncherController launcher = scene.planet.launchers.outro;

            stage.Display(isActive, ui.defaultDuration);

            if (isActive) launcher.Appear();
            else          launcher.Disappear();

            return base.Open(isActive, prev);
        }


        #region Level

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
            launcher.Transport(player);
        }

        #endregion


        #region Stage

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

        #endregion


        public virtual void OpenSelectMenu(bool isActive)
        {
            var root = ui.root;

            root.main.gameObject.SetActive(!isActive);
            root.select.gameObject.SetActive(isActive);
            ui.Set();

            if (isActive) ui.list.Display(true);
        }

        #endregion
    }
}
