using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
public class HexGenerator : MonoBehaviour
{
    public bool DEBUG = true;
    public float height = 0.2f;
	public float edge;
	public int id;				// Set by Geosphere generator

	Mesh mesh;
	Vector3[] vertices;
    int[] triangles;
	float prevEdge;

	// Allow parent object to control edge width
	public void setEdge(float e) {
		//Debug.Log(string.Format("Set edge to {0} in HexGenerator", e));
		edge = e;
	}

	// The width should be the total width of the hex:
	//     +---------+
	//    /|          \
	//   + |w          +
	//    \|          /
	//     +---------+
	static float getWidth(float edge)
	{
		return edge * (float)System.Math.Sqrt(3);
	}

	// We want the origin of this object to be it's geometric 
	// center, and we want to compute this vector before computing
	// the points of the hex.
	//     +---------+
	//    /           \
	//   +      C      +
	//    \           /
	//     +---------+
	static Vector3 getCenterOffset(float edge, float height)
    {
		return new Vector3(edge, height / 2, getWidth(edge) / 2);
    }

    private void Awake()
    {
        id = -1;
		mesh = new Mesh();    
	}

    // Start is called before the first frame update 
    void Start()
    {
        GetComponent<MeshFilter>().mesh = mesh;
		GetComponent<MeshCollider>().sharedMesh = mesh;
        UpdateMesh();
		prevEdge = edge;
    }

	void Update()
	{
		if (prevEdge != edge)
		{
			UpdateMesh();
			prevEdge = edge;
		}
	}

	void OnCollisionEnter(Collision col)
	{
		// If this isn't implemented there are no collisions...?
	}

	void UpdateMesh() {
		vertices = getVertices(edge, height);
		triangles = getTriangles();
		mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        GetComponent<MeshFilter>().mesh = mesh;
		GetComponent<MeshCollider>().sharedMesh = mesh;
    }
	
	public static Mesh getMesh(float edge, float height)
    {
		Mesh m = new Mesh();
		m.vertices = getVertices(edge, height);
		m.triangles = getTriangles();
		return m;
    }

	// Bottom layer (y=0):
	//      2-----3
	//     /       \
	//    1         4
	//     \       /
	//      0-----5
	// Top layer (y=height):
	//      8-----9
	//     /       \
	//    7         10
	//     \       /
	//      6-----11
	public static Vector3[] getVertices(float edge, float height) {
		Vector3[] v = new Vector3[12];
		float width = getWidth(edge);
		Vector3 centerOffset = getCenterOffset(edge, height);
		for (int i = 0; i <= 1; ++i) {
            float y = height*i;
            v[0 + 6*i] = -centerOffset + new Vector3(edge/2,   y,       0);
            v[1 + 6*i] = -centerOffset + new Vector3(0,        y, width/2);
            v[2 + 6*i] = -centerOffset + new Vector3(edge/2,   y,   width);
            v[3 + 6*i] = -centerOffset + new Vector3(3*edge/2, y,   width);
            v[4 + 6*i] = -centerOffset + new Vector3(2*edge,   y, width/2);
            v[5 + 6*i] = -centerOffset + new Vector3(3*edge/2, y,       0);
        }
		return v;
	}
	
	public static int[] getTriangles() {

        // See the picture in the comment in getVertices() to see the order of the nodes.
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
            6,7,8,11,6,8,11,8,9,11,9,10,
		    // For the bottom view we need to go counter-clockwise.
		    0,2,1,5,2,0,5,3,2,5,4,3,
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
		    0,1,6,1,7,6,
		    1,2,7,2,8,7,
		    2,3,8,3,9,8,
		    3,4,9,4,10,9,
		    4,5,10,5,11,10,
		    5,0,11,0,6,11
		};
	}

}
