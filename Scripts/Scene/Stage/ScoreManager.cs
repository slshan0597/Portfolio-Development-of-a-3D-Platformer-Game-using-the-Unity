using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using Game;


namespace Stage
{
    using RewardConditionType = ScoreManager.RewardConditionType;
    using StageData           = Game.DataManager.Stages.Stage;
    using LevelData           = Game.DataManager.Stages.Stage.Levels.Level;
    using ChallengeData       = Game.DataManager.Stages.Stage.Levels.Level.Challenge;
    using ChallengeType       = Game.DataManager.Setting.Stage.Level.Challenge.Type;
    using ComparisonType      = Game.DataManager.Setting.Stage.Level.Challenge.Comparison.Type;
    using MoneyData           = SaveMenuManager.Data.Money;
    using MainUICountType     = MainUIController.Root.Main.Count.Type;


    public interface IScoreManager
    {
        #region Property

        // Component
        Stopwatch stopwatch { get; }

        // Reference
        ISceneDirector scene { get; }

        // Data
        TimeSpan                             elapsedTime { get; }
        int                                  coin        { get; }
        Dictionary<ChallengeType, int>       challenges  { get; }
        Dictionary<RewardConditionType, int> rewards     { get; }

        #endregion


        #region Method

        void IncreaseCoin(int amount = 1);
        void TryIncrease(ChallengeType type, int amount = 1);
        void Load();
        void Save();
        void Write();

        #endregion
    }


    public class ScoreManager : MonoBehaviour, IScoreManager
    {
        #region Definition

        public enum RewardConditionType { FirstClear, Challenge, Coin }

        #endregion


        #region Field

        public Stopwatch      stopwatch { get; protected set; }
        public ISceneDirector scene     { get; protected set; }

        public TimeSpan                             elapsedTime { get { return savedTime + stopwatch.Elapsed; } }
        public int                                  coin        { get; protected set; }
        public Dictionary<ChallengeType, int>       challenges  { get; protected set; }
        public Dictionary<RewardConditionType, int> rewards     { get; protected set; }

        protected TimeSpan savedTime, prevElapsedTime;

        #endregion


        #region Method

        #region Event

        protected virtual void Awake() { SetField(); }

        protected virtual void Update()
        {
            if (elapsedTime.Minutes != prevElapsedTime.Minutes) TryIncrease(ChallengeType.Time);

            prevElapsedTime = elapsedTime;
        }

        #endregion


        #region Initialization

        protected virtual void SetField()
        {
            Game.IDataManager data = GameDirector.instance.data;

            stopwatch = new Stopwatch();
            scene     = GetComponentInParent<ISceneDirector>(true);

            challenges = new Dictionary<ChallengeType,       int>();
            rewards    = new Dictionary<RewardConditionType, int>();

            var challengeDatas = data.stages.Current.levels.Current.challenges;

            foreach (ChallengeType       type in challengeDatas.Keys)                         challenges.Add(type, 0);
            foreach (RewardConditionType type in Enum.GetValues(typeof(RewardConditionType))) rewards.Add(type, 0);
        }

        #endregion


        public virtual void IncreaseCoin(int amount = 1)
        {
            IMainUIController ui = scene.ui.main;

            coin += amount;

            ui.SetContent(MainUICountType.Coin, coin, amount);
            TryIncrease(ChallengeType.Coin, amount);
        }

        public virtual void TryIncrease(ChallengeType type, int amount = 1)
        {
            if (!challenges.ContainsKey(type)) return;

            IMainUIController ui = scene.ui.main;

            challenges[type] += amount;

            ui.SetContent(type, challenges[type]);
        }


        #region Data

        public virtual void Load()
        {
            IDataManager data = scene.data;

            var scoreData = data.score;

            savedTime = prevElapsedTime = scoreData.elapsedTime;
            coin      = scoreData.coin;

            foreach (var element in scoreData.challenges)
            {
                ChallengeType type = element.Key;

                challenges[type] = element.Value;
            }
        }

        public virtual void Save()
        {
            IDataManager data = scene.data;

            var scoreData = data.score;

            scoreData.elapsedTime = elapsedTime;
            scoreData.coin        = coin;

            foreach (var element in challenges)
            {
                ChallengeType type = element.Key;

                scoreData.challenges[type] = element.Value;
            }
        }

        public virtual void Write()
        {
            IGameDirector     game     = GameDirector.instance;
            Game.IDataManager data     = game.data;
            ISaveMenuManager  saveMenu = game.menu.save;

            stopwatch.Stop();

            var stageDatas     = data.stages;
            var stageData      = stageDatas.Current;
            var levelDatas     = stageDatas.Current.levels;
            var levelData      = levelDatas.Current;
            var challengeDatas = levelData.challenges;
            var moneyData      = data.money;

            if (!levelData.cleared) rewards[RewardConditionType.FirstClear] = levelData.reward;
                                    rewards[RewardConditionType.Coin]       = coin * moneyData.rewardOfCoin;

            levelData.cleared = true;

            foreach (var challengeData in challengeDatas.Values)
            {
                if (challengeData.cleared) continue;

                _Write(challengeData, moneyData);
            }

            foreach (var reward in rewards.Values) moneyData.value += reward;

            if (levelDatas.TryGetNext(out LevelData nextLevelData)) nextLevelData.playable = true;
            else
            {
                stageData.cleared = true;

                if (stageDatas.TryGetNext(out StageData nextStageData))
                {
                    nextStageData.playable = true;
                    nextLevelData          = nextStageData.levels.First();
                    nextLevelData.playable = true;
                }
            }

            saveMenu.Save(0);
        }

        protected virtual void _Write(ChallengeData challengeData, MoneyData moneyData)
        {
            IMainUIController ui = scene.ui.main;

            ChallengeType type = challengeData.type;

            ui.SetContent(type, challenges[type]);

            var  comparison = challengeData.comparison;
            int  rhs        = comparison.rhs;
            bool cleared    = false;
            int  score      = challenges[type];

            switch (comparison.type)
            {
                case ComparisonType.Greater:      cleared = (score >  rhs); break;
                case ComparisonType.GreaterEqual: cleared = (score >= rhs); break;
                case ComparisonType.Less:         cleared = (score <  rhs); break;
                case ComparisonType.LessEqual:    cleared = (score <= rhs); break;
            }

            challengeData.cleared = cleared;

            if (challengeData.cleared) rewards[RewardConditionType.Challenge] += challengeData.reward;
        }

        #endregion

        #endregion
    }
}
