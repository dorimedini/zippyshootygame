using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FirstPersonController : MonoBehaviour
{

    public float movementSpeed = 25;
    public float jumpSpeed = 12;

    public GameObject projectilePrefab;     // ProjectileRenderer?
    public float projectileImpulse = 50;
    public float weaponCooldown = 0.5f;
    private float weaponCooldownCounter;

    private Rigidbody rb;
    private Camera camera;

    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        camera = GetComponentInChildren<Camera>();
        weaponCooldownCounter = 0f;
    }

    bool grounded() {
        // transform.position - the ground level the player is standing on, if grounded.
        // Check if there's a collider somewhere between 1 meter into the players body
        // and 1.1 meters "downward", where "downward" in global coordinates depends on the
        // player's location on the sphere.
        return GeoPhysics.IsPlayerGrounded(rb);
    }

    // Update is called once per frame
    void Update()
    {
        // Movement
        float forwardSpeed = Input.GetAxis("Vertical");
        float sideSpeed = Input.GetAxis("Horizontal");
        Vector3 moveDirection = new Vector3(sideSpeed, 0, forwardSpeed);
        Vector3 speed = rb.rotation * (movementSpeed * moveDirection);
        rb.MovePosition(rb.position + Time.deltaTime * speed);

        // Gravity
        GeoPhysics.ApplyGravity(rb);

        // Jump
        if (grounded() && Input.GetButtonDown("Jump"))
            rb.velocity = rb.velocity + jumpSpeed * transform.up;

        // Fire
        weaponCooldownCounter -= Time.deltaTime;
        if (weaponCooldownCounter <= 0 && Input.GetButton("Fire1"))
        {
            weaponCooldownCounter = weaponCooldown;
            Vector3 projectileSpawn = camera.transform.position + camera.transform.forward;
            GameObject projectile = (GameObject)Instantiate(projectilePrefab, projectileSpawn, camera.transform.rotation);
            projectile.GetComponent<Rigidbody>().AddForce(camera.transform.forward * projectileImpulse, ForceMode.Impulse);
            projectile.GetComponent<Projectile>().shooterId = gameObject.GetInstanceID();
        }
        if (weaponCooldown < 0f)
            weaponCooldown = 0f;
    }

    void OnCollisionEnter(Collision other)
    {
        GameObject obj = other.gameObject;
        TileBehaviour tile = obj.GetComponent<TileBehaviour>();
        if (tile != null)
        {
            Debug.Log(string.Format("Collided with tile {0}", tile.id));
        }
    }

    void OnCollisionExit(Collision other)
    {
        GameObject obj = other.gameObject;
        TileBehaviour tile = obj.GetComponent<TileBehaviour>();
        if (tile != null)
        {
            Debug.Log(string.Format("Exited collision with tile {0}", tile.id));
        }
    }

}
