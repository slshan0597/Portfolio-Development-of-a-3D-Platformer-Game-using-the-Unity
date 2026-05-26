using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;


namespace Game
{
    using SettingType = SettingBase.Type;


    public interface ISettingMenuManager : IMenuBase, IEnumerable<ISettingBase>
    {
        #region Property

        // Component
        new ISettingMenuUIController ui      { get; }
        IGraphicSettingManager       graphic { get; }
        IAudioSettingManager         audio   { get; }
        IControlSettingManager       control { get; }

        #endregion


        #region Indexing

        ISettingBase this[SettingType type] => this.First(menu => menu.type == type);

        #endregion
    }


    public class SettingMenuManager : MenuBase, ISettingMenuManager
    {
        #region Definition

        public class SystemMenusEnumerator : IEnumerator<ISettingBase>
        {
            #region Field

            public ISettingBase[] _settings;

            private int index = -1;

            #endregion


            #region Constructor

            public SystemMenusEnumerator(ISettingBase[] settings) { _settings = settings; }

            #endregion


            #region Method

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

            #endregion
        }

        #endregion


        #region Field

        public new ISettingMenuUIController ui      { get; protected set; }
        public IGraphicSettingManager       graphic { get; protected set; }
        public new IAudioSettingManager     audio   { get; protected set; }
        public IControlSettingManager       control { get; protected set; }

        protected ISettingBase[] _settings;

        #endregion


        #region Method

        #region Initialization

        protected override void SetField()
        {
            base.SetField();

            ui        = GetComponentInChildren<ISettingMenuUIController>(true);
            graphic   = GetComponentInChildren<IGraphicSettingManager>(true);
            audio     = GetComponentInChildren<IAudioSettingManager>(true);
            control   = GetComponentInChildren<IControlSettingManager>(true);
            _settings = GetComponentsInChildren<ISettingBase>(true);
        }

        #endregion


        #region Enumerable

        IEnumerator<ISettingBase> IEnumerable<ISettingBase>.GetEnumerator() { return GetEnumerator(); }

        IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }

        public SystemMenusEnumerator GetEnumerator() { return new SystemMenusEnumerator(_settings); }

        #endregion

        #endregion
    }
}
