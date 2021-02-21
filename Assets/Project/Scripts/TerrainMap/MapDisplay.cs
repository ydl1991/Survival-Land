using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapDisplay : MonoBehaviour
{
    public Renderer m_textureRenderer;
    public MeshFilter m_meshFilter;
    public MeshRenderer m_meshRenderer;
    public void DrawTexture(Texture2D texture)
    {
        // using sharedMaterial instead of Material since we want to be able to preview our map without entering into
        // game mode, Material can only be instantiated during runtime.
        m_textureRenderer.sharedMaterial.mainTexture = texture;
        m_textureRenderer.transform.localScale = new Vector3(texture.width, 1, texture.height);
    }

    public void DrawMesh(MeshData data, Texture2D texture)
    {
        m_meshFilter.sharedMesh = data.CreateMesh();
        m_meshRenderer.sharedMaterial.mainTexture = texture;
    }
}
