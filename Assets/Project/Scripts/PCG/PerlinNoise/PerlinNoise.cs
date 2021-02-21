using UnityEngine;
using System;

// ------------------------------------------------ //
//    Trying perlin noise implementation using a 
//    permutation table and hash
// ------------------------------------------------ //
public class PerlinNoise
{
    // Permutation table as defined by Ken Perlin. A randomly arranged array of all numbers from 0-255 inclusive.
	private static readonly int[] s_permutation = { 
        151, 160, 137, 91, 90, 15, 131, 13, 201, 95, 96, 53, 194, 233, 7, 225, 140,
        36, 103, 30, 69, 142, 8, 99, 37, 240, 21, 10, 23, 190, 6, 148, 247, 120, 234,
        75, 0, 26, 197, 62, 94, 252, 219, 203, 117, 35, 11, 32, 57, 177, 33, 88, 237,
        149, 56, 87, 174, 20, 125, 136, 171, 168, 68, 175, 74, 165, 71, 134, 139, 48,
        27, 166, 77, 146, 158, 231, 83, 111, 229, 122, 60, 211, 133, 230, 220, 105, 
        92, 41, 55, 46, 245, 40, 244, 102, 143, 54, 65, 25, 63, 161, 1, 216, 80, 73,
        209, 76, 132, 187, 208, 89, 18, 169, 200, 196, 135, 130, 116, 188, 159, 86, 
        164, 100, 109, 198, 173, 186, 3, 64, 52, 217, 226, 250, 124, 123, 5, 202, 38,
        147, 118, 126, 255, 82, 85, 212, 207, 206, 59, 227, 47, 16, 58, 17, 182, 189,
        28, 42, 223, 183, 170, 213, 119, 248, 152, 2, 44, 154, 163, 70, 221, 153, 101,
        155, 167, 43, 172, 9, 129, 22, 39, 253, 19, 98, 108, 110, 79, 113, 224, 232,
        178, 185, 112, 104, 218, 246, 97, 228, 251, 34, 242, 193, 238, 210, 144, 12,
        191, 179, 162, 241, 81, 51, 145, 235, 249, 14, 239, 107, 49, 192, 214, 31, 181,
        199, 106, 157, 184, 84, 204, 176, 115, 121, 50, 45, 127, 4, 150, 254, 138, 236,
        205, 93, 222, 114, 67, 29, 24, 72, 243, 141, 128, 195, 78, 66, 215, 61, 156, 180
	};

    // Lookup table
    private static readonly int[] s_lookUpTable;

    // Static Constructor
    static PerlinNoise() 
    {
		s_lookUpTable = new int[512];
		for (int i = 0; i < 512; ++i) 
        {
			s_lookUpTable[i] = s_permutation[i % 256];
		}
	}

    // -------------------------------------- //
    //          Noise Functions
    // -------------------------------------- //
    public static float Get1DNoise(float x)
    {
        var xi = ((Mathf.FloorToInt(x) % 256) + 256) % 256;
        float xf = x - Mathf.Floor(x);
        if (x < 0)
            xf += 1;
        var u = SmoothStep(xf);
        float noise = Lerp( Grad1D(s_lookUpTable[xi], xf), Grad1D(s_lookUpTable[xi + 1], xf - 1), u );

        // bound it to 0 - 1 (theoretical min/max before is -1 - 1)
        return (noise + 1f) / 2f;
    }

    public static float Get2DNoise(float x, float y)
    {
        var xi = ((Mathf.FloorToInt(x) % 256) + 256) % 256;
        var yi = ((Mathf.FloorToInt(y) % 256) + 256) % 256;
        float xf = x - Mathf.Floor(x);
        float yf = y - Mathf.Floor(y);
        if (x < 0)
            xf += 1f;
        if (y < 0)
            yf += 1f;

        var u = SmoothStep(xf);
        var v = SmoothStep(yf);
        var a = (s_lookUpTable[xi] + yi)   & 0xff;
        var b = (s_lookUpTable[xi + 1] + yi) & 0xff;

        float noise = Lerp(
                Lerp( Grad2D(s_lookUpTable[a], xf, yf), Grad2D(s_lookUpTable[b], xf - 1, yf), u ),
                Lerp( Grad2D(s_lookUpTable[a + 1], xf, yf - 1), Grad2D(s_lookUpTable[b + 1], xf - 1, yf - 1), u ), 
                v
            );

        // bound it to 0 - 1 (theoretical min/max before is -1 - 1)
        return (noise + 1f) / 2f;
    }

    public static float Get2DNoise(Vector2 coord)
    {
        return Get2DNoise(coord.x, coord.y);
    }

    // We are not using 3D noise but I combined couple of implementations I researched here
    public static float Get3DNoise(float x, float y, float z)
    {
        var xi = ((Mathf.FloorToInt(x) % 256) + 256) % 256;
        var yi = ((Mathf.FloorToInt(y) % 256) + 256) % 256;
        var zi = ((Mathf.FloorToInt(z) % 256) + 256) % 256;
        
        float xf = x - Mathf.Floor(x);
        float yf = y - Mathf.Floor(y);
        float zf = z - Mathf.Floor(z);
        if (x < 0)
            xf += 1;
        if (y < 0)
            yf += 1;
        if (z < 0)
            zf += 1;
        
        var u = SmoothStep(xf);
        var v = SmoothStep(yf);
        var w = SmoothStep(zf);
        var a  = (s_lookUpTable[xi] + yi)     & 0xff;
        var b  = (s_lookUpTable[xi + 1] + yi) & 0xff;
        var aa = (s_lookUpTable[a] + zi)      & 0xff;
        var ba = (s_lookUpTable[b] + zi)      & 0xff;
        var ab = (s_lookUpTable[a + 1] + zi)  & 0xff;
        var bb = (s_lookUpTable[b + 1] + zi)  & 0xff;

        var x1 = Lerp(Grad3D(s_lookUpTable[aa], xf, yf, zf), Grad3D(s_lookUpTable[ba], xf - 1, yf, zf), u);
        var x2 = Lerp(Grad3D(s_lookUpTable[ab], xf, yf - 1, zf), Grad3D(s_lookUpTable[bb], xf - 1, yf - 1, zf), u);
        var y1 = Lerp(x1, x2, v);

        x1 = Lerp(Grad3D(s_lookUpTable[aa + 1], xf, yf, zf - 1), Grad3D(s_lookUpTable[ba + 1], xf - 1, yf, zf - 1), u);
        x2 = Lerp(Grad3D(s_lookUpTable[ab + 1], xf, yf - 1, zf - 1), Grad3D(s_lookUpTable[bb + 1], xf - 1, yf - 1, zf - 1), u);
        var y2 = Lerp(x1, x2, v);

        // bound it to 0 - 1 (theoretical min/max before is -1 - 1)
        return (Lerp(y1, y2, w) + 1f) / 2f;
    }

    public static float Get3DNoise(Vector3 coord)
    {
        return Get3DNoise(coord.x, coord.y, coord.z);
    }

    // -------------------------------------- //
    //          Perlin Noise Functions
    // -------------------------------------- //
    // incorporating seeded random offsets into perlin noise, make the noise to show more variation
    // if want to get same result, we can insert same seed, otherwise if seed is null it will use random seed
    public static float Perlin1D(float x, int numOctaves, float scale, float lacunarity, float persistance, float[] octaveOffsets)
    {
        bool applysOffset = (octaveOffsets != null);
        var noise = 0.0f;
        var frequency = 1f;
        var currentAmplitude = 1f;
        for (var i = 0; i < numOctaves; ++i) 
        {
            noise += currentAmplitude * Get1DNoise(x);
            float nx = (x + (applysOffset ? octaveOffsets[i] : 0)) / scale * frequency;
            currentAmplitude *= persistance;
            frequency *= lacunarity;
        }

        return noise / 2f;
    }

    public static float Perlin2D(Vector2 coord, int numOctaves, float scale, float lacunarity, float persistance, Vector2[] octaveOffsets)
    {
        return Perlin2D(coord.x, coord.y, numOctaves, scale, lacunarity, persistance, octaveOffsets);
    }

    public static float Perlin2D(float x, float y, int numOctaves, float scale, float lacunarity, float persistance, Vector2[] octaveOffsets)
    {
        bool applysOffset = (octaveOffsets != null);
        var noise = 0.0f;
        var frequency = 1f;
        var currentAmplitude = 1f;
        for (var i = 0; i < numOctaves; ++i) 
        {
            float nx = (x + (applysOffset ? octaveOffsets[i].x : 0)) / scale * frequency;
            float ny = (y + (applysOffset ? octaveOffsets[i].y : 0)) / scale * frequency;
            noise += currentAmplitude * Get2DNoise(nx, ny);
            currentAmplitude *= persistance;
            frequency *= lacunarity;
        }

        return noise / 2f;
    }

    public static float Perlin3D(Vector3 coord, int numOctaves, float scale, float lacunarity, float persistance, Vector3[] octaveOffsets)
    {
        return Perlin3D(coord.x, coord.y, coord.z, numOctaves, scale, lacunarity, persistance, octaveOffsets);
    }

    public static float Perlin3D(float x, float y, float z, int numOctaves, float scale, float lacunarity, float persistance, Vector3[] octaveOffsets)
    {
        bool applysOffset = (octaveOffsets != null);
        var noise = 0.0f;
        var frequency = 1f;
        var currentAmplitude = 1f;
        for (var i = 0; i < numOctaves; ++i) 
        {
            noise += currentAmplitude * Get3DNoise(x, y, z);
            x = (x + (applysOffset ? octaveOffsets[i].x : 0)) / scale * frequency;
            y = (y + (applysOffset ? octaveOffsets[i].y : 0)) / scale * frequency;
            z = (z + (applysOffset ? octaveOffsets[i].z : 0)) / scale * frequency;
            currentAmplitude *= persistance;
            frequency *= lacunarity;
        }

        return noise / 2f;
    }    

    // -------------------------------------- //
    //          Private Helpers
    // -------------------------------------- //
    // Smooth transition between gradients, originally called Fade step by Ken Perlin 
    static float SmoothStep(float t)
    {
        return t * t * t * (t * (t * 6 - 15) + 10);
    }

    static float Lerp(float a, float b, float t)
    {
        return a + t * (b - a);
    }

    // Gradient function: calculate the dot product of a randomly selected gradient vector 
    // and the 8 location vectors. Directly using Ken Perlin's Implementation
    static float Grad1D(int hash, float x)
    {
        return (hash & 1) == 0 ? x : -x;
    }

    static float Grad2D(int hash, float x, float y)
    {
        return ((hash & 1) == 0 ? x : -x) + ((hash & 2) == 0 ? y : -y);
    }

    static float Grad3D(int hash, float x, float y, float z)
    {
        var h = hash & 15;
        var u = h < 8 ? x : y;
        var v = h < 4 ? y : (h == 12 || h == 14 ? x : z);
        return ((h & 1) == 0 ? u : -u) + ((h & 2) == 0 ? v : -v);
    }
}
