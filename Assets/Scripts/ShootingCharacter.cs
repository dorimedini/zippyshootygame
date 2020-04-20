using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class ShootingCharacter : MonoBehaviourPun
{
    public float weaponCooldown = 1f;
    public float projectileImpulse = 50f;
    public GameObject projectilePrefab;

    private Camera cam;
    private int shooterId;
    private bool shootPressed;
    private float weaponCooldownCounter;

    // Start is called before the first frame update
    void Start()
    {
        shooterId = gameObject.GetInstanceID();
        shootPressed = false;
        cam = gameObject.GetComponentInChildren<Camera>();
        if (cam == null)
            Debug.LogError("No camera on shooting character!");
        if (projectilePrefab == null)
            Debug.LogError("No projectile prefab provided for shooting character!");
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
            photonView.RPC("FireProjectile", RpcTarget.All, source, force, shooterId);
        }
        if (weaponCooldown < 0f)
            weaponCooldown = 0f;
    }

    /** We don't want to send each projectile's location updates on the network, so let's hope initial spawn location 
     *  and force vector is good enough for syncing */
    [PunRPC]
    public void FireProjectile(Vector3 source, Vector3 force, int shooterId)
    {
        GameObject projectile = Instantiate(projectilePrefab, source, Quaternion.identity);
        projectile.GetComponent<Rigidbody>().AddForce(force, ForceMode.Impulse);
        projectile.GetComponent<Projectile>().shooterId = shooterId;
        if (photonView.IsMine)
        {
            projectile.GetComponent<MeshCollider>().enabled = true;
        }
    }
}
