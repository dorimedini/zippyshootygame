using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(PlayerMovementController))]
public class NetworkCharacter : MonoBehaviourPun, IPunObservable, IPunInstantiateMagicCallback
{
    public Animator anim;
    public PlayerMovementController playerMovement;
    public GrapplingCharacter grappleChar;
    public GameObject hookshotPrefab;
    public Transform grappleHand;
    public Rigidbody rb;
    public GravityAffected gravityComponent;
    public Transform rootGraphicTransform;

    float disableRemoteUpdatesFor;
    bool remoteUpdatesDisabled;

    GameObject activeHookshot;
    Vector3 realPosition, grappleTarget;
    Quaternion realRotation, realRootGraphicRotation;
    float realFwdBack, realLeftRight, realDistFromGround;
    bool isInAir, grappling;

    int baseLayerIdx, flyGrappleArmLayerIdx, flyRestOfBodyLayerIdx;
    float baseLayerWeight, grappleLayersWeight;

    void Start()
    {
        activeHookshot = null;
        baseLayerIdx = anim.GetLayerIndex("Base Layer");
        flyGrappleArmLayerIdx = anim.GetLayerIndex("FlyGrappleArm");
        flyRestOfBodyLayerIdx = anim.GetLayerIndex("FlyRestOfBody");
        disableRemoteUpdatesFor = 0;
        remoteUpdatesDisabled = false;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        disableRemoteUpdatesFor = Mathf.Max(0, disableRemoteUpdatesFor - Time.deltaTime);
        if (remoteUpdatesDisabled && Tools.NearlyEqual(disableRemoteUpdatesFor, 0, 0.01f))
        {
            EnableRemoteUpdates();
        }
        if (!photonView.IsMine && !remoteUpdatesDisabled)
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
            rootGraphicTransform.rotation = Quaternion.Lerp(rootGraphicTransform.rotation, realRootGraphicRotation, lerpConst);
        }
        // As the grapple rope shares common behaviour across the network and we want the graphics updated locally, the NetworkCharacter
        // is responsible for drawing it's own grapple rope.
        DrawRope();
    }

    void DisableRemoteUpdates(float duration)
    {
        if (photonView.IsMine)
        {
            Debug.LogError("Attempt to disable remote updates of local player");
            return;
        }
        remoteUpdatesDisabled = true;
        disableRemoteUpdatesFor = duration;
        // No longer get gravity updates from remote character
        gravityComponent.enabled = true;
        rb.isKinematic = false;
    }

    void EnableRemoteUpdates()
    {
        if (!remoteUpdatesDisabled)
            return; // The Disable() function makes sure this flag can only be true if network character isn't local
        remoteUpdatesDisabled = false;
        gravityComponent.enabled = false;
        rb.isKinematic = true;
    }

    public void ApplyLocalForce(Vector3 explosionForce, ForceMode mode)
    {
        DisableRemoteUpdates(UserDefinedConstants.localMovementOverrideWindow);
        explosionForce *= UserDefinedConstants.localForceDampen;
        rb.AddForce(explosionForce, mode);
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
            stream.SendNext(grappleChar.GetGrappleTarget());
            stream.SendNext(anim.GetLayerWeight(baseLayerIdx));
            stream.SendNext(anim.GetLayerWeight(flyGrappleArmLayerIdx));
            stream.SendNext(rootGraphicTransform.rotation);
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
            grappleTarget = (Vector3)stream.ReceiveNext();
            baseLayerWeight = (float)stream.ReceiveNext();
            grappleLayersWeight = (float)stream.ReceiveNext();
            realRootGraphicRotation = (Quaternion)stream.ReceiveNext();
        }
    }

    void DrawRope()
    {
        if (!IsGrappling())
        {
            if (activeHookshot != null)
            {
                Destroy(activeHookshot);
                activeHookshot = null;
            }
            return;
        }
        // If the rope already exists, we only ever need to update the target; the hand transform is always the same (player's hand).
        if (activeHookshot != null)
        {
            HookshotController hc = activeHookshot.GetComponent<HookshotController>();
            if (hc == null)
            {
                Debug.LogError("No hookshot controller on hookshot prefab!");
                return;
            }
            hc.UpdateTarget(GetThisGrappleTarget());
            return;
        }
        else
        {
            activeHookshot = HookshotController.DrawHookshot(hookshotPrefab, grappleHand, GetThisGrappleTarget());
        }
    }

    bool IsGrappling()
    {
        return photonView.IsMine ? anim.GetBool("Grappling") : grappling;
    }
    Vector3 GetThisGrappleTarget()
    {
        return photonView.IsMine ? grappleChar.GetGrappleTarget() : grappleTarget;
    }

    public void OnPhotonInstantiate(PhotonMessageInfo info)
    {
        info.Sender.TagObject = gameObject;
    }
}
