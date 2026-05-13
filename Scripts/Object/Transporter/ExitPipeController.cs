using System;
using System.Collections;
using UnityEngine;

using JumpSetting     = ExitPipeController.JumpSetting;
using PlayerLandState = PlayerController.State.Land;
using PlayerJumpState = PlayerController.State.Jump;


public interface IExitPipeController : IPipeController
{
    #region Property

    // Setting
    JumpSetting jumpSetting { get; }

    #endregion
}


public class ExitPipeController : PipeController, IExitPipeController
{
    #region Definition

    [Serializable] public struct JumpSetting
    {
        #region Field

        [SerializeField]                 private float _force;
        [SerializeField, Range(0f, 90f)] private float _angle;

        public float force { get { return _force; } }
        public float angle { get { return _angle; } }

        #endregion


        #region Constructor

        public JumpSetting(float force, float angle)
        {
            _force = force;
            _angle = angle;
        }

        #endregion


        #region Method

        public Vector3 GetForce(IPlayerController player)
        {
            float   angle     = this.angle * Mathf.Deg2Rad;
            Vector3 direction = (Vector3.forward * Mathf.Cos(angle)) + (Vector3.up * Mathf.Sin(angle));
            Vector3 force     = Mathf.Sqrt(this.force) * direction;

            return player.transform.TransformDirection(force);
        }

        #endregion
    }

    #endregion


    #region Field

    [SerializeField] protected JumpSetting _jumpSetting;

    public JumpSetting jumpSetting { get { return _jumpSetting; } }

    protected Coroutine jumpAction;

    #endregion


    #region Method

    #region Event

    protected override void Start()
    {
        base.Start();
        gameObject.SetActive(false);
    }

    #endregion


    #region Initialization

    protected override void ResetField()
    {
        base.ResetField();

        _jumpSetting = new JumpSetting(250f, 75f);
    }

    #endregion


    #region Disappear

    protected override IEnumerator _Disappear(float duration, bool useUnscaledTime)
    {
        yield return useUnscaledTime ? new WaitForSecondsRealtime(duration) : new WaitForSeconds(duration);
    }

    #endregion


    #region Exit

    public override Coroutine Exit(IPlayerController player)
    {
        gameObject.SetActive(true);

        return base.Exit(player);
    }

    protected override IEnumerator _Exit(IPlayerController player)
    {
        yield return Appear();
        yield return base._Exit(player);
        yield return JumpAndLand(player);
        yield return Disappear();

        gameObject.SetActive(false);
    }

    protected virtual IEnumerator JumpAndLand(IPlayerController player)
    {
        Vector3 force = jumpSetting.GetForce(player);

        player.rigidbody.AddForce(force, ForceMode.VelocityChange);
        player.resources.Play(PlayerJumpState.Low);

        int count    = 0;
        int maxCount = 5;

        while (player.groundState.isGrounded && (count < maxCount))
        {
            yield return new WaitForFixedUpdate();
            count++;
        }

        while (!player.groundState.isGrounded) yield return new WaitForFixedUpdate();

        player.resources.Play(PlayerLandState.Light);
    }

    #endregion

    #endregion
}
