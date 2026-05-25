// //////////////////////////////////////////////////////////////////////////////
// * 요약
//    - 게임의 데이터 처리 클래스
//    - 캐릭터 스텟(타입, 강화 횟수, 비용 등)
//    - 스테이지(레벨) 정보(식별, 클리어, 챌린지 달성 등)
//    - 재화(Money) 정보
//
// * 목차
//    1. 인터페이스 ... Line 
//    2. 클래스 ....... Line 
//        1) 내부 타입 ... Line 
//            1- 캐릭터 스텟 ...... Line 
//            2- 스테이지(레벨) ... Line 
//            3- 재화(Money) ..... Line 
//            4- 설정 ............ Line 
//                1_ 캐릭터 스텟 ...... Line 
//                2_ 스테이지(레벨) ... Line 
//                3_ 재화(Money) ..... Line 
//        2) 필드 ..... Line 
//        3) 메서드 ... Line 
//            1- 이벤트 함수 ... Line 
//            2- 초기화 ........ Line 
//            3- 겟(Get) ....... Line 
//            4- 셋(Set) ....... Line 
// //////////////////////////////////////////////////////////////////////////////
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Game
{
    using CharacterStat        = DataManager.CharacterStat;
    using Stages               = DataManager.Stages;
    using Stage                = DataManager.Stages.Stage;
    using Levels               = DataManager.Stages.Stage.Levels;
    using Level                = DataManager.Stages.Stage.Levels.Level;
    using Challenge            = DataManager.Stages.Stage.Levels.Level.Challenge;
    using Money                = DataManager.Money;
    using Setting              = DataManager.Setting;
    using BaseSetting          = DataManager.Setting.Base;
    using CharacterStatSetting = DataManager.Setting.CharacterStat;
    using CharacterStatType    = DataManager.Setting.CharacterStat.Type;
    using StageSetting         = DataManager.Setting.Stage;
    using LevelSetting         = DataManager.Setting.Stage.Level;
    using ChallengeSetting     = DataManager.Setting.Stage.Level.Challenge;
    using ChallengeType        = DataManager.Setting.Stage.Level.Challenge.Type;
    using ComparisonSetting    = DataManager.Setting.Stage.Level.Challenge.Comparison;
    using ComparisonType       = DataManager.Setting.Stage.Level.Challenge.Comparison.Type;
    using MoneySetting         = DataManager.Setting.Money;
    using Save                 = SaveMenuManager.Data;
    using CharacterStatSave    = SaveMenuManager.Data.CharacterStat;
    using BaseSave             = SaveMenuManager.Data.Base;
    using StageSave            = SaveMenuManager.Data.Stage;
    using LevelSave            = SaveMenuManager.Data.Stage.Level;
    using ChallengeSave        = SaveMenuManager.Data.Stage.Level.Challenge;
    using MoneySave            = SaveMenuManager.Data.Money;

    // //////////////////////////////////////////////////////////////////////////////
    // 1. 인터페이스
    // //////////////////////////////////////////////////////////////////////////////
    public interface IDataManager
    {
        // 프로퍼티
        // Reference
        IGameDirector game { get; }

        // Data
        public Dictionary<CharacterStatType, CharacterStat> characterStats { get; }
        public Stages                                       stages         { get; }
        public Money                                        money          { get; }

        // Setting
        Setting setting { get; }

        // 메서드
        Save Get();
        void Set(Save save);
    }

    // //////////////////////////////////////////////////////////////////////////////
    // 2. 클래스
    // //////////////////////////////////////////////////////////////////////////////
    public class DataManager : MonoBehaviour, IDataManager
    {
        // ==============================================================================
        // 1) 내부 타입
        //    - 세이브 데이터(클래스)에서 확장
        // ==============================================================================
        // ------------------------------------------------------------------------------
        // 1-1) 내부 타입 -> 캐릭터 스텟
        // ------------------------------------------------------------------------------
        public class CharacterStat : CharacterStatSave
        {
            // 필드
            public int   maxCount      { get; protected set; }
            public int   maxPercentage { get; protected set; }
            public int   cost          { get; protected set; }
            public float rate          { get { return (maxPercentage * value / (float)maxCount) * 0.01f; } }

            // 생성자
            public CharacterStat(CharacterStatSave _base, int maxCount, int maxPercentage, int cost) : base(_base)
            {
                this.maxCount      = maxCount;
                this.maxPercentage = maxPercentage;
                this.cost          = cost;
            }

            public CharacterStat(CharacterStat other) : base(other)
            {
                maxCount      = other.maxCount;
                maxPercentage = other.maxPercentage;
                cost          = other.cost;
            }
        }

        // ------------------------------------------------------------------------------
        // 1-2) 내부 타입 -> 스테이지(레벨)
        // ------------------------------------------------------------------------------
        public class Base : BaseSave
        {
            // 필드
            public string name { get; protected set; }

            // 생성자
            public Base(BaseSave _base, string name) : base(_base) { this.name = name; }

            public Base(Base other) : base(other) { name = other.name; }
        }

        public class Stages : LinkedList<Stage>
        {
            // 내부 타입 - 스테이지 리스트
            public class Stage : Base
            {
                // 내부 타입 - 스테이지
                public class Levels : LinkedList<Level>
                {
                    // 내부 타입 - 레벨 리스트
                    public class Level : Base
                    {
                        // 내부 타입 - 레벨
                        public class Challenge : ChallengeSave
                        {
                            // 필드 - 챌린지
                            public ComparisonSetting comparison { get; protected set; }
                            public int               reward     { get; protected set; }

                            // 생성자 - 챌린지
                            public Challenge(ChallengeSave _base, ComparisonSetting comparison, int reward) : base(_base)
                            {
                                this.comparison = new ComparisonSetting(comparison);
                                this.reward     = reward;
                            }

                            public Challenge(Challenge other) : base(other)
                            {
                                comparison = new ComparisonSetting(other.comparison);
                                reward     = other.reward;
                            }
                        }

                        // 필드 - 레벨
                        public int                                  reward     { get; protected set; }
                        public Dictionary<ChallengeType, Challenge> challenges { get; protected set; }

                        // 생성자 - 레벨
                        public Level(Base _base, int reward, Dictionary<ChallengeType, Challenge> challenges) : base(_base)
                        {
                            this.reward     = reward;
                            this.challenges = new Dictionary<ChallengeType, Challenge>(challenges);
                        }

                        public Level(Level other) : base(other)
                        {
                            reward     = other.reward;
                            challenges = new Dictionary<ChallengeType, Challenge>(other.challenges);
                        }
                    }

                    // 생성자 - 레벨 리스트
                    public Levels(List<Level> levels) : base(levels) { }

                    public Levels(Levels other) : base(other) { }

                    // 메서드 - 레벨 리스트
                    public Level this[int id] => this.First(element => element.id == id);

                    public Level Current => this.First(element => element.selected);

                    public bool TryGetNext(out Level nextData)
                    {
                        var  node  = Find(Current);
                        var  next  = node.Next;
                        bool exist = next != null;

                        nextData = exist ? next.Value : null;

                        return exist;
                    }

                    public bool TryGetPrevious(out Level prevData)
                    {
                        var  node  = Find(Current);
                        var  prev  = node.Previous;
                        bool exist = prev != null;

                        prevData = exist ? prev.Value : null;

                        return exist;
                    }
                }

                // 필드 - 스테이지
                public Levels levels { get; protected set; }

                // 생성자 - 스테이지
                public Stage(Base _base, Levels levels) : base(_base) { this.levels = levels; }

                public Stage(Stage other) : base(other) { levels = other.levels; }
            }

            // 생성자 - 스테이지 리스트
            public Stages(List<Stage> stages) : base(stages) { }

            public Stages(Stages other) : base(other) { }

            // 메서드 - 스테이지 리스트
            public Stage this[int id] => this.First(element => element.id == id);

            public Stage Current => this.First(element => element.selected);

            public bool TryGetNext(out Stage nextData)
            {
                var  node  = Find(Current);
                var  next  = node.Next;
                bool exist = next != null;

                nextData = exist ? next.Value : null;

                return exist;
            }

            public bool TryGetPrevious(out Stage prevData)
            {
                var  node  = Find(Current);
                var  prev  = node.Previous;
                bool exist = prev != null;

                prevData = exist ? prev.Value : null;

                return exist;
            }
        }

        // ------------------------------------------------------------------------------
        // 1-3) 내부 타입 -> 재화(Money)
        // ------------------------------------------------------------------------------
        public class Money : MoneySave
        {
            // 필드
            public int rewardOfCoin { get; protected set; }

            // 생성자
            public Money(MoneySave _base, int rewardOfCoin) : base(_base) { this.rewardOfCoin = rewardOfCoin; }

            public Money(Money other) : base(other) { rewardOfCoin = other.rewardOfCoin; }
        }

        // ------------------------------------------------------------------------------
        // 1-4) 내부 타입 -> 설정
        //    - 데이터의 식별(이름, 타입 등), 최댓값, 비용 등 설정
        // ------------------------------------------------------------------------------
        [Serializable] public class Setting
        {
            // ******************************************************************************
            // 1-4-1) 내부 타입 -> 설정 -> 캐릭터 스텟
            // ******************************************************************************
            [Serializable] public class CharacterStat
            {
                // 타입
                public enum Type { RunSpeed, JumpForce, AttackCoolDown, PowerUpDuration }

                // 필드
                [SerializeField] protected Type _type;
                [SerializeField] protected int  _maxCount;
                [SerializeField] protected int  _maxPercentage;
                [SerializeField] protected int  _cost;

                public Type type          { get { return _type; } }
                public int  maxCount      { get { return _maxCount; } }
                public int  maxPercentage { get { return _maxPercentage; } }
                public int  cost          { get { return _cost; } }

                // 생성자
                public CharacterStat(Type type, int maxCount, int maxPercentage, int cost)
                {
                    _type          = type;
                    _maxCount      = maxCount;
                    _maxPercentage = maxPercentage;
                    _cost          = cost;
                }

                public CharacterStat(CharacterStat other)
                {
                    _type          = other.type;
                    _maxCount      = other.maxCount;
                    _maxPercentage = other.maxPercentage;
                    _cost          = other.cost;
                }
            }

            // ******************************************************************************
            // 1-4-2) 내부 타입 -> 설정 -> 스테이지(레벨)
            // ******************************************************************************
            [Serializable] public class Base
            {
                // 필드
                [SerializeField] protected int    _id;
                [SerializeField] protected string _name;

                public int    id   { get { return _id; } }
                public string name { get { return _name; } }

                // 생성자
                public Base(int id, string name)
                {
                    _id   = id;
                    _name = name;
                }

                public Base(Base other)
                {
                    _id   = other.id;
                    _name = other.name;
                }
            }

            [Serializable] public class Stage : Base
            {
                // 내부 타입 - 스테이지
                [Serializable] public class Level : Base
                {
                    // 내부 타입 - 레벨
                    [Serializable] public class Challenge
                    {
                        // 내부 타입 - 챌린지
                        public enum Type { Time, Coin, Hit, Attack }

                        [Serializable] public class Comparison
                        {
                            // 내부 타입 - 챌린지 비교
                            public enum Type { Greater, Less, GreaterEqual, LessEqual }

                            // 필드 - 챌린지 비교
                            [SerializeField] protected Type _type;
                            [SerializeField] protected int  _rhs;

                            public Type type { get { return _type; } }
                            public int  rhs  { get { return _rhs; } }

                            // 생성자 - 챌린지 비교
                            public Comparison(Type type, int rhs)
                            {
                                _type = type;
                                _rhs  = rhs;
                            }

                            public Comparison(Comparison other)
                            {
                                _type = other.type;
                                _rhs  = other.rhs;
                            }
                        }

                        // 필드 - 챌린지
                        [SerializeField] protected Type       _type;
                        [SerializeField] protected Comparison _comparison;
                        [SerializeField] protected int        _reward;

                        public Type       type       { get { return _type; } }
                        public Comparison comparison { get { return _comparison; } }
                        public int        reward     { get { return _reward; } }

                        // 생성자 - 챌린지
                        public Challenge(Type type, Comparison comparison, int reward)
                        {
                            _type       = type;
                            _comparison = comparison;
                            _reward     = reward;
                        }

                        public Challenge(Challenge other)
                        {
                            _type       = other.type;
                            _comparison = other.comparison;
                            _reward     = other.reward;
                        }
                    }

                    // 필드 - 레벨
                    [SerializeField] protected int             _reward;
                    [SerializeField] protected List<Challenge> _challenges;

                    public int             reward     { get { return _reward; } }
                    public List<Challenge> challenges { get { return _challenges; } }

                    // 생성자 - 레벨
                    public Level(Base _base, int reward, List<Challenge> challenges) : base(_base)
                    {
                        _reward     = reward;
                        _challenges = new List<Challenge>(challenges);
                    }

                    public Level(Level other) : base(other)
                    {
                        _reward     = other.reward;
                        _challenges = new List<Challenge>(other.challenges);
                    }
                }

                // 필드 - 스테이지
                [SerializeField] protected List<Level> _levels;

                public List<Level> levels { get { return _levels; } }

                // 생성자 - 스테이지
                public Stage(Base _base, List<Level> levels) : base(_base)
                { 
                    _levels = new List<Level>(levels);
                }

                public Stage(Stage other) : base(other) { _levels = new List<Level>(other.levels); }
            }

            // ******************************************************************************
            // 1-4-3) 내부 타입 -> 설정 -> 재화(Money)
            // ******************************************************************************
            [Serializable] public class Money
            {
                // 필드
                [SerializeField] protected int _rewardOfCoin;

                public int rewardOfCoin { get { return _rewardOfCoin; } }

                // 생성자
                public Money(int rewardOfCoin) { _rewardOfCoin = rewardOfCoin; }

                public Money(Money other) { _rewardOfCoin = other.rewardOfCoin; }
            }

            // 필드
            [SerializeField] protected List<CharacterStat> _characterStats;
            [SerializeField] protected List<Stage>         _stages;
            [SerializeField] protected Money               _money;

            public List<CharacterStat> characterStats { get { return _characterStats; } }
            public List<Stage>         stages         { get { return _stages; } }
            public Money               money          { get { return _money; } }

            // 생성자
            public Setting(List<CharacterStat> characterStats, List<Stage> stages, Money money)
            {
                _characterStats = new List<CharacterStat>(characterStats);
                _stages         = new List<Stage>(stages);
                _money          = new Money(money);
            }

            public Setting(Setting other)
            {
                _characterStats = new List<CharacterStat>(other.characterStats);
                _stages         = new List<Stage>(other.stages);
                _money          = new Money(other.money);
            }
        }

        // ==============================================================================
        // 2) 필드
        // ==============================================================================
        // Component & Reference
        public IGameDirector game { get; protected set; }

        // Data
        public Dictionary<CharacterStatType, CharacterStat> characterStats { get; protected set; }
        public Stages                                       stages         { get; protected set; }
        public Money                                        money          { get; protected set; }

        // Setting
        [SerializeField] protected Setting _setting;

        public Setting setting { get { return _setting; } }

        // ==============================================================================
        // 3) 메서드
        // ==============================================================================
        // ------------------------------------------------------------------------------
        // 3-1) 메서드 -> 이벤트 함수
        // ------------------------------------------------------------------------------
        protected virtual void Awake() { SetField(); }

        protected virtual void Reset() { ResetField(); }

        // ------------------------------------------------------------------------------
        // 3-2) 메서드 -> 초기화
        // ------------------------------------------------------------------------------
        protected virtual void SetField() { game = GetComponentInParent<IGameDirector>(true); }

        protected virtual void ResetField()
        {
            _setting = new Setting(
                new List<CharacterStatSetting>()
                {
                    new CharacterStatSetting(CharacterStatType.RunSpeed,        5, 20, 500),
                    new CharacterStatSetting(CharacterStatType.JumpForce,       5, 10, 500),
                    new CharacterStatSetting(CharacterStatType.AttackCoolDown,  5, 50, 500),
                    new CharacterStatSetting(CharacterStatType.PowerUpDuration, 5, 20, 500)
                },
                new List<StageSetting>()
                {
                    new StageSetting(
                        new BaseSetting(1, "Stage 01"), new List<LevelSetting>()
                        {
                            new LevelSetting(
                                new BaseSetting(1, "Level 01"), 1000, new List<ChallengeSetting>()
                                {
                                    new ChallengeSetting(ChallengeType.Time, new ComparisonSetting(ComparisonType.Less,         10), 100),
                                    new ChallengeSetting(ChallengeType.Coin, new ComparisonSetting(ComparisonType.GreaterEqual, 10), 100),
                                    new ChallengeSetting(ChallengeType.Hit,  new ComparisonSetting(ComparisonType.LessEqual,    5),  100)
                                }),
                            new LevelSetting(
                                new BaseSetting(2, "Level 02"), 1200, new List<ChallengeSetting>()
                                {
                                    new ChallengeSetting(ChallengeType.Time,   new ComparisonSetting(ComparisonType.Less,      9), 200),
                                    new ChallengeSetting(ChallengeType.Hit,    new ComparisonSetting(ComparisonType.Less,      5), 200),
                                    new ChallengeSetting(ChallengeType.Attack, new ComparisonSetting(ComparisonType.LessEqual, 5), 200)
                                })
                        }),
                    new StageSetting(
                        new BaseSetting(2, "Stage 02"), new List<LevelSetting>()
                        {
                            new LevelSetting(
                                new BaseSetting(1, "Level 01"), 1400, new List<ChallengeSetting>()
                                {
                                    new ChallengeSetting(ChallengeType.Time, new ComparisonSetting(ComparisonType.Less,         8),  300),
                                    new ChallengeSetting(ChallengeType.Coin, new ComparisonSetting(ComparisonType.GreaterEqual, 10), 300),
                                    new ChallengeSetting(ChallengeType.Hit,  new ComparisonSetting(ComparisonType.LessEqual,    4),  300)
                                }),
                            new LevelSetting(
                                new BaseSetting(2, "Level 02"), 1600, new List<ChallengeSetting>()
                                {
                                    new ChallengeSetting(ChallengeType.Time,   new ComparisonSetting(ComparisonType.Less, 7), 400),
                                    new ChallengeSetting(ChallengeType.Hit,    new ComparisonSetting(ComparisonType.Less, 4), 400),
                                    new ChallengeSetting(ChallengeType.Attack, new ComparisonSetting(ComparisonType.Less, 5), 400)
                                })
                        })
                },
                new MoneySetting(10));
        }

        // ------------------------------------------------------------------------------
        // 3-3) 메서드 -> 겟(Get)
        //    - 데이터를 추출하여 세이브(데이터) 타입으로 가공
        // ------------------------------------------------------------------------------
        public virtual Save Get()
        {
            var characterStats = Extract(this.characterStats);
            var stages         = Extract(this.stages);
            var money          = new MoneySave(this.money);

            return new Save(characterStats, stages, money);
        }

        // ******************************************************************************
        // 3-3-1) 메서드 -> 겟(Get) -> 캐릭터 스텟
        // ******************************************************************************
        protected virtual List<CharacterStatSave> Extract(Dictionary<CharacterStatType, CharacterStat> datas)
        {
            var saves = new List<CharacterStatSave>();

            foreach (var data in datas.Values)
            {
                var save = new CharacterStatSave(data);

                saves.Add(new CharacterStatSave(save));
            }

            return saves;
        }

        // ******************************************************************************
        // 3-3-2) 메서드 -> 겟(Get) -> 스테이지(레벨)
        // ******************************************************************************
        protected virtual List<StageSave> Extract(Stages datas)
        {
            var saves = new List<StageSave>();

            foreach (var data in datas)
            {
                var save = _Extract(data);

                saves.Add(new StageSave(save));
            }

            return saves;
        }

        protected virtual StageSave _Extract(Stage data)
        {
            var levels = __Extract(data.levels);

            return new StageSave(data, levels);
        }

        protected virtual List<LevelSave> __Extract(Levels datas)
        {
            var saves = new List<LevelSave>();

            foreach (var data in datas)
            {
                var save = ___Extract(data);

                saves.Add(new LevelSave(save));
            }

            return saves;
        }

        protected virtual LevelSave ___Extract(Level data)
        {
            var challenges = ____Extract(data.challenges);

            return new LevelSave(data, challenges);
        }

        protected virtual List<ChallengeSave> ____Extract(Dictionary<ChallengeType, Challenge> datas)
        {
            var saves = new List<ChallengeSave>();

            foreach (var data in datas.Values)
            {
                var save = new ChallengeSave(data);

                saves.Add(new ChallengeSave(save));
            }

            return saves;
        }

        // ------------------------------------------------------------------------------
        // 3-4) 메서드 -> 셋(Set)
        //    - 세이브(데이터)를 가져와 설정(데이터)와 병합
        // ------------------------------------------------------------------------------
        public virtual void Set(Save save)
        {
            characterStats = Combine(setting.characterStats, save?.characterStats);
            stages         = Combine(setting.stages,         save?.stages);
            money          = Combine(setting.money,          save?.money);
        }

        // ******************************************************************************
        // 3-4-1) 메서드 -> 셋(Set) -> 캐릭터 스텟
        // ******************************************************************************
        protected virtual Dictionary<CharacterStatType, CharacterStat> Combine(List<CharacterStatSetting> settings,
            List<CharacterStatSave> saves)
        {
            var datas = new Dictionary<CharacterStatType, CharacterStat>();

            foreach (var setting in settings)
            {
                CharacterStatType type = setting.type;
                var               save = ((saves != null) && saves.Exists(save => save.type == type))
                                       ? saves.Find(save => save.type == type) : new CharacterStatSave(type, 0);
                var               data = new CharacterStat(save, setting.maxCount, setting.maxPercentage, setting.cost);

                datas.Add(type, data);
            }

            return datas;
        }

        // ******************************************************************************
        // 3-4-2) 메서드 -> 셋(Set) -> 스테이지(레벨)
        // ******************************************************************************
        protected virtual Stages Combine(List<StageSetting> settings, List<StageSave> saves)
        {
            var   datas    = new List<Stage>();
            Stage prevData = null;

            foreach (var setting in settings)
            {
                int id       = setting.id;
                var save     = saves?.Find(save => save.id == id);
                var data     = _Combine(setting, save, prevData);
                    prevData = data;

                datas.Add(data);
            }

            return new Stages(datas);
        }

        protected virtual Stage _Combine(StageSetting setting, StageSave save, Stage prevData)
        {
            int id       = setting.id;
            var baseSave = (save != null) ? save
                                          : ((prevData != null) ? new BaseSave(id, prevData.cleared, false, false) 
                                                                : new BaseSave(id, true,             true,  false));
            var _base    = new Base(baseSave, setting.name);
            var levels   = __Combine(setting.levels, save?.levels);

            return new Stage(_base, levels);
        }

        protected virtual Levels __Combine(List<LevelSetting> settings, List<LevelSave> saves)
        {
            var   datas    = new List<Level>();
            Level prevData = null;

            foreach (var setting in settings)
            {
                int id       = setting.id;
                var save     = saves?.Find(save =>save.id == id);
                var data     = ___Combine(setting, save, prevData);
                    prevData = data;

                datas.Add(data);
            }

            return new Levels(datas);
        }

        protected virtual Level ___Combine(LevelSetting setting, LevelSave save, Level prevData)
        {
            int id         = setting.id;
            var baseSave   = (save != null) ? save
                                            : ((prevData != null) ? new BaseSave(id, prevData.cleared, false, false)
                                                                  : new BaseSave(id, true,             true,  false));
            var _base      = new Base(baseSave, setting.name);
            var challenges = ____Combine(setting.challenges, save?.challenges);

            return new Level(_base, setting.reward, challenges);
        }

        protected virtual Dictionary<ChallengeType, Challenge> ____Combine(List<ChallengeSetting> settings,
            List<ChallengeSave> saves)
        {
            var datas = new Dictionary<ChallengeType, Challenge>();

            foreach (var setting in settings)
            {
                ChallengeType type = setting.type;
                var           save = ((saves != null) && saves.Exists(save => save.type == type))
                                   ? saves.Find(save => save.type == type) : new ChallengeSave(type, false);
                var           data = new Challenge(save, setting.comparison, setting.reward);

                datas.Add(type, data);
            }

            return datas;
        }

        protected virtual Money Combine(MoneySetting setting, MoneySave save)
        {
            var _base = (save != null) ? save : new MoneySave(0);

            return new Money(_base, setting.rewardOfCoin);
        }
    }
}
