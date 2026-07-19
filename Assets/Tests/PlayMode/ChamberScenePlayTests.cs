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
        Assert.That(game.enabled, Is.True, "Saved Chamber references are incomplete.");
        Assert.That(Object.FindAnyObjectByType<Canvas>(), Is.Null, "The art pass should not contain UI.");
        Assert.That(GameObject.Find("The House — Dealer Character"), Is.Null, "The humanoid dealer must be removed.");
        Assert.That(Object.FindObjectsByType<Animator>(FindObjectsInactive.Include), Is.Empty, "The apparition must not depend on a humanoid rig or IK chain.");
        Assert.That(GameObject.Find("Player Hands Rig"), Is.Null);
        Assert.That(GameObject.Find("Player Left Hand"), Is.Null);
        Assert.That(GameObject.Find("Player Right Hand"), Is.Null);

        var apparition = GameObject.Find("The House — Apparition");
        var rightHand = GameObject.Find("Apparition Right Hand").transform;
        var leftHand = GameObject.Find("Apparition Left Hand").transform;
        var face = GameObject.Find("Apparition Face Debug Point").transform;
        Assert.That(apparition, Is.Not.Null);
        Assert.That(rightHand, Is.Not.Null);
        Assert.That(leftHand, Is.Not.Null);
        Assert.That(rightHand.parent, Is.EqualTo(apparition.transform));
        Assert.That(leftHand.parent, Is.EqualTo(apparition.transform));
        Assert.That(apparition.GetComponentsInChildren<Renderer>(true), Has.Length.EqualTo(3), "The entity should be one shroud and two independent hands.");
        Assert.That(apparition.GetComponentInChildren<Animator>(), Is.Null);
        Assert.That(CombinedBounds(apparition).max.y, Is.InRange(1.38f, 1.52f));
        Assert.That(CombinedBounds(apparition).min.y, Is.GreaterThan(0.80f));
        Assert.That(Vector3.Distance(face.position, apparition.transform.position), Is.LessThan(0.001f));
        Assert.That(Object.FindAnyObjectByType<ChamberPoseDebug>(), Is.Not.Null, "Scene-space aim lines are missing.");

        Assert.That(GameObject.Find("Horror Package — Authored Dressing"), Is.Not.Null);
        Assert.That(GameObject.Find("Horror Coffin — Back Right"), Is.Not.Null);
        Assert.That(GameObject.Find("Hanging Body Bag — Back Left"), Is.Not.Null);
        Assert.That(GameObject.Find("Horror Bottles — Evidence Shelf"), Is.Not.Null);
        Assert.That(GameObject.Find("Apparition Red Focus"), Is.Not.Null);
        Assert.That(GameObject.Find("Table Cold Focus"), Is.Not.Null);
        Assert.That(RenderSettings.ambientLight.maxColorComponent, Is.LessThan(0.02f), "The room ambient light is not black enough.");
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
        Assert.That(gunRenderers, Has.Length.GreaterThanOrEqualTo(2));
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

        var shells = 0;
        foreach (var item in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include))
        {
            if (!item.name.StartsWith("Shotgun Shell ")) continue;
            shells++;
            var shellRenderer = item.GetComponentInChildren<Renderer>();
            var shellSize = Vector3.Scale(shellRenderer.localBounds.size, shellRenderer.transform.lossyScale);
            Assert.That(Mathf.Max(shellSize.x, shellSize.y, shellSize.z), Is.InRange(0.055f, 0.075f));
            Assert.That(Mathf.Min(shellSize.x, shellSize.y, shellSize.z), Is.InRange(0.014f, 0.020f));
        }
        Assert.That(shells, Is.EqualTo(6));

        game.StopAllCoroutines();
        Invoke(game, "CompleteOpening");
        var pump = GameObject.Find("Reload").transform;
        var pumpRestPosition = pump.localPosition;
        game.StartCoroutine((IEnumerator)InvokeWithResult(game, "PumpWeapon", 0.40f));
        var maximumPumpTravel = 0f;
        for (var elapsed = 0f; elapsed < 0.46f; elapsed += Time.deltaTime)
        {
            maximumPumpTravel = Mathf.Max(maximumPumpTravel, Vector3.Distance(pumpRestPosition, pump.localPosition));
            yield return null;
        }
        Assert.That(maximumPumpTravel, Is.GreaterThan(0.007f));
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
        var rightRestPosition = rightHand.position;
        var leftRestPosition = leftHand.position;
        game.StartCoroutine((IEnumerator)InvokeWithResult(game, "ResolveShot", false, false));
        yield return new WaitForSeconds(1.48f);
        var rightGrip = GameObject.Find("Apparition Right Grip").transform;
        var leftGrip = GameObject.Find("Apparition Left Grip").transform;
        Assert.That(Vector3.Distance(rightHand.position, rightGrip.position), Is.LessThan(0.001f), "Direct right hand misses the rear grip.");
        Assert.That(Vector3.Distance(leftHand.position, leftGrip.position), Is.LessThan(0.001f), "Direct left hand misses the fore grip.");
        Assert.That(rightHand.parent, Is.EqualTo(rightGrip));
        Assert.That(leftHand.parent, Is.EqualTo(leftGrip));
        var camera = GameObject.Find("Duel Camera").transform;
        Assert.That(Vector3.Angle(muzzle.forward, camera.position - muzzle.position), Is.LessThan(1f), "Apparition aim misses the player camera.");
        yield return new WaitForSeconds(2.5f);
        Assert.That(CurrentRound(game).RemainingTotal, Is.LessThan(6));
        Assert.That(rightHand.parent, Is.EqualTo(apparition.transform));
        Assert.That(leftHand.parent, Is.EqualTo(apparition.transform));
        Assert.That(Vector3.Distance(rightHand.position, rightRestPosition), Is.LessThan(0.025f));
        Assert.That(Vector3.Distance(leftHand.position, leftRestPosition), Is.LessThan(0.025f));

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
        var entityRestScale = apparition.transform.localScale;
        game.StartCoroutine((IEnumerator)InvokeWithResult(game, "AnimateShot", true, false, true));
        var maximumOffset = 0f;
        var maximumTilt = 0f;
        var maximumScaleChange = 0f;
        for (var elapsed = 0f; elapsed < 3.15f; elapsed += Time.deltaTime)
        {
            maximumOffset = Mathf.Max(maximumOffset, Vector3.Distance(entityRestPosition, apparition.transform.localPosition));
            maximumTilt = Mathf.Max(maximumTilt, Vector3.Angle(Vector3.up, apparition.transform.up));
            maximumScaleChange = Mathf.Max(maximumScaleChange, Vector3.Distance(entityRestScale, apparition.transform.localScale));
            yield return null;
        }
        Assert.That(maximumOffset, Is.InRange(0.17f, 0.23f), "Apparition hit reaction is too weak or leaves its authored position.");
        Assert.That(maximumTilt, Is.InRange(16f, 20f));
        Assert.That(maximumScaleChange, Is.GreaterThan(0.15f), "Apparition does not distort on impact.");
        Assert.That(Vector3.Distance(entityRestPosition, apparition.transform.localPosition), Is.LessThan(0.013f), "Apparition did not return to its small idle hover envelope.");
        Debug.Log($"[ApparitionTransformTrace] hitOffset={maximumOffset:F4}m tilt={maximumTilt:F3}deg scaleDelta={maximumScaleChange:F4}");
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
