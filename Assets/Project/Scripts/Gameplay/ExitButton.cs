using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExitButton : MonoBehaviour
{
    public void EndGame()
    {
        Debug.Log("Quit Game!");
        Application.Quit();
    }
}
