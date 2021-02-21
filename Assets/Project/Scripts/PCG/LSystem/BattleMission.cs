using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum TargetType
{
    kEnemy,
    kMagicCircle
}

public class BattleMission : MonoBehaviour
{
    public int targetCount { get; private set; }
    public int targetCompleted { get; private set; }
    public string rewardString { get; private set; }
    public float missionCountDown { get; private set; }
    public TargetType targetType { get; private set; }

    [SerializeField] Text m_missionDescription;
    [SerializeField] Text m_completion;
    [SerializeField] Text m_rewardCount;
    [SerializeField] Text m_timer;
    
    private ItemComponent m_itemComp;

    // Start is called before the first frame update
    void Start()
    {
        m_timer.text = "-";
    }

    // Update is called once per frame
    void Update()
    {
        if (missionCountDown > 0f)
        {
            missionCountDown -= Time.deltaTime;
            if (missionCountDown <= 0f)
            {
                missionCountDown = 0f;
                Destroy(gameObject);
                return;
            }

            m_timer.text = Mathf.FloorToInt(missionCountDown).ToString() + "s";  
        }
    }

    public void Init(int targetCount, string rewardString, float missionCountDown, string missionDesc, TargetType targetType, GameObject player)
    {
        this.targetCount = targetCount;
        this.rewardString = rewardString;
        this.missionCountDown = missionCountDown;
        this.targetType = targetType;
        m_itemComp = player.GetComponent<ItemComponent>();
        m_missionDescription.text = missionDesc;
        m_rewardCount.text = rewardString.Length.ToString();
        m_completion.text = targetCompleted.ToString() + "  /  " + targetCount.ToString();
        targetCompleted = 0;
    }

    public void NotifyObjectDown(TargetType type)
    {
        if (targetType != type)
            return;

        targetCompleted += 1;
        m_completion.text = targetCompleted.ToString() + "  /  " + targetCount.ToString();

        if (targetCompleted >= targetCount)
        {
            MissionComplete();
        }
    }

    private void MissionComplete()
    {
        MissionManager.playCompletionSound();

        foreach (char c in rewardString)
        {
            int number = int.Parse(ObjectSpawner.delegateRunRule(c));
            if (c == 'A')
                m_itemComp.PickUpBullet(number);
            else if (c == 'F')
                m_itemComp.PickUpFirstAidCabinet(number);
        }

        Destroy(gameObject);
    }
}
