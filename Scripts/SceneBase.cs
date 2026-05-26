using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

using Game;

using Type                   = SceneBase.Type;
using LetterboxUIState       = Game.LetterboxUIController.State;
using CursorVisibleEventType = Game.GameDirector.CursorVisibleEventType;


public interface ISceneBase
{
    #region Property

    // Component
    IBackGroundMusicController bgm { get; }

    // Reference
    ICameraController camera { get; }

    // Setting
    public Type type { get; }

    #endregion


    #region Method

    public Coroutine Enter(Type prev);
    public Coroutine Exit(Type next);
    public void      Pause(bool paused, params IAudioBase[] exceptions);

    #endregion
}


public class SceneBase : MonoBehaviour, ISceneBase
{
    #region Definition

    public enum Type { None, Title, Tutorial, Lobby, Stage }

    #endregion


    #region Field

    public IBackGroundMusicController bgm    { get; protected set; }
    public new ICameraController      camera { get; protected set; }

    public Type type { get; protected set; }

    #endregion


    #region Method

    #region Event

    protected virtual void Awake() 
    {
        IGameDirector game = GameDirector.instance;

        SetField();

        if (game != null) game.scenes.Set(this);
    }

    protected virtual void Start()
    {
        IGameDirector game = GameDirector.instance;

        Enter(game.scenes.previous.Key);
    }

    #endregion


    #region Initialzation

    protected virtual void SetField()
    {
        bgm    = GetComponentInChildren<IBackGroundMusicController>(true);
        camera = FindObjectOfType<CameraController>(true);
    }

    #endregion


    #region Enter

    public virtual Coroutine Enter(Type prev)
    {
        IGameDirector game = GameDirector.instance;

        Time.timeScale = 1f;

        game.SetCursorVisible(CursorVisibleEventType.MenuOpen, false);

        return StartCoroutine(_Enter(prev));
    }

    protected virtual IEnumerator _Enter(Type prev)
    {
        IFadeUIController fadeUI = GameDirector.instance.ui.fade;

        yield return new WaitForSeconds(fadeUI.defaultDuration);
        yield return fadeUI.Display(false);

        bgm.Play();
    }

    #endregion


    #region Exit

    public virtual Coroutine Exit(Type next) { return StartCoroutine(_Exit(next)); }

    protected virtual IEnumerator _Exit(Type next)
    {
        IGameDirector          game      = GameDirector.instance;
        IFadeUIController      fadeUI    = game.ui.fade;
        ILetterboxUIController letterbox = game.ui.letterbox;

        bgm.Stop(fadeUI.defaultDuration);

        yield return fadeUI.Display(true);

        letterbox.Display(LetterboxUIState.Open, false);

        switch (next)
        {
            case Type.None: Application.Quit();                      break;
            default:        SceneManager.LoadScene(next.ToString()); break;
        }
    }

    #endregion


    #region Pause

    public virtual void Pause(bool paused, params IAudioBase[] exceptions)
    {
        IGameDirector game = GameDirector.instance;

        game.PauseAudios(paused, exceptions);

        Time.timeScale = paused ? 0f : 1f;
    }

    #endregion

    #endregion
}
