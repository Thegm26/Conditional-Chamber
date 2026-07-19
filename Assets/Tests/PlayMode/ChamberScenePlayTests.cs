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
    public IEnumerator SceneLoadsAndBothShotChoicesRunCleanly()
    {
        yield return SceneManager.LoadSceneAsync("Chamber", LoadSceneMode.Single);
        yield return null;

        var game = Object.FindAnyObjectByType<ChamberLogicGame>();
        Assert.That(game, Is.Not.Null, "Gameplay controller must be present.");
        Assert.That(Object.FindAnyObjectByType<Canvas>(), Is.Null, "The scene should not contain UI during the art pass.");
        Assert.That(GameObject.Find("EventSystem"), Is.Null);
        Assert.That(GameObject.Find("Environment — Authored Detail Pass"), Is.Not.Null);
        Assert.That(GameObject.Find("The House — Dealer Character"), Is.Not.Null);
        Assert.That(GameObject.Find("Left Security Watcher"), Is.Null);
        Assert.That(GameObject.Find("Right Security Watcher"), Is.Null);
        Assert.That(GameObject.Find("Duel Furniture — Matching Wood Set"), Is.Not.Null);
        Assert.That(GameObject.Find("Hero Duel Table — Wood Table 7"), Is.Not.Null);
        Assert.That(GameObject.Find("Dealer Chair — Matching Wood Chair 4"), Is.Not.Null);
        Assert.That(GameObject.Find("Player Chair — Matching Wood Chair 4"), Is.Not.Null);
        Assert.That(GameObject.Find("Imported Walnut Duel Table"), Is.Null, "The stretched placeholder table should be removed.");
        Assert.That(GameObject.Find("Player Hands Rig"), Is.Null, "Player hands should be removed from the authored scene.");
        Assert.That(GameObject.Find("Player Left Hand"), Is.Null);
        Assert.That(GameObject.Find("Player Right Hand"), Is.Null);
        Assert.That(Object.FindAnyObjectByType<ChamberPoseDebug>(), Is.Not.Null, "Scene-space grip and aim debug lines are missing.");
        Assert.That(Object.FindObjectsByType<Light>(FindObjectsInactive.Include), Has.Length.GreaterThanOrEqualTo(8));
        foreach (var transform in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include))
            Assert.That(transform.name, Does.Not.Contain("Skeleton"));

        var shotguns = 0;
        Transform playerShotgun = null;
        foreach (var item in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include))
        {
            if (item.name != "Player Shotgun" && item.name != "Dealer Shotgun") continue;
            shotguns++;
            playerShotgun = item;
        }
        Assert.That(shotguns, Is.EqualTo(1), "Exactly one visible shotgun should exist.");

        var gunRenderers = playerShotgun.GetComponentsInChildren<Renderer>(true);
        Assert.That(gunRenderers, Has.Length.GreaterThanOrEqualTo(2));
        var baseRenderer = System.Array.Find(gunRenderers, renderer => renderer.sharedMaterials.Length == 6);
        Assert.That(baseRenderer, Is.Not.Null, "The authored shotgun body lost its six material slots.");
        foreach (var renderer in gunRenderers)
            foreach (var material in renderer.sharedMaterials) Assert.That(material, Is.Not.Null);
        Assert.That(Mathf.Max(baseRenderer.bounds.size.x, baseRenderer.bounds.size.z), Is.InRange(0.9f, 1.3f), "Shotgun visual scale regressed.");
        Assert.That(Mathf.Abs(playerShotgun.lossyScale.x - playerShotgun.lossyScale.y), Is.LessThan(0.0001f), "Shotgun has distorted non-uniform scale.");
        Assert.That(Mathf.Abs(playerShotgun.lossyScale.y - playerShotgun.lossyScale.z), Is.LessThan(0.0001f), "Shotgun has distorted non-uniform scale.");
        Assert.That(GameObject.Find("Shotgun Detail — Barrel Light"), Is.Null, "The fake barrel-light mesh should be removed.");
        var duelCamera = GameObject.Find("Duel Camera").GetComponent<Camera>();
        var gunBounds = CombinedBounds(playerShotgun.gameObject);
        foreach (var corner in BoundsCorners(gunBounds))
        {
            var viewport = duelCamera.WorldToViewportPoint(corner);
            Assert.That(viewport.z, Is.GreaterThan(duelCamera.nearClipPlane), "Part of the table shotgun is behind the camera near plane.");
            Assert.That(viewport.x, Is.InRange(0.05f, 0.95f), "Part of the table shotgun is outside the horizontal camera frame.");
            Assert.That(viewport.y, Is.InRange(0.05f, 0.95f), "Part of the table shotgun is outside the vertical camera frame.");
        }

        var table = GameObject.Find("Hero Duel Table — Wood Table 7");
        var tableBounds = CombinedBounds(table);
        Assert.That(tableBounds.min.y, Is.EqualTo(0f).Within(0.002f), "Table legs do not meet the floor.");
        Assert.That(tableBounds.max.y, Is.InRange(0.82f, 0.90f), "Tabletop height is implausible.");
        Assert.That(tableBounds.size.x, Is.InRange(1.65f, 1.71f));
        Assert.That(tableBounds.size.z, Is.InRange(1.14f, 1.20f));
        AssertUniformScale(table.transform, 0.84f, "duel table");
        foreach (var renderer in table.GetComponentsInChildren<Renderer>())
            foreach (var material in renderer.sharedMaterials) Assert.That(material.name, Is.EqualTo("WoodFurniture_DarkOak"));
        var dealerChair = GameObject.Find("Dealer Chair — Matching Wood Chair 4");
        var playerChair = GameObject.Find("Player Chair — Matching Wood Chair 4");
        AssertUniformScale(dealerChair.transform, 0.70f, "dealer chair");
        AssertUniformScale(playerChair.transform, 0.70f, "player chair");
        Assert.That(CombinedBounds(dealerChair).min.y, Is.EqualTo(0f).Within(0.002f));
        Assert.That(CombinedBounds(playerChair).min.y, Is.EqualTo(0f).Within(0.002f));
        Assert.That(gunBounds.min.y - tableBounds.max.y, Is.InRange(0.005f, 0.06f), "Shotgun intersects or floats above the tabletop.");
        Assert.That(gunBounds.center.x, Is.EqualTo(tableBounds.center.x).Within(0.002f), "Shotgun is not centered across the new table.");
        Assert.That(gunBounds.center.z, Is.EqualTo(tableBounds.center.z).Within(0.002f), "Shotgun is not centered along the new table.");
        Assert.That(gunBounds.min.x, Is.GreaterThan(tableBounds.min.x));
        Assert.That(gunBounds.max.x, Is.LessThan(tableBounds.max.x));
        Assert.That(gunBounds.min.z, Is.GreaterThan(tableBounds.min.z));
        Assert.That(gunBounds.max.z, Is.LessThan(tableBounds.max.z));

        var dealer = GameObject.Find("The House — Dealer Character");
        var dealerAnimator = dealer.GetComponentInChildren<Animator>();
        Assert.That(dealerAnimator, Is.Not.Null, "Dealer Animator is missing.");
        Assert.That(dealerAnimator.runtimeAnimatorController, Is.Not.Null, "Dealer animation controller is missing.");
        Assert.That(dealerAnimator.applyRootMotion, Is.False, "Dealer root motion must remain disabled.");
        Assert.That(Object.FindObjectsByType<Animator>(FindObjectsInactive.Include), Has.Length.EqualTo(1), "Only the seated dealer should remain in the scene.");
        var musicSource = (AudioSource)typeof(ChamberLogicGame).GetField("musicSource", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(game);
        Assert.That(musicSource, Is.Not.Null, "The saved scene lost its horror-music source reference.");
        Assert.That(musicSource.isPlaying, Is.True, "The horror score did not begin playing.");
        var animatedArm = System.Array.Find(dealer.GetComponentsInChildren<Transform>(true), item => item.name == "UpperArm.L");
        Assert.That(animatedArm, Is.Not.Null, "Dealer animation rig is incomplete.");
        var armBefore = animatedArm.localRotation;
        yield return new WaitForSeconds(0.55f);
        Assert.That(Quaternion.Angle(armBefore, animatedArm.localRotation), Is.GreaterThan(0.05f), "Dealer idle animation is not advancing.");

        var dealerBounds = CombinedBounds(dealer);
        Assert.That(dealerBounds.max.y, Is.LessThan(1.65f), "Dealer is not seated behind the table.");
        Assert.That(dealerBounds.center.y, Is.LessThan(0.85f));
        Assert.That(GameObject.Find("Range Target"), Is.Null, "The dealer is now the shot target; the old range target must be removed.");

        var shells = 0;
        foreach (var item in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include))
        {
            if (!item.name.StartsWith("Shotgun Shell ")) continue;
            shells++;
            var shellRenderer = item.GetComponentInChildren<Renderer>();
            var shellSize = Vector3.Scale(shellRenderer.localBounds.size, shellRenderer.transform.lossyScale);
            var shellLength = Mathf.Max(shellSize.x, shellSize.y, shellSize.z);
            var shellDiameter = Mathf.Min(shellSize.x, shellSize.y, shellSize.z);
            Assert.That(shellLength, Is.InRange(0.055f, 0.075f), "A shell is not close to a real 12-gauge shell length after rotation.");
            Assert.That(shellDiameter, Is.InRange(0.014f, 0.020f), "A shell has an implausible diameter.");
            AssertUniformScale(item, 0.22f, "shotgun shell");
        }
        Assert.That(shells, Is.EqualTo(6));
        Assert.That(GameObject.Find("Side Loading Rack"), Is.Not.Null);

        game.StopAllCoroutines();
        Invoke(game, "CompleteOpening");

        var pump = GameObject.Find("Reload").transform;
        var pumpRestPosition = pump.localPosition;
        var pumpRoutine = (IEnumerator)typeof(ChamberLogicGame).GetMethod("PumpWeapon", BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(game, new object[] { 0.40f });
        game.StartCoroutine(pumpRoutine);
        var maximumPumpTravel = 0f;
        for (var elapsed = 0f; elapsed < 0.46f; elapsed += Time.deltaTime)
        {
            maximumPumpTravel = Mathf.Max(maximumPumpTravel, Vector3.Distance(pumpRestPosition, pump.localPosition));
            yield return null;
        }
        Assert.That(maximumPumpTravel, Is.GreaterThan(0.007f), "The shotgun fore-end did not visibly pump.");
        Assert.That(Vector3.Distance(pumpRestPosition, pump.localPosition), Is.LessThan(0.001f), "The shotgun pump did not return to battery.");

        var tablePosition = playerShotgun.position;
        Invoke(game, "ChooseDealer");
        yield return new WaitForSeconds(1.08f);
        Assert.That(Vector3.Distance(tablePosition, playerShotgun.position), Is.GreaterThan(0.08f), "The player did not pick the shotgun up from the table.");
        var muzzle = GameObject.Find("Muzzle Flash").transform;
        var dealerFace = GameObject.Find("Dealer Face Debug Point").transform;
        Assert.That(Vector3.Angle(muzzle.forward, dealerFace.position - muzzle.position), Is.LessThan(1f), "Player shotgun debug line misses the dealer's face.");
        yield return new WaitForSeconds(1.92f);
        Assert.That(CurrentRound(game).RemainingTotal, Is.LessThan(6), "House-target shot did not consume a shell.");

        game.StopAllCoroutines();
        Invoke(game, "StartExperiment");
        game.StopAllCoroutines();
        Invoke(game, "CompleteOpening");
        Assert.That(CurrentRound(game).RemainingTotal, Is.EqualTo(6));

        tablePosition = playerShotgun.position;
        var dealerRoutine = (IEnumerator)typeof(ChamberLogicGame).GetMethod("ResolveShot", BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(game, new object[] { false, false });
        game.StartCoroutine(dealerRoutine);
        yield return new WaitForSeconds(0.9f);
        Assert.That(Vector3.Distance(tablePosition, playerShotgun.position), Is.GreaterThan(0.08f), "The dealer did not take the shared shotgun from the table.");
        Assert.That(Vector3.Angle(muzzle.forward, duelCamera.transform.position - muzzle.position), Is.LessThan(1f), "Dealer shotgun debug line misses the player camera.");
        var rightHand = dealerAnimator.GetBoneTransform(HumanBodyBones.RightHand);
        var leftHand = dealerAnimator.GetBoneTransform(HumanBodyBones.LeftHand);
        var rearGrip = GameObject.Find("Shotgun Rear Grip Debug Point").transform;
        var foreGrip = GameObject.Find("Shotgun Fore Grip Debug Point").transform;
        Assert.That(Vector3.Distance(rightHand.position, rearGrip.position), Is.LessThan(0.025f), "Dealer right hand misses the rear grip.");
        Assert.That(Vector3.Distance(leftHand.position, foreGrip.position), Is.LessThan(0.025f), "Dealer left hand misses the fore grip.");
        yield return new WaitForSeconds(2.05f);
        Assert.That(CurrentRound(game).RemainingTotal, Is.LessThan(6), "Dealer shot did not consume a shell.");

        game.StopAllCoroutines();
        Invoke(game, "StartExperiment");
        game.StopAllCoroutines();
        Invoke(game, "CompleteOpening");
        Assert.That(CurrentRound(game).RemainingTotal, Is.EqualTo(6), "Restart did not restore the chamber.");

        var dealerSelfRoutine = (IEnumerator)typeof(ChamberLogicGame).GetMethod("ResolveShot", BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(game, new object[] { false, true });
        game.StartCoroutine(dealerSelfRoutine);
        yield return new WaitForSeconds(0.9f);
        var selfRightGrip = GameObject.Find("Shotgun Self Right Contact").transform;
        var selfLeftGrip = GameObject.Find("Shotgun Self Left Contact").transform;
        Assert.That(Vector3.Distance(rightHand.position, selfRightGrip.position), Is.LessThan(0.025f), "Dealer right hand misses the self-aim contact.");
        Assert.That(Vector3.Distance(leftHand.position, selfLeftGrip.position), Is.LessThan(0.025f), "Dealer left hand misses the self-aim contact.");
        Assert.That(Vector3.Distance(muzzle.position, dealerFace.position), Is.GreaterThan(0.18f), "Self-aim muzzle is embedded in the dealer's face.");
        Assert.That(Vector3.Angle(muzzle.forward, dealerFace.position - muzzle.position), Is.LessThan(1f), "Self-aim muzzle line misses the dealer's face.");

        game.StopAllCoroutines();
        Invoke(game, "StartExperiment");
        game.StopAllCoroutines();
        Invoke(game, "CompleteOpening");

        var dealerRootPosition = dealer.transform.localPosition;
        var dealerHitRoutine = (IEnumerator)typeof(ChamberLogicGame).GetMethod("AnimateShot", BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(game, new object[] { true, false, true });
        game.StartCoroutine(dealerHitRoutine);
        var maximumRootOffset = 0f;
        var maximumUpTilt = 0f;
        var minimumDealerTop = float.PositiveInfinity;
        var restDealerCenter = CombinedBounds(dealer).center;
        var maximumRenderedSideShift = 0f;
        var maximumLateralRoll = 0f;
        for (var elapsed = 0f; elapsed < 3.0f; elapsed += Time.deltaTime)
        {
            maximumRootOffset = Mathf.Max(maximumRootOffset, Vector3.Distance(dealerRootPosition, dealer.transform.localPosition));
            maximumUpTilt = Mathf.Max(maximumUpTilt, Vector3.Angle(Vector3.up, dealer.transform.up));
            var currentBounds = CombinedBounds(dealer);
            minimumDealerTop = Mathf.Min(minimumDealerTop, currentBounds.max.y);
            maximumRenderedSideShift = Mathf.Max(maximumRenderedSideShift, Mathf.Abs(currentBounds.center.x - restDealerCenter.x));
            maximumLateralRoll = Mathf.Max(maximumLateralRoll, Mathf.Abs(Vector3.Dot(dealer.transform.right, Vector3.up)));
            yield return null;
        }
        Assert.That(maximumRootOffset, Is.InRange(0.08f, 0.14f), "Dealer hit reaction is either invisible or leaves the chair.");
        Assert.That(maximumUpTilt, Is.InRange(10f, 15.5f), "Dealer lacks a controlled backward hit reaction.");
        Assert.That(maximumLateralRoll, Is.LessThan(0.01f), "Dealer fell sideways instead of recoiling backward.");
        Assert.That(minimumDealerTop, Is.GreaterThan(0.9f), "Dealer hit animation collapsed below the tabletop.");
        Assert.That(maximumRenderedSideShift, Is.LessThan(0.35f), "Dealer mesh fell sideways during the hit.");
        Assert.That(Vector3.Distance(dealerRootPosition, dealer.transform.localPosition), Is.LessThan(0.002f), "Dealer did not settle back into the chair.");
        Debug.Log($"[DealerTransformTrace] hit maxRootOffset={maximumRootOffset:F4}m maxBackwardTilt={maximumUpTilt:F3}deg maxLateralRoll={maximumLateralRoll:F4} maxMeshSideShift={maximumRenderedSideShift:F3}m minTop={minimumDealerTop:F3}m");

        SetField(game, "dealerActing", true);
        dealerAnimator.CrossFade("Death", 0f);
        Invoke(game, "ApplyDealerDefeatedPose");
        yield return new WaitForSeconds(0.8f);
        var defeatedBounds = CombinedBounds(dealer);
        Assert.That(Vector3.Distance(dealerRootPosition, dealer.transform.localPosition), Is.InRange(0.08f, 0.12f), "Dealer death pose lacks a visible backward displacement.");
        Assert.That(Vector3.Angle(Vector3.up, dealer.transform.up), Is.InRange(10f, 13f), "Dealer death pose lacks a controlled backward lean.");
        Assert.That(Mathf.Abs(Vector3.Dot(dealer.transform.right, Vector3.up)), Is.LessThan(0.01f), "Dealer death pose has sideways roll.");
        Assert.That(defeatedBounds.max.y, Is.GreaterThan(0.9f), "Dealer death animation fell below the tabletop.");
        Assert.That(Mathf.Abs(defeatedBounds.center.x - restDealerCenter.x), Is.LessThan(0.35f), "Dealer death mesh fell sideways.");
        Debug.Log($"[DealerTransformTrace] death root={dealer.transform.localPosition} euler={dealer.transform.localEulerAngles} meshCenter={defeatedBounds.center} meshTop={defeatedBounds.max.y:F3}m");
    }

    private static ChamberRound CurrentRound(ChamberLogicGame game)
    {
        return (ChamberRound)typeof(ChamberLogicGame).GetField("round", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(game);
    }

    private static void Invoke(ChamberLogicGame game, string methodName)
    {
        typeof(ChamberLogicGame).GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(game, null);
    }

    private static void SetField(ChamberLogicGame game, string fieldName, object value)
    {
        typeof(ChamberLogicGame).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(game, value);
    }

    private static Bounds CombinedBounds(GameObject item)
    {
        Assert.That(item, Is.Not.Null);
        var renderers = item.GetComponentsInChildren<Renderer>(true);
        Assert.That(renderers, Is.Not.Empty);
        var bounds = renderers[0].bounds;
        for (var i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
        return bounds;
    }

    private static Vector3[] BoundsCorners(Bounds bounds)
    {
        var min = bounds.min;
        var max = bounds.max;
        return new[]
        {
            new Vector3(min.x, min.y, min.z), new Vector3(min.x, min.y, max.z),
            new Vector3(min.x, max.y, min.z), new Vector3(min.x, max.y, max.z),
            new Vector3(max.x, min.y, min.z), new Vector3(max.x, min.y, max.z),
            new Vector3(max.x, max.y, min.z), new Vector3(max.x, max.y, max.z)
        };
    }

    private static void AssertUniformScale(Transform item, float expected, string label)
    {
        Assert.That(item.localScale.x, Is.EqualTo(expected).Within(0.0001f), $"{label} X scale is wrong.");
        Assert.That(item.localScale.y, Is.EqualTo(expected).Within(0.0001f), $"{label} Y scale is wrong.");
        Assert.That(item.localScale.z, Is.EqualTo(expected).Within(0.0001f), $"{label} Z scale is wrong.");
    }
}
