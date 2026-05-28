// //////////////////////////////////////////////////////////////////////////////
// * 요약
//    - 게임 상의 모든 오디오에 대한 기반 클래스
//    - 타입에 따른 오디오 볼륨 조절
//    - 오디오 시작, 정지, 중단 기능
//
// * 목차
//    1. 인터페이스 ... Line 33
//    2. 클래스 ....... Line 53
//        1) 필드 ..... Line 62
//        2) 메서드 ... Line 77
//            1- 이벤트 함수 ... Line 80
//            2- 초기화 ........ Line 109
//            3- 셋(Set) ....... Line 128
//            4- 재생(Play) .... Line 144
//            5- 정지(Stop) .... Line 158
//            6- 중단(Pause) ... Line 204
// //////////////////////////////////////////////////////////////////////////////
using System.Collections;
using UnityEngine;

using Game;

using AudioType = AudioBase.Type;
using AudioData = Game.AudioSettingManager.Data;

// //////////////////////////////////////////////////////////////////////////////
// 1. 인터페이스
// //////////////////////////////////////////////////////////////////////////////
public interface IAudioBase
{
    // 프로퍼티
    // Component
    GameObject  gameObject  { get; }
    AudioSource audioSource { get; }

    // Setting
    AudioType audioType { get; }

    // 메서드
    void  Set(AudioData data);
    float Play();
    float Stop(float fadeDuration = 0f);
    void  Pause(bool isOn);
}

// //////////////////////////////////////////////////////////////////////////////
// 2. 클래스
//    - 클래스 확장을 위한 기반 기능만을 구현
// //////////////////////////////////////////////////////////////////////////////
[RequireComponent(typeof(AudioSource))]
public class AudioBase : MonoBehaviour, IAudioBase
{
    public enum Type { BGM, Voice, Effect, System }

    // ==============================================================================
    // 1) 필드
    // ==============================================================================
    // Component & Reference
    public AudioSource audioSource { get; protected set; }

    // Setting
    [SerializeField] protected Type _audioType;

    public Type audioType { get { return _audioType; } }

    // etc.
    protected Coroutine fadeAction;
    protected float     originVolume;

    // ==============================================================================
    // 2) 메서드
    // ==============================================================================
    // ------------------------------------------------------------------------------
    // 2-1) 메서드 -> 이벤트 함수
    //    - 오브젝트 초기화
    //    - 활성화 시 오디오 세팅 매니저와 연결
    // ------------------------------------------------------------------------------
    protected virtual void Awake() { SetField(); }

    protected virtual void Reset()
    {
        if (!TryGetComponent(out AudioSource audioSource)) return;

        ResetField(audioSource);
    }

    protected virtual void Start()
    {
        IAudioSettingManager audioSetting = GameDirector.instance.menu.setting.audio;

        audioSetting.connectedAudios.Add(this);
        Set(audioSetting.data);
    }

    protected virtual void OnDestroy()
    {
        IAudioSettingManager audioSetting = GameDirector.instance.menu.setting.audio;

        audioSetting.connectedAudios.Remove(this);
    }

    // ------------------------------------------------------------------------------
    // 2-2) 메서드 -> 초기화
    //    - 필드(컴포넌트 등) 초기화
    // ------------------------------------------------------------------------------
    protected virtual void SetField() { audioSource = GetComponentInChildren<AudioSource>(true); }

    protected virtual void ResetField(AudioSource audioSource)
    {
        audioSource.mute         = false;
        audioSource.playOnAwake  = false;
        audioSource.loop         = false;
        audioSource.volume       = 0f;
        audioSource.dopplerLevel = 0f;
        audioSource.spread       = 0f;
        audioSource.rolloffMode  = AudioRolloffMode.Linear;
        audioSource.minDistance  = 15f;
        audioSource.maxDistance  = 25f;
    }

    // ------------------------------------------------------------------------------
    // 2-3) 메서드 -> 셋(Set)
    //    - 매니저를 통해 전달받은 데이터 값으로 볼륨 조절
    // ------------------------------------------------------------------------------
    public virtual void Set(AudioData data)
    {
        if (fadeAction != null) StopFadeOut();

        float value  = data.details[audioType] * 0.1f;
        float master = data.master * 0.1f;

        audioSource.volume = value * master;
        //audioSource.mute   = data.details[audioType].mute || data.full.mute;
        audioSource.mute   = false;
    }

    // ------------------------------------------------------------------------------
    // 2-4) 메서드 -> 재생(Play)
    // ------------------------------------------------------------------------------
    public virtual float Play()
    {
        if (fadeAction       != null) StopFadeOut();
        if (audioSource.clip == null) return default;

        if (audioSource.loop) audioSource.Play();
        else                  audioSource.PlayOneShot(audioSource.clip);

        return audioSource.clip.length;
    }

    // ------------------------------------------------------------------------------
    // 2-5) 메서드 -> 정지(Stop)
    //    - 지정된 시간동안 페이드 아웃
    // ------------------------------------------------------------------------------
    public virtual float Stop(float duration = 0f)
    {
        if ((fadeAction != null) || !audioSource.isPlaying) return default;

        //fadeAction = StartCoroutine(FadeOut(duration, Time.timeScale == 0f));
        fadeAction = StartCoroutine(FadeOut(duration, true));

        return duration;
    }

    protected virtual IEnumerator FadeOut(float duration, bool useUnscaledTime)
    {
        originVolume = audioSource.volume;

        float elapsedTime  = 0f;

        while (elapsedTime < duration)
        {
            float rate = elapsedTime / duration;

            audioSource.volume = Mathf.Lerp(originVolume, 0f, rate);

            elapsedTime += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            yield return null;
        }

        audioSource.Stop();

        audioSource.volume = originVolume;
        fadeAction         = null;
    }

    protected virtual void StopFadeOut()
    {
        StopCoroutine(fadeAction);

        audioSource.Stop();

        audioSource.volume = originVolume;
        fadeAction         = null;
    }

    // ------------------------------------------------------------------------------
    // 2-6) 메서드 -> 중단(Pause)
    //    - 씬이 일시 정지 중일 때 오디오를 잠시 멈춤
    // ------------------------------------------------------------------------------
    public virtual void Pause(bool isOn)
    {
        if (isOn) audioSource.Pause();
        else      audioSource.UnPause();
    }
}
