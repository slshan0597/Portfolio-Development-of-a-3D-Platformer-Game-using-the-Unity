// //////////////////////////////////////////////////////////////////////////////
// * 요약
//    - 게임의 로비의 메인 메뉴 클래스
//
// * 목차
//    1. 인터페이스 ... Line 15
//    2. 클래스 ....... Line 28
// //////////////////////////////////////////////////////////////////////////////
using System.Collections;
using UnityEngine;

namespace Lobby
{
    // //////////////////////////////////////////////////////////////////////////////
    // 1. 인터페이스(IMenuBase 인터페이스 상속)
    // //////////////////////////////////////////////////////////////////////////////
    public interface IMainMenuManager : IMenuBase
    {
        // 프로퍼티
        // Component
        new IMainMenuUIController ui { get; }

        // 메서드
        Coroutine HideMainMenu(bool isActive);
    }

    // //////////////////////////////////////////////////////////////////////////////
    // 2. 클래스(MenuBase 클래스 상속)
    // //////////////////////////////////////////////////////////////////////////////
    public class MainMenuManager : MenuBase, IMainMenuManager
    {
        // 필드
        // Component & Reference
        public new IMainMenuUIController ui { get; protected set; }

        // 메서드 - 초기화
        protected override void SetField()
        {
            base.SetField();

            ui = GetComponentInChildren<IMainMenuUIController>(true);

            type = Type.Main;
        }

        // 메서드 - 감추기(Hide)
        public virtual Coroutine HideMainMenu(bool isActive) { return StartCoroutine(_HideMainMenu(isActive)); }

        protected virtual IEnumerator _HideMainMenu(bool isActive)
        {
            yield return isActive ? ui.DisplayMain(false) : ui.DisplayHide(false);
            yield return isActive ? ui.DisplayHide(true)  : ui.DisplayMain(true);
        }
    }
}
