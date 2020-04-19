using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Animator))]
public class PlayerMovementController : MonoBehaviour
{
    public float animationSpeed = 5;

    private Animator anim;
    private float speed;
    private float direction;

    // Start is called before the first frame update
    void Start()
    {
        anim = GetComponent<Animator>();
        anim.speed = animationSpeed;
    }

    // Update is called once per frame
    void Update()
    {
        speed = Input.GetAxis("Vertical");
        direction = Input.GetAxis("Horizontal");
    }

    void FixedUpdate()
    {
        anim.SetFloat("Speed", speed);
        anim.SetFloat("Direction", direction);
    }
}
