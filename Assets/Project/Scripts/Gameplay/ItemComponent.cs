using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemComponent : MonoBehaviour
{
    public int numOfBullet { get; private set; }
    public int numOfFirstAid { get; private set; }
    public bool consumingFirstAidCabinet { get; private set; }
    public float currentConsumingTime { get; private set; }
    
    public int m_startingBullet;
    public int m_startingFirstAidKit;
    public float m_timeToConsumeFirstAidCabinet = 4f;
    public float m_firstAidHealingAmount;
    private HealthComponent m_playerHealth;

    public AudioSource m_injectionSoundEffect;

    // Start is called before the first frame update
    void Awake()
    {
        numOfBullet = m_startingBullet;
        numOfFirstAid = m_startingFirstAidKit;
        currentConsumingTime = 0;
        m_firstAidHealingAmount = 50f;
        consumingFirstAidCabinet = false;
        m_playerHealth = gameObject.GetComponent<HealthComponent>();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F1))
        {
            if (m_playerHealth.healthPercentage < 1f && numOfFirstAid > 0)
            {
                currentConsumingTime = 0;
                consumingFirstAidCabinet = true;
                m_injectionSoundEffect.Play();
            }
            else if (numOfFirstAid > 0)
                CanvasManager.s_instance.ShowMessage("You are very healthy, better not to waste supplement.");
            else
                CanvasManager.s_instance.ShowMessage("No more first-aid cabinet, better go find some.");
        }
        else if (consumingFirstAidCabinet && Input.anyKeyDown)
        {
            CanvasManager.s_instance.AddDisplayText("First-aid cabinet consumption interrupted.");
            consumingFirstAidCabinet = false;
            m_injectionSoundEffect.Stop();
        }

        if (consumingFirstAidCabinet)
        {
            currentConsumingTime += Time.deltaTime;
            if (currentConsumingTime >= m_timeToConsumeFirstAidCabinet)
            {
                currentConsumingTime = m_timeToConsumeFirstAidCabinet;
                ConsumeFirstAidCabinet();
                consumingFirstAidCabinet = false;
            }
        }
    }

    public void SetBulletCount(int num)
    {
        numOfBullet = num;
        CanvasManager.s_instance.AddDisplayText("Cheater gets " + num.ToString() + " ammo.");
    }

    public void PickUpBullet(int num)
    {
        if (num == 0)
            return;

        numOfBullet += num;
        CanvasManager.s_instance.AddDisplayText("You gain " + num.ToString() + " ammo.");
    }

    public void PickUpFirstAidCabinet(int num)
    {
        if (num == 0)
            return;

        numOfFirstAid += num;
        CanvasManager.s_instance.AddDisplayText("You gain " + num.ToString() + " first-aid kits.");
    }

    public int GetBulletToUse(int num)
    {
        if (numOfBullet >= num)
        {
            numOfBullet -= num;
            return num;
        }
        else if (numOfBullet > 0)
        {
            int ammo = numOfBullet;
            numOfBullet = 0;
            return ammo;
        }

        return 0;
    }

    public int GetFirstAidCabinetToUse(int num)
    {
        if (numOfFirstAid >= num)
        {
            numOfFirstAid -= num;
            return num;
        }
        else if (numOfFirstAid > 0)
        {
            numOfFirstAid = 0;
            return numOfFirstAid;
        }

        return 0;
    }

    private void ConsumeFirstAidCabinet()
    {
        m_playerHealth.ChangeHealth(m_firstAidHealingAmount);
        CanvasManager.s_instance.AddDisplayText("Recorver 50 HP from First-aid cabinet consumption.");
        --numOfFirstAid;
    }
}
