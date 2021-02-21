using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CheatScript : MonoBehaviour
{
    public GameObject m_player;
    public GameObject m_magicCircleHolder;
    private int m_magicCircleIndex;

    void Awake()
    {
        m_magicCircleIndex = 0;
    }

    // Update is called once per frame
    void Update()
    {
        // press 1 for instant die
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            m_player.GetComponent<HealthComponent>().Die();
        }
        // press 2 for instant win
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            MissionManager.instantWin();
        }
        // press 3 for teleporting to next magic circle
        else if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            TeleportToNextMagicCircle();
        }
        // press 4 for getting 10000 health and 1000 ammo
        else if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            m_player.GetComponent<HealthComponent>().SetHealth(10000f);
            m_player.GetComponent<ItemComponent>().SetBulletCount(1000);
            CanvasManager.s_instance.ShowMessage("You cheated to gain 10000 health and 1000 ammo.");
        }
    }

    private void TeleportToNextMagicCircle()
    {
        int childCount = m_magicCircleHolder.transform.childCount;

        // if no magic circle on the map, exit
        if (childCount == 0)
            return;

        // if invalid index, reset index to 0
        if (m_magicCircleIndex >= childCount)
            m_magicCircleIndex = 0;

        // find next child
        Transform nextChild = m_magicCircleHolder.transform.GetChild(m_magicCircleIndex);
        Vector3 newPos = nextChild.position;
        newPos.y += 2.5f;
        m_player.transform.position = newPos;

        ++m_magicCircleIndex;
    }
}
