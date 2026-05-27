// //////////////////////////////////////////////////////////////////////////////
// * 요약
//    - 게임의 로비의 캐릭터 메뉴 클래스
//    - 게임의 재화를 소모해 캐릭터의 능력치(Stat) 강화
//
// * 목차
//    1. 인터페이스 ... Line 15
//    2. 클래스 ....... Line 28
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

    // //////////////////////////////////////////////////////////////////////////////
    // 1. 인터페이스(IMenuBase 인터페이스 상속)
    // //////////////////////////////////////////////////////////////////////////////
    public interface ICharacterMenuManager : IMenuBase
    {
        // 프로퍼티
        // Component
        new ICharacterMenuUIController                  ui            { get; }
        Dictionary<CameraType, ICameraTargetController> cameraTargets { get; }

        // 메서드
        void      GrowUp(CharacterStatType type);
        Coroutine OpenViewMenu(bool isActive);
    }

    // //////////////////////////////////////////////////////////////////////////////
    // 2. 클래스(MenuBase 클래스 상속)
    // //////////////////////////////////////////////////////////////////////////////
    public class CharacterMenuManager : MenuBase, ICharacterMenuManager
    {
        // 내부 타입
        public enum CameraType { None, Main, CharacterView }

        // ==============================================================================
        // 1) 필드
        // ==============================================================================
        // Component & Reference
        public new ICharacterMenuUIController                  ui            { get; protected set; }
        public Dictionary<CameraType, ICameraTargetController> cameraTargets { get; protected set; }

        // ==============================================================================
        // 2) 메서드
        // ==============================================================================
        // ------------------------------------------------------------------------------
        // 2-1) 메서드 -> 초기화
        // ------------------------------------------------------------------------------
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

        // ------------------------------------------------------------------------------
        // 2-2) 메서드 -> 뷰(View) 모드 열기
        //    - 캐릭터 둘러보기 메뉴 호출
        // ------------------------------------------------------------------------------
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

        // ------------------------------------------------------------------------------
        // 2-3) 메서드 -> 캐릭터 강화
        //    - 게임의 재화를 소모하여 타입에 따른 캐릭터 능력치(Stat)를 강화
        //    - 강화 후 자동 저장
        // ------------------------------------------------------------------------------
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
    }
}
