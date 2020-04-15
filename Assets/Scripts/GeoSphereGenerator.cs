using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEditor;
using UnityEngine;

public class GeoSphereGenerator : MonoBehaviour
{
    // Materials used for the tiles
    public List<Material> hexMaterials;
    public List<Material> pentMaterials;

    // Positive integer. Controls how many tile center-points the geodesic sphere has.
    // In the end there will be:
    // 12 pentagons
    // 30*(2^EHN-1) edge hexes
    // 20*(2^EHN-1)(2^(EHN-1)-1) triangle hexes
    public int expHexNumber;

    // Radius of the sphere - distance from the center to any center of any tile.
    public float radius;

    // Number of lights, between 1 and 4
    public int numLights;
    public float lightRadiusMultiplier; // 0.8f?
    public float lightIntensity; // 6f?

    // The initial height of the tiles, as a percentage of the radius
    public float initialHeight;

    // Tile's colliders are wider than their rendering mesh; this float value determines
    // how much wider (percentage of edge length)
    public float collisionExpansion;

    // The edges of the tiles vary depending on their locations on the sphere,
    // but the "base" edge length should be proportional to radius/2^EHN. Trial and error
    // gives a constant of 0.6, but we can change in the future.
    public float baseEdgeMultiplier;
    private float prevEdgeMultiplier;

    // Edge length of tiles also depend on degree.
    // Use this constant to control how intense the dependency is.
    public float edgeDegreeMultiplier;
    public float pentEdgeMultiplier;
    private float prevEdgeDegreeMultiplier;
    private float prevPentEdgeMultiplier;

    Vector3 origin { set; get; }
    float epsilon { set; get; }        // Used as an ADDITIVE "close enough" value for floats
    const float X = 0.525731112119133606f;
    const float Z = 0.850650808352039932f;
    long expectedTiles { set; get; }
    List<Vector3> spherePoints { set; get; }
    List<Vector3> pentCenters { set; get; }  // Locations of the 12 pentagons
    List<Vector3> pentCentersNormalized = new List<Vector3> {
            new Vector3(-X, 0.0f, Z), new Vector3(X, 0.0f, Z),  new Vector3(-X, 0.0f, -Z), new Vector3(X, 0.0f, -Z),
            new Vector3(0.0f, Z, X),  new Vector3(0.0f, Z, -X), new Vector3(0.0f, -Z, X),  new Vector3(0.0f, -Z, -X),
            new Vector3(Z, X, 0.0f),  new Vector3(-Z, X, 0.0f), new Vector3(Z, -X, 0.0f),  new Vector3(-Z, -X, 0.0f)
        };
    List<TileBehaviour> tiles;             // Updated in addTiles(). First 12 items are the pentagons.
    Dictionary<(int, int), Plane> planes;  // Indexed by pentagon-index pairs, updated in initializePlanes()
    List<List<int>> neighbors;             // Lists of tile IDs that touch the respective tile (lists are of length 5 or 6)
    System.Random rng;

    void Awake()
    {
        prevEdgeMultiplier = baseEdgeMultiplier;
        prevEdgeDegreeMultiplier = edgeDegreeMultiplier;
        prevPentEdgeMultiplier = pentEdgeMultiplier;
        tiles = new List<TileBehaviour>();
        planes = new Dictionary<(int, int), Plane>();
        neighbors = new List<List<int>>();
        origin = new Vector3(0, 0, 0);
        spherePoints = new List<Vector3>();
        expectedTiles = 12 +
            30 * (Tools.IntPow(2, expHexNumber) - 1) +
            20 * (Tools.IntPow(2, expHexNumber) - 1) * (Tools.IntPow(2, expHexNumber - 1) - 1);
        epsilon = baseEdgeLength() / 100f;
    }

    // Start is called before the first frame update
    void Start()
    {
        BuildSphere();
        colorTilesByDeg();
    }

    public List<TileBehaviour> GetTiles() { return tiles; }

    void BuildSphere()
    {
        initializeSpherePoints();   // Compute the vertex locations of the tile midpoints
        addTiles();                 // Instantiate the tile objects
        initializePlanes();         // Each neighboring pair of pentagons, along with the origin, define a plane
        sortTiles();                // Pentagons, then hexagons on pentagon arcs, then by distance from arc hexagons
        computeNeighborLists();     // Each tile should know which tiles are it's neighbors
        makeFaceOrigin();           // Rotate tiles to face the origin
        spreadTiles();              // Correct slight misalignment by averaging each tile's location by it's neighbors
        orientTiles();              // Rotate tiles so their edges align with their neighbors
        updateEdgeLengths();        // Enlarge some tiles to fill the gaps
        addLights();                // Between 1 and 4 point lights in the arena
    }

    // ONLY CALL AFTER SORTING TILES
    private void colorTilesByDeg()
    {
        for (int deg=0; deg<=maxDegree(); ++deg)
        {
            (int start, int end) = degRange(deg);
            for (int i=start; i<end; ++i)
            {
                Material mat = tiles[i].isHex ?
                    hexMaterials[deg % hexMaterials.Count] :
                    pentMaterials[deg % pentMaterials.Count];
                tiles[i].GetComponent<MeshRenderer>().material = mat;
            }
        }
    }

    // Length of an edge of a tile should be proportional to radius/2^EHN
    float baseEdgeLength()
    {
        return baseEdgeMultiplier * radius / (float)Tools.IntPow(2, expHexNumber);
    }

    // Use powers of this constant to determine the effect of the degree of a tile
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

    // !Assumes the tiles[] list is initialized and sorted!
    //
    // We classify tiles by "degree", a non-negative integer.
    // Two special values are 0 and 1: degree 0 tiles are pentagons
    // and degree 1 tiles are hexagons on an arc between pentagons.
    // Other degrees k>1 describe the "distance" (in tiles) of the
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
    // In the above example (with expHexNumber=3), P and H denote 
    // pentagons or hexagons, and the number next to the letter is
    // the degree of the tile.
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
        int exp = (int)Tools.IntPow(2, expHexNumber);
        int deg2StartIdx = 12 + 30 * (exp - 1);
        if (deg <= 1)
            return deg == 0 ? (0, 12) : (12, deg2StartIdx);
        if (3 * (deg - 1) >= exp)
            return (tiles.Count, tiles.Count);
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
        int exp = (int)Tools.IntPow(2, expHexNumber);
        int deg2StartIdx = 12 + 30 * (exp - 1);
        if (i < deg2StartIdx)
            return 1;
        int offset = deg2StartIdx;
        int nextDeg = 2;
        while (offset < tiles.Count)
        {
            offset += 60 * (exp - 3 * (nextDeg - 1));
            if (i < offset)
                return nextDeg;
            ++nextDeg;
        }
        Debug.LogError(string.Format("Index out of bounds: got i={0}, there are only {1} tiles", i, tiles.Count));
        return -1;
    }
    private int maxDegree()
    {
        return Mathf.FloorToInt((float)Tools.IntPow(2, expHexNumber) / 3f + 1f);
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
        Debug.Log(string.Format("Found and removed {0} duplicate points", duplicatesFound));
        if (spherePoints.Count != expectedTiles)
            Debug.LogError(string.Format("Expected {0} tiles, spherePoints list contains {1} items", expectedTiles, spherePoints.Count));
    }

    private void subdivide(Vector3 v1, Vector3 v2, Vector3 v3, int depth) {
        if (depth == 0) {
            //Debug.Log(string.Format("Adding points {0},{1},{2}", v1 * radius, v2 * radius, v3 * radius));
            spherePoints.Add(v1 * radius);
            spherePoints.Add(v2 * radius);
            spherePoints.Add(v3 * radius);
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
        Debug.Log("Got pentagon points: " + string.Join(",", pentCenters));
        List<int> tindices = new List<int> {
            0, 4,  1  ,  0, 9,  4  ,  9, 5,  4  ,   4, 5, 8  ,  4, 8,  1,
            8, 10, 1  ,  8, 3, 10  ,  5, 3,  8  ,   5, 2, 3  ,  2, 7,  3,
            7, 10, 3  ,  7, 6, 10  ,  7, 11, 6  ,  11, 0, 6  ,  0, 1,  6,
            6, 1, 10  ,  9, 0, 11  ,  9, 11, 2  ,   9, 2, 5  ,  7, 2, 11
        };
        for (int i = 0; 3 * i < tindices.Count; ++i)
        {
            //Debug.Log(string.Format("Calling subdivide on {0},{1},{2}", pentCenters[tindices[i * 3 + 0]], pentCenters[tindices[i * 3 + 1]], pentCenters[tindices[i * 3 + 2]]));
            subdivide(
                pentCenters[tindices[i * 3 + 0]],
                pentCenters[tindices[i * 3 + 1]],
                pentCenters[tindices[i * 3 + 2]],
                expHexNumber);
        }
        removeDuplicates();
        // Now lengthen the pentagon points to fit the radius as well (in the spherePoints
        // array this was taken care of).
        for (int i = 0; i < 12; i++)
            pentCenters[i] *= radius;
    }

    private void addTiles()
    {
        foreach (Vector3 point in spherePoints)
        {
            bool isHex = !isPentPoint(point);
            TileBehaviour tile = TileBehaviour.Create(
                isHex, 
                point, 
                baseEdgeLength(), 
                radius, 
                initialHeight * radius, 
                collisionExpansion);
            setParent(tile.gameObject);
            if (isHex)
                tiles.Add(tile);
            else
                tiles.Insert(0, tile);
        }
    }
    private void setParent(GameObject go) { go.transform.parent = transform; }
    private void makeFaceOrigin()
    {
        foreach (TileBehaviour tile in tiles)
        {
            makeFaceOrigin(tile.gameObject);
        }
    }
    private void makeFaceOrigin(GameObject go)
    {
        go.transform.LookAt(origin);
        go.transform.Rotate(new Vector3(90, 0, 0));
    }
    private void randomizeTileHeights(float max = 0.7f, float min = 0.1f)
    {
        foreach (TileBehaviour tile in tiles)
        {
            tile.setHeight(UnityEngine.Random.Range(min, max) * radius);
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
                if ((tiles[i].transform.position - tiles[j].transform.position).magnitude < neighborDistanceUpperBound)
                    planes[(i, j)] = Plane.Translate(new Plane(origin, tiles[i].transform.position, tiles[j].transform.position), origin);
        if (planes.Count != 30)
            Debug.LogError(string.Format("Uh oh... plane initializer computed {0} planes instead of 30", planes.Count));
    }

    // This will be important for the orientation phase.
    // Placing the center-points of the tiles is the easy part (or
    // at least the easily copy-pasted part); when we need to rotate
    // all tiles to reach their correct orientation we must start
    // with the pentagons, then all edge-hexes (hexagons on a line
    // between two pentagons), and then fill each "hex-traingle" 
    // inwards from the edges.
    // So, when we sort the tiles, ensure the following order:
    // 1. The 12 pentagons are first (already taken care of for us).
    // 2. The 30*(2^expHexNumber - 1) edge hexes come next, grouped
    //    by common plane first, then by order of hexes closer to
    //    the end-pentagon with smaller index.
    //    Actually, we don't need to group these at all...
    // 3. The 20*(2^expHexNumber - 1)*(2^(expHexNumber-1) - 1) 
    //    inner-triangle-hexes come last, grouped by "quadrant" (only
    //    there're 12 of them) which is uniquely defined by the the
    //    30-entry binary vector denoting the hex's relative location
    //    (above or below) to each of the 30 planes. In each quadrant,
    //    sort by shortest distance to any plane.
    //    We actually don't need to group by quadrant...
    int compareTileIndexes(int i1, int i2)
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
    private void sortTiles()
    {
        List<int> sortedTileIndexes = new List<int>();
        for (int i = 0; i < tiles.Count; ++i)
            sortedTileIndexes.Add(i);
        sortedTileIndexes.Sort((i1, i2) => compareTileIndexes(i1, i2));
        tiles = sortedTileIndexes.Select(i => tiles[i]).ToList();
        updateTileIds();
    }
    private void updateTileIds()
    {
        for (int i = 0; i < tiles.Count; ++i)
            tiles[i].GetComponent<TileBehaviour>().id = i;
    }

    // Call this to set up neighbors list.
    // To do so, just sweep the tile list (quadratic) and build each pentagon's five neighbors
    // and each hexagons' six.
    // We find the tiles that are close enough, and take the closest 5 or 6 (pentagon or hexagon).
    private void computeNeighborLists()
    {
        neighbors.Clear();
        Vector3 source, candidate;
        // Pentagons
        for (int i = 0; i < tiles.Count; ++i)
        {
            int expectedNeighbors = i < 12 ? 5 : 6;
            List<int> foundNeighbors = new List<int>();
            for (int j = 0; j < tiles.Count; ++j)
            {
                if (i == j)
                    continue;
                if (areNeighbors(i, j))
                {
                    //Debug.Log(string.Format("Adding {0} as a neighbor for tile {1}", j, i));
                    foundNeighbors.Add(j);
                }
            }
            // Clear out those farthest away if need be
            if (foundNeighbors.Count > expectedNeighbors)
            {
                foundNeighbors = foundNeighbors.OrderBy(idx => distanceBetweenTiles(i, idx)).ToList();
                foundNeighbors = foundNeighbors.Take(expectedNeighbors).ToList();
            }
            if (foundNeighbors.Count < expectedNeighbors)
                Debug.LogError(string.Format("Only {0} neighbors found for tile {1}", foundNeighbors.Count, i));
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
    // 3. Rotate triangle hexagons. To do so, iterate over the tiles in ascending 
    //    index order to ensure that by the time we reach hex H, at least two of 
    //    it's neighbors are orientated. Find which ones, and rotate H as the 
    //    average rotation required of each aligned neighbor (may be more than 2!).
    //    There may be issues regarding an offset of 2pi/6, but try a naive
    //    implementation and maybe it'll work
    private void orientTiles()
    {
        // Pentagons first
        fixNeighborRotation(0, 1);
        fixNeighborRotation(1, 0);
        int fixedRotations = 2;
        for (int i = 2; i < 12; ++i)
            if (Tools.NearlyEqual(distanceBetweenTiles(i - 1, i), distanceBetweenPents(), epsilon))
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
        while (end < tiles.Count)
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

    // Now, move the tiles so they are distributed uniformly as possible around the sphere.
    // To avoid complicated computations, I'll just go several times over all tiles, and
    // each pass move them to the average point (on the sphere) of their neighbors.
    private void spreadTiles() { spreadTiles(10); }
    private void spreadTiles(int passes)
    {
        // Do passes without pentagons; pentagons are done in one go at the end.
        for (int pass=1; pass<=passes; ++pass)
        {
            // Pentagons don't move, start from 12
            for (int i=12; i<tiles.Count; ++i)
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
                    avg += tiles[j].transform.position;
                }
                // Push location to the sphere. No need to actually average
                // because we're normalizing anyway.
                tiles[i].transform.position = avg.normalized * radius;
            }
        }
        // Pentagons
        for (int i=0; i<12; ++i)
        {
            Vector3 avg = origin;
            foreach (int j in neighbors[i])
                avg += tiles[j].transform.position;
            tiles[i].transform.position = avg.normalized * radius;
        }
    }

    // Dynamically set each tiles' new edge length.
    // The edge length should depend on the degree of the tile
    // in some way; pentagons get a big boost, but after that the
    // smaller the degree the smaller the boost.
    private void updateEdgeLengths()
    {
        float baseEdge = baseEdgeLength();
        for (int i=0; i<tiles.Count; ++i)
            tiles[i].GetComponent<TileBehaviour>().setEdge(baseEdge * edgeMultiplier(getDeg(i)));
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
            lightComp.range = lightRadiusMultiplier * radius;
            lightComp.intensity = lightIntensity;
        }
        // Place them
        switch(numLights)
        {
            // In case of 1 light source, we need to up the range/intensity to illuminate everything
            case 1:
                lights[0].transform.position = origin;
                lights[0].GetComponent<Light>().range = 2 * radius;
                break;
            // For 2 or 3 lights, keep them on the XZ plane
            case 2:
                lights[0].transform.position = origin + new Vector3(0, 0, radius / 3);
                lights[1].transform.position = origin + new Vector3(0, 0, -radius / 3);
                break;
            case 3:
                float deg30 = Mathf.PI / 6f;
                float cos30 = Mathf.Cos(deg30);
                float sin30 = Mathf.Sin(deg30);
                lights[0].transform.position = origin + new Vector3(0, 0, radius / 2);
                lights[1].transform.position = origin + new Vector3(-(radius / 2) * cos30, 0, -(radius / 2) * sin30);
                lights[2].transform.position = origin + new Vector3(radius / 2 * cos30, 0, -(radius / 2) * sin30);
                break;
            // If we have 4 lights it's a bit more complicated - we want to form a 3D pyramid from the lights.
            // From https://en.wikipedia.org/wiki/Tetrahedron#Coordinates_for_a_regular_tetrahedron, we can adapt
            // the coordinates (sqrt(8/9), 0, -1/3), (-sqrt(2/9), sqrt(2/3), -1/3), (-sqrt(2/9), -sqrt(2/3), -1/3), (0,0,1)
            // to our radius (multiply everything by R/2):
            case 4:
                float sqrt2 = Mathf.Sqrt(2);
                float sqrt3 = Mathf.Sqrt(3);
                lights[0].transform.position = origin + (radius / 2f) * (new Vector3(2f / 3f * sqrt2, 0, -1f / 3f));
                lights[1].transform.position = origin + (radius / 2f) * (new Vector3(-sqrt2 / 3f, sqrt2 / sqrt3, -1f / 3f));
                lights[2].transform.position = origin + (radius / 2f) * (new Vector3(-sqrt2 / 3f, -sqrt2 / sqrt3, -1f / 3f));
                lights[3].transform.position = origin + (radius / 2f) * (new Vector3(0, 0, 1));
                break;
            default:
                Debug.LogError(string.Format("Got numLights={0}, valid values are 1~4", numLights));
                break;
        }
    }

    private (int, int) onPentArc(int tileIdx)
    {
        Vector3 point = tiles[tileIdx].transform.position;
        foreach (KeyValuePair<(int,int),Plane> entry in planes)
        {
            Plane plane = entry.Value;
            (int k1, int k2) = entry.Key;
            Vector3 pent1 = tiles[k1].transform.position;
            Vector3 pent2 = tiles[k2].transform.position;
            if (Tools.NearlyEqual(distanceToPlane(tileIdx, entry.Key), 0, epsilon) &&
                (pent1 - point).magnitude < distanceBetweenPents() &&
                (pent2 - point).magnitude < distanceBetweenPents())
                return entry.Key;
        }
        return (-1, -1);
    }
    private bool isOnPentArc(int tileIdx)
    {
        (int i, int _) = onPentArc(tileIdx);
        return i >= 0;
    }

    // The distance of any point to the nearest pentagon-plane ABO for
    // some neighboring pentagons A,B and the origin O.
    // It's not enough to check proximity to the plane, because hexagons
    // in the middle of pent-triangles can still intersect a plane from
    // a pair of pentagons on the opposite side of the sphere.
    private float distanceToPlane(int tileIdx)
    {
        Vector3 point = tiles[tileIdx].transform.position;
        float minDist = radius; // INF, in practice, for any point on the sphere
        float pentDistance = distanceBetweenPents();
        foreach (KeyValuePair<(int,int), Plane> entry in planes)
        {
            // Both pentagons must be within pent-distance of the tile, or it 
            // doesn't count.
            (int p1, int p2) = entry.Key;
            if ((point - tiles[p1].transform.position).magnitude >= pentDistance ||
                (point - tiles[p2].transform.position).magnitude >= pentDistance)
                continue;
            float dist = distanceToPlane(tileIdx, entry.Key);
            if (dist < minDist)
                minDist = dist;
        }
        return minDist;
    }
    private float distanceToPlane(int tileIdx, (int,int) planeKey)
    {
        return System.Math.Abs(planes[planeKey].GetDistanceToPoint(tiles[tileIdx].transform.position));
    }

    // If two tiles are close enough to each other (say, at most the distance between
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
    private void fixNeighborRotation(int sourceTileIdx, int neighborTileIdx)
    {
        TileBehaviour A = tiles[sourceTileIdx];
        TileBehaviour B = tiles[neighborTileIdx];
        Vector3 vA = A.transform.position;
        Vector3 vB = B.transform.position;
        Vector3 BX = B.transform.right;
        Vector3 BY = B.transform.up;
        Vector3 vAxB = Vector3.Cross(vA, vB);
        float targetDeg = 0.0f;
        float rotation = 180f - Vector3.SignedAngle(vAxB, BX, BY);
        rotation = Tools.NearlyEqual(rotation, targetDeg, epsilon) ? 0.0f : rotation;
        B.transform.RotateAround(vB, BY, rotation);
    }

    private float distanceBetweenTiles(int i1, int i2)
    {
        return (tiles[i1].transform.position - tiles[i2].transform.position).magnitude;
    }

    // A distance threshold between two neighboring tiles.
    // THIS DOES NOT GUARANTEE tiles of this distance apart are neighbors, but it's
    // not a bad filter.
    private float neighborTileDistance()
    {
        return 3f * baseEdgeLength();
    }
    private bool areNeighbors(int i1, int i2) {
        return (tiles[i1].transform.position - tiles[i2].transform.position).magnitude <= neighborTileDistance();
    }

    // Assumes the tiles[] list is initialized with the pentagons as the first 12 elements.
    private float distanceBetweenPents()
    {
        return (tiles[0].transform.position - tiles[1].transform.position).magnitude;
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
