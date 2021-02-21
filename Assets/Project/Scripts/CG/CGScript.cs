
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class CGScript : MonoBehaviour
{
    public Text m_messageText;
    public GameObject m_messagePanel;
    public float m_maxMessageDisplayTime = 3f;
    private float m_messageDisplayTime;

    // Start is called before the first frame update
    void Awake()
    {
        m_messageDisplayTime = m_maxMessageDisplayTime;
        m_messagePanel.SetActive(false);
    }

    void Start()
    {
        StartCoroutine(StartCGScript());
    }

    // Update is called once per frame
    void Update()
    {
        UpdateMessageBox();
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

    public void ShowMessage(string message)
    {
        m_messageText.text = message;
        m_messageDisplayTime = m_maxMessageDisplayTime;
        m_messagePanel.SetActive(true);
    }

    IEnumerator StartCGScript()
    {
        yield return new WaitForSeconds(2f);
        ShowMessage("Finally...");
        yield return new WaitForSeconds(4f);
        ShowMessage("Finally got to the damn place...");
        yield return new WaitForSeconds(4f);
        ShowMessage("Hah! Actually it's quite nice!");
        yield return new WaitForSeconds(4f);
        ShowMessage("Anyway!");
        yield return new WaitForSeconds(1.5f);
        ShowMessage("Let's get the mission done, can't wait to go home and see my lovely kids!");
        yield return new WaitForSeconds(3f);
        GameManager.s_instance.GameStart();
    }
}
