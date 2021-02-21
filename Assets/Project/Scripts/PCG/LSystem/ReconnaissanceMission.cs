using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ReconnaissanceMission : MonoBehaviour
{
    public Vector2 targetLocation { get; private set; }
    public string rewardString { get; private set; }
    public float missionCountDown { get; private set; }

    [SerializeField] Text m_missionDescription;
    [SerializeField] Text m_location;
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

        Vector2 playerPosition = new Vector2(m_itemComp.transform.position.x, m_itemComp.transform.position.z);
        if (Input.GetKeyDown(KeyCode.E) && Vector2.Distance(playerPosition, targetLocation) <= 10f)
        {
            MissionComplete();
        }
    }

    public void Init(Vector2 location, string rewardString, float missionCountDown, string missionDesc, GameObject player)
    {
        this.targetLocation = location;
        this.rewardString = rewardString;
        this.missionCountDown = missionCountDown;
        m_itemComp = player.GetComponent<ItemComponent>();
        m_missionDescription.text = missionDesc;
        m_rewardCount.text = rewardString.Length.ToString();
        m_location.text = "(" + Mathf.FloorToInt(location.x).ToString() + ", " + Mathf.FloorToInt(location.y).ToString() + ")";
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
