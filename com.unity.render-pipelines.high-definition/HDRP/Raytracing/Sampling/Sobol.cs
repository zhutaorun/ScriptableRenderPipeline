using UnityEngine;

public class Sobol
{
    static public void FillBuffer(ComputeBuffer buffer)
    {
        if (buffer.count != qmc.SobolMatrices.Length)
            return; // Fixme add exception

        buffer.SetData(qmc.SobolMatrices);        
    }
}
