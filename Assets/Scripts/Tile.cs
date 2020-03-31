using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class Tile
{
	public bool DEBUG = true;
	public float height;
	public float edge;
	public float radius;
	public int id;              // Set by Geosphere generator
	public bool isHex;

	private Mesh mesh;

	public Tile(bool isHexagon, float edgeLength, float r, float h, int tileId = -1)
	{
		isHex = isHexagon;
		height = h;
		edge = edgeLength;
		radius = r;
		id = tileId;
		mesh = new Mesh();
	}

	public void redraw()
	{
		mesh = new Mesh();
		mesh.vertices = getVertices();
		mesh.triangles = getTriangles();
		mesh.RecalculateNormals();
	}

	public Mesh getMesh() { return mesh; }

	// Allow parent object to control edge width
	public void setEdge(float e) { edge = e; }
	public void setHeight(float h) { height = h; }
	public void setRadius(float r) { radius = r; }

	int totalEdges() { return isHex ? 6 : 5; }

	// The length is the the following distance:
	//     +-----+           _+_      ^
	//    /|      \       +--   --+   |
	//   + |l      +      \       /   |l
	//    \|      /        +-----+    v
	//     +-----+
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
	// center, and we want to compute this vector before computing
	// the points of the hexagon/pentagon.
	Vector3 getCenterOffset()
	{
		return new Vector3(edge * 0.5f + getFloorGap(), height * 0.5f, getLength() * 0.5f);
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
	Vector3[] getVertices()
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
			v[totalEdges() + i] = v[i] + height * (radiusUp - v[i]).normalized;
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
