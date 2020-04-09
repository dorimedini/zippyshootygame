using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshCollider))]
public class TileBehaviour : MonoBehaviour
{
    public static float maxHeightPercentage = 0.9f;
    public static float extensionDeltaPercentage = 0.1f;
    public static float timeToHeightDelta = 2f;
    public static float launchForceMultiplier = 8f; // Multiplied by the distance to the target height
    public float currentHeight;
    private float maxHeight;
    private float targetHeight;
    private float timeToTarget;
    private bool heightLocked;
    private bool extending;
    private bool primedToLaunch;
    private float extensionDelta;

    public float edge;
    public float radius;
    public float collisionExpansion;  // How much wider the collider is (percentage)

    public int id;           // Set by Geosphere generator

    public Mesh mesh;
    public Mesh collMesh;   // Collider's mesh is different than the renderer's mesh
    public bool needRedraw;
    public bool isHex;

    // Need these to handle player/rigidbody launching
    private Dictionary<int, GameObject> collidedWithTile;

    public static TileBehaviour Create(
        bool isHexagon,
        Vector3 location,
        float edgeLength,
        float r,
        float h,
        float collExpansion,
        int tileId = -1)
    {
        Object obj = isHexagon ? Resources.Load("Prefabs/HexRenderer") : Resources.Load("Prefabs/PentRenderer");
        GameObject tile_obj = Instantiate(obj, location, Quaternion.identity) as GameObject;
        TileBehaviour tile = tile_obj.GetComponent<TileBehaviour>();
        tile.isHex = isHexagon;
        tile.edge = edgeLength;
        tile.radius = r;
        tile.targetHeight = h;
        tile.currentHeight = h;
        tile.maxHeight = maxHeightPercentage * r;
        tile.extensionDelta = extensionDeltaPercentage * r;
        tile.timeToTarget = 0f;
        tile.collisionExpansion = collExpansion;
        tile.extending = false;
        tile.primedToLaunch = false;
        tile.heightLocked = false;
        tile.id = tileId;
        tile.mesh = new Mesh();
        tile.collMesh = new Mesh();
        tile.collidedWithTile = new Dictionary<int, GameObject>();
        return tile;
    }

    void Awake()
    {
        mesh = new Mesh();
        collMesh = new Mesh();
        needRedraw = true;
        extending = false;
        primedToLaunch = false;
        heightLocked = false;
        maxHeight = maxHeightPercentage * radius;
        extensionDelta = extensionDeltaPercentage * radius;
    }

    void Start()
    {
        collidedWithTile = new Dictionary<int, GameObject>();
    }

    void FixedUpdate()
    {
        if (extending)
            extendStep();
        if (needRedraw)
        {
            redraw();
            UpdateMesh();
            needRedraw = false;
        }
    }
    void UpdateMesh()
    {
        GetComponent<MeshFilter>().mesh = mesh;
        GetComponent<MeshCollider>().sharedMesh = collMesh;
    }

    void OnCollisionEnter(Collision col)
    {
        // FIXME: THIS KEEPS HAPPENING
        // I don't know why we sometimes get here with an uninitialized list... but catch it here
        if (collidedWithTile == null)
        {
            Debug.LogError(string.Format("Registered collision on tile {0} but collided list not initialized!", id));
            collidedWithTile = new Dictionary<int, GameObject>();
        }

        GameObject obj = col.gameObject;

        // We ignore projectiles. The projectile itself handles hits
        if (obj.GetComponent<Projectile>() != null) return;

        int objId = obj.GetInstanceID();
        if (obj.GetComponent<Rigidbody>() != null && !collidedWithTile.ContainsKey(objId))
            collidedWithTile[objId] = obj;
    }
    void OnCollisionExit(Collision col)
    {
        int objId = col.gameObject.GetInstanceID();
        if (collidedWithTile.ContainsKey(objId))
            collidedWithTile.Remove(objId);
    }

    public void redraw()
    {
        mesh = MeshFactory.Tile.GetMesh(currentHeight, edge, radius, isHex);
        collMesh = MeshFactory.Tile.GetMesh(currentHeight, edge, radius, isHex);
    }

    public Mesh getMesh() { return mesh; }

    // Allow parent object to control edge width
    public void setEdge(float e) { edge = e; needRedraw = true; }
    public void setRadius(float r) { radius = r; needRedraw = true; }
    public void setHeight(float h) {
        currentHeight = targetHeight = h;
        timeToTarget = 0f;
        extending = false;
        needRedraw = true;
    }

    // Call this from a projectile's OnCollisionEnter() method
    public void projectileHit()
    {
        if (heightLocked)
            return;

        // On hit, the tile gets the energy to launch rigidbodies again.
        primedToLaunch = true;

        // Compute the target height.
        // Take current height + delta. If this new height is under the maximal height, fine;
        // just use the default time-to-height.
        // If not, multiply the extension time by the ratio between the actual delta (distance
        // to max) and the default delta.
        float desiredTarget = currentHeight + extensionDelta;
        float actualTarget = desiredTarget > maxHeight ? maxHeight : desiredTarget;
        float timeMultiplier = (desiredTarget <= maxHeight ? 1 : (actualTarget - currentHeight) / extensionDelta);
        extendTo(actualTarget, timeToHeightDelta * timeMultiplier);
    }
    private void onFullExtend()
    {
        // Lock the tile
        heightLocked = true;
    }
    private void extendTo(float height, float timeToHeight) {
        extending = true;
        targetHeight = height;
        timeToTarget = timeToHeight;
    }
    private void extendStep()
    {
        // Compute height increase
        float deltaHeight;
        if (timeToTarget < 0.01)
        {
            // Snap to target when almost out of time
            deltaHeight = targetHeight - currentHeight;
            timeToTarget = 0f;
            extending = false;
        }
        else
        {
            deltaHeight = Mathf.Lerp(currentHeight, targetHeight, Mathf.Sqrt(Time.deltaTime / timeToTarget)) - currentHeight;
            timeToTarget -= Time.deltaTime;
        }
        // Don't update currentHeight yet! isOverTile uses is

        // First, get a list of Rigidbodies standing on the tile so we can
        // launch them. Only launch if height increased!
        Dictionary<int, Rigidbody> rbs = new Dictionary<int, Rigidbody>();
        if (primedToLaunch && deltaHeight > 0)
        {
            foreach (var kvp in collidedWithTile)
            {
                GameObject obj = kvp.Value;
                int objId = kvp.Key;
                Rigidbody rb = obj.GetComponent<Rigidbody>();
                if (rb == null)
                    continue;
                if (!isOverTile(rb))
                    continue;
                rbs[objId] = rb;
            }
        }

        // Update current height after checking which rigidbodies are eligible for launch.
        currentHeight += deltaHeight;

        // Launch objects
        if (primedToLaunch && deltaHeight > 0) {
            primedToLaunch = false; // Only launch objects that were on the tile WHEN IT WAS HIT
            foreach (var kvp in rbs)
            {
                Rigidbody rb = kvp.Value;
                Vector3 force = -rb.transform.position.normalized;
                force *= launchForceMultiplier * (targetHeight - currentHeight);
                rb.AddForce(force, ForceMode.Impulse);
            }
        }

        // If this hit the end of extension and we're at max height, trigger onFullExtend
        if (!extending && Tools.NearlyEqual(currentHeight, maxHeight, 0.01f))
            onFullExtend();

        // Set redraw
        needRedraw = true;
    }

    // Assumes rigidbody is collided with tile, and is just as close to the origin as the tile.
    // TODO: This could be given some more thought...
    private bool isOverTile(Rigidbody rb)
    {
        GameObject groundOfRb;
        if (!GeoPhysics.IsPlayerGrounded(rb, out groundOfRb))
            return false;
        // Return true <==> the object is grounded on this specific tile
        TileBehaviour tile = groundOfRb.GetComponent<TileBehaviour>();
        return tile != null ? tile.id == id : false;
    }

    int totalEdges() { return isHex ? 6 : 5; }
}
