using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class GeoPhysics
{
    public static float gravity = 9.8f;
    public static float radius = 70;

    public static void ApplyGravity(Rigidbody rb)
    {
        rb.AddForce(rb.mass * gravity * UserDefinedConstants.gravityMultiplier * rb.position.normalized);
    }

    /** Returns true if the rigidbody has an object at feet level. Optionally returns the object */
    public static bool IsPlayerGrounded(Rigidbody player)
    {
        PillarBehaviour dud;
        return IsPlayerGrounded(player, out dud);
    }
    public static bool IsPlayerGrounded(Rigidbody player, out PillarBehaviour hitPillar)
    {
        float dist = DistanceFromGround(player, out hitPillar);
        return hitPillar == null ? false : dist < 0.1f;
    }

    public static float DistanceFromGround(Rigidbody player)
    {
        PillarBehaviour dud;
        return DistanceFromGround(player, out dud);
    }
    public static float DistanceFromGround(Rigidbody player, out PillarBehaviour hitPillar)
    {
        // Find the highest pillar cllider under the player and return the distance from it
        hitPillar = null;
        float highestHit = -1;
        var hits = Physics.RaycastAll(player.transform.position + player.transform.up, -player.transform.up);
        if (hits.Length == 0)
        {
            Debug.LogError("Nothing under player!");
            return -1;
        }
        foreach (var hit in hits)
        {
            var pillar = hit.collider.gameObject.GetComponent<PillarBehaviour>();
            if (pillar != null && pillar.currentHeight > highestHit)
            {
                hitPillar = pillar;
                highestHit = pillar.currentHeight;
            }
        }
        if (hitPillar == null)
        {
            // We may have hit the bounding sphere between the pillars with this raycast. If so, ground is radius away from origin.
            // If hits[] has no elements we've already returned
            if (hits[0].collider.gameObject.name == "InvertableSphere(Clone)")
                return radius - player.position.magnitude;
            Debug.LogError(string.Format("Raycast didn't get a pillar / bounding-sphere hit! Hit {0} instead", hits[0].collider.gameObject.name));
            return -1;
        }
        float dist = (radius - hitPillar.currentHeight) - player.position.magnitude; // Dist of pillar surface from origin minus dist of player from origin
        return dist;
    }
}