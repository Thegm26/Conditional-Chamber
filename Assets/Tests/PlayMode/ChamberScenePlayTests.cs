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
        Assert.That(GameObject.Find("Left Security Watcher"), Is.Not.Null);
        Assert.That(GameObject.Find("Right Security Watcher"), Is.Not.Null);
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
        Assert.That(baseRenderer.bounds.size.z, Is.InRange(0.9f, 1.3f), "Shotgun visual scale regressed.");

        var dealer = GameObject.Find("The House — Dealer Character");
        var dealerAnimator = dealer.GetComponentInChildren<Animator>();
        Assert.That(dealerAnimator, Is.Not.Null, "Dealer Animator is missing.");
        Assert.That(dealerAnimator.runtimeAnimatorController, Is.Not.Null, "Dealer animation controller is missing.");
        Assert.That(Object.FindObjectsByType<Animator>(FindObjectsInactive.Include), Has.Length.EqualTo(3), "Exactly three character Animators must be saved in the scene.");
        var musicSource = (AudioSource)typeof(ChamberLogicGame).GetField("musicSource", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(game);
        Assert.That(musicSource, Is.Not.Null, "The saved scene lost its horror-music source reference.");
        Assert.That(musicSource.isPlaying, Is.True, "The horror score did not begin playing.");
        var animatedArm = System.Array.Find(dealer.GetComponentsInChildren<Transform>(true), item => item.name == "UpperArm.L");
        Assert.That(animatedArm, Is.Not.Null, "Dealer animation rig is incomplete.");
        var armBefore = animatedArm.localRotation;
        yield return new WaitForSeconds(0.55f);
        Assert.That(Quaternion.Angle(armBefore, animatedArm.localRotation), Is.GreaterThan(0.05f), "Dealer idle animation is not advancing.");

        var dealerBounds = CombinedBounds(dealer);
        Assert.That(dealerBounds.max.y, Is.LessThan(2f), "Dealer is too high in the scene.");
        Assert.That(dealerBounds.center.y, Is.LessThan(1.2f));

        var targetBounds = CombinedBounds(GameObject.Find("Range Target"));
        Assert.That(targetBounds.size.x, Is.GreaterThan(1f), "Target is too small or edge-on.");
        Assert.That(targetBounds.size.y, Is.GreaterThan(1f));
        Assert.That(targetBounds.size.z, Is.LessThan(0.6f));

        var shells = 0;
        foreach (var item in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include))
            if (item.name.StartsWith("Shotgun Shell ")) shells++;
        Assert.That(shells, Is.EqualTo(6));
        Assert.That(GameObject.Find("Recessed Shell Inspection Tray"), Is.Not.Null);

        game.StopAllCoroutines();
        Invoke(game, "CompleteOpening");

        Invoke(game, "ChooseDealer");
        yield return new WaitForSeconds(1.4f);
        Assert.That(CurrentRound(game).RemainingTotal, Is.LessThan(6), "House-target shot did not consume a shell.");

        game.StopAllCoroutines();
        Invoke(game, "StartExperiment");
        game.StopAllCoroutines();
        Invoke(game, "CompleteOpening");
        Assert.That(CurrentRound(game).RemainingTotal, Is.EqualTo(6));

        Invoke(game, "ChooseSelf");
        yield return new WaitForSeconds(1.4f);
        Assert.That(CurrentRound(game).RemainingTotal, Is.LessThan(6), "Self-target shot did not consume a shell.");

        game.StopAllCoroutines();
        Invoke(game, "StartExperiment");
        game.StopAllCoroutines();
        Invoke(game, "CompleteOpening");
        Assert.That(CurrentRound(game).RemainingTotal, Is.EqualTo(6), "Restart did not restore the chamber.");
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
}
