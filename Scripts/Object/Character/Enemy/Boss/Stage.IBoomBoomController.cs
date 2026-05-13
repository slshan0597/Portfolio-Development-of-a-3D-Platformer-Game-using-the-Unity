using System.Collections;
using UnityEngine;

using Game;


namespace Stage
{
    using BossState        = BossBase.State.Main;
    using BGMType          = BackGroundMusicController.SoundType;
    using LetterboxUIState = LetterboxUIController.State;


    public interface IBoomBoomController : global::IBoomBoomController
    {
        #region Property

        // Reference
        new IBossPlanetController planet { get; }

        #endregion
    }


    public class BoomBoomController : global::BoomBoomController
    {
        #region Field

        public new IBossPlanetController planet { get; protected set; }

        #endregion


        #region Method

        #region Initialization

        protected override void SetField()
        {
            base.SetField();

            planet = GetComponentInParent<IBossPlanetController>(true);
        }

        #endregion


        #region Action

        #region Appear

        public override Coroutine Appear()
        {
            gameObject.SetActive(true);
            planet.Set(BossState.Appear);

            resources.voice.audioSource.spatialBlend = 0f;

            foreach (var effect in resources.effects.elements) effect.audioSource.spatialBlend = 0f;

            return base.Appear();
        }

        protected override IEnumerator _Appear(bool useUnscaledTime)
        {
            ILetterboxUIController     letterboxUI = GameDirector.instance.ui.letterbox;
            ISceneDirector             scene       = planet.level.stage.scene;
            IBackGroundMusicController bgm         = scene.audios.bgm;

            yield return letterboxUI.DisplayWithSkipButton(bossSetting.appear.duration);

            scene.Pause(false);
            bgm.Play(BGMType.Boss);
            Idle(letterboxUI.state == LetterboxUIState.Closed);
            letterboxUI.Display(LetterboxUIState.Open, false);
        }

        protected override void StopAppear()
        {
            base.StopAppear();

            resources.voice.audioSource.spatialBlend = 1f;

            foreach (var effect in resources.effects.elements) effect.audioSource.spatialBlend = 1f;
        }

        #endregion


        #region Die

        public override Coroutine Die()
        {
            planet.Set(BossState.Die);

            resources.voice.audioSource.spatialBlend = 0f;

            foreach (var effect in resources.effects.elements) effect.audioSource.spatialBlend = 0f;

            return base.Die();
        }

        protected override IEnumerator _Die()
        {
            yield return base._Die();

            planet.Clear(false);
        }

        protected override void StopDie()
        {
            base.StopDie();

            resources.voice.audioSource.spatialBlend = 1f;

            foreach (var effect in resources.effects.elements) effect.audioSource.spatialBlend = 1f;
        }

        #endregion

        #endregion

        #endregion
    }
}
