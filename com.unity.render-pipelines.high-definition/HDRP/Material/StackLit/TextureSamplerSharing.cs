using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    [GenerateHLSL]
    public enum SharedSamplerID
    {
        First = 10,
        Zero = 10,
        One = 11,
        Two = 12,
        Three = 13,
        Four = 14,
        Last = 14,
    };

    [GenerateHLSL]
    public enum ExternalExistingSampler
    {
        LinearClamp = 1,
        LinearRepeat = 2,
        // to add others, edit EncodeSamplerState(ExternalExistingSampler extSamplerUsed), and edit shader code
        // to add them to the switch
    }
    public class TextureSamplerSharing
    {

        private const string k_SharedPropertyBaseName = "_SharedSamplerMap"; // texture property for the shared samplers will be _SharedSampler0...n
        // absolute maximum number of shared sampler slots available for use regardless of material declarations:
        private const int k_SharedSamplerMaxNum = (SharedSamplerID.Last - SharedSamplerID.First + 1);

        private Material m_Material;
        private int m_MaterialSharedSamplerNum; // number of shared sampler slots found declared in material properties
        private int m_UniqueClientStateCount; // for when adding clients that want unique sampler assignments: we use this to generate dummy state

        private Action<SamplerClient, int, bool> m_ClientAssignedToSharedSlot; // callback to give shared slot number assigned to the client texture

        public class SamplerClient
        {
            public string BasePropertyName;
            public Texture ATexture;
        }

        private Dictionary<ulong, List<SamplerClient>> m_UniqueSamplerStates;
        private Dictionary<ulong, List<SamplerClient>> m_UniqueExternalSamplerStates; // for samplers we know exist outside those known to this TextureSamplerSharing object

        private ulong EncodeSamplerState(Texture texture, bool makeUnique = false)
        {
            byte filterMode = (byte)texture.filterMode; // 6 bits is enough, 36 in DX11, but here 3 values, 2 bits is enough
            byte addressU = (byte)texture.wrapModeU; // 5 values, 3 bits is enough in fact!, even 2 in unity since border mode can't be set.
            byte addressV = (byte)texture.wrapModeV;
            byte addressW = (byte)texture.wrapModeW;
            float mipLODBias = texture.mipMapBias;
            byte anisoLevel = (byte)texture.anisoLevel;
            return EncodeSamplerState(filterMode, addressU, addressV, addressW, mipLODBias, anisoLevel, makeUnique);
        }

        private ulong EncodeSamplerState(ExternalExistingSampler extSamplerUsed)
        {
            switch(extSamplerUsed)
            {
                case ExternalExistingSampler.LinearClamp:
                    return EncodeSamplerState(
                        (byte)FilterMode.Bilinear,
                        (byte)TextureWrapMode.Clamp,
                        (byte)TextureWrapMode.Clamp,
                        (byte)TextureWrapMode.Clamp, 
                        0.0f, 1, makeUnique:false);
                case ExternalExistingSampler.LinearRepeat:
                    return EncodeSamplerState(
                        (byte)FilterMode.Bilinear,
                        (byte)TextureWrapMode.Repeat,
                        (byte)TextureWrapMode.Repeat,
                        (byte)TextureWrapMode.Repeat,
                        0.0f, 1, makeUnique: false);
            }
            return EncodeSamplerState(
                (byte)FilterMode.Bilinear,
                (byte)TextureWrapMode.Clamp,
                (byte)TextureWrapMode.Clamp,
                (byte)TextureWrapMode.Clamp,
                0.0f, 1, makeUnique: false);
        }

        private ulong EncodeSamplerState(byte filterMode, byte addressU, byte addressV, byte addressW, float mipLODBias, byte anisoLevel, bool makeUnique)
        {
            // The following takes 15 bits:
            ulong ls15bits = (ulong)( ((filterMode & (64 - 1)) << (3 + 3 + 3)) | ((addressW & (8 - 1)) << (3 + 3)) | ((addressV & (8 - 1)) << (3)) | ((addressU & (8 - 1)) << 0) );
            ulong stateLowerClearMask = (~(ulong)((1 << 29) - 1));
            // double has 53 bits mantissa and 11 bits exponent, anisoLevel is 4 bits, and we're roughly storing it in the exponent
            // part of the double by scaling mipLODBias of which only 24 bits of mantissa matter and a very low exponent already there.
            // mipLODBias has 24 bits mantissa. 53-24 = 29 bits, which are left ie the low binary part left for encoding state is 29bits
            // of the 64bit double which we use to store the rest of our state:
            ulong state = (ulong)(BitConverter.DoubleToInt64Bits((double)(mipLODBias + 1.0f) * anisoLevel)) & stateLowerClearMask;
            state = state | ls15bits;
            // Finally, we also want to add-in some bits in case a client wants its own slot to escape sharing
            // We do that with the (29-15) = 14 bits left: we only need 3-5 bits at most due to total sampler limit
            // anyways
            if (makeUnique)
            {
                m_UniqueClientStateCount++;
                ulong uniqueState = ((uint)(m_UniqueClientStateCount) << 15);
                state = state | uniqueState;
            }
            return state;
        }

        private static int GetMaterialSharedSamplerNum(Material material)
        {
            int sharedSamplerNum = 0;
            //Texture texture = material.GetTexture(k_SharedPropertyBaseName + sharedSamplerNum.ToString());
            bool hasSampler = material.HasProperty(k_SharedPropertyBaseName + sharedSamplerNum.ToString());

            while (hasSampler && sharedSamplerNum < k_SharedSamplerMaxNum)
            {
                sharedSamplerNum++;
                //texture = material.GetTexture(k_SharedPropertyBaseName + sharedSamplerNum.ToString());
                hasSampler = material.HasProperty(k_SharedPropertyBaseName + sharedSamplerNum.ToString());
            }
            return sharedSamplerNum;
        }

        public void AddExternalExistingSamplerStates()
        {
            m_UniqueExternalSamplerStates.Clear();
            foreach(ExternalExistingSampler extSamplerState in Enum.GetValues(typeof(ExternalExistingSampler)))
            {
                ulong samplerState = EncodeSamplerState(extSamplerState);
                SamplerClient client = new SamplerClient()
                {
                    // we will identify our dummy "clients" in the dictionary with empty property name:
                    // texture can still be null in case we want to force sampling of a default built-in
                    // symbol for the texture (eg "white", "black").
                    BasePropertyName = "",
                    ATexture = null,
                };
                List<SamplerClient> clients = new List<SamplerClient>()
                {
                    client,
                };
                m_UniqueExternalSamplerStates.Add(samplerState, clients);
            }
        }

        public void Reset(Material material, Action<SamplerClient, int, bool> assignmentCallback)
        {
            if (material != null)
            {
                m_Material = material;
            }
            m_MaterialSharedSamplerNum = GetMaterialSharedSamplerNum(material);
            m_UniqueClientStateCount = 0;
            m_ClientAssignedToSharedSlot = assignmentCallback;
            m_UniqueSamplerStates.Clear();
            m_UniqueExternalSamplerStates.Clear();
        }

        public TextureSamplerSharing(Material material, Action<SamplerClient, int, bool> assignmentCallback = null)
        {
            m_UniqueSamplerStates = new Dictionary<ulong, List<SamplerClient>>();
            m_UniqueExternalSamplerStates = new Dictionary<ulong, List<SamplerClient>>();
            Reset(material, assignmentCallback);
        }
        public void AddClientForExistingSampler(string basePropertyName, ExternalExistingSampler extSamplerUsed)
        {
            List<SamplerClient> clients;
            ulong samplerState = EncodeSamplerState(extSamplerUsed);

            if (m_UniqueExternalSamplerStates.TryGetValue(samplerState, out clients))
            {
                SamplerClient client = new SamplerClient()
                {
                    BasePropertyName = basePropertyName,
                    ATexture = null,
                };
                clients.Add(client);
            }
            else
            {
                // Error: call AddExistingSamplerStates() first, there should already
                // be an entry in the dictionary for that state.
                Assert.IsTrue(false, "Call to AddClientForExistingSampler without existing / external samplers added.");
            }
        } //AddClient using an existing external sampler (eg named-based parsed by the engine)

        public void AddClient(string basePropertyName, Texture texture, bool makeUnique = false, bool tryExistingExternalSamplers = true)
        {
            List<SamplerClient> clients;
            ulong samplerState = EncodeSamplerState(texture, makeUnique);

            SamplerClient client = new SamplerClient()
            {
                BasePropertyName = basePropertyName,
                ATexture = texture,
            };

            if (tryExistingExternalSamplers && (m_UniqueExternalSamplerStates.TryGetValue(samplerState, out clients)) )
            {
                clients.Add(client);
            }
            else if (m_UniqueSamplerStates.TryGetValue(samplerState, out clients))
            {
                clients.Add(client);
            }
            else
            {
                clients = new List<SamplerClient>()
                {
                    client,
                };
                m_UniqueSamplerStates.Add(samplerState, clients);
            }
        }//AddClient

        public int GetRequiredSamplerStatesNum()
        {
            return m_UniqueSamplerStates.Count;
        }

        // Returns the number of shared samplers that will be used.
        public int DoClientAssignment()
        {
            int totalSamplerUsed = 0;

            // We will sort the unique states used by order of number of clients using them
            // so that we minimize the number of clients that will use their own unshared samplers
            // in the case where m_UniqueSamplerStates.Count > m_MaterialSharedSamplerNum
            var stateList = m_UniqueSamplerStates.ToList();
            // This would give ascending sort:
            //stateList.Sort( (keyValA, keyValB) => (keyValA.Value.Count - keyValB.Value.Count));
            // We want the opposite:
            stateList.Sort((keyValA, keyValB) => (keyValB.Value.Count - keyValA.Value.Count));

            int i;
            for (i = 0; i < Math.Min(stateList.Count, m_MaterialSharedSamplerNum); ++i)
            {
                // Make sure at least one texture client will fill the shared texture property
                // slot so it is binded and with it, the sampler state we're going to share.
                // We will use the first client for that:
                m_Material.SetTexture(k_SharedPropertyBaseName + i, stateList[i].Value[0].ATexture);

                // Callback to let the client of the TextureSamplerSharing know the slot assignment
                // for each client of this state "i" (which will be setup on shared texture slot "i",
                // per the SetTexture we just did above):
                foreach(var samplerClient in stateList[i].Value)
                {
                    m_ClientAssignedToSharedSlot(samplerClient, i, false);
                }
            }
            totalSamplerUsed += i;

            // Process callback for external samplers:
            stateList = m_UniqueExternalSamplerStates.ToList();
            for (i = 0; i < stateList.Count; ++i)
            {
                // Here, the UnityEngine Texture could well be null for the client, as this can be
                // used to provide sampling and reduce sampler sharing for unassigned textured properties
                // that nonetheless have a default property symbolic value ("white", "black", etc) that
                // the engine manage internally with a dummy texture and a sampler.
                // Here, we don't set the texture on a dummy texture object slot / property since that's
                // the point of the external samplers: they already exist (through some way or are just
                // engine parsed by-name samplers, this is up to the shader code to decide).

                // We've setup the state dictionary for these samplers by first inserting dummy "clients"
                // with their corresponding state so that further queries/additions would match on those
                // states. These sentinels are at the beginning of each client list for each state and
                // we skip them.

                // Callback to let the client of the TextureSamplerSharing know the slot assignment
                // for each client of this state "i" (which is external, and specified as such to the client
                // callback):
                int j = 0;
                foreach (var samplerClient in stateList[i].Value)
                {
                    if (samplerClient.BasePropertyName == "")
                    {
                        Assert.IsTrue(j == 0, "Sentinel found but not at the beginning!");
                        j++;
                        continue;
                    }
                    m_ClientAssignedToSharedSlot(samplerClient, i, true);
                    j++;
                }
            }

            totalSamplerUsed += i;
            return totalSamplerUsed;
        }
    }

}
