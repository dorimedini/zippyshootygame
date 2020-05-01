using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshCollider))]
public class PillarBehaviour : MonoBehaviour
{
    public GameObject steamPrefab;
    public AudioClip extensionSoundSteam;
    public AudioClip[] extensionSoundScreech;

    public static float maxHeightPercentage = 0.9f;
    public static float extensionDeltaPercentage = 0.1f;
    public static float timeToHeightDelta = 2f;
    public float currentHeight;
    private float maxHeight;
    private float targetHeight;
    private float timeToTarget;
    private bool heightLocked;
    private bool extending;
    private bool primedToLaunch;
    private float extensionDelta;

    private List<PillarBehaviour> neighbors;

    private GameObject player = null;    // So the pillar can launch the player if need be

    public float edge;
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

    public static PillarBehaviour Create(
        bool isHexagon,
        Vector3 location,
        float edgeLength,
        float h,
        float collExpansion,
        int pillarId = -1)
    {
        Object obj = isHexagon ? Resources.Load("Prefabs/Hexagon") : Resources.Load("Prefabs/Pentagon");
        GameObject pillarObj = Instantiate(obj, location, Quaternion.identity) as GameObject;
        PillarBehaviour pillar = pillarObj.GetComponent<PillarBehaviour>();
        pillar.isHex = isHexagon;
        pillar.edge = edgeLength;
        pillar.targetHeight = h;
        pillar.currentHeight = h;
        pillar.maxHeight = maxHeightPercentage * UserDefinedConstants.sphereRadius;
        pillar.extensionDelta = extensionDeltaPercentage * UserDefinedConstants.sphereRadius;
        pillar.timeToTarget = 0f;
        pillar.collisionExpansion = collExpansion;
        pillar.extending = false;
        pillar.primedToLaunch = false;
        pillar.heightLocked = false;
        pillar.id = pillarId;
        pillar.mesh = new Mesh();
        pillar.collMesh = new Mesh();
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
        maxHeight = maxHeightPercentage * UserDefinedConstants.sphereRadius;
        extensionDelta = extensionDeltaPercentage * UserDefinedConstants.sphereRadius;
    }

    void Start()
    {
        needRedraw = true;
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
        Vector3 origin = new Vector3(0, UserDefinedConstants.sphereRadius, 0);
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
        GameObject obj = col.gameObject;

        // We ignore projectiles. The projectile itself handles hits
        if (obj.GetComponent<Projectile>() != null) return;

        // If this is a player, and it's OUR player, we need to store the movement script so if this pillar extends
        // we can call launch()
        var netchar = obj.GetComponent<NetworkCharacter>();
        if (netchar != null && netchar.photonView.IsMine)
            player = obj;
    }
    void OnCollisionExit(Collision col)
    {
        // Register player exiting the pillar so we don't launch
        GameObject obj = col.gameObject;
        var netchar = obj.GetComponent<NetworkCharacter>();
        if (netchar != null && netchar.photonView.IsMine)
            player = null;
    }

    public Mesh getMesh() { return mesh; }

    // Allow parent object to control edge width
    public void setEdge(float e) { edge = e; needRedraw = true; }
    public void setHeight(float h) {
        currentHeight = targetHeight = h;
        timeToTarget = 0f;
        extending = false;
        needRedraw = true;
    }
    public void setNeighbors(List<PillarBehaviour> neighbors) { this.neighbors = neighbors; }

    // Call this from PillarExtensionController
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
        if (actualTarget > currentHeight + 0.01f)
        {
            extensionFX();
        }
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

        // Check if we should launch our local player
        if (primedToLaunch &&
            deltaHeight > 0 &&
            player != null &&
            isOverPillar(player.GetComponent<Rigidbody>())
            ) {
            player.GetComponent<PlayerMovementController>().LaunchFromPillar(id, targetHeight - currentHeight);
            primedToLaunch = false; // Only launch objects that were on the pillar WHEN IT WAS HIT
        }

        // Update current height after checking which rigidbodies are eligible for launch.
        currentHeight += deltaHeight;

        // If this hit the end of extension and we're at max height, trigger onFullExtend
        if (!extending && Tools.NearlyEqual(currentHeight, maxHeight, 0.01f))
            onFullExtend();

        // Set redraw
        needRedraw = true;
    }
    private void extensionFX()
    {
        // We want to blow steam out from each exposed base side of the extending pillar.
        // To do so, we need the neighbor set of the pillar S, and for every neighbor pillar P in S we check if our current height
        // is greater than P's current height. If so, we blow steam off the top-base edge of P touching us.
        PillarBehaviour lowestNeighbor = null;
        foreach (PillarBehaviour neighbor in neighbors)
        {
            if (neighbor.currentHeight > currentHeight + 1)
                continue;
            // We'll play a sound from the height of the lowest-height neighbor (if we're not surrounded by higher neighbors).
            if (lowestNeighbor == null || lowestNeighbor.currentHeight > neighbor.currentHeight)
            {
                lowestNeighbor = neighbor;
            }
            addSteamNextToNeighbor(neighbor);
        }
        // Now for sounds. Set the sound origin to be at the lowest height of a neighbor, or our height if no neighbors are lower.
        float soundHeight = lowestNeighbor == null ? currentHeight : lowestNeighbor.currentHeight;
        AudioSource.PlayClipAtPoint(extensionSoundSteam, at);
        AudioSource.PlayClipAtPoint(extensionSoundScreech[Mathf.FloorToInt(Random.Range(0, extensionSoundScreech.Length - 0.01f))], at);
    }
    private void addSteamNextToNeighbor(PillarBehaviour neighbor)
    {
        // To compute the steam location for neighbor P, let H be the current height of P and let v1,v2 the points at distance radius-H
        // over P and over this pillar, respectively. Then, the steam origin should be at around (v1+v2)/2 (maybe with slight offset) and
        // should face in direction (-v1.normalized + (v1-v2).normalized).
        Vector3 v1 = neighbor.transform.position - (neighbor.currentHeight * neighbor.transform.position.normalized);
        Vector3 v2 = transform.position - (neighbor.currentHeight * transform.position.normalized);
        float neighborPreferenceOffset = 0.7f;
        Vector3 startPos = neighborPreferenceOffset * v1 + (1- neighborPreferenceOffset) * v2;
        Vector3 lookDir = startPos - v1.normalized + (v1 - v2).normalized;
        var steam = Instantiate(steamPrefab, startPos, Quaternion.identity);
        steam.transform.LookAt(lookDir);
        Destroy(steam, steam.GetComponent<ParticleSystem>().main.duration * 5);
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
