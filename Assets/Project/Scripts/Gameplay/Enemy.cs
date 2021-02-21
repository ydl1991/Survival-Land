using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class Enemy : MonoBehaviour, SpawnedObject
{
    public float m_attackSpeed;

    private GameObject m_player;
    private NavMeshAgent m_agent;
    private HealthComponent m_health;
    private Animator m_animator;
    private float m_attackCoolDown;
    private float m_pathUpdateTime;
    private float m_currentTime;
    private NavMeshPath path;
    private ObjectSpawner m_spawner;
    private bool m_dieing;

    void Awake()
    {
        m_animator = GetComponent<Animator>();
        m_health = GetComponent<HealthComponent>();
        m_agent = GetComponent<NavMeshAgent>();
        path = new NavMeshPath();
    }

    // Start is called before the first frame update
    void Start()
    {
        m_dieing = false;
        m_player = GameManager.s_instance.m_player;
        m_pathUpdateTime = 2f;
        m_currentTime = m_pathUpdateTime;
    }

    public void Init(ObjectSpawner spawner)
    {
        m_spawner = spawner;
    }

    // Update is called once per frame
    void Update()
    {
        if (m_health.alive)
        {
            m_currentTime += Time.deltaTime;
            if (m_currentTime > m_pathUpdateTime && m_agent.isOnNavMesh)
            {
                m_currentTime -= m_pathUpdateTime;
                NavMesh.CalculatePath (transform.position, m_player.transform.position, m_agent.areaMask, path);
                m_agent.SetPath (path);
            }

            FaceTarget();

            if (m_attackCoolDown > 0f)
                m_attackCoolDown -= Time.deltaTime;

            float distance = Vector3.Distance(m_player.transform.position, transform.position);
            if (distance <= m_agent.stoppingDistance && m_attackCoolDown <= 0f)
            {
                Attack();
                m_attackCoolDown = 1f / m_attackSpeed;
            }
        }
        else if (!m_health.alive && !m_dieing)
            Die();
    }

    private void FaceTarget()
    {
        Vector3 direction = (m_player.transform.position - transform.position).normalized;
        Quaternion lookRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 4f);
    }
    
    private void Die()
    {
        m_dieing = true;
        m_agent.isStopped = true;
        m_animator.SetTrigger("Die");
        Reward();
        gameObject.SetActive(false);
        Destroy(gameObject);
        MissionManager.notifyMissionObjectDown(TargetType.kEnemy);
    }

    private void Attack()
    {
        m_animator.SetBool("Moving", false);
        m_animator.SetTrigger("Attack");
        m_player.GetComponent<HealthComponent>().ChangeHealth(-10f);
    }

    private void Reward()
    {
        CanvasManager.s_instance.AddDisplayText("You killed an enemy.");
        
        string rewardItem = m_spawner.RunRule('E');
        foreach (char c in rewardItem)
        {
            if (c == '_')
                continue;
                
            Vector3 pos = transform.position;
            pos.y += 0.5f;
            m_spawner.SpawnObjectFromChar(c, pos);
            CanvasManager.s_instance.AddDisplayText("A " + SpawningGrammar.GetDescription(c) + " is dropped from the enemy.");
        }
    }
}
