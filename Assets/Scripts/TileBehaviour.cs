using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshCollider))]
public class TileBehaviour : MonoBehaviour
{
    public bool DEBUG;

    public float targetHeight;
    private float currentHeight;
    private float timeToTarget;
    private bool extending;
    public float edge;
    public float radius;
    public int id;           // Set by Geosphere generator
    public float collisionExpansion;  // How much wider the collider is (percentage)

    public Mesh mesh;
    public Mesh collMesh;   // Collider's mesh is different than the renderer's mesh
    public bool needRedraw;
    public bool isHex;

    // Need these to handle player launching
    private Dictionary<int, GameObject> collidedWithTile;
    private Dictionary<int, float> launchCooldownList;
    public const float launchCooldown = 1.5f;

    public static TileBehaviour Create(
        bool isHexagon,
        Vector3 location,
        float edgeLength,
        float r,
        float h,
        float collExpansion,
        bool debug,
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
        tile.timeToTarget = 0f;
        tile.collisionExpansion = collExpansion;
        tile.DEBUG = debug;
        tile.id = tileId;
        tile.mesh = new Mesh();
        tile.collMesh = new Mesh();
        tile.collidedWithTile = new Dictionary<int, GameObject>();
        tile.launchCooldownList = new Dictionary<int, float>();
        return tile;
    }

    void Awake()
    {
        mesh = new Mesh();
        collMesh = new Mesh();
        collidedWithTile = new Dictionary<int, GameObject>();
        launchCooldownList = new Dictionary<int, float>();
        needRedraw = true;
        extending = false;
    }

    void Start()
    {
        
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
        UpdateLaunchCooldowns();
    }
    void UpdateMesh()
    {
        GetComponent<MeshFilter>().mesh = mesh;
        GetComponent<MeshCollider>().sharedMesh = collMesh;
    }
    void UpdateLaunchCooldowns()
    {
        if (launchCooldownList.Count > 0)
        {
            var objIds = launchCooldownList.Keys.ToList();
            foreach (int objId in objIds)
            {
                launchCooldownList[objId] -= Time.deltaTime;
                if (launchCooldownList[objId] <= 0)
                    launchCooldownList.Remove(objId);
            }
        }
    }

    void OnCollisionEnter(Collision col)
    {
        GameObject obj = col.gameObject;
        int objId = obj.GetInstanceID();
        if (!collidedWithTile.ContainsKey(objId))
        {
            collidedWithTile[objId] = obj;
        }
    }
    void OnCollisionExit(Collision col)
    {
        collidedWithTile.Remove(col.gameObject.GetInstanceID());
    }

    public void redraw()
    {
        mesh = new Mesh();
        collMesh = new Mesh();
        mesh.vertices = getVertices();
        mesh.triangles = getTriangles();
        mesh.RecalculateNormals();
        mesh.MarkDynamic();
        collMesh.vertices = getVertices(true);
        collMesh.triangles = mesh.triangles;
        collMesh.RecalculateNormals();
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
    public void extendTo(float height, float timeToHeight) {
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
        // launch them. Objects that have been launched have a cooldown.
        // Only launch if height increased!
        Dictionary<int, Rigidbody> rbs = new Dictionary<int, Rigidbody>();
        if (deltaHeight > 0)
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
                if (launchCooldownList.ContainsKey(objId))
                    continue;
                rbs[objId] = rb;
            }
        }

        // Update current height after checking which rigidbodies are eligible for launch.
        currentHeight += deltaHeight;

        // Fix locations of objects (if height increases).
        if (deltaHeight > 0) {
            float force = 25f * deltaHeight;
            foreach (var kvp in rbs)
            {
                Rigidbody rb = kvp.Value;
                int objId = kvp.Key;
                Vector3 dir = -rb.transform.position.normalized;
                rb.AddForce(force * dir, ForceMode.Impulse);
                launchCooldownList[objId] = launchCooldown;
            }
        }

        // Set redraw
        needRedraw = true;
    }

    // Assumes rigidbody is collided with tile, and is just as close to the origin as the tile.
    // TODO: This could be given some more thought...
    private bool isOverTile(Rigidbody rb)
    {
        return rb.transform.position.magnitude <= radius - currentHeight + 0.1f;
    }

    int totalEdges() { return isHex ? 6 : 5; }

    // The length is the the following distance:
    //     ^-----+           _+_      ^
    //    /|      \       +--   --+   |
    //   + |l      +      \       /   |l
    //    \|      /        +-----+    v
    //     v-----+
    float getLength()
    {
        return isHex ? 
            edge * (float)System.Math.Sqrt(3) :
            getLowerLength() + getUpperLength();
    }

    // The width is the following distance:
    //   +-----+           _+_      
    //  /       \       +--   --+   
    // +         +      \       /   
    //  \       /        +-----+    
    //   +-----+                 
    //                           
    // |<---w--->|      |<--w-->|
    float getWidth()
    {
        return edge + 2 * getFloorGap();
    }
    // For pentagons, the lower length should be the distance between the floor 
    // and the mid-layer vertices:
    //     _+_
    //  +--   --+   ^
    //  \       /   | lower length
    //   +-----+    v
    // For hexagons just return half the length.
    float getLowerLength()
    {
        return isHex ? 
            getLength() * 0.5f :
            edge * (float)System.Math.Cos(getPentagonAcuteDegree());
    }

    // The upper length should be the distance between the mid-layer vertices
    // and the top of the pentagon
    //     _+_      ^
    //  +--   --+   v upper length
    //  \       /
    //   +-----+
    // For hexagons just return half the length.
    float getUpperLength()
    {
        // The acute degree is 18, our angle in question is 36 degree
        return isHex ?
            getLength() * 0.5f :
            edge * (float)System.Math.Sin(2 * getPentagonAcuteDegree());
    }

    // The floor gap should be the distance between the bottom-left corner
    // of the bounding box and the floor of the tile:
    //     _+_        +-----+
    //  +--   --+    /       \
    //  \       /   +         +
    //   +-----+     \       /
    //                +-----+
    // |-|          |-|
    float getFloorGap()
    {
        return isHex ? 
            edge * 0.5f :
            edge * (float)System.Math.Sin(getPentagonAcuteDegree());
    }

    // Converts the pentagon's 18 degree angle to Radians
    double getPentagonAcuteDegree()
    {
        return (System.Math.PI / 180) * 18;
    }

    // We want the origin of this object to be it's geometric 
    // center AT ITS BASE, and we want to compute this vector before computing
    // the points of the hexagon/pentagon.
    Vector3 getCenterOffset()
    {
        return new Vector3(edge * 0.5f + getFloorGap(), 0, getLength() * 0.5f);
    }

    // Build the vertex list.
    // Bottom layer:
    //      2-----3          __--2--__
    //     /       \        1         3
    //    1         4        \       /
    //     \       /          0-----4
    //      0-----5
    // Top layer (y=height):
    //      8-----9          __--7--__
    //     /       \        6         8
    //    7         10       \       /
    //     \       /          5-----9
    //      6-----11
    // Note that the height extends towards a point of radial distance from the tile
    // in the Y direction, not directly upwards from each point.
    Vector3[] getVertices(bool forCollider = false)
    {
        float width = getWidth();
        float length = getLength();
        float lowerLength = getLowerLength();
        float floorGap = getFloorGap();
        Vector3 centerOffset = getCenterOffset();
        Vector3 radiusUp = new Vector3(0, radius, 0);
        Vector3[] v = new Vector3[isHex ? 12 : 10];
        // Start with the bottom layer of points
        if (isHex)
        {
            v[0] = -centerOffset + new Vector3(floorGap, 0, 0);
            v[1] = -centerOffset + new Vector3(0, 0, lowerLength);
            v[2] = -centerOffset + new Vector3(floorGap, 0, length);
            v[3] = -centerOffset + new Vector3(floorGap + edge, 0, length);
            v[4] = -centerOffset + new Vector3(width, 0, lowerLength);
            v[5] = -centerOffset + new Vector3(floorGap + edge, 0, 0);
        }
        else
        {
            v[0] = -centerOffset + new Vector3(floorGap, 0, 0);
            v[1] = -centerOffset + new Vector3(0, 0, lowerLength);
            v[2] = -centerOffset + new Vector3(width * 0.5f, 0, length);
            v[3] = -centerOffset + new Vector3(width, 0, lowerLength);
            v[4] = -centerOffset + new Vector3(floorGap + edge, 0, 0);
        }
        // Add second layer in the radial direction
        for (int i = 0; i < totalEdges(); ++i)
            v[totalEdges() + i] = v[i] + currentHeight * (radiusUp - v[i]).normalized;
        // If this is for the collider, add some extra width
        if (forCollider)
        {
            int halfway = v.Length / 2;
            for (int i = 0; i < v.Length; ++i)
                v[i] += collisionExpansion * (v[i] - new Vector3(0, i < halfway ? 0 : currentHeight, 0));
        }
        return v;
    }

    int[] getTriangles()
    {
        if (isHex)
        {
            // See the picture in the comment in getVertices() to see the node numbers.
            // Hex triangulation should look like:
            //     +-------+
            //    /| \     |\
            //   + |   \   | +
            //    \|     \ |/
            //     +-------+
            return new int[] {
                // Start with the triangles forming the top view (positive y value).
                // This is just adding 6 to the node IDs on the bottom layer, and
                // each triangle should be layed out clockwise so the material is
                // displayed.
                6,7,8, 11,6,8, 11,8,9, 11,9,10,
                // For the bottom view we need to go counter-clockwise.
                0,2,1, 5,2,0,  5,3,2,  5,4,3,
                // Now, the edges. Starting with the 0-6 vertex pair and looking towards
                // the 1-7 pair it should look like:
                // 7---6   ^
                // | / |   |height
                // 1---0   v
                // Keeping in mind that we should be going clockwise and the last step
                // looks like:
                // 6---11
                // | / |
                // 0---5
                // we get:
                0,1,6,  1,7,6,
                1,2,7,  2,8,7,
                2,3,8,  3,9,8,
                3,4,9,  4,10,9,
                4,5,10, 5,11,10,
                5,0,11, 0,6,11
            };
        }
        else
        {
            // See the picture in the comment in getVertices() to see the node numbers.
            // Pentagon triangulation should look like:
            //     _+_
            //  +-- | --+
            //  \  / \  /
            //   +/---\+
            return new int[] {
                // Start with the triangles forming the top view (positive y value).
                // This is just adding 5 to the node IDs on the bottom layer, and
                // each triangle should be layed out clockwise so the material is
                // displayed.
                5,6,7, 5,7,9, 9,7,8,
                // For the bottom view we need to go counter-clockwise.
                0,2,1, 0,4,2, 4,3,2,
                // Now, the edges. Starting with the 0-5 vertex pair and looking towards
                // the 1-6 pair it should look like:
                // 6---5   ^
                // | / |   |height
                // 1---0   v
                // Keeping in mind that we should be going clockwise and the last step
                // looks like:
                // 5---9
                // | / |
                // 0---4
                // we get:
                0,1,5, 1,6,5,
                1,2,6, 2,7,6,
                2,3,7, 3,8,7,
                3,4,8, 4,9,8,
                4,0,9, 0,5,9
            };
        }
    }

}
