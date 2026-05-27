// //////////////////////////////////////////////////////////////////////////////
// * 요약
//    - 스코어(Score) 정보 저장 클래스
//    - 경과 시간, 도전과제(Challenge)와 그에 따른 보상(Reward) 정보 저장 및 갱신
//
// * 목차
//    1. 인터페이스 ... Line 
//    2. 클래스 ....... Line 
//        1) 필드 ..... Line 
//        2) 메서드 ... Line 
//            1- 이벤트 함수 ... Line 
//            2- 초기화 ........ Line 
//            3- 셋(Set) ....... Line 
//            4- 데이터 ........ Line 
//                1_ 불러오기(Load) .... Line 
//                2_ 저장하기(Save) .... Line 
//                3_ 기록하기(Write) ... Line 
// //////////////////////////////////////////////////////////////////////////////
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

    // //////////////////////////////////////////////////////////////////////////////
    // 1. 인터페이스
    // //////////////////////////////////////////////////////////////////////////////
    public interface IScoreManager
    {
        // 프로퍼티
        // Component
        Stopwatch stopwatch { get; }

        // Reference
        ISceneDirector scene { get; }

        // Data
        TimeSpan                             elapsedTime { get; }
        int                                  coin        { get; }
        Dictionary<ChallengeType, int>       challenges  { get; }
        Dictionary<RewardConditionType, int> rewards     { get; }

        // 메서드
        void IncreaseCoin(int amount = 1);
        void TryIncrease(ChallengeType type, int amount = 1);
        void Load();
        void Save();
        void Write();
    }

    // //////////////////////////////////////////////////////////////////////////////
    // 2. 클래스
    // //////////////////////////////////////////////////////////////////////////////
    public class ScoreManager : MonoBehaviour, IScoreManager
    {
        public enum RewardConditionType { FirstClear, Challenge, Coin }

        // ==============================================================================
        // 1) 필드
        // ==============================================================================
        // Component & Reference
        public Stopwatch      stopwatch { get; protected set; }
        public ISceneDirector scene     { get; protected set; }

        // Data
        public TimeSpan                             elapsedTime { get { return savedTime + stopwatch.Elapsed; } }
        public int                                  coin        { get; protected set; }
        public Dictionary<ChallengeType, int>       challenges  { get; protected set; }
        public Dictionary<RewardConditionType, int> rewards     { get; protected set; }

        // etc.
        protected TimeSpan savedTime, prevElapsedTime;

        // ==============================================================================
        // 2) 메서드
        // ==============================================================================
        // ------------------------------------------------------------------------------
        // 2-1) 메서드 -> 이벤트 함수
        // ------------------------------------------------------------------------------
        protected virtual void Awake() { SetField(); }

        protected virtual void Update()
        {
            if (elapsedTime.Minutes != prevElapsedTime.Minutes) TryIncrease(ChallengeType.Time);

            prevElapsedTime = elapsedTime;
        }

        // ------------------------------------------------------------------------------
        // 2-2) 메서드 -> 초기화
        // ------------------------------------------------------------------------------
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

        // ------------------------------------------------------------------------------
        // 2-3) 메서드 -> 셋(Set)
        //    - 이벤트에 따른 스코어 정보 갱신
        // ------------------------------------------------------------------------------
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

        // ------------------------------------------------------------------------------
        // 2-4) 메서드 -> 데이터
        // ------------------------------------------------------------------------------
        // ******************************************************************************
        // 2-4-1) 메서드 -> 데이터 -> 불러오기(Load)
        //    - 스테이지 실패 후 스테이지 씬 재진입 시 임시로 저장된 스코어 정보를 불러옴
        // ******************************************************************************
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

        // ******************************************************************************
        // 2-4-2) 메서드 -> 데이터 -> 저장하기(Save)
        //    - 체크포인트 도달 시 현재 스코어 정보를 임시로 저장
        // ******************************************************************************
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

        // ******************************************************************************
        // 2-4-3) 메서드 -> 데이터 -> 기록하기(Write)
        //    - 게임 클리어 시 스코어 정보를 게임의 현재 데이터에 기록
        // ******************************************************************************
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
    }
}
