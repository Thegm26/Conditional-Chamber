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
        Assert.That(GameObject.Find("Duel Table — Imported Furniture Set"), Is.Not.Null);
        Assert.That(GameObject.Find("Imported Walnut Duel Table"), Is.Not.Null);
        Assert.That(GameObject.Find("Imported Shell Side Table"), Is.Not.Null);
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
        Assert.That(gunRenderers, Has.Length.GreaterThanOrEqualTo(3));
        var baseRenderer = System.Array.Find(gunRenderers, renderer => renderer.sharedMaterials.Length == 6);
        Assert.That(baseRenderer, Is.Not.Null, "The authored shotgun body lost its six material slots.");
        foreach (var renderer in gunRenderers)
            foreach (var material in renderer.sharedMaterials) Assert.That(material, Is.Not.Null);
        Assert.That(Mathf.Max(baseRenderer.bounds.size.x, baseRenderer.bounds.size.z), Is.InRange(0.9f, 1.3f), "Shotgun visual scale regressed.");
        Assert.That(Mathf.Abs(playerShotgun.lossyScale.x - playerShotgun.lossyScale.y), Is.LessThan(0.0001f), "Shotgun has distorted non-uniform scale.");
        Assert.That(Mathf.Abs(playerShotgun.lossyScale.y - playerShotgun.lossyScale.z), Is.LessThan(0.0001f), "Shotgun has distorted non-uniform scale.");
        var duelCamera = GameObject.Find("Duel Camera").GetComponent<Camera>();
        var gunBounds = CombinedBounds(playerShotgun.gameObject);
        foreach (var corner in BoundsCorners(gunBounds))
        {
            var viewport = duelCamera.WorldToViewportPoint(corner);
            Assert.That(viewport.z, Is.GreaterThan(duelCamera.nearClipPlane), "Part of the table shotgun is behind the camera near plane.");
            Assert.That(viewport.x, Is.InRange(0.05f, 0.95f), "Part of the table shotgun is outside the horizontal camera frame.");
            Assert.That(viewport.y, Is.InRange(0.05f, 0.95f), "Part of the table shotgun is outside the vertical camera frame.");
        }

        var table = GameObject.Find("Imported Walnut Duel Table");
        var tableBounds = CombinedBounds(table);
        Assert.That(tableBounds.min.y, Is.EqualTo(0f).Within(0.025f), "Table legs do not meet the floor.");
        Assert.That(tableBounds.max.y, Is.InRange(0.80f, 1.05f), "Tabletop height is implausible.");
        Assert.That(tableBounds.size.x, Is.InRange(1.8f, 2.6f));
        Assert.That(tableBounds.size.z, Is.InRange(1.1f, 1.6f));
        Assert.That(gunBounds.min.y - tableBounds.max.y, Is.InRange(0.005f, 0.06f), "Shotgun intersects or floats above the tabletop.");
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
            Assert.That(Mathf.Max(shellRenderer.bounds.size.x, shellRenderer.bounds.size.y, shellRenderer.bounds.size.z),
                Is.LessThan(0.15f), "A shell is implausibly oversized.");
        }
        Assert.That(shells, Is.EqualTo(6));
        Assert.That(GameObject.Find("Side Loading Rack"), Is.Not.Null);

        game.StopAllCoroutines();
        Invoke(game, "CompleteOpening");

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
        yield return new WaitForSeconds(2.05f);
        Assert.That(CurrentRound(game).RemainingTotal, Is.LessThan(6), "Dealer shot did not consume a shell.");

        game.StopAllCoroutines();
        Invoke(game, "StartExperiment");
        game.StopAllCoroutines();
        Invoke(game, "CompleteOpening");
        Assert.That(CurrentRound(game).RemainingTotal, Is.EqualTo(6), "Restart did not restore the chamber.");

        var dealerRootPosition = dealer.transform.localPosition;
        var dealerHitRoutine = (IEnumerator)typeof(ChamberLogicGame).GetMethod("AnimateShot", BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(game, new object[] { true, false, true });
        game.StartCoroutine(dealerHitRoutine);
        var maximumRootOffset = 0f;
        var maximumUpTilt = 0f;
        var minimumDealerTop = float.PositiveInfinity;
        var restDealerCenter = CombinedBounds(dealer).center;
        var maximumRenderedSideShift = 0f;
        for (var elapsed = 0f; elapsed < 2.1f; elapsed += Time.deltaTime)
        {
            maximumRootOffset = Mathf.Max(maximumRootOffset, Vector3.Distance(dealerRootPosition, dealer.transform.localPosition));
            maximumUpTilt = Mathf.Max(maximumUpTilt, Vector3.Angle(Vector3.up, dealer.transform.up));
            var currentBounds = CombinedBounds(dealer);
            minimumDealerTop = Mathf.Min(minimumDealerTop, currentBounds.max.y);
            maximumRenderedSideShift = Mathf.Max(maximumRenderedSideShift, Mathf.Abs(currentBounds.center.x - restDealerCenter.x));
            yield return null;
        }
        Assert.That(maximumRootOffset, Is.LessThan(0.002f), "Dealer root slid out of the chair during the hit.");
        Assert.That(maximumUpTilt, Is.LessThan(0.5f), "Dealer root fell sideways during the hit.");
        Assert.That(minimumDealerTop, Is.GreaterThan(0.9f), "Dealer hit animation collapsed below the tabletop.");
        Assert.That(maximumRenderedSideShift, Is.LessThan(0.35f), "Dealer mesh fell sideways during the hit.");
        Debug.Log($"[DealerTransformTrace] hit maxRootOffset={maximumRootOffset:F4}m maxRootTilt={maximumUpTilt:F3}deg maxMeshSideShift={maximumRenderedSideShift:F3}m minTop={minimumDealerTop:F3}m");

        dealerAnimator.CrossFade("Death", 0f);
        yield return new WaitForSeconds(0.8f);
        var defeatedBounds = CombinedBounds(dealer);
        Assert.That(Vector3.Distance(dealerRootPosition, dealer.transform.localPosition), Is.LessThan(0.002f), "Dealer death moved the character root.");
        Assert.That(Vector3.Angle(Vector3.up, dealer.transform.up), Is.LessThan(0.5f), "Dealer death tilted the root sideways.");
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
}
