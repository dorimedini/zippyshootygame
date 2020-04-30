using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(Rigidbody))]
public class GrapplingCharacter : MonoBehaviour
{
    public Camera cam;
    public Animator anim;
    public Rigidbody rb;
    public NetworkCharacter networkCharacter;
    public PlayerMovementController moveCtrl;

    private bool grappling;
    private bool initialGrapple;
    private bool grappleRampup;
    private Vector3 grapplePoint;
    private float grappleRampupCountdown;
    private float cameraGrappleFOV, cameraOriginalFOV;
    private Vector3 grappleCameraPullback, originalCamLocalPos;

    private int baseLayerIdx, flyGrappleArmLayerIdx, flyRestOfBodyLayerIdx;

    // Start is called before the first frame update
    void Start()
    {
        grappleRampup = initialGrapple = grappling = false;
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
        UpdateGrapple();
        UpdateCancelGrapple();
        moveCtrl.UpdateApplyRootMotion(InGrappleSequence());
        UpdateCameraFOV();
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
            if (Physics.Raycast(
                ray,
                out hit,
                UserDefinedConstants.sphereRadius * UserDefinedConstants.maxGrappleDistanceRatio,
                1 << LayerMask.NameToLayer("Environment")))
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
    void UpdateCameraFOV()
    {
        if (!InGrappleSequence() && !Tools.NearlyEqual(cam.fieldOfView, cameraOriginalFOV, 0.01f))
        {
            cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, cameraOriginalFOV, 0.1f);
        }
    }

    void FixedUpdate()
    {
        // If we're not grappling make sure we're either at or transitioning to the correct animation layer
        LerpGrappleBodyAnimation();

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

    public static GameObject DrawRope(GameObject ropePrefab, Transform grappleHand, Vector3 to)
    {
        var rope = Instantiate(ropePrefab, grappleHand.position, Quaternion.identity);
        RopeController rc = rope.GetComponent<RopeController>();
        if (rc == null)
            Debug.LogError("No ropecontroller on rope prefab!");
        rc.Init(grappleHand, to);
        return rope;
    }
}
