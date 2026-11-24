using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Serialization;

public class Guest : MonoBehaviour
{
    [Header("----- Move -----")]
    public float float_MoveSpeed = 4f;
    public float float_ArriveThreshold = 0.05f;

    [Header("----- Refs -----")]
    public Animator animator;
    public SpriteRenderer spriteRenderer_GuestDisplay;
    public SpriteRenderer spriteRenderer_CoinSpecial;
    
    [Header("----- Runtime -----")]
    public bool isHasSpecialCoin = false;

    [HideInInspector] public ObjectColor objectColor;

    private GuestPool guestPool;
    private Transform trans_TargetPoint;
    private bool _isMoving;
    private bool _isBoarding;

    public System.Action<Guest, CarControl> OnFinishedBoard;

    public void Initialize(GuestPool pool, ObjectColor setColor, Color tint)
    {
        guestPool = pool;
        objectColor = setColor;
        if (spriteRenderer_GuestDisplay && tint != default) spriteRenderer_GuestDisplay.color = tint;
    }

    public void GoToPoint(Transform t)
    {
        trans_TargetPoint = t;
        _isMoving = true;
        _isBoarding = false;
        if (t.rotation.eulerAngles.z.Equals(90f))
        {
            if (animator)
            {
                animator.SetBool(Params.IS_LEFT, true);
            }
        }
        else
        {
            if (animator)
            {
                animator.SetBool(Params.IS_LEFT, false);
            }
        }
        animator.SetBool(Params.IS_RUN, true);
    }
    
    public void TeleportToPoint(Transform t)
    {
        transform.position = t.position;
        _isBoarding = false;
        if (t.rotation.eulerAngles.z.Equals(90f))
        {
            if (animator)
            {
                animator.SetBool(Params.IS_LEFT, true);
            }
        }
        else
        {
            if (animator)
            {
                animator.SetBool(Params.IS_LEFT, false);
            }
        }
        animator.SetBool(Params.IS_RUN, false);
    }

    private void Update()
    {
        if (_isMoving)
        {
            Vector3 dir = trans_TargetPoint.position - transform.position;
            float dist = dir.magnitude;
            if (dist <= float_ArriveThreshold)
            {
                transform.position = trans_TargetPoint.position;
                _isMoving = false;
                if (animator) animator.SetBool(Params.IS_RUN, false);
                return;
            }
            dir.Normalize();
            transform.position += dir * float_MoveSpeed * Time.deltaTime;
            //if (sr) sr.flipX = (dir.x < 0);
        }
    }

    public void BoardTo(CarControl car, float vanishDelay = 0.25f)
    {
        if (_isBoarding) return;
        _isBoarding = true;
        if (animator) animator.SetBool(Params.IS_RUN, true);
        BoardRoutine(car, vanishDelay);
    }

    private void BoardRoutine(CarControl car, float vanishDelay)
    {
        // while (Vector2.Distance(transform.position, car.trans_BusDoor.position) > arriveThreshold)
        // {
        //     Vector3 dir = (car.trans_BusDoor.position - transform.position).normalized;
        //     transform.position += dir * moveSpeed * Time.deltaTime;
        //     if (sr) sr.flipX = (dir.x < 0);
        //     yield return null;
        // }
        // Hủy tween cũ nếu đang có
        DOTween.Kill(transform);

        // Di chuyển mượt
        transform.DOMove(car.trans_BusDoor.position, vanishDelay)
            .SetEase(Ease.InOutSine)
            .OnComplete(() =>
            {
                transform.position = car.trans_BusDoor.position;
                if (animator) animator.SetBool(Params.IS_RUN, false);
                guestPool?.Release(this);
                OnFinishedBoard?.Invoke(this, car);
            });
    }
}
