using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SelfDestruction : MonoBehaviour
{
    public float m_liveLength;

    void Start()
    {
        if (m_liveLength != 0f)
            Destroy(gameObject, m_liveLength);
    }
}
