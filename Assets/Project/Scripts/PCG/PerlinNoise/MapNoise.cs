using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class MapNoise
{
    public static float[,] Generate2DNoiseMap(int mapWidth, int mapHeight, ulong seed, float scale, int numOctaves, float lacunarity, float persistence, Vector2 offset)
    {
        float[,] noiseMap = new float[mapWidth, mapHeight];

        // adding random offset on noise map to easily get different map layout by setting different seed value 
        XOrShiftRNG rng = new XOrShiftRNG(seed);
        Vector2[] octaveOffsets = new Vector2[numOctaves];
        for (int i = 0; i < numOctaves; ++i)
        {
            float offsetX = rng.RandomFloatRange(0f, 1000000f) + offset.x;
            float offsetY = rng.RandomFloatRange(0f, 1000000f) - offset.y;
            octaveOffsets[i] = new Vector2(offsetX, offsetY);
        }
        
        if (scale <= 0)
            scale = 0.0001f;

        for (int y = 0; y < mapHeight; ++y)
        {
            for (int x = 0; x < mapWidth; ++x)
            {
                float perlinValue = PerlinNoise.Perlin2D(x, y, numOctaves, scale, lacunarity, persistence, octaveOffsets);
                noiseMap[x, y] = perlinValue;
            }
        }

        return noiseMap;
    }

    public static float[,] Generate2DRandomNoise(int mapWidth, int mapHeight, ulong seed)
    {
        float[,] noiseMap = new float[mapWidth, mapHeight];
        XOrShiftRNG rng = new XOrShiftRNG(seed);

        for (int y = 0; y < mapHeight; ++y)
        {
            for (int x = 0; x < mapWidth; ++x)
            {
                float noiseValue = rng.RandomFloat();
                noiseMap[x, y] = noiseValue;
            }
        }

        return noiseMap;
    }
}
