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


    public interface ICharacterMenuListUIController : IUIBase
    {
        #region Property

        // Component
        Root root { get; }

        // Reference
        ICharacterMenuUIController ui { get; }

        #endregion
    }


    public class CharacterMenuListUIController : UIBase, ICharacterMenuListUIController
    {
        #region Definition

        public class Root
        {
            #region Definition

            public class Slot : SlotBase
            {
                #region Definition

                public class Option : WindowBase
                {
                    #region Field

                    public Text   text       { get; }
                    public Button button     { get; }
                    public Text   buttonText { get; }

                    #endregion


                    #region Constructor

                    public Option(Transform transform) : base(transform)
                    {
                        text       = content.Find("Text").GetComponent<Text>();
                        button     = content.GetComponentInChildren<Button>(true);
                        buttonText = button.GetComponentInChildren<Text>(true);
                    }

                    #endregion


                    #region Method

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

                    #endregion
                }

                #endregion


                #region Field

                public Option option { get; }

                #endregion


                #region Constructor

                public Slot(Transform transform) : base(transform) { option = new Option(content.Find("Option")); }

                #endregion


                #region Method

                public void SetContent(CharacterStatType type, CharacterStatData characterStatData, MoneyData moneyData)
                {
                    labelText.text = type.ToString();

                    option.SetContent(characterStatData, moneyData);
                }

                #endregion
            }

            #endregion


            #region Field

            public Dictionary<CharacterStatType, Slot> slots { get; }

            #endregion


            #region Constructor

            public Root(Transform transform, Dictionary<CharacterStatType, CharacterStatData> characterStatDatas)
            {
                var content = transform.GetComponentInChildren<LayoutGroup>(true).transform;
                var slot    = content.Find("Slot");

                slots = new Dictionary<CharacterStatType, Slot>();

                foreach (var type in characterStatDatas.Keys) slots.Add(type, new Slot(Instantiate(slot, content)));

                DestroyImmediate(slot.gameObject);
            }

            #endregion


            #region Method

            public void SetContent(Dictionary<CharacterStatType, CharacterStatData> characterStatDatas, MoneyData moneyData)
            {
                foreach (var element in slots)
                {
                    CharacterStatType type = element.Key;
                    var               slot = element.Value;

                    slot.SetContent(type, characterStatDatas[type], moneyData);
                }
            }

            #endregion
        }

        #endregion


        #region Field

        public Root                       root { get; protected set; }
        public ICharacterMenuUIController ui   { get; protected set; }

        #endregion


        #region Method

        #region Initialization

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

        #endregion


        #region Set

        public override void Set()
        {
            base.Set();

            IDataManager data = GameDirector.instance.data;

            root.SetContent(data.characterStats, data.money);
        }

        #endregion


        #region Option

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

        #endregion

        #endregion
    }
}
