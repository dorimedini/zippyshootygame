using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Animator))]
public class PlayerMovementController : MonoBehaviour
{
    private Animator anim;

    // Start is called before the first frame update
    void Start()
    {
        anim = GetComponent<Animator>();
        anim.SetLayerWeight(1, 1);
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        float speed = Input.GetAxis("Vertical");
        float direction = Input.GetAxis("Horizontal");
        anim.SetFloat("Speed", speed);
        anim.SetFloat("Direction", direction);
    }
}
