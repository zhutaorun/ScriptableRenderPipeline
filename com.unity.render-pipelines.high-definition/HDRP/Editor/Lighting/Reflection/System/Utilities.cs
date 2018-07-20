using UnityEngine;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    unsafe static class Utilities
    {
        public static float CalculateProgress(int numJobsTodo, int totalNumJobs)
        {
            return (totalNumJobs != 0) ? 1.0f - ((float)numJobsTodo / totalNumJobs) : 1.0f;
        }

        public static void CombineHashes(int count, Hash128* hashes, Hash128* outHash)
        {
            for (int i = 0; i < count; ++i)
                HashUtilities.AppendHash(ref hashes[i], ref *outHash);
        }

        public static void CopyToIndirect(int count, int* indices, byte* src, byte* dest, int stride)
        {
            for (int i = 0; i < count; ++i)
            {
                var srcAddress = src + stride * indices[i];
                var destAddress = dest + stride * i;
                Unity.Collections.LowLevel.Unsafe.UnsafeUtility.MemCpy(destAddress, srcAddress, stride);
            }
        }

        public static int CompareHashes(
            int oldHashCount, Hash128* oldHashes,
            int newHashCount, Hash128* newHashes,
            // assume that the capacity of indices is >= max(oldHashCount, newHashCount)
            int* addIndicies, int* removeIndicies,
            out int addCount, out int remCount
        )
        {
            addCount = 0;
            remCount = 0;
            // Check combined hashes
            if (oldHashCount == newHashCount)
            {
                var oldHash = new Hash128();
                var newHash = new Hash128();
                CombineHashes(oldHashCount, oldHashes, &oldHash);
                CombineHashes(newHashCount, newHashes, &newHash);
                if (oldHash == newHash)
                    return 0;
            }

            var numOperations = 0;

            var oldI = 0;
            var newI = 0;

            while (oldI < oldHashCount || newI < newHashCount)
            {
                // At the end of old array.
                if (oldI == oldHashCount)
                {
                    // No more hashes in old array. Add remaining entries from new array.
                    for (; newI < newHashCount; ++newI)
                    {
                        addIndicies[addCount++] = newI;
                        ++numOperations;
                    }
                    continue;
                }

                // At end of new array.
                if (newI == newHashCount)
                {
                    // No more hashes in old array. Remove remaining entries from old array.
                    for (; oldI < oldHashCount; ++oldI)
                    {
                        removeIndicies[remCount++] = oldI;
                        ++numOperations;
                    }
                    continue;
                }

                // Both arrays have data.
                if (newHashes[newI] == oldHashes[oldI])
                {
                    // Matching hash, skip.
                    ++newI;
                    ++oldI;
                    continue;
                }

                // Both arrays have data, but hashes do not match.
                if (newHashes[newI] < oldHashes[oldI])
                {
                    // oldIter is the greater hash. Push "add" jobs from the new array until reaching the oldIter hash.
                    while (newI < newHashCount && newHashes[newI] < oldHashes[oldI])
                    {
                        addIndicies[addCount++] = newI;
                        ++newI;
                        ++numOperations;
                    }
                }
                else
                {
                    // newIter is the greater hash. Push "remove" jobs from the old array until reaching the newIter hash.
                    while (oldI < oldHashCount && oldHashes[oldI] < newHashes[newI])
                    {
                        removeIndicies[remCount++] = oldI;
                        ++numOperations;
                    }
                }
            }

            return numOperations;
        }
    }
}
