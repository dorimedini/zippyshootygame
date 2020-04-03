using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FirstPersonController : MonoBehaviour
{

    public float movementSpeed;
    public float lookSpeedX;
    public float gravity; // Usually 9.8f

    private Rigidbody rb;

    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void Update()
    {
        // Rotation
        float rotX = Input.GetAxis("Mouse X");
        float rotY = Input.GetAxis("Mouse Y");
        transform.rotation = rb.rotation * Quaternion.Euler(0, lookSpeedX * rotX, 0);
        // The "forward" direction needs to deviate from the natural XZ plane when walking inside
        // the sphere.
        // Project the current forward direction on the plane perpendicular to player's position
        // and call LookAt to orientate the player so his head is towards the origin.
        // LookAt needs a target to look at, and the "up" direction.
        Vector3 newForward = Vector3.ProjectOnPlane(transform.forward, -transform.position.normalized);
        transform.LookAt(transform.position + newForward, -transform.position);

        // Movement
        float forwardSpeed = Input.GetAxis("Vertical");
        float sideSpeed = Input.GetAxis("Horizontal");
        Vector3 moveDirection = new Vector3(sideSpeed, 0, forwardSpeed);
        Vector3 speed = rb.rotation * (movementSpeed * moveDirection);
        rb.MovePosition(rb.position + Time.deltaTime * speed);

        // Gravity
        rb.AddForce(rb.mass * gravity * rb.position.normalized);
    }

    void OnCollisionEnter(Collision other)
    {
        GameObject obj = other.gameObject;
        TileBehaviour tile = obj.GetComponent<TileBehaviour>();
        if (tile != null)
        {
            Debug.Log(string.Format("Collided with tile {0}", tile.id));
        }
    }

    void OnCollisionExit(Collision other)
    {
        GameObject obj = other.gameObject;
        TileBehaviour tile = obj.GetComponent<TileBehaviour>();
        if (tile != null)
        {
            Debug.Log(string.Format("Exited collision with tile {0}", tile.id));
        }
    }

}
