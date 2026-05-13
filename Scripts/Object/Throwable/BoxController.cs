using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Resources  = BoxController.Resources;
using DamageType = IDamageable.Type;


public interface IBoxController : IThrowableBase
{
    #region Property

    // Component
    new Resources resources { get; }

    #endregion
}


public class BoxController : ThrowableBase, IBoxController
{
    #region Definition

    public new class Resources : ThrowableBase.Resources
    {
        #region Definition

        public class Models : Dictionary<State, IModelController>
        {
            #region Field

            public IBoxModelController destroy { get; }

            #endregion


            #region Constructor

            public Models(Transform transform) : base()
            {
                destroy = transform.GetComponentInChildren<IBoxModelController>(true);

                foreach (var model in transform.GetComponentsInChildren<IModelController>(true))
                {
                    string name = model.gameObject.name.Replace(" ", string.Empty);

                    if (Enum.TryParse(name, out State type)) Add(type, model);
                }
            }

            #endregion


            #region Method

            public void Set(State state)
            {
                foreach (var element in this)
                {
                    State type  = element.Key;
                    var   model = element.Value;

                    model.gameObject.SetActive(type == state);
                }
            }

            #endregion
        }

        #endregion


        #region Field

        public Models            models { get; }
        public IEffectController effect { get; }

        #endregion


        #region Constructor

        public Resources(Transform transform) : base(transform)
        {
            models = new Models(transform.Find("Models"));
            effect = transform.GetComponentInChildren<IEffectController>(true);
        }

        #endregion
    }

    #endregion


    #region Field

    public new Resources resources { get; protected set; }

    #endregion


    #region Method

    #region Initialization

    protected override void SetField()
    {
        base.SetField();

        resources = new Resources(transform.Find("Resources"));
    }

    protected override void ResetField(Rigidbody rigidbody)
    {
        base.ResetField(rigidbody);

        _mask = DamageType.Normal | DamageType.PressDown | DamageType.Explode;
    }

    #endregion


    #region Idle

    public override void Idle()
    {
        base.Idle();
        resources.models.Set(state);
    }

    #endregion


    #region Destroy

    protected override IEnumerator _Destroy(bool useUnscaledTime)
    {
        resources.models.Set(state);

        float modelDuration  = resources.models.destroy.Play();
        float effectDuration = resources.effect.Play();
        float remainDuration = Mathf.Clamp(effectDuration - modelDuration, 0f, effectDuration);

        yield return useUnscaledTime ? new WaitForSecondsRealtime(modelDuration) : new WaitForSeconds(modelDuration);

        resources.models[state].gameObject.SetActive(false);

        yield return useUnscaledTime ? new WaitForSecondsRealtime(remainDuration) : new WaitForSeconds(remainDuration);

        Destroy(gameObject);
    }

    #endregion

    #endregion
}
