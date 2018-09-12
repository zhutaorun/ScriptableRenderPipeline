using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class qmc
{
    public struct Rng
    {
        public uint val;
    }

	// Hash function
	static uint WangHash(uint seed)
    {
        seed = (seed ^ 61) ^ (seed >> 16);
        seed *= 9;
        seed = seed ^ (seed >> 4);
        seed *= 0x27d4eb2d;
        seed = seed ^ (seed >> 15);
        return seed;
    }

    // Return random unsigned
    static public uint RandUint(ref Rng rng)
    {
        rng.val = WangHash(1664525U * rng.val + 1013904223U);
        return rng.val;
    }

    // Return random float
    static public float RandFloat(ref Rng rng)
    {
        return ((float)RandUint(ref rng)) / 0xffffffffU;
    }

    // Initialize RNG
    static public void InitRng(uint seed, ref Rng rng)
    {
        rng.val = WangHash(seed);
        for (int i = 0; i < 100; ++i)
            RandFloat(ref rng);
    }
}
