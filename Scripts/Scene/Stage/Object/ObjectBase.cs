using UnityEngine;


namespace Stage
{
    public interface ITriggerable
    {
        #region Property

        // Reference
        IPlanetController planet { get; }

        // Setting
        bool useTrigger { get; }

        #endregion
    }


    public interface IRewardable
    {
        #region Property

        // Component
        GameObject gameObject { get; }
        Transform  transform  { get; }

        // Reference
        IPlanetController planet { get; }

        #endregion


        #region Method

        Coroutine Appear();

        #endregion
    }


    public interface ICheckPointable
    {
        #region Property

        // Component
        ICharacterTargetController characterTarget { get; }

        // Reference
        ILevelController level { get; }

        // Setting
        bool useCheckPoint { get; }

        #endregion
    }
}
