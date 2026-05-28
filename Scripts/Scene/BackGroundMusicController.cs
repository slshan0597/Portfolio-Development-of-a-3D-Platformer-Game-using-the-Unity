// //////////////////////////////////////////////////////////////////////////////
// * 요약
//    - 각 씬의 메인 BGM 오디오 클래스
//
// * 목차
//    1. 인터페이스 ... Line 17
//    2. 클래스 ....... Line 27
//        1) 필드 ..... Line 32
//        2) 메서드 ... Line 38
//            1- 초기화 ... Line 41
// //////////////////////////////////////////////////////////////////////////////
using UnityEngine;

using AudioType = AudioBase.Type;

// //////////////////////////////////////////////////////////////////////////////
// 1. 인터페이스(IAudioBase 인터페이스 상속)
// //////////////////////////////////////////////////////////////////////////////
public interface IBackGroundMusicController : IAudioBase
{
    // 프로퍼티
    // Reference
    ISceneBase scene { get; }
}

// //////////////////////////////////////////////////////////////////////////////
// 2. 클래스(AudioBase 클래스 상속)
// //////////////////////////////////////////////////////////////////////////////
public class BackGroundMusicController : AudioBase, IBackGroundMusicController
{
    // ==============================================================================
    // 1) 필드
    // ==============================================================================
    // Component & Reference
    public ISceneBase scene { get; protected set; }

    // ==============================================================================
    // 2) 메서드
    // ==============================================================================
    // ------------------------------------------------------------------------------
    // 2-1) 메서드 -> 초기화
    // ------------------------------------------------------------------------------
    protected override void SetField()
    {
        base.SetField();

        scene = GetComponentInParent<ISceneBase>(true);
    }

    protected override void ResetField(AudioSource audioSource)
    {
        base.ResetField(audioSource);

        audioSource.loop         = true;
        audioSource.spatialBlend = 0f;
        _audioType               = AudioType.BGM;
    }
}
