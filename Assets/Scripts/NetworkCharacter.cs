using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(PlayerMovementController))]
public class NetworkCharacter : MonoBehaviourPun, IPunObservable, IPunInstantiateMagicCallback
{
    public Animator anim;
    public Material localPlayerMaterial;
    public PlayerMovementController playerMovement;
    public GrapplingCharacter grappleChar;
    public GameObject hookshotPrefab;
    public Transform grappleHand;

    GameObject activeHookshot;
    Vector3 realPosition, grappleTarget;
    Quaternion realRotation;
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
        if (photonView.IsMine)
            GetComponentInChildren<SkinnedMeshRenderer>().material = localPlayerMaterial;
        else
        {
            anim.applyRootMotion = false;
        }
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
            stream.SendNext(grappleChar.GetGrappleTarget());
            stream.SendNext(anim.GetLayerWeight(baseLayerIdx));
            stream.SendNext(anim.GetLayerWeight(flyGrappleArmLayerIdx));
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
