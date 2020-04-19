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
        TileBehaviour dud;
        return IsPlayerGrounded(player, out dud);
    }
    public static bool IsPlayerGrounded(Rigidbody player, out TileBehaviour hitTile)
    {
        float dist = DistanceFromGround(player, out hitTile);
        return hitTile == null ? false : dist < 0.1f;
    }

    public static float DistanceFromGround(Rigidbody player)
    {
        TileBehaviour dud;
        return DistanceFromGround(player, out dud);
    }
    public static float DistanceFromGround(Rigidbody player, out TileBehaviour hitTile)
    {
        RaycastHit rHit;
        hitTile = null;
        if (!Physics.Raycast(player.transform.position + player.transform.up, -player.transform.up, out rHit))
        {
            // FIXME Wrap the sphere with solid ground "under" the tiles so this raycast has something to hit
            // FIXME when the player is in between tiles
            return -1;
        }
        hitTile = rHit.collider.gameObject.GetComponent<TileBehaviour>();
        if (hitTile == null)
        {
            Debug.LogError(string.Format("Raycast didn't get a tile hit! Hit {0} instead", rHit.collider.gameObject.name));
            return -1;
        }
        float dist = (hitTile.radius - hitTile.currentHeight) - player.position.magnitude; // Dist of tile surface from origin minus dist of player from origin
        return dist;
    }
}