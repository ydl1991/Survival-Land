using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using UnityEngine;
using System.Threading;
using System;

public class CloudGenerator : MonoBehaviour
{
    // cell state for cellular automata
    public enum CellState
    {
        kNoCloud = 0,
        kHasCloud,
        kHasRainCloud,
    }

    // ------------------- //
    //      Prafebs
    // ------------------- //
    [SerializeField] GameObject[] m_cloudPrefabs = null;

    // ------------------- //
    //    Static & Const
    // ------------------- //
    const float k_cloudSpawnWetLevel = 0.8f;

    // ------------------- //
    //  Public Variables
    // ------------------- //
    public float m_cloudLayerWidth;
    public float m_cloudLayerHeight;
    public float m_cloudLayerY;
    public int m_numRegionRows;
    public int m_numRegionCols;
    public int m_numCellColumns;
    public int m_numCellRows;
    public float m_cloudSpawnChance;

    // ------------------- //
    //  Private Variables
    // ------------------- //
    GridSimulator m_grid;
    XOrShiftRNG m_rng;
    ConcurrentQueue<int> m_processActiveCloudCells;
    ConcurrentQueue<int> m_processInactiveCloudCells;
    ConcurrentQueue<int> m_idleClouds;  // stores previously spawned cloud indices that are currently idle, use to recycle in other place
    List<GameObject> m_clouds;

    // all arrays combine to provide cell info, index represents the cell index
    CellState[] m_cellStates;
    float[] m_cellHumidness;
    int[] m_cellCloudIndices;
    float[] m_humidUpScaleMultiplier;
    float[] m_humidDownScaleMultiplier;

    // thread
    bool[] m_threadWoring;
    bool m_activationCoroutineInProcess;
    bool m_deactivationCoroutineInProcess;
    object m_locker;
    bool m_running;

    void Awake()
    {
        m_rng = new XOrShiftRNG();

        // Init grid
        GridInitializer initializer = new GridInitializer();
        initializer.mapWidth = m_cloudLayerWidth;
        initializer.mapHeight = m_cloudLayerHeight;
        initializer.numRegionRows = m_numRegionRows;
        initializer.numRegionColumns = m_numRegionCols;
        initializer.numCellColumns = m_numCellColumns;
        initializer.numCellRows = m_numCellRows;
        m_grid = new GridSimulator(initializer);

        // create cell arrays
        m_cellStates = new CellState[m_numCellColumns * m_numCellRows];
        m_cellHumidness = new float[m_numCellColumns * m_numCellRows];
        m_cellCloudIndices = new int[m_numCellColumns * m_numCellRows];
        m_humidUpScaleMultiplier = new float[m_numCellColumns * m_numCellRows];
        m_humidDownScaleMultiplier = new float[m_numCellColumns * m_numCellRows];

        // create data structure to store clouds
        //m_clouds = new ConcurrentDictionary<int, GameObject>();
        m_clouds = new List<GameObject>();
        m_idleClouds = new ConcurrentQueue<int>();
        m_processActiveCloudCells = new ConcurrentQueue<int>();
        m_processInactiveCloudCells = new ConcurrentQueue<int>();
        m_locker = new object();

        // init thread states
        m_threadWoring = new bool[m_numRegionCols * m_numRegionRows];
        Array.ForEach(m_threadWoring, x => x = false);
        m_activationCoroutineInProcess = false;
        m_deactivationCoroutineInProcess = false;
        m_running = true;
    }

    // Start is called before the first frame update
    void Start()
    {
        // generate initial state
        GenerateInitialStates();
    }

    // Update is called once per frame
    void Update()
    {
        // automatically iterate each frame to update cell state
        if (m_running && !m_activationCoroutineInProcess && !m_deactivationCoroutineInProcess)
        {
            CellState[] previousState = (CellState[])m_cellStates.Clone();
            // spawn thread in each region to process cellular automata
            for (int i = 0; i < m_numRegionRows * m_numRegionCols; ++i)
            {
                ProcessRegionInThread(previousState, i);
            }

            // process clouds update after cellular automata iteration in coroutines
            StartCoroutine(ActivateClouds());
            StartCoroutine(DeactivateClouds());
        }

        // press P to pause or play iteration
        if (Input.GetKeyDown(KeyCode.P))
        {
            m_running = !m_running;
        }
    }

    private void ProcessRegionInThread(CellState[] prevState, int regionIndex)
    {
        lock (m_locker)
        {
            m_threadWoring[regionIndex] = true;
        }

        ThreadStart threadStart = delegate { UpdateCells(prevState, regionIndex); };
        new Thread(threadStart).Start();
    }

    private void UpdateCells(CellState[] prevState, int regionIndex)
    {
        int regionRow = regionIndex / m_numRegionCols;
        int regionCol = regionIndex % m_numRegionCols;
        int regionCellStartRow = regionRow * (m_numCellRows / m_numRegionRows);
        int regionCellStartCol = regionCol * (m_numCellColumns / m_numRegionCols);

        // go through each cell in the current region, apply rules to it
        for (int row = regionCellStartRow; row < regionCellStartRow + (m_numCellRows / m_numRegionRows); ++row)
        {
            for (int col = regionCellStartCol; col < regionCellStartCol + (m_numCellColumns / m_numRegionCols); ++col)
            {
                int index = m_grid.CoordinateToIndex(row, col);
                ApplyRules(prevState, index);
            }
        }

        lock (m_locker)
        {
            m_threadWoring[regionIndex] = false;
        }
    }

    // check if all threads finished work
    private bool AllThreadsDone()
    {
        lock (m_locker)
        {
            return Array.FindIndex(m_threadWoring, x => x == true) == -1;
        }
    }

    // coroutine to set clouds to correct position and render it
    IEnumerator ActivateClouds()
    {
        m_activationCoroutineInProcess = true;
        while (!AllThreadsDone() || m_processActiveCloudCells.Count > 0)
        {
            if (m_processActiveCloudCells.Count > 0)
            {
                if (!m_processActiveCloudCells.TryDequeue(out int index))
                {
                    yield return null;
                }

                int cloudIndex = ActivateCloud(index);
                m_cellCloudIndices[index] = cloudIndex;
            }
            yield return null;
        }
        m_activationCoroutineInProcess = false;
        
    }

    // coroutine to hide clouds that previously show on cell
    IEnumerator DeactivateClouds()
    {
        m_deactivationCoroutineInProcess = true;
        while (!AllThreadsDone() || m_processInactiveCloudCells.Count > 0)
        {
            if (m_processInactiveCloudCells.Count > 0)
            {
                if (!m_processInactiveCloudCells.TryDequeue(out int cloudIndex))
                {
                    yield return null;
                }

                // cloud disappear
                DeactivateCloud(cloudIndex);
            }
            yield return null;
        }
        m_deactivationCoroutineInProcess = false; 
    }

    private void GenerateInitialStates()
    {
        for (int i = 0; i < m_numCellRows * m_numCellColumns; ++i)
        {
            CreateNewCell(i);
            if (m_cellStates[i] == CellState.kHasCloud)
            {
                int cloudIndex = SpawnNewCloudAt(i);
                m_cellCloudIndices[i] = cloudIndex;
            }
        }
    }

    private void ApplyRules(CellState[] stateArr, int index)
    {
        int[] kDirRow = { -1, -1, -1,  0, 0,  1, 1, 1 };
        int[] kDirCol = { -1,  0,  1, -1, 1, -1, 0, 1 };

        Coordinate coord = m_grid.IndexToCoordinate(index);
        int numAdjacentClouds = 0;
        int numAdjacentRainClouds = 0;

        // go through each neighbor and count neighbor cell states
        for (int i = 0; i < kDirRow.Length; ++i)
        {
            int newRow = coord.row + kDirRow[i];
            int newCol = coord.col + kDirCol[i];

            if (!m_grid.IsValidCell(newRow, newCol))
                continue;
            
            int newIndex = m_grid.CoordinateToIndex(newRow, newCol);

            if (stateArr[newIndex] == CellState.kHasCloud)
                numAdjacentClouds += 1;
            else if (stateArr[newIndex] == CellState.kHasRainCloud)
                numAdjacentRainClouds += 1;
        }

        // apply different rules according to the current cell state
        if (stateArr[index] == CellState.kNoCloud)
        {
            ApplyNoCloudRules(index, numAdjacentClouds, numAdjacentRainClouds);
        }
        else if (stateArr[index] == CellState.kHasCloud)
        {
            ApplyCloudRules(index, numAdjacentClouds, numAdjacentRainClouds);
        }    
        else
        {
            ApplyRainCloudRules(index, numAdjacentClouds, numAdjacentRainClouds);
        }
    }

    // Case 1: current cell has no cloud
    //      - if no clouds or rain clouds are around, absort humidness normally;
    //      - if between 1 and 4 clouds around, absort humidness by multiplier * 0.5, as less resource exists
    //      - if more than 4 clouds or rain clouds are around, absort humidness by scaling multiplier * 0.2;
    //      - if humidness is greater than 0.6, there is a chance to becomes cloud
    private void ApplyNoCloudRules(int index, int numAdjacentClouds, int numAdjacentRainClouds)
    {
        // rules
        if (numAdjacentClouds == 0 && numAdjacentRainClouds == 0)
        {
            m_cellHumidness[index] += m_humidUpScaleMultiplier[index];
        }
        else if (numAdjacentClouds > 4 || numAdjacentRainClouds > 4 || numAdjacentRainClouds + numAdjacentClouds > 4)
        {
            m_cellHumidness[index] += m_humidUpScaleMultiplier[index] * 0.2f;
        }
        else
        {
            m_cellHumidness[index] += m_humidUpScaleMultiplier[index] * 0.5f;
        }

        if (m_cellHumidness[index] > k_cloudSpawnWetLevel && m_rng.RandomFloat() < m_cloudSpawnChance)
        {
            m_cellStates[index] = CellState.kHasCloud;
            m_processActiveCloudCells.Enqueue(index);
        }
    }

    // Rules:
    // Case 2: current cell is a cloud
    //      - if number of adjacent clouds or rain cloud is greater than 6, cloud becomes rain cloud.
    //      - if number of adjacent rain clouds are greater than 3, humidness scale up 4 times as fast
    //      - if number of adjacent rain clouds are between 2 and 3, humidness scale up 2 times as fast
    //      - if no adjacent clouds or rain clouds exists, the cloud slowly release humidness.
    //      - if number of adjacent clouds are less than 4, humidness scale up normally.
    private void ApplyCloudRules(int index, int numAdjacentClouds, int numAdjacentRainClouds)
    {
        // rules
        if (numAdjacentRainClouds > 6 || numAdjacentClouds > 6 || numAdjacentClouds + numAdjacentRainClouds > 6)
        {
            m_cellHumidness[index] = 1f;
        }
        else if (numAdjacentRainClouds > 3)
        {
            m_cellHumidness[index] += m_humidUpScaleMultiplier[index] * 4f;
        }
        else if (numAdjacentRainClouds > 1)
        {
            m_cellHumidness[index] += m_humidUpScaleMultiplier[index] * 2f;
        }
        else if (numAdjacentClouds + numAdjacentRainClouds < 1)
        {
            m_cellHumidness[index] -= m_humidDownScaleMultiplier[index] * 0.2f;
        }
        else if (numAdjacentClouds < 4)
        {
            m_cellHumidness[index] += m_humidUpScaleMultiplier[index];
        }

        if (m_cellHumidness[index] >= 1f)
        {
            m_cellStates[index] = CellState.kHasRainCloud;
        }
        else if (m_cellHumidness[index] < 0.2f)
        {
            m_cellStates[index] = CellState.kNoCloud;
            m_processInactiveCloudCells.Enqueue(m_cellCloudIndices[index]);
            // reset cell cloud id to -1
            m_cellCloudIndices[index] = -1;
        }
    }

    // Case 3: current cell is a rain cloud
    //      - if number of adjacent rain clouds is 8, release humidness 10 times faster;
    //      - if number of adjacent rain clouds is between 6 and 7, release humidness 7 times faster;
    //      - if number of adjacent rain clouds is between 3 and 5, release humidness * 2 times faster;
    //      - otherwise release as normal
    private void ApplyRainCloudRules(int index, int numAdjacentClouds, int numAdjacentRainClouds)
    {
        if (numAdjacentRainClouds == 8)
        {
            m_cellHumidness[index] -= m_humidDownScaleMultiplier[index] * 10f;
        }
        if (numAdjacentRainClouds > 5)
        {
            m_cellHumidness[index] -= m_humidDownScaleMultiplier[index] * 7f;
        }
        else if (numAdjacentRainClouds > 3)
        {
            m_cellHumidness[index] -= m_humidDownScaleMultiplier[index] * 2f;
        }
        else if (numAdjacentRainClouds > 0)
        {
            m_cellHumidness[index] -= m_humidDownScaleMultiplier[index];
        }

        // state change check
        if (m_cellHumidness[index] <= 0.2f)
        {
            m_cellStates[index] = CellState.kNoCloud;
            m_processInactiveCloudCells.Enqueue(m_cellCloudIndices[index]);
            // reset cell cloud id to -1
            m_cellCloudIndices[index] = -1;
        }
    }

    private void DeactivateCloud(int cloudId)
    {
        // deactivate cloud
        m_clouds[cloudId].SetActive(false);
        //m_clouds[cloudId].transform.position = new Vector3(-1000f, 0, -1000f);
        // push cloud id to idle cloud queue for recycle
        m_idleClouds.Enqueue(cloudId);
    }

    private int ActivateCloud(int cellIndex)
    {
        int cloudIndex = 0;

        // if we have idle clouds, use it
        if (m_idleClouds.Count > 0)
        {
            m_idleClouds.TryDequeue(out cloudIndex);
            // get cell position
            Vector3 pos = m_grid.IndexToWorldPositionCentered(cellIndex);
            pos.y = m_cloudLayerY;
            // set cloud to the cell position
            m_clouds[cloudIndex].transform.position = pos;
            // activate cloud
            m_clouds[cloudIndex].SetActive(true);
        }
        // otherwise we spawn new cloud
        else
        {
            cloudIndex = SpawnNewCloudAt(cellIndex);
        }

        return cloudIndex;
    }

    // create and init cell data
    private void CreateNewCell(int index)
    {
        m_cellCloudIndices[index] = -1;
        
        // generate random starting cell humidness between 0 and 1
        m_cellHumidness[index] = m_rng.RandomFloat();
        
        // generate random humidness up scaling multiplier and down scaling multiplier
        // set to small number because we don't want to generate and dispose clouds too frequently
        m_humidUpScaleMultiplier[index] = m_rng.RandomFloatRange(0.0001f, 0.001f);
        m_humidDownScaleMultiplier[index] = m_rng.RandomFloatRange(0.01f, 0.08f);
        
        // if current humidness is greater and equal to cloud spawn level, there is a chance to spawn clouds
        if (m_cellHumidness[index] >= k_cloudSpawnWetLevel && m_rng.RandomFloat() < m_cloudSpawnChance)
            m_cellStates[index] = CellState.kHasCloud;
        else
            m_cellStates[index] = CellState.kNoCloud;
    }

    // spawn cloud at given position
    private int SpawnNewCloudAt(int index)
    {
        int cloudIndex = m_clouds.Count;

        Vector3 pos = m_grid.IndexToWorldPositionCentered(index);
        pos.y = m_cloudLayerY;

        GameObject cloud = Instantiate(m_cloudPrefabs[m_rng.RandomIntRange(0, m_cloudPrefabs.Length)], pos, Quaternion.Euler(new Vector3(90f, 0, 0)));
        cloud.transform.parent = transform;

        m_clouds.Add(cloud);

        return cloudIndex;
    }
}
