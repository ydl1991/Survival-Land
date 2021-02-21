using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class MapMeshGenerator
{
    // this return mesh data instead of actual mesh because unity cannot spawn mesh in thread
    public static MeshData GenerateTerrainMesh(float[,] heightMap, float heightMultiplier, AnimationCurve heightCurve, int levelOfDetail)
    {
        AnimationCurve heightCurveTemp = new AnimationCurve(heightCurve.keys);
        int width = heightMap.GetLength(0);
        int height = heightMap.GetLength(1);
        float topLeftX = (float)(width - 1) / -2f;
        float topLeftZ = (float)(height - 1) / 2f;
        
        int meshSimplificationIncrement = (levelOfDetail == 0) ? 1 : levelOfDetail * 2;
        int verticesPerLine = (width - 1) / meshSimplificationIncrement + 1;

        MeshData mesh = new MeshData(verticesPerLine, verticesPerLine);
        int vertexIndex = 0;

        for (int y = 0; y < height; y += meshSimplificationIncrement)
        {
            for (int x = 0; x < width; x += meshSimplificationIncrement)
            {
                mesh.vertices[vertexIndex] = new Vector3(topLeftX + x, heightCurveTemp.Evaluate(heightMap[x, y]) * heightMultiplier, topLeftZ - y);
                mesh.uvs[vertexIndex] = new Vector2(x / (float)width, y / (float)height);
                if ( x < width - 1 && y < height - 1)
                {
                    mesh.AddTriangle(vertexIndex, vertexIndex + verticesPerLine + 1, vertexIndex + verticesPerLine);
                    mesh.AddTriangle(vertexIndex + verticesPerLine + 1, vertexIndex, vertexIndex + 1);
                }
                ++vertexIndex;
            }
        }

        return mesh;
    }

}

public class MeshData
{
    public Vector3[] vertices { get; set; }
    public int[] triangles { get; set; }
    public Vector2[] uvs { get; set; }
    int m_currentTriangle;
    public MeshData(int numVertHorizontal, int numVertVertical)
    {
        vertices = new Vector3[numVertHorizontal * numVertVertical];
        uvs = new Vector2[numVertHorizontal * numVertVertical];

        // The number of squares in the mesh should be equal to (numVertHorizontal - 1) * (numVertVertical - 1)
        // and each square is 2 triangle, each triangle is 3 vertex
        triangles = new int[(numVertHorizontal - 1) * (numVertVertical - 1) * 6];
    }

    public void AddTriangle(int a, int b, int c)
    {
        triangles[m_currentTriangle] = a;
        triangles[m_currentTriangle + 1] = b;
        triangles[m_currentTriangle + 2] = c;
        m_currentTriangle += 3;
    }

    public Mesh CreateMesh()
    {
        Mesh mesh = new Mesh();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;
        mesh.RecalculateNormals();
        return mesh;

    }
}