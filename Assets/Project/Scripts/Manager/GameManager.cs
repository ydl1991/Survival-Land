using UnityEngine;
using System.Collections;

public class GameManager : MonoBehaviour
{
    // Singleton
    public static GameManager s_instance;

    public GameObject m_gameCanvas;
    public GameObject m_player;
    public GameObject m_CG;

    void Awake()
    {
        s_instance = this;
    }

    public void EndGame()
    {
        Debug.Log("Quit Game!");
        Application.Quit();
    }

    public void GameStart()
    {
        StartCoroutine(ActivateGameCanvasAndPlayer());
    }

    IEnumerator ActivateGameCanvasAndPlayer()
    {
        SceneTransition.s_instance.FadeOut();
        yield return new WaitForSeconds(3f);
        m_CG.SetActive(false);
        m_gameCanvas.SetActive(true);
        m_player.SetActive(true);
        yield return new WaitForSeconds(1f);
        SceneTransition.s_instance.FadeIn();
        SceneTransition.s_instance.ResumeSound();
    }   
}
