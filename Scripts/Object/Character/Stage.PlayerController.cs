// //////////////////////////////////////////////////////////////////////////////
// * 요약
//    - Stage 씬(Director)의 기능을 사용하기 위한 확장 클래스
//    - Stage 씬의 점수(Score) 부여
//    - 플레이어가 죽거나 골에 도착하면 Stage 씬 정지
//    - 글로벌 캐릭터 능력치(Stat)에 따라 플레이어의 행동력(이동속도 등) 변경
//
// * 목차
//    1. 인터페이스 ... Line 49
//    2. 클래스 ....... Line 63
//        1) 필드 ..... Line 68
//        2) 메서드 ... Line 76
//            1- 셋(Set) ... Line 89
//            2- 액션 ...... Line 107
//                1_ 달리기(Run) ...... Line 111
//                2_ 뛰기(Jump) ....... Line 153
//                3_ 공격(Attack) ..... Line 196
//                4_ 죽기(Die) ........ Line 209
//                5_ 도착하기(Goal) ... Line 244
//            5- 오버랩(Overlap) ... Line 264
//                2_ 파워업(Power Up) .... Line 267
//                3_ 쿨타임(Cool Down) ... Line 294
// //////////////////////////////////////////////////////////////////////////////
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

    // //////////////////////////////////////////////////////////////////////////////
    // 1. 인터페이스(IPlayerController 인터페이스 상속)
    // //////////////////////////////////////////////////////////////////////////////
    public interface IPlayerController : global::IPlayerController
    {
        // 프로퍼티
        // Component
        CameraTargets cameraTargets { get; }
        new Resources resources     { get; }

        // Reference
        new ISceneDirector scene { get; }
    }

    // //////////////////////////////////////////////////////////////////////////////
    // 2. 클래스(PlayerController 클래스 상속)
    // //////////////////////////////////////////////////////////////////////////////
    public class PlayerController : global::PlayerController, IPlayerController
    {
        // ==============================================================================
        // 1) 필드
        // ==============================================================================
        // Component & Reference
        public CameraTargets      cameraTargets { get; protected set; }
        public new Resources      resources     { get; protected set; }
        public new ISceneDirector scene         { get; protected set; }

        // ==============================================================================
        // 2) 메서드
        //    - 부모 클래스의 함수들을 재정의하여 확장
        // ==============================================================================
        protected override void SetField()
        {
            base.SetField();

            cameraTargets = new CameraTargets(transform.Find("Camera Targets"));
            resources     = new Resources(transform.Find("Resources"));
            scene         = FindObjectOfType<SceneDirector>(true);
        }

        // ------------------------------------------------------------------------------
        // 2-1) 메서드 -> 셋(Set)
        //    - 캐릭터의 체력 설정 후 UI 와 Score 정보 갱신
        // ------------------------------------------------------------------------------
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

        // ------------------------------------------------------------------------------
        // 2-2) 메서드 -> 액션
        // ------------------------------------------------------------------------------
        // ******************************************************************************
        // 2-2-1) 메서드 -> 액션 -> 달리기(Run)
        //    - 글로벌 캐릭터 능력치(Run Speed)에 따른 달리기 속도 조정
        // ******************************************************************************
        protected override IEnumerator _Run()
        {
            Game.IDataManager data = GameDirector.instance.data;

            Vector3 inputDirection = input.moveDirection;
            Vector3 prevDirection  = Vector3.zero;
            var     settings       = setting.run;
            var     defaultSetting = settings[PowerUpType.None];
            float   rate           = 1f + data.characterStats[CharacterStatType.RunSpeed].rate;    // 속도 조정
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

        // ******************************************************************************
        // 2-2-2) 메서드 -> 액션 -> 뛰기(Jump)
        //    - 글로벌 캐릭터 능력치(Jump Force)에 따른 점프 높이 조정
        // ******************************************************************************
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
            float rate    = 1f + data.characterStats[CharacterStatType.JumpForce].rate;    // 점프 높이 조정
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

        // ******************************************************************************
        // 2-2-3) 메서드 -> 액션 -> 공격(Attack)
        //    - 공격 횟수에 따른 Score 정보 갱신
        // ******************************************************************************
        public override Coroutine Attack(State.Attack type)
        {
            IScoreManager score = scene.score;

            score.TryIncrease(ChallengeType.Attack);

            return base.Attack(type);
        }

        // ******************************************************************************
        // 2-2-4) 메서드 -> 액션 -> 죽기(Die)
        //    - 실행 후 Stage 씬의 Fail Level 함수 호출(실질적 호출은 캐릭터 애니메이션 이벤트를 통해 호출됨)
        // ******************************************************************************
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

                        // scene.FailLevel(type);    캐릭터 애니메이션 이벤트를 통해 호출됨
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

        // ******************************************************************************
        // 2-2-5) 메서드 -> 액션 -> 도착하기(Goal)
        //    - 실행 후 Stage 씬의 Clear Level 함수 호출(실질적 호출은 캐릭터 애니메이션 이벤트를 통해 호출됨)
        // ******************************************************************************
        protected override IEnumerator _Goal(GoalType type)
        {
            IGameSetCameraController gameSetCamera = scene.cameras.gameSet;

            yield return base._Goal(type);

            float cameraTargetAngle = cameraTargets.main.transform.localRotation.eulerAngles.y;

            cameraTargets.gameSet.transform.localRotation = Quaternion.Euler(Vector3.up * cameraTargetAngle);

            gameSetCamera.Set(cameraTargets.main, 0f);
            gameSetCamera.Set(cameraTargets.gameSet);

            // scene.ClearLevel(type);    캐릭터 애니메이션 이벤트를 통해 호출됨
        }

        // ------------------------------------------------------------------------------
        // 2-3) 메서드 -> 오버랩(Overlap)
        // ------------------------------------------------------------------------------
        // ******************************************************************************
        // 2-3-1) 메서드 -> 오버랩 -> 파워업(Power Up)
        //    - 글로벌 캐릭터 능력치(Power Up Duration)에 따른 지속 시간 조정
        // ******************************************************************************
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
            float        rate     = 1f + data.characterStats[CharacterStatType.PowerUpDuration].rate;    // 지속 시간 조정
            float        duration = setting.overlap[state].duration * rate;

            yield return ui.timer.Display(state, duration);

            StopPowerUp();
        }

        // ******************************************************************************
        // 2-3-2) 메서드 -> 오버랩 -> 쿨타임(Cool Down)
        //    - 글로벌 캐릭터 능력치(Attack Cool Down)에 따른 대기 시간 조정
        // ******************************************************************************
        protected override IEnumerator _CoolDown()
        {
            Game.IDataManager data = GameDirector.instance.data;

            OverlapState state    = OverlapState.CoolDown;
            float        rate     = 1f - data.characterStats[CharacterStatType.AttackCoolDown].rate;    // 대기 시간 조정
            float        duration = setting.overlap[state].duration * rate;

            yield return ui.timer.Display(state, duration);

            StopCoolDown();
        }
    }
}
