using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshCollider))]
public class PentGenerator : MonoBehaviour {

	public float radius;
	public float edge;
	public float height;
	public int id;

	private Tile tile;
	private Mesh mesh;
	private bool needRedraw;

    void Awake()
    {
        tile = new Tile(false, edge, radius, height);
		needRedraw = true;
    }

    void Start()
    {
		
    }
	  
	void Update()
    {
		if (needRedraw) {
			tile.redraw();
			UpdateMesh();
			needRedraw = false;
		}
    }
	void UpdateMesh()
	{
		mesh = tile.getMesh();
        GetComponent<MeshFilter>().mesh = mesh;
		GetComponent<MeshCollider>().sharedMesh = mesh;
	}

	void OnCollisionEnter(Collision col)
	{
		// If this isn't implemented there are no collisions...?
	}

	public void setEdge(float e) {
		edge = e;
		tile.setEdge(e);
		needRedraw = true;
	}

	public void setHeight(float h) {
		height = h;
		tile.setHeight(h);
		needRedraw = true;
	}

	public void setRadius(float r) {
		radius = r;
		tile.setRadius(r);
		needRedraw = true;
	}
}

/*
[RequireComponent(typeof(MeshFilter))]
public class PentGenerator : MonoBehaviour
{
	public bool DEBUG = false;
	public float edge = 1;
	public float height = 0.2f;
	public int id;              // Set by Geosphere generator

	Mesh mesh;
	Vector3[] vertices;
	private float prevEdge;
	int[] triangles;

	void Start()
	{
		mesh = new Mesh();
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

	void UpdateMesh()
	{
		vertices = getVertices();
		triangles = getTriangles();
		mesh.vertices = vertices;
		mesh.triangles = triangles;
		mesh.RecalculateNormals();
        GetComponent<MeshFilter>().mesh = mesh;
		GetComponent<MeshCollider>().sharedMesh = mesh;
	}

	// Allow parent object to control edge width
	public void setEdge(float e) {
		Debug.Log(string.Format("Set edge to {0} in PentGenerator", e));
		edge = e;
	}

	// The width should be the total width of the pentagon:
	//     _+_
	//  +--   --+
	//  \       /
	//   +-----+
	//
	// |<---w--->|
	float getWidth()
	{
		return edge + 2 * getFloorGap();
	}

	// The length should be the total length of the pentagon:
	//     _+_      ^
	//  +--   --+   |
	//  \       /   | length
	//   +-----+    v
	float getLength()
	{
		return getLowerLength() + getUpperLength();
	}

	// The floor gap should be the distance between the bottom-left corner
	// of the bounding box and the floor of the pentagon:
	//     _+_
	//  +--   --+
	//  \       /
	//   +-----+
	//
	// |-|
	//
	float getFloorGap()
	{
		return edge * (float)System.Math.Sin(getAcuteDegree());
	}

	// The lower length should be the distance between the floor and the mid-
	// layer vertices:
	//     _+_
	//  +--   --+   ^
	//  \       /   | lower length
	//   +-----+    v
	float getLowerLength()
	{
		return edge * (float)System.Math.Cos(getAcuteDegree());
	}

	// The upper length should be the distance between the mid-layer vertices
	// and the top of the pentagon
	//     _+_      ^
	//  +--   --+   v upper length
	//  \       /
	//   +-----+
	float getUpperLength()
	{
		// The acute degree is 18, our angle in question is 36 degree
		return edge * (float)System.Math.Sin(2 * getAcuteDegree());
	}

	// Converts the pentagon's 18 degree angle to Radians
	double getAcuteDegree()
    {
		return (System.Math.PI / 180) * 18;
	}

	// We need to compute the center offset before the vertices themselves
	// so we can set the offset correctly
	Vector3 getCenterOffset()
    {
		return new Vector3(getFloorGap() + edge / 2, height / 2, getLength() / 2);
    }

	public Vector3[] getVertices()
	{
		Vector3[] v = new Vector3[10];
		Vector3 centerOffset = getCenterOffset();
		float width = getWidth();
		float length = getLength();
		float floorGap = getFloorGap();
		float lowerLength = getLowerLength();
		// Bottom layer (y=0):
		//     __--2--__
		//    1         3
		//     \       /
		//      0-----4
		// Top layer (y=height):
		//     __--7--__
		//    6         8
		//     \       /
		//      5-----9
		for (int i = 0; i <= 1; ++i)
		{
			float y = height * i;
			v[0 + 5 * i] = -centerOffset + new Vector3(floorGap, y, 0);
			v[1 + 5 * i] = -centerOffset + new Vector3(0, y, lowerLength);
			v[2 + 5 * i] = -centerOffset + new Vector3(width / 2, y, length);
			v[3 + 5 * i] = -centerOffset + new Vector3(width, y, lowerLength);
			v[4 + 5 * i] = -centerOffset + new Vector3(floorGap + edge, y, 0);
		}
		return v;
	}

	public int[] getTriangles()
	{
		// See the picture in the comment in getVertices() to see the order of the nodes.
		// Pentagon triangulation should look like:
		//     _+_
		//  +-- | --+
		//  \  / \  /
		//   +-----+
		return new int[] {
		    // Start with the triangles forming the top view (positive y value).
		    // This is just adding 5 to the node IDs on the bottom layer, and
		    // each triangle should be layed out clockwise so the material is
		    // displayed.
            5,6,7,5,7,9,9,7,8,
		    // For the bottom view we need to go counter-clockwise.
		    0,2,1,0,4,2,4,3,2,
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
		    0,1,5,1,6,5,
			1,2,6,2,7,6,
			2,3,7,3,8,7,
			3,4,8,4,9,8,
			4,0,9,0,5,9
		};
	}

}
*/
