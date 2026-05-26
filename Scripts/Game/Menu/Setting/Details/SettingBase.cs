//using System.Text;
//using System.Runtime.InteropServices;
using UnityEngine;


namespace Game
{
    using Type = SettingBase.Type;


    public interface ISettingBase
    {
        #region Property

        // Component
        GameObject gameObject { get; }

        // Reference
        ISettingMenuManager menu { get; }

        // Setting
        Type type { get; }

        #endregion


        #region Method

        void Load();
        void Save();
        void Set(bool reset = false);

        #endregion
    }


    public class SettingBase : MonoBehaviour, ISettingBase
    {
        //[DllImport("Kernel32")]
        ////[DllImport("__Internal")]
        //static extern long WritePrivateProfileString(string section, string key, string value, string filePath);
        //[DllImport("Kernel32")]
        ////[DllImport("__Internal")]
        //static extern int GetPrivateProfileString(string section, string key, string defVal, StringBuilder retVal,
        //    int size, string filePath);


        #region Definition

        public enum Type { None, Graphic, Audio, Control }

        #endregion


        #region Field

        public ISettingMenuManager menu { get; protected set; }

        public Type type { get; protected set; }

        #endregion


        #region Method

        #region Event

        protected virtual void Awake() 
        {
            SetField();
            Load();
        }

        protected virtual void Start() { Set(); }

        #endregion


        #region Initialization

        protected virtual void SetField() { menu = GetComponentInParent<ISettingMenuManager>(true); }

        #endregion


        #region Data

        public virtual void Load() { }

        //protected virtual string ReadFile(string section, string key)
        //{
        //    int    size = 255;
        //    var    data = new StringBuilder(size);
        //    string path = menu.pathSetting.GetPath();

        //    GetPrivateProfileString(section, key, string.Empty, data, size, path);

        //    return data.ToString();
        //}

        public virtual void Save() { }

        //protected virtual void WriteFile(string section, string key, string value)
        //{
        //    string path = menu.pathSetting.GetPath();

        //    WritePrivateProfileString(section, key, value, path);
        //}

        #endregion


        #region Set

        public virtual void Set(bool reset = false)
        {
            if (reset)
            {
                ISettingListUIBase listUI = menu.ui.lists[type];

                listUI.Set();
            }

            Save();
        }

        #endregion

        #endregion
    }
}
