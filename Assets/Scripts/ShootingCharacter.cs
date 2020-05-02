using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

[RequireComponent(typeof(CrosshairGUIController))]
[RequireComponent(typeof(Rigidbody))]
public class ShootingCharacter : MonoBehaviourPun
{
    public CrosshairGUIController crosshairCtrl;
    public Camera cam;
    public Rigidbody rb;

    private bool buttonDown, buttonUp, charging;
    private float weaponCooldownCounter, chargeTime;
    private ProjectileController projectileCtrl;

    // Start is called before the first frame update
    void Start()
    {
        charging = false;
        projectileCtrl = GameObject.Find("_GLOBAL_VIEWS").GetComponentInChildren<ProjectileController>();
        if (projectileCtrl == null)
            Debug.LogError("Got null ProjectileController");
    }

    // Update is called once per frame
    void Update()
    {
        buttonDown = Input.GetButtonDown("Fire1");
        buttonUp = Input.GetButtonUp("Fire1");
        weaponCooldownCounter = Mathf.Max(weaponCooldownCounter - Time.deltaTime, 0);

        // Different behaviour depending on weapon mode
        if (UserDefinedConstants.chargeMode)
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
                crosshairCtrl.updateChargeState(chargeTime, UserDefinedConstants.maxChargeTime);
            }

            // Fire (when player releases button or max charge is reached)
            if (charging && (buttonUp || chargeTime >= UserDefinedConstants.maxChargeTime))
            {
                buttonUp = false;
                charging = false;
                FireProjectile(chargeTime);
                crosshairCtrl.updateChargeState(0, UserDefinedConstants.maxChargeTime);
            }

        }
        else // !chargeMode
        {
            // Fire immediately (at "max charge"), if cooldown allows
            if (buttonDown && Tools.NearlyEqual(weaponCooldownCounter, 0, 0.01f))
            {
                weaponCooldownCounter = UserDefinedConstants.weaponCooldown;
                FireProjectile(1);
            }
        }
    }

    void FireProjectile(float charge)
    {
        Vector3 currentShooterSpeed = rb.velocity;
        Vector3 force = charge * cam.transform.forward * UserDefinedConstants.projectileImpulse;
        Vector3 source = cam.transform.position + cam.transform.forward;
        projectileCtrl.BroadcastFireProjectile(source, force, currentShooterSpeed, photonView.Owner.UserId);
    }
}
