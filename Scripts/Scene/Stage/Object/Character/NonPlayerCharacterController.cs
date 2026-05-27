// //////////////////////////////////////////////////////////////////////////////
// * 요약
//    - Stage 씬(Director)의 기능을 사용하기 위한 확장 클래스
//    - 플레이어와 대화 시 필드(Planet)의 클리어 트리거 작동
//
// * 목차
//    1. 인터페이스 ... Line 24
//    2. 클래스 ....... Line 34
//        1) 필드 ..... Line 39
//        2) 메서드 ... Line 56
//            1- 초기화 ... Line 60
//            2- 액션 ..... Line 110
//                1_ 대화(Talk) ... Line 113
// //////////////////////////////////////////////////////////////////////////////
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Stage
{
    using SubState = CharacterBase.State.Sub;

    // //////////////////////////////////////////////////////////////////////////////
    // 1. 인터페이스(INonPlayerCharacterController 인터페이스 상속)
    // //////////////////////////////////////////////////////////////////////////////
    public interface INonPlayerCharacterController : global::INonPlayerCharacterController
    {
        // 프로퍼티
        // Reference
        new IPlanetController planet { get; }
    }

    // //////////////////////////////////////////////////////////////////////////////
    // 2. 클래스(NonPlayerCharacterController 클래스 상속)
    // //////////////////////////////////////////////////////////////////////////////
    public class NonPlayerCharacterController : global::NonPlayerCharacterController, ITriggerable
    {
        // ==============================================================================
        // 1) 필드
        // ==============================================================================
        // Component & Reference
        public new IPlanetController planet { get; protected set; }

        // Setting
        [Header("Trigger Setting")]
        [SerializeField] protected bool _useTrigger;

        public bool useTrigger { get { return _useTrigger; } }

        [SerializeField] protected SimpleData<string, List<string>> scripts;

        // etc.
        protected bool firstMeet = true;

        // ==============================================================================
        // 2) 메서드
        //    - 부모 클래스의 함수들을 재정의하여 확장
        // ==============================================================================
        // ------------------------------------------------------------------------------
        // 2-1) 메서드 -> 초기화
        //    - 필드(컴포넌트 등) 초기화
        // ------------------------------------------------------------------------------
        protected override void SetField()
        {
            base.SetField();

            planet = GetComponentInParent<IPlanetController>(true);
        }

        protected override void ResetField(Rigidbody rigidbody)
        {
            base.ResetField(rigidbody);

            scripts = new SimpleData<string, List<string>>(
                new List<SimpleData<string, List<string>>.Element>()
                {
                    new SimpleData<string, List<string>>.Element(
                        "First Meet",
                        new List<string>()
                        {
                            "Printing First Meet Script Line 01",
                            "Printing First Meet Script Line 02",
                            "Printing First Meet Script Line 03",
                        }),
                    new SimpleData<string, List<string>>.Element(
                        "Quest Order",
                        new List<string>()
                        {
                            "Printing Quest Order Script Line 01",
                            "Printing Quest Order Script Line 02",
                        }),
                    new SimpleData<string, List<string>>.Element(
                        "Quest Clear",
                        new List<string>()
                        {
                            "Printing Quest Clear Script Line 01",
                            "Printing Quest Clear Script Line 02",
                            "Printing Quest Clear Script Line 03",
                        }),
                    new SimpleData<string, List<string>>.Element(
                        "Quest End",
                        new List<string>()
                        {
                            "Printing Quest End Script Line 01"
                        })
                });
        }

        // ------------------------------------------------------------------------------
        // 2-2) 메서드 -> 액션
        // ------------------------------------------------------------------------------
        // ******************************************************************************
        // 2-2-1) 메서드 -> 액션 -> 대화(Talk)
        //    - 다음 필드(Planet)로 진행하거나 레벨 클리어를 위한 트리거 기능 추가
        // ******************************************************************************
        protected override IEnumerator _Talk(global::IPlayerController player)
        {
            ISceneDirector                  scene  = planet.level.stage.scene;
            ICameraController               camera = scene.cameras.main;
            IPlayerConversationUIController ui     = player.ui.conversation;

            state.sub = SubState.Start;

            camera.Set(direction.GetCameraTarget(player), ui.defaultDuration);

            yield return ui.Display(true);

            state.sub = SubState.Loop;

            resources.Play(state.main, state.sub);

            if (useTrigger)
            {
                if (planet.reward.gameObject.activeInHierarchy)
                {
                    yield return ui.Print(scripts["Quest End"]);

                    state.sub = SubState.End;

                    camera.Set(player.cameraTarget, ui.defaultDuration);

                    yield return ui.Display(false);
                }
                else
                {
                    if (firstMeet)
                    {
                        firstMeet = false;

                        yield return ui.Print(scripts["First Meet"]);
                    }

                    // 조건부 트리거
                    if (planet.CheckClearable())
                    {
                        yield return ui.Print(scripts["Quest Clear"]);

                        state.sub = SubState.End;

                        resources.Play(state.main, state.sub);

                        yield return ui.Display(false);
                        yield return new WaitForSecondsRealtime(ui.defaultDuration);
                        yield return planet.Clear(false);
                    }
                    else
                    {
                        yield return ui.Print(scripts["Quest Order"]);

                        state.sub = SubState.End;

                        camera.Set(player.cameraTarget, ui.defaultDuration);

                        yield return ui.Display(false);
                    }
                }
            }
            else
            {
                yield return ui.Print(setting.talk.script);

                state.sub = SubState.End;

                camera.Set(player.cameraTarget, ui.defaultDuration);

                yield return ui.Display(false);
            }

            Farewell();
        }
    }
}
