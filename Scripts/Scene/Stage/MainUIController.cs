using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using Game;


namespace Stage
{
    using Root              = MainUIController.Root;
    using CountType         = MainUIController.Root.Main.Count.Type;
    using ChallengeData     = Game.DataManager.Stages.Stage.Levels.Level.Challenge;
    using ChallengeType     = Game.DataManager.Setting.Stage.Level.Challenge.Type;
    using ComparisonType    = Game.DataManager.Setting.Stage.Level.Challenge.Comparison.Type;
    using SystemControlType = ControlSettingManager.Data.SystemType;


    public interface IMainUIController : IUIBase
    {
        #region Property

        // Component
        Root root { get; }

        // Reference
        ISceneDirector scene { get; }

        #endregion


        #region Method

        Coroutine SetContent(CountType type, int count, int amount);
        void      SetContent(ChallengeType type, int score);

        #endregion
    }


    public class MainUIController : UIBase, IMainUIController
    {
        #region Definition

        public class Root : MenuBase
        {
            #region Definition

            public class Main : MenuBase
            {
                #region Definition

                public class Count : MenuBase
                {
                    #region Definition

                    public enum Type { HP, Coin }


                    public class Slot : MenuBase
                    {
                        #region Field

                        public Text     contentText { get; }
                        public SlotBase amount      { get; }

                        #endregion


                        #region Constructor

                        public Slot(Transform transform) : base(transform)
                        {
                            contentText = content.Find("Content Text").GetComponent<Text>();
                            amount      = new SlotBase(content.Find("Amount"));
                        }

                        #endregion


                        #region Method

                        public void SetContent(Type type, int count, int amount)
                        {
                            string sign    = (amount >  0) ? "+"               : string.Empty;
                            string _amount = (amount != 0) ? amount.ToString() : string.Empty;

                            contentText.text           = $"{type}\t: {count}";
                            this.amount.labelText.text = $"{sign}{_amount}";
                        }

                        #endregion
                    }

                    #endregion


                    #region Field

                    public Dictionary<Type, Slot> slots { get; }

                    #endregion


                    #region Constructor

                    public Count(Transform transform) : base(transform)
                    {
                        slots = new Dictionary<CountType, Slot>();

                        for (int i = 0; i < content.childCount; i++)
                        {
                            Transform slot = content.GetChild(i);
                            string    name = slot.name.Replace(" ", string.Empty);

                            if (Enum.TryParse(name, out Type type)) slots.Add(type, new Slot(slot));
                        }
                    }

                    #endregion


                    #region Method

                    public void SetContent(int hitPoint, int coin)
                    {
                        slots[Type.HP].SetContent(Type.HP, hitPoint, 0);
                        slots[Type.Coin].SetContent(Type.Coin, coin, 0);
                    }

                    #endregion
                }


                public class Challenge : WindowBase
                {
                    #region Definition

                    public class Slot : MenuBase
                    {
                        #region Field

                        public Toggle toggle         { get; }
                        public Text   contentText    { get; }
                        public Image  strikeoutImage { get; }

                        #endregion


                        #region Constructor

                        public Slot(Transform transform) : base(transform)
                        {
                            toggle         = content.GetComponentInChildren<Toggle>(true);
                            contentText    = content.GetComponentInChildren<Text>(true);
                            strikeoutImage = content.Find("Strikeout Image").GetComponent<Image>();
                        }

                        #endregion


                        #region Method

                        public void SetContent(ChallengeType type, int score, ChallengeData challengeData)
                        {
                            var    comparison = challengeData.comparison;
                            int    rhs        = comparison.rhs;
                            bool   preCleared = challengeData.cleared;
                            bool   cleared    = false;
                            string unit       = (type == ChallengeType.Time) ? " (m)" : string.Empty;

                            switch (comparison.type)
                            {
                                case ComparisonType.Greater:      cleared = (score >  rhs); break;
                                case ComparisonType.GreaterEqual: cleared = (score >= rhs); break;
                                case ComparisonType.Less:         cleared = (score <  rhs); break;
                                case ComparisonType.LessEqual:    cleared = (score <= rhs); break;
                            }

                            toggle.interactable = false;
                            toggle.isOn         = cleared || preCleared;
                            contentText.text    = $"{type}_{comparison.type}\t: {score} / {rhs}{unit}";

                            strikeoutImage.gameObject.SetActive(preCleared);
                        }

                        #endregion
                    }

                    #endregion


                    #region Field

                    public Dictionary<ChallengeType, Slot> slots { get; }

                    #endregion


                    #region Constructor

                    public Challenge(Transform transform, Dictionary<ChallengeType, ChallengeData> challengeDatas)
                        : base(transform)
                    {
                        var slot = content.Find("Slot");

                        slots = new Dictionary<ChallengeType, Slot>();

                        foreach (var type in challengeDatas.Keys) slots.Add(type, new Slot(Instantiate(slot, content)));

                        //DestroyImmediate(slot.gameObject);
                        slot.gameObject.SetActive(false);
                    }

                    #endregion


                    #region Method

                    public void SetContent(Dictionary<ChallengeType, int> score,
                        Dictionary<ChallengeType, ChallengeData> challengeDatas)
                    {
                        foreach (var element in slots)
                        {
                            ChallengeType type = element.Key;
                            var           slot = element.Value;

                            slot.SetContent(type, score[type], challengeDatas[type]);
                        }
                    }

                    #endregion
                }

                #endregion


                #region Field

                public Text      timeText   { get; }
                public Count     count      { get; }
                public Challenge challenge  { get; }
                public KeyButton menuButton { get; }

                #endregion


                #region Constructor

                public Main(Transform transform, Dictionary<ChallengeType, ChallengeData> challengeDatas) : base(transform)
                {
                    timeText   = content.Find("Time Text").GetComponent<Text>();
                    count      = new Count(content.Find("Count"));
                    challenge  = new Challenge(content.Find("Challenge"), challengeDatas);
                    menuButton = content.GetComponentInChildren<KeyButton>(true);
                }

                #endregion


                #region Method

                public void SetContent(int hitPoint, IScoreManager score,
                    Dictionary<ChallengeType, ChallengeData> challengeDatas, IControlSettingManager controlSetting)
                {
                    SetContent(score.elapsedTime);
                    count.SetContent(hitPoint, score.coin);
                    challenge.SetContent(score.challenges, challengeDatas);
                    menuButton.SetContent(controlSetting.GetKey(SystemControlType.Pause));
                }

                public void SetContent(TimeSpan elapsedTime) { timeText.text = elapsedTime.ToString(@"mm\:ss\:ff"); }

                #endregion
            }

            #endregion


            #region Field

            public Main main { get; }

            #endregion


            #region Constructor

            public Root(Transform transform, Dictionary<ChallengeType, ChallengeData> challengeDatas) : base(transform)
            { 
                main = new Main(content.Find("Main"), challengeDatas);
            }

            #endregion
        }

        #endregion


        #region Field

        public Root           root  { get; protected set; }
        public ISceneDirector scene { get; protected set; }

        protected Dictionary<CountType, Coroutine> actions;

        #endregion


        #region Method

        #region Event

        protected virtual void Update()
        {
            IScoreManager score = scene.score;

            root.main.SetContent(score.elapsedTime);
        }

        #endregion


        #region Initialization

        protected override void SetField()
        {
            base.SetField();

            Game.IDataManager data = GameDirector.instance.data;

            var challengeDatas = data.stages.Current.levels.Current.challenges;

            root  = new Root(transform, challengeDatas);
            scene = GetComponentInParent<ISceneDirector>(true);

            root.main.menuButton.onClick.AddListener(delegate { OnClickMenuButton(); });

            actions = new Dictionary<CountType, Coroutine>()
            {
                { CountType.HP,   null },
                { CountType.Coin, null }
            };
        }

        protected override void ResetField() { _defaultDuration = 0.25f; }

        #endregion


        #region Set

        public override void Set()
        {
            base.Set();

            IScoreManager          score          = scene.score;
            IPlayerController      player         = scene.player;
            IGameDirector          game           = GameDirector.instance;
            Game.IDataManager      data           = game.data;
            IControlSettingManager controlSetting = game.menu.setting.control;

            var challengeDatas = data.stages.Current.levels.Current.challenges;

            root.main.SetContent(player.state.hitPoint, score, challengeDatas, controlSetting);
        }

        public virtual Coroutine SetContent(CountType type, int count, int amount)
        {
            var slot = root.main.count.slots[type];

            slot.SetContent(type, count, amount);

            if (actions[type] != null) StopCoroutine(actions[type]);

            return actions[type] = StartCoroutine(FadeSlot(slot.amount, false, defaultDuration * 3f, FadeDirection.Down));
        }

        public virtual void SetContent(ChallengeType type, int score)
        {
            Game.IDataManager data = GameDirector.instance.data;

            var challengeData = data.stages.Current.levels.Current.challenges[type];

            root.main.challenge.slots[type].SetContent(type, score, challengeData);
        }

        public override void SelectFirstSelectable() { }

        protected override void SetCurrent(bool isActive) { }

        #endregion


        #region Display

        public override Coroutine Display(bool isActive, bool animated = true)
        {
            gameObject.SetActive(true);

            return base.Display(isActive, animated);
        }

        protected override IEnumerator _Display(bool isActive, float duration)
        {
            foreach (var slot in root.main.count.slots.Values) slot.amount.gameObject.SetActive(false);

            yield return FadeContent(root.main, isActive, duration);

            //SetInteractables(true);
            root.main.menuButton.interactable = true;

            if (!isActive) gameObject.SetActive(false);
        }

        #endregion


        protected virtual void OnClickMenuButton()
        {
            IMainMenuManager gameMenu = GameDirector.instance.menu.main;

            gameMenu.Open(true);
        }

        #endregion
    }
}
