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
    public NetworkCharacter networkCharacter;

    private float fwdBack;
    private float leftRight;
    private Vector3 movement;
    private float distFromGround;
    private bool jumping;
    private bool initialJump;
    private bool grounded;
    private bool grappling;
    private bool initialGrapple;
    private bool grappleRampup;
    private Vector3 grapplePoint;
    private float grappleRampupCountdown;
    private float cameraGrappleFOV, cameraOriginalFOV;
    private Vector3 grappleCameraPullback, originalCamLocalPos;
    private float airtimeCooldown;
    private float rootMotionOffFor;

    private int baseLayerIdx, flyGrappleArmLayerIdx, flyRestOfBodyLayerIdx;

    static int locomotionState = Animator.StringToHash("Base Layer.Locomotion");
    static int jumpStartState = Animator.StringToHash("Base Layer.JumpStart");
    static int jumpEndState = Animator.StringToHash("Base Layer.JumpEnd");
    private AnimatorStateInfo currentBaseAnimState;

    // Start is called before the first frame update
    void Start()
    {
        anim.speed = 2 * UserDefinedConstants.movementSpeed;
        grappleRampup = initialGrapple = grappling = initialJump = jumping = false;
        airtimeCooldown = rootMotionOffFor = 0;
        originalCamLocalPos = cam.transform.localPosition;
        grappleCameraPullback = originalCamLocalPos + new Vector3(0, 0, -0.3f);
        cameraOriginalFOV = cam.fieldOfView;
        cameraGrappleFOV = 1.5f * cameraOriginalFOV;
        baseLayerIdx = anim.GetLayerIndex("Base Layer");
        flyGrappleArmLayerIdx = anim.GetLayerIndex("FlyGrappleArm");
        flyRestOfBodyLayerIdx = anim.GetLayerIndex("FlyRestOfBody");
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
        UpdateCancelGrapple();
        UpdateJump();
        UpdateLand();
        UpdateApplyRootMotion();
        UpdateCameraFOV();
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
    void UpdateCancelGrapple()
    {
        // Cancel grapple on jump, even if not grounded
        if (Input.GetButtonDown("Jump"))
            CancelGrapple();
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
                grappleRampupCountdown = UserDefinedConstants.grappleRampupTime;
                grapplePoint = hit.point;
            }
        }

        // Only actual updates happen during rampup
        if (grappleRampup)
        {
            grappleRampupCountdown = Mathf.Max(0, grappleRampupCountdown - Time.deltaTime);
            if (grappleRampupCountdown > UserDefinedConstants.grappleRampupTime / 2)
            {
                // In the first half of the rampup, pull the camera back a bit
                float pullbackPercentage = 2 * (1 - (grappleRampupCountdown / UserDefinedConstants.grappleRampupTime));
                cam.transform.localPosition = Vector3.Lerp(originalCamLocalPos, grappleCameraPullback, pullbackPercentage);
            }
            else
            {
                // In the second half of the rampup, push the camera back towards the original location.
                // Also, widen the FOV
                float pushForwardPercentage = 1 - (2 * grappleRampupCountdown / UserDefinedConstants.grappleRampupTime);
                cam.transform.localPosition = Vector3.Lerp(grappleCameraPullback, originalCamLocalPos, pushForwardPercentage);
                cam.fieldOfView = Mathf.Lerp(cameraOriginalFOV, cameraGrappleFOV, pushForwardPercentage);
            }
            // Should we stop rampup phase?
            if (Tools.NearlyEqual(grappleRampupCountdown, 0, 0.01f))
            {
                // At this point, just set the camera location back to normal
                cam.transform.localPosition = originalCamLocalPos;
                grappleRampup = false;
                grappling = true;
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
        // Note that we may be "grounded" and grappling at the same time
        rootMotionOffFor = Mathf.Max(0, rootMotionOffFor - Time.deltaTime);
        if (grounded && Tools.NearlyEqual(rootMotionOffFor, 0, 0.01f) && !InGrappleSequence())
        {
            anim.applyRootMotion = true;
        }
    }
    void UpdateCameraFOV()
    {
        if (!InGrappleSequence() && !Tools.NearlyEqual(cam.fieldOfView, cameraOriginalFOV, 0.01f))
        {
            cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, cameraOriginalFOV, 0.1f);
        }
    }

    void FixedUpdate()
    {
        anim.SetFloat("FwdBack", fwdBack);
        anim.SetFloat("LeftRight", leftRight);
        anim.SetBool("InAir", !grounded);
        anim.SetFloat("DistFromGround", Mathf.Min(1f, distFromGround));
        currentBaseAnimState = anim.GetCurrentAnimatorStateInfo(0);

        // If we're not grappling make sure we're either at or transitioning to the correct animation layer
        LerpGrappleBodyAnimation();

        // Jump (may cancel grapple)
        if (initialJump)
        {
            FixedUpdateInitialJump();
        }

        // Grapple (first register initial click, then proceed to rampup stage, then to grappling stage).
        if (initialGrapple)
        {
            FixedUpdateInitialGrapple();
        }
        if (grappleRampup)
        {
            FixedUpdateGrappleRampup();
        }
        else if (grappling)
        {
            FixedUpdateGrappling();
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
    void FixedUpdateInitialGrapple()
    {
        initialGrapple = false;
        grappleRampup = true;
        anim.SetBool("Grappling", true);
        anim.applyRootMotion = false;
    }
    void FixedUpdateGrappleRampup()
    {
        LerpGrappleBodyAnimation();
        RotateFlightGraphic();
        AccelerateTowardsGrappleTarget();
    }
    void FixedUpdateGrappling()
    {
        // When we reach this point (after rampup), we should have already pretty much reached the target speed.
        // So, if our grapple speed (speed in the target direction) is less than half the desired grapple speed, we probably
        // hit something along the way and should cancel the grapple.
        // Otherwise, we may have bumped into something that shouldn't cancel our movement - if so, fix the speed by adding more
        // acceleration in the target direction.
        float speedInDirection = Vector3.Dot(rb.velocity, (GetGrappleTarget() - transform.position).normalized);
        if (speedInDirection < UserDefinedConstants.grappleSpeed / 2)
        {
            CancelGrapple();
        }
        else
        {
            RotateFlightGraphic();
            AccelerateTowardsGrappleTarget();
        }
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

    public Vector3 GetGrappleTarget() { return grapplePoint; }
    void CancelGrapple()
    {
        anim.SetBool("Grappling", false);
        if (InGrappleSequence())
        {
            initialGrapple = grappling = grappleRampup = false;
        }
    }
    void AccelerateTowardsGrappleTarget()
    {
        // If we're already at the desired speed in the target direction, do nothing.
        // Otherwise, give a bit of additive speed in the direction, proportional to deltatime.
        // Acceleration rate is defined as follows: we want to accelerate from *any* speed & direction to target speed & direction
        // in exactly grappleRampupTime/2 seconds.
        // So, lerp from current speed to target speed.
        rb.velocity = Vector3.Lerp(
                rb.velocity,
                UserDefinedConstants.grappleSpeed * (GetGrappleTarget() - transform.position).normalized,
                0.1f
            );
    }
    bool InGrappleSequence()
    {
        return grappleRampup || grappling || initialGrapple;
    }
    void RotateFlightGraphic()
    {
        // Two GameObjects are relevant here for Robot Kyle: the Robot2 object and the Root object.
        // I suspect we only really need to rotate the Root object, because the animations derive from it, but it depends if the
        // animator does animations relative to Root or what.
        if (InGrappleSequence())
        {
            // TODO: When animation is in place, rotate graphic to face target here. This should be done manually since the headTowardsOrigin
            // TODO: component may be lerping our rotation
        }
        else
        {
            // TODO: After grappling the graphic component of the character may be out of sync.
        }
    }
    void LerpGrappleBodyAnimation()
    {
        float baseLayerTarget = InGrappleSequence() ? 0 : 1;
        anim.SetLayerWeight(baseLayerIdx, Mathf.Lerp(anim.GetLayerWeight(baseLayerIdx), baseLayerTarget, 0.1f));
        anim.SetLayerWeight(flyGrappleArmLayerIdx, Mathf.Lerp(anim.GetLayerWeight(flyGrappleArmLayerIdx), 1 - baseLayerTarget, 0.1f));
        anim.SetLayerWeight(flyRestOfBodyLayerIdx, Mathf.Lerp(anim.GetLayerWeight(flyRestOfBodyLayerIdx), 1 - baseLayerTarget, 0.1f));
    }
}
