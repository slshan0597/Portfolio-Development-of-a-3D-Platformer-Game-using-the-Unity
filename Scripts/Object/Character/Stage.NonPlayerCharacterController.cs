using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Stage
{
    using SubState = CharacterBase.State.Sub;


    public interface INonPlayerCharacterController : global::INonPlayerCharacterController
    {
        #region Property

        // Reference
        new IPlanetController planet { get; }

        #endregion
    }


    public class NonPlayerCharacterController : global::NonPlayerCharacterController, ITriggerable
    {
        #region Field

        public new IPlanetController planet { get; protected set; }

        [Header("Trigger Setting")]
        [SerializeField] protected bool _useTrigger;

        public bool useTrigger { get { return _useTrigger; } }

        [SerializeField] protected SimpleData<string, List<string>> scripts;

        protected bool firstMeet = true;

        #endregion


        #region Method

        #region Initialization

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

        #endregion


        #region Action

        #region Talk

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

            //yield return ui.Print(null);

            //state.sub = SubState.End;

            //if (useTrigger && planet.CheckClearable())
            //{
            //    resources.Play(state.main, state.sub);

            //    yield return ui.Display(false);
            //    yield return new WaitForSecondsRealtime(ui.defaultDuration);
            //    yield return planet.Clear(false);
            //}
            //else
            //{
            //    camera.Set(player.cameraTarget, ui.defaultDuration);

            //    yield return ui.Display(false);
            //}
        }

        #endregion

        #endregion

        #endregion
    }
}
