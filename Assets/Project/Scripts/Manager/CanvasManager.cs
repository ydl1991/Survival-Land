
using UnityEngine;
using UnityEngine.UI;

public class CanvasManager : MonoBehaviour
{
    // Singleton
    public static CanvasManager s_instance;

    // Display Panel
    public Text m_displayTextPrefab;
    public GameObject m_displayPanel;

    // Message Panel
    public Text m_messageText;
    public GameObject m_messagePanel;
    public float m_maxMessageDisplayTime;
    private float m_messageDisplayTime;

    // Timer
    public Text m_countDownText;
    public float m_timeToNextSpawningWave;
    public ObjectSpawner m_objectSpawner;

    // Enemy Number Info
    public Text m_enemyNumberText;
    public GameObject m_enemyHolder;

    // Player Health
    public RawImage m_playerHealthBar;
    public HealthComponent m_playerHealth;

    // Weapon Ammo
    public Text m_ammoText;
    public Gun m_gun;

    // Player Items
    public ItemComponent m_playerItem;
    public Text m_holdingFirstAidCabinet;
    public Text m_holdingBullet;

    // Loading Slider
    public GameObject m_loadingBar;
    public Slider m_loadingSlider;

    // Coordinate Update
    public Text m_playerCoordinate;

    // Map Panel
    public GameObject m_mapPanel;
    public Camera m_mapCamera;

    void Awake()
    {
        s_instance = this;
        m_messageDisplayTime = m_maxMessageDisplayTime;
        m_messagePanel.SetActive(false);
        m_loadingBar.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        UpdateTimer();
        UpdateEnemyNumber();
        UpdateMessageBox();
        UpdateHealthBar();
        UpdateWeaponStatus();
        UpdateItemInfo();
        UpdateLoadingBar();
        UpdatePlayerCoordinate();
        UpdateMapUI();
    }

    public void UpdatePlayerCoordinate()
    {
        var playerPos = m_playerHealth.transform.position;
        m_playerCoordinate.text = "[ " + Mathf.FloorToInt(playerPos.x).ToString() + " , " + Mathf.FloorToInt(playerPos.z).ToString() + " ]";
    }

    public void AddDisplayText(string txt)
    {
        Text instance = Instantiate(m_displayTextPrefab, Vector3.zero, Quaternion.identity, m_displayPanel.transform);
        instance.text = txt;
    }

    public void ShowMessage(string message)
    {
        m_messageText.text = message;
        m_messageDisplayTime = m_maxMessageDisplayTime;
        m_messagePanel.SetActive(true);
    }

    private void UpdateLoadingBar()
    {
        if (m_playerItem.consumingFirstAidCabinet && !m_loadingBar.activeSelf)
        {
            m_loadingBar.SetActive(true);
        }
        else if (!m_playerItem.consumingFirstAidCabinet && m_loadingBar.activeSelf)
        {
            m_loadingBar.SetActive(false);
        }

        if (m_playerItem.consumingFirstAidCabinet)
        {
            m_loadingSlider.value = m_playerItem.currentConsumingTime / m_playerItem.m_timeToConsumeFirstAidCabinet;
        }
    }

    private void UpdateItemInfo()
    {
        m_holdingBullet.text = m_playerItem.numOfBullet.ToString();
        m_holdingFirstAidCabinet.text = m_playerItem.numOfFirstAid.ToString();
    }

    private void UpdateWeaponStatus()
    {
        m_ammoText.text = string.Format("{0} / {1}", m_gun.CurrentAmmoInGun(), m_gun.m_maxAmmo);
    }

    private void UpdateHealthBar()
    {
        m_playerHealthBar.transform.localScale = new Vector3(m_playerHealth.healthPercentage, 1, 1);
    }

    private void UpdateMessageBox()
    {
        if (m_messageDisplayTime > 0f)
        {
            m_messageDisplayTime -= Time.deltaTime;
            if (m_messageDisplayTime <= 0f)
                m_messagePanel.SetActive(false);
        }
    }

    private void UpdateTimer()
    {
        // timer count down
        m_timeToNextSpawningWave -= Time.deltaTime;

        if (m_timeToNextSpawningWave > 60f)
        {
            return;
        }
        else if (m_timeToNextSpawningWave <= 60f && !m_countDownText.gameObject.activeSelf)
        {
            m_countDownText.gameObject.SetActive(true);
        }
        else if (m_timeToNextSpawningWave <= 0f)
        {
            m_timeToNextSpawningWave = 0f;
            m_objectSpawner.NotifySpawnNextWave();
            m_timeToNextSpawningWave = m_objectSpawner.m_timeGapPerSpawningWave;
            m_countDownText.gameObject.SetActive(false);
            return;
        }

        float minutes = Mathf.FloorToInt(m_timeToNextSpawningWave / 60);  
        float seconds = Mathf.FloorToInt(m_timeToNextSpawningWave % 60);
        m_countDownText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
    }

    private void UpdateEnemyNumber()
    {
        m_enemyNumberText.text = m_enemyHolder.transform.childCount.ToString();
    }

    private void UpdateMapUI()
    {
        if (Input.GetKeyDown(KeyCode.M) && !m_mapPanel.activeSelf)
        {
            m_mapCamera.gameObject.SetActive(true);
            m_mapPanel.SetActive(true);
        }
        else if (Input.GetKeyUp(KeyCode.M) && m_mapPanel.activeSelf)
        {
            m_mapPanel.SetActive(false);
            m_mapCamera.gameObject.SetActive(false);
        }
    }
}
