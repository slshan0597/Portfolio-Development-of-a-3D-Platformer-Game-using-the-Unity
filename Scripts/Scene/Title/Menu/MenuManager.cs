using UnityEngine;


namespace Title
{
    public interface IMenuManager
    {
        #region Property

        // Component
        IMenuUIController ui { get; }

        // Reference
        ISceneDirector scene { get; }

        #endregion


        #region Method

        void Initialize();
        void Open(bool isActive);

        #endregion
    }


    public class MenuManager : MonoBehaviour, IMenuManager
    {
        #region Field

        public IMenuUIController ui    { get; protected set; }
        public ISceneDirector    scene { get; protected set; }

        #endregion


        #region Method

        #region Event

        protected virtual void Awake() { SetField(); }

        #endregion


        #region Initialization

        protected virtual void SetField()
        {
            ui    = GetComponentInChildren<IMenuUIController>(true);
            scene = GetComponentInParent<ISceneDirector>(true);
        }

        public virtual void Initialize()
        {
            gameObject.SetActive(true);
            ui.gameObject.SetActive(false);
        }

        #endregion


        public void Open(bool isActive) { ui.Display(true); }

        #endregion
    }
}
