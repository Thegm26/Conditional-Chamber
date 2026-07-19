using System.Collections;
using System.Reflection;
using ChamberLogic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public sealed class ChamberScenePlayTests
{
    [UnityTest]
    public IEnumerator OpeningLoadsFromSideStandWithActionOpen()
    {
        yield return SceneManager.LoadSceneAsync("Chamber", LoadSceneMode.Single);
        yield return null;

        var game = Object.FindAnyObjectByType<ChamberLogicGame>();
        var pump = GameObject.Find("Reload").transform;
        var pumpRestPosition = pump.localPosition;
        var firstShell = GameObject.Find("Shotgun Shell 1 — Live").transform;
        var loadingHand = GameObject.Find("Doll Right Hand").transform;
        var supportHand = GameObject.Find("Doll Left Hand").transform;
        var rearGrip = GameObject.Find("Apparition Right Grip").transform;
        var foreGrip = GameObject.Find("Apparition Left Grip").transform;
        var loadingHandRenderer = loadingHand.GetComponentInChildren<Renderer>();
        var firstShellRenderer = firstShell.GetComponentInChildren<Renderer>();
        var shellStartPosition = firstShell.position;
        var tableBounds = CombinedBounds(GameObject.Find("Hero Duel Table — Wood Table 7"));
        Assert.That(shellStartPosition.x, Is.LessThan(tableBounds.min.x), "Opening shell does not start beside the duel table.");

        yield return new WaitForSeconds(4.58f);
        var maximumOpenTravel = 0f;
        var maximumShellTravel = 0f;
        var minimumHandToShellDistance = float.MaxValue;
        var shellMovedWithoutHand = false;
        var bothHandsTookWeapon = false;
        var shellMovedWithoutGunSupport = false;
        for (var elapsed = 0f; elapsed < 2.85f; elapsed += Time.deltaTime)
        {
            bothHandsTookWeapon |= loadingHand.parent == rearGrip && supportHand.parent == foreGrip;
            maximumOpenTravel = Mathf.Max(maximumOpenTravel, Vector3.Distance(pumpRestPosition, pump.localPosition));
            maximumShellTravel = Mathf.Max(maximumShellTravel, Vector3.Distance(shellStartPosition, firstShell.position));
            if (firstShell.gameObject.activeSelf)
            {
                var handToShellDistance = Vector3.Distance(loadingHandRenderer.bounds.center, firstShellRenderer.bounds.center);
                minimumHandToShellDistance = Mathf.Min(minimumHandToShellDistance, handToShellDistance);
                if (Vector3.Distance(shellStartPosition, firstShell.position) > 0.03f && handToShellDistance > 0.14f)
                    shellMovedWithoutHand = true;
                if (Vector3.Distance(shellStartPosition, firstShell.position) > 0.03f && supportHand.parent != foreGrip)
                    shellMovedWithoutGunSupport = true;
            }
            yield return null;
        }
        Assert.That(maximumOpenTravel, Is.GreaterThan(0.018f), "The shotgun action was not visibly held open during loading.");
        Assert.That(maximumShellTravel, Is.GreaterThan(0.55f), "The first shell never travelled from the side stand to the open breech.");
        Assert.That(minimumHandToShellDistance, Is.LessThan(0.07f), "The loading hand never grasped the first shell.");
        Assert.That(shellMovedWithoutHand, Is.False, "The first shell moved without the loading hand holding it.");
        Assert.That(bothHandsTookWeapon, Is.True, "The doll did not take the shotgun with both hands before opening it.");
        Assert.That(shellMovedWithoutGunSupport, Is.False, "The supporting hand released the shotgun during loading.");

        yield return new WaitForSeconds(10.8f);
        foreach (var item in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include))
            if (item.name.StartsWith("Shotgun Shell ")) Assert.That(item.gameObject.activeSelf, Is.False, $"{item.name} was not loaded.");
        Assert.That(Vector3.Distance(pumpRestPosition, pump.localPosition), Is.LessThan(0.001f), "Shotgun did not close after the opening load.");
        Assert.That(game.enabled, Is.True);
        Debug.Log($"[OpeningTransformTrace] pumpTravel={maximumOpenTravel:F4}m shellTravel={maximumShellTravel:F3}m handGrip={minimumHandToShellDistance:F3}m start={shellStartPosition}");
    }

    [UnityTest]
    public IEnumerator SceneLoadsAndBothShotChoicesRunCleanly()
    {
        yield return SceneManager.LoadSceneAsync("Chamber", LoadSceneMode.Single);
        yield return null;

        var game = Object.FindAnyObjectByType<ChamberLogicGame>();
        Assert.That(game, Is.Not.Null, "Gameplay controller must be present.");
        Assert.That(game.enabled, Is.True, "Saved Chamber references are incomplete.");
        Assert.That(Object.FindAnyObjectByType<Canvas>(), Is.Null, "The art pass should not contain UI.");
        Assert.That(GameObject.Find("The House — Dealer Character"), Is.Null, "The humanoid dealer must be removed.");
        Assert.That(Object.FindObjectsByType<Animator>(FindObjectsInactive.Include), Is.Empty, "The doll must remain a static prop without an animation rig.");
        Assert.That(GameObject.Find("Player Hands Rig"), Is.Null);
        Assert.That(GameObject.Find("Player Left Hand"), Is.Null);
        Assert.That(GameObject.Find("Player Right Hand"), Is.Null);

        var apparition = GameObject.Find("The House — Voodoo Doll");
        var rightHand = GameObject.Find("Doll Right Hand").transform;
        var leftHand = GameObject.Find("Doll Left Hand").transform;
        var face = GameObject.Find("Voodoo Doll Face Target").transform;
        var handRest = GameObject.Find("Doll Hands — Side Rest").transform;
        var doll = GameObject.Find("Voodoo Doll — Seated Target");
        Assert.That(apparition, Is.Not.Null);
        Assert.That(doll, Is.Not.Null);
        Assert.That(rightHand, Is.Not.Null);
        Assert.That(leftHand, Is.Not.Null);
        Assert.That(rightHand.parent, Is.EqualTo(handRest));
        Assert.That(leftHand.parent, Is.EqualTo(handRest));
        Assert.That(apparition.GetComponentsInChildren<Renderer>(true), Has.Length.EqualTo(1), "The target should contain only the real Voodoo Doll renderer.");
        Assert.That(apparition.GetComponentInChildren<Animator>(), Is.Null);
        var dollBounds = CombinedBounds(doll);
        Assert.That(dollBounds.size.x, Is.InRange(0.59f, 0.64f));
        Assert.That(dollBounds.size.y, Is.InRange(0.82f, 0.86f));
        Assert.That(dollBounds.min.y, Is.InRange(0.47f, 0.51f));
        Assert.That(dollBounds.max.y, Is.InRange(1.31f, 1.35f));
        Assert.That(face.position.y, Is.EqualTo(dollBounds.max.y - 0.16f).Within(0.015f));
        Assert.That(dollBounds.min.z - face.position.z, Is.InRange(0.005f, 0.045f));
        Assert.That(GameObject.Find("Horror Package Hanging Shroud"), Is.Null);
        Assert.That(GameObject.Find("Apparition Scary Face"), Is.Null);
        Assert.That(Object.FindAnyObjectByType<ChamberPoseDebug>(), Is.Not.Null, "Scene-space aim lines are missing.");

        Assert.That(GameObject.Find("Horror Package — Authored Dressing"), Is.Not.Null);
        Assert.That(GameObject.Find("Horror Coffin — Back Right"), Is.Not.Null);
        Assert.That(GameObject.Find("Hanging Body Bag — Back Left"), Is.Not.Null);
        Assert.That(GameObject.Find("Horror Bottles — Evidence Shelf"), Is.Not.Null);
        Assert.That(GameObject.Find("Apparition Red Focus"), Is.Not.Null);
        var tableFocus = GameObject.Find("Table Cold Focus").GetComponent<Light>();
        Assert.That(tableFocus, Is.Not.Null);
        Assert.That(tableFocus.intensity, Is.GreaterThanOrEqualTo(2.1f), "The shotgun work light is too dim.");
        Assert.That(tableFocus.spotAngle, Is.GreaterThanOrEqualTo(56f), "The shotgun work light is too narrow.");
        Assert.That(RenderSettings.ambientLight.maxColorComponent, Is.LessThan(0.002f), "The room ambient light is not black enough.");
        var mainMusic = (AudioSource)typeof(ChamberLogicGame).GetField("musicSource", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(game);
        var layeredMusic = GameObject.Find("Horror Music Layer").GetComponent<AudioSource>();
        var dollMusic = GameObject.Find("Doll Music Box").GetComponent<AudioSource>();
        Assert.That(mainMusic.clip.name, Is.EqualTo("Creepy_Ambient_Layer"));
        Assert.That(layeredMusic.clip.name, Is.EqualTo("Abandoned_Passages"));
        Assert.That(dollMusic.clip.name, Is.EqualTo("Doll_Spooky_Waltz"));
        Assert.That(mainMusic.isPlaying, Is.True);
        Assert.That(layeredMusic.isPlaying, Is.True);
        Assert.That(dollMusic.isPlaying, Is.True);
        foreach (var name in new[] { "Right Wall", "Left Wall", "Back Wall", "Floor", "Ceiling" })
            foreach (var renderer in GameObject.Find(name).GetComponentsInChildren<Renderer>(true))
                foreach (var material in renderer.sharedMaterials)
                    Assert.That(material.name, Is.EqualTo("Room_Black"), $"{name} is not using the authored black-room material.");

        var shotgun = GameObject.Find("Player Shotgun").transform;
        var shotgunCount = 0;
        foreach (var item in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include))
            if (item.name == "Player Shotgun" || item.name == "Dealer Shotgun") shotgunCount++;
        Assert.That(shotgunCount, Is.EqualTo(1), "Exactly one shared shotgun should exist.");
        var gunRenderers = shotgun.GetComponentsInChildren<Renderer>(true);
        Assert.That(GameObject.Find("Imported ShotGun C Visual"), Is.Not.Null);
        Assert.That(gunRenderers, Has.Length.GreaterThanOrEqualTo(8));
        foreach (var renderer in gunRenderers)
            foreach (var material in renderer.sharedMaterials)
                Assert.That(material, Is.Not.Null);
        Assert.That(GameObject.Find("Shotgun Detail — Barrel Light"), Is.Null);

        var table = GameObject.Find("Hero Duel Table — Wood Table 7");
        var tableBounds = CombinedBounds(table);
        var gunBounds = CombinedBounds(shotgun.gameObject);
        Assert.That(tableBounds.min.y, Is.EqualTo(0f).Within(0.002f));
        Assert.That(tableBounds.max.y, Is.InRange(0.82f, 0.90f));
        Assert.That(tableBounds.size.x, Is.InRange(1.65f, 1.71f));
        Assert.That(tableBounds.size.z, Is.InRange(1.14f, 1.20f));
        Assert.That(gunBounds.min.y - tableBounds.max.y, Is.InRange(0.005f, 0.06f), "Shotgun intersects or floats above the table.");
        Assert.That(gunBounds.center.x, Is.EqualTo(tableBounds.center.x).Within(0.002f));
        Assert.That(gunBounds.center.z, Is.EqualTo(tableBounds.center.z).Within(0.002f));
        Assert.That(gunBounds.size.z, Is.InRange(0.82f, 0.86f));

        var shellStandBounds = CombinedBounds(GameObject.Find("Shell Crate Stand"));
        Assert.That(shellStandBounds.min.y, Is.EqualTo(0f).Within(0.002f), "Shell stand does not meet the floor.");
        Assert.That(shellStandBounds.max.y, Is.InRange(0.62f, 0.67f));
        Assert.That(shellStandBounds.max.x, Is.LessThan(tableBounds.min.x - 0.02f), "Shell stand overlaps the duel table.");

        var shells = 0;
        foreach (var item in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include))
        {
            if (!item.name.StartsWith("Shotgun Shell ")) continue;
            shells++;
            var shellRenderer = item.GetComponentInChildren<Renderer>();
            var shellSize = Vector3.Scale(shellRenderer.localBounds.size, shellRenderer.transform.lossyScale);
            Assert.That(Mathf.Max(shellSize.x, shellSize.y, shellSize.z), Is.InRange(0.055f, 0.075f));
            Assert.That(Mathf.Min(shellSize.x, shellSize.y, shellSize.z), Is.InRange(0.014f, 0.020f));
            Assert.That(shellRenderer.bounds.min.y - shellStandBounds.max.y, Is.InRange(0.002f, 0.025f), "A shell floats above or intersects the side stand.");
            Assert.That(shellRenderer.bounds.max.x, Is.LessThan(tableBounds.min.x), "A shell is not beside the main table.");
        }
        Assert.That(shells, Is.EqualTo(6));

        game.StopAllCoroutines();
        Invoke(game, "CompleteOpening");
        var pump = GameObject.Find("Reload").transform;
        var pumpRestPosition = pump.localPosition;
        game.StartCoroutine((IEnumerator)InvokeWithResult(game, "OpenWeaponAction", 0.52f));
        yield return new WaitForSeconds(0.58f);
        Assert.That(Vector3.Distance(pumpRestPosition, pump.localPosition), Is.GreaterThan(0.018f), "Shotgun did not remain visibly open for loading.");
        game.StartCoroutine((IEnumerator)InvokeWithResult(game, "CloseWeaponAction", 0.46f));
        yield return new WaitForSeconds(0.52f);
        Assert.That(Vector3.Distance(pumpRestPosition, pump.localPosition), Is.LessThan(0.001f), "Shotgun did not close after loading.");
        game.StartCoroutine((IEnumerator)InvokeWithResult(game, "PumpWeapon", 0.40f));
        var maximumPumpTravel = 0f;
        for (var elapsed = 0f; elapsed < 0.46f; elapsed += Time.deltaTime)
        {
            maximumPumpTravel = Mathf.Max(maximumPumpTravel, Vector3.Distance(pumpRestPosition, pump.localPosition));
            yield return null;
        }
        Assert.That(maximumPumpTravel, Is.GreaterThan(0.055f));
        Assert.That(Vector3.Distance(pumpRestPosition, pump.localPosition), Is.LessThan(0.001f));

        var muzzle = GameObject.Find("Muzzle Flash").transform;
        var tablePosition = shotgun.position;
        Invoke(game, "ChooseDealer");
        yield return new WaitForSeconds(0.92f);
        Assert.That(Vector3.Distance(tablePosition, shotgun.position), Is.GreaterThan(0.08f));
        Assert.That(Vector3.Angle(muzzle.forward, face.position - muzzle.position), Is.LessThan(1f), "Player aim misses the apparition face point.");
        yield return new WaitForSeconds(2.2f);
        Assert.That(CurrentRound(game).RemainingTotal, Is.LessThan(6));

        ResetRound(game);
        var rightRestPosition = rightHand.localPosition;
        var leftRestPosition = leftHand.localPosition;
        game.StartCoroutine((IEnumerator)InvokeWithResult(game, "ResolveShot", false, false));
        yield return new WaitForSeconds(1.48f);
        var rightGrip = GameObject.Find("Apparition Right Grip").transform;
        var leftGrip = GameObject.Find("Apparition Left Grip").transform;
        Assert.That(Vector3.Distance(rightHand.position, rightGrip.position), Is.LessThan(0.001f), "Direct right hand misses the rear grip.");
        Assert.That(Vector3.Distance(leftHand.position, leftGrip.position), Is.LessThan(0.001f), "Direct left hand misses the fore grip.");
        Assert.That(rightHand.parent, Is.EqualTo(rightGrip));
        Assert.That(leftHand.parent, Is.EqualTo(leftGrip));
        var rearHandAxisError = Mathf.Min(Vector3.Angle(rightHand.up, muzzle.forward), Vector3.Angle(-rightHand.up, muzzle.forward));
        Assert.That(rearHandAxisError, Is.LessThan(12f), "Rear hand is not aligned along the shotgun stock.");
        Assert.That(Vector3.Angle(leftHand.up, muzzle.forward), Is.InRange(75f, 105f), "Fore hand is not wrapped across the fore-end.");
        var camera = GameObject.Find("Duel Camera").transform;
        Assert.That(Vector3.Angle(muzzle.forward, camera.position - muzzle.position), Is.LessThan(1f), "Apparition aim misses the player camera.");
        yield return new WaitForSeconds(2.5f);
        Assert.That(CurrentRound(game).RemainingTotal, Is.LessThan(6));
        Assert.That(rightHand.parent, Is.EqualTo(handRest));
        Assert.That(leftHand.parent, Is.EqualTo(handRest));
        Assert.That(Vector3.Distance(rightHand.localPosition, rightRestPosition), Is.LessThan(0.001f));
        Assert.That(Vector3.Distance(leftHand.localPosition, leftRestPosition), Is.LessThan(0.001f));

        ResetRound(game);
        game.StartCoroutine((IEnumerator)InvokeWithResult(game, "ResolveShot", false, true));
        yield return new WaitForSeconds(1.48f);
        var selfRightGrip = GameObject.Find("Apparition Self Right Grip").transform;
        var selfLeftGrip = GameObject.Find("Apparition Self Left Grip").transform;
        Assert.That(Vector3.Distance(rightHand.position, selfRightGrip.position), Is.LessThan(0.001f));
        Assert.That(Vector3.Distance(leftHand.position, selfLeftGrip.position), Is.LessThan(0.001f));
        Assert.That(Vector3.Distance(muzzle.position, face.position), Is.GreaterThan(0.18f), "Self-aim muzzle intersects the apparition.");
        Assert.That(Vector3.Angle(muzzle.forward, face.position - muzzle.position), Is.LessThan(1f), "Self-aim misses the apparition face point.");

        ResetRound(game);
        var entityRestPosition = apparition.transform.localPosition;
        var entityRestRotation = apparition.transform.localRotation;
        yield return new WaitForSeconds(0.45f);
        Assert.That(Vector3.Distance(entityRestPosition, apparition.transform.localPosition), Is.LessThan(0.0001f), "The seated doll moves while idle.");
        Assert.That(Quaternion.Angle(entityRestRotation, apparition.transform.localRotation), Is.LessThan(0.001f), "The seated doll rotates while idle.");
        game.StartCoroutine((IEnumerator)InvokeWithResult(game, "AnimateShot", true, false, true));
        var maximumOffset = 0f;
        var maximumTilt = 0f;
        var maximumBackwardOffset = 0f;
        var maximumLateralOffset = 0f;
        var voicePlayed = false;
        var voiceSource = GameObject.Find("Doll Voice Source").GetComponent<AudioSource>();
        for (var elapsed = 0f; elapsed < 4.35f; elapsed += Time.deltaTime)
        {
            maximumOffset = Mathf.Max(maximumOffset, Vector3.Distance(entityRestPosition, apparition.transform.localPosition));
            maximumTilt = Mathf.Max(maximumTilt, Vector3.Angle(Vector3.up, apparition.transform.up));
            maximumBackwardOffset = Mathf.Max(maximumBackwardOffset, apparition.transform.localPosition.z - entityRestPosition.z);
            maximumLateralOffset = Mathf.Max(maximumLateralOffset, Mathf.Abs(apparition.transform.localPosition.x - entityRestPosition.x));
            voicePlayed |= voiceSource.isPlaying;
            yield return null;
        }
        Assert.That(maximumOffset, Is.InRange(0.30f, 0.34f), "The player-shot doll does not fall backward far enough.");
        Assert.That(maximumTilt, Is.InRange(59f, 63f));
        Assert.That(maximumBackwardOffset, Is.GreaterThan(0.27f));
        Assert.That(maximumLateralOffset, Is.LessThan(0.03f));
        Assert.That(voicePlayed, Is.True, "The doll did not vocalize when shot.");
        Assert.That(Vector3.Distance(entityRestPosition, apparition.transform.localPosition), Is.LessThan(0.002f), "The surviving doll did not return to its fixed seat.");

        ResetRound(game);
        game.StartCoroutine((IEnumerator)InvokeWithResult(game, "AnimateShot", false, true, true));
        var maximumSideOffset = 0f;
        var selfShotBackwardOffset = 0f;
        var selfShotTilt = 0f;
        for (var elapsed = 0f; elapsed < 5.35f; elapsed += Time.deltaTime)
        {
            maximumSideOffset = Mathf.Max(maximumSideOffset, Mathf.Abs(apparition.transform.localPosition.x - entityRestPosition.x));
            selfShotBackwardOffset = Mathf.Max(selfShotBackwardOffset, apparition.transform.localPosition.z - entityRestPosition.z);
            selfShotTilt = Mathf.Max(selfShotTilt, Vector3.Angle(Vector3.up, apparition.transform.up));
            yield return null;
        }
        Assert.That(maximumSideOffset, Is.GreaterThan(0.20f), "The self-shot doll did not fall sideways.");
        Assert.That(selfShotBackwardOffset, Is.LessThan(0.09f), "The self-shot reaction incorrectly reused the backward fall.");
        Assert.That(selfShotTilt, Is.InRange(65f, 70f));
        Debug.Log($"[DollTransformTrace] backOffset={maximumBackwardOffset:F4}m backTilt={maximumTilt:F2}deg sideOffset={maximumSideOffset:F4}m sideTilt={selfShotTilt:F2}deg");
    }

    private static void ResetRound(ChamberLogicGame game)
    {
        game.StopAllCoroutines();
        Invoke(game, "StartExperiment");
        game.StopAllCoroutines();
        Invoke(game, "CompleteOpening");
        Assert.That(CurrentRound(game).RemainingTotal, Is.EqualTo(6));
    }

    private static ChamberRound CurrentRound(ChamberLogicGame game) =>
        (ChamberRound)typeof(ChamberLogicGame).GetField("round", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(game);

    private static void Invoke(ChamberLogicGame game, string methodName) =>
        typeof(ChamberLogicGame).GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(game, null);

    private static object InvokeWithResult(ChamberLogicGame game, string methodName, params object[] arguments) =>
        typeof(ChamberLogicGame).GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(game, arguments);

    private static Bounds CombinedBounds(GameObject item)
    {
        Assert.That(item, Is.Not.Null);
        var renderers = item.GetComponentsInChildren<Renderer>(true);
        Assert.That(renderers, Is.Not.Empty);
        var bounds = renderers[0].bounds;
        for (var i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
        return bounds;
    }
}
