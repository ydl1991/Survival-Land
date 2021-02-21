using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading;
using UnityEngine;
using System;

public class MissionManager : MonoBehaviour
{
    private static int s_kMaxMissionAllowed = 3;

    // delegate
    public delegate void MissionCompleteSound();
    public static MissionCompleteSound playCompletionSound;
    public delegate void NotifyMissionTarget(TargetType type);
    public static NotifyMissionTarget notifyMissionObjectDown;
    public delegate bool CheckMissionDone();
    public static CheckMissionDone checkMissionDone;
    public delegate void AllMissionsCompleted();
    public static AllMissionsCompleted allMissionsCompleted;
    public delegate void InstantWin();
    public static InstantWin instantWin;

    // prefabs
    public GameObject m_battleMissionPrefab;
    public GameObject m_reconnaissanceMissionPrefab;

    // Seed
    public ulong m_LSystemSeed;
    public int m_numberOfMissions;
    public AudioSource m_missionCompleteSound;
    public AudioSource m_allMissionDoneSound;
    public GameObject m_player;
    public GameObject m_missionPanel;

    private LSystemMissionGenerator m_missionGenerator;
    private XOrShiftRNG m_rng;
    private AnimationCurve m_curve;
    private float[,] m_heightMap;
    private CellularAutomataTerrain m_cellularAutomataTerrain;
    private Animator m_animator;

    // Threading related
    private Thread m_missionSpawnThread;
    private ConcurrentQueue<MissionInfo> m_missionQueue;

    // Mission Complete
    private bool m_missionComplete;

    void Awake()
    {
        m_missionQueue = new ConcurrentQueue<MissionInfo>();
        m_rng = new XOrShiftRNG(m_LSystemSeed);
        m_missionGenerator = new LSystemMissionGenerator(m_numberOfMissions);
        playCompletionSound = PlayMissionCompletionSound;
        notifyMissionObjectDown = NotifyObjectDown;
        checkMissionDone = CheckMissionComplete;
        allMissionsCompleted = MissionCompleted;
        instantWin = ClearMissions;
        m_missionComplete = false;
    }

    void Start()
    {
        m_animator = GetComponent<Animator>();
        m_cellularAutomataTerrain = CellularAutomataTerrain.s_instance;
        m_curve = null;
        m_heightMap = null;
        BuildMissionsInThreads();
    }

    void Update()
    {
        if (m_missionComplete)
            return;

        if (Input.GetKeyDown(KeyCode.Tab))
            m_animator.SetBool("Show", true);
        else if (Input.GetKeyUp(KeyCode.Tab))
            m_animator.SetBool("Show", false);

        int currentNumberOfMissions = m_missionPanel.transform.childCount;
        if (currentNumberOfMissions < s_kMaxMissionAllowed && !m_missionQueue.IsEmpty)
        {
            SpawnMissions(s_kMaxMissionAllowed - currentNumberOfMissions);
        }
    }

    public void ClearMissions()
    {
        // clear all missions in queue
        while (m_missionQueue.TryDequeue(out var item))
		{
			// do nothing
		}

        // clear all current running missions
        foreach (Transform child in m_missionPanel.transform)
        {
            Destroy(child.gameObject);
        }
    }

    public bool CheckMissionComplete()
    {
        if (m_missionPanel.transform.childCount > 0 || !m_missionQueue.IsEmpty)
            return false;

        return true;
    }

    public void MissionCompleted()
    {
        m_missionComplete = true;
        ObjectSpawner.delegateDestroyAllEnemies();
        m_allMissionDoneSound.Play();
    }

    public void PlayMissionCompletionSound()
    {
        m_missionCompleteSound.Play();
    }

    public void NotifyObjectDown(TargetType type)
    {
        foreach (BattleMission mission in m_missionPanel.GetComponentsInChildren<BattleMission>())
        {
            if (mission != null)
            {
                mission.NotifyObjectDown(type);
            }
        }
    }

    private void BuildMissionsInThreads()
    {
        m_missionSpawnThread = null;

        // spawn new thread and start the work
        ThreadStart threadStart = delegate { BuildMission(); };
        m_missionSpawnThread = new Thread(threadStart);
        m_missionSpawnThread.Start();
    }

    private void BuildMission()
    {
        m_missionGenerator.GenerateMissionInfo(m_rng, m_missionQueue);
    }

    private void SpawnMissions(int numberOfMissions)
    {
        if (m_missionSpawnThread == null || m_missionSpawnThread.IsAlive)
            return;

        for (int i = 0; i < numberOfMissions; ++i)
        {
            if (m_missionQueue.IsEmpty)
                return;

            m_missionQueue.TryDequeue(out MissionInfo result);

            if (result.type == MissionType.kBattle)
            {
                SpawnBattleMission(result.description, result.level, result.target);
            }
            else if (result.type == MissionType.kReconnaissance)
            {
                SpawnReconnaissanceMission(result.description, result.level);
            }
        }
    }

    private void SpawnBattleMission(string desc, MissionLevel level, TargetType type)
    {
        GameObject battleMission = Instantiate(m_battleMissionPrefab);
        int targetCount = m_rng.RandomIntRange(5, 10);
        string rewardString = ObjectSpawner.delegateRunRule('I');
        int numSeconds = level == MissionLevel.kMajor ? -1 : m_rng.RandomIntRange(2, 5);
        battleMission.GetComponent<BattleMission>()?.Init(targetCount, rewardString, (float)(numSeconds * 60), desc, type, m_player);
        battleMission.transform.SetParent(m_missionPanel.transform);
    }

    private void SpawnReconnaissanceMission(string desc, MissionLevel level)
    {
        GameObject reconnaissanceMission = Instantiate(m_reconnaissanceMissionPrefab);
        int targetCount = m_rng.RandomIntRange(5, 10);
        string rewardString = ObjectSpawner.delegateRunRule('I');
        int numSeconds = level == MissionLevel.kMajor ? -1 : m_rng.RandomIntRange(2, 5);
        GenerateValidMissionLocation(out int x, out int y);
        reconnaissanceMission.GetComponent<ReconnaissanceMission>()?.Init(new Vector2(x, y), rewardString, (float)(numSeconds * 60), desc, m_player);
        reconnaissanceMission.transform.SetParent(m_missionPanel.transform);
    }

    private void GenerateValidMissionLocation(out int x, out int y)
    {
        if (m_heightMap == null)
            m_heightMap = CellularAutomataTerrain.s_instance.m_region.mapData.heightMap;

        if (m_curve == null)
            m_curve = new AnimationCurve(MapGenerator.s_instance.m_meshHeightCurve.keys);

        // Animation curve is used to transfer mesh position to world position
        int mapSize = MapGenerator.m_kMapChunkSize;

        float topLeftX = (float)(mapSize - 1) / -2f;
        float topLeftZ = (float)(mapSize - 1) / 2f;

        int tempX = m_rng.RandomIntRange(0, mapSize - 1);
        int tempY = m_rng.RandomIntRange(0, mapSize - 1);
        int index = tempY * mapSize + tempX;

        while (!m_cellularAutomataTerrain.m_region.LandType(index, out Nullable<LandType> type) || type == LandType.kWater)
        {
            tempX = m_rng.RandomIntRange(0, mapSize - 1);
            tempY = m_rng.RandomIntRange(0, mapSize - 1);
            index = tempY * mapSize + tempX;
        }

        // using the height curve to find the surface location on mesh, and use mesh's LocalToWorld matrix to convert mesh position to world position
        // for spawning object
        Vector3 meshPos = new Vector3(
            topLeftX + tempX, 
            m_curve.Evaluate(m_heightMap[tempX, tempY]) * MapGenerator.s_instance.m_mapHeightMultiplier, 
            topLeftZ - tempY
        );

        Vector3 worldPos = m_cellularAutomataTerrain.m_region.localToWorldMatrix.MultiplyPoint3x4(meshPos);
        x = Mathf.FloorToInt(worldPos.x);
        y = Mathf.FloorToInt(worldPos.z);
    }
}
