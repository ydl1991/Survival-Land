using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LoadingScreen : MonoBehaviour
{
    public float m_loadingTime;
    public GameObject m_cgObject;

    // Update is called once per frame
    void Update()
    {
        m_loadingTime -= Time.deltaTime;

        if (m_loadingTime <= 0f)
        {
            m_cgObject.SetActive(true);
            SceneTransition.s_instance.ResumeSound();
            gameObject.SetActive(false);
        }
    }
}
