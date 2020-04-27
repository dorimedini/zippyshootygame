using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HeadTowardsOrigin : MonoBehaviour
{
    void Update()
    {
        // The "forward" direction needs to deviate from the natural XZ plane when walking inside
        // the sphere.
        // Project the current forward direction on the plane perpendicular to player's position
        // and call LookAt to orientate the player so his head is towards the origin.
        // LookAt needs a target to look at, and the "up" direction.
        Vector3 newForward = Vector3.ProjectOnPlane(transform.forward, -transform.position.normalized);
        transform.LookAt(transform.position + newForward, -transform.position);
    }
}
