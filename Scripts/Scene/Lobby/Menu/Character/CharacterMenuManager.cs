using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Game;


namespace Lobby
{
    using CharacterStatType = DataManager.Setting.CharacterStat.Type;
    using CameraType        = CharacterMenuManager.CameraType;
    using PlayerEmoteType   = PlayerModelController.EmoteType;


    public interface ICharacterMenuManager : IMenuBase
    {
        #region Property

        // Component
        new ICharacterMenuUIController                  ui            { get; }
        Dictionary<CameraType, ICameraTargetController> cameraTargets { get; }

        #endregion


        #region Method

        void      GrowUp(CharacterStatType type);
        Coroutine OpenViewMenu(bool isActive);

        #endregion
    }


    public class CharacterMenuManager : MenuBase, ICharacterMenuManager
    {
        #region Definition

        public enum CameraType { None, Main, CharacterView }

        #endregion


        #region Field

        public new ICharacterMenuUIController                  ui            { get; protected set; }
        public Dictionary<CameraType, ICameraTargetController> cameraTargets { get; protected set; }

        #endregion


        #region Method

        #region Initialization

        protected override void SetField()
        {
            base.SetField();

            ui            = GetComponentInChildren<ICharacterMenuUIController>(true);
            cameraTargets = new Dictionary<CameraType, ICameraTargetController>();

            foreach (var target in GetComponentsInChildren<ICameraTargetController>(true))
            {
                string name = target.transform.name.Replace(" ", string.Empty);

                if (Enum.TryParse(name, out CameraType type)) cameraTargets.Add(type, target);
            }

            type = Type.Character;
        }

        #endregion


        public virtual void GrowUp(CharacterStatType type)
        {
            IGameDirector       game     = GameDirector.instance;
            IDataManager        data     = game.data;
            ISaveMenuManager    saveMenu = game.menu.save;
            INoticeUIController noticeUI = game.ui.notice;
            IPlayerController   player   = scene.player;

            var moneyData         = data.money;
            var characterStatData = data.characterStats[type];

            moneyData.value -= characterStatData.cost;
            characterStatData.value++;

            saveMenu.Save(0);
            ui.Set();
            ui.list.Set();
            noticeUI.Display("Success", ui, ui.list);
            player.resources.model.Play(PlayerEmoteType.Happy);
        }

        public virtual Coroutine OpenViewMenu(bool isActive) { return StartCoroutine(_OpenViewMenu(isActive)); }

        protected virtual IEnumerator _OpenViewMenu(bool isActive)
        {
            var                     cameras      = scene.cameras;
            ICameraTargetController cameraTarget = cameraTargets[isActive ? CameraType.CharacterView : CameraType.Main];

            cameras.main.Set(cameraTarget, ui.defaultDuration * 2f);

            if (!isActive) ui.canvas.worldCamera = cameras.main.inner;

            yield return isActive ? ui.DisplayMain(false) : ui.DisplayView(false);
            yield return isActive ? ui.DisplayView(true)  : ui.DisplayMain(true);

            if (isActive)
            {
                cameras.characterView.Set(cameraTarget);

                ui.canvas.worldCamera = cameras.characterView.inner;
            }
        }

        #endregion
    }
}
