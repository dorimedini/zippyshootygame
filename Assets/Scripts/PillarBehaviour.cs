using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshCollider))]
public class PillarBehaviour : MonoBehaviour
{
    public static float maxHeightPercentage = 0.9f;
    public static float extensionDeltaPercentage = 0.1f;
    public static float timeToHeightDelta = 2f;
    public static float launchForceMultiplier = 7f; // Multiplied by the distance to the target height
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

    // This dictionary maps indices in the mesh.vertices list to integers better describing the vertices'
    // 'function'; i.e. for hexagons, mesh.vertices[i] is a bottom-base vertex <==> meshVertexMap[i] is
    // one of 0,1,2,3,4,5.
    // Different keys may map to the same value! At time of writing, imported hexagons have 36 vertices,
    // but only 12 distinct vertices, so each number in 0,1,...,11 exists as a value exactly 3 times in
    // the resulting dictionary.
    private Dictionary<int, int> meshVertexMap;

    public bool isHex;

    // Need these to handle player/rigidbody launching
    private Dictionary<int, GameObject> collidedWithPillar;

    public static PillarBehaviour Create(
        bool isHexagon,
        Vector3 location,
        float edgeLength,
        float r,
        float h,
        float collExpansion,
        int pillarId = -1)
    {
        Object obj = isHexagon ? Resources.Load("Prefabs/Hexagon") : Resources.Load("Prefabs/Pentagon");
        GameObject pillarObj = Instantiate(obj, location, Quaternion.identity) as GameObject;
        PillarBehaviour pillar = pillarObj.GetComponent<PillarBehaviour>();
        pillar.isHex = isHexagon;
        pillar.edge = edgeLength;
        pillar.radius = r;
        pillar.targetHeight = h;
        pillar.currentHeight = h;
        pillar.maxHeight = maxHeightPercentage * r;
        pillar.extensionDelta = extensionDeltaPercentage * r;
        pillar.timeToTarget = 0f;
        pillar.collisionExpansion = collExpansion;
        pillar.extending = false;
        pillar.primedToLaunch = false;
        pillar.heightLocked = false;
        pillar.id = pillarId;
        pillar.mesh = new Mesh();
        pillar.collMesh = new Mesh();
        pillar.collidedWithPillar = new Dictionary<int, GameObject>();
        pillar.needRedraw = true;
        pillar.meshVertexMap = new Dictionary<int, int>();
        return pillar;
    }

    void Awake()
    {
        mesh = new Mesh();
        collMesh = new Mesh();
        meshVertexMap = new Dictionary<int, int>();
        needRedraw = true;
        extending = false;
        primedToLaunch = false;
        heightLocked = false;
        maxHeight = maxHeightPercentage * radius;
        extensionDelta = extensionDeltaPercentage * radius;
    }

    void Start()
    {
        needRedraw = true;
        collidedWithPillar = new Dictionary<int, GameObject>();
        meshVertexMap = new Dictionary<int, int>();
        InitPillar();
        UpdateMesh();
    }

    /** Mesh imported from blender prefab requires some initial analysis and fixups to be workable */
    private void InitPillar()
    {
        // Build the vertex dict
        meshVertexMap.Clear();
        int nEdges = totalEdges();
        List<Vector3> v = new List<Vector3>(GetComponent<MeshFilter>().mesh.vertices);
        for (int i=0; i<v.Count; ++i)
        {
            // To determine the index of a vertex by it's X,Y,Z coordinates, it matters if the pillar is
            // a pentagon or hexagon.
            // Both hexagon and pentagon vertices have Y coordinates equal to 0 or 2 (approximately).
            // These are the bottom / top bases, respectively. The XZ coordinates differ between the
            // shapes:
            // * For hexagons, the X value comes from {0,+-0.9} and the Z value comes from {+-1,+-0.5}:
            //   (X,Z) is in {(0,-1),(-0.9,-0.5),(-0.9,0.5),(0,1),(0.9,0.5),(0.9,-0.5)}
            // * For pentagons, the X value is in {0,+-1,+-0.6} and the Z value comes from {-1,-0.3,0.8}:
            //   (X,Z) is in {(0,-1),(-1,-0.3),(-0.6,0.8),(0.6,0.8),(1,-0.3)}
            //
            // XZ map:               (0,1)                        (-0.6,0.8)  (0.6,0.8)
            //            (-0.9,0.5)       (0.9,0.5)
            //
            //           (-0.9,-0.5)       (0.9,-0.5)          (-1,-0.3)          (1,-0.3)
            //                       (0,-1)                               (0,-1)
            //
            // Map (bottom) to:
            //                         0                              2          3
            //                  5             1
            //
            //                  4             2                    1                 4
            //                         3                                    0
            // For top, add nEdges.
            float X = v[i].x, Y = v[i].y, Z = v[i].z;
            if (isHex)
            {
                if (Tools.NearlyEqual(X, 0, 0.1f))
                    meshVertexMap[i] = Z > 0 ? 0 : 3;
                else if (Tools.NearlyEqual(X, 0.9f, 0.1f))
                    meshVertexMap[i] = Z > 0 ? 1 : 2;
                else
                    meshVertexMap[i] = Z > 0 ? 5 : 4;
            }
            else
            {
                if (Tools.NearlyEqual(Z, 0.8f, 0.1f))
                    meshVertexMap[i] = X > 0 ? 3 : 2;
                else if (Tools.NearlyEqual(Z, -0.3f, 0.1f))
                    meshVertexMap[i] = X > 0 ? 4 : 1;
                else // Z == -1, X == 0
                    meshVertexMap[i] = 0;
            }

            // Modify mapped value if this is the top base
            if (Y > 1)
                meshVertexMap[i] += nEdges;
        }
        // Next, do an initial Y rotation of all vertices because rotation data doesn't seem to survive
        // the trip from blender...
        List<Vector3> rotated = new List<Vector3>();
        for (int i = 0; i < v.Count; ++i)
            rotated.Add(Quaternion.Euler(0, isHex ? 90 : 180, 0) * v[i]);
        SetupMeshWithVertices(rotated);
    }

    private void UpdateMesh()
    {
        /**
         * Use the mesh-vertex dictionary to re-compute the vectors describing the mesh.
         *
         * All base vertices need to be stretched the size
         */
        int nEdges = totalEdges();
        Mesh currentMesh = GetComponent<MeshFilter>().mesh;
        Vector3[] v = currentMesh.vertices;
        List<Vector3> distinctVerts = new List<Vector3>(new Vector3[2 * nEdges]);
        // Populate the distinct verts list in order (bottom base, top base)
        for (int i = 0; i < v.Length; ++i)
            distinctVerts[meshVertexMap[i]] = v[i];
        float currentEdge = (distinctVerts[0] - distinctVerts[1]).magnitude;
        // Extend each bottom base vector by edge ratio
        for (int i = 0; i < nEdges; ++i)
            distinctVerts[i] *= (edge / currentEdge);
        // Now, set each top vector to the correct height and on the radial direction from
        // it's bottom-base counterpart
        Vector3 origin = new Vector3(0, radius, 0);
        for (int i = nEdges; i < 2 * nEdges; ++i)
        {
            Vector3 radialDirection = (origin - distinctVerts[i - nEdges]).normalized;
            distinctVerts[i] = distinctVerts[i - nEdges] + (radialDirection * currentHeight);
        }
        // Update the original mesh
        List<Vector3> newVerts = new List<Vector3>();
        for (int i = 0; i < v.Length; ++i)
            newVerts.Add(distinctVerts[meshVertexMap[i]]);
        SetupMeshWithVertices(newVerts);
    }

    private void SetupMeshWithVertices(List<Vector3> v)
    {
        GetComponent<MeshFilter>().mesh.SetVertices(v.ToArray());
        GetComponent<MeshFilter>().mesh.RecalculateBounds();
        GetComponent<MeshFilter>().mesh.RecalculateNormals();
        GetComponent<MeshFilter>().mesh.MarkDynamic();
        GetComponent<MeshCollider>().sharedMesh = GetComponent<MeshFilter>().mesh;
    }

    void FixedUpdate()
    {
        if (extending)
            extendStep();
        if (needRedraw)
        {
            UpdateMesh();
            needRedraw = false;
        }
    }

    void OnCollisionEnter(Collision col)
    {
        // FIXME: THIS KEEPS HAPPENING
        // I don't know why we sometimes get here with an uninitialized list... but catch it here
        if (collidedWithPillar == null)
        {
            Debug.LogError(string.Format("Registered collision on pillar {0} but collided list not initialized!", id));
            collidedWithPillar = new Dictionary<int, GameObject>();
        }

        GameObject obj = col.gameObject;

        // We ignore projectiles. The projectile itself handles hits
        if (obj.GetComponent<Projectile>() != null) return;

        int objId = obj.GetInstanceID();
        if (obj.GetComponent<Rigidbody>() != null && !collidedWithPillar.ContainsKey(objId))
            collidedWithPillar[objId] = obj;
    }
    void OnCollisionExit(Collision col)
    {
        // FIXME: THIS KEEPS HAPPENING
        // I don't know why we sometimes get here with an uninitialized list... but catch it here
        if (collidedWithPillar == null)
        {
            Debug.LogError(string.Format("Exited collision on pillar {0} but collided list not initialized!", id));
            collidedWithPillar = new Dictionary<int, GameObject>();
        }

        int objId = col.gameObject.GetInstanceID();
        if (collidedWithPillar.ContainsKey(objId))
            collidedWithPillar.Remove(objId);
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

        // On hit, the pillar gets the energy to launch rigidbodies again.
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
        // Lock the pillar
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
        // Don't update currentHeight yet! isOverPillar uses is

        // First, get a list of Rigidbodies standing on the pillar so we can
        // launch them. Only launch if height increased!
        Dictionary<int, Rigidbody> rbs = new Dictionary<int, Rigidbody>();
        if (primedToLaunch && deltaHeight > 0)
        {
            foreach (var kvp in collidedWithPillar)
            {
                GameObject obj = kvp.Value;
                int objId = kvp.Key;
                Rigidbody rb = obj.GetComponent<Rigidbody>();
                if (rb == null)
                    continue;
                if (!isOverPillar(rb))
                    continue;
                rbs[objId] = rb;
            }
        }

        // Update current height after checking which rigidbodies are eligible for launch.
        currentHeight += deltaHeight;

        // Launch objects
        if (primedToLaunch && deltaHeight > 0) {
            primedToLaunch = false; // Only launch objects that were on the pillar WHEN IT WAS HIT
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

    // Assumes rigidbody is collided with pillar, and is just as close to the origin as the pillar.
    // TODO: This could be given some more thought...
    private bool isOverPillar(Rigidbody rb)
    {
        PillarBehaviour underneath;
        if (!GeoPhysics.IsPlayerGrounded(rb, out underneath))
            return false;
        // Return true <==> the object is grounded on this specific pillar
        return underneath != null ? underneath.id == id : false;
    }

    int totalEdges() { return isHex ? 6 : 5; }
}
