using System.Collections;
using System.Collections.Concurrent;
using UnityEngine;
using System.Threading;
using System;

public enum GameObjectType
{
    kEnemy = 0,
    kSummoningCircle,
    kItemContainer,
    kNone
}

public interface SpawnedObject
{
    void Init(ObjectSpawner spawner);
}

[System.Serializable]
public struct ObjectSpawnData
{
    public GameObjectType m_type;
    public GameObject m_prefab;
}

public class ObjectSpawner : MonoBehaviour
{
    // delegate
    public delegate string DelegateRunRule(char c);
    public static DelegateRunRule delegateRunRule;
    public delegate void DelegateDestroyAllEnemy();
    public static DelegateDestroyAllEnemy delegateDestroyAllEnemies;

    // Prefabs
    public ObjectSpawnData[] m_spawnData;
    public GameObject m_spaceTunnel;
    
    // Seed
    public ulong m_objectSpawnerSeed;
    // Timer
    public float m_timeGapPerSpawningWave;
    // ObjectHolder
    public GameObject m_objectHolder;
    public GameObject m_enemyHolder;
    public GameObject m_magicCircleHolder;

    private SpawningGrammar m_formalGrammar;        // Formal Grammar
    private MapGenerator m_mapGenerator;
    private CellularAutomataTerrain m_cellularAutomataTerrain;
    private XOrShiftRNG m_reproducingRng;
    private XOrShiftRNG m_rewardRng;
    private string m_currentFormation;

    // Threading related
    private Thread m_spawnInfoThread;
    private ConcurrentQueue<Tuple<Vector3, GameObjectType>> m_objectSpawnQueue;

    void Awake()
    {
        m_currentFormation = string.Empty;
        m_objectSpawnQueue = new ConcurrentQueue<Tuple<Vector3, GameObjectType>>();
        m_reproducingRng = new XOrShiftRNG(m_objectSpawnerSeed);
        m_rewardRng = new XOrShiftRNG();
        m_formalGrammar = new SpawningGrammar();
        delegateRunRule = RunRule;
        delegateDestroyAllEnemies = DestroyAllEnemies;
    }

    void Start()
    {
        m_mapGenerator = MapGenerator.s_instance;
        m_cellularAutomataTerrain = CellularAutomataTerrain.s_instance;
    }

    public void NotifySpawnNextWave()
    {
        RunNextIteration();
        ProcessSpawnInfoInThread();
        StartCoroutine(SpawnObjects());
    }

    public void ProcessSpawnInfoInThread()
    {
        m_spawnInfoThread = null;

        // spawn new thread and start the work
        ThreadStart threadStart = delegate { ProcessSpawnInfo(); };
        m_spawnInfoThread = new Thread(threadStart);
        m_spawnInfoThread.Start();
    }

    public void DestroyAllEnemies()
    {
        foreach (Transform child in m_enemyHolder.transform)
        {
            Destroy(child.gameObject);
        }
    }

    // Run Grammar Rules
    public void RunNextIteration()
    {
        if (m_currentFormation == string.Empty)
            m_currentFormation = m_formalGrammar.InitialGeneration(m_reproducingRng);
        else
            m_currentFormation = m_formalGrammar.CalculateNextFormation(m_currentFormation, m_reproducingRng);

        Debug.Log("Current Formation: " + m_currentFormation);
    }

    public GameObjectType CharToObjectType(char c)
    {
        switch (c)
        {
            case 'E':
                return GameObjectType.kEnemy;
            case 'C':
                return GameObjectType.kSummoningCircle;
            case 'I':
                return GameObjectType.kItemContainer;
        }

        return GameObjectType.kNone;
    }

    public string RunRule(char c)
    {
        return m_formalGrammar.RunResultRule(c, m_rewardRng);
    }

    public void SpawnObjectFromChar(char c, Vector3 pos)
    {
        GameObjectType spawnType = CharToObjectType(c);
        SpawnObjectAtWorld(pos, spawnType);
    }

    private void ProcessSpawnInfo()
    {
        // Animation curve is used to transfer mesh position to world position
        AnimationCurve heightCurveTemp = new AnimationCurve(m_mapGenerator.m_meshHeightCurve.keys);
        var heightMap = m_cellularAutomataTerrain.m_region.mapData.heightMap;
        int mapSize = MapGenerator.m_kMapChunkSize;

        float topLeftX = (float)(mapSize - 1) / -2f;
        float topLeftZ = (float)(mapSize - 1) / 2f;

        for (int i = 0; i < m_currentFormation.Length; ++i)
        {
            if (m_currentFormation[i] == '_')
                continue;

            GameObjectType spawnType = CharToObjectType(m_currentFormation[i]);

            GenerateValidSpawnCoordinate(mapSize, spawnType, out int x, out int y);

            // using the height curve to find the surface location on mesh, and use mesh's LocalToWorld matrix to convert mesh position to world position
            // for spawning object
            Vector3 meshPos = new Vector3(
                topLeftX + x, 
                heightCurveTemp.Evaluate(heightMap[x, y]) * m_mapGenerator.m_mapHeightMultiplier, 
                topLeftZ - y
            );

            // object is set to spawn at y = 50f on the air, and drop to the ground instead of spawn directly on the ground.
            Vector3 worldPos = m_cellularAutomataTerrain.m_region.localToWorldMatrix.MultiplyPoint3x4(meshPos);
            worldPos.y += 1.5f;

            m_objectSpawnQueue.Enqueue(Tuple.Create(worldPos, spawnType));
        }
    }

    private void GenerateValidSpawnCoordinate(int mapSize, GameObjectType spawnType, out int x, out int y)
    {
        x = m_reproducingRng.RandomIntRange(0, mapSize - 1);
        y = m_reproducingRng.RandomIntRange(0, mapSize - 1);
        int index = y * mapSize + x;

        while (!m_cellularAutomataTerrain.m_region.LandType(index, out Nullable<LandType> type) || type == LandType.kWater)
        {
            x = m_reproducingRng.RandomIntRange(0, mapSize - 1);
            y = m_reproducingRng.RandomIntRange(0, mapSize - 1);
            index = y * mapSize + x;
        }
    }

    IEnumerator SpawnObjects()
    {
        // if not all thread finished, come back next frame
        while(m_spawnInfoThread.IsAlive || m_objectSpawnQueue.Count > 0)
        {
            if (m_objectSpawnQueue.Count > 0)
            {
                if (!m_objectSpawnQueue.TryDequeue(out var spawnData))
                {
                    yield return null;
                }

                SpawnObjectAtWorld(spawnData.Item1, spawnData.Item2);
                SpawnSpaceTunel(spawnData.Item1);
            }

            yield return null;
        }

        m_spawnInfoThread = null;
    }

    public void SpawnObjectAtWorld(Vector3 pos, GameObjectType type)
    {
        var prefab = m_spawnData[(int)type].m_prefab;
        GameObject newObject = Instantiate(prefab, pos, prefab.transform.rotation);

        if (type == GameObjectType.kEnemy)
        {
            newObject.transform.parent = m_enemyHolder.transform;
        }
        else if (type == GameObjectType.kSummoningCircle)
        {
            newObject.transform.parent = m_magicCircleHolder.transform;
        }
        else
        {
            newObject.transform.parent = m_objectHolder.transform;
        }

        newObject.GetComponent<SpawnedObject>()?.Init(this);
    }

    private void SpawnSpaceTunel(Vector3 pos)
    {
        pos.y = 56f;
        GameObject spaceTunel = Instantiate(m_spaceTunnel, pos, m_spaceTunnel.transform.rotation);
        spaceTunel.transform.parent = m_objectHolder.transform;
    }
}
