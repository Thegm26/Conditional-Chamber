using System.Collections;
using System.Collections.Generic;
using ChamberLogic;
using UnityEngine;

public sealed class ChamberLogicGame : MonoBehaviour
{
    private const int StartingHealth = 3;

    [Header("Authored scene references")]
    [SerializeField] private Transform playerWeapon;
    [SerializeField] private Transform dealerCharacter;
    [SerializeField] private Animator dealerAnimator;
    [SerializeField] private Transform weaponTableAnchor;
    [SerializeField] private Transform playerAimDealerAnchor;
    [SerializeField] private Transform playerAimSelfAnchor;
    [SerializeField] private Transform dealerAimPlayerAnchor;
    [SerializeField] private Transform dealerAimSelfAnchor;
    [SerializeField] private Transform weaponBreechAnchor;
    [SerializeField] private Transform duelCamera;
    [SerializeField] private Light muzzleFlash;
    [SerializeField] private Light overheadLight;
    [SerializeField] private Light dealerRimLight;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioSource ambientSource;
    [SerializeField] private AudioSource mechanicalSource;
    [SerializeField] private AudioSource musicSource;
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
    private Vector3 dealerRestPosition;
    private Quaternion dealerRestRotation;
    private bool dealerActing;

    private void Awake()
    {
        if (duelCamera == null || playerWeapon == null || dealerCharacter == null || dealerAnimator == null ||
            weaponTableAnchor == null || playerAimDealerAnchor == null || playerAimSelfAnchor == null ||
            dealerAimPlayerAnchor == null || dealerAimSelfAnchor == null || weaponBreechAnchor == null ||
            audioSource == null || ambientSource == null || mechanicalSource == null || musicSource == null)
        {
            Debug.LogError("Chamber scene references are incomplete. The saved Chamber scene needs repair.");
            enabled = false;
            return;
        }

        cameraRestPosition = duelCamera.localPosition;
        cameraRestRotation = duelCamera.localRotation;
        dealerRestPosition = dealerCharacter.localPosition;
        dealerRestRotation = dealerCharacter.localRotation;
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
        musicSource.clip = CreateHorrorMusicClip();
        musicSource.loop = true;
        musicSource.volume = 0.16f;
        musicSource.Play();
        dealerAnimator.speed = 0.76f;
        dealerAnimator.Play("Idle", 0, 0.18f);
        StartExperiment();
    }

    private void Update()
    {
        var time = Time.unscaledTime;
        if (overheadLight != null)
        {
            var unstable = Mathf.PerlinNoise(time * 7.5f, 0.37f);
            var dropout = Mathf.PerlinNoise(time * 1.3f, 4.1f) > 0.91f ? 0.22f : 1f;
            overheadLight.intensity = (4.6f + unstable * 1.5f) * dropout;
        }
        if (dealerRimLight != null) dealerRimLight.intensity = 2.15f + Mathf.Sin(time * 0.72f) * 0.32f;

        if (!introPlaying)
        {
            duelCamera.localPosition = cameraRestPosition + new Vector3(Mathf.Sin(time * 0.37f) * 0.008f, Mathf.Sin(time * 0.83f) * 0.012f, 0f);
            duelCamera.localRotation = cameraRestRotation * Quaternion.Euler(Mathf.Sin(time * 0.44f) * 0.18f, Mathf.Sin(time * 0.29f) * 0.22f, 0f);
        }

        if (dealerCharacter != null)
        {
            dealerCharacter.localPosition = dealerRestPosition;
            dealerCharacter.localRotation = dealerActing
                ? dealerRestRotation
                : dealerRestRotation * Quaternion.Euler(0f, Mathf.Sin(time * 0.48f) * 1.1f, 0f);
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
        dealerCharacter.localPosition = dealerRestPosition;
        dealerCharacter.localRotation = dealerRestRotation;
        dealerAnimator.CrossFade("Idle", 0.08f);
        SetWeaponAt(weaponTableAnchor);
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

        var closePosition = new Vector3(0f, 1.48f, -0.72f);
        var closeRotation = Quaternion.LookRotation(new Vector3(0f, 1.02f, 0.35f) - closePosition);
        yield return MoveCamera(cameraRestPosition, cameraRestRotation, closePosition, closeRotation, 1f);

        if (roundRevealText != null)
        {
            roundRevealText.text = "2 LIVE   •   4 BLANK\nORDER UNKNOWN";
            roundRevealText.color = new Color(0.95f, 0.67f, 0.42f, 1f);
        }
        Debug.Log("[Chamber] Loading reveal: 2 live shells and 4 blank shells. Their order is hidden.");
        yield return new WaitForSeconds(1.8f);

        for (var i = 0; i < shellProps.Count; i++)
        {
            var shell = shellProps[i];
            if (shell == null) continue;
            var start = shell.transform.position;
            var startRotation = shell.transform.rotation;
            mechanicalSource.PlayOneShot(shellLoadClip, 0.52f);
            for (var t = 0f; t < 0.38f; t += Time.deltaTime)
            {
                var progress = Mathf.SmoothStep(0f, 1f, t / 0.38f);
                shell.transform.position = Vector3.Lerp(start, weaponBreechAnchor.position, progress) + Vector3.up * Mathf.Sin(progress * Mathf.PI) * 0.08f;
                shell.transform.rotation = Quaternion.Slerp(startRotation, weaponBreechAnchor.rotation, progress);
                yield return null;
            }
            shell.SetActive(false);
            yield return new WaitForSeconds(0.3f);
        }

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
                dealerAnimator.speed = 0.48f;
                dealerAnimator.CrossFade("Death", 0.08f);
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
            yield return new WaitForSeconds(0.22f);
        }
        else
        {
            dealerActing = true;
            dealerAnimator.speed = 0.88f;
            dealerAnimator.CrossFade("Gun Hold", 0.18f);
        }

        mechanicalSource.PlayOneShot(shellLoadClip, 0.46f);
        yield return MoveWeapon(aimAnchor, isPlayer ? 0.62f : 0.82f);
        if (!isPlayer)
        {
            dealerAnimator.CrossFade("Gun Point", 0.12f);
            yield return new WaitForSeconds(0.38f);
        }
        else
        {
            yield return new WaitForSeconds(0.22f);
        }

        if (!isPlayer) dealerAnimator.CrossFade("Gun Fire", 0.04f);
        audioSource.PlayOneShot(live ? liveShotClip : blankClickClip, live ? 0.9f : 0.68f);
        if (live && muzzleFlash != null) muzzleFlash.intensity = 9f;
        var aimPosition = playerWeapon.position;
        playerWeapon.position -= aimAnchor.forward * (live ? 0.16f : 0.035f);
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
            dealerAnimator.speed = 1f;
            dealerAnimator.CrossFade("Hit", 0.06f);
            yield return new WaitForSeconds(0.48f);
        }

        yield return MoveWeapon(weaponTableAnchor, 0.72f);
        dealerAnimator.speed = 0.76f;
        dealerAnimator.CrossFade("Idle", 0.16f);
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

    private static AudioClip CreateHorrorMusicClip()
    {
        const int sampleRate = 22050;
        const int seconds = 32;
        var frames = sampleRate * seconds;
        var samples = new float[frames * 2];
        var notes = new[] { 73.42f, 77.78f, 87.31f, 82.41f }; // D, Eb, F, E: a restrained Phrygian loop.
        for (var i = 0; i < frames; i++)
        {
            var time = (float)i / sampleRate;
            var bar = Mathf.FloorToInt(time / 8f) % notes.Length;
            var root = notes[bar];
            var slowSwell = 0.32f + Mathf.Pow(Mathf.Sin(time * Mathf.PI / 8f), 2f) * 0.68f;
            var low = Mathf.Sin(time * Mathf.PI * 2f * root * 0.5f) * 0.18f;
            var fifth = Mathf.Sin(time * Mathf.PI * 2f * root * 0.75f + 0.7f) * 0.07f;
            var dissonance = Mathf.Sin(time * Mathf.PI * 2f * (root + 1.35f)) * 0.035f;
            var bellPhase = time % 8f;
            var bell = bellPhase < 2.4f
                ? Mathf.Sin(bellPhase * Mathf.PI * 2f * root * 2f) * Mathf.Exp(-bellPhase * 1.7f) * 0.13f
                : 0f;
            var value = (low + fifth + dissonance) * slowSwell + bell;
            samples[i * 2] = value * (0.92f + Mathf.Sin(time * 0.21f) * 0.08f);
            samples[i * 2 + 1] = value * (0.92f + Mathf.Sin(time * 0.19f + 1.8f) * 0.08f);
        }
        var clip = AudioClip.Create("House elegy", frames, 2, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }
}
