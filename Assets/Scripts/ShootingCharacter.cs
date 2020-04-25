using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class ShootingCharacter : MonoBehaviourPun
{
    public CrosshairGUIController crosshairCtrl;

    private Camera cam;
    private bool initiateCharge, releaseCharge, charging;
    private float weaponCooldownCounter, chargeTime;
    private ProjectileController projectileCtrl;

    // Start is called before the first frame update
    void Start()
    {
        charging = false;
        cam = gameObject.GetComponentInChildren<Camera>();
        if (cam == null)
            Debug.LogError("No camera on shooting character!");
        projectileCtrl = GameObject.Find("_GLOBAL_VIEWS").GetComponentInChildren<ProjectileController>();
        if (projectileCtrl == null)
            Debug.LogError("Got null ProjectileController");
    }

    // Update is called once per frame
    void Update()
    {
        initiateCharge = Input.GetButtonDown("Fire1");
        releaseCharge = Input.GetButtonUp("Fire1");
        weaponCooldownCounter -= Time.deltaTime;

        // Start weapon charge if cooldown allows, we're not currently charging (somehow?) and the player initiated charge
        if (weaponCooldownCounter <= 0 && initiateCharge && !charging)
        {
            initiateCharge = false;
            charging = true;
            weaponCooldownCounter = UserDefinedConstants.weaponCooldown;
            chargeTime = 0;
        }

        // Update weapon charge (if charging) and GUI
        if (charging)
        {
            chargeTime += Time.deltaTime;
            crosshairCtrl.updateChargeState(chargeTime, UserDefinedConstants.maxChargeTime);
        }

        // Fire (when player releases button or max charge is reached)
        if (charging && (releaseCharge || chargeTime >= UserDefinedConstants.maxChargeTime))
        {
            releaseCharge = false;
            charging = false;
            Vector3 force = chargeTime * cam.transform.forward * UserDefinedConstants.projectileImpulse;
            Vector3 source = cam.transform.position + cam.transform.forward;
            projectileCtrl.BroadcastFireProjectile(source, force, photonView.Owner.UserId);
            crosshairCtrl.updateChargeState(0, UserDefinedConstants.maxChargeTime);
        }

        weaponCooldownCounter = Mathf.Max(weaponCooldownCounter, 0);
    }
}
