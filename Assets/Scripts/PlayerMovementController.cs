using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Animator))]
public class PlayerMovementController : MonoBehaviour
{
    public float animationSpeed = 1;
    public float jumpSpeed = 12;
    public float minimalAirtime = 0.5f; // Don't check for grounded state too soon into the jump

    private Animator anim;
    private Rigidbody rb;
    private float speed;
    private float direction;
    private bool inAir;
    private bool initialJump;
    private float airtimeCooldown;

    // Start is called before the first frame update
    void Start()
    {
        anim = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();
        anim.speed = animationSpeed;
        initialJump = inAir = false;
        airtimeCooldown = 0;
    }

    // Update is called once per frame
    void Update()
    {
        speed = Input.GetAxis("Vertical");
        direction = Input.GetAxis("Horizontal");
        bool grounded = GeoPhysics.IsPlayerGrounded(rb);

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
        anim.SetFloat("Speed", speed);
        anim.SetFloat("Direction", direction);
        anim.SetBool("InAir", inAir);

        // If we're jumping we need to handle movement ourselves; the jump animation is stationary.
        // Give the initial burst of speed, and allow some XZ movement while in the air
        if (initialJump)
        {
            rb.velocity += jumpSpeed * transform.up;
            initialJump = false;
        }
        if (inAir)
        {
            // TODO
        }
    }
}
