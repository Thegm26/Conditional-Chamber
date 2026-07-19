using UnityEngine;

[RequireComponent(typeof(Animator))]
public sealed class DealerWeaponIK : MonoBehaviour
{
    [SerializeField] private Animator dealerAnimator;
    [SerializeField] private Transform rearGrip;
    [SerializeField] private Transform foreGrip;
    [SerializeField] private Transform selfRightGrip;
    [SerializeField] private Transform selfLeftGrip;
    [SerializeField] private Vector3 rightHandRotationOffset = new Vector3(8f, -82f, 96f);
    [SerializeField] private Vector3 leftHandRotationOffset = new Vector3(-8f, 84f, -92f);

    private bool holding;
    private bool selfAim;
    private float weight;

    public float Weight => weight;

    public void SetHolding(bool value, bool aimsAtSelf = false)
    {
        holding = value;
        selfAim = aimsAtSelf;
    }

    private void Awake()
    {
        if (dealerAnimator == null) dealerAnimator = GetComponent<Animator>();
    }

    private void OnAnimatorIK(int layerIndex)
    {
        if (dealerAnimator == null || rearGrip == null || foreGrip == null || selfRightGrip == null || selfLeftGrip == null) return;
        weight = Mathf.MoveTowards(weight, holding ? 1f : 0f, Time.deltaTime * (holding ? 3.8f : 5f));

        SetHand(AvatarIKGoal.RightHand, selfAim ? selfRightGrip : rearGrip, Quaternion.Euler(rightHandRotationOffset));
        SetHand(AvatarIKGoal.LeftHand, selfAim ? selfLeftGrip : foreGrip, Quaternion.Euler(leftHandRotationOffset));
    }

    private void SetHand(AvatarIKGoal goal, Transform grip, Quaternion rotationOffset)
    {
        dealerAnimator.SetIKPositionWeight(goal, weight);
        dealerAnimator.SetIKRotationWeight(goal, weight * 0.82f);
        dealerAnimator.SetIKPosition(goal, grip.position);
        dealerAnimator.SetIKRotation(goal, grip.rotation * rotationOffset);
    }
}
