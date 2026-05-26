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


    public interface ISaveMenuManager : IMenuBase
    {
        #region Property

        // Component
        new ISaveMenuUIController ui { get; }

        // Data
        Dictionary<int, Data> datas { get; }

        // Setting
        PathSetting pathSetting { get; }

        #endregion


        #region Method

        void Load(int index);
        void Save(int index);

        #endregion
    }


    public class SaveMenuManager : MenuBase, ISaveMenuManager
    {
        #region Definition

        [Serializable] public class Data
        {
            #region Definition

            [Serializable] public class CharacterStat
            {
                #region Field

                public CharacterStatType type;
                public int               value;

                #endregion


                #region Constructor

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

                #endregion
            }


            [Serializable] public class Base
            {
                #region Field

                public int  id;
                public bool playable;
                public bool selected;
                public bool cleared;

                #endregion


                #region Constructor

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

                #endregion
            }


            [Serializable] public class Stage : Base
            {
                #region Definition

                [Serializable] public class Level : Base
                {
                    #region Definition

                    [Serializable] public class Challenge
                    {
                        #region Field

                        public ChallengeType type;
                        public bool          cleared;

                        #endregion


                        #region Constructor

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

                        #endregion
                    }

                    #endregion


                    #region Field

                    public List<Challenge> challenges;

                    #endregion


                    #region Constructor

                    public Level(Base _base, List<Challenge> challenges) : base(_base)
                    {
                        this.challenges = new List<Challenge>(challenges);
                    }

                    public Level(Level other) : base(other)
                    {
                        challenges = new List<Challenge>(other.challenges);
                    }

                    #endregion
                }

                #endregion


                #region Field

                public List<Level> levels;

                #endregion


                #region Constructor

                public Stage(Base _base, List<Level> levels) : base(_base) { this.levels = new List<Level>(levels); }

                public Stage(Stage other) : base(other) { levels = new List<Level>(other.levels); }

                #endregion
            }


            [Serializable] public class Money
            {
                #region Field

                public int value;

                #endregion


                #region Constructor

                public Money(int value) { this.value = value; }

                public Money(Money other) { value = other.value; }

                #endregion
            }


            [Serializable] public class Record
            {
                #region Field

                public string dateSaved;
                public string runTime;

                #endregion


                #region Constructor

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

                #endregion
            }

            #endregion


            #region Field

            public List<CharacterStat> characterStats;
            public List<Stage>         stages;
            public Money               money;
            public Record              record;

            #endregion


            #region Constructor

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

            #endregion
        }


        [Serializable] public struct PathSetting
        {
            #region Field

            [SerializeField] private string _directoryName;
            [SerializeField] private string _fileName;

            public string directoryName { get { return _directoryName; } }
            public string fileName      { get { return _fileName; } }

            #endregion


            #region Constructor

            public PathSetting(string directoryName, string fileName)
            {
                _directoryName = directoryName;
                _fileName      = fileName;
            }

            #endregion


            #region Method

            public string GetDirectory() => Path.Combine(Application.persistentDataPath, directoryName);

            public string GetPath(int index) => Path.Combine(GetDirectory(), $"{fileName}{index.ToString("00")}");

            #endregion
        }

        #endregion


        #region Field

        public new ISaveMenuUIController ui { get; protected set; }

        public Dictionary<int, Data> datas { get; protected set; }

        [SerializeField] protected PathSetting _pathSetting;

        public PathSetting pathSetting { get { return _pathSetting; } }

        protected DateTime dateUpdated, dateUpdatedAuto;
        protected TimeSpan elapsedTime, elapsedTimeAuto;

        #endregion


        #region Method

        #region Event

        protected override void Awake()
        {
            base.Awake();

            datas = ReadFiles();
        }

        protected virtual void Reset() { ResetField(); }

        #endregion


        #region Initialization

        protected override void SetField()
        {
            base.SetField();

            ui   = GetComponentInChildren<ISaveMenuUIController>(true);
            type = Type.Save;
        }

        protected virtual void ResetField() { _pathSetting = new PathSetting("Save", "Data"); }

        #endregion


        #region Data

        #region Load

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

        #endregion


        #region Save

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

        #endregion

        #endregion

        #endregion
    }
}
