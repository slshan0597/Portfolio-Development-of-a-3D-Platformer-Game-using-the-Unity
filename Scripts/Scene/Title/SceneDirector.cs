using System.Collections;
using UnityEngine;

using Game;


namespace Title
{
    using CursorVisibleEventType = GameDirector.CursorVisibleEventType;


    public interface ISceneDirector : ISceneBase
    {
        #region Property

        // Component
        IMenuManager  menu { get; }
        IUIController ui   { get; }

        #endregion
    }


    public class SceneDirector : SceneBase, ISceneDirector
    {
        #region Field

        public IMenuManager  menu { get; protected set; }
        public IUIController ui   { get; protected set; }

        #endregion


        #region Method

        #region Event

        protected virtual void Update() { camera.transform.Rotate(Vector3.up, 5f * Time.deltaTime); }

        #endregion


        #region Initialization

        protected override void SetField()
        {
            base.SetField();

            menu = GetComponentInChildren<IMenuManager>(true);
            ui   = GetComponentInChildren<IUIController>(true);

            type = Type.Title;
        }

        #endregion


        #region Enter

        public override Coroutine Enter(Type prev)
        {
            menu.Initialize();
            ui.gameObject.SetActive(false);

            return base.Enter(prev);
        }

        protected override IEnumerator _Enter(Type prev) 
        {
            IGameDirector game = GameDirector.instance;

            yield return base._Enter(prev);

            game.SetCursorVisible(CursorVisibleEventType.MenuOpen, true);

            yield return ui.Display(true);

            menu.Open(true);
        }

        #endregion


        #region Exit

        public override Coroutine Exit(Type next)
        {
            menu.Open(false);

            return base.Exit(next);
        }

        #endregion


        #region Pause

        public override void Pause(bool paused, params IAudioBase[] exceptions)
        {
            base.Pause(paused, exceptions);
            menu.ui.SetInteractables(!paused);
        }

        #endregion

        #endregion
    }
}
