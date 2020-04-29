using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(PlayerMovementController))]
public class NetworkCharacter : MonoBehaviourPun, IPunObservable, IPunInstantiateMagicCallback
{
    public GameObject ropePrefab;
    public Animator anim;
    public Transform grappleHand;
    public Material localPlayerMaterial;
    public PlayerMovementController playerMovement;

    GameObject activeRope;
    Vector3 realPosition, grappleTarget, prevGrappleTarget;
    Quaternion realRotation;
    float realFwdBack, realLeftRight, realDistFromGround;
    bool isInAir, grappling, rootMotionApplied;

    int baseLayerIdx, flyGrappleArmLayerIdx, flyRestOfBodyLayerIdx;
    float baseLayerWeight, grappleLayersWeight;

    void Start()
    {
        activeRope = null;
        prevGrappleTarget = Vector3.zero;
        baseLayerIdx = anim.GetLayerIndex("Base Layer");
        flyGrappleArmLayerIdx = anim.GetLayerIndex("FlyGrappleArm");
        flyRestOfBodyLayerIdx = anim.GetLayerIndex("FlyRestOfBody");
        if (photonView.IsMine)
            GetComponentInChildren<SkinnedMeshRenderer>().material = localPlayerMaterial;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (!photonView.IsMine)
        {
            float lerpConst = 0.1f;
            transform.position = Vector3.Lerp(transform.position, realPosition, lerpConst);
            transform.rotation = Quaternion.Lerp(transform.rotation, realRotation, lerpConst);
            anim.SetFloat("FwdBack", Mathf.Lerp(anim.GetFloat("FwdBack"), realFwdBack, lerpConst));
            anim.SetFloat("LeftRight", Mathf.Lerp(anim.GetFloat("LeftRight"), realLeftRight, lerpConst));
            anim.SetFloat("DistFromGround", Mathf.Lerp(anim.GetFloat("DistFromGround"), realDistFromGround, lerpConst));
            anim.SetBool("InAir", isInAir);  // Can't lerp booleans
            anim.SetBool("Grappling", grappling);
            anim.SetLayerWeight(baseLayerIdx, baseLayerWeight);
            anim.SetLayerWeight(flyGrappleArmLayerIdx, grappleLayersWeight);
            anim.SetLayerWeight(flyRestOfBodyLayerIdx, grappleLayersWeight);
            anim.applyRootMotion = rootMotionApplied;
        }
        // As the grapple rope shares common behaviour across the network and we want the graphics updated locally, the NetworkCharacter
        // is responsible for drawing it's own grapple rope.
        DrawRope();
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo message)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(transform.position);
            stream.SendNext(transform.rotation);
            stream.SendNext(anim.GetFloat("FwdBack"));
            stream.SendNext(anim.GetFloat("LeftRight"));
            stream.SendNext(anim.GetBool("InAir"));
            stream.SendNext(anim.GetFloat("DistFromGround"));
            stream.SendNext(anim.GetBool("Grappling"));
            stream.SendNext(playerMovement.GetGrappleTarget());
            stream.SendNext(anim.GetLayerWeight(baseLayerIdx));
            stream.SendNext(anim.GetLayerWeight(flyGrappleArmLayerIdx));
            stream.SendNext(anim.applyRootMotion);
        }
        else
        {
            realPosition = (Vector3)stream.ReceiveNext();
            realRotation = (Quaternion)stream.ReceiveNext();
            realFwdBack = (float)stream.ReceiveNext();
            realLeftRight = (float)stream.ReceiveNext();
            isInAir = (bool)stream.ReceiveNext();
            realDistFromGround = (float)stream.ReceiveNext();
            grappling = (bool)stream.ReceiveNext();
            // If the player changed grapple direction mid-grapple catch it here. We won't get a false boolean for "Grappling"
            Vector3 newGrappleTarget = (Vector3)stream.ReceiveNext();
            if (newGrappleTarget != grappleTarget)
            {
                prevGrappleTarget = grappleTarget;
                grappleTarget = newGrappleTarget;
            }
            baseLayerWeight = (float)stream.ReceiveNext();
            grappleLayersWeight = (float)stream.ReceiveNext();
            rootMotionApplied = (bool)stream.ReceiveNext();
        }
    }

    void DrawRope()
    {
        // If we're not grappling, destroy the rope and do nothing else.
        // Also, if the rope target changed, we may not get a 'false' value in the Grappling boolean but we still need to destroy the old rope.
        Vector3 currentGrappleTarget = GetThisGrappleTarget();
        if (activeRope != null && (!IsGrappling() || prevGrappleTarget != currentGrappleTarget))
        {
            Destroy(activeRope);
            activeRope = null;
            prevGrappleTarget = currentGrappleTarget;
        }
        if (activeRope == null && IsGrappling())
        {
            activeRope = Instantiate(ropePrefab, transform.position, Quaternion.identity);
            RopeController rc = activeRope.GetComponent<RopeController>();
            if (rc == null)
                Debug.LogError("No ropecontroller on rope prefab!");
            rc.Init(grappleHand, GetThisGrappleTarget());
        }
    }

    bool IsGrappling()
    {
        return photonView.IsMine ? anim.GetBool("Grappling") : grappling;
    }
    Vector3 GetThisGrappleTarget()
    {
        return photonView.IsMine ? playerMovement.GetGrappleTarget() : grappleTarget;
    }

    public void OnPhotonInstantiate(PhotonMessageInfo info)
    {
        info.Sender.TagObject = gameObject;
    }
}
