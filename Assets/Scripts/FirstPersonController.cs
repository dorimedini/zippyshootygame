﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FirstPersonController : MonoBehaviour
{

    public float movementSpeed;
    public float lookSpeedX;
    public float lookSpeedY;
    public float jumpSpeed;

    public GameObject projectilePrefab;
    public float projectileImpulse;
    public float weaponCooldown;
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
        return Physics.Raycast(transform.position + transform.up, -transform.up, 1.1f);
    }

    // Update is called once per frame
    void Update()
    {
        // Rotation
        float rotX = Input.GetAxis("Mouse X");
        float rotY = -Input.GetAxis("Mouse Y");
        transform.rotation = rb.rotation * Quaternion.Euler(0, lookSpeedX * rotX, 0);
        // The "forward" direction needs to deviate from the natural XZ plane when walking inside
        // the sphere.
        // Project the current forward direction on the plane perpendicular to player's position
        // and call LookAt to orientate the player so his head is towards the origin.
        // LookAt needs a target to look at, and the "up" direction.
        Vector3 newForward = Vector3.ProjectOnPlane(transform.forward, -transform.position.normalized);
        transform.LookAt(transform.position + newForward, -transform.position);
        // Rotate the camera depending on Y axis input.
        camera.transform.Rotate(lookSpeedY * rotY, 0, 0);
        // If we look too far up: the camera's "up" direction will form an angle of over 180 degrees with
        // the player's forward direction. We can check this by checking SignedAngle from player's forward
        // to camera's up. If this angle is negative and less than -90 degrees, we leaned too far back.
        // If this angle is negative and more than -90, then we leaned too far forward.
        // To fix this, split into two cases: if the player is leaning back, we set the camera to look at
        // the origin with up towards the negative player forward direction.
        // If the player is leaning forward, look away from origin with up towards the player's forward.
        float angle = Vector3.SignedAngle(camera.transform.up, transform.forward, transform.right);
        if (angle < 0)
        {
            if (angle < -90)
                camera.transform.LookAt(Vector3.zero, -transform.forward);
            else
                camera.transform.LookAt(2 * transform.position, transform.forward);
        }

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
