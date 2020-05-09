using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

[RequireComponent(typeof(Rigidbody))]
public class ShootingCharacter : MonoBehaviourPun, Pausable
{
    public PlayerUIController ui;
    public Camera cam;
    public Rigidbody rb;
    public LockingTargetImageBehaviour lockingImageCtrl;
    public RectTransform uiCanvas;

    private bool buttonDown, buttonUp, buttonPressed, charging;
    private float weaponCooldownCounter, chargeTime;
    private ProjectileController projectileCtrl;

    private bool paused;

    private TargetableCharacter targetedCharacter;
    private float currentSharpestTargetAngle;
    private bool lockedOnTarget;

    // Start is called before the first frame update
    void Start()
    {
        lockedOnTarget = paused = charging = false;
        targetedCharacter = null;
        projectileCtrl = GameObject.Find("_GLOBAL_VIEWS").GetComponentInChildren<ProjectileController>();
        if (projectileCtrl == null)
            Debug.LogError("Got null ProjectileController");
    }

    // Update is called once per frame
    void Update()
    {
        if (!paused)
        {
            buttonDown = Input.GetButtonDown("Fire1");
            buttonPressed = Input.GetButton("Fire1");
            buttonUp = Input.GetButtonUp("Fire1");
        }
        weaponCooldownCounter = Mathf.Max(weaponCooldownCounter - Time.deltaTime, 0);

        if (paused)
            return;

        // Different behaviour depending on weapon mode
        if (UserDefinedConstants.chargeMode)
        {
            UpdateChargefire();
        }
        else if (UserDefinedConstants.weaponLockMode)
        {
            UpdateLockFire();
        }
        else // !chargeMode
        {
            UpdateInstafire();
        }
    }

    void UpdateChargefire()
    {
        // Start weapon charge if cooldown allows, we're not currently charging (somehow?) and the player initiated charge
        if (Tools.NearlyEqual(weaponCooldownCounter, 0, 0.01f) && buttonDown && !charging)
        {
            buttonDown = false;
            charging = true;
            weaponCooldownCounter = UserDefinedConstants.weaponCooldown;
            chargeTime = UserDefinedConstants.minProjectileCharge;
        }

        // Update weapon charge (if charging) and GUI
        if (charging)
        {
            chargeTime += Time.deltaTime;
            ui.crosshair.updateChargeState(chargeTime, UserDefinedConstants.maxChargeTime);
        }

        // Fire (when player releases button or max charge is reached)
        if (charging && (buttonUp || chargeTime >= UserDefinedConstants.maxChargeTime))
        {
            buttonUp = false;
            charging = false;
            FireProjectile(chargeTime);
            ui.crosshair.updateChargeState(0, UserDefinedConstants.maxChargeTime);
        }
    }

    void UpdateLockFire()
    {
        // Initial lock-on check
        if (buttonDown)
        {
            if (targetedCharacter != null)
            {
                Debug.LogWarning("Got fire-button-down but lock-target is already set, did we miss a fire-button-up event?");
                targetedCharacter = null;
                lockedOnTarget = false;
            }
            // I'm not a performance expert but it may be best to just iterate over all players and see who's in scope.
            // Any targetable object will have an angle less than 1+maxAngle anyway, so this is like setting it to infinity:
            currentSharpestTargetAngle = 1 + MaxAngleToBeTargeted();
            foreach (Player player in PhotonNetwork.PlayerList)
            {
                if (NetworkCharacter.IsLocalPlayer(player) || !NetworkCharacter.IsPlayerAlive(player))
                    continue;
                TargetableCharacter playerTarget = NetworkCharacter.GetPlayerGameObject(player).GetComponent<TargetableCharacter>();
                SwitchToTargetIfCloserToCenter(playerTarget);
            }

            if (targetedCharacter != null)
            {
                StartTargeting(targetedCharacter);
            }
        }

        // Stop targeting if:
        // 1. We are currently targeting and not locked on yet
        // 2. Either the player can no longer be targeted, or we're not pressing the fire button anymore
        if (targetedCharacter != null && !lockedOnTarget && (!CanBeTargeted(targetedCharacter) || !buttonPressed))
            StopTargeting(targetedCharacter);

        // When targeting completes it's handled in the Action passed to StartTargeting

        // Fire on button up!
        if (buttonUp)
        {
            if (lockedOnTarget)
            {
                FireSeekingProjectile();
            }
            else
            {
                FireProjectileMaxImpulse();
            }
            if (targetedCharacter != null)
                StopTargeting(targetedCharacter);
        }
        // TODO: Implement
        // Basic idea:
        // 1. Need to choose a button for a hold-to-lock, or passive locking (player in sights-->commence locking).
        //    A button is uncomfortable (or is it?), but passive locking can be buggy and can't always make the right decision:
        //    what if player A has player B in his sights, and player C also comes into A's vision and A decides he wants to start
        //    locking on C. He would need to jerk the cursor aside and carefully re-place the crosshair on player C, wasting 
        //    valuable lock time.
        //    Maybe set this as a boolean option?
        //    Also, maybe the 'lock' button can be just to press and hold the 'fire' button; if released before lockTime is over
        //    it just fires in a straight line, otherwise it's heat-seeking.
        // 2. Change the cursor graphic to a + sign, make it smaller. Perhaps 3px yellow with 1px black outline, with alpha channel
        //    on everything? Also need an indication of the lock-on area, perhaps another yellow-black circle, depending on a new
        //    float constant 'lockSightRadius'
        // 3. Introduce a 'lockTime' float value determining how long it takes to lock on. If player A has player B in his sights
        //    when Fire key is pressed, locking commences and continues until either a. player A releases the fire button or b.
        //    lockTime seconds pass. During the locking phase, show box graphics with three or four descending sizes, located at
        //    player B's location. If locking completed, show a flashing yellow-red square around player B until the fire button
        //    is released. Player A can release the fire button to fire a heat-seeking projectile in this case; if the fire button
        //    was released before the lockTime seconds passed, a regular projectile is fired.
        // 4. On fire button release, revert the graphics to normal
        // 5. Default crosshair should have a randomly-floating square, similar in color to the locking-phase squares, to indicate
        //    locking mechanism is idle.
    }

    void SwitchToTargetIfCloserToCenter(TargetableCharacter candidate)
    {
        float targetSightAngle = TargetSightAngle(candidate.centerTransform.position);
        bool canBeTargeted = CanBeTargeted(targetSightAngle);
        // If several enemy players are in scope, choose the player closest to the center of the scope.
        if (canBeTargeted && targetSightAngle < currentSharpestTargetAngle)
        {
            targetedCharacter = candidate;
            currentSharpestTargetAngle = targetSightAngle;
        }
    }
    float TargetSightAngle(Vector3 target)
    {
        // To check if player 2 is in player 1's scope, we ensure the angle between p1's line of sight and p2's location is bounded above
        // by a specific constant, depending on lock scope radius.
        return Vector3.Angle(cam.transform.forward, target - cam.transform.position);
    }

    float MaxAngleToBeTargeted()
    {
        // Say p1 wants to target another player:
        //
        //                                        p3
        //      lock scope                    ______------
        //          ^             ______------  \
        //            ______------               | max angle A
        //   p1 --- O ______    -    -    -    - +  -    -
        //                  ------______
        //                              ------______    p2
        //                                          ------
        //
        // In the above illustration, p1 sees p3 but not within his lock scope, so p1 cannot target p3. p2, on the other hand,
        // is within the scope.
        // This can be checked by computing the angle A, and then checking if the angle between p1 and the target is at most A.
        // To compute A, we only need to know the scope radius of p1:
        //
        //           <--------1------->/\    ^
        //                             ||    |  Scope radius
        // p1 camera    -    -    -    ||    v
        //                             ||
        //                             \/
        //                    scope circle (side view)
        //
        //             angle = arctan(scope radius / 1)
        //
        // The actual world-space is proportional to lockScopeRadius as defined by the user, but not equal exacly to the value.
        // This actually depends on how "far away" the scope circle is from the camera, but this distance doesn't actually exist
        // as the scope circle is a UI element.
        // By manual testing, if we arbitrarily set the "distance of the scope from camera" to 1 (as in the above illustration),
        // we get A=arctan(radius) and the radius can be computed by:
        float radius = 0.17f * UserDefinedConstants.lockScopeRadius;
        return Mathf.Rad2Deg * Mathf.Atan(radius);
    }
    bool CanBeTargeted(TargetableCharacter target) { return CanBeTargeted(target.centerTransform.position); }
    bool CanBeTargeted(Vector3 target) { return CanBeTargeted(TargetSightAngle(target)); }
    bool CanBeTargeted(float targetSightAngle)
    {
        // TODO: Also check if there's nothing blocking the way! Make sure a raycast (NOT raycastall) from player to target hits the target
        return targetSightAngle <= MaxAngleToBeTargeted();
    }
    void StartTargeting(TargetableCharacter targetChar)
    {
        // TODO: Stop idle-floaty-square image in the crosshair when targetting
        // Inform remote player he's being targeted
        if (targetChar == null)
        {
            Debug.LogError("Target character has no TargetableCharacter component!");
            return;
        }
        targetChar.BroadcastBecameTargeted(UserId());
        lockingImageCtrl.StartTargeting(targetChar.centerTransform, () =>
        {
            targetChar.BroadcastBecameLockedOn(UserId());
            lockedOnTarget = true;
        });
    }
    void StopTargeting(TargetableCharacter targetChar)
    {
        // TODO: Reactivate idle-floaty-square image in the crosshair
        lockingImageCtrl.StopTargeting();
        targetedCharacter = null;
        lockedOnTarget = false;
        if (targetChar == null)
        {
            Debug.LogError("Target character has no TargetableCharacter component (how did we START targeting this guy?)");
            return;
        }
        targetChar.BroadcastBecameUntargeted(UserId());
    }

    void UpdateInstafire()
    {
        // Fire immediately (at "max charge"), if cooldown allows.
        // TODO: If we switched from weapon charge mode to normal mode we need to update the crosshair GUI like this somewhere...
        ui.crosshair.updateChargeState(0, UserDefinedConstants.maxChargeTime);
        if (buttonDown && Tools.NearlyEqual(weaponCooldownCounter, 0, 0.01f))
        {
            weaponCooldownCounter = UserDefinedConstants.weaponCooldown;
            FireProjectileMaxImpulse();
        }
    }

    void FireProjectileMaxImpulse() { FireProjectile(1); }

    void FireProjectile(float charge)
    {
        Vector3 force = charge * cam.transform.forward * UserDefinedConstants.projectileImpulse;
        projectileCtrl.BroadcastFireProjectile(ProjectileSource(), force, rb.velocity, UserId());
    }

    void FireSeekingProjectile()
    {
        // FIXME: A seeking projectile may need to be very different from a normal projectile when syncing.
        // If each player is allowed to spawn his own copy of the graphics and only the shooter is responsible for
        // collision detection this may hurt the way players feel when evading missiles; the graphic you see may not
        // be the real location of what you're running from.
        // On the other hand, there may be many seeking projectiles in the air at once, so syncing all of their photon
        // views may be infeasble.
        // Start with graphics for all and collider for shooter, as with regular projectiles; the behaviour should be
        // to rotate towards target every frame, and every frame apply constant acceleration (we'll hit something
        // eventually, I think, so that's enough).
        // Note that a seeking projectile shouldn't take the current player speed into account
        Vector3 force = cam.transform.forward * UserDefinedConstants.projectileImpulse;
        projectileCtrl.BroadcastFireSeekingProjectile(ProjectileSource(), force, UserId(), targetedCharacter.photonView.Owner.UserId);
    }

    Vector3 ProjectileSource() { return cam.transform.position + cam.transform.forward; }

    public void Pause(bool pause) { paused = pause; }

    string UserId() { return photonView.Owner.UserId; }
}
