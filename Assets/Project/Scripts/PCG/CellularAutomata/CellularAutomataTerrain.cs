using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.AI;
using System.Threading;
using System;

[System.Serializable]
public struct ObjectPrefabs
{
    public TerrainObjectType m_type;
    public GameObject[] m_prefabs;
}

public class CellularAutomataTerrain : MonoBehaviour
{
    // a class responsible for generating mesh in thread and update terrain
    class LevelOfDetailMesh
    {
        public Mesh m_mesh;
        public bool m_hasRequestedMesh;
        public bool m_hasMesh;
        int m_levelOfDetail;
        System.Action m_updateCallback;

        public LevelOfDetailMesh(int lod, System.Action updateCallback)
        {
            m_levelOfDetail = lod;
            m_updateCallback = updateCallback;
        }

        public void RequestMesh(MapData mapData)
        {
            m_hasRequestedMesh = true;
            m_sMapGenerator.RequestMeshData(mapData, m_levelOfDetail, OnMeshDataReceived);
        }

        void OnMeshDataReceived(MeshData meshData)
        {
            m_mesh = meshData.CreateMesh();
            m_hasMesh = true;

            m_updateCallback();
        }
    }

    // a class that represents the map chunk in game, it will request to generate map data 
    // when it's spawned and trigger mesh generation
    public class TerrainRegion
    {
        public MapData mapData { get; set; }
        public GameObject meshObject { get; set; }
        public Matrix4x4 localToWorldMatrix { get; set; }
        public NavMeshSurface navMeshSurface { get; set; }
        Vector2 position { get; set; }
        Bounds bounds { get; set; }
        MeshRenderer m_meshRenderer;
        MeshFilter m_meshFilter;
        MeshCollider m_meshCollider;
        int m_detailLevel;
        LevelOfDetailMesh m_detailMesh;
        bool m_mapDataReceived;

        public TerrainRegion(Vector2 coord, int size, int detailLevel, Transform parent, Material mat)
        {
            m_detailLevel = detailLevel;

            position = coord * size;
            bounds = new Bounds(position, Vector2.one * size);
            Vector3 positionV3 = new Vector3(position.x, 0, position.y);

            // spawn mesh objects and component for terrain region
            meshObject = new GameObject("Terrain Region");
            m_meshRenderer = meshObject.AddComponent<MeshRenderer>();
            m_meshFilter = meshObject.AddComponent<MeshFilter>();
            m_meshCollider = meshObject.AddComponent<MeshCollider>();
            m_meshRenderer.material = mat;
            m_meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
            meshObject.transform.position = positionV3 * m_playerScale;
            meshObject.transform.parent = parent;
            meshObject.transform.localScale = Vector3.one * m_playerScale;
            localToWorldMatrix = meshObject.transform.localToWorldMatrix;
            SetVisible(false);

            // store level of details of current terrain
            m_detailMesh = new LevelOfDetailMesh(m_detailLevel, UpdateRegion);
            m_sMapGenerator.RequestMapData(position, OnMapDataReceived);
        }

        // create texture from mapData's color map and assign new texture to terrain mesh renderer
        public void UpdateTextureFromMapData()
        {
            Texture2D texture = TextureGenerator.TextureFromColorMap(mapData.colorMap, MapGenerator.m_kMapChunkSize, MapGenerator.m_kMapChunkSize);
            m_meshRenderer.material.mainTexture = texture;            
        }

        // a callback function that gets called when need to form mesh (if not done yet), when assign formed mesh to render terrain
        public void UpdateRegion()
        {
            if (m_mapDataReceived)
            {
                LevelOfDetailMesh lodMesh = m_detailMesh;
                if (lodMesh.m_hasMesh)
                {
                    m_meshFilter.mesh = lodMesh.m_mesh;
                    m_meshCollider.sharedMesh = lodMesh.m_mesh;
                }
                else if (!lodMesh.m_hasRequestedMesh)
                {
                    lodMesh.RequestMesh(mapData);
                }

                SetVisible(true);
            }
        }

        public bool LandType(int index, out Nullable<LandType> type)
        {
            if (index < 0 || index >= mapData.landTypeMap.Length)
            {
                type = null;
                return false;
            }

            type = mapData.landTypeMap[index];
            return true;
        }

        public void AddNavMeshSurface()
        {
            navMeshSurface = meshObject.AddComponent<NavMeshSurface>();
        }

        public void SetVisible(bool visible)
        {
            meshObject.SetActive(visible);
        }

        public bool IsVisible()
        {
            return meshObject.activeSelf;
        }

        void OnMapDataReceived(MapData mapData)
        {
            this.mapData = mapData;
            m_mapDataReceived = true;

            UpdateTextureFromMapData();
            UpdateRegion();
        }
    }

    public static CellularAutomataTerrain s_instance;

    // Mesh data
    public int m_detailLevel;
    public Material m_mapMaterial;

    // Player data
    const float m_playerScale = 5f;

    // Terrain region data
    static MapGenerator m_sMapGenerator;
    public TerrainRegion m_region;
    int m_regionSize;

    // Cellular Automata data
    XOrShiftRNG m_rng;
    public int m_numIterationPerProcess;  // Customized in editor that determines how many iteration the cellular automata runs per round

    // threading data
    Thread[] m_cellularAutomataTerrainBuilders;
    bool m_updateTextureCoroutineInProcess;     // Boolean to record texture update process status
    bool m_updateTextureDone;
    int m_numRegionRows;
    int m_numRegionCols;

    // object generation
    bool m_objectSpawnDone;
    bool m_objectSpawnInProcess;
    bool m_complete;
    public ulong m_humidityMapSeed;
    public ObjectPrefabs[] m_objectPrefabs;
    ConcurrentQueue<Tuple<Vector3, TerrainObjectType>> m_objectSpawnQueue;
    Thread m_objectSpawningThread;

    void Awake()
    {
        s_instance = this;
        
        m_numRegionRows = 3;
        m_numRegionCols = 3;
        m_complete = false;
        m_updateTextureCoroutineInProcess = false;
        m_updateTextureDone = false;
        m_objectSpawnDone = false;
        m_objectSpawnInProcess = false;
        
        m_rng = new XOrShiftRNG();

        // spawn number of threads accordingly, set status of all threads to idle
        m_cellularAutomataTerrainBuilders = new Thread[m_numRegionCols * m_numRegionRows];

        // object spawn queue
        m_objectSpawnQueue = new ConcurrentQueue<Tuple<Vector3, TerrainObjectType>>();
    }

    void Start()
    {
        // create and init map and mesh
        m_sMapGenerator = MapGenerator.s_instance;
        m_regionSize = MapGenerator.m_kMapChunkSize - 1;
        CreateTerrainRegion();
    }

    void Update()
    {
        if (m_complete)
            return;

        // manually update cellular automata by pressing Enter button
        if (m_region.mapData != null && !m_updateTextureDone && !m_updateTextureCoroutineInProcess)
        {
            // go through the number of iteration
            for (int i = 0; i < m_numIterationPerProcess; ++i)
            {
                // update each cell in current iteration in thread
                LandType[] previousState = (LandType[])m_region.mapData.landTypeMap.Clone();
                for (int region = 0; region < m_numRegionRows * m_numRegionCols; ++region)
                {
                    ProcessRegionInThread(previousState, region);
                }

                // make sure all threads finish job before next iteration
                JoinAllTerrainThreads();
            }

            // after iteration is done, update texture on map
            StartCoroutine(ChangeTexture());
        }
        
        if (!m_objectSpawnDone && !m_objectSpawnInProcess && m_updateTextureDone)
        {
            m_objectSpawnInProcess = true;
            StartCoroutine(SafelySpawnObjects());
        }

        if (!m_complete && m_objectSpawnDone)
        {   
            m_complete = true;
            StartCoroutine(BakeNavMeshInCoroutine());
        }
    }

    // A delay process that triggers texture update after all threads finish work
    IEnumerator ChangeTexture()
    {
        m_updateTextureCoroutineInProcess = true;

        // if not all thread finished, come back next frame
        while(!AllTerrainThreadsDone())
        {
            yield return null;
        }

        // update texture after all thread finish
        m_region.UpdateTextureFromMapData();
        m_updateTextureCoroutineInProcess = false;
        m_updateTextureDone = true;
    }

    IEnumerator SafelySpawnObjects()
    {
        // if we are not finishing update mesh, wait till it's finish
        while(m_updateTextureCoroutineInProcess)
        {
            yield return null;
        }

        // start spawn object thread and start coroutine after thread has spawned
        StartObjectSpawningThread();
        StartCoroutine(SpawnObjects());
    }

    IEnumerator BakeNavMeshInCoroutine()
    {
        yield return null;
        m_region.AddNavMeshSurface();
        AsyncOperation asyncOperation = NavMeshExtention.BuildNavMeshAsync(m_region.navMeshSurface);
        asyncOperation.allowSceneActivation = false;
        
        //When the load is still in progress, output the Text and progress bar
        while (!asyncOperation.isDone)
        {
            yield return null;
        }

        //Activate the Scene
        asyncOperation.allowSceneActivation = true;
    }

    private void ProcessRegionInThread(LandType[] prevState, int regionIndex)
    {
        // clear outdated thread
        m_cellularAutomataTerrainBuilders[regionIndex] = null;

        // spawn new thread and start the work
        ThreadStart threadStart = delegate { UpdateCells(prevState, regionIndex); };
        m_cellularAutomataTerrainBuilders[regionIndex] = new Thread(threadStart);
        m_cellularAutomataTerrainBuilders[regionIndex].Start();
    }

    // finish all thread work
    private void JoinAllTerrainThreads()
    {
        Array.ForEach(m_cellularAutomataTerrainBuilders, thread => thread.Join());
    }

    // check if all threads are finished
    private bool AllTerrainThreadsDone()
    {
        return Array.FindIndex(m_cellularAutomataTerrainBuilders, thread => thread.IsAlive) == -1;
    }

    private void UpdateCells(LandType[] prevState, int regionIndex)
    {
        int regionRow = regionIndex / m_numRegionCols;
        int regionCol = regionIndex % m_numRegionCols;
        int numCellPerRegionRow = (MapGenerator.m_kMapChunkSize / m_numRegionRows);
        int numCellPerRegionCol = (MapGenerator.m_kMapChunkSize / m_numRegionCols);
        int regionCellStartRow = regionRow * numCellPerRegionRow;
        int regionCellStartCol = regionCol * numCellPerRegionCol;

        // apply rules to each cell within the current region
        for (int row = regionCellStartRow; row < regionCellStartRow + numCellPerRegionRow; ++row)
        {
            for (int col = regionCellStartCol; col < regionCellStartCol + numCellPerRegionCol; ++col)
            {
                ApplyRules(prevState, row, col);
            }
        }
    }

    private void ApplyRules(LandType[] stateArr, int row, int col)
    {
        int[] kDirRow = { -1, -1, -1,  0, 0,  1, 1, 1 };
        int[] kDirCol = { -1,  0,  1, -1, 1, -1, 0, 1 };
        int mapSize = MapGenerator.m_kMapChunkSize;

        int numWater = 0;
        int numPlants = 0;
        int numRock = 0;
        int numSnow = 0;

        // go through each of the 8 neighbors and find what land type they are and record the type
        for (int i = 0; i < kDirRow.Length; ++i)
        {
            int newRow = row + kDirRow[i];
            int newCol = col + kDirCol[i];

            if (newRow < 0 || newRow >= mapSize || newCol < 0 || newCol >= mapSize)
                continue;
            
            int newIndex = newRow * mapSize + newCol;

            if (stateArr[newIndex] == LandType.kSnow)
                numSnow += 1;
            else if (stateArr[newIndex] == LandType.kRock)
                numRock += 1;
            else if (stateArr[newIndex] == LandType.kPlants)
                numPlants += 1;
            else if (stateArr[newIndex] == LandType.kWater)
                numWater += 1;
        }

        // scoring the current cell according to neighbors
        ScoreLandType(stateArr[row * mapSize + col], row, col, numWater, numPlants, numRock, numSnow);
    }

    private void ScoreLandType(LandType cur, int row, int col, int numWater, int numPlants, int numRock, int numSnow)
    {
        // initial score of each land type set to corresponding number of neighbors in that land type
        float snowLandScore = numSnow;
        float rockLandScore = numRock;
        float plantsLandScore = numPlants;
        float waterLandScore = numWater;

        // Adjust score by neighbor type
        //
        // if current land type is rock, and more than 2 neighbors are grass, add 2 to snow score 
        // and if there is snow around, add 1 to water score, as by humid environment
        if (cur == LandType.kRock)
        {
            if (numPlants > 2)
                snowLandScore += 2f;
            else if (numSnow > 0)
                waterLandScore += 1f;
        }
        // if current land type is grass, and number of neighbor grasses are between 4 and 5, plus there are
        // water source, add grass score making the grass more likely to stay, otherwise add snow score
        else if (cur == LandType.kPlants)
        {
            if (4 <= numPlants && numPlants <= 5 && (numWater > 0 || numSnow > 0))
                plantsLandScore += 1f;
            else
                snowLandScore += 1f;
        }
        // if current land type is water and is surrounded by all rock, add water score, make the water more likely to stay
        // else if number of rock are greater than 3, add grass score as grass spreads
        else if (cur == LandType.kWater)
        {
            if (numRock == 8)
                waterLandScore += 1f;
            if (numRock > 3)
                plantsLandScore += 2f;
        }

        // Adjust score by height
        float height = m_region.mapData.heightMap[col, row];
        // if current cell is at a higher depth, it is more likely to accumulate snow
        if (height <= m_sMapGenerator.m_regions[(int)LandType.kSnow].m_depth)
        {
            waterLandScore = 0;
            plantsLandScore *= 0.1f;
            rockLandScore *= 0.5f;
            snowLandScore *= 2f;
        }
        // if at middle high depth, rocks are usually seen with slight number of grass and snow
        else if (height <= m_sMapGenerator.m_regions[(int)LandType.kRock].m_depth)
        {
            waterLandScore = 0;
            rockLandScore *= 2f;
            plantsLandScore *= 0.4f;
            snowLandScore *= 0.2f;
        }
        // if at middle low depth, grass are usually seen with some rocks and water
        else if (height <= m_sMapGenerator.m_regions[(int)LandType.kPlants].m_depth)
        {
            snowLandScore = 0;
            waterLandScore *= 0.3f;
            plantsLandScore *= 1.5f;
            rockLandScore *= 0.1f;
        }
        // if at lower depth, water are likely to accumulate and form lake
        else if (height <= m_sMapGenerator.m_regions[(int)LandType.kWater].m_depth)
        {
            snowLandScore = 0;
            rockLandScore = 0;
            plantsLandScore *= 0.2f;
            waterLandScore *= 2;
        }

        float totalChance = snowLandScore + rockLandScore + plantsLandScore + waterLandScore;

        // if all score equals 0, skip
        if (totalChance == 0f)
            return;

        // add weighted random to assign room type, item1: probability, item2: land type
        List<Tuple<float, LandType>> chanceArr = new List<Tuple<float, LandType>>();
        if (snowLandScore > 0)
            chanceArr.Add(Tuple.Create(snowLandScore, LandType.kSnow));
        if (rockLandScore > 0)
            chanceArr.Add(Tuple.Create(rockLandScore, LandType.kRock));
        if (plantsLandScore > 0)
            chanceArr.Add(Tuple.Create(plantsLandScore, LandType.kPlants));
        if (waterLandScore > 0)
            chanceArr.Add(Tuple.Create(waterLandScore, LandType.kWater));

        int index = 0;
        float roll = m_rng.RandomFloatRange(0f, totalChance);

        while (true)
        {
            if (index >= chanceArr.Count)
                return;

            roll -= chanceArr[index].Item1;
            if (roll < 0)
                break;
            ++index;
        }

        // update land type and color
        int cellIndex = row * MapGenerator.m_kMapChunkSize + col;
        LandType type = chanceArr[index].Item2;
        m_region.mapData.landTypeMap[cellIndex] = type;
        m_region.mapData.colorMap[cellIndex] = m_sMapGenerator.m_regions[(int)type].m_color;
    }

    private void StartObjectSpawningThread()
    {
        // spawn new thread and start the work
        ThreadStart threadStart = delegate { GenerateObjectOnTerrain(); };
        m_objectSpawningThread = new Thread(threadStart);
        m_objectSpawningThread.Start();
    }

    private void GenerateObjectOnTerrain()
    {
        // Animation curve is used to transfer mesh position to world position
        AnimationCurve heightCurveTemp = new AnimationCurve(m_sMapGenerator.m_meshHeightCurve.keys);

        int mapSize = MapGenerator.m_kMapChunkSize;

        // Humidity Map is a random noise map and is used together with perlin noise height map to generate biomes on map
        float[,] humidityMap = MapNoise.Generate2DRandomNoise(mapSize, mapSize, m_humidityMapSeed);

        float topLeftX = (float)(mapSize - 1) / -2f;
        float topLeftZ = (float)(mapSize - 1) / 2f;

        for (int y = 0; y < mapSize; ++y)
        {
            for (int x = 0; x < mapSize; ++x)
            {
                float currentHumidity = humidityMap[x, y];
                LandType landType = m_region.mapData.landTypeMap[y * mapSize + x];
                var objectType = SelectSpawningObjectByLandTypeAndHumidity(landType, currentHumidity);
                if (objectType.HasValue)
                {
                    // using the height curve to find the surface location on mesh, and use mesh's LocalToWorld matrix to convert mesh position to world position
                    // for spawning object
                    Vector3 meshPos = new Vector3(
                            topLeftX + x, 
                            heightCurveTemp.Evaluate(m_region.mapData.heightMap[x, y]) * m_sMapGenerator.m_mapHeightMultiplier, 
                            topLeftZ - y
                        );
                    Vector3 worldPos = m_region.localToWorldMatrix.MultiplyPoint3x4(meshPos);
                    
                    // pass in world pos and prefab reference to spawn object on map
                    m_objectSpawnQueue.Enqueue(Tuple.Create(worldPos, objectType.Value));
                }
            }
        }
    }

    private Nullable<TerrainObjectType> SelectSpawningObjectByLandTypeAndHumidity(LandType landType, float humidity)
    {
        foreach (var condition in m_sMapGenerator.m_regions[(int)landType].m_objectSpawnConditions)
        {
            if (humidity < condition.m_humility)
            {
               return condition.m_type;
            }
        }

        return null;
    }

    IEnumerator SpawnObjects()
    {
        // if not all thread finished, come back next frame
        while(m_objectSpawningThread.IsAlive || m_objectSpawnQueue.Count > 0)
        {
            if (m_objectSpawnQueue.Count > 0)
            {
                if (!m_objectSpawnQueue.TryDequeue(out var spawnData))
                {
                    yield return null;
                }

                SpawnObjectAtWorld(spawnData.Item1, spawnData.Item2);
            }
        }

        m_objectSpawningThread = null;
        m_objectSpawnInProcess = false;
        m_objectSpawnDone = true;
    }

    private void SpawnObjectAtWorld(Vector3 pos, TerrainObjectType type)
    {
        var prefabs = m_objectPrefabs[(int)type].m_prefabs;
        var spawnPrefab = prefabs[m_rng.RandomIntRange(0, prefabs.Length)];
        GameObject newObject = Instantiate(spawnPrefab, pos, Quaternion.identity);
        newObject.transform.parent = m_region.meshObject.transform;
    }

    private void CreateTerrainRegion()
    {
        // create terrain
        m_region = new TerrainRegion(Vector2.zero, m_regionSize, m_detailLevel, transform, m_mapMaterial);
    }
}
