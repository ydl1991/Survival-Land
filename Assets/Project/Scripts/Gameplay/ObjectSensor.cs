using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectSensor : MonoBehaviour
{
    public Camera m_fpsCam;
    public GameObject m_pickUpAndDestroyInstruction;

    private RaycastHit m_hit;
    private int m_sensorLayer;

    // Start is called before the first frame update
    void Awake()
    {
        m_sensorLayer = 1 << 11;
    }

    // Update is called once per frame
    void Update()
    {
        SenseTarget();
    }

    private void SenseTarget()
    {
        if (Physics.Raycast(m_fpsCam.transform.position, m_fpsCam.transform.forward, out m_hit, 2.5f, m_sensorLayer))
        {
            Debug.Log("Sense " + m_hit.transform.name);

            if (!m_pickUpAndDestroyInstruction.activeSelf)
                m_pickUpAndDestroyInstruction.SetActive(true);
            
            if (Input.GetKeyDown(KeyCode.F))
            {
                if (m_hit.transform.CompareTag("ItemChest"))
                    PickUpItemChest(m_hit.transform.gameObject);
                else if (m_hit.transform.CompareTag("SummonCircle"))
                    BreakSummonCircle(m_hit.transform.gameObject);
                
                Destroy(m_hit.transform.gameObject);
            }
        }
        else
            m_pickUpAndDestroyInstruction.SetActive(false);
    }

    private void PickUpItemChest(GameObject chest)
    {
        ItemChest items = chest.GetComponent<ItemChest>();
        ItemComponent comp = GetComponent<ItemComponent>();
        comp.PickUpBullet(items.numAmmo);
        comp.PickUpFirstAidCabinet(items.numFirstAidKit);
    }

    private void BreakSummonCircle(GameObject circle)
    {
        circle.GetComponent<SummonCircle>().ReleaseEnergy();
    }
}
