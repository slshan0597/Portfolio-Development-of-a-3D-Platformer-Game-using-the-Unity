// //////////////////////////////////////////////////////////////////////////////
// * 요약
//    - 게임의 설정 메뉴 클래스
//
// * 목차
//    1. 인터페이스 ... Line 
//    2. 클래스 ....... Line 
//        1) 내부 타입 ... Line 
//        2) 필드 ........ Line 
//        3) 메서드 ...... Line 
//            1- 초기화 ............... Line 
//            2- 반복자(Enumerator) ... Line 
// //////////////////////////////////////////////////////////////////////////////
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Game
{
    using SettingType = SettingBase.Type;

    // //////////////////////////////////////////////////////////////////////////////
    // 1. 인터페이스(IMenuBase 인터페이스 상속)
    // //////////////////////////////////////////////////////////////////////////////
    public interface ISettingMenuManager : IMenuBase, IEnumerable<ISettingBase>
    {
        // 프로퍼티
        // Component
        new ISettingMenuUIController ui      { get; }
        IGraphicSettingManager       graphic { get; }
        IAudioSettingManager         audio   { get; }
        IControlSettingManager       control { get; }

        // 메서드
        // Indexing
        ISettingBase this[SettingType type] => this.First(menu => menu.type == type);
    }

    // //////////////////////////////////////////////////////////////////////////////
    // 2. 클래스(MenuBase 클래스 상속)
    // //////////////////////////////////////////////////////////////////////////////
    public class SettingMenuManager : MenuBase, ISettingMenuManager
    {
        // ==============================================================================
        // 1) 내부 타입
        //    - 반복자 정의
        // ==============================================================================
        public class SystemMenusEnumerator : IEnumerator<ISettingBase>
        {
            // 필드
            public ISettingBase[] _settings;

            private int index = -1;

            // 생성자
            public SystemMenusEnumerator(ISettingBase[] settings) { _settings = settings; }

            // 메서드
            public bool MoveNext() { return ++index < _settings.Length; }

            public void Reset() { index = -1; }

            ISettingBase IEnumerator<ISettingBase>.Current { get { return Current; } }

            object IEnumerator.Current { get { return Current; } }

            public ISettingBase Current
            {
                get
                {
                    try                              { return _settings[index]; }
                    catch (IndexOutOfRangeException) { throw new InvalidOperationException(); }
                }
            }

            public void Dispose() { }
        }

        // ==============================================================================
        // 2) 필드
        // ==============================================================================
        // Component & Reference
        public new ISettingMenuUIController ui      { get; protected set; }
        public IGraphicSettingManager       graphic { get; protected set; }
        public new IAudioSettingManager     audio   { get; protected set; }
        public IControlSettingManager       control { get; protected set; }

        // etc.
        protected ISettingBase[] _settings;

        // ==============================================================================
        // 3) 메서드
        // ==============================================================================
        // ------------------------------------------------------------------------------
        // 3-1) 메서드 -> 초기화
        // ------------------------------------------------------------------------------
        protected override void SetField()
        {
            base.SetField();

            ui        = GetComponentInChildren<ISettingMenuUIController>(true);
            graphic   = GetComponentInChildren<IGraphicSettingManager>(true);
            audio     = GetComponentInChildren<IAudioSettingManager>(true);
            control   = GetComponentInChildren<IControlSettingManager>(true);
            _settings = GetComponentsInChildren<ISettingBase>(true);
        }

        // ------------------------------------------------------------------------------
        // 3-2) 메서드 -> 반복자(Enumerator)
        // ------------------------------------------------------------------------------
        IEnumerator<ISettingBase> IEnumerable<ISettingBase>.GetEnumerator() { return GetEnumerator(); }

        IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }

        public SystemMenusEnumerator GetEnumerator() { return new SystemMenusEnumerator(_settings); }
    }
}
