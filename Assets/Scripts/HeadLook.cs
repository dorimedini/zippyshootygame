using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Animator))]
public class HeadLook : MonoBehaviour
{
    Animator anim;
    Camera cam;

    // Start is called before the first frame update
    void Start()
    {
        anim = GetComponent<Animator>();
        cam = GetComponentInChildren<Camera>();
        if (cam == null)
            Debug.LogError("HeadLook requires camera to function properly");
    }

    void OnAnimatorIK(int layer)
    {
        // Move head to look in the direction the player is aiming
        anim.SetLookAtWeight(1);
        anim.SetLookAtPosition(cam.transform.position + cam.transform.forward);
    }
}
