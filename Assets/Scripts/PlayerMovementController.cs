﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Animator))]
public class PlayerMovementController : MonoBehaviour
{
    public float animationSpeed = 2;
    public float airMovementSpeed = 5;
    public float jumpSpeed = 8;
    public float minimalAirtime = 0.5f; // Don't check for grounded state too soon into the jump

    private Animator anim;
    private Rigidbody rb;
    private Camera cam;
    private float fwdBack;
    private float leftRight;
    private Vector3 movement;
    private float distFromGround;
    private bool inAir;
    private bool initialJump;
    private bool grounded;
    private float airtimeCooldown;

    static int locomotionState = Animator.StringToHash("Base Layer.Locomotion");
    static int jumpStartState = Animator.StringToHash("Base Layer.JumpStart");
    static int jumpEndState = Animator.StringToHash("Base Layer.JumpEnd");
    private AnimatorStateInfo currentBaseAnimState;

    // Start is called before the first frame update
    void Start()
    {
        anim = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();
        cam = GetComponentInChildren<Camera>();
        if (cam == null)
        {
            Debug.LogError("PlayerMovementController requires the player to have a camera among it's children");
        }
        anim.speed = animationSpeed;
        initialJump = inAir = false;
        airtimeCooldown = 0;
    }

    // Update is called once per frame
    void Update()
    {
        fwdBack = Input.GetAxis("Vertical");
        leftRight = Input.GetAxis("Horizontal");
        grounded = GeoPhysics.IsPlayerGrounded(rb);

        // Movement speed capped at 1
        movement = new Vector3(leftRight, 0, fwdBack);
        if (movement.magnitude > 1f)
            movement = movement.normalized;

        // If the DistanceFromGround is negative keep the previous value; it just means the function failed
        float newDist = GeoPhysics.DistanceFromGround(rb);
        if (newDist >= 0)
            distFromGround = newDist;

        // Handle jumping
        if (!inAir && Input.GetButton("Jump") && grounded)
        {
            airtimeCooldown = minimalAirtime;
            initialJump = true;
            inAir = true;
        }

        // Should we land?
        airtimeCooldown -= Time.deltaTime;
        if (airtimeCooldown < 0)
        {
            airtimeCooldown = 0;
            if (inAir && grounded)
                inAir = false;
        }
    }

    void FixedUpdate()
    {
        anim.SetFloat("FwdBack", fwdBack);
        anim.SetFloat("LeftRight", leftRight);
        anim.SetBool("InAir", inAir);
        anim.SetFloat("DistFromGround", Mathf.Min(1f, distFromGround));
        currentBaseAnimState = anim.GetCurrentAnimatorStateInfo(0);

        // If we're jumping we need to handle movement ourselves; the jump animation is stationary.
        // Give the initial burst of speed, and allow some XZ movement while in the air
        if (initialJump)
        {
            rb.velocity += jumpSpeed * transform.up;
            initialJump = false;
        }
        if (inAir)
        {
            Vector3 speed = rb.rotation * (airMovementSpeed * movement);
            rb.MovePosition(rb.position + Time.deltaTime * speed);
        }
    }

    void OnAnimatorIK(int layer)
    {
        // Move head to look in the direction the player is aiming
        anim.SetLookAtWeight(1);
        anim.SetLookAtPosition(cam.transform.position + cam.transform.forward);
    }
}
