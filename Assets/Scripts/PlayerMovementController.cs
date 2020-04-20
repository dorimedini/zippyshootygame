using System.Collections;
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
    public float launchForceMultiplier = 4f; // Multiplied by the distance to the target height

    private Animator anim;
    private Rigidbody rb;
    private float fwdBack;
    private float leftRight;
    private Vector3 movement;
    private float distFromGround;
    private bool jumping;
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
        anim.speed = animationSpeed;
        initialJump = jumping = false;
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
        if (!jumping && Input.GetButton("Jump") && grounded)
        {
            airtimeCooldown = minimalAirtime;
            initialJump = true;
            jumping = true;
        }

        // Should we land?
        airtimeCooldown -= Time.deltaTime;
        if (airtimeCooldown < 0)
        {
            airtimeCooldown = 0;
            if (jumping && grounded)
                jumping = false;
        }
    }

    void FixedUpdate()
    {
        anim.SetFloat("FwdBack", fwdBack);
        anim.SetFloat("LeftRight", leftRight);
        anim.SetBool("InAir", !grounded);
        anim.SetFloat("DistFromGround", Mathf.Min(1f, distFromGround));
        currentBaseAnimState = anim.GetCurrentAnimatorStateInfo(0);

        // Give the initial burst of speed, and allow some XZ movement while in the air
        if (initialJump)
        {
            rb.velocity += jumpSpeed * transform.up;
            initialJump = false;
        }
        // If we're airborne we need to handle movement ourselves; the airborne animation is stationary.
        if (anim.GetBool("InAir"))
        {
            Vector3 speed = rb.rotation * (airMovementSpeed * movement);
            rb.MovePosition(rb.position + Time.deltaTime * speed);
        }
    }

    public void LaunchFromPillar(int pillarId, float pillarHeightChange)
    {
        Vector3 force = -rb.transform.position.normalized;
        force *= launchForceMultiplier * pillarHeightChange;
        rb.AddForce(force, ForceMode.Impulse);
    }
}
