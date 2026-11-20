using System;
using System.Collections.Generic;
using UnityEngine;

public class RoadMeshGeneratorStarter : MonoBehaviour
{
    public SplinePath splinePath;
    public int rungs = 10;
    public float width = 3f;
    
    // Added for the Stretch: Solid Road part of the assignment 
    // Controls the vertical thickness of the road
    // When it's 0 it should be flat like it previously was
    public float roadThickness = 0f;
    
    public Material material;
    public float textureRepeatLength = 2f;
    public bool rebuildEveryFrame;
    public float[] bankAngles;
    
    MeshFilter _meshFilter;
    MeshRenderer _meshRenderer;
    
    void Start()
    {
        _meshFilter = gameObject.AddComponent<MeshFilter>();
        _meshRenderer = gameObject.AddComponent<MeshRenderer>();
        
        BuildRoadMesh();
    }

    void Update()
    {
        if (rebuildEveryFrame)
        {
            BuildRoadMesh();
        }
    }

    void BuildRoadMesh()
    {
        List<Vector3> verts = new();
        List<Vector2> uvs = new();
        
        Array.Resize(ref bankAngles, splinePath.SegmentCount + 1);

        float thickness = Mathf.Max(0f, roadThickness); // Making sure that negatives in the inspector don't make the road go weird
        
        for (int i = 0; i < rungs; i++)
        {
            // Get the distance to the rung
            float fraction = i / (float)(rungs - 1);
            float distance = splinePath.TotalLength * fraction;

            // Use distance to look up u
            float u = splinePath.ParameterAtDistance(distance);
            
            // Use u to get the center rung
            Vector3 centerPoint = splinePath.SamplePoint(u);
            Vector3 tangent = splinePath.SampleTangent(u);

            int segment = (int)Mathf.Min(u, splinePath.SegmentCount - 1);
            float t = u - segment;
            float bankAngle = Mathf.SmoothStep(bankAngles[segment], bankAngles[segment + 1], t);
            
            // Calculate the "right" direction
            Vector3 right = Vector3.Cross(Vector3.up, tangent).normalized;
            right = Quaternion.AngleAxis(bankAngle, tangent) * right;

            float halfWidth = width * 0.5f;
            float lift = Mathf.Sin(bankAngle * Mathf.Deg2Rad) * halfWidth;
            
            centerPoint += Vector3.up * Mathf.Abs(lift);
            
            // Existing road ribbon will become the bottom of the road as well
            Vector3 bottomLeftVert = centerPoint - right * halfWidth;
            Vector3 bottomRightVert = centerPoint + right * halfWidth;
            
            // Create the top left and top right verts 
            Vector3 topLeftVert = bottomLeftVert + Vector3.up * thickness;
            Vector3 topRightVert = bottomRightVert + Vector3.up * thickness;
            
            // Four vertices per rung instead of the previous two 
            // The side UVs use distance as their V coordinate so the side texture repeats along the road just like the top texture
            verts.Add(bottomLeftVert);
            verts.Add(bottomRightVert);
            verts.Add(topLeftVert);
            verts.Add(topRightVert);
            
            float v = distance/textureRepeatLength;
            uvs.Add(new Vector2(0f, v));
            uvs.Add(new Vector2(1f, v));
            uvs.Add(new Vector2(0f, v));
            uvs.Add(new Vector2(1f, v));
        }
        
        List<int> triangles = new();
        for (int i = 0; i < rungs - 1; i++)
        {
            int rung = i * 4;
            int nextRung = (i + 1) * 4;
            
            int bottomLeft = rung;
            int bottomRight = rung + 1;
            int topLeft = rung + 2;
            int topRight = rung + 3;
            
            int nextBottomLeft = nextRung;
            int nextBottomRight = nextRung + 1;
            int nextTopLeft = nextRung + 2;
            int nextTopRight = nextRung + 3;
            
            // Top surface of the road mesh
            AddQuad(triangles, topLeft, nextTopLeft,topRight, nextTopRight);
            
            // Only if the road will have a thickness greater than zero
            // If not just render the top part
            if (thickness > 0f)
            {
                // Bottom surface of road mesh
                AddQuad(triangles, bottomLeft, bottomRight, nextBottomLeft, nextBottomRight);
                
                // Sides of the road
                AddQuad(triangles, bottomLeft, nextBottomLeft, topLeft, nextTopLeft); // Left Side
                AddQuad(triangles, bottomRight, topRight, nextBottomRight, nextTopRight); // Right side
            }
        }

        if (thickness > 0f)
        {
            // Beginning of the road
            AddTriangle(triangles, 0, 2, 3);
            AddTriangle(triangles, 0, 3, 1);
            
            // End of the road
            int end = (rungs - 1) * 4;
            AddTriangle(triangles, end, end + 1, end + 3);
            AddTriangle(triangles, end, end + 3, end + 2);
        }
        
        
        Mesh mesh = new();
        mesh.vertices = verts.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.uv = uvs.ToArray();
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        _meshFilter.mesh = mesh;
        _meshRenderer.material = material;

    }
    
    // Adds two triangles to form one rectangular face
    static void AddQuad(List<int> triangles, int a, int b, int c, int d)
    {
        triangles.Add(a);
        triangles.Add(b);
        triangles.Add(c);

        triangles.Add(b);
        triangles.Add(d);
        triangles.Add(c);
    }
    
    // Adds one triangle
    // Will be used to adding the surfaces at the ends of the road
    static void AddTriangle(List<int> triangles, int a, int b, int c)
    {
        triangles.Add(a);
        triangles.Add(b);
        triangles.Add(c);
    }
    
}