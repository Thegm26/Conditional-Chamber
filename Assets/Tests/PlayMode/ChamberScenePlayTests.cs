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
        var shotgun = GameObject.Find("Player Shotgun").transform;
        var shotgunTablePosition = shotgun.position;
        var shotgunTableRotation = shotgun.rotation;
        var loadingPose = GameObject.Find("Doll Shotgun Loading Pose").transform;
        var loadingPort = GameObject.Find("Shotgun Breech").transform;
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
        Assert.That(Vector3.Distance(firstShell.position, shellStartPosition), Is.LessThan(0.001f),
            "The shells did not remain displayed and stationary during the opening reveal.");
        var maximumOpenTravel = 0f;
        var maximumShellTravel = 0f;
        var minimumHandToShellDistance = float.MaxValue;
        var shellMovedWithoutHand = false;
        var bothHandsTookWeapon = false;
        var shellMovedWithoutGunSupport = false;
        var weaponLiftedByBothHands = false;
        var maximumWeaponLift = 0f;
        var maximumWeaponRotation = 0f;
        var minimumShellToPortDistance = float.MaxValue;
        var handReachedShellBeforeMove = false;
        var shellAxisErrorAtPort = 180f;
        var shellPinchDistanceAtPort = float.MaxValue;
        for (var elapsed = 0f; elapsed < 3.35f; elapsed += Time.deltaTime)
        {
            bothHandsTookWeapon |= loadingHand.parent == rearGrip && supportHand.parent == foreGrip;
            maximumWeaponLift = Mathf.Max(maximumWeaponLift, Vector3.Distance(shotgunTablePosition, shotgun.position));
            maximumWeaponRotation = Mathf.Max(maximumWeaponRotation, Quaternion.Angle(shotgunTableRotation, shotgun.rotation));
            weaponLiftedByBothHands |= Vector3.Distance(shotgunTablePosition, shotgun.position) > 0.20f &&
                                       loadingHand.parent == rearGrip && supportHand.parent == foreGrip;
            maximumOpenTravel = Mathf.Max(maximumOpenTravel, Vector3.Distance(pumpRestPosition, pump.localPosition));
            maximumShellTravel = Mathf.Max(maximumShellTravel, Vector3.Distance(shellStartPosition, firstShell.position));
            if (firstShell.gameObject.activeSelf)
            {
                var shellHasMoved = Vector3.Distance(shellStartPosition, firstShell.position) > 0.003f;
                var pinchDistance = DistanceToSegment(firstShell.position,
                    FindDescendant(loadingHand, "Thumb2").position,
                    FindDescendant(loadingHand, "Index3").position);
                if (!shellHasMoved && pinchDistance < 0.045f) handReachedShellBeforeMove = true;
                var handToShellDistance = Vector3.Distance(loadingHandRenderer.bounds.center, firstShellRenderer.bounds.center);
                minimumHandToShellDistance = Mathf.Min(minimumHandToShellDistance, handToShellDistance);
                var shellToPortDistance = Vector3.Distance(firstShell.position, loadingPort.position);
                if (shellToPortDistance < minimumShellToPortDistance)
                {
                    minimumShellToPortDistance = shellToPortDistance;
                    shellAxisErrorAtPort = Quaternion.Angle(firstShell.rotation, loadingPort.rotation);
                    shellPinchDistanceAtPort = DistanceToSegment(
                        firstShell.position,
                        FindDescendant(loadingHand, "Thumb2").position,
                        FindDescendant(loadingHand, "Index3").position);
                }
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
        Assert.That(handReachedShellBeforeMove, Is.True, "The shell moved before the loading fingers reached it.");
        Assert.That(bothHandsTookWeapon, Is.True, "The doll did not take the shotgun with both hands before opening it.");
        Assert.That(weaponLiftedByBothHands, Is.True, "The doll gripped the shotgun but never picked it up.");
        Assert.That(maximumWeaponLift, Is.GreaterThan(0.30f), "The shotgun barely left the table.");
        Assert.That(maximumWeaponRotation, Is.GreaterThan(45f), "The shotgun translated to loading height without rotating out of its table pose.");
        Assert.That(Vector3.Distance(loadingPose.position, shotgun.position), Is.LessThan(0.03f), "Shotgun missed the authored loading pose.");
        Assert.That(Quaternion.Angle(loadingPose.rotation, shotgun.rotation), Is.LessThan(0.5f), "Shotgun missed the authored loading rotation.");
        Assert.That(minimumShellToPortDistance, Is.LessThan(0.015f), "The shell never entered the shotgun loading port.");
        Assert.That(shellAxisErrorAtPort, Is.LessThan(1f), "The shell entered the breech sideways.");
        Assert.That(shellPinchDistanceAtPort, Is.LessThan(0.03f), "The shell was not held between the loading fingers at the breech.");
        Assert.That(shellMovedWithoutGunSupport, Is.False, "The supporting hand released the shotgun during loading.");

        yield return new WaitForSeconds(13.3f);
        foreach (var item in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include))
            if (item.name.StartsWith("Shotgun Shell ")) Assert.That(item.gameObject.activeSelf, Is.False, $"{item.name} was not loaded.");
        Assert.That(Vector3.Distance(pumpRestPosition, pump.localPosition), Is.LessThan(0.001f), "Shotgun did not close after the opening load.");
        Assert.That(Vector3.Distance(shotgunTablePosition, shotgun.position), Is.LessThan(0.002f), "Shotgun was not lowered back onto the table.");
        Assert.That(Quaternion.Angle(shotgunTableRotation, shotgun.rotation), Is.LessThan(0.1f));
        Assert.That(game.enabled, Is.True);
        Debug.Log($"[OpeningTransformTrace] gunLift={maximumWeaponLift:F3}m gunRotation={maximumWeaponRotation:F1}deg pumpTravel={maximumOpenTravel:F4}m shellTravel={maximumShellTravel:F3}m handGrip={minimumHandToShellDistance:F3}m portError={minimumShellToPortDistance:F3}m shellAxis={shellAxisErrorAtPort:F2}deg pinch={shellPinchDistanceAtPort:F3}m start={shellStartPosition}");
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
        Assert.That(rightHand.GetComponentInChildren<SkinnedMeshRenderer>(), Is.Not.Null, "Right hand is not using the imported finger rig.");
        Assert.That(leftHand.GetComponentInChildren<SkinnedMeshRenderer>(), Is.Not.Null, "Left hand is not using the imported finger rig.");
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
        Assert.That(tableFocus.intensity, Is.GreaterThanOrEqualTo(5.1f), "The shotgun work light is too dim.");
        Assert.That(tableFocus.range, Is.GreaterThanOrEqualTo(4.7f), "The shotgun work light does not reach the whole table.");
        Assert.That(tableFocus.spotAngle, Is.GreaterThanOrEqualTo(74f), "The shotgun work light is too narrow.");
        var softTableFill = GameObject.Find("Soft Table Fill").GetComponent<Light>();
        Assert.That(softTableFill.intensity, Is.GreaterThanOrEqualTo(1.1f), "The hand-loading fill light is too dim.");
        var overhead = GameObject.Find("Flickering Interrogation Light").GetComponent<Light>();
        Assert.That(overhead.intensity, Is.GreaterThan(1.2f), "The runtime flicker blacks out the room too aggressively.");
        Assert.That(RenderSettings.ambientLight.maxColorComponent, Is.LessThan(0.002f), "The room ambient light is not black enough.");
        var mainMusic = (AudioSource)typeof(ChamberLogicGame).GetField("musicSource", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(game);
        var liveShot = (AudioClip)typeof(ChamberLogicGame).GetField("liveShotClip", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(game);
        var blankClick = (AudioClip)typeof(ChamberLogicGame).GetField("blankClickClip", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(game);
        var layeredMusic = GameObject.Find("Horror Music Layer").GetComponent<AudioSource>();
        var dollMusic = GameObject.Find("Doll Music Box").GetComponent<AudioSource>();
        Assert.That(mainMusic.clip.name, Is.EqualTo("Creepy_Ambient_Layer"));
        Assert.That(layeredMusic.clip.name, Is.EqualTo("Abandoned_Passages"));
        Assert.That(dollMusic.clip.name, Is.EqualTo("Doll_Spooky_Waltz"));
        Assert.That(liveShot.name, Is.EqualTo("Shotgun_Boom"));
        Assert.That(liveShot.length, Is.InRange(1.4f, 1.7f), "Live fire is not using the full shotgun report asset.");
        Assert.That(liveShot.channels, Is.EqualTo(2));
        Assert.That(blankClick.name, Is.EqualTo("Shotgun_Dry_Click"));
        Assert.That(blankClick.length, Is.InRange(0.3f, 0.4f), "Blank fire is not using the short mechanical click asset.");
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
        Assert.That(CombinedBounds(rightHand.gameObject).max.y, Is.LessThan(tableBounds.max.y - 0.04f), "Right hand should wait hidden below the table.");
        Assert.That(CombinedBounds(leftHand.gameObject).max.y, Is.LessThan(tableBounds.max.y - 0.04f), "Left hand should wait hidden below the table.");
        Assert.That(tableBounds.min.y, Is.EqualTo(0f).Within(0.002f));
        Assert.That(tableBounds.max.y, Is.InRange(0.82f, 0.90f));
        Assert.That(tableBounds.size.x, Is.InRange(1.65f, 1.71f));
        Assert.That(tableBounds.size.z, Is.InRange(1.14f, 1.20f));
        Assert.That(gunBounds.min.y - tableBounds.max.y, Is.InRange(0.005f, 0.06f), "Shotgun intersects or floats above the table.");
        Assert.That(gunBounds.center.x, Is.EqualTo(tableBounds.center.x).Within(0.002f));
        Assert.That(gunBounds.center.z, Is.EqualTo(tableBounds.center.z).Within(0.002f));
        Assert.That(gunBounds.size.z, Is.InRange(0.82f, 0.86f));
        Assert.That(gunBounds.size.y, Is.InRange(0.045f, 0.075f), "Shotgun is standing on its edge instead of lying flat.");
        Assert.That(gunBounds.size.x, Is.InRange(0.14f, 0.18f), "Shotgun side profile was not rotated onto the table.");
        Assert.That(Quaternion.Angle(GameObject.Find("Weapon Table Anchor").transform.rotation, Quaternion.Euler(0f, 0f, 90f)), Is.LessThan(0.1f));
        Debug.Log($"[TableGunTrace] clearance={gunBounds.min.y - tableBounds.max.y:F3}m height={gunBounds.size.y:F3}m width={gunBounds.size.x:F3}m length={gunBounds.size.z:F3}m");

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
        var rightOpenFingerSpan = Vector3.Distance(FindDescendant(rightHand, "Index3").position, FindDescendant(rightHand, "Hand").position);
        var leftOpenFingerSpan = Vector3.Distance(FindDescendant(leftHand, "Middle3").position, FindDescendant(leftHand, "Hand").position);
        var weaponRotationBeforeAim = shotgun.rotation;
        var dealerAimRotation = GameObject.Find("Dealer Hand Grip — Player").transform.rotation;
        game.StartCoroutine((IEnumerator)InvokeWithResult(game, "ResolveShot", false, false));
        var maximumAimRotation = 0f;
        for (var elapsed = 0f; elapsed < 1.48f; elapsed += Time.deltaTime)
        {
            var rotationFromTable = Quaternion.Angle(weaponRotationBeforeAim, shotgun.rotation);
            maximumAimRotation = Mathf.Max(maximumAimRotation, rotationFromTable);
            yield return null;
        }
        var rightGrip = GameObject.Find("Apparition Right Grip").transform;
        var leftGrip = GameObject.Find("Apparition Left Grip").transform;
        Assert.That(Vector3.Distance(rightHand.position, rightGrip.position), Is.LessThan(0.001f), "Direct right hand misses the rear grip.");
        Assert.That(Vector3.Distance(leftHand.position, leftGrip.position), Is.LessThan(0.001f), "Direct left hand misses the fore grip.");
        Assert.That(rightHand.parent, Is.EqualTo(rightGrip));
        Assert.That(leftHand.parent, Is.EqualTo(leftGrip));
        Assert.That(maximumAimRotation, Is.GreaterThan(45f), "Dealer shotgun did not rotate out of the table pose.");
        Assert.That(Quaternion.Angle(shotgun.rotation, dealerAimRotation), Is.LessThan(0.5f), "Dealer shotgun missed its final aiming rotation.");
        Assert.That(Quaternion.Angle(Quaternion.identity, FindDescendant(rightHand, "Index2").localRotation), Is.GreaterThan(42f), "Right fingers did not curl around the rear grip.");
        Assert.That(Quaternion.Angle(Quaternion.identity, FindDescendant(leftHand, "Middle2").localRotation), Is.GreaterThan(42f), "Left fingers did not curl around the fore grip.");
        Assert.That(Vector3.Distance(FindDescendant(rightHand, "Index3").position, FindDescendant(rightHand, "Hand").position), Is.LessThan(rightOpenFingerSpan * 0.86f), "Right index finger curled away from the palm.");
        Assert.That(Vector3.Distance(FindDescendant(leftHand, "Middle3").position, FindDescendant(leftHand, "Hand").position), Is.LessThan(leftOpenFingerSpan * 0.86f), "Left middle finger curled away from the palm.");
        AssertHandWrapsGrip("dealer-player-right", shotgun, GameObject.Find("Shotgun Rear Grip Debug Point").transform, rightHand);
        AssertHandWrapsGrip("dealer-player-left", shotgun, GameObject.Find("Shotgun Fore Grip Debug Point").transform, leftHand);
        Debug.Log($"[HandGripTrace] gunRotation={maximumAimRotation:F1}deg rightSpan={Vector3.Distance(FindDescendant(rightHand, "Index3").position, FindDescendant(rightHand, "Hand").position):F3}/{rightOpenFingerSpan:F3}m leftSpan={Vector3.Distance(FindDescendant(leftHand, "Middle3").position, FindDescendant(leftHand, "Hand").position):F3}/{leftOpenFingerSpan:F3}m");
        var rearHandAxisError = Mathf.Min(Vector3.Angle(rightHand.up, muzzle.forward), Vector3.Angle(-rightHand.up, muzzle.forward));
        Assert.That(rearHandAxisError, Is.LessThan(12f), "Rear hand is not aligned along the shotgun stock.");
        Assert.That(Vector3.Angle(leftHand.up, muzzle.forward), Is.InRange(75f, 105f), "Fore hand is not wrapped across the fore-end.");
        var camera = GameObject.Find("Duel Camera").transform;
        Assert.That(Vector3.Angle(muzzle.forward, camera.position - muzzle.position), Is.LessThan(1f), "Apparition aim misses the player camera.");
        Assert.That(muzzle.position.x, Is.LessThan(-0.35f), "Player-facing shotgun is not held to the doll's side.");
        Assert.That(muzzle.position.y, Is.InRange(0.97f, 1.07f), "Player-facing shotgun is not held low enough.");
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
        AssertHandWrapsGrip("dealer-self-right", shotgun, GameObject.Find("Shotgun Self Right Contact").transform, rightHand);
        AssertHandWrapsGrip("dealer-self-left", shotgun, GameObject.Find("Shotgun Self Left Contact").transform, leftHand);
        Assert.That(muzzle.position.x - face.position.x, Is.InRange(0.29f, 0.35f), "Self-aim muzzle is not beside the doll's head.");
        Assert.That(Mathf.Abs(muzzle.position.y - face.position.y), Is.LessThan(0.025f));
        Assert.That(Mathf.Abs(muzzle.position.z - face.position.z), Is.LessThan(0.04f));
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
        var maximumDownwardOffset = 0f;
        var downHoldTime = 0f;
        var voicePlayed = false;
        var voiceSource = GameObject.Find("Doll Voice Source").GetComponent<AudioSource>();
        for (var elapsed = 0f; elapsed < 6.1f; elapsed += Time.deltaTime)
        {
            maximumOffset = Mathf.Max(maximumOffset, Vector3.Distance(entityRestPosition, apparition.transform.localPosition));
            maximumTilt = Mathf.Max(maximumTilt, Vector3.Angle(Vector3.up, apparition.transform.up));
            maximumBackwardOffset = Mathf.Max(maximumBackwardOffset, apparition.transform.localPosition.z - entityRestPosition.z);
            maximumLateralOffset = Mathf.Max(maximumLateralOffset, Mathf.Abs(apparition.transform.localPosition.x - entityRestPosition.x));
            maximumDownwardOffset = Mathf.Max(maximumDownwardOffset, entityRestPosition.y - apparition.transform.localPosition.y);
            if (entityRestPosition.y - apparition.transform.localPosition.y > 0.28f) downHoldTime += Time.deltaTime;
            voicePlayed |= voiceSource.isPlaying;
            yield return null;
        }
        Assert.That(maximumOffset, Is.InRange(0.31f, 0.34f), "The player-shot doll does not drop far enough.");
        Assert.That(maximumDownwardOffset, Is.GreaterThan(0.30f));
        Assert.That(maximumTilt, Is.LessThan(15f), "The player-shot doll swings instead of dropping.");
        Assert.That(maximumBackwardOffset, Is.InRange(0.05f, 0.08f));
        Assert.That(maximumLateralOffset, Is.LessThan(0.03f));
        Assert.That(downHoldTime, Is.InRange(2.35f, 2.9f), "The doll does not remain down for roughly 2.5 seconds.");
        Assert.That(voicePlayed, Is.True, "The doll did not vocalize when shot.");
        Assert.That(Vector3.Distance(entityRestPosition, apparition.transform.localPosition), Is.LessThan(0.002f), "The surviving doll did not return to its fixed seat.");

        ResetRound(game);
        game.StartCoroutine((IEnumerator)InvokeWithResult(game, "AnimateShot", false, true, true));
        var maximumSideOffset = 0f;
        var selfShotBackwardOffset = 0f;
        var selfShotTilt = 0f;
        var selfShotDownwardOffset = 0f;
        var selfDownHoldTime = 0f;
        for (var elapsed = 0f; elapsed < 7.25f; elapsed += Time.deltaTime)
        {
            maximumSideOffset = Mathf.Max(maximumSideOffset, Mathf.Abs(apparition.transform.localPosition.x - entityRestPosition.x));
            selfShotBackwardOffset = Mathf.Max(selfShotBackwardOffset, apparition.transform.localPosition.z - entityRestPosition.z);
            selfShotTilt = Mathf.Max(selfShotTilt, Vector3.Angle(Vector3.up, apparition.transform.up));
            selfShotDownwardOffset = Mathf.Max(selfShotDownwardOffset, entityRestPosition.y - apparition.transform.localPosition.y);
            if (entityRestPosition.y - apparition.transform.localPosition.y > 0.30f) selfDownHoldTime += Time.deltaTime;
            yield return null;
        }
        Assert.That(maximumSideOffset, Is.InRange(0.07f, 0.10f));
        Assert.That(selfShotBackwardOffset, Is.LessThan(0.04f));
        Assert.That(selfShotDownwardOffset, Is.GreaterThan(0.32f));
        Assert.That(selfShotTilt, Is.LessThan(18f), "The self-shot doll swings instead of dropping.");
        Assert.That(selfDownHoldTime, Is.InRange(2.35f, 2.9f));
        Assert.That(Vector3.Distance(entityRestPosition, apparition.transform.localPosition), Is.LessThan(0.002f));
        Debug.Log($"[DollTransformTrace] playerDown={maximumDownwardOffset:F3}m playerTilt={maximumTilt:F2}deg playerHold={downHoldTime:F2}s selfDown={selfShotDownwardOffset:F3}m selfTilt={selfShotTilt:F2}deg selfHold={selfDownHoldTime:F2}s");
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

    private static Transform FindDescendant(Transform root, string name)
    {
        foreach (var item in root.GetComponentsInChildren<Transform>(true))
            if (item.name == name) return item;
        return null;
    }

    private static float DistanceToSegment(Vector3 point, Vector3 start, Vector3 end)
    {
        var segment = end - start;
        var progress = Mathf.Clamp01(Vector3.Dot(point - start, segment) / segment.sqrMagnitude);
        return Vector3.Distance(point, start + segment * progress);
    }

    private static void AssertHandWrapsGrip(string label, Transform weapon, Transform contact, Transform hand)
    {
        var palm = FindDescendant(hand, "Hand");
        var thumb = FindDescendant(hand, "Thumb2");
        var fingertips = new[]
        {
            FindDescendant(hand, "Index3"),
            FindDescendant(hand, "Middle3"),
            FindDescendant(hand, "Ring3"),
            FindDescendant(hand, "Little3")
        };
        Assert.That(palm, Is.Not.Null, $"{label}: palm bone is missing.");
        Assert.That(thumb, Is.Not.Null, $"{label}: thumb bone is missing.");
        Assert.That(fingertips, Has.None.Null, $"{label}: a fingertip bone is missing.");

        var palmDistance = Vector3.Distance(palm.position, contact.position);
        var nearestFingerDistance = float.MaxValue;
        var nearestWrapDistance = float.MaxValue;
        var furthestFingerDistance = 0f;
        foreach (var fingertip in fingertips)
        {
            var fingerDistance = Vector3.Distance(fingertip.position, contact.position);
            nearestFingerDistance = Mathf.Min(nearestFingerDistance, fingerDistance);
            furthestFingerDistance = Mathf.Max(furthestFingerDistance, fingerDistance);
            nearestWrapDistance = Mathf.Min(nearestWrapDistance,
                DistanceToSegment(contact.position, thumb.position, fingertip.position));
        }

        Assert.That(palmDistance, Is.LessThan(0.075f), $"{label}: palm misses the authored grip center.");
        Assert.That(nearestFingerDistance, Is.LessThan(0.055f), $"{label}: no fingertip reaches the grip.");
        Assert.That(furthestFingerDistance, Is.LessThan(0.10f), $"{label}: fingers float away from the weapon.");
        Assert.That(nearestWrapDistance, Is.LessThan(0.055f), $"{label}: grip center is not enclosed between thumb and fingers.");
        Debug.Log($"[GripGeometry] {label} contact={weapon.InverseTransformPoint(contact.position)} palmDistance={palmDistance:F3}m nearestFinger={nearestFingerDistance:F3}m furthestFinger={furthestFingerDistance:F3}m wrapError={nearestWrapDistance:F3}m");
    }
}
