// //////////////////////////////////////////////////////////////////////////////
// * 요약
//    - 게임의 설정(그래픽, 오디오, 컨트롤)에 대한 기반 클래스
//
// * 목차
//    1. 인터페이스 ... Line 16
//    2. 클래스 ....... Line 37
// //////////////////////////////////////////////////////////////////////////////
using UnityEngine;

namespace Game
{
    using Type = SettingBase.Type;

    // //////////////////////////////////////////////////////////////////////////////
    // 1. 인터페이스
    // //////////////////////////////////////////////////////////////////////////////
    public interface ISettingBase
    {
        // 프로퍼티
        // Component
        GameObject gameObject { get; }

        // Reference
        ISettingMenuManager menu { get; }

        // Setting
        Type type { get; }

        // 메서드
        void Load();
        void Save();
        void Set(bool reset = false);
    }

    // //////////////////////////////////////////////////////////////////////////////
    // 2. 클래스
    //    - 클래스 확장을 위한 기반 기능만을 구현
    // //////////////////////////////////////////////////////////////////////////////
    public class SettingBase : MonoBehaviour, ISettingBase
    {
        // 내부 타입
        public enum Type { None, Graphic, Audio, Control }

        // 필드
        // Component & Reference
        public ISettingMenuManager menu { get; protected set; }

        // Setting Type
        public Type type { get; protected set; }

        // 메서드
        // Event
        protected virtual void Awake() 
        {
            SetField();
            Load();
        }

        protected virtual void Start() { Set(); }

        // Initialization
        protected virtual void SetField() { menu = GetComponentInParent<ISettingMenuManager>(true); }

        // Data
        public virtual void Load() { }

        public virtual void Save() { }

        public virtual void Set(bool reset = false)
        {
            if (reset)
            {
                ISettingListUIBase listUI = menu.ui.lists[type];

                listUI.Set();
            }

            Save();
        }
    }
}
