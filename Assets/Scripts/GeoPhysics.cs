using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class GeoPhysics
{
    public static float gravity = 9.8f;

    public static void ApplyGravity(Rigidbody rb)
    {
        rb.AddForce(rb.mass * gravity * rb.position.normalized);
    }

    /** Returns true if the rigidbody has an object at feet level. Optionally returns the object */
    public static bool IsPlayerGrounded(Rigidbody player)
    {
        GameObject dud;
        return IsPlayerGrounded(player, out dud);
    }
    public static bool IsPlayerGrounded(Rigidbody player, out GameObject hit)
    {
        RaycastHit rHit;
        if (Physics.Raycast(player.transform.position + player.transform.up, -player.transform.up, out rHit, 1.1f))
        {
            hit = rHit.collider.gameObject;
            return true;
        }
        hit = null;
        return false;
    }
}