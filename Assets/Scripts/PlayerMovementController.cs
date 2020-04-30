using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(Rigidbody))]
public class PlayerMovementController : MonoBehaviour
{
    public float airMovementSpeed = 5;
    public float minimalAirtime = 0.5f; // Don't check for grounded state too soon into the jump
    public Camera cam;
    public Animator anim;
    public Rigidbody rb;
    public NetworkCharacter networkCharacter;

    private float fwdBack;
    private float leftRight;
    private Vector3 movement;
    private float distFromGround;
    private bool jumping;
    private bool initialJump;
    private bool grounded;
    private float airtimeCooldown;
    private float rootMotionOffFor;

    static int locomotionState = Animator.StringToHash("Base Layer.Locomotion");
    static int jumpStartState = Animator.StringToHash("Base Layer.JumpStart");
    static int jumpEndState = Animator.StringToHash("Base Layer.JumpEnd");
    private AnimatorStateInfo currentBaseAnimState;

    // Start is called before the first frame update
    void Start()
    {
        anim.speed = 2 * UserDefinedConstants.movementSpeed;
        initialJump = jumping = false;
        airtimeCooldown = rootMotionOffFor = 0;
    }

    // Update is called once per frame
    void Update()
    {
        fwdBack = Input.GetAxis("Vertical");
        leftRight = Input.GetAxis("Horizontal");
        grounded = GeoPhysics.IsPlayerGrounded(rb);
        distFromGround = GeoPhysics.DistanceFromGround(rb);

        UpdateMove();
        UpdateJump();
        UpdateLand();
    }

    void UpdateMove()
    {
        // Movement speed capped at 1
        movement = new Vector3(leftRight, 0, fwdBack);
        if (movement.magnitude > 1f)
            movement = movement.normalized;
    }
    void UpdateJump()
    {
        // Handle jumping
        if (!jumping && Input.GetButton("Jump") && grounded)
        {
            airtimeCooldown = minimalAirtime;
            initialJump = true;
            jumping = true;
        }
    }
    void UpdateLand()
    {
        // Should we land?
        airtimeCooldown -= Time.deltaTime;
        if (airtimeCooldown < 0)
        {
            airtimeCooldown = 0;
            if (jumping && grounded)
                jumping = false;
        }
    }
    public void UpdateApplyRootMotion(bool inGrappleSequence)
    {
        // Should we re-apply root motion?
        // Note that we may be "grounded" and grappling at the same time
        rootMotionOffFor = Mathf.Max(0, rootMotionOffFor - Time.deltaTime);
        if (grounded && Tools.NearlyEqual(rootMotionOffFor, 0, 0.01f) && !inGrappleSequence)
        {
            anim.applyRootMotion = true;
        }
    }

    void FixedUpdate()
    {
        anim.SetFloat("FwdBack", fwdBack);
        anim.SetFloat("LeftRight", leftRight);
        anim.SetBool("InAir", !grounded);
        anim.SetFloat("DistFromGround", Mathf.Min(1f, distFromGround));
        currentBaseAnimState = anim.GetCurrentAnimatorStateInfo(0);

        // Jump (may cancel grapple)
        if (initialJump)
        {
            FixedUpdateInitialJump();
        }

        // Airborne
        if (!grounded)
        {
            FixedUpdateHandleAirborneMovement();
        }
    }
    void FixedUpdateInitialJump()
    {
        // Give the initial burst of speed, and allow some XZ movement while in the air
        rb.velocity += UserDefinedConstants.jumpSpeed * transform.up;
        initialJump = false;
    }
    void FixedUpdateHandleAirborneMovement()
    {
        // If we're airborne we need to handle movement ourselves; the airborne animation is stationary.
        // Also, root motion should NOT be applied while airborne!
        anim.applyRootMotion = false;
        Vector3 speed = rb.rotation * (airMovementSpeed * movement);
        rb.MovePosition(rb.position + Time.deltaTime * speed);
    }

    public void LaunchFromPillar(int pillarId, float pillarHeightChange)
    {
        Vector3 force = -rb.transform.position.normalized;
        force *= UserDefinedConstants.launchForceMultiplier * pillarHeightChange;
        rb.AddForce(force, ForceMode.Impulse);
    }

    public void DisableRootMotionFor(float duration)
    {
        if (anim == null)
        {
            Debug.LogWarning("DisableRootMotion() called on character with no active animator!");
            return;
        }
        anim.applyRootMotion = false;
        rootMotionOffFor = duration;
    }
}
