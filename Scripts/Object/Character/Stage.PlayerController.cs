using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Game;


namespace Stage
{
    using CameraTargets      = PlayerController.CameraTargets;
    using Resources          = PlayerController.Resources;
    using MainState          = global::PlayerController.State.Main;
    using JumpState          = global::PlayerController.State.Jump;
    using DieState           = global::PlayerController.State.Die;
    using OverlapState       = global::PlayerController.State.Overlap.State;
    using HandType           = global::PlayerModelController.Meshes.HandType;
    using PowerUpEffectState = PlayerPowerUpEffectController.State;
    using MoveSetting        = CharacterBase.CharacterSetting.Move;
    using DamageType         = IDamageable.Type;
    using PowerUpType        = PowerUpItemController.Type;
    using GoalType           = global::GoalController.Type;
    using MainUICountType    = MainUIController.Root.Main.Count.Type;
    using CharacterStatType  = Game.DataManager.Setting.CharacterStat.Type;
    using ChallengeType      = Game.DataManager.Setting.Stage.Level.Challenge.Type;


    public interface IPlayerController : global::IPlayerController
    {
        #region Property

        // Component
        CameraTargets cameraTargets { get; }
        new Resources resources     { get; }

        // Reference
        new ISceneDirector scene { get; }

        #endregion
    }


    public class PlayerController : global::PlayerController, IPlayerController
    {
        #region Definition

        public class CameraTargets : List<ICameraTargetController>
        {
            #region Field

            public IPlayerCameraTargetController main    { get; }
            public ICameraTargetController       gameSet { get; }

            #endregion


            #region Constructor

            public CameraTargets(Transform transform) : base(transform.GetComponentsInChildren<ICameraTargetController>(true))
            {
                main    = transform.GetComponentInChildren<IPlayerCameraTargetController>(true);
                gameSet = transform.Find("Game Set").GetComponent<ICameraTargetController>();
            }

            #endregion
        }


        public new class Resources : global::PlayerController.Resources
        {
            #region Field

            public new IPlayerModelController model { get; }

            #endregion


            #region Constructor

            public Resources(Transform transform) : base(transform)
            {
                model = transform.GetComponentInChildren<IPlayerModelController>(true);
            }

            #endregion
        }

        #endregion


        #region Field

        public CameraTargets      cameraTargets { get; protected set; }
        public new Resources      resources     { get; protected set; }
        public new ISceneDirector scene         { get; protected set; }

        #endregion


        #region Method

        #region Initialization

        protected override void SetField()
        {
            base.SetField();

            cameraTargets = new CameraTargets(transform.Find("Camera Targets"));
            resources     = new Resources(transform.Find("Resources"));
            scene         = FindObjectOfType<SceneDirector>(true);
        }

        #endregion


        #region Set

        public override void SetHitPoint(int amount)
        {
            IMainUIController ui    = scene.ui.main;
            IScoreManager     score = scene.score;

            int prevHitPoint   = state.hitPoint;
                state.hitPoint = Mathf.Clamp(state.hitPoint + amount, 0, setting.maxHitPoint);
            int _amount        = state.hitPoint - prevHitPoint;

            ui.SetContent(MainUICountType.HP, state.hitPoint, _amount);

            if (_amount < 0) score.TryIncrease(ChallengeType.Hit);
        }

        #endregion


        #region Action

        #region Run

        protected override IEnumerator _Run()
        {
            Game.IDataManager data = GameDirector.instance.data;

            Vector3 inputDirection = input.moveDirection;
            Vector3 prevDirection  = Vector3.zero;
            var     settings       = setting.run;
            var     defaultSetting = settings[PowerUpType.None];
            float   rate           = 1f + data.characterStats[CharacterStatType.RunSpeed].rate;
            var     moveSetting    = MoveSetting.MultiplySpeed(defaultSetting.move, rate);

            do
            {
                float speedRate  = moveSpeed / defaultSetting.move.speed;
                float inputAngle = Vector3.Angle(inputDirection, prevDirection);

                if ((speedRate > 0.9f) && ((inputAngle > 90f) || (inputDirection.magnitude <= 0f)))
                {
                    Brake();
                    yield break;
                }

                PowerUpType powerUpType       = state.overlap.powerUp;
                bool        useDefaultSetting = (powerUpType == PowerUpType.None) || !settings.ContainsKey(powerUpType);

                RotateAndMove(useDefaultSetting ? moveSetting : settings[powerUpType].move);
                resources.model.SetMoveRate(useDefaultSetting ? Mathf.Clamp(speedRate, 0f, 1f) : speedRate);

                yield return new WaitForFixedUpdate();

                prevDirection  = inputDirection;
                inputDirection = input.moveDirection;
            }
            while ((inputDirection.magnitude > 0f) || (moveSpeed > 0f));

            resources.model.PlayNext();
            Idle();
        }

        #endregion


        #region Jump

        public override Coroutine Jump(JumpState type)
        {
            Game.IDataManager data = GameDirector.instance.data;

            StopAction();

            if ((type == JumpState.Bounce) && state.overlap[OverlapState.CoolDown]) StopCoolDown(true);
            if (jumpKeepAction != null)
            {
                StopCoroutine(jumpKeepAction);

                jumpKeepAction = null;
            }

            state.main = MainState.Jump;
            state.jump = type;

            switch (type)
            {
                case JumpState.Turn: direction.transform.Rotate(Vector3.up * 180f); break;
                case JumpState.Long: resources.model.SetHand(HandType.Open);        break;
            }

            var   setting = this.setting.jump.GetOrDefault(type);
            float rate    = 1f + data.characterStats[CharacterStatType.JumpForce].rate;
            float force   = this.setting.jump.force * rate;

            if (setting.angle != 90f) StopMove();
            else                      rigidbody.velocity = Vector3.ProjectOnPlane(rigidbody.velocity, transform.up);

            SetHeight(setting.heightRate);
            rigidbody.AddForce(setting.GetForce(direction.transform, force), ForceMode.VelocityChange);
            triggers[DamageType.Foot].gameObject.SetActive(true);
            triggers[DamageType.Head].gameObject.SetActive(true);
            resources.Play(state.jump);

            return action = StartCoroutine(_Jump(type));
        }

        #endregion


        #region Attack

        public override Coroutine Attack(State.Attack type)
        {
            IScoreManager score = scene.score;

            score.TryIncrease(ChallengeType.Attack);

            return base.Attack(type);
        }

        #endregion


        #region Die

        protected override IEnumerator _Die(DieState type)
        {
            ICameraController        mainCamera    = scene.cameras.main;
            IGameSetCameraController gameSetCamera = scene.cameras.gameSet;

            switch (type)
            {
                case DieState.Normal:
                    {
                        float cameraTargetAngle = cameraTargets.main.transform.localRotation.eulerAngles.y;

                        cameraTargets.gameSet.transform.localRotation = Quaternion.Euler(Vector3.up * cameraTargetAngle);

                        gameSetCamera.Set(cameraTargets.main, 0f);
                        gameSetCamera.Set(cameraTargets.gameSet);
                    }
                    break;

                case DieState.Bungee:
                    {
                        mainCamera.LookAt(transform);
                        scene.FailLevel(type);
                    }
                    break;
            }

            yield return base._Die(type);
        }

        #endregion


        #region Goal

        protected override IEnumerator _Goal(GoalType type)
        {
            IGameSetCameraController gameSetCamera = scene.cameras.gameSet;

            yield return base._Goal(type);

            float cameraTargetAngle = cameraTargets.main.transform.localRotation.eulerAngles.y;

            cameraTargets.gameSet.transform.localRotation = Quaternion.Euler(Vector3.up * cameraTargetAngle);

            gameSetCamera.Set(cameraTargets.main, 0f);
            gameSetCamera.Set(cameraTargets.gameSet);
        }

        #endregion

        #endregion


        #region Overlap

        #region Power Up

        protected override IEnumerator _PowerUp(PowerUpType prevType, PowerUpType type)
        {
            Game.IDataManager data = GameDirector.instance.data;

            if (prevType != type)
            {
                scene.Pause(true);

                yield return new WaitForSecondsRealtime(resources.effects.powerUp.Play(PowerUpEffectState.On) * 0.5f);

                scene.Pause(false);
            }
            else resources.effects.powerUp.Play(PowerUpEffectState.On);

            OverlapState state    = OverlapState.PowerUp;
            float        rate     = 1f + data.characterStats[CharacterStatType.PowerUpDuration].rate;
            float        duration = setting.overlap[state].duration * rate;

            yield return ui.timer.Display(state, duration);

            StopPowerUp();
        }

        #endregion


        #region Cool Down

        protected override IEnumerator _CoolDown()
        {
            Game.IDataManager data = GameDirector.instance.data;

            OverlapState state    = OverlapState.CoolDown;
            float        rate     = 1f - data.characterStats[CharacterStatType.AttackCoolDown].rate;
            float        duration = setting.overlap[state].duration * rate;

            yield return ui.timer.Display(state, duration);

            StopCoolDown();
        }

        #endregion

        #endregion

        #endregion
    }
}
