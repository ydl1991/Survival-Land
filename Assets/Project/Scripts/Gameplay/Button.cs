using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Button : MonoBehaviour
{
    public AudioSource m_clickSound;

    public void ClickSound()
    {
        m_clickSound.Play();
    }
}
