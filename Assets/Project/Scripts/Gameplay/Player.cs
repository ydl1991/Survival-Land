using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

public class Player : MonoBehaviour
{
    public SceneTransition m_transition;
    public Animator m_playerAnimator;
    public AudioSource m_playerDie;

    private HealthComponent m_playerHealth;
    private bool m_end;

    // Start is called before the first frame update
    void Start()
    {
        m_playerHealth = GetComponent<HealthComponent>();
        m_end = false;
    }

    // Update is called once per frame
    void Update()
    {
        if (m_end)
            return;

        if (!m_end && !m_playerHealth.alive)
        {
            m_end = true;
            StartCoroutine(DieAndLoadMenu());
        }

        if (MissionManager.checkMissionDone())
        {
            m_end = true;
            MissionManager.allMissionsCompleted();
            StartCoroutine(WinAndLoadMenu());
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            m_end = true;
            m_transition.FadeToMenu();
        }
    }

    public void DieLoadMenu()
    {
        m_transition.DieAndFadeToMenu();
    }

    IEnumerator DieAndLoadMenu()
    {
        GetComponent<UnityStandardAssets.Characters.FirstPerson.FirstPersonController>().enabled = false;
        GetComponentInChildren<Gun>().enabled = false;
        m_playerAnimator.enabled = true;
        m_playerDie.Play();
        yield return new WaitForSeconds(5f);
        DieLoadMenu();
    }
    
    public void WinLoadMenu()
    {
        m_transition.WinAndFadeToMenu();
    }

    IEnumerator WinAndLoadMenu()
    {
        GetComponent<UnityStandardAssets.Characters.FirstPerson.FirstPersonController>().enabled = false;
        yield return new WaitForSeconds(1f);
        WinLoadMenu();
    }
}
