// //////////////////////////////////////////////////////////////////////////////
// * 요약
//    - 기반 클래스(ProjectileBase)의 확장 클래스
//    - 타겟(좌표)을 향해 발사, 포물선 궤도로 이동
//
// * 목차
//    1. 인터페이스 ... Line 
//    2. 클래스 ....... Line 
//        1) 필드 ..... Line 
//        2) 메서드 ... Line 
//            1- 초기화 ... Line 
//            2- 액션 ..... Line 
//                1_ 발사(Launch) .... Line 
//                2_ 파괴(Destroy) ... Line 
// //////////////////////////////////////////////////////////////////////////////
using System.Collections;
using UnityEngine;

// //////////////////////////////////////////////////////////////////////////////
// 1. 인터페이스(IProjectileBase 인터페이스 상속)
// //////////////////////////////////////////////////////////////////////////////
public interface IHammerController : IProjectileBase
{
    // 프로퍼티
    // Setting
    float modelRPS { get; }
}

// //////////////////////////////////////////////////////////////////////////////
// 2. 클래스(ProjectileBase 클래스 상속)
// //////////////////////////////////////////////////////////////////////////////
public class HammerController : ProjectileBase, IHammerController
{
    // ==============================================================================
    // 1) 필드
    // ==============================================================================
    // Setting
    [SerializeField] protected float _modelRPS;

    public float modelRPS { get { return _modelRPS; } }

    // ==============================================================================
    // 2) 메서드
    // ==============================================================================
    // ------------------------------------------------------------------------------
    // 2-1) 메서드 -> 초기화
    // ------------------------------------------------------------------------------
    protected override void ResetField(Rigidbody rigidbody)
    {
        base.ResetField(rigidbody);

        _launchSetting = new LaunchSetting(60f, 0f, 25f);
        _modelRPS      = 2f;
    }

    // ------------------------------------------------------------------------------
    // 2-2) 메서드 -> 액션
    // ------------------------------------------------------------------------------
    // ******************************************************************************
    // 2-2-1) 메서드 -> 액션 -> 발사(Launch)
    //    - 타겟(좌표)을 향해 지정된 각도와 속도를 통해 포물선 궤도를 계산하여 발사
    //    - 지정된 시간이 지나면 오브젝트 파괴
    // ******************************************************************************
    protected override IEnumerator _Launch(Transform target)
    {
        float   angle         = launchSetting.angle * Mathf.Deg2Rad;
        float   speed         = launchSetting.speed;
        float   gravity       = GetGravity(target, angle, speed);
        Vector3 startPosition = transform.position;
        float   elapsedTime   = 0f;

        while (elapsedTime < lifeSpan)
        {
            Vector3 nextAmount = GetNextAmount(angle, speed, gravity, elapsedTime);

            transform.position = startPosition + nextAmount;
            rigidbody.velocity = Vector3.zero;

            resources.model.transform.Rotate(Vector3.right * 360f * modelRPS * Time.deltaTime);

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        Destroy();
    }

    protected virtual float GetGravity(Transform target, float angle, float speed)
    {
        Vector3 displacement = transform.InverseTransformPoint(target.position);
        float   width        = displacement.z;
        float   height       = displacement.y;

        return 2F * Mathf.Pow(speed, 2f) * Mathf.Pow(Mathf.Cos(angle), 2f) * (width * Mathf.Tan(angle) - height)
            / Mathf.Pow(width, 2f);
    }

    protected virtual Vector3 GetNextAmount(float angle, float speed, float gravity, float time)
    {
        float   width       = speed * Mathf.Cos(angle) * time;
        float   height      = (speed * Mathf.Sin(angle) * time) - (0.5f * gravity * time * time);
        Vector3 localAmount = (Vector3.forward * width) + (Vector3.up * height);

        return transform.TransformDirection(localAmount);
    }

    // ******************************************************************************
    // 2-2-2) 메서드 -> 액션 -> 파괴(Destroy)
    // ******************************************************************************
    protected override IEnumerator _Destroy(bool useUnscaledTime)
    {
        resources.model.gameObject.SetActive(false);

        float duration = resources.effects[state].Play();

        yield return useUnscaledTime ? new WaitForSecondsRealtime(duration) : new WaitForSeconds(duration);

        Destroy(gameObject);
    }
}
