using System.Collections;
using System.Collections.Generic;
using ChamberLogic;
using UnityEngine;

public sealed class ChamberLogicGame : MonoBehaviour
{
    private const int StartingHealth = 3;

    [Header("Authored scene references")]
    [SerializeField] private Transform playerWeapon;
    [SerializeField] private Transform dealerEntity;
    [SerializeField] private Transform dealerRightHand;
    [SerializeField] private Transform dealerLeftHand;
    [SerializeField] private Transform dealerRightGrip;
    [SerializeField] private Transform dealerLeftGrip;
    [SerializeField] private Transform dealerSelfRightGrip;
    [SerializeField] private Transform dealerSelfLeftGrip;
    [SerializeField] private Transform weaponTableAnchor;
    [SerializeField] private Transform playerAimDealerAnchor;
    [SerializeField] private Transform playerAimSelfAnchor;
    [SerializeField] private Transform dealerAimPlayerAnchor;
    [SerializeField] private Transform dealerAimSelfAnchor;
    [SerializeField] private Transform weaponBreechAnchor;
    [SerializeField] private Transform weaponPump;
    [SerializeField] private Transform duelCamera;
    [SerializeField] private Light muzzleFlash;
    [SerializeField] private Light overheadLight;
    [SerializeField] private Light dealerRimLight;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioSource ambientSource;
    [SerializeField] private AudioSource mechanicalSource;
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource musicLayerSource;
    [SerializeField] private AudioClip horrorMusicClip;
    [SerializeField] private AudioClip horrorMusicLayerClip;
    [SerializeField] private TextMesh roundRevealText;
    [SerializeField] private List<GameObject> shellProps = new List<GameObject>();

    private ChamberRound round;
    private readonly List<string> report = new List<string>();
    private readonly List<Vector3> shellRestPositions = new List<Vector3>();
    private readonly List<Quaternion> shellRestRotations = new List<Quaternion>();
    private readonly System.Random seedSource = new System.Random();
    private AudioClip liveShotClip;
    private AudioClip blankClickClip;
    private AudioClip shellLoadClip;
    private int playerHealth;
    private int dealerHealth;
    private bool playerTurn;
    private bool resolving;
    private bool introPlaying;
    private Vector3 cameraRestPosition;
    private Quaternion cameraRestRotation;
    private Vector3 entityRestPosition;
    private Quaternion entityRestRotation;
    private Vector3 entityRestScale;
    private Transform rightHandRestParent;
    private Transform leftHandRestParent;
    private Vector3 rightHandRestPosition;
    private Vector3 leftHandRestPosition;
    private Quaternion rightHandRestRotation;
    private Quaternion leftHandRestRotation;
    private bool dealerActing;
    private Vector3 pumpRestPosition;
    private Quaternion pumpRestRotation;

    private void Awake()
    {
        if (duelCamera == null || playerWeapon == null || dealerEntity == null || dealerRightHand == null || dealerLeftHand == null ||
            dealerRightGrip == null || dealerLeftGrip == null || dealerSelfRightGrip == null || dealerSelfLeftGrip == null ||
            weaponTableAnchor == null || playerAimDealerAnchor == null || playerAimSelfAnchor == null ||
            dealerAimPlayerAnchor == null || dealerAimSelfAnchor == null || weaponBreechAnchor == null || weaponPump == null ||
            audioSource == null || ambientSource == null || mechanicalSource == null || musicSource == null || musicLayerSource == null ||
            horrorMusicClip == null || horrorMusicLayerClip == null)
        {
            Debug.LogError("Chamber scene references are incomplete. The saved Chamber scene needs repair.");
            enabled = false;
            return;
        }

        cameraRestPosition = duelCamera.localPosition;
        cameraRestRotation = duelCamera.localRotation;
        entityRestPosition = dealerEntity.localPosition;
        entityRestRotation = dealerEntity.localRotation;
        entityRestScale = dealerEntity.localScale;
        rightHandRestParent = dealerRightHand.parent;
        leftHandRestParent = dealerLeftHand.parent;
        rightHandRestPosition = dealerRightHand.localPosition;
        leftHandRestPosition = dealerLeftHand.localPosition;
        rightHandRestRotation = dealerRightHand.localRotation;
        leftHandRestRotation = dealerLeftHand.localRotation;
        pumpRestPosition = weaponPump.localPosition;
        pumpRestRotation = weaponPump.localRotation;
        foreach (var shell in shellProps)
        {
            shellRestPositions.Add(shell.transform.localPosition);
            shellRestRotations.Add(shell.transform.localRotation);
        }

        liveShotClip = CreateShotClip("Layered shotgun report", 1.05f, true);
        blankClickClip = CreateShotClip("Dry chamber click", 0.24f, false);
        shellLoadClip = CreateMechanicalClip();
        ambientSource.clip = CreateAmbientClip();
        ambientSource.loop = true;
        ambientSource.volume = 0.28f;
        ambientSource.Play();
        musicSource.clip = horrorMusicClip;
        musicSource.loop = true;
        musicSource.volume = 0.24f;
        musicSource.Play();
        musicLayerSource.clip = horrorMusicLayerClip;
        musicLayerSource.loop = true;
        musicLayerSource.volume = 0.14f;
        musicLayerSource.Play();
        StartExperiment();
    }

    private void Update()
    {
        var time = Time.unscaledTime;
        if (overheadLight != null)
        {
            var unstable = Mathf.PerlinNoise(time * 7.5f, 0.37f);
            var dropout = Mathf.PerlinNoise(time * 1.3f, 4.1f) > 0.91f ? 0.22f : 1f;
            overheadLight.intensity = (1.15f + unstable * 0.72f) * dropout;
        }
        if (dealerRimLight != null) dealerRimLight.intensity = 2.65f + Mathf.Sin(time * 0.72f) * 0.42f;

        if (!introPlaying)
        {
            duelCamera.localPosition = cameraRestPosition + new Vector3(Mathf.Sin(time * 0.37f) * 0.008f, Mathf.Sin(time * 0.83f) * 0.012f, 0f);
            duelCamera.localRotation = cameraRestRotation * Quaternion.Euler(Mathf.Sin(time * 0.44f) * 0.18f, Mathf.Sin(time * 0.29f) * 0.22f, 0f);
        }

        if (dealerEntity != null && !dealerActing)
        {
            dealerEntity.localPosition = entityRestPosition + Vector3.up * (Mathf.Sin(time * 0.72f) * 0.012f);
            dealerEntity.localRotation = entityRestRotation * Quaternion.Euler(0f, Mathf.Sin(time * 0.48f) * 1.8f, 0f);
        }

        if (!introPlaying && !resolving && playerTurn && round != null)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1)) ChooseDealer();
            if (Input.GetKeyDown(KeyCode.Alpha2)) ChooseSelf();
        }
        if (Input.GetKeyDown(KeyCode.R)) StartExperiment();
    }

    private void StartExperiment()
    {
        StopAllCoroutines();
        round = new ChamberRound(2, 4, seedSource.Next());
        playerHealth = StartingHealth;
        dealerHealth = StartingHealth;
        playerTurn = false;
        resolving = true;
        introPlaying = true;
        report.Clear();
        report.Add("Round begins: 2 live and 4 blank shells.");
        dealerActing = false;
        ResetDealerEntity();
        ReleaseDealerHands();
        SetWeaponAt(weaponTableAnchor);
        weaponPump.localPosition = pumpRestPosition;
        weaponPump.localRotation = pumpRestRotation;
        ResetShellReveal();
        StartCoroutine(OpeningSequence());
    }

    private IEnumerator OpeningSequence()
    {
        playerWeapon.gameObject.SetActive(true);
        if (roundRevealText != null)
        {
            roundRevealText.gameObject.SetActive(true);
            roundRevealText.text = "THE HOUSE LOADS SIX SHELLS";
            roundRevealText.color = new Color(0.78f, 0.82f, 0.78f, 1f);
        }

        yield return new WaitForSeconds(1.35f);

        var closePosition = new Vector3(-0.48f, 1.36f, -0.92f);
        var closeRotation = Quaternion.LookRotation(new Vector3(-0.58f, 0.86f, 0.02f) - closePosition);
        yield return MoveCamera(cameraRestPosition, cameraRestRotation, closePosition, closeRotation, 1f);

        if (roundRevealText != null)
        {
            roundRevealText.text = "2 LIVE   •   4 BLANK\nORDER UNKNOWN";
            roundRevealText.color = new Color(0.95f, 0.67f, 0.42f, 1f);
        }
        Debug.Log("[Chamber] Loading reveal: 2 live shells and 4 blank shells. Their order is hidden.");
        yield return new WaitForSeconds(1.8f);
        yield return OpenWeaponAction(0.62f);
        yield return new WaitForSeconds(0.24f);

        for (var i = 0; i < shellProps.Count; i++)
        {
            var shell = shellProps[i];
            if (shell == null) continue;
            var start = shell.transform.position;
            var startRotation = shell.transform.rotation;
            mechanicalSource.PlayOneShot(shellLoadClip, 0.52f);
            var approach = weaponBreechAnchor.position + Vector3.down * 0.115f;
            for (var t = 0f; t < 0.32f; t += Time.deltaTime)
            {
                var progress = Mathf.SmoothStep(0f, 1f, t / 0.32f);
                shell.transform.position = Vector3.Lerp(start, approach, progress) + Vector3.up * Mathf.Sin(progress * Mathf.PI) * 0.10f;
                shell.transform.rotation = Quaternion.Slerp(startRotation, weaponBreechAnchor.rotation, progress);
                yield return null;
            }
            for (var t = 0f; t < 0.18f; t += Time.deltaTime)
            {
                var progress = Mathf.SmoothStep(0f, 1f, t / 0.18f);
                shell.transform.position = Vector3.Lerp(approach, weaponBreechAnchor.position, progress);
                shell.transform.rotation = weaponBreechAnchor.rotation;
                yield return null;
            }
            shell.SetActive(false);
            yield return new WaitForSeconds(0.22f);
        }

        yield return CloseWeaponAction(0.58f);

        if (roundRevealText != null) roundRevealText.text = "REMEMBER THE MIX\nP(LIVE NEXT) = 2 / 6";
        yield return new WaitForSeconds(2.2f);
        yield return MoveCamera(closePosition, closeRotation, cameraRestPosition, cameraRestRotation, 0.9f);
        CompleteOpening();
    }

    private void CompleteOpening()
    {
        if (roundRevealText != null) roundRevealText.gameObject.SetActive(false);
        SetWeaponAt(weaponTableAnchor);
        duelCamera.localPosition = cameraRestPosition;
        duelCamera.localRotation = cameraRestRotation;
        introPlaying = false;
        resolving = false;
        playerTurn = true;
        Debug.Log("[Chamber] Your turn — press 1 for the house, 2 for yourself, or R to restart.");
    }

    private IEnumerator MoveCamera(Vector3 fromPosition, Quaternion fromRotation, Vector3 toPosition, Quaternion toRotation, float duration)
    {
        for (var t = 0f; t < duration; t += Time.deltaTime)
        {
            var progress = Mathf.SmoothStep(0f, 1f, t / duration);
            duelCamera.localPosition = Vector3.Lerp(fromPosition, toPosition, progress);
            duelCamera.localRotation = Quaternion.Slerp(fromRotation, toRotation, progress);
            yield return null;
        }
        duelCamera.localPosition = toPosition;
        duelCamera.localRotation = toRotation;
    }

    private void ResetShellReveal()
    {
        for (var i = 0; i < shellProps.Count; i++)
        {
            if (shellProps[i] == null) continue;
            shellProps[i].SetActive(true);
            if (i < shellRestPositions.Count) shellProps[i].transform.localPosition = shellRestPositions[i];
            if (i < shellRestRotations.Count) shellProps[i].transform.localRotation = shellRestRotations[i];
        }
    }

    private void ChooseDealer() => StartCoroutine(ResolveShot(true, false));
    private void ChooseSelf() => StartCoroutine(ResolveShot(true, true));

    private IEnumerator ResolveShot(bool isPlayer, bool targetsSelf)
    {
        if (introPlaying || resolving || round == null || round.RemainingTotal == 0) yield break;
        resolving = true;
        var liveBefore = round.RemainingLive;
        var totalBefore = round.RemainingTotal;
        var shell = round.Fire();
        var actor = isPlayer ? "You" : "Dealer";
        var target = targetsSelf ? actor : (isPlayer ? "Dealer" : "you");
        var outcome = shell == Shell.Live ? "LIVE" : "BLANK";
        report.Add($"{actor} aimed at {target}: {outcome} [before: {liveBefore}/{totalBefore} live]");

        yield return AnimateShot(isPlayer, targetsSelf, shell == Shell.Live);
        if (shell == Shell.Live)
        {
            if (targetsSelf)
            {
                if (isPlayer) playerHealth--; else dealerHealth--;
            }
            else
            {
                if (isPlayer) dealerHealth--; else playerHealth--;
            }
            if (dealerHealth <= 0)
            {
                dealerActing = true;
                ReleaseDealerHands();
                ApplyDealerDefeatedPose();
            }
        }
        yield return new WaitForSeconds(0.9f);
        if (EndIfNeeded()) yield break;

        var keepTurn = shell == Shell.Blank && targetsSelf;
        if (keepTurn)
        {
            playerTurn = isPlayer;
            resolving = false;
            if (!isPlayer) StartCoroutine(DealerMove());
            yield break;
        }

        playerTurn = !isPlayer;
        resolving = false;
        if (isPlayer) StartCoroutine(DealerMove());
    }

    private IEnumerator DealerMove()
    {
        resolving = true;
        yield return new WaitForSeconds(0.65f);
        var targetsSelf = round.LiveChance < 0.5f;
        yield return new WaitForSeconds(0.25f);
        resolving = false;
        yield return ResolveShot(false, targetsSelf);
    }

    private bool EndIfNeeded()
    {
        if (playerHealth > 0 && dealerHealth > 0 && round.RemainingTotal > 0) return false;
        report.Insert(0, "LEVEL 1: Conditional probability review");
        report.Insert(1, "P(live next | revealed shells) = live remaining / total remaining.");
        report.Insert(2, "Each revealed shell is evidence: remove it, then recalculate.");
        playerTurn = false;
        resolving = false;
        Debug.Log("[Chamber] " + string.Join("\n", report));
        return true;
    }

    private IEnumerator AnimateShot(bool isPlayer, bool targetsSelf, bool live)
    {
        var aimAnchor = isPlayer
            ? (targetsSelf ? playerAimSelfAnchor : playerAimDealerAnchor)
            : (targetsSelf ? dealerAimSelfAnchor : dealerAimPlayerAnchor);

        if (isPlayer)
        {
            dealerActing = true;
            ResetDealerEntity();
            yield return new WaitForSeconds(0.22f);
        }
        else
        {
            dealerActing = true;
            ResetDealerEntity();
            yield return MoveDealerHandsToWeapon(targetsSelf, 0.52f);
        }

        mechanicalSource.PlayOneShot(shellLoadClip, 0.46f);
        yield return MoveWeapon(aimAnchor, isPlayer ? 0.62f : 0.82f);
        yield return PumpWeapon(isPlayer ? 0.34f : 0.42f);
        if (!isPlayer)
        {
            yield return new WaitForSeconds(0.38f);
        }
        else
        {
            yield return new WaitForSeconds(0.22f);
        }

        audioSource.PlayOneShot(live ? liveShotClip : blankClickClip, live ? 0.9f : 0.68f);
        if (live && muzzleFlash != null) muzzleFlash.intensity = 9f;
        var aimPosition = playerWeapon.position;
        playerWeapon.position += aimAnchor.forward * (live ? 0.16f : 0.035f);
        if (live && ((!isPlayer && !targetsSelf) || (isPlayer && targetsSelf)))
            yield return CameraImpact();
        else
            yield return new WaitForSeconds(live ? 0.16f : 0.09f);
        if (muzzleFlash != null) muzzleFlash.intensity = 0f;

        playerWeapon.position = aimPosition;
        var dealerWasHit = live && ((isPlayer && !targetsSelf) || (!isPlayer && targetsSelf));
        if (dealerWasHit)
        {
            dealerActing = true;
            yield return DealerHitReaction();
        }

        yield return MoveWeapon(weaponTableAnchor, 0.72f);
        if (!isPlayer) yield return ReturnDealerHands(0.42f);
        ResetDealerEntity();
        dealerActing = false;
    }

    private IEnumerator MoveWeapon(Transform destination, float duration)
    {
        var startPosition = playerWeapon.position;
        var startRotation = playerWeapon.rotation;
        for (var t = 0f; t < duration; t += Time.deltaTime)
        {
            var progress = Mathf.SmoothStep(0f, 1f, t / duration);
            var lift = Mathf.Sin(progress * Mathf.PI) * 0.16f;
            playerWeapon.position = Vector3.Lerp(startPosition, destination.position, progress) + Vector3.up * lift;
            playerWeapon.rotation = Quaternion.Slerp(startRotation, destination.rotation, progress);
            yield return null;
        }
        SetWeaponAt(destination);
    }

    private IEnumerator PumpWeapon(float duration)
    {
        var backPosition = pumpRestPosition + Vector3.down * 0.022f;
        var backDuration = duration * 0.46f;
        mechanicalSource.PlayOneShot(shellLoadClip, 0.62f);
        for (var t = 0f; t < backDuration; t += Time.deltaTime)
        {
            var progress = Mathf.SmoothStep(0f, 1f, t / backDuration);
            weaponPump.localPosition = Vector3.Lerp(pumpRestPosition, backPosition, progress);
            weaponPump.localRotation = pumpRestRotation * Quaternion.Euler(progress * 2.5f, 0f, 0f);
            yield return null;
        }
        for (var t = 0f; t < duration - backDuration; t += Time.deltaTime)
        {
            var progress = Mathf.SmoothStep(0f, 1f, t / (duration - backDuration));
            weaponPump.localPosition = Vector3.Lerp(backPosition, pumpRestPosition, progress);
            weaponPump.localRotation = pumpRestRotation * Quaternion.Euler((1f - progress) * 2.5f, 0f, 0f);
            yield return null;
        }
        weaponPump.localPosition = pumpRestPosition;
        weaponPump.localRotation = pumpRestRotation;
    }

    private IEnumerator OpenWeaponAction(float duration)
    {
        mechanicalSource.PlayOneShot(shellLoadClip, 0.76f);
        yield return MoveWeaponAction(pumpRestPosition, pumpRestRotation,
            pumpRestPosition + Vector3.down * 0.022f,
            pumpRestRotation * Quaternion.Euler(6f, 0f, 0f), duration);
    }

    private IEnumerator CloseWeaponAction(float duration)
    {
        mechanicalSource.PlayOneShot(shellLoadClip, 0.82f);
        yield return MoveWeaponAction(weaponPump.localPosition, weaponPump.localRotation,
            pumpRestPosition, pumpRestRotation, duration);
    }

    private IEnumerator MoveWeaponAction(Vector3 startPosition, Quaternion startRotation, Vector3 endPosition, Quaternion endRotation, float duration)
    {
        for (var t = 0f; t < duration; t += Time.deltaTime)
        {
            var progress = Mathf.SmoothStep(0f, 1f, t / duration);
            weaponPump.localPosition = Vector3.Lerp(startPosition, endPosition, progress);
            weaponPump.localRotation = Quaternion.Slerp(startRotation, endRotation, progress);
            yield return null;
        }
        weaponPump.localPosition = endPosition;
        weaponPump.localRotation = endRotation;
    }

    private IEnumerator DealerHitReaction()
    {
        var hitPosition = entityRestPosition + new Vector3(0f, 0.07f, 0.18f);
        var hitRotation = entityRestRotation * Quaternion.Euler(-18f, 0f, 0f);
        var hitScale = Vector3.Scale(entityRestScale, new Vector3(1.13f, 0.82f, 1.13f));
        for (var t = 0f; t < 0.16f; t += Time.deltaTime)
        {
            var progress = Mathf.SmoothStep(0f, 1f, t / 0.16f);
            dealerEntity.localPosition = Vector3.Lerp(entityRestPosition, hitPosition, progress);
            dealerEntity.localRotation = Quaternion.Slerp(entityRestRotation, hitRotation, progress);
            dealerEntity.localScale = Vector3.Lerp(entityRestScale, hitScale, progress);
            yield return null;
        }
        for (var t = 0f; t < 0.48f; t += Time.deltaTime)
        {
            var progress = Mathf.SmoothStep(0f, 1f, t / 0.48f);
            dealerEntity.localPosition = Vector3.Lerp(hitPosition, entityRestPosition, progress);
            dealerEntity.localRotation = Quaternion.Slerp(hitRotation, entityRestRotation, progress);
            dealerEntity.localScale = Vector3.Lerp(hitScale, entityRestScale, progress);
            yield return null;
        }
        ResetDealerEntity();
    }

    private void ApplyDealerDefeatedPose()
    {
        dealerEntity.localPosition = entityRestPosition + new Vector3(0f, -0.12f, 0.14f);
        dealerEntity.localRotation = entityRestRotation * Quaternion.Euler(-22f, 0f, 0f);
        dealerEntity.localScale = Vector3.Scale(entityRestScale, new Vector3(1.18f, 0.65f, 1.18f));
    }

    private IEnumerator MoveDealerHandsToWeapon(bool targetsSelf, float duration)
    {
        var rightTarget = targetsSelf ? dealerSelfRightGrip : dealerRightGrip;
        var leftTarget = targetsSelf ? dealerSelfLeftGrip : dealerLeftGrip;
        var rightStartPosition = dealerRightHand.position;
        var leftStartPosition = dealerLeftHand.position;
        var rightStartRotation = dealerRightHand.rotation;
        var leftStartRotation = dealerLeftHand.rotation;
        dealerRightHand.SetParent(null, true);
        dealerLeftHand.SetParent(null, true);

        for (var t = 0f; t < duration; t += Time.deltaTime)
        {
            var progress = Mathf.SmoothStep(0f, 1f, t / duration);
            var lift = Mathf.Sin(progress * Mathf.PI) * 0.07f;
            dealerRightHand.position = Vector3.Lerp(rightStartPosition, rightTarget.position, progress) + Vector3.up * lift;
            dealerLeftHand.position = Vector3.Lerp(leftStartPosition, leftTarget.position, progress) + Vector3.up * lift;
            dealerRightHand.rotation = Quaternion.Slerp(rightStartRotation, rightTarget.rotation, progress);
            dealerLeftHand.rotation = Quaternion.Slerp(leftStartRotation, leftTarget.rotation, progress);
            yield return null;
        }

        AttachHand(dealerRightHand, rightTarget);
        AttachHand(dealerLeftHand, leftTarget);
    }

    private IEnumerator ReturnDealerHands(float duration)
    {
        var rightStartPosition = dealerRightHand.position;
        var leftStartPosition = dealerLeftHand.position;
        var rightStartRotation = dealerRightHand.rotation;
        var leftStartRotation = dealerLeftHand.rotation;
        dealerRightHand.SetParent(null, true);
        dealerLeftHand.SetParent(null, true);
        var rightTargetPosition = rightHandRestParent.TransformPoint(rightHandRestPosition);
        var leftTargetPosition = leftHandRestParent.TransformPoint(leftHandRestPosition);
        var rightTargetRotation = rightHandRestParent.rotation * rightHandRestRotation;
        var leftTargetRotation = leftHandRestParent.rotation * leftHandRestRotation;

        for (var t = 0f; t < duration; t += Time.deltaTime)
        {
            var progress = Mathf.SmoothStep(0f, 1f, t / duration);
            dealerRightHand.position = Vector3.Lerp(rightStartPosition, rightTargetPosition, progress);
            dealerLeftHand.position = Vector3.Lerp(leftStartPosition, leftTargetPosition, progress);
            dealerRightHand.rotation = Quaternion.Slerp(rightStartRotation, rightTargetRotation, progress);
            dealerLeftHand.rotation = Quaternion.Slerp(leftStartRotation, leftTargetRotation, progress);
            yield return null;
        }
        ReleaseDealerHands();
    }

    private static void AttachHand(Transform hand, Transform grip)
    {
        hand.SetParent(grip, false);
        hand.localPosition = Vector3.zero;
        hand.localRotation = Quaternion.identity;
    }

    private void ReleaseDealerHands()
    {
        dealerRightHand.SetParent(rightHandRestParent, false);
        dealerRightHand.localPosition = rightHandRestPosition;
        dealerRightHand.localRotation = rightHandRestRotation;
        dealerLeftHand.SetParent(leftHandRestParent, false);
        dealerLeftHand.localPosition = leftHandRestPosition;
        dealerLeftHand.localRotation = leftHandRestRotation;
    }

    private void ResetDealerEntity()
    {
        dealerEntity.localPosition = entityRestPosition;
        dealerEntity.localRotation = entityRestRotation;
        dealerEntity.localScale = entityRestScale;
    }

    private IEnumerator CameraImpact()
    {
        var startPosition = duelCamera.localPosition;
        var startRotation = duelCamera.localRotation;
        for (var t = 0f; t < 0.34f; t += Time.deltaTime)
        {
            var decay = 1f - t / 0.34f;
            duelCamera.localPosition = startPosition + new Vector3(Mathf.Sin(t * 93f), Mathf.Cos(t * 77f), -0.06f) * (0.035f * decay);
            duelCamera.localRotation = startRotation * Quaternion.Euler(Mathf.Sin(t * 81f) * 4.5f * decay, Mathf.Cos(t * 69f) * 3f * decay, 0f);
            yield return null;
        }
        duelCamera.localPosition = startPosition;
        duelCamera.localRotation = startRotation;
    }

    private void SetWeaponAt(Transform anchor)
    {
        playerWeapon.SetParent(anchor, true);
        playerWeapon.localPosition = Vector3.zero;
        playerWeapon.localRotation = Quaternion.identity;
    }

    private static AudioClip CreateShotClip(string name, float duration, bool live)
    {
        const int sampleRate = 44100;
        var frames = Mathf.CeilToInt(duration * sampleRate);
        var samples = new float[frames * 2];
        var random = new System.Random(live ? 31415 : 92653);
        for (var i = 0; i < frames; i++)
        {
            var t = (float)i / sampleRate;
            var noiseL = (float)(random.NextDouble() * 2.0 - 1.0);
            var noiseR = (float)(random.NextDouble() * 2.0 - 1.0);
            float left;
            float right;
            if (live)
            {
                var crack = Mathf.Exp(-t * 72f);
                var boom = Mathf.Sin(t * Mathf.PI * 2f * (74f - t * 18f)) * Mathf.Exp(-t * 6.2f);
                var blast = Mathf.Exp(-t * 13f);
                var reflection = t > 0.075f ? Mathf.Exp(-(t - 0.075f) * 8f) * 0.18f : 0f;
                left = noiseL * crack * 0.72f + boom * 0.56f + noiseL * blast * 0.28f + noiseR * reflection;
                right = noiseR * crack * 0.69f + boom * 0.58f + noiseR * blast * 0.27f + noiseL * reflection * 0.92f;
            }
            else
            {
                var snap = Mathf.Exp(-t * 48f);
                var metal = Mathf.Sin(t * Mathf.PI * 2f * 1220f) * snap * 0.28f;
                var clack = t > 0.035f ? Mathf.Sin((t - 0.035f) * Mathf.PI * 2f * 410f) * Mathf.Exp(-(t - 0.035f) * 35f) * 0.18f : 0f;
                left = metal + clack + noiseL * snap * 0.08f;
                right = metal * 0.92f + clack * 1.08f + noiseR * snap * 0.08f;
            }
            samples[i * 2] = Mathf.Clamp(left, -1f, 1f);
            samples[i * 2 + 1] = Mathf.Clamp(right, -1f, 1f);
        }
        var clip = AudioClip.Create(name, frames, 2, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private static AudioClip CreateMechanicalClip()
    {
        const int sampleRate = 44100;
        var frames = Mathf.CeilToInt(sampleRate * 0.16f);
        var samples = new float[frames];
        var random = new System.Random(4407);
        for (var i = 0; i < frames; i++)
        {
            var t = (float)i / sampleRate;
            var strike = Mathf.Exp(-t * 54f) * Mathf.Sin(t * Mathf.PI * 2f * 780f) * 0.34f;
            var slide = t > 0.025f ? Mathf.Exp(-(t - 0.025f) * 30f) * (float)(random.NextDouble() * 2.0 - 1.0) * 0.12f : 0f;
            samples[i] = strike + slide;
        }
        var clip = AudioClip.Create("Shell chamber mechanism", frames, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private static AudioClip CreateAmbientClip()
    {
        const int sampleRate = 22050;
        var samples = new float[sampleRate * 12];
        var random = new System.Random(7182);
        var smoothedNoise = 0f;
        for (var i = 0; i < samples.Length; i++)
        {
            var t = (float)i / sampleRate;
            smoothedNoise = Mathf.Lerp(smoothedNoise, (float)(random.NextDouble() * 2.0 - 1.0), 0.004f);
            var drone = Mathf.Sin(t * 2f * Mathf.PI * 43.65f) * 0.14f + Mathf.Sin(t * 2f * Mathf.PI * 55f) * 0.07f;
            var pulsePhase = t % 3.2f;
            var pulse = pulsePhase < 0.23f ? Mathf.Sin(t * 2f * Mathf.PI * 38f) * Mathf.Exp(-pulsePhase * 15f) * 0.19f : 0f;
            samples[i] = (drone + pulse + smoothedNoise * 0.035f) * 0.5f;
        }
        var clip = AudioClip.Create("The chamber breathes", samples.Length, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

}
