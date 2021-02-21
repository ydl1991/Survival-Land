using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum Axis
{
    kX = 0,
    kY,
    kZ
}

public class AutoRotation : MonoBehaviour
{
    public Axis m_rotationAxis;
    public float m_rotationSpeed;

    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(AutoRotate());
    }

    IEnumerator AutoRotate()
    {
        while (true)
        {
            transform.Rotate(Vector3.forward * m_rotationSpeed * Time.deltaTime);
            yield return null;
        }
    }
}
