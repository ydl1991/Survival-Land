using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System;
using System.Threading;
using UnityEngine;

public enum LandType
{
    kSnow = 0,
    kRock,
    kPlants,
    kWater,
}

// Terrain Type stores the land data reflected from noise height map
[System.Serializable]
public struct LandTypeData
{
    public string m_name;
    public float m_depth;
    public Color m_color;
    public ObjectSpawnCondition[] m_objectSpawnConditions;
}

// used to determine what object to spawn on map
public enum TerrainObjectType
{
    kRock = 0,
    kGrass,
    kTree
}

// Record the spawing condition of a specific object type
[System.Serializable]
public struct ObjectSpawnCondition
{
    public float m_humility;
    public TerrainObjectType m_type;
}

// Map data stores all the data needed to form a noise map, color map, a mesh, and object spawning data on the map
public class MapData
{
    public float[,] heightMap { get; }
    public Color[] colorMap { get; }
    public LandType[] landTypeMap { get; }

    public MapData(float[,] heightMap, Color[] colorMap, LandType[] landMap)
    {
        this.heightMap = heightMap;
        this.colorMap = colorMap;
        this.landTypeMap = landMap;
    }
}

public class MapGenerator : MonoBehaviour
{
    // This draw mode is for editor draw only, not related to play mode render
    public enum DrawMode
    {
        kNoise = 0,
        kColor,
        kMesh,
    }

    struct MapThreadInfo<T>
    {
        public Action<T> callback { get; }
        public T parameter { get; }

        public MapThreadInfo(Action<T> callback, T parameter)
        {
            this.callback = callback;
            this.parameter = parameter;
        }
    }

    public static MapGenerator s_instance;

    // map data
    public const int m_kMapChunkSize = 150;
    [Range(0, 6)] public int m_editorPreviewLevelOfDetail;
    public float m_mapHeightMultiplier;
    public AnimationCurve m_meshHeightCurve;
    
    // Noise data
    public float m_noiseScale;
    public int m_numOctaves;
    [Range(0, 1)] public float m_persistance;
    public float m_lacunarity;
    public ulong m_noiseMapSeed;
    public Vector2 m_offset;
    
    // Map display data
    public bool m_autoGenerate;
    public DrawMode m_drawMode;
    public LandTypeData[] m_regions;

    // Threading
    ConcurrentQueue<MapThreadInfo<MapData>> m_mapDataThreadQueue;
    ConcurrentQueue<MapThreadInfo<MeshData>> m_meshDataThreadQueue;

    // rng
    XOrShiftRNG m_rng;

    void Awake()
    {
        s_instance = this;
        m_mapDataThreadQueue = new ConcurrentQueue<MapThreadInfo<MapData>>();
        m_meshDataThreadQueue = new ConcurrentQueue<MapThreadInfo<MeshData>>();
        m_rng = new XOrShiftRNG();
        OnValidate();
    }

    // ---------------------------------------------- //
    //                  Threading
    // ---------------------------------------------- //
    // start a thread to generate map data, get's called in CellularAutomataTerrain.CS while a new Terrain is spawned and need
    // map data to spawn mesh. 
    // Center is a offet data passed in for map generation, callback is the reaction function of TerrainRegion class
    // that uses the generated map data and call generate mesh
    public void RequestMapData(Vector2 center, Action<MapData> callback)
    {
        ThreadStart threadStart = delegate { MapDataThread(center, callback); };
        new Thread(threadStart).Start();
    }
    void MapDataThread(Vector2 center, Action<MapData> callback)
    {
        MapData mapData = GenerateMapData(center);
        m_mapDataThreadQueue.Enqueue(new MapThreadInfo<MapData>(callback, mapData));
    }

    // Gets called when generating mesh with a specific level of details, level of details can be edited in inspector
    public void RequestMeshData(MapData data, int levelOfDetail, Action<MeshData> callback)
    {
        ThreadStart threadStart = delegate { MeshDataThread(data, levelOfDetail, callback); };
        new Thread(threadStart).Start();
    }
    void MeshDataThread(MapData data, int levelOfDetail, Action<MeshData> callback)
    {
        MeshData meshData = MapMeshGenerator.GenerateTerrainMesh(data.heightMap, m_mapHeightMultiplier, m_meshHeightCurve, levelOfDetail);
        m_meshDataThreadQueue.Enqueue(new MapThreadInfo<MeshData>(callback, meshData));
    }


    // After data has been generated in thread and returned, the callback gets called in update function
    void Update()
    {
        while (m_mapDataThreadQueue.Count > 0)
        {
            m_mapDataThreadQueue.TryDequeue(out MapThreadInfo<MapData> threadInfo);
            threadInfo.callback(threadInfo.parameter);
        }

        while (m_meshDataThreadQueue.Count > 0)
        {
            m_meshDataThreadQueue.TryDequeue(out MapThreadInfo<MeshData> threadInfo); 
            threadInfo.callback(threadInfo.parameter);
        }
    }

    // Function to generate map data without considering object generation, this is the one gets called in Endless mode and editor draw
    public MapData GenerateMapData(Vector2 center)
    {
        float[,] noiseMap = MapNoise.Generate2DNoiseMap(
                m_kMapChunkSize, 
                m_kMapChunkSize, 
                m_noiseMapSeed, 
                m_noiseScale, 
                m_numOctaves, 
                m_lacunarity, 
                m_persistance, 
                center + m_offset
            );

        // Generate land type that determines the mesh color
        LandType[] landMap = new LandType[m_kMapChunkSize * m_kMapChunkSize];
        // Generating land color map that shows on the mesh
        Color[] colorMap = new Color[m_kMapChunkSize * m_kMapChunkSize];

        // Seed cells that randomly placed on map, seed cell will only be grass or water
        int numSeeds = 0;

        // Go through each of the cell, set default land type to rock, and color to rock color
        for (int y = 0; y < m_kMapChunkSize; ++y)
        {
            for (int x = 0; x < m_kMapChunkSize; ++x)
            {
                landMap[y * m_kMapChunkSize + x] = LandType.kRock;
                colorMap[y * m_kMapChunkSize + x] = m_regions[(int)LandType.kRock].m_color;
  
                // according to depth map, if the depth noise at current position is in our seeding range, there is a chance
                // to place a seed which changes the land type and color to seed land type and color.
                // place seed type randomly gives a more variation of the final map result.
                float currentDepth = noiseMap[x, y];
                if (currentDepth > m_regions[(int)LandType.kRock].m_depth && currentDepth <= m_regions[(int)LandType.kWater].m_depth)
                {
                    // maximum 10 seed at start
                    if (numSeeds < 10 && m_rng.RandomFloat() < 0.0005f)
                    {    
                        landMap[y * m_kMapChunkSize + x] = LandType.kWater;
                        colorMap[y * m_kMapChunkSize + x] = m_regions[(int)LandType.kWater].m_color;
                        ++numSeeds;
                    }
                }
            }
        }
        
        return new MapData(noiseMap, colorMap, landMap); 
    }

    void OnValidate()
    {
        if (m_lacunarity < 1f)
            m_lacunarity = 1f;
        
        if (m_numOctaves < 1)
            m_numOctaves = 1;
    }
}

