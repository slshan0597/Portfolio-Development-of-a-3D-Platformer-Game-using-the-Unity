// //////////////////////////////////////////////////////////////////////////////
// * 요약
//    - 게임의 세이브 데이터의 저장 및 불러오기
//
// * 목차
//    1. 인터페이스 ... Line 
//    2. 클래스 ....... Line 
// //////////////////////////////////////////////////////////////////////////////
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace Game
{
    using Data              = SaveMenuManager.Data;
    using PathSetting       = SaveMenuManager.PathSetting;
    using CharacterStatType = DataManager.Setting.CharacterStat.Type;
    using ChallengeType     = DataManager.Setting.Stage.Level.Challenge.Type;
    using SceneType         = SceneBase.Type;

    // //////////////////////////////////////////////////////////////////////////////
    // 1. 인터페이스(IMenuBase 인터페이스 상속)
    // //////////////////////////////////////////////////////////////////////////////
    public interface ISaveMenuManager : IMenuBase
    {
        // 프로퍼티
        // Component
        new ISaveMenuUIController ui { get; }

        // Data
        Dictionary<int, Data> datas { get; }

        // Setting
        PathSetting pathSetting { get; }

        // 메서드
        void Load(int index);
        void Save(int index);
    }

    // //////////////////////////////////////////////////////////////////////////////
    // 2. 클래스(MenuBase 클래스 상속)
    // //////////////////////////////////////////////////////////////////////////////
    public class SaveMenuManager : MenuBase, ISaveMenuManager
    {
        // ==============================================================================
        // 1) 내부 타입
        // ==============================================================================
        // ------------------------------------------------------------------------------
        // 1-1) 내부 타입 -> 데이터
        //    - 파일 형식으로 저장
        // ------------------------------------------------------------------------------
        [Serializable] public class Data
        {
            // ******************************************************************************
            // 1-1-1) 내부 타입 -> 데이터 -> 캐릭터 스텟
            // ******************************************************************************
            [Serializable] public class CharacterStat
            {
                // 필드
                public CharacterStatType type;
                public int               value;

                // 생성자
                public CharacterStat(CharacterStatType type, int value)
                {
                    this.type  = type;
                    this.value = value;
                }

                public CharacterStat(CharacterStat other)
                {
                    type  = other.type;
                    value = other.value;
                }
            }

            // ******************************************************************************
            // 1-1-2) 내부 타입 -> 데이터 -> 스테이지(레벨)
            // ******************************************************************************
            [Serializable] public class Base
            {
                // 필드
                public int  id;
                public bool playable;
                public bool selected;
                public bool cleared;

                // 생성자
                public Base(int id, bool playable, bool selected, bool cleared)
                {
                    this.id       = id;
                    this.playable = playable;
                    this.selected = selected;
                    this.cleared  = cleared;
                }

                public Base(Base other)
                {
                    id       = other.id;
                    playable = other.playable;
                    selected = other.selected;
                    cleared  = other.cleared;
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
                        // 필드 - 챌린지
                        public ChallengeType type;
                        public bool          cleared;

                        // 생성자 - 챌린지
                        public Challenge(ChallengeType type, bool cleared)
                        { 
                            this.type    = type;
                            this.cleared = cleared;
                        }

                        public Challenge(Challenge other)
                        {
                            type    = other.type;
                            cleared = other.cleared;
                        }
                    }

                    // 필드 - 레벨
                    public List<Challenge> challenges;

                    // 생성자 - 레벨
                    public Level(Base _base, List<Challenge> challenges) : base(_base)
                    {
                        this.challenges = new List<Challenge>(challenges);
                    }

                    public Level(Level other) : base(other)
                    {
                        challenges = new List<Challenge>(other.challenges);
                    }
                }

                // 필드 - 스테이지
                public List<Level> levels;

                // 생성자 - 스테이지
                public Stage(Base _base, List<Level> levels) : base(_base) { this.levels = new List<Level>(levels); }

                public Stage(Stage other) : base(other) { levels = new List<Level>(other.levels); }
            }

            // ******************************************************************************
            // 1-1-3) 내부 타입 -> 데이터 -> 재화(Money)
            // ******************************************************************************
            [Serializable] public class Money
            {
                // 필드
                public int value;

                // 생성자
                public Money(int value) { this.value = value; }

                public Money(Money other) { value = other.value; }
            }

            // ******************************************************************************
            // 1-1-4) 내부 타입 -> 데이터 -> 기록(Record)
            //    - 시간에 대한 기록
            // ******************************************************************************
            [Serializable] public class Record
            {
                // 필드
                public string dateSaved;
                public string runTime;

                // 생성자
                public Record()
                {
                    dateSaved = DateTime.Now.ToString();
                    runTime   = TimeSpan.Zero.ToString();
                }

                public Record(DateTime dateSaved, TimeSpan runTime)
                {
                    this.dateSaved = dateSaved.ToString();
                    this.runTime   = runTime.ToString();
                }

                public Record(Record other)
                {
                    dateSaved = other.dateSaved;
                    runTime   = other.runTime;
                }
            }

            // 필드 - 데이터
            public List<CharacterStat> characterStats;
            public List<Stage>         stages;
            public Money               money;
            public Record              record;

            // 생성자 - 데이터
            public Data(List<CharacterStat> characterStats, List<Stage> stages, Money money)
            {
                this.characterStats = new List<CharacterStat>(characterStats);
                this.stages         = new List<Stage>(stages);
                this.money          = new Money(money);
                record              = new Record();
            }

            public Data(List<CharacterStat> characterStats, List<Stage> stages, Money money, Record record)
            {
                this.characterStats = new List<CharacterStat>(characterStats);
                this.stages         = new List<Stage>(stages);
                this.money          = new Money(money);
                this.record         = new Record(record);
            }

            public Data(Data other)
            {
                characterStats = new List<CharacterStat>(other.characterStats);
                stages         = new List<Stage>(other.stages);
                money          = new Money(other.money);
                record         = new Record(other.record);
            }
        }

        // ------------------------------------------------------------------------------
        // 1-2) 내부 타입 -> 저장 경로
        // ------------------------------------------------------------------------------
        [Serializable] public struct PathSetting
        {
            // 필드
            [SerializeField] private string _directoryName;
            [SerializeField] private string _fileName;

            public string directoryName { get { return _directoryName; } }
            public string fileName      { get { return _fileName; } }

            // 생성자
            public PathSetting(string directoryName, string fileName)
            {
                _directoryName = directoryName;
                _fileName      = fileName;
            }

            // 메서드
            public string GetDirectory() => Path.Combine(Application.persistentDataPath, directoryName);

            public string GetPath(int index) => Path.Combine(GetDirectory(), $"{fileName}{index.ToString("00")}");
        }

        // ==============================================================================
        // 2) 필드
        // ==============================================================================
        // Component & Reference
        public new ISaveMenuUIController ui { get; protected set; }

        // Data
        public Dictionary<int, Data> datas { get; protected set; }

        // Setting
        [SerializeField] protected PathSetting _pathSetting;

        public PathSetting pathSetting { get { return _pathSetting; } }

        // etc.
        protected DateTime dateUpdated, dateUpdatedAuto;
        protected TimeSpan elapsedTime, elapsedTimeAuto;

        // ==============================================================================
        // 3) 메서드
        // ==============================================================================
        // ------------------------------------------------------------------------------
        // 3-1) 메서드 -> 이벤트 함수
        // ------------------------------------------------------------------------------
        protected override void Awake()
        {
            base.Awake();

            datas = ReadFiles();
        }

        protected virtual void Reset() { ResetField(); }

        // ------------------------------------------------------------------------------
        // 3-2) 메서드 -> 초기화
        // ------------------------------------------------------------------------------
        protected override void SetField()
        {
            base.SetField();

            ui   = GetComponentInChildren<ISaveMenuUIController>(true);
            type = Type.Save;
        }

        protected virtual void ResetField() { _pathSetting = new PathSetting("Save", "Data"); }

        // ------------------------------------------------------------------------------
        // 3-3) 메서드 -> 데이터
        // ------------------------------------------------------------------------------
        // ******************************************************************************
        // 3-3-1) 메서드 -> 데이터 -> 불러오기(Load)
        //    - 지정된 경로에서 Json 형식의 파일들을 불러와 데이터 리스트에 적재
        //    - Load 메뉴에서 해당 데이터를 불러옴과 동시에 시간 기록 시작
        // ******************************************************************************
        protected virtual Dictionary<int, Data> ReadFiles()
        {
            var datas = new Dictionary<int, Data>();

            for (int index = 0; index <= 30; index++)
            {
                string path = pathSetting.GetPath(index);

                if (!File.Exists(path)) continue;

                using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read))
                {
                    byte[] dataBytes = new byte[(int)fs.Length];

                    fs.Read(dataBytes, 0, (int)fs.Length);

                    string dataStr = Encoding.UTF8.GetString(dataBytes);

                    try   { datas.Add(index, JsonUtility.FromJson<Data>(dataStr)); }
                    catch { }
                }
            }

            return datas;
        }

        public virtual void Load(int index)
        {
            IDataManager data       = game.data;
            ISceneBase   titleScene = game.scenes.current.Value;

            data.Set(datas.ContainsKey(index) ? datas[index] : null);
            titleScene.Exit((index >= 0) ? SceneType.Lobby : SceneType.Tutorial);

            dateUpdated = dateUpdatedAuto = DateTime.Now;
            elapsedTime = elapsedTimeAuto = datas.ContainsKey(index) 
                ? (TimeSpan.TryParse(datas[index].record.runTime, out TimeSpan value) ? value : TimeSpan.Zero) 
                : TimeSpan.Zero;
        }

        // ******************************************************************************
        // 3-3-2) 메서드 -> 데이터 -> 저장하기(Save)
        //    - 해당 데이터를 지정된 경로에 Json 형식의 파일로 저장
        //    - 저장 전 기록한 시간도 추가
        // ******************************************************************************
        public virtual void Save(int index)
        {
            IDataManager        data     = game.data;
            INoticeUIController noticeUI = game.ui.notice;

            Data     current            = data.Get();
            TimeSpan prevElapsedTime    = (index > 0) ? elapsedTime : elapsedTimeAuto;
            TimeSpan currentElapsedTime = DateTime.Now - ((index > 0) ? dateUpdated : dateUpdatedAuto);
            TimeSpan _elapsedTime       = prevElapsedTime + currentElapsedTime;

            current.record.runTime = _elapsedTime.ToString(@"hh\:mm");

            if (datas.ContainsKey(index)) datas[index] = new Data(current);
            else                          datas.Add(index, new Data(current));

            WriteFile(index);

            if (index > 0)
            {
                ISaveListUIController listUI = ui.list;

                listUI.Set();
                noticeUI.Display("Data Saved", ui, listUI);

                dateUpdated = DateTime.Now;
                elapsedTime = _elapsedTime;
            }
            else
            {
                dateUpdatedAuto = DateTime.Now;
                elapsedTimeAuto = _elapsedTime;
            }
        }

        protected virtual void WriteFile(int index)
        {
            DirectoryInfo dirInfo = new DirectoryInfo(pathSetting.GetDirectory());

            if (!dirInfo.Exists) dirInfo.Create();

            using (FileStream fs = new FileStream(pathSetting.GetPath(index), FileMode.Create, FileAccess.Write))
            {
                string dataStr   = JsonUtility.ToJson(datas[index]);
                byte[] dataBytes = Encoding.UTF8.GetBytes(dataStr);

                fs.Write(dataBytes, 0, dataBytes.Length);
            }
        }
    }
}
