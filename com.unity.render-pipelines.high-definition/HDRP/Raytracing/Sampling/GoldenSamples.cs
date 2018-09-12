using UnityEngine;

public class GoldenSamples
{
    static public void FillBuffer(ComputeBuffer buffer)
    {
        float[] data = new float[buffer.count];

        qmc.Rng rnd = new qmc.Rng();
        qmc.InitRng(98764546, ref rnd);
        for (int i = 0; i < buffer.count; i++)
        {
            data[i] = qmc.RandFloat(ref rnd);
        }

        buffer.SetData(data);
        return;
    }
}
