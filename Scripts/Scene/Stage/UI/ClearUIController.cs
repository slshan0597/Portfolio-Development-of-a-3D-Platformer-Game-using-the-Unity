// //////////////////////////////////////////////////////////////////////////////
// * 요약
//    - 스테이지 씬의 성공 UI 클래스
//    - 종료된 시점의 스코어(Score) 정보 표시
//
// * 목차
//    1. 인터페이스 ... Line 
//    2. 클래스 ....... Line 
//        1) 내부 타입 ... Line 
//            1- 메인 ..... Line 
//            2- 나가기 ... Line 
//        2) 필드 ..... Line 
//        3) 메서드 ... Line 
//            1- 초기화 .......... Line 
//            2- 셋(Set) ......... Line 
//            3- 표시(Display) ... Line 
//            4- 나가기 .......... Line 
// //////////////////////////////////////////////////////////////////////////////
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

using Game;

namespace Stage
{
    using Root                = ClearUIController.Root;
    using ChallengeData       = Game.DataManager.Stages.Stage.Levels.Level.Challenge;
    using ChallengeType       = Game.DataManager.Setting.Stage.Level.Challenge.Type;
    using RewardConditionType = ScoreManager.RewardConditionType;
    using ControlState        = ControlSettingManager.State;

    // //////////////////////////////////////////////////////////////////////////////
    // 1. 인터페이스(IUIBase 인터페이스 상속)
    // //////////////////////////////////////////////////////////////////////////////
    public interface IClearUIController : IUIBase
    {
        // 프로퍼티
        // Component
        Root root { get; }

        // Reference
        ISceneDirector scene { get; }
    }

    // //////////////////////////////////////////////////////////////////////////////
    // 2. 클래스(UIBase 클래스 상속)
    // //////////////////////////////////////////////////////////////////////////////
    public class ClearUIController : UIBase, IClearUIController
    {
        // ==============================================================================
        // 1) 내부 타입 - 구조
        // ==============================================================================
        public class Root : SlotBase
        {
            // ------------------------------------------------------------------------------
            // 1-1) 내부 타입 - 구조 -> 메인
            //    - 스코어 정보 표시
            // ------------------------------------------------------------------------------
            public class Main : WindowBase
            {
                public class Score : WindowBase
                {
                    public Text contentText { get; }
                    
                    public Score(Transform transform) : base(transform)
                    {
                        contentText = content.GetComponentInChildren<Text>(true);
                    }

                    public void SetContent(IScoreManager score, Dictionary<ChallengeType, ChallengeData> challengeDatas)
                    {
                        string        time      = $"Time\t\t\t: {score.elapsedTime.ToString(@"mm\:ss\:ff")}";
                        string        coin      = $"Coin\t\t\t: {score.coin}";
                        StringBuilder challenge = new StringBuilder();

                        foreach (var challengeData in challengeDatas.Values)
                            challenge.Append(challengeData.cleared ? "★" : "☆");

                        string        _challenge = $"Challenge\t: {challenge}";
                        int           reward     = 0;
                        StringBuilder _reward    = new StringBuilder();

                        foreach (var element in score.rewards)
                        {
                            RewardConditionType type  = element.Key;
                            int                 value = element.Value;

                            reward += value;

                            if (value > 0) _reward.Append($"({type} : {value}) + ");
                        }

                        if (_reward.Length > 1) _reward = _reward.Remove(_reward.Length - 2, 2);

                        string __reward = (reward > 0) ? $"Reward\t: {reward} [{_reward}]" : $"Reward\t: 0";

                        contentText.text = $"{time}\n{coin}\n{_challenge}\n\n{__reward}";
                    }
                }

                public Score score { get; }

                public Main(Transform transform) : base(transform) { score = new Score(content.Find("Score")); }
            }

            // ------------------------------------------------------------------------------
            // 1-2) 내부 타입 - 구조 -> 나가기
            //    - 현재 입력 상태에 따른 나가기 텍스트 표시
            // ------------------------------------------------------------------------------
            public class Exit : MenuBase
            {
                public Text text { get; }

                public Exit(Transform transform) : base(transform)
                {
                    text = content.GetComponentInChildren<Text>(true);
                }
            }

            // 필드 - 구조
            public Main main { get; }
            public Exit exit { get; }

            // 생성자 - 구조
            public Root(Transform transform) : base(transform)
            { 
                main = new Main(content.Find("Main"));
                exit = new Exit(content.Find("Exit"));
            }

            // 메서드 - 구조
            public void SetContent(IScoreManager score, Dictionary<ChallengeType, ChallengeData> challengeDatas,
                IControlSettingManager controlSetting)
            {
                main.score.SetContent(score, challengeDatas);

                switch (controlSetting.state)
                {
                    case ControlState.Touch: exit.text.text = "- Touch Screen -";     break;
                    default:                 exit.text.text = "- Press Any Button -"; break;
                }
            }
        }

        // ==============================================================================
        // 2) 필드
        // ==============================================================================
        // Component & Reference
        public Root           root  { get; protected set; }
        public ISceneDirector scene { get; protected set; }

        // ==============================================================================
        // 3) 메서드
        // ==============================================================================
        // ------------------------------------------------------------------------------
        // 3-1) 메서드 -> 초기화
        // ------------------------------------------------------------------------------
        protected override void SetField()
        {
            base.SetField();

            root  = new Root(transform);
            scene = GetComponentInParent<ISceneDirector>(true);
        }

        protected override void ResetField() { _defaultDuration = 1f; }

        // ------------------------------------------------------------------------------
        // 3-2) 메서드 -> 셋(Set)
        //    - 컨트롤 설정과 스코어 정보를 통해 UI 갱신
        // ------------------------------------------------------------------------------
        public override void Set()
        {
            base.Set();

            IScoreManager          score          = scene.score;
            IGameDirector          game           = GameDirector.instance;
            Game.IDataManager      data           = game.data;
            IControlSettingManager controlSetting = game.menu.setting.control;

            var challengeDatas = data.stages.Current.levels.Current.challenges;

            root.SetContent(score, challengeDatas, controlSetting);
        }

        // ------------------------------------------------------------------------------
        // 3-3) 메서드 -> 표시(Display)
        // ------------------------------------------------------------------------------
        protected override IEnumerator _Display(bool isActive, float duration)
        {
            if (!isActive) yield return base._Display(isActive, duration);
            else
            {
                IGameSetCameraController camera = scene.cameras.gameSet;

                root.main.gameObject.SetActive(false);
                root.exit.gameObject.SetActive(false);

                yield return FadeLabel(duration);
                //yield return new WaitForSecondsRealtime(duration);

                camera.Rotate(Vector3.up * 25f, duration);

                yield return FadeMain(duration);

                root.exit.gameObject.SetActive(true);

                yield return FadeGraphics(root.exit, true, duration * 0.5f);

                StartCoroutine(TryExit());
            }
        }

        protected virtual IEnumerator FadeLabel(float duration)
        {
            IAnimationCurvePreset curvePreset = GameDirector.instance.curvePreset;

            var rectTransform = root.labelText.rectTransform;

            Vector3 originScale = root.initialSettings.rectTransforms[rectTransform].localScale;
            Vector3 startScale  = Vector3.zero;
            Vector3 endScale    = originScale;
            float   elapsedTime = 0f;
            var     curveType   = curvePreset.types[1];

            while (elapsedTime < defaultDuration)
            {
                float rate = curveType.Evaluate(elapsedTime / duration);

                rectTransform.localScale = Vector2.Lerp(startScale, endScale, rate);

                elapsedTime += Time.unscaledDeltaTime;
                yield return null;
            }

            rectTransform.localScale = endScale;
        }

        protected virtual IEnumerator FadeMain(float duration)
        {
            IAnimationCurvePreset curvePreset = GameDirector.instance.curvePreset;

            root.main.gameObject.SetActive(true);

            var rectTransform  = root.main.rectTransform;

            var     initialSetting = root.main.initialSettings.rectTransforms[rectTransform];
            Vector2 originPosition = initialSetting.anchoredPosition;
            float   width          = (canvas.transform as RectTransform).sizeDelta.x;
            Vector2 startPosition  = originPosition + (Vector2.right * width);
            Vector2 endPosition    = originPosition;
            float   elapsedTime    = 0f;
            var     curveType      = curvePreset.types[1];

            while (elapsedTime < duration)
            {
                float rate = curveType.Evaluate(elapsedTime / duration);

                rectTransform.anchoredPosition = Vector2.Lerp(startPosition, endPosition, rate);

                elapsedTime += Time.unscaledDeltaTime;
                yield return null;
            }

            rectTransform.anchoredPosition = endPosition;
        }

        // ------------------------------------------------------------------------------
        // 3-4) 메서드 -> 나가기
        //    - 아무 키나 입력받은 후 씬을 종료하고 로비 씬으로 전환
        // ------------------------------------------------------------------------------
        protected virtual IEnumerator TryExit()
        {
            IControlSettingManager controlSetting = GameDirector.instance.menu.setting.control;

            while (true)
            {
                switch (controlSetting.state)
                {
                    case ControlState.Touch: if (Input.touchCount > 0) goto End; break;
                    default:                 if (Input.anyKeyDown)     goto End; break;
                }

                yield return null;
            }

            End:

            SetInteractables(false);
            scene.Exit(SceneBase.Type.Lobby);
        }
    }
}
