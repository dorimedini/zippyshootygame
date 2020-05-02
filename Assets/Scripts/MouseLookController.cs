using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class MouseLookController : MonoBehaviour
{
    public Camera cam;
    public Rigidbody rb;
    public GrapplingCharacter grappleChar;

    void Update()
    {
        // Rotation
        float rotX = Input.GetAxis("Mouse X");
        float rotY = -Input.GetAxis("Mouse Y");
        transform.rotation = rb.rotation * Quaternion.Euler(0, UserDefinedConstants.lookSpeedX * rotX, 0);
        // Rotate the camera depending on Y axis input.
        cam.transform.Rotate(UserDefinedConstants.lookSpeedY * rotY, 0, 0);
        // If we look too far up: the camera's "up" direction will form an angle of over 180 degrees with
        // the player's forward direction. We can check this by checking SignedAngle from player's forward
        // to camera's up. If this angle is negative and less than -90 degrees, we leaned too far back.
        // If this angle is negative and more than -90, then we leaned too far forward.
        // To fix this, split into two cases: if the player is leaning back, we set the camera to look at
        // the origin with up towards the negative player forward direction.
        // If the player is leaning forward, look away from origin with up towards the player's forward.
        float angle = Vector3.SignedAngle(cam.transform.up, transform.forward, transform.right);
        if (angle < 0)
        {
            if (angle < -90)
                cam.transform.LookAt(Vector3.zero, -transform.forward);
            else
                cam.transform.LookAt(2 * transform.position, transform.forward);
        }
    }
}
