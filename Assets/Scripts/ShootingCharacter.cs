using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class ShootingCharacter : MonoBehaviourPun
{
    public float weaponCooldown = 1f;
    public float projectileImpulse = 50f;

    private Camera cam;
    private int shooterId;
    private bool shootPressed;
    private float weaponCooldownCounter;
    private ProjectileController projectileCtrl;

    // Start is called before the first frame update
    void Start()
    {
        shooterId = gameObject.GetInstanceID();
        shootPressed = false;
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
        shootPressed = Input.GetButton("Fire1");
    }

    void FixedUpdate()
    {
        weaponCooldownCounter -= Time.deltaTime;
        if (weaponCooldownCounter <= 0 && shootPressed)
        {
            weaponCooldownCounter = weaponCooldown;
            Vector3 source = cam.transform.position + cam.transform.forward;
            Vector3 force = cam.transform.forward * projectileImpulse;
            projectileCtrl.BroadcastFireProjectile(source, force, photonView.Owner.UserId);
        }
        if (weaponCooldown < 0f)
            weaponCooldown = 0f;
    }
}
