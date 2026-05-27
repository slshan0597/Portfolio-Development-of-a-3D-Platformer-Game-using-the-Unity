// //////////////////////////////////////////////////////////////////////////////
// * 요약
//    - 스테이지 씬의 메인 UI 클래스
//    - 경과시간, 도전과제 등의 스코어(진행도) 정보 표시
//    - 플레이어 체력 정보 표시
//
// * 목차
//    1. 인터페이스 ... Line 39
//    2. 클래스 ....... Line 65
//        1) 필드 ..... Line 72
//        2) 메서드 ... Line 88
//            1- 이벤트 함수 ... Line 91
//            2- 초기화 ........ Line 103
//            3- 셋(Set) ....... Line 122
//            4- 데이터 ........ Line 147
//                1_ 불러오기(Load) .... Line 150
//                2_ 저장하기(Save) .... Line 171
//                3_ 기록하기(Write) ... Line 192
// //////////////////////////////////////////////////////////////////////////////
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

    // //////////////////////////////////////////////////////////////////////////////
    // 1. 인터페이스(IUIBase 인터페이스 상속)
    // //////////////////////////////////////////////////////////////////////////////
    public interface IMainUIController : IUIBase
    {
        // 프로퍼티
        // Component
        Root root { get; }

        // Reference
        ISceneDirector scene { get; }

        // 메서드
        Coroutine SetContent(CountType type, int count, int amount);
        void      SetContent(ChallengeType type, int score);
    }

    // //////////////////////////////////////////////////////////////////////////////
    // 2. 클래스(UIBase 클래스 상속)
    // //////////////////////////////////////////////////////////////////////////////
    public class MainUIController : UIBase, IMainUIController
    {
        // ==============================================================================
        // 1) 내부 타입 - 구조
        // ==============================================================================
        public class Root : MenuBase
        {
            // ------------------------------------------------------------------------------
            // 1-1) 내부 타입 - 구조 -> 메인
            // ------------------------------------------------------------------------------
            public class Main : MenuBase
            {
                // ******************************************************************************
                // 1-1-1) 내부 타입 - 구조 -> 메인 -> 카운트(Count)
                //    - 플레이어의 체력, 얻은 코인 수 표시
                // ******************************************************************************
                public class Count : MenuBase
                {
                    // 내부 타입 - 카운트
                    public enum Type { HP, Coin }

                    public class Slot : MenuBase
                    {
                        public Text     contentText { get; }
                        public SlotBase amount      { get; }

                        public Slot(Transform transform) : base(transform)
                        {
                            contentText = content.Find("Content Text").GetComponent<Text>();
                            amount      = new SlotBase(content.Find("Amount"));
                        }

                        public void SetContent(Type type, int count, int amount)
                        {
                            string sign    = (amount >  0) ? "+"               : string.Empty;
                            string _amount = (amount != 0) ? amount.ToString() : string.Empty;

                            contentText.text           = $"{type}\t: {count}";
                            this.amount.labelText.text = $"{sign}{_amount}";
                        }
                    }

                    // 필드 - 카운트
                    public Dictionary<Type, Slot> slots { get; }

                    // 생성자 - 카운트
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

                    // 메서드 - 카운트
                    public void SetContent(int hitPoint, int coin)
                    {
                        slots[Type.HP].SetContent(Type.HP, hitPoint, 0);
                        slots[Type.Coin].SetContent(Type.Coin, coin, 0);
                    }
                }

                // ******************************************************************************
                // 1-1-2) 내부 타입 - 구조 -> 메인 -> 도전과제(Challenge)
                //    - 현재 스코어(진행도)에 따른 도전과제 정보 표시
                // ******************************************************************************
                public class Challenge : WindowBase
                {
                    // 내부 타입 - 도전과제
                    public class Slot : MenuBase
                    {
                        public Toggle toggle         { get; }
                        public Text   contentText    { get; }
                        public Image  strikeoutImage { get; }

                        public Slot(Transform transform) : base(transform)
                        {
                            toggle         = content.GetComponentInChildren<Toggle>(true);
                            contentText    = content.GetComponentInChildren<Text>(true);
                            strikeoutImage = content.Find("Strikeout Image").GetComponent<Image>();
                        }

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
                    }

                    // 필드 - 도전과제
                    public Dictionary<ChallengeType, Slot> slots { get; }

                    // 생성자 - 도전과제
                    public Challenge(Transform transform, Dictionary<ChallengeType, ChallengeData> challengeDatas)
                        : base(transform)
                    {
                        var slot = content.Find("Slot");

                        slots = new Dictionary<ChallengeType, Slot>();

                        foreach (var type in challengeDatas.Keys) slots.Add(type, new Slot(Instantiate(slot, content)));

                        //DestroyImmediate(slot.gameObject);
                        slot.gameObject.SetActive(false);
                    }

                    // 메서드 - 도전과제
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
                }

                // 필드 - 메인
                public Text      timeText   { get; }
                public Count     count      { get; }
                public Challenge challenge  { get; }
                public KeyButton menuButton { get; }

                // 생성자 - 메인
                public Main(Transform transform, Dictionary<ChallengeType, ChallengeData> challengeDatas) : base(transform)
                {
                    timeText   = content.Find("Time Text").GetComponent<Text>();
                    count      = new Count(content.Find("Count"));
                    challenge  = new Challenge(content.Find("Challenge"), challengeDatas);
                    menuButton = content.GetComponentInChildren<KeyButton>(true);
                }

                // 메서드 - 메인
                public void SetContent(int hitPoint, IScoreManager score,
                    Dictionary<ChallengeType, ChallengeData> challengeDatas, IControlSettingManager controlSetting)
                {
                    SetContent(score.elapsedTime);
                    count.SetContent(hitPoint, score.coin);
                    challenge.SetContent(score.challenges, challengeDatas);
                    menuButton.SetContent(controlSetting.GetKey(SystemControlType.Pause));
                }

                public void SetContent(TimeSpan elapsedTime) { timeText.text = elapsedTime.ToString(@"mm\:ss\:ff"); }
            }

            // 필드 - 구조
            public Main main { get; }

            // 생성자 - 구조
            public Root(Transform transform, Dictionary<ChallengeType, ChallengeData> challengeDatas) : base(transform)
            { 
                main = new Main(content.Find("Main"), challengeDatas);
            }
        }

        // ==============================================================================
        // 2) 필드
        // ==============================================================================
        // Component & Reference
        public Root           root  { get; protected set; }
        public ISceneDirector scene { get; protected set; }

        // etc.
        protected Dictionary<CountType, Coroutine> actions;

        // ==============================================================================
        // 3) 메서드
        // ==============================================================================
        // ------------------------------------------------------------------------------
        // 3-1) 메서드 -> 이벤트 함수
        //    - 경과 시간을 실시간으로 갱
        // ------------------------------------------------------------------------------
        protected virtual void Update()
        {
            IScoreManager score = scene.score;

            root.main.SetContent(score.elapsedTime);
        }

        // ------------------------------------------------------------------------------
        // 3-2) 메서드 -> 초기화
        // ------------------------------------------------------------------------------
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

        // ------------------------------------------------------------------------------
        // 3-3) 메서드 -> 셋(Set)
        //    - 스코어 갱신 시 호출됨
        // ------------------------------------------------------------------------------
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

        // ------------------------------------------------------------------------------
        // 3-4) 메서드 -> 표시(Display)
        // ------------------------------------------------------------------------------
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

        // ------------------------------------------------------------------------------
        // 3-5) 메서드 -> 이벤트
        //    - 게임 일시 정지 후 게임 메뉴 호출
        // ------------------------------------------------------------------------------
        protected virtual void OnClickMenuButton()
        {
            IMainMenuManager gameMenu = GameDirector.instance.menu.main;

            gameMenu.Open(true);
        }
    }
}
