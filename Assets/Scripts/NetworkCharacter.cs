using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

[RequireComponent(typeof(Animator))]
public class NetworkCharacter : MonoBehaviourPun, IPunObservable
{
    Animator anim;

    Vector3 realPosition;
    Quaternion realRotation;
    float realFwdBack, realLeftRight, realDistFromGround;
    bool isInAir;

    // Start is called before the first frame update
    void Start()
    {
        anim = GetComponent<Animator>();
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
        }
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
        }
        else
        {
            realPosition = (Vector3)stream.ReceiveNext();
            realRotation = (Quaternion)stream.ReceiveNext();
            realFwdBack = (float)stream.ReceiveNext();
            realLeftRight = (float)stream.ReceiveNext();
            isInAir = (bool)stream.ReceiveNext();
            realDistFromGround = (float)stream.ReceiveNext();
        }
    }
}
