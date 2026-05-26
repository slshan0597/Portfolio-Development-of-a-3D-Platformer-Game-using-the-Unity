using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Game;
using UnityEngine.SceneManagement;


namespace Tutorial
{
    using UI = SceneDirector.UI;


    public enum GuideType { None, Move, Jump1, Jump2, HipDrop, Attack }


    public interface ISceneDirector : ISceneBase 
    {
        #region Property

        // Component
        UI ui { get; }

        // Reference
        IPlanetController planet { get; }
        IPlayerController player { get; }

        #endregion


        #region Method

        void OpenGuide(GuideType type);

        #endregion
    }


    public class SceneDirector : SceneBase, ISceneDirector
    {
        #region Definition

        public class UI : List<IUIBase>
        {
            #region Field

            public IMainUIController  main  { get; }
            public IGuideUIController guide { get; }

            #endregion


            #region Constructor

            public UI(Transform transform) : base(transform.GetComponentsInChildren<IUIBase>(true))
            {
                main  = transform.GetComponentInChildren<IMainUIController>(true);
                guide = transform.GetComponentInChildren<IGuideUIController>(true);
            }

            #endregion


            #region Method

            public void Initialize() { foreach (var element in this) element.gameObject.SetActive(false); }

            public void Display(bool isActive)
            {
                main.Display(isActive);
                guide.Display(isActive);
            }

            public void Hide(bool paused)
            {
                main.root.content.gameObject.SetActive(!paused);
                guide.root.content.gameObject.SetActive(!paused);
            }

            #endregion
        }

        #endregion


        #region Field

        public UI                ui     { get; protected set; }
        public IPlanetController planet { get; protected set; }
        public IPlayerController player { get; protected set; }

        #endregion


        #region Method

        #region Event

        //protected override void Awake()
        //{
        //    if (GameDirector.instance == null) SceneManager.LoadScene("Game");

        //    base.Awake();
        //}

        protected virtual void Update() { player.state.hitPoint = player.setting.maxHitPoint; }

        #endregion


        #region Initialization

        protected override void SetField()
        {
            base.SetField();

            ui     = new UI(transform.Find("UI"));
            planet = FindObjectOfType<PlanetController>(true);
            player = FindObjectOfType<PlayerController>(true);

            type = Type.Tutorial;
        }

        #endregion


        #region General

        public override Coroutine Enter(Type prev = Type.None)
        {
            ui.Initialize();
            planet.Initialize();
            player.Set(planet.characterTarget);
            camera.Follow(player.cameraTarget);

            //StartCoroutine(ForRecording());

            return base.Enter(prev);
        }

        //private IEnumerator ForRecording()
        //{
        //    player.transform.position = new Vector3(151.5f, 0.4f, 0f);
        //    player.transform.rotation = Quaternion.Euler(0f, -90f, 0f);
        //    player.resources.transform.gameObject.SetActive(false);
        //    player.state.overlap[PlayerController.State.Overlap.State.Immunize] = true;
        //    Camera.main.transform.GetComponent<AudioListener>().enabled = false;

        //    var testCamera = player.transform.Find("Camera");
        //    var area = GameObject.Find("Planet").transform.Find("Objects").Find("Test Objects").GetChild(1).GetComponent<SphereCollider>();

        //    var box = GameObject.Find("Planet").transform.Find("Objects").Find("Test Objects").GetChild(1);

        //    box.transform.position += Vector3.up * 5f;

        //    var interactable = GameObject.Find("Planet").transform.Find("Objects").Find("Test Objects").GetChild(1).GetComponent<IInteractable>();

        //    yield return new WaitForSeconds(2);

        //    player.Interact(interactable);

        //    player.Attack(PlayerController.State.Attack.Spin);

        //    var boss = area.transform.GetComponent<IBossBase>();

        //    boss.Die();
        //    player.Die(PlayerController.State.Die.Normal);

        //    player.Damage(testCamera, IDamageable.Type.Normal);
        //    area.radius = 200f;

        //    yield return new WaitForSeconds(4);

        //    area.radius = 0.1f;
        //}

        protected override IEnumerator _Enter(Type prev)
        {
            IFadeUIController fadeUI = GameDirector.instance.ui.fade;

            yield return base._Enter(prev);
            yield return new WaitForSeconds(fadeUI.defaultDuration);

            ui.Display(true);
            player.Idle();

            if (Application.platform == RuntimePlatform.Android) player.ui.virtualJoystick.Display(true);
        }

        public override Coroutine Exit(Type next = Type.None)
        {
            next = (next == Type.None) ? Type.Lobby : next;

            return base.Exit(next);
        }

        public override void Pause(bool paused, params IAudioBase[] exceptions)
        {
            base.Pause(paused, exceptions);
            ui.Hide(paused);
            player.ui.Hide(paused);

            if (paused) camera.StopFollow();
            else        camera.Follow(player.cameraTarget);
        }

        #endregion


        public virtual void OpenGuide(GuideType type)
        {
            ui.guide.Display(type);

            if (type == GuideType.Attack)
                foreach (var enemy in planet.enemies) enemy.Walk();
        }

        #endregion
    }
}
