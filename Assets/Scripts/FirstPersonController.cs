using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FirstPersonController : MonoBehaviour
{

    public float movementSpeed;
    public float lookSpeedX;
    public float lookSpeedY;
    public float gravity; // Usually 9.8f

    private Rigidbody rb;
    private Camera camera;

    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        camera = GetComponentInChildren<Camera>();
    }

    // Update is called once per frame
    void Update()
    {
        // Rotation
        float rotX = Input.GetAxis("Mouse X");
        float rotY = -Input.GetAxis("Mouse Y");
        transform.rotation = rb.rotation * Quaternion.Euler(0, lookSpeedX * rotX, 0);
        // The "forward" direction needs to deviate from the natural XZ plane when walking inside
        // the sphere.
        // Project the current forward direction on the plane perpendicular to player's position
        // and call LookAt to orientate the player so his head is towards the origin.
        // LookAt needs a target to look at, and the "up" direction.
        Vector3 newForward = Vector3.ProjectOnPlane(transform.forward, -transform.position.normalized);
        transform.LookAt(transform.position + newForward, -transform.position);
        // Rotate the camera depending on Y axis input
        camera.transform.Rotate(lookSpeedY * rotY, 0, 0);
        // If we look too far up: the camera's "up" direction will form an angle of over 180 degrees with
        // the player's forward direction. We can check this by checking SignedAngle from player's forward
        // to camera's up. If this angle is negative and less than -90 degrees, we leaned too far back.
        // If this angle is negative and more than -90, then we leaned too far forward.
        // To fix this, split into two cases: if the player is leaning back, we set the camera to look at
        // the origin with up towards the negative player forward direction.
        // If the player is leaning forward, look away from origin with up towards the player's forward.
        float angle = Vector3.SignedAngle(camera.transform.up, transform.forward, transform.right);
        if (angle < 0)
        {
            if (angle < -90)
                camera.transform.LookAt(Vector3.zero, -transform.forward);
            else
                camera.transform.LookAt(2 * transform.position, transform.forward);
        }

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
