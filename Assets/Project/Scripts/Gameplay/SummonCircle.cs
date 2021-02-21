using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SummonCircle : MonoBehaviour, SpawnedObject
{
    float summonCountDown { get; set; }

    public float m_summonTimeGap;
    
    private ObjectSpawner m_spawner;

    void Awake()
    {
        summonCountDown = 30;
    }

    void Update()
    {
        summonCountDown -= Time.deltaTime;
        
        if (summonCountDown <= 0f)
        {
            summonCountDown = m_summonTimeGap;

            Vector3 pos = transform.position;
            pos.y += 1f;
            m_spawner.SpawnObjectAtWorld(pos, GameObjectType.kEnemy);
        }
    }

    public void Init(ObjectSpawner spawner)
    {
        m_spawner = spawner;
    }

    public void ReleaseEnergy()
    {
        CanvasManager.s_instance.AddDisplayText("You destroyed a magic circle.");
        
        string destroyItems = m_spawner.RunRule('C');
        foreach (char c in destroyItems)
        {
            if (c == '_')
                continue;

            Vector3 pos = transform.position;
            pos.y += 0.5f;
            m_spawner.SpawnObjectFromChar(c, pos);
            CanvasManager.s_instance.AddDisplayText("A " + SpawningGrammar.GetDescription(c) + " is released from magic circle.");
        }

        MissionManager.notifyMissionObjectDown(TargetType.kMagicCircle);
    }
}
