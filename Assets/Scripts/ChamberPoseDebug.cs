using UnityEngine;

public sealed class ChamberPoseDebug : MonoBehaviour
{
    [Header("Shotgun geometry")]
    [SerializeField] private Transform weapon;
    [SerializeField] private Transform muzzle;
    [SerializeField] private Transform rearGrip;
    [SerializeField] private Transform foreGrip;

    [Header("Authored poses")]
    [SerializeField] private Transform tablePose;
    [SerializeField] private Transform playerToDealerPose;
    [SerializeField] private Transform playerToSelfPose;
    [SerializeField] private Transform dealerToPlayerPose;
    [SerializeField] private Transform dealerToSelfPose;

    [Header("Targets and hands")]
    [SerializeField] private Transform dealerFace;
    [SerializeField] private Transform playerFace;
    [SerializeField] private Transform playerLeftHand;
    [SerializeField] private Transform playerRightHand;

    private void OnDrawGizmos()
    {
        if (!Ready()) return;

        DrawPose(tablePose, null, new Color(0.25f, 0.65f, 1f));
        DrawPose(playerToDealerPose, dealerFace, Color.green);
        DrawPose(playerToSelfPose, playerFace, new Color(1f, 0.55f, 0.1f));
        DrawPose(dealerToPlayerPose, playerFace, Color.red);
        DrawPose(dealerToSelfPose, dealerFace, Color.magenta);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(playerLeftHand.position, 0.035f);
        Gizmos.DrawWireSphere(playerRightHand.position, 0.035f);
        Gizmos.DrawLine(playerLeftHand.position, playerRightHand.position);
    }

    [ContextMenu("Log pose audit")]
    public void LogPoseAudit()
    {
        if (!Ready())
        {
            Debug.LogError("[PoseAudit] Missing debug references.", this);
            return;
        }

        LogPose("TABLE", tablePose, null);
        LogPose("PLAYER -> DEALER", playerToDealerPose, dealerFace);
        LogPose("PLAYER -> SELF", playerToSelfPose, playerFace);
        LogPose("DEALER -> PLAYER", dealerToPlayerPose, playerFace);
        LogPose("DEALER -> SELF", dealerToSelfPose, dealerFace);
        Debug.Log($"[PoseAudit] hands left={Format(playerLeftHand.position)} right={Format(playerRightHand.position)} separation={Vector3.Distance(playerLeftHand.position, playerRightHand.position):F3}m", this);
    }

    private bool Ready()
    {
        return weapon != null && muzzle != null && rearGrip != null && foreGrip != null && tablePose != null &&
               playerToDealerPose != null && playerToSelfPose != null &&
               dealerToPlayerPose != null && dealerToSelfPose != null &&
               dealerFace != null && playerFace != null &&
               playerLeftHand != null && playerRightHand != null;
    }

    private void DrawPose(Transform pose, Transform target, Color color)
    {
        GetMuzzlePose(pose, out var muzzlePosition, out var barrelDirection);
        var rearGripPosition = PointAtPose(pose, rearGrip);
        var foreGripPosition = PointAtPose(pose, foreGrip);
        Gizmos.color = color;
        Gizmos.DrawWireSphere(pose.position, 0.045f);
        Gizmos.DrawWireSphere(muzzlePosition, 0.035f);
        Gizmos.DrawWireCube(rearGripPosition, Vector3.one * 0.055f);
        Gizmos.DrawWireCube(foreGripPosition, Vector3.one * 0.055f);
        Gizmos.DrawLine(rearGripPosition, foreGripPosition);
        Gizmos.DrawLine(muzzlePosition, muzzlePosition + barrelDirection * 2f);
        if (target != null)
        {
            Gizmos.DrawWireSphere(target.position, 0.06f);
            Gizmos.DrawLine(muzzlePosition, target.position);
        }
    }

    private void LogPose(string label, Transform pose, Transform target)
    {
        GetMuzzlePose(pose, out var muzzlePosition, out var barrelDirection);
        if (target == null)
        {
            Debug.Log($"[PoseAudit] {label} grip={Format(pose.position)} muzzle={Format(muzzlePosition)} barrel={Format(barrelDirection)}", this);
            return;
        }

        var targetDirection = (target.position - muzzlePosition).normalized;
        var angleError = Vector3.Angle(barrelDirection, targetDirection);
        Debug.Log($"[PoseAudit] {label} root={Format(pose.position)} rearGrip={Format(PointAtPose(pose, rearGrip))} foreGrip={Format(PointAtPose(pose, foreGrip))} muzzle={Format(muzzlePosition)} target={Format(target.position)} distance={Vector3.Distance(muzzlePosition, target.position):F3}m aimError={angleError:F2}deg", this);
    }

    private void GetMuzzlePose(Transform pose, out Vector3 position, out Vector3 direction)
    {
        var muzzleLocalPosition = Vector3.Scale(weapon.InverseTransformPoint(muzzle.position), weapon.localScale);
        var barrelLocalDirection = weapon.InverseTransformDirection(muzzle.forward);
        position = pose.TransformPoint(muzzleLocalPosition);
        direction = pose.TransformDirection(barrelLocalDirection).normalized;
    }

    private Vector3 PointAtPose(Transform pose, Transform point)
    {
        var modelPoint = Vector3.Scale(weapon.InverseTransformPoint(point.position), weapon.localScale);
        return pose.TransformPoint(modelPoint);
    }

    private static string Format(Vector3 value) => $"({value.x:F3}, {value.y:F3}, {value.z:F3})";
}
