using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

[RequireComponent(typeof(Rigidbody))]
public class ShootingCharacter : MonoBehaviourPun, Pausable
{
    public PlayerUIController ui;
    public Camera cam;
    public Rigidbody rb;

    private bool buttonDown, buttonUp, charging;
    private float weaponCooldownCounter, chargeTime;
    private ProjectileController projectileCtrl;

    private bool paused;

    // Start is called before the first frame update
    void Start()
    {
        paused = charging = false;
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

    void UpdateInstafire()
    {
        // Fire immediately (at "max charge"), if cooldown allows.
        // TODO: If we switched from weapon charge mode to normal mode we need to update the crosshair GUI like this somewhere...
        ui.crosshair.updateChargeState(0, UserDefinedConstants.maxChargeTime);
        if (buttonDown && Tools.NearlyEqual(weaponCooldownCounter, 0, 0.01f))
        {
            weaponCooldownCounter = UserDefinedConstants.weaponCooldown;
            FireProjectile(1);
        }
    }

    void FireProjectile(float charge)
    {
        Vector3 currentShooterSpeed = rb.velocity;
        Vector3 force = charge * cam.transform.forward * UserDefinedConstants.projectileImpulse;
        Vector3 source = cam.transform.position + cam.transform.forward;
        projectileCtrl.BroadcastFireProjectile(source, force, currentShooterSpeed, photonView.Owner.UserId);
    }

    public void Pause(bool pause) { paused = pause; }
}
