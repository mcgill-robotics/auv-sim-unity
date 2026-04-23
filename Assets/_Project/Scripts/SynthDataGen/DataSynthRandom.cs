using System.Linq;

// Utility class for generating random numbers with a seed that can be reset for reproducibility
public static class DataSynthRandom
{
    public static int[] GetShuffledIndices(int count, Unity.Mathematics.Random randomState)
    {
        // Shuffle a copy of the config indices, then assign in order
        // Fisher-Yates shuffle guarantees no two targets get the same config
        int[] indices = Enumerable.Range(0, count).ToArray();
        for (int i = indices.Length - 1; i > 0; i--)
        {
            int j = randomState.NextInt(0, i + 1);
            (indices[i], indices[j]) = (indices[j], indices[i]);
            // index j is swapped to position i, and so after shuffle we do not touch config at index i again (due to i-- and upper bound of NextInt), ensuring it is not assigned to another target 
        }
        return indices;
    }
}