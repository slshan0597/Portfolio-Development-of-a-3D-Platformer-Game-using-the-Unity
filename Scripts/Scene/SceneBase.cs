// //////////////////////////////////////////////////////////////////////////////
// * 요약
//    - 씬 디렉터의 기반 클래스
//
// * 목차
//    1. 인터페이스 ... Line 
//    2. 클래스 ....... Line 
//        1) 내부 타입 ... Line 
//        2) 필드 ........ Line 
//        3) 메서드 ...... Line 
//            1- 이벤트 함수 ....... Line 
//            2- 초기화 ............ Line 
//            3- 들어오기(Enter) ... Line 
//            4- 나가기(Exit) ...... Line 
//            5- 일시정지(Pause) ... Line 
// //////////////////////////////////////////////////////////////////////////////
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

using Game;

using Type                   = SceneBase.Type;
using LetterboxUIState       = Game.LetterboxUIController.State;
using CursorVisibleEventType = Game.GameDirector.CursorVisibleEventType;

// //////////////////////////////////////////////////////////////////////////////
// 1. 인터페이스
// //////////////////////////////////////////////////////////////////////////////
public interface ISceneBase
{
    // 프로퍼티
    // Component
    IBackGroundMusicController bgm { get; }

    // Reference
    ICameraController camera { get; }

    // State
    public Type type { get; }

    // 메서드
    public Coroutine Enter(Type prev);
    public Coroutine Exit(Type next);
    public void      Pause(bool paused, params IAudioBase[] exceptions);
}

// //////////////////////////////////////////////////////////////////////////////
// 2. 클래스
// //////////////////////////////////////////////////////////////////////////////
public class SceneBase : MonoBehaviour, ISceneBase
{
    // ==============================================================================
    // 1) 내부 타입
    // ==============================================================================
    public enum Type { None, Title, Tutorial, Lobby, Stage }

    // ==============================================================================
    // 2) 필드
    // ==============================================================================
    // Component & Reference
    public IBackGroundMusicController bgm    { get; protected set; }
    public new ICameraController      camera { get; protected set; }

    // State
    public Type type { get; protected set; }

    // ==============================================================================
    // 3) 메서드
    // ==============================================================================
    // ------------------------------------------------------------------------------
    // 3-1) 메서드 -> 이벤트 함수
    // ------------------------------------------------------------------------------
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

    // ------------------------------------------------------------------------------
    // 3-2) 메서드 -> 초기화
    // ------------------------------------------------------------------------------
    protected virtual void SetField()
    {
        bgm    = GetComponentInChildren<IBackGroundMusicController>(true);
        camera = FindObjectOfType<CameraController>(true);
    }

    // ------------------------------------------------------------------------------
    // 3-3) 메서드 -> 들어오기(Enter)
    //    - 현재 씬으로 넘어옴
    // ------------------------------------------------------------------------------
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

    // ------------------------------------------------------------------------------
    // 3-4) 메서드 -> 나가기(Exit)
    //    - 다음 씬으로 넘어가거나 프로그램 종료
    // ------------------------------------------------------------------------------
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

    // ------------------------------------------------------------------------------
    // 3-5) 메서드 -> 일시정지(Pause)
    //    - 현재 씬에서 메뉴 호출 등 특수 기능을 위한 게임의 일시정지
    // ------------------------------------------------------------------------------
    public virtual void Pause(bool paused, params IAudioBase[] exceptions)
    {
        IGameDirector game = GameDirector.instance;

        game.PauseAudios(paused, exceptions);

        Time.timeScale = paused ? 0f : 1f;
    }
}
