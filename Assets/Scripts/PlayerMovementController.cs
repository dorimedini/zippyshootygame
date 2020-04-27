using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Camera))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(Rigidbody))]
public class PlayerMovementController : MonoBehaviour
{
    public float airMovementSpeed = 5;
    public float minimalAirtime = 0.5f; // Don't check for grounded state too soon into the jump
    public Camera cam;
    public Animator anim;
    public Rigidbody rb;

    private float fwdBack;
    private float leftRight;
    private Vector3 movement;
    private float distFromGround;
    private bool jumping;
    private bool initialJump;
    private bool grounded;
    private bool grappling;
    private bool initialGrapple;
    private Vector3 grapplePoint;
    private GameObject grappleObject;
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
        initialGrapple = grappling = initialJump = jumping = false;
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
        UpdateGrapple();
        UpdateJump();
        UpdateLand();
        UpdateApplyRootMotion();
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
    void UpdateGrapple()
    {
        // Player should be able to grapple at any time, even while grappling.
        // Only catch the initial buttondown though, so it doesn't speedfire.
        // Also, we need to grab the hit object and hit location here (in Update) for precision.
        if (Input.GetButtonDown("Fire2"))
        {
            RaycastHit hit;
            Ray ray = new Ray(cam.transform.position + cam.transform.forward, cam.transform.forward);
            if (Physics.Raycast(ray, out hit, UserDefinedConstants.maxGrappleDistance, 1 << LayerMask.NameToLayer("Environment")))
            {
                initialGrapple = true;
                grapplePoint = hit.point;
                grappleObject = hit.collider.gameObject;
            }
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
    void UpdateApplyRootMotion()
    {
        // Should we re-apply root motion?
        rootMotionOffFor = Mathf.Max(0, rootMotionOffFor - Time.deltaTime);
        if (grounded && Tools.NearlyEqual(rootMotionOffFor, 0, 0.01f))
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

        if (initialJump)
        {
            FixedUpdateInitialJump();
        }
        if (initialGrapple)
        {
            FixedUpdateInitialGrapple();
        }
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
    void FixedUpdateInitialGrapple()
    {
        initialGrapple = false;
        // TODO: This is how grappling is going to work:
        // Grappling momentarily cancels all other player-input movement (and root motion), but keeps applied movement. This means that player 
        // keeps falling, if he's on a rising pillar he keeps launching, if blown by explosion keep flying backwards... etc. Only exception: on
        // jump command, the grapple sequence cancels (even at this preliminary stage).
        // During this small timespan, the player CAMERA has a move-back effect, to give the player indication he's about to be launched
        // forward. During this timespan, disable mouselook (keep the camera focussed on the grapple target).
        // After that timespan, the player gets mouselook back, Camera slides back into position and the player gets some initial speed burst 
        // in the direction of grapple. The player steadily accelerates until the dot product between the speed and the direction of the target
        // is small. Why use this method? Because 1. if the player reached the grapple destination he'll hit a wall and stop so this will stop
        // the grapple, and 2. if the player hits something on the way we should allow minor bumps but walls should stop the grapple.
        // As before, during this airtime, pressing jump should cancel the grapple BUT should keep the previous speed and let natural gravity
        // decay it.
        // TODO: How should I handle speed decay? The moment air-movement control is restored current implementation will cap the airborne
        // TODO: movement speed
        // After grapple reaches it's natural end (not cancelled by jump), I need to decide if the resulting behavior is what I want or not.
        // It could be that the player suddenly stops when root motion is given back, or maybe the player will fly forward with the previous
        // momentum... or maybe crash through the sphere for some reason.... anyway, tackle this when it comes up.
        // ANIMATION AND GRAPHICS:
        // First thing to note: the graphic representation of the grappling character needs to counter the default rotation setting! During 
        // grapple we want to display a Superman-style flight, fist-forward and plank body, oriented in the flight direction.
        // A good approach would be to simply disable rotation orientation while grappling.
        // Note that the camera "forward" direction should now point from hips to head (body is horizontal). This is desired behaviour, so the
        // player can look around as feels natural grappling.
        // In any case, the animator will need an ease-in-ease-out of the flying pose. Grappling can be done from any state, even from grappling,
        // so maybe this should be 3 states (enter-, during-, exit-grapple), or maybe use a Grappling bool parameter and simply ease transitions
        // via Animator controls?
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
