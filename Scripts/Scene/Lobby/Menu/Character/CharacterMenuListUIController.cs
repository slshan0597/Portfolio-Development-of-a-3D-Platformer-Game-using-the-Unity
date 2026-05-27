// //////////////////////////////////////////////////////////////////////////////
// * 요약
//    - 게임의 로비의 캐릭터 메뉴의 캐릭터 강화 리스트 UI 클래스
//    - 캐릭터 항목마다 강화 수치와 요구 강화 재료 표시
//
// * 목차
//    1. 인터페이스 ... Line 
//    2. 클래스 ....... Line 
//        1) 내부 타입 ... Line 
//        2) 필드 ........ Line 
//        3) 메서드 ...... Line 
//            1- 초기화 .... Line 
//            2- 셋(Set) ... Line 
//            3- 이벤트 .... Line 
// //////////////////////////////////////////////////////////////////////////////
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

using Game;

namespace Lobby
{
    using Root              = CharacterMenuListUIController.Root;
    using CharacterStatData = DataManager.CharacterStat;
    using CharacterStatType = DataManager.Setting.CharacterStat.Type;
    using MoneyData         = SaveMenuManager.Data.Money;

    // //////////////////////////////////////////////////////////////////////////////
    // 1. 인터페이스(IUIBase 인터페이스 상속)
    // //////////////////////////////////////////////////////////////////////////////
    public interface ICharacterMenuListUIController : IUIBase
    {
        // 프로퍼티
        // Component
        Root root { get; }

        // Reference
        ICharacterMenuUIController ui { get; }
    }

    // //////////////////////////////////////////////////////////////////////////////
    // 2. 클래스(UIBase 클래스 상속)
    // //////////////////////////////////////////////////////////////////////////////
    public class CharacterMenuListUIController : UIBase, ICharacterMenuListUIController
    {
        // ==============================================================================
        // 1) 내부 타입 - 구조
        // ==============================================================================
        public class Root
        {
            // 내부 타입
            public class Slot : SlotBase
            {
                public class Option : WindowBase
                {
                    public Text   text       { get; }
                    public Button button     { get; }
                    public Text   buttonText { get; }

                    public Option(Transform transform) : base(transform)
                    {
                        text       = content.Find("Text").GetComponent<Text>();
                        button     = content.GetComponentInChildren<Button>(true);
                        buttonText = button.GetComponentInChildren<Text>(true);
                    }

                    public void SetContent(CharacterStatData characterStatData, MoneyData moneyData)
                    {
                        int maxCount = characterStatData.maxCount;
                        int cost     = characterStatData.cost;
                        int count    = characterStatData.value;
                        int money    = moneyData.value;

                        text.text           = $"{count} / {maxCount}";
                        button.interactable = (count < maxCount) && (money >= cost);
                        buttonText.text     = (count < maxCount) ? $"Cost :\n{cost}" : "Max";
                    }
                }

                public Option option { get; }

                public Slot(Transform transform) : base(transform) { option = new Option(content.Find("Option")); }

                public void SetContent(CharacterStatType type, CharacterStatData characterStatData, MoneyData moneyData)
                {
                    labelText.text = type.ToString();

                    option.SetContent(characterStatData, moneyData);
                }
            }

            // 필드
            public Dictionary<CharacterStatType, Slot> slots { get; }

            // 생성자
            public Root(Transform transform, Dictionary<CharacterStatType, CharacterStatData> characterStatDatas)
            {
                var content = transform.GetComponentInChildren<LayoutGroup>(true).transform;
                var slot    = content.Find("Slot");

                slots = new Dictionary<CharacterStatType, Slot>();

                foreach (var type in characterStatDatas.Keys) slots.Add(type, new Slot(Instantiate(slot, content)));

                DestroyImmediate(slot.gameObject);
            }

            // 메서드
            public void SetContent(Dictionary<CharacterStatType, CharacterStatData> characterStatDatas, MoneyData moneyData)
            {
                foreach (var element in slots)
                {
                    CharacterStatType type = element.Key;
                    var               slot = element.Value;

                    slot.SetContent(type, characterStatDatas[type], moneyData);
                }
            }
        }

        // ==============================================================================
        // 2) 필드
        // ==============================================================================
        // Component & Reference
        public Root                       root { get; protected set; }
        public ICharacterMenuUIController ui   { get; protected set; }

        // ==============================================================================
        // 3) 메서드
        // ==============================================================================
        // ------------------------------------------------------------------------------
        // 3-1) 메서드 -> 초기화
        // ------------------------------------------------------------------------------
        protected override void SetField()
        {
            IDataManager data = GameDirector.instance.data;

            root = new Root(transform, data.characterStats);
            ui   = GetComponentInParent<ICharacterMenuUIController>(true);

            base.SetField();

            foreach (var element in root.slots)
            {
                CharacterStatType type = element.Key;
                var               slot = element.Value;

                slot.option.button.onClick.AddListener(delegate { OnClickButton(type); });
            }
        }

        // ------------------------------------------------------------------------------
        // 3-2) 메서드 -> 셋(Set)
        // ------------------------------------------------------------------------------
        public override void Set()
        {
            base.Set();

            IDataManager data = GameDirector.instance.data;

            root.SetContent(data.characterStats, data.money);
        }

        // ------------------------------------------------------------------------------
        // 3-3) 메서드 -> 이벤트
        //    - 강화 시도 및 강화 확인 창 표시
        // ------------------------------------------------------------------------------
        protected virtual void OnClickButton(CharacterStatType type)
        {
            IAudioController audio = GameDirector.instance.audio;

            audio.PlayButtonClick();
            StartCoroutine(TryGrowUp(type));
        }

        protected virtual IEnumerator TryGrowUp(CharacterStatType type)
        {
            IConfirmUIController  confirm = GameDirector.instance.ui.confirm;
            ICharacterMenuManager menu    = ui.menu;

            yield return confirm.Display("Grow Up", GetConfirmText(type), ui, this);

            if (confirm.state == ConfirmUIController.State.Cancel) yield break;

            menu.GrowUp(type);
        }

        protected virtual string GetConfirmText(CharacterStatType type)
        {
            IDataManager data = GameDirector.instance.data;

            var _data    = data.characterStats[type];
            int maxCount = _data.maxCount;
            int cost     = _data.cost;
            int count    = _data.value;
            int money    = data.money.value;

            StringBuilder str = new StringBuilder();

            str.Append($"Money\t: {money} - {cost} = {money - cost}");
            str.Append("\n");
            str.Append((count + 1 < maxCount) ? $"Count\t: {count} >> {count + 1}"
                                              : $"Count\t: {count} >> {count + 1}(Max)");

            return str.ToString();
        }
    }
}
