using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Animator))]
public class HeadLook : MonoBehaviour
{
    public Animator anim;
    public Camera cam;

    void OnAnimatorIK(int layer)
    {
        // Move head to look in the direction the player is aiming
        anim.SetLookAtWeight(1);
        anim.SetLookAtPosition(cam.transform.position + cam.transform.forward);
    }
}
