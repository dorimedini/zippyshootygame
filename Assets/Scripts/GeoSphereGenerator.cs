﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEditor;
using UnityEngine;

public class GeoSphereGenerator : MonoBehaviour
{
    // Materials used for the pillars
    public List<Material> hexMaterials;
    public List<Material> pentMaterials;

    // Number of lights, between 1 and 4
    public int numLights;
    public float lightRadiusMultiplier; // 0.8f?
    public float lightIntensity; // 6f?

    // The initial height of the pillars, as a percentage of the radius
    public float initialHeight;

    // Pillar's colliders are wider than their rendering mesh; this float value determines
    // how much wider (percentage of edge length)
    public float collisionExpansion;

    // The edges of the pillars vary depending on their locations on the sphere,
    // but the "base" edge length should be proportional to radius/2^EHN. Trial and error
    // gives a constant of 0.6, but we can change in the future.
    public float baseEdgeMultiplier;
    private float prevEdgeMultiplier;

    // Edge length of pillars also depend on degree.
    // Use this constant to control how intense the dependency is.
    public float edgeDegreeMultiplier;
    public float pentEdgeMultiplier;
    private float prevEdgeDegreeMultiplier;
    private float prevPentEdgeMultiplier;

    Vector3 origin { set; get; }
    float epsilon { set; get; }        // Used as an ADDITIVE "close enough" value for floats
    const float X = 0.525731112119133606f;
    const float Z = 0.850650808352039932f;
    long expectedPillars { set; get; }
    List<Vector3> spherePoints { set; get; }
    List<Vector3> pentCenters { set; get; }  // Locations of the 12 pentagons
    List<Vector3> pentCentersNormalized = new List<Vector3> {
            new Vector3(-X, 0.0f, Z), new Vector3(X, 0.0f, Z),  new Vector3(-X, 0.0f, -Z), new Vector3(X, 0.0f, -Z),
            new Vector3(0.0f, Z, X),  new Vector3(0.0f, Z, -X), new Vector3(0.0f, -Z, X),  new Vector3(0.0f, -Z, -X),
            new Vector3(Z, X, 0.0f),  new Vector3(-Z, X, 0.0f), new Vector3(Z, -X, 0.0f),  new Vector3(-Z, -X, 0.0f)
        };
    List<PillarBehaviour> pillars;             // Updated in addPillars(). First 12 items are the pentagons.
    Dictionary<(int, int), Plane> planes;  // Indexed by pentagon-index pairs, updated in initializePlanes()
    List<List<int>> neighbors;             // Lists of pillar IDs that touch the respective pillar (lists are of length 5 or 6)
    System.Random rng;

    void Awake()
    {
        prevEdgeMultiplier = baseEdgeMultiplier;
        prevEdgeDegreeMultiplier = edgeDegreeMultiplier;
        prevPentEdgeMultiplier = pentEdgeMultiplier;
        pillars = new List<PillarBehaviour>();
        planes = new Dictionary<(int, int), Plane>();
        neighbors = new List<List<int>>();
        origin = new Vector3(0, 0, 0);
        spherePoints = new List<Vector3>();
        expectedPillars = 12 +
            30 * (Tools.IntPow(2, UserDefinedConstants.EHN) - 1) +
            20 * (Tools.IntPow(2, UserDefinedConstants.EHN) - 1) * (Tools.IntPow(2, UserDefinedConstants.EHN - 1) - 1);
        epsilon = baseEdgeLength() / 100f;
        BuildSphere();
        colorPillarsByDeg();
    }

    public List<PillarBehaviour> GetPillars() { return pillars; }

    void BuildSphere()
    {
        initializeSpherePoints();   // Compute the vertex locations of the pillar midpoints
        addPillars();                 // Instantiate the pillar objects
        initializePlanes();         // Each neighboring pair of pentagons, along with the origin, define a plane
        sortPillars();                // Pentagons, then hexagons on pentagon arcs, then by distance from arc hexagons
        computeNeighborLists();     // Each pillar should know which pillars are it's neighbors
        makeFaceOrigin();           // Rotate pillars to face the origin
        addLights();                // Between 1 and 4 point lights in the arena
        spreadPillars();              // Correct slight misalignment by averaging each pillar's location by it's neighbors
        orientPillars();              // Rotate pillars so their edges align with their neighbors
        updateEdgeLengths();        // Enlarge some pillars to fill the gaps
        updatePillarIds();
    }

    // ONLY CALL AFTER SORTING PILLARS
    private void colorPillarsByDeg()
    {
        for (int deg=0; deg<=maxDegree(); ++deg)
        {
            (int start, int end) = degRange(deg);
            for (int i=start; i<end; ++i)
            {
                Material mat = pillars[i].isHex ?
                    hexMaterials[deg % hexMaterials.Count] :
                    pentMaterials[deg % pentMaterials.Count];
                pillars[i].GetComponent<MeshRenderer>().material = mat;
            }
        }
    }

    // Length of an edge of a pillar should be proportional to radius/2^EHN
    float baseEdgeLength()
    {
        return baseEdgeMultiplier * UserDefinedConstants.sphereRadius / (float)Tools.IntPow(2, UserDefinedConstants.EHN);
    }

    // Use powers of this constant to determine the effect of the degree of a pillar
    // on the edge-length multiplier
    float edgeMultiplier(int degree)
    {
        if (degree == 0) // Pentagon
            return pentEdgeMultiplier;
        return Tools.FloatPow(1f + edgeDegreeMultiplier, 2*degree);
    }

    float edgeLength(int degree)
    {
        return baseEdgeLength() * edgeMultiplier(degree);
    }

    // We need this to not place hexagons in the pentagon's places.
    // Note that we are checking non-normalized pent points:
    private bool isPentPoint(Vector3 point)
    {
        foreach (Vector3 pentPoint in pentCenters)
            if (Tools.NearlyEqual((point - pentPoint).magnitude, 0, epsilon))
                return true;
        return false;
    }

    // !Assumes the pillars[] list is initialized and sorted!
    //
    // We classify pillars by "degree", a non-negative integer.
    // Two special values are 0 and 1: degree 0 pillars are pentagons
    // and degree 1 pillars are hexagons on an arc between pentagons.
    // Other degrees k>1 describe the "distance" (in pillars) of the
    // hexagon to a degree-1 hexagon:
    //
    //                  P0
    //                H1  H1
    //              H1  H2  H1
    //            H1  H2  H2  H1
    //          H1  H2  H3  H2  H1
    //        H1  H2  H3  H3  H2  H1
    //      H1  H2  H3  H3  H3  H2  H1
    //    H1  H2  H2  H2  H2  H2  H2  H1
    //  P0  H1  H1  H1  H1  H1  H1  H1  P0
    // 
    // In the above example (with EHN=3), P and H denote 
    // pentagons or hexagons, and the number next to the letter is
    // the degree of the pillar.
    //
    // The implementation of the function just finds the specific
    // range of indexes the input is in. We use the fact that there
    // are:
    // 12 Pentagons
    // 30*(2^EHN-1) edge hexagons (deg-1)
    // 60*(2^EHN-3) deg-2 hexagons (neighbors of edge hexagons)
    // 60*(2^EHN-6) deg 3 hexagons (neighbors of deg-2 hexagons)
    // 60*(2^EHN-9) deg 4 hexagons
    // ...
    // 60*(2^EHN-3(i-1)) deg i hexagons
    // ...
    // until i satisfies 3(i-1)>=2^EHN.
    private (int, int) degRange(int deg)
    {
        int exp = (int)Tools.IntPow(2, UserDefinedConstants.EHN);
        int deg2StartIdx = 12 + 30 * (exp - 1);
        if (deg <= 1)
            return deg == 0 ? (0, 12) : (12, deg2StartIdx);
        if (3 * (deg - 1) >= exp)
            return (pillars.Count, pillars.Count);
        // Those where the easy cases. Now we need to keep track of
        // the offset of lower degree indexes. 
        int offset = deg2StartIdx;
        for (int i=2; i<deg; ++i)
        {
            offset += 60 * (exp - 3 * (i - 1));
        }
        return (offset, offset + 60 * (exp - 3 * (deg - 1)));
    }
    private int getDeg(int i)
    {
        if (i < 12) 
            return 0;
        int exp = (int)Tools.IntPow(2, UserDefinedConstants.EHN);
        int deg2StartIdx = 12 + 30 * (exp - 1);
        if (i < deg2StartIdx)
            return 1;
        int offset = deg2StartIdx;
        int nextDeg = 2;
        while (offset < pillars.Count)
        {
            offset += 60 * (exp - 3 * (nextDeg - 1));
            if (i < offset)
                return nextDeg;
            ++nextDeg;
        }
        Debug.LogError(string.Format("Index out of bounds: got i={0}, there are only {1} pillars", i, pillars.Count));
        return -1;
    }
    private int maxDegree()
    {
        return Mathf.FloorToInt((float)Tools.IntPow(2, UserDefinedConstants.EHN) / 3f + 1f);
    }

    // Some sphere points may be added several times by the subdivide algorithm.
    // Clean up the point set before adding GameObjects to them.
    private void removeDuplicates()
    {
        int duplicatesFound = 0;
        for (int i = 0; i < spherePoints.Count - 1; ++i)
            for (int j = i + 1; j < spherePoints.Count; ++j)
                if (Tools.NearlyEqual(spherePoints[i], spherePoints[j], epsilon))
                {
                    spherePoints.RemoveAt(j--);
                    ++duplicatesFound;
                }
        if (spherePoints.Count != expectedPillars)
            Debug.LogError(string.Format("Expected {0} pillars, spherePoints list contains {1} items", expectedPillars, spherePoints.Count));
    }

    private void subdivide(Vector3 v1, Vector3 v2, Vector3 v3, int depth) {
        if (depth == 0) {
            spherePoints.Add(v1 * UserDefinedConstants.sphereRadius);
            spherePoints.Add(v2 * UserDefinedConstants.sphereRadius);
            spherePoints.Add(v3 * UserDefinedConstants.sphereRadius);
            return;
        }
        Vector3 v12 = (v1 + v2).normalized;
        Vector3 v23 = (v2 + v3).normalized;
        Vector3 v31 = (v3 + v1).normalized;
        subdivide(v1, v12, v31, depth - 1);
        subdivide(v2, v23, v12, depth - 1);
        subdivide(v3, v31, v23, depth - 1);
        subdivide(v12, v23, v31, depth - 1);
    }

    private void initializeSpherePoints()
    {
        pentCenters = pentCentersNormalized;
        List<int> tindices = new List<int> {
            0, 4,  1  ,  0, 9,  4  ,  9, 5,  4  ,   4, 5, 8  ,  4, 8,  1,
            8, 10, 1  ,  8, 3, 10  ,  5, 3,  8  ,   5, 2, 3  ,  2, 7,  3,
            7, 10, 3  ,  7, 6, 10  ,  7, 11, 6  ,  11, 0, 6  ,  0, 1,  6,
            6, 1, 10  ,  9, 0, 11  ,  9, 11, 2  ,   9, 2, 5  ,  7, 2, 11
        };
        for (int i = 0; 3 * i < tindices.Count; ++i)
        {
            subdivide(
                pentCenters[tindices[i * 3 + 0]],
                pentCenters[tindices[i * 3 + 1]],
                pentCenters[tindices[i * 3 + 2]],
                UserDefinedConstants.EHN);
        }
        removeDuplicates();
        // Now lengthen the pentagon points to fit the radius as well (in the spherePoints
        // array this was taken care of).
        for (int i = 0; i < 12; i++)
            pentCenters[i] *= UserDefinedConstants.sphereRadius;
    }

    private void addPillars()
    {
        foreach (Vector3 point in spherePoints)
        {
            bool isHex = !isPentPoint(point);
            PillarBehaviour pillar = PillarBehaviour.Create(
                isHex,
                point,
                baseEdgeLength(),
                initialHeight * UserDefinedConstants.sphereRadius,
                collisionExpansion);
            setParent(pillar.gameObject);
            if (isHex)
                pillars.Add(pillar);
            else
                pillars.Insert(0, pillar);
        }
    }
    private void setParent(GameObject go) { go.transform.parent = transform; }
    private void makeFaceOrigin()
    {
        foreach (PillarBehaviour pillar in pillars)
        {
            makeFaceOrigin(pillar.gameObject);
        }
    }
    private void makeFaceOrigin(GameObject go)
    {
        go.transform.LookAt(origin);
        go.transform.Rotate(new Vector3(90, 0, 0));
    }
    private void randomizePillarHeights(float max = 0.7f, float min = 0.1f)
    {
        foreach (PillarBehaviour pillar in pillars)
        {
            pillar.setHeight(UnityEngine.Random.Range(min, max) * UserDefinedConstants.sphereRadius);
        }
    }

    // We need to keep track of all 30 planes defined by neighboring
    // pairs of pentagons
    private void initializePlanes()
    {
        planes.Clear();
        float neighborDistanceUpperBound = distanceBetweenPents() * 1.02f;
        for (int i = 0; i < 11; ++i)
            for (int j = i + 1; j < 12; ++j)
                if ((pillars[i].transform.position - pillars[j].transform.position).magnitude < neighborDistanceUpperBound)
                    planes[(i, j)] = Plane.Translate(new Plane(origin, pillars[i].transform.position, pillars[j].transform.position), origin);
        if (planes.Count != 30)
            Debug.LogError(string.Format("Uh oh... plane initializer computed {0} planes instead of 30", planes.Count));
    }

    // This will be important for the orientation phase.
    // Placing the center-points of the pillars is the easy part (or
    // at least the easily copy-pasted part); when we need to rotate
    // all pillars to reach their correct orientation we must start
    // with the pentagons, then all edge-hexes (hexagons on a line
    // between two pentagons), and then fill each "hex-traingle" 
    // inwards from the edges.
    // So, when we sort the pillars, ensure the following order:
    // 1. The 12 pentagons are first (already taken care of for us).
    // 2. The 30*(2^EHN - 1) edge hexes come next, grouped
    //    by common plane first, then by order of hexes closer to
    //    the end-pentagon with smaller index.
    //    Actually, we don't need to group these at all...
    // 3. The 20*(2^EHN- 1)*(2^(EHN-1) - 1) 
    //    inner-triangle-hexes come last, grouped by "quadrant" (only
    //    there're 12 of them) which is uniquely defined by the the
    //    30-entry binary vector denoting the hex's relative location
    //    (above or below) to each of the 30 planes. In each quadrant,
    //    sort by shortest distance to any plane.
    //    We actually don't need to group by quadrant...
    int comparePillarIndexes(int i1, int i2)
    {
        // First, if i1 is a pentagon, then there is no real order so
        // we can just compare numeric indexes (for consistency):
        if (i1 < 12)
            return i1.CompareTo(i2);
        // Now, if i2 is a pentagon, i1 is greater than i2
        if (i2 < 12)
            return 1;
        // Both are hexes. Check which one(s) are on an arc.
        // If only one is, the arc-one is less than the other.
        // If both are on arcs, sort by integer value for
        // consistency.
        bool isArc1 = isOnPentArc(i1);
        bool isArc2 = isOnPentArc(i2);
        if (isArc1 != isArc2)
            return isArc1 ? -1 : 1;
        if (isArc1) // && isArc2
            return i1.CompareTo(i2);
        // OK, both are triangular hexes. Compute there distances from
        // the closest plane: if they are almost equal, the order doesn't
        // matter, otherwise sort by distance.
        float dist1 = distanceToPlane(i1);
        float dist2 = distanceToPlane(i2);
        if (Tools.NearlyEqual(dist1, dist2, epsilon))
            return i1.CompareTo(i2);
        return dist1.CompareTo(dist2);
    }
    private void sortPillars()
    {
        List<int> sortedPillarIndexes = new List<int>();
        for (int i = 0; i < pillars.Count; ++i)
            sortedPillarIndexes.Add(i);
        sortedPillarIndexes.Sort((i1, i2) => comparePillarIndexes(i1, i2));
        pillars = sortedPillarIndexes.Select(i => pillars[i]).ToList();
        updatePillarIds();
    }
    private void updatePillarIds()
    {
        for (int i = 0; i < pillars.Count; ++i)
            pillars[i].GetComponent<PillarBehaviour>().id = i;
    }

    // Call this to set up neighbors list.
    // To do so, just sweep the pillar list (quadratic) and build each pentagon's five neighbors
    // and each hexagons' six.
    // We find the pillars that are close enough, and take the closest 5 or 6 (pentagon or hexagon).
    private void computeNeighborLists()
    {
        neighbors.Clear();
        // Pentagons
        for (int i = 0; i < pillars.Count; ++i)
        {
            int expectedNeighbors = i < 12 ? 5 : 6;
            List<int> foundNeighbors = new List<int>();
            for (int j = 0; j < pillars.Count; ++j)
            {
                if (i == j)
                    continue;
                if (areNeighbors(i, j))
                {
                    foundNeighbors.Add(j);
                }
            }
            // Clear out those farthest away if need be
            if (foundNeighbors.Count > expectedNeighbors)
            {
                foundNeighbors = foundNeighbors.OrderBy(idx => distanceBetweenPillars(i, idx)).ToList();
                foundNeighbors = foundNeighbors.Take(expectedNeighbors).ToList();
            }
            if (foundNeighbors.Count < expectedNeighbors)
                Debug.LogError(string.Format("Only {0} neighbors found for pillar {1}", foundNeighbors.Count, i));
            pillars[i].setNeighbors(foundNeighbors.Select(j => pillars[j]).ToList());
            neighbors.Add(foundNeighbors);
        }
    }

    // Do this in two parts:
    // 1. Rotate pentagons s.t. every "neighbor" pentagon's closest edge is
    //    parallel to the subject pentagon's closest edge. To do so, just
    //    start with any pentagon as the base of reference, fix it's neighbors,
    //    then their neighbors... etc.
    // 2. Rotate edge hexagons. Do so by finding one of the pentagons defining
    //    the edge, and align by it (should be good enough).
    // 3. Rotate triangle hexagons. To do so, iterate over the pillars in ascending 
    //    index order to ensure that by the time we reach hex H, at least two of 
    //    it's neighbors are orientated. Find which ones, and rotate H as the 
    //    average rotation required of each aligned neighbor (may be more than 2!).
    //    There may be issues regarding an offset of 2pi/6, but try a naive
    //    implementation and maybe it'll work
    private void orientPillars()
    {
        // Pentagons first
        fixNeighborRotation(0, 1);
        fixNeighborRotation(1, 0);
        int fixedRotations = 2;
        for (int i = 2; i < 12; ++i)
            if (Tools.NearlyEqual(distanceBetweenPillars(i - 1, i), distanceBetweenPents(), epsilon))
            {
                fixNeighborRotation(i - 1, i);
                ++fixedRotations;
            }
        // One small problem: pentagon #9 is not a neighbor of pentagon #8.
        // Thus, we need to rotate it manually via one of it's neighbors; say,
        // pentagon #1:
        fixNeighborRotation(1, 9);
        ++fixedRotations;
        if (fixedRotations != 12)
            Debug.LogError(string.Format("Pentagons not correctly rotated, missed {0}", 12 - fixedRotations));

        // Edge hexagons
        int deg = 1;
        (int start, int end) = degRange(deg);
        for (int i=start; i<end; ++i)
        {
            (int p1, int p2) = onPentArc(i);
            if (p1 < 0)
                Debug.LogError(string.Format("ID {0} identified as degree-1 hex but is not", i));
            else
                fixNeighborRotation(p1, i);
        }

        // For the rest, do them in ascending degree order. For each
        // unaligned hex, find any neighbor of strictly lower degree
        // and align by it.
        while (end < pillars.Count)
        {
            (start, end) = degRange(++deg);
            for (int i=start; i<end; ++i)
            {
                foreach (int j in neighbors[i])
                    if (j < start) // Neighbor with strictly lower degree
                    {
                        fixNeighborRotation(j, i);
                        break;
                    }
            }
        }
    }

    // Now, move the pillars so they are distributed uniformly as possible around the sphere.
    // To avoid complicated computations, I'll just go several times over all pillars, and
    // each pass move them to the average point (on the sphere) of their neighbors.
    private void spreadPillars() { spreadPillars(10); }
    private void spreadPillars(int passes)
    {
        // Do passes without pentagons; pentagons are done in one go at the end.
        for (int pass=1; pass<=passes; ++pass)
        {
            // Pentagons don't move, start from 12
            for (int i=12; i<pillars.Count; ++i)
            {
                int deg = getDeg(i);
                Vector3 avg = origin;
                foreach (int j in neighbors[i])
                {
                    // Pentagons don't move, and edge hexagons should only
                    // align by pentagons and other edge hexagons on the
                    // same arc:
                    if (deg <= 1)
                    {
                        if (getDeg(j) > 1)
                            continue;
                        if (getDeg(j) == 1 && onPentArc(i) != onPentArc(j))
                            continue;
                    }
                    avg += pillars[j].transform.position;
                }
                // Push location to the sphere. No need to actually average
                // because we're normalizing anyway.
                pillars[i].transform.position = avg.normalized * UserDefinedConstants.sphereRadius;
            }
        }
        // Pentagons
        for (int i=0; i<12; ++i)
        {
            Vector3 avg = origin;
            foreach (int j in neighbors[i])
                avg += pillars[j].transform.position;
            pillars[i].transform.position = avg.normalized * UserDefinedConstants.sphereRadius;
        }
    }

    // Dynamically set each pillars' new edge length.
    // The edge length should depend on the degree of the pillar
    // in some way; pentagons get a big boost, but after that the
    // smaller the degree the smaller the boost.
    private void updateEdgeLengths()
    {
        float baseEdge = baseEdgeLength();
        for (int i=0; i<pillars.Count; ++i)
            pillars[i].GetComponent<PillarBehaviour>().setEdge(baseEdge * edgeMultiplier(getDeg(i)));
    }

    private void addLights()
    {
        // Create colored lights. Edit list for different behaviour
        List<Color> lightColors = new List<Color>{
            Color.white,
            Color.white,
            Color.white,
            Color.white
        };
        List<GameObject> lights = new List<GameObject>();
        for (int i=0; i<numLights; ++i)
        {
            lights.Add(new GameObject(string.Format("Pointlight{0}", i)));
            Light lightComp = lights[i].AddComponent<Light>();
            lightComp.color = lightColors[i % lightColors.Count];
            lightComp.range = lightRadiusMultiplier * UserDefinedConstants.sphereRadius;
            lightComp.intensity = lightIntensity;
            lights[i].transform.parent = transform;
        }
        // Place them
        switch(numLights)
        {
            // In case of 1 light source, we need to up the range/intensity to illuminate everything
            case 1:
                lights[0].transform.position = origin;
                lights[0].GetComponent<Light>().range = 2 * UserDefinedConstants.sphereRadius;
                break;
            // For 2 or 3 lights, keep them on the XZ plane
            case 2:
                lights[0].transform.position = origin + new Vector3(0, 0, UserDefinedConstants.sphereRadius / 3);
                lights[1].transform.position = origin + new Vector3(0, 0, -UserDefinedConstants.sphereRadius / 3);
                break;
            case 3:
                float deg30 = Mathf.PI / 6f;
                float cos30 = Mathf.Cos(deg30);
                float sin30 = Mathf.Sin(deg30);
                lights[0].transform.position = origin + new Vector3(0, 0, UserDefinedConstants.sphereRadius / 2);
                lights[1].transform.position = origin + new Vector3(-(UserDefinedConstants.sphereRadius / 2) * cos30, 0, -(UserDefinedConstants.sphereRadius / 2) * sin30);
                lights[2].transform.position = origin + new Vector3(UserDefinedConstants.sphereRadius / 2 * cos30, 0, -(UserDefinedConstants.sphereRadius / 2) * sin30);
                break;
            // If we have 4 lights it's a bit more complicated - we want to form a 3D pyramid from the lights.
            // From https://en.wikipedia.org/wiki/Tetrahedron#Coordinates_for_a_regular_tetrahedron, we can adapt
            // the coordinates (sqrt(8/9), 0, -1/3), (-sqrt(2/9), sqrt(2/3), -1/3), (-sqrt(2/9), -sqrt(2/3), -1/3), (0,0,1)
            // to our radius (multiply everything by R/2):
            case 4:
                float sqrt2 = Mathf.Sqrt(2);
                float sqrt3 = Mathf.Sqrt(3);
                lights[0].transform.position = origin + (UserDefinedConstants.sphereRadius / 2f) * (new Vector3(2f / 3f * sqrt2, 0, -1f / 3f));
                lights[1].transform.position = origin + (UserDefinedConstants.sphereRadius / 2f) * (new Vector3(-sqrt2 / 3f, sqrt2 / sqrt3, -1f / 3f));
                lights[2].transform.position = origin + (UserDefinedConstants.sphereRadius / 2f) * (new Vector3(-sqrt2 / 3f, -sqrt2 / sqrt3, -1f / 3f));
                lights[3].transform.position = origin + (UserDefinedConstants.sphereRadius / 2f) * (new Vector3(0, 0, 1));
                break;
            default:
                Debug.LogError(string.Format("Got numLights={0}, valid values are 1~4", numLights));
                break;
        }
    }

    private (int, int) onPentArc(int pillarIdx)
    {
        Vector3 point = pillars[pillarIdx].transform.position;
        foreach (KeyValuePair<(int,int),Plane> entry in planes)
        {
            Plane plane = entry.Value;
            (int k1, int k2) = entry.Key;
            Vector3 pent1 = pillars[k1].transform.position;
            Vector3 pent2 = pillars[k2].transform.position;
            if (Tools.NearlyEqual(distanceToPlane(pillarIdx, entry.Key), 0, epsilon) &&
                (pent1 - point).magnitude < distanceBetweenPents() &&
                (pent2 - point).magnitude < distanceBetweenPents())
                return entry.Key;
        }
        return (-1, -1);
    }
    private bool isOnPentArc(int pillarIdx)
    {
        (int i, int _) = onPentArc(pillarIdx);
        return i >= 0;
    }

    // The distance of any point to the nearest pentagon-plane ABO for
    // some neighboring pentagons A,B and the origin O.
    // It's not enough to check proximity to the plane, because hexagons
    // in the middle of pent-triangles can still intersect a plane from
    // a pair of pentagons on the opposite side of the sphere.
    private float distanceToPlane(int pillarIdx)
    {
        Vector3 point = pillars[pillarIdx].transform.position;
        float minDist = UserDefinedConstants.sphereRadius; // INF, in practice, for any point on the sphere
        float pentDistance = distanceBetweenPents();
        foreach (KeyValuePair<(int,int), Plane> entry in planes)
        {
            // Both pentagons must be within pent-distance of the pillar, or it 
            // doesn't count.
            (int p1, int p2) = entry.Key;
            if ((point - pillars[p1].transform.position).magnitude >= pentDistance ||
                (point - pillars[p2].transform.position).magnitude >= pentDistance)
                continue;
            float dist = distanceToPlane(pillarIdx, entry.Key);
            if (dist < minDist)
                minDist = dist;
        }
        return minDist;
    }
    private float distanceToPlane(int pillarIdx, (int,int) planeKey)
    {
        return System.Math.Abs(planes[planeKey].GetDistanceToPoint(pillars[pillarIdx].transform.position));
    }

    // If two pillars are close enough to each other (say, at most the distance between
    // two pentagons), we use this method to rotate one to face the other.
    // 
    // Say we fix some pentagon A and a neighbor pentagon B, and we want to rotate
    // B so it has a flat edge towards A. In a pentagon's local space:
    //       ^
    //       | Z
    //       |
    //      -+-
    //  +--/ | \--+
    //  \    |    /
    //   |   +---+----->  X (red, right)
    //   +-------+
    // Think of A,B as vectors from the origin. From the point of view of the origin
    // we can visualize A,B and AxB:
    //
    //    A                   B
    //                 |
    //                 | AxB
    //                 v
    // What we're aiming for is the following orientation of the X axis for B:
    //
    //                        ^
    //                        | X
    //    A                   B
    //                 |
    //                 | AxB
    //                 v
    // So, measure T as the angle between AxB and B.X, and rotate B by 180 - T
    // around B's Y axis.
    private void fixNeighborRotation(int sourcePillarIdx, int neighborPillarIdx)
    {
        PillarBehaviour A = pillars[sourcePillarIdx];
        PillarBehaviour B = pillars[neighborPillarIdx];
        Vector3 vA = A.transform.position;
        Vector3 vB = B.transform.position;
        Vector3 BX = B.transform.right;
        Vector3 BY = B.transform.up;
        Vector3 vAxB = Vector3.Cross(vA, vB);
        float targetDeg = 0.0f;
        float rotation = 180f - Vector3.SignedAngle(vAxB, BX, BY);
        rotation = Tools.NearlyEqual(rotation, targetDeg, 0.01f) ? 0.0f : rotation;
        B.transform.RotateAround(vB, BY, rotation);
    }

    private float distanceBetweenPillars(int i1, int i2)
    {
        return (pillars[i1].transform.position - pillars[i2].transform.position).magnitude;
    }

    // A distance threshold between two neighboring pillars.
    // THIS DOES NOT GUARANTEE pillars of this distance apart are neighbors, but it's
    // not a bad filter.
    private float neighborPillarDistance()
    {
        return 3f * baseEdgeLength();
    }
    private bool areNeighbors(int i1, int i2) {
        return (pillars[i1].transform.position - pillars[i2].transform.position).magnitude <= neighborPillarDistance();
    }

    // Assumes the pillars[] list is initialized with the pentagons as the first 12 elements.
    private float distanceBetweenPents()
    {
        return (pillars[0].transform.position - pillars[1].transform.position).magnitude;
    }

    // Update is called once per frame
    void Update()
    {
        if (baseEdgeMultiplier != prevEdgeMultiplier || 
            edgeDegreeMultiplier != prevEdgeDegreeMultiplier ||
            prevPentEdgeMultiplier != pentEdgeMultiplier)
        {
            updateEdgeLengths();
            prevEdgeMultiplier = baseEdgeMultiplier;
            prevEdgeDegreeMultiplier = edgeDegreeMultiplier;
            prevPentEdgeMultiplier = pentEdgeMultiplier;
        }
    }
}
