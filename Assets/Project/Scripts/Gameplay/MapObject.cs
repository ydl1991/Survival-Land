using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapObject : MonoBehaviour
{
    public Material m_objectMat;

    // Start is called before the first frame update
    void Start()
    {
        GetComponent<MeshRenderer>().sharedMaterials[0] = m_objectMat;
    }
}
