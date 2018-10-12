using System;
using System.IO;
using System.Text;
using System.Security.Cryptography;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.HDPipeline;
using System.Text.RegularExpressions;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    // See classes TextureSamplerSharing and TextureSamplerSharingShaderGenerator
    // TODO: The only thing we're not handling in the sampler state is the LOD Min/Max clamps.
    public class TextureSamplerSharing
    {
        public const int k_DefaultSharedSamplerUsedNum = 5; // default valu SHARED_SAMPLER_USED_NUM declared in the .shader file of stacklit.
        public const string k_SharedSamplerUsedNumDefine = "_SharedSamplerUsedNumDefine";

        // Make sure k_SharedPropertyBaseName is in synch with TextureSamplerSharing.hlsl's defined SHAREDSAMPLER_BASENAME
        private const string k_SharedPropertyBaseName = "_SharedSamplerMap"; // texture property for the shared samplers will be _SharedSampler0...n
        // absolute maximum number of shared sampler slots available for use regardless of material declarations:
        private const int k_SharedSamplersMaxNum = (SharedSamplerID.Last - SharedSamplerID.First + 1);
        public static int SharedSamplersMaxNum { get { return k_SharedSamplersMaxNum; } }

        private Material m_Material;
        private int m_MaterialSharedSamplersMaxAllowed; // number of shared sampler slots found declared in material properties capped by SharedSamplersMaxNum
        private int m_UniqueClientStateCount; // for when adding clients that want unique sampler assignments: we use this to generate dummy state

        // Action callback to give a shared slot number assigned to the client texture
        //
        // Note: assigned sampler numbers all start at 0 and are relative to numspace for shared or external samplers
        // eg 0 means the first shared sampler slots, etc.
        // The actual flattened UseMap values numspace where we have carved out a range for the shared samplers numbers and external
        // samplers numbers are defined in the engine side UnityEngine.Experimental.Rendering.HDPipeline.TextureSamplerSharing,
        // represented by the two enums SharedSamplerID and ExternalExistingSampler.
        //
        // Params: (samplerClient, assigned sampler number,  external sampler? (vs shared slot), unique client on this sampler? )
        private Action<SamplerClient, int, bool, bool> m_ClientAssignedToSharedSlot;

        // Sampler usage stats:
        // external =  builtin (these are shared too, but not managed by TextureSamplerSharing, so called "external") 
        //           + maps using their own (if generating),
        // then we have shared slots used vs needed.
        private int m_ExternalSamplersUsedOnLastAssignement;
        public int ExternalSamplersUsedOnLastAssignement { get { return m_ExternalSamplersUsedOnLastAssignement; } private set { m_ExternalSamplersUsedOnLastAssignement = value; } }
        private int m_BuiltInSamplersUsedOnLastAssignement;
        public int BuiltInSamplersUsedOnLastAssignement { get { return m_BuiltInSamplersUsedOnLastAssignement; } private set { m_BuiltInSamplersUsedOnLastAssignement = value; } }
        public int OwnSamplersUsedOnLastAssignement { get { return m_ExternalSamplersUsedOnLastAssignement - m_BuiltInSamplersUsedOnLastAssignement; } }

        private int m_SharedSamplersUsedOnLastAssignement;
        public int SharedSamplersUsedOnLastAssignement { get { return m_SharedSamplersUsedOnLastAssignement; } private set { m_SharedSamplersUsedOnLastAssignement = value; } }
        private int m_SharedSamplersNeededOnLastAssignement;
        public int SharedSamplersNeededOnLastAssignement { get { return m_SharedSamplersNeededOnLastAssignement; } private set { m_SharedSamplersNeededOnLastAssignement = value; } }

        public class SamplerClient
        {
            public string BasePropertyName;
            public Texture ATexture;
        }

        public struct EngineSamplerState
        {
            public FilterMode FilterMode;
            public TextureWrapMode AddressU;
            public TextureWrapMode AddressV;
            public TextureWrapMode AddressW;
            public float MipLODBias;
            public int AnisoLevel;
        }

        public struct SerializedSamplerState
        {
            public float HSBits;
            public float LSBits;
        }

        private Dictionary<ulong, List<SamplerClient>> m_UniqueSharedSamplerStates;
        private Dictionary<ulong, List<SamplerClient>> m_UniqueExternalSamplerStates; // for samplers we know exist outside those known to this TextureSamplerSharing object

        private SerializedSamplerState SerializeSamplerState(ulong state)
        {
            byte[] highPart = BitConverter.GetBytes((uint)((state & 0xFFFFFFFF00000000) >> 32));
            byte[] lowPart = BitConverter.GetBytes((uint)(state & 0xFFFFFFFF));
            SerializedSamplerState encodedState = new SerializedSamplerState
            {
                HSBits = BitConverter.ToSingle(highPart, 0),
                LSBits = BitConverter.ToSingle(lowPart, 0),
            };
            return encodedState;
        }

        private EngineSamplerState DeserializeSamplerState(SerializedSamplerState serializedState)
        {
            uint lowPart = BitConverter.ToUInt32(BitConverter.GetBytes(serializedState.LSBits), 0);

            EngineSamplerState engineState;

            engineState.MipLODBias = serializedState.HSBits;
            engineState.AnisoLevel = (int)((lowPart & ((((uint)(1) << k_StateAnisoLevelNumBits) - 1) << k_StateAnisoLevelPos)) >> k_StateAnisoLevelPos);
            engineState.FilterMode = (FilterMode)((lowPart & ((((uint)(1) << k_StateFilterModeNumBits) - 1) << k_StateFilterModePos)) >> k_StateFilterModePos);
            engineState.AddressW = (TextureWrapMode)((lowPart & ((((uint)(1) << k_StateWrapModeNumBits) - 1) << k_StateWWrapModePos)) >> k_StateWWrapModePos);
            engineState.AddressV = (TextureWrapMode)((lowPart & ((((uint)(1) << k_StateWrapModeNumBits) - 1) << k_StateVWrapModePos)) >> k_StateVWrapModePos);
            engineState.AddressU = (TextureWrapMode)((lowPart & ((((uint)(1) << k_StateWrapModeNumBits) - 1) << k_StateUWrapModePos)) >> k_StateUWrapModePos);

            //uint unique = ( (lowPart & ((((uint)(1) << k_StateUniqueNumBits) - 1) << k_StateUniquePos) ) >> k_StateUniquePos);

            return engineState;
        }

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

        private ulong EncodeSamplerState(ExternalExistingSampler extSamplerUsed, bool makeUnique = false)
        {
            // For why there's "makeUnique" here, see AddClientForOwnUniqueSampler
            switch (extSamplerUsed)
            {
                case ExternalExistingSampler.LinearClamp:
                    return EncodeSamplerState(
                        (byte)FilterMode.Bilinear,
                        (byte)TextureWrapMode.Clamp,
                        (byte)TextureWrapMode.Clamp,
                        (byte)TextureWrapMode.Clamp,
                        0.0f, 1, makeUnique: makeUnique);
                case ExternalExistingSampler.LinearRepeat:
                    return EncodeSamplerState(
                        (byte)FilterMode.Bilinear,
                        (byte)TextureWrapMode.Repeat,
                        (byte)TextureWrapMode.Repeat,
                        (byte)TextureWrapMode.Repeat,
                        0.0f, 1, makeUnique: makeUnique);
            }
            return EncodeSamplerState(
                (byte)FilterMode.Bilinear,
                (byte)TextureWrapMode.Clamp,
                (byte)TextureWrapMode.Clamp,
                (byte)TextureWrapMode.Clamp,
                0.0f, 1, makeUnique: makeUnique);
        }

        // See EncodeSamplerState below:
        private const int k_StateAnisoLevelNumBits = 4;
        private const int k_StateAnisoLevelPos = 32 - k_StateAnisoLevelNumBits;
        private const int k_StateFilterModeNumBits = 6;
        private const int k_StateFilterModePos = k_StateAnisoLevelPos - k_StateFilterModeNumBits;
        private const int k_StateWrapModeNumBits = 3;
        private const int k_StateWWrapModePos = k_StateFilterModePos - k_StateWrapModeNumBits;
        private const int k_StateVWrapModePos = k_StateWWrapModePos - k_StateWrapModeNumBits;
        private const int k_StateUWrapModePos = k_StateVWrapModePos - k_StateWrapModeNumBits;

        private const int k_StateLSBitsUsed = 32 - k_StateUWrapModePos; // 19: anisoLevel: 4, filterMode: 6, textureWrapMode: 3*3
        private const int k_StateLowBitsAvailable = 32 - k_StateLSBitsUsed; // 32-19 = 13

        private const int k_StateUniqueNumBits = k_StateLowBitsAvailable; // ie use everything else for the "make sampler state unique" bits
        private const int k_StateUniquePos = k_StateUWrapModePos - k_StateUniqueNumBits; //0

        private const ulong k_StateUniqueBitsSetMask = ((((ulong)(1) << k_StateUniqueNumBits) - 1) << k_StateUniquePos); // OR mask to set, AND mask to select
        private const ulong k_StateUniqueBitsClearMask = ~k_StateUniqueBitsSetMask; // AND mask to clear

        private ulong EncodeSamplerState(byte filterMode, byte addressU, byte addressV, byte addressW, float mipLODBias, byte anisoLevel, bool makeUnique)
        {
            // The highPart is just the mipLODBias, whatever endianness we have is ok since we use the same conversion functions
            ulong highPart = BitConverter.ToUInt32(BitConverter.GetBytes(mipLODBias), 0);

            // The lowPart uses 19 bits: (4bits + 6bits + 3x3bits), 2^4 = 16, 2^6 = 64, 2^3 = 8
            ulong lowPart =
                ((ulong)anisoLevel << k_StateAnisoLevelPos)
                | ((ulong)filterMode << k_StateFilterModePos)
                | ((ulong)addressW << k_StateWWrapModePos)
                | ((ulong)addressV << k_StateVWrapModePos)
                | ((ulong)addressU << k_StateUWrapModePos);
            ulong state = (highPart << 32) | lowPart;

            // Finally, we also want to add-in some bits in case a client wants its own slot to escape sharing.
            // We do that with the (32-19) = 13 bits left: we only need 3-5 bits at most due to total sampler limit anyways.
            if (makeUnique)
            {
                m_UniqueClientStateCount++;
                ulong uniqueState = ((uint)(m_UniqueClientStateCount) << k_StateUniquePos);
                state = state | uniqueState;
            }
            return state;
        }

        // Tools for the asset importer observer that takes note that the StackLit shader was edited to reset SHARED_SAMPLER_USED_NUM
        // (see GetMaterialSharedSamplersMaxAllowedNum())
        // returns true if the property was changed.
        public static bool CheckUpdateMaterialFloatProperty(Material material, string property, float value)
        {
            if (material.HasProperty(property) && material.GetFloat(property) != value)
            {
                material.SetFloat(property, value);
                return true;
            }
            return false;
        }

        // returns true if the property was changed (material now dirty)
        public static bool CheckUpdateMaterialSharedSamplerUsedNumDefineProperty(Material material, float value)
        {
            return CheckUpdateMaterialFloatProperty(material, k_SharedSamplerUsedNumDefine, value);
        }

        // Tools for the StackLitUI when creating an instance of this class so it doesn't have to force a read from file
        // at each UI tick.
        // (see GetMaterialSharedSamplersMaxAllowedNum())
        public static int GetMaterialSharedSamplerUsedNumDefineProperty(Material material)
        {
            if (material.HasProperty(k_SharedSamplerUsedNumDefine))
            {
                return (int)material.GetFloat(k_SharedSamplerUsedNumDefine);
            }
            Debug.LogError("Can't find shader property " + k_SharedSamplerUsedNumDefine + " will use a default value of " + k_DefaultSharedSamplerUsedNum);
            return k_DefaultSharedSamplerUsedNum;
        }

        // Return allowed maximum number of shared samplers that can be used:
        // Minimum between what the material has declared, definedSharedSamplerUsedNum and the maximum we have in the enum
        // Make sure the hardcoded SHARED_SAMPLER_USED_NUM is high enough to include those of the material obviously.
        private static int GetMaterialSharedSamplersMaxAllowedNum(Material material, string shaderName, int definedSharedSamplerUsedNum)
        {
            int sharedSamplerNum = 0;
            //Texture texture = material.GetTexture(k_SharedPropertyBaseName + sharedSamplerNum.ToString());
            bool hasSampler = material.HasProperty(k_SharedPropertyBaseName + sharedSamplerNum.ToString());

            while (hasSampler && sharedSamplerNum < SharedSamplersMaxNum)
            {
                sharedSamplerNum++;
                //texture = material.GetTexture(k_SharedPropertyBaseName + sharedSamplerNum.ToString());
                hasSampler = material.HasProperty(k_SharedPropertyBaseName + sharedSamplerNum.ToString());
            }

            // The promise definedSharedSamplerUsedNum is capped above by SharedSamplersMaxNum of the enum and the declared
            // material's shared sampler slots properties just discovered, sharedSamplerNum.
            // If we specify <= 0, it means to parse to file to discover it ourselves from the first found in the .shader:
            if (definedSharedSamplerUsedNum <= 0)
            {
                Shader masterShader = Shader.Find(shaderName);
                if (masterShader == null)
                {
                    Debug.LogWarning("Cannot find original non generated shader!");
                    masterShader = material.shader;
                }

                definedSharedSamplerUsedNum = TextureSamplerSharingShaderGenerator.GetOriginalShaderDefinedSharedSamplerUseNum(ref definedSharedSamplerUsedNum, masterShader) ?
                    definedSharedSamplerUsedNum : k_DefaultSharedSamplerUsedNum;
                // Update the StackLitUI used caching property so that it can pass it back to us next time so we don't read it
                // from file at each UI tick:
                CheckUpdateMaterialSharedSamplerUsedNumDefineProperty(material, definedSharedSamplerUsedNum);
            }

            return Math.Min(definedSharedSamplerUsedNum, sharedSamplerNum);
        }

        // shaderName is the master, non generated shader "name" (ie HDRenderPipeline/StackLit)
        public void Reset(Material material, string shaderName, Action<SamplerClient, int, bool, bool> assignmentCallback, int definedSharedSamplerUsedNum)
        {
            if (material != null)
            {
                m_Material = material;
            }
            m_MaterialSharedSamplersMaxAllowed = GetMaterialSharedSamplersMaxAllowedNum(material, shaderName, definedSharedSamplerUsedNum);
            m_UniqueClientStateCount = 0;
            m_ClientAssignedToSharedSlot = assignmentCallback;
            m_UniqueSharedSamplerStates.Clear();
            m_UniqueExternalSamplerStates.Clear();
            ExternalSamplersUsedOnLastAssignement = 0;
            BuiltInSamplersUsedOnLastAssignement = 0;
            SharedSamplersUsedOnLastAssignement = 0;
            SharedSamplersNeededOnLastAssignement = 0;
        }

        public TextureSamplerSharing(Material material, string shaderName = StackLitGUI.k_StackLitShaderName,
            Action<SamplerClient, int, bool, bool> assignmentCallback = null, int definedSharedSamplerUsedNum = k_DefaultSharedSamplerUsedNum)
        {
            m_UniqueSharedSamplerStates = new Dictionary<ulong, List<SamplerClient>>();
            m_UniqueExternalSamplerStates = new Dictionary<ulong, List<SamplerClient>>();
            Reset(material, shaderName, assignmentCallback, definedSharedSamplerUsedNum);
        }

        // This needs to be called first before calling AddClientForExternalExistingSampler() to use common built-in samplers
        // we know we already use and which are enumerated in the enum of this class.
        // This places dummy/sentinel clients in the dictionary to hold these sampler's states so we can generically add
        // clients using those.
        public void AddExternalExistingSamplerStates()
        {
            m_UniqueExternalSamplerStates.Clear();
            foreach (ExternalExistingSampler extSamplerState in Enum.GetValues(typeof(ExternalExistingSampler)))
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
                List<SamplerClient> clients = new List<SamplerClient>() { client, };
                m_UniqueExternalSamplerStates.Add(samplerState, clients);
            }
        }

        public void SetClientAssignmentCallback(Action<SamplerClient, int, bool, bool> assignmentCallback)
        {
            m_ClientAssignedToSharedSlot = assignmentCallback;
        }

        private void AddClientForExternalExistingSampler(string basePropertyName, ExternalExistingSampler extSamplerUsed, bool makeUnique)
        {
            List<SamplerClient> clients;
            ulong samplerState = EncodeSamplerState(extSamplerUsed, makeUnique);

            if (m_UniqueExternalSamplerStates.TryGetValue(samplerState, out clients) || makeUnique)
            {
                SamplerClient client = new SamplerClient()
                {
                    BasePropertyName = basePropertyName,
                    ATexture = null,
                };
                if (makeUnique == false)
                {
                    clients.Add(client);
                }
                else
                {
                    // This is for clients who only register for assignment callback, but slot number assignments will be
                    // meaningless for them (this is used when using shader generation and making maps use their own samplers)
                    clients = new List<SamplerClient>() { client, };
                    m_UniqueExternalSamplerStates.Add(samplerState, clients);
                }
            }
            else
            {
                // Error: call AddExistingSamplerStates() first, there should already
                // be an entry in the dictionary for that state.
                Assert.IsTrue(false, "Call to AddClientForExternalExistingSampler without existing / external samplers added.");
            }
        } //AddClient using an existing external sampler (eg named-based parsed by the engine)

        // See AddExternalExistingSamplerStates:
        // Add a client map that is using an existing external sampler (eg named-based parsed by the engine)
        public void AddClientForExternalExistingSampler(string basePropertyName, ExternalExistingSampler extSamplerUsed)
        {
            AddClientForExternalExistingSampler(basePropertyName, extSamplerUsed, makeUnique: false);
        }

        // Add a client map that is going to be using its own existing external sampler (only useful when using generation)
        // This is to link with the shader generator using the same callback mechanism when doing DoClientAssignment:
        // This creates a dummy unique state entry in the external sampler states and allow to register for callback
        // for those clients too, which then configure the generator.
        public void AddClientForOwnUniqueSampler(string basePropertyName)
        {
            AddClientForExternalExistingSampler(basePropertyName, ExternalExistingSampler.LinearClamp, makeUnique: true);
        }

        public void AddClient(string basePropertyName, Texture texture, bool makeUnique = false, bool tryExternalExistingSamplers = true)
        {
            List<SamplerClient> clients;
            ulong samplerState = EncodeSamplerState(texture, makeUnique);

            SamplerClient client = new SamplerClient()
            {
                BasePropertyName = basePropertyName,
                ATexture = texture,
            };

            if (tryExternalExistingSamplers && (m_UniqueExternalSamplerStates.TryGetValue(samplerState, out clients)))
            {
                clients.Add(client);
            }
            else if (m_UniqueSharedSamplerStates.TryGetValue(samplerState, out clients))
            {
                clients.Add(client);
            }
            else
            {
                clients = new List<SamplerClient>() { client, };
                m_UniqueSharedSamplerStates.Add(samplerState, clients);
            }
        }//AddClient

        public int GetRequiredSamplerStatesNum()
        {
            return m_UniqueSharedSamplerStates.Count;
        }

        private List<KeyValuePair<ulong, List<SamplerClient>>> GetSortedStateList()
        {
            // We will sort the unique states used by order of number of clients using them
            // so that we minimize the number of clients that will use their own unshared samplers
            // in the case where m_UniqueSharedSamplerStates.Count > m_MaterialSharedSamplerNum
            var stateList = m_UniqueSharedSamplerStates.ToList();
            // This would give ascending sort:
            //stateList.Sort( (keyValA, keyValB) => (keyValA.Value.Count - keyValB.Value.Count));
            // We want the opposite:
            stateList.Sort((keyValA, keyValB) => (keyValB.Value.Count - keyValA.Value.Count));
            return stateList;
        }

        // Returns the number of shared samplers that will be used.
        public int DoClientAssignment()
        {
            int totalSamplersUsed = 0;
            var stateList = GetSortedStateList();

            int i;
            for (i = 0; i < Math.Min(stateList.Count, m_MaterialSharedSamplersMaxAllowed); ++i)
            {
                // Make sure at least one texture client will fill the shared texture property
                // slot so it is binded and with it, the sampler state we're going to share.
                // We will use the first client for that:
                m_Material.SetTexture(k_SharedPropertyBaseName + i, stateList[i].Value[0].ATexture);

                // Callback to let the client of the TextureSamplerSharing know the slot assignment
                // for each client of this state "i" (which will be setup on shared texture slot "i",
                // per the SetTexture we just did above):
                foreach (var samplerClient in stateList[i].Value)
                {
                    m_ClientAssignedToSharedSlot(samplerClient, i, /* external sampler?:*/ false, /* unique client?:*/ IsSamplerForUniqueClient(stateList[i].Key));
                }
            }

            totalSamplersUsed += i;
            SharedSamplersUsedOnLastAssignement = i;
            SharedSamplersNeededOnLastAssignement = stateList.Count;

            // Make sure we null the unused left-over shared sampler maps (if any) to clear any maps
            // that might have been assigned previously:
            for (; i < m_MaterialSharedSamplersMaxAllowed; ++i)
            {
                m_Material.SetTexture(k_SharedPropertyBaseName + i, null);
            }

            int builtInSamplersUsed = 0;
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
                        builtInSamplersUsed++;
                        continue;
                    }
                    m_ClientAssignedToSharedSlot(samplerClient, i, /* external sampler?:*/ true, /* unique client?:*/ IsSamplerForUniqueClient(stateList[i].Key));
                    j++;
                }
            }
            totalSamplersUsed += i;
            ExternalSamplersUsedOnLastAssignement = i;
            BuiltInSamplersUsedOnLastAssignement = builtInSamplersUsed;

            return totalSamplersUsed;
        } // DoClientAssignment

        // Like DoClientAssignment() but to process the spill if any (only callback is done)
        public void DoSpilledClientAssignment(Action<SamplerClient, int, bool, bool> clientSpilledCallback)
        {
            var stateList = GetSortedStateList();
            for (int i = m_MaterialSharedSamplersMaxAllowed; i < stateList.Count; ++i)
            {
                foreach (var samplerClient in stateList[i].Value)
                {
                    clientSpilledCallback(samplerClient, i, /* external sampler?:*/ false, /* unique client?:*/ IsSamplerForUniqueClient(stateList[i].Key));
                }
            }
        }

        public uint UniqueClientNumFromSamplerState(ulong samplerState)
        {
            uint lowPart = (uint)(samplerState & 0xFFFFFFFF);
            uint unique = ((lowPart & ((((uint)(1) << k_StateUniqueNumBits) - 1) << k_StateUniquePos)) >> k_StateUniquePos);
            return unique;
        }

        public bool IsSamplerForUniqueClient(ulong samplerState)
        {
            return UniqueClientNumFromSamplerState(samplerState) != 0;
        }

    } // TextureSamplerSharing

    public class TextureSamplerSharingShaderGenerator
    {
        // Note: this class depends on some naming conventions used in BaseMaterialUI and TextureSamplerSharing.hlsl,
        // eg all texture maps properties are named with a base property name + "Map",
        // the property base property name + "UseMap" is also declared and enables map sampling when > 0,
        // <PropertyName>Map_USES_OWN_SAMPLER defines (to 0 or 1) exist in the property file,
        // SHARED_SAMPLER_USED_NUM is defined in the .shader.
        // 
        // Strings: (Some of these are used on the HLSL side as well)
        // Generated
        // Properties.hlsl
        // Map
        // UseMap
        // _USES_OWN_SAMPLER
        // SHARED_SAMPLER_USED_NUM
        private const string k_Generated = "Generated";
        private const string k_GeneratedSuffix = "/" + k_Generated + "/";

        public const string k_SharedSamplerUsedNumDefine = "SHARED_SAMPLER_USED_NUM";
        public const string k_StackLitGeneratedShaderNamePrefix = StackLitGUI.k_StackLitShaderName + k_GeneratedSuffix; // shaderlab name string of a generated shader is this the sampling config state MD5
        // Matches main or generated shaders, no anchor (\A \z)
        public const string k_StackLitFamilyFindRegexPattern = @"(?<shadername>HDRenderPipeline\/StackLit)(\/Generated\/(?<statecode>[0-9a-fA-F]{32}))?";

        private const int k_UseMapUseOwnSamplerValue = 1000;

        // Regex pattern for #define _USES_OWN_SAMPLER lines:
        // ^[\t\ ]*#define[\t\ ]{1,}(?<mapname>(?<basename>[a-zA-Z_0-9]{1,}?)Map)(?<uses_own>_USES_OWN_SAMPLER)[\t\ ]*(?<value>[0-1])[\t\ ]*$
        //
        // Regex pattern for #define SHARED_SAMPLER_USED_NUM line (optional commented, uncommented, uncommented with same line comment):
        // ^[\t\ ]*(?<comment>\/\/){0,}[\t\ ]*#define[\t\ ]{1,}(?<uses_own>SHARED_SAMPLER_USED_NUM)[\t\ ]*(?<value>[0-9]{1,2})[\t\ ]*$
        // ^[\t\ ]*#define[\t\ ]{1,}(?<uses_own>SHARED_SAMPLER_USED_NUM)[\t\ ]*(?<value>[0-9]{1,2})[\t\ ]*$
        // ^[\t\ ]*#define[\t\ ]{1,}(?<uses_own>SHARED_SAMPLER_USED_NUM)[\t\ ]*(?<value>[0-9]{1,2})[\t\ ]*(?<comment>(\/\/.*|[\t\ ]*))$
        //
        // Regex pattern for UseMap declaration lines:
        // ^[\t\ ]*(?<type>float)[\t\ ]{1,}(?<name>\w.*UseMap);[\t\ ]*$
        //
        // Regex pattern for #include properties file in the .shader:
        //^[\t\ ]*#include[\t\ ]*".{1,}?(?<name>StackLit\/StackLitProperties\.hlsl)"[\t\ ]*$
        //
        //const string k_IncludeRegexPattern = @"^[\t\ ]*#include[\t\ ]*"".{1,}?(?<name>{0}\/{0}Properties\.hlsl)""[\t\ ]*$";
        //
        const string k_DefineUsesOwnSamplerRegexPattern = @"^[\t\ ]*#define[\t\ ]{1,}(?<mapname>(?<basename>[a-zA-Z_0-9]{1,}?)Map)(?<uses_own>_USES_OWN_SAMPLER)[\t\ ]*(?<value>[0-1])[\t\ ]*(\/\/.*|[\t\ ]*)$";
        //const string k_DefineSharedSamplerUsedNumRegexPattern = @"^[\t\ ]*#define[\t\ ]{1,}(?<uses_own>SHARED_SAMPLER_USED_NUM)[\t\ ]*(?<value>[0-9]{1,2})[\t\ ]*(\/\/.*|[\t\ ]*)$";
        const string k_DefineSharedSamplerUsedNumRegexPattern = @"^[\t\ ]*#define[\t\ ]{1,}(?<uses_own>" + k_SharedSamplerUsedNumDefine + @")[\t\ ]*(?<value>[0-9]{1,2})[\t\ ]*(\/\/.*|[\t\ ]*)$";

        const string k_IncludeRegexPatternA = @"^[\t\ ]*#include[\t\ ]*"".{1,}?(?<name>";
        const string k_IncludeRegexPatternB = @"{0}\/{0}Properties\.hlsl)""[\t\ ]*(\/\/.*|[\t\ ]*)$"; // maybe use (?i)Properties\.hlsl(?-i)
        const string k_UseMapProperyRegexPattern = @"^[\t\ ]*(?<type>float)[\t\ ]{1,}(?<name>\w.*UseMap);[\t\ ]*(\/\/.*|[\t\ ]*)$";
        const string k_ShaderHeaderRegexPattern = @"^[\t\ ]*Shader[\t\ ]*""[\t\ ]*(?<name>.{1,}?)[\t\ ]*""[\t\ ]*(\/\/.*|[\t\ ]*)$";

        private string m_ShaderName; // "HDRenderPipeline/StackLit";
        private string m_ShaderSimpleName; // "StackLit";
        private string m_DefaultGeneratedFilesPath; // "Assets/" + m_ShaderName + "Generated";
        private string m_GeneratedFilesPath; // m_DefaultGeneratedFilesPath;

        private Shader m_OriginalShader;
        private string m_OriginalShaderFilePath;
        private string m_OriginalShaderPropertiesFilePath;

        private Dictionary<string, float> m_SamplerSharingConfig;

        // For state building, we assume AddUseMapPropertyConfig() is called-back by the TextureSamplerSharing
        // client assignment callback and since TextureSamplerSharing object do assignments by usemap value groups,
        // we assume we will receive all client map properties for any one sampler slot grouped together and ease
        // the config state build-up that way:
        private class SamplerClientGroup
        {
            public float UseMapValue;
            public List<string> Clients;
        }
        private string m_FinalConfigString;
        private string m_FinalConfigStringPrint;
        private string m_FinalConfigMD5;
        private MD5 m_MD5; // we don't need cryto grade here, this is ok

        private SamplerClientGroup m_CurrentGroup;
        private bool m_CurrentGroupIsSamplerExternal;

        private List<SamplerClientGroup> m_SharedSamplerClientGroups;
        private List<SamplerClientGroup> m_ExtSamplerClientGroups;
        private List<string> m_UsesOwnSamplerGroup;

        private void GroupsAndConfigStateReset()
        {
            m_SharedSamplerClientGroups = new List<SamplerClientGroup>();
            m_ExtSamplerClientGroups = new List<SamplerClientGroup>();
            m_UsesOwnSamplerGroup = new List<string>();
            m_CurrentGroup = null;
            m_FinalConfigString = null;
            m_FinalConfigStringPrint = null;
            m_FinalConfigMD5 = null;
        }

        private void GroupBeginNew(string usemapPropertyName, float useMapNum, bool isSamplerExternal)
        {
            m_CurrentGroup = new SamplerClientGroup() { UseMapValue = useMapNum, Clients = new List<string>() { usemapPropertyName } };
            m_CurrentGroupIsSamplerExternal = isSamplerExternal;
        }

        private void CurrentGroupFinalize()
        {
            m_CurrentGroup.Clients.Sort(StringComparer.Ordinal);
            if (m_CurrentGroupIsSamplerExternal)
            {
                m_ExtSamplerClientGroups.Add(m_CurrentGroup);
            }
            else
            {
                m_SharedSamplerClientGroups.Add(m_CurrentGroup);
            }
            m_CurrentGroup = null;
        }

        private void CurrentGroupTestFinalizeBeginNew(string usemapPropertyName, float useMapNum, bool isSamplerExternal)
        {
            bool changeGroup = (m_CurrentGroup != null) &&
                (m_CurrentGroup.UseMapValue != useMapNum
                || m_CurrentGroupIsSamplerExternal != isSamplerExternal);

            if (changeGroup)
            {
                CurrentGroupFinalize();
                GroupBeginNew(usemapPropertyName, useMapNum, isSamplerExternal);
            }
            else
            {
                if (m_CurrentGroup == null)
                {
                    GroupBeginNew(usemapPropertyName, useMapNum, isSamplerExternal);
                }
                else
                {
                    m_CurrentGroup.Clients.Add(usemapPropertyName);
                }
            }
        }

        // Called on AddUseMapPropertyConfig:
        private void AddUseMapPropertyHandleGrouping(string usemapPropertyName, float useMapNum, bool isSamplerExternal)
        {
            if (useMapNum == k_UseMapUseOwnSamplerValue)
            {
                m_UsesOwnSamplerGroup.Add(usemapPropertyName);
            }
            else
            {
                CurrentGroupTestFinalizeBeginNew(usemapPropertyName, useMapNum, isSamplerExternal);
            }
        }

        // Called before generating MD5, generates the state string of map sampling configuration, along with a beautified
        // version.
        private void GroupingFinalizeAndGenerateFinalConfigString(bool outputBeautify)
        {
            if (m_CurrentGroup != null)
            {
                CurrentGroupFinalize();
            }
            m_UsesOwnSamplerGroup.Sort(StringComparer.Ordinal);
            // We sort the groups between each other, using the first client of each as comparison point:
            m_SharedSamplerClientGroups.Sort((a, b) => string.Compare(a.Clients[0], b.Clients[0], StringComparison.Ordinal));
            m_ExtSamplerClientGroups.Sort((a, b) => string.Compare(a.Clients[0], b.Clients[0], StringComparison.Ordinal));

            // Now just build the txt
            StringBuilder stringBuilder = new StringBuilder();
            StringBuilder stringBuilderPrint = new StringBuilder();

            if (outputBeautify) stringBuilderPrint.Append("<shared_sampler_slots>\n");
            foreach (SamplerClientGroup group in m_SharedSamplerClientGroups)
            {
                if (outputBeautify) stringBuilderPrint.Append("\t<slot>\n");
                if (outputBeautify) stringBuilderPrint.AppendFormat("\t\t<num>{0}</num>\n", group.UseMapValue);
                stringBuilder.Append("shared:");
                foreach (string clientUseMapProp in group.Clients)
                {
                    if (outputBeautify) stringBuilderPrint.AppendFormat("\t\t<map>{0}</map>\n", clientUseMapProp);
                    stringBuilder.AppendFormat("{0},", clientUseMapProp);
                }
                stringBuilder.Append("\n");
                if (outputBeautify) stringBuilderPrint.Append("\t</slot>\n");
            }
            if (outputBeautify) stringBuilderPrint.AppendFormat("\t<num_used>{0}</num_used>\n", m_SharedSamplerClientGroups.Count);
            if (outputBeautify) stringBuilderPrint.Append("</shared_sampler_slots>\n");

            if (outputBeautify) stringBuilderPrint.Append("<ext_sampler_slots>\n");
            foreach (SamplerClientGroup group in m_ExtSamplerClientGroups)
            {
                if (outputBeautify) stringBuilderPrint.Append("\t<slot>\n");
                if (outputBeautify) stringBuilderPrint.AppendFormat("\t\t<num>{0}</num>\n", group.UseMapValue);
                stringBuilder.AppendFormat("ext_num:{0}:", group.UseMapValue);
                foreach (string clientUseMapProp in group.Clients)
                {
                    if (outputBeautify) stringBuilderPrint.AppendFormat("\t\t<map>{0}</map>\n", clientUseMapProp);
                    stringBuilder.AppendFormat("{0},", clientUseMapProp);
                }
                stringBuilder.Append("\n");
                if (outputBeautify) stringBuilderPrint.Append("\t</slot>\n");
            }
            if (outputBeautify) stringBuilderPrint.Append("</ext_sampler_slots>\n");

            if (outputBeautify) stringBuilderPrint.Append("<own_sampler>\n");
            stringBuilder.AppendFormat("own:");
            foreach (string clientUseMapProp in m_UsesOwnSamplerGroup)
            {
                if (outputBeautify) stringBuilderPrint.AppendFormat("\t<map>{0}</map>\n", clientUseMapProp);
                stringBuilder.AppendFormat("{0},", clientUseMapProp);
            }
            stringBuilder.Append("\n");
            if (outputBeautify) stringBuilderPrint.Append("</own_sampler>\n");

            m_FinalConfigString = stringBuilder.ToString();
            m_FinalConfigStringPrint = outputBeautify ? stringBuilderPrint.ToString() : null;
        }

        private void SetShaderNameAndPaths(string shaderName)
        {
            m_ShaderName = shaderName ?? StackLitGUI.k_StackLitShaderName;
            m_ShaderSimpleName = m_ShaderName.Substring(m_ShaderName.LastIndexOf('/') + 1); // if there's no /, too bad, we take everything

            m_DefaultGeneratedFilesPath = "Assets/" + m_ShaderName + k_Generated;
            m_GeneratedFilesPath = m_DefaultGeneratedFilesPath;

            m_OriginalShader = Shader.Find(m_ShaderName);
            if (m_OriginalShader != null)
            {
                m_OriginalShaderFilePath = AssetDatabase.GetAssetPath(m_OriginalShader);
                m_OriginalShaderPropertiesFilePath = m_OriginalShaderFilePath.Replace(".shader", "Properties.hlsl");
            }
            else
            {
                m_OriginalShaderFilePath = m_OriginalShaderPropertiesFilePath = "";
            }
        }

        private bool TestCreatePath(string suggestedPath)
        {
            bool exists = Directory.Exists(suggestedPath);
            if (!exists)
            {
                try
                {
                    Directory.CreateDirectory(suggestedPath);
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }
                exists = Directory.Exists(suggestedPath);
            }
            return exists;
        }

        private string GetFileCheckSum(string path)
        {
            try
            {
                using (var stream = File.OpenRead(path))
                {
                    return ByteToHexString(m_MD5.ComputeHash(stream), lowerCase: true); //BitConverter.ToString(m_MD5.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
                }
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                return null;
            }
        }

        private static string ByteToHexString(byte[] bytes, bool lowerCase = false)
        {
            char[] c = new char[bytes.Length * 2];
            int nibble;
            // reminder:
            // ascii '0' has value 48, 'A' is 65, 'a' is 97, nibble is 0 to 15,
            // 55 = 65-10 (when nibble >= 10, we start at 'A')
            // -7 = 48-55 (when nibble >= 0, we start at '0')
            // we work on 32bit int before casting to char:
            int offsetA = lowerCase ? 97 - 10 : 65 - 10; // offsetAFrom10
            int offset0 = 48 - offsetA; // offset0 from offsetAFrom10

            for (int i = 0; i < bytes.Length; i++)
            {
                nibble = bytes[i] >> 4;
                c[i * 2] = (char)(nibble + offsetA + (((nibble - 10) >> 31) & offset0));
                nibble = bytes[i] & 0xF;
                c[i * 2 + 1] = (char)(nibble + offsetA + (((nibble - 10) >> 31) & offset0));
            }
            return new string(c);
        }

        private string GetGeneratedShaderFilePath()
        {
            string generatedShaderFilePath = m_GeneratedFilesPath + "/" + m_FinalConfigMD5 + ".shader";
            return generatedShaderFilePath;
        }

        private string GetGeneratedPropertiesFilePath()
        {
            string ouputPropertiesFilePath = m_GeneratedFilesPath + "/" + m_FinalConfigMD5 + ".hlsl";
            return ouputPropertiesFilePath;
        }

        private string GetGeneratedShaderInfoAssetPath()
        {
            string ouputInfoAssetFilePath = m_GeneratedFilesPath + "/" + m_FinalConfigMD5 + ".asset";
            return ouputInfoAssetFilePath;
        }

        private string GetGeneratedConfigDebugAssetPath()
        {
            string ouputInfoAssetFilePath = m_GeneratedFilesPath + "/" + m_FinalConfigMD5 + ".dbg.txt";
            return ouputInfoAssetFilePath;
        }

        private string GetGeneratedConfigAssetPath()
        {
            string ouputInfoAssetFilePath = m_GeneratedFilesPath + "/" + m_FinalConfigMD5 + ".txt";
            return ouputInfoAssetFilePath;
        }

        private string GetGeneratedShaderNameForFind()
        {
            string generatedShaderName = m_ShaderName + k_GeneratedSuffix + m_FinalConfigMD5; // check stacklitUI if /Generated/ is changed
            return generatedShaderName;
        }

        private Shader GetGeneratedShaderAtPath(string generatedShaderFilePath)
        {
            Shader generatedShader = AssetDatabase.LoadAssetAtPath(generatedShaderFilePath, typeof(Shader)) as Shader;
            //TODOTODO also check if this leaks, consider ShaderUtil
            return generatedShader;
        }

        private bool ReadyToGenerateShader()
        {
            bool ready = (m_FinalConfigMD5 != null) && Directory.Exists(m_GeneratedFilesPath);
            ready &= (m_OriginalShader != null);
            ready &= File.Exists(m_OriginalShaderFilePath);
            ready &= File.Exists(m_OriginalShaderPropertiesFilePath);
            return ready;
        }

        public TextureSamplerSharingShaderGenerator(string shaderName = null)
        {
            Reset(shaderName);
        }

        public void SetGeneratedFilesPath(string path = null)
        {
            string usePath = path ?? m_DefaultGeneratedFilesPath;
            m_GeneratedFilesPath = TestCreatePath(usePath) ? usePath : m_DefaultGeneratedFilesPath;
        }

        public void Reset(string shaderName = null)
        {
            if (m_SamplerSharingConfig != null)
            {
                m_SamplerSharingConfig.Clear();
            }
            else
            {
                m_SamplerSharingConfig = new Dictionary<string, float>();
            }

            SetShaderNameAndPaths(shaderName);
            SetGeneratedFilesPath();
            if (m_MD5 == null)
            {
                m_MD5 = MD5.Create();
            }
            GroupsAndConfigStateReset();
        }

        public void AddUseMapPropertyConfig(string usemapPropertyName, float usemapPropertyValue, bool isSamplerExternal, bool mapUsesOwnSampler = false)
        {
            // mapUsesOwnSampler makes sense for generation when the opt-out of sharing option was used for the sampler.
            // In that case, when we're doing shader generation, the map shouldn't even be handled by the TextureSamplerSharing
            // allocator (normally using "make unique" in AddClient) since it will eat a shared sampler for nothing.
            // Note that in that case, we might run out of samplers for real (not our shared ones) with a message from the engine
            // instead of our shader UI.
            //
            // Here, we change the usemap value to an an arbitrary value k_UseMapUseOwnSamplerValue to both encode
            // state and take note on our side to change the USES_OWN_SAMPLER define for that map on generation.
            //
            float useMapValue = (mapUsesOwnSampler ? k_UseMapUseOwnSamplerValue : usemapPropertyValue);
            m_SamplerSharingConfig.Add(usemapPropertyName, useMapValue);

            bool encodeStateWithMapName = true; // if true, just use eg _NormalMap instead of _NormalUseMap (cosmetic for the txt config dumps)
            string statePropertyName = encodeStateWithMapName ? usemapPropertyName.Remove(usemapPropertyName.Length - "UseMap".Length, "Use".Length) : usemapPropertyName;
            AddUseMapPropertyHandleGrouping(statePropertyName, useMapValue, isSamplerExternal);
        }

        // Must call this before HaveCurrentAndComptabibleGeneratedShader and GenerateShader
        public void SetSamplerFinalConfigMD5()
        {
            //old, simple, but too conservative:
            //byte[] finalConfig = m_SamplerSharingConfig.SelectMany(keyVal => Encoding.ASCII.GetBytes(keyVal.Key).Concat(BitConverter.GetBytes(keyVal.Value)) ).ToArray();
            //
            //new:
            //
            // We make the state MD5 less conservative when shuffling of same client assignments on differently ordered shared
            // sampler numbers (not for the built-in one though as the number used is important in that case, just for the shared
            // ones, in the usemap number range SharedSamplerID.First to Last):
            //
            // ie when only the sampler numbers used - but not clients sharing them - are changed, the state should
            // stay the same. This could be thought of being moot as our assignment algorithm is deterministic but it still depends
            // via the dictionary add and sorting (tie breaker when same number of clients use two states) on the key which is the
            // sampler state that the shared sampler slot is used for. So, eg, 3 clients could share one sampler but be ordered
            // differently in the dictionary wrt to another sampler state being shared by 3 other clients, depending on the sampler
            // state, although the generated shader shouldn't care if in the end the first group of clients use slot x and the second
            // group slot y.
            bool outputBeautify = true;
            GroupingFinalizeAndGenerateFinalConfigString(outputBeautify);
            byte[] finalConfig = Encoding.ASCII.GetBytes(m_FinalConfigString);

            m_FinalConfigMD5 = ByteToHexString(m_MD5.ComputeHash(finalConfig), lowerCase: true); //BitConverter.ToString(m_MD5.ComputeHash(finalConfig)).Replace("-", "").ToLowerInvariant();
        }

        public bool HaveCurrentAndComptabibleGeneratedShader(Material material, bool tryAssignAlreadyGenerated = true)
        {
            // If the original shader isn't even found, can't say if it is compatible:
            if (m_OriginalShader == null || !ReadyToGenerateShader())
                return false;

            string materialShaderPath = AssetDatabase.GetAssetPath(material.shader);
            string generatedShaderFilePath = GetGeneratedShaderFilePath();
            if (!generatedShaderFilePath.Equals(materialShaderPath, StringComparison.OrdinalIgnoreCase))
            {
                if (!tryAssignAlreadyGenerated)
                    return false;

                // The currently assigned shader is not good for our wanted config, check if we have a generated one
                // We don't use the Shader.Find() method (lookup of shaderlab's Shader "" header path):
                // because we could have a case where identically UseMap (sampler) configured shaders have the same
                // generatedShaderName but are different paths, and one would prevent us from reaching the other in the
                // path we want (including a newer - if needed - generated one we would have generated in a new m_GeneratedFilesPath 
                // and that could be "hidden" by another matching the Find header string).
                // For this reason, we prefer loading by path:

                Shader generatedShader = GetGeneratedShaderAtPath(generatedShaderFilePath);
                if (generatedShader == null)
                    return false;

                // Re-assign the shader to the generated we found. We will check if it's stale with infoAssetPath below:
                material.shader = generatedShader;
            }

            string infoAssetPath = GetGeneratedShaderInfoAssetPath();
            TextureSamplerSharingShaderInfo shaderInfo = AssetDatabase.LoadAssetAtPath(infoAssetPath, typeof(TextureSamplerSharingShaderInfo)) as TextureSamplerSharingShaderInfo;
            if (shaderInfo == null)
                return false;

            try
            {
                bool origShaderFileTouchedSinceLastCheck = shaderInfo.LastChecksumDateUTC < File.GetLastWriteTimeUtc(m_OriginalShaderFilePath);
                bool origPropertiesFileTouchedSinceLastCheck = shaderInfo.LastChecksumDateUTC < File.GetLastWriteTimeUtc(m_OriginalShaderPropertiesFilePath);
                bool origShaderFileSame = !origShaderFileTouchedSinceLastCheck;
                bool origPropertiesFileSame = !origPropertiesFileTouchedSinceLastCheck;

                if (origShaderFileTouchedSinceLastCheck)
                {
                    string origShaderFileChecksum = GetFileCheckSum(m_OriginalShaderFilePath);
                    origShaderFileSame = (origShaderFileChecksum != null) && shaderInfo.ShaderFileChecksum.Equals(origShaderFileChecksum);
                }
                if (origPropertiesFileTouchedSinceLastCheck)
                {
                    string origPropertiesFileChecksum = GetFileCheckSum(m_OriginalShaderPropertiesFilePath);
                    origPropertiesFileSame = (origPropertiesFileChecksum != null) && shaderInfo.PropertiesFileChecksum.Equals(origPropertiesFileChecksum);
                }

                if (origShaderFileTouchedSinceLastCheck || origPropertiesFileTouchedSinceLastCheck)
                {
                    if (origShaderFileSame && origPropertiesFileSame)
                    {
                        shaderInfo.LastChecksumDateUTC = DateTime.UtcNow;
                        EditorUtility.SetDirty(shaderInfo);
                        //AssetDatabase.SaveAssets(); // not needed
                    }
                    else
                    {
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                return false;
            }

            return true;
        } //IsCurrentShaderGeneratedAndComptabible(Material material)

        // Return the first found #define of SHARED_SAMPLER_USED_NUM in the original (master) .shader file.
        public static bool GetOriginalShaderDefinedSharedSamplerUseNum(ref int definedSharedSamplerUseNum, Shader shader)
        {
            bool retValid = false;
            if (shader == null)
            {
                return false;
            }

            string originalShaderFilePath = AssetDatabase.GetAssetPath(shader);
            try
            {
                using (StreamReader reader = File.OpenText(originalShaderFilePath))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        Match match = Regex.Match(line, k_DefineSharedSamplerUsedNumRegexPattern, RegexOptions.ExplicitCapture);
                        int readVal;
                        if (match.Success && int.TryParse(match.Groups["value"].Value, out readVal))
                        {
                            definedSharedSamplerUseNum = Math.Min(TextureSamplerSharing.SharedSamplersMaxNum, Math.Max(0, readVal));
                            retValid = true;
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }

            return retValid;
        } // GetOriginalShaderDefinedSharedSamplerUseNum

        public bool GenerateShader(Material material, bool assignGeneratedShader = false)
        {
            if (!ReadyToGenerateShader())
                return false;

            int numSharedSamplersEnabled = m_SharedSamplerClientGroups.Count;

            string includePropertyVariablesRegexPattern = k_IncludeRegexPatternA + string.Format(k_IncludeRegexPatternB, m_ShaderSimpleName);

            string ouputPropertiesFilePath = GetGeneratedPropertiesFilePath();
            string ouputShaderFilePath = GetGeneratedShaderFilePath();
            string ouputInfoAssetFilePath = GetGeneratedShaderInfoAssetPath();
            string outputConfigFilePath = GetGeneratedConfigAssetPath();
            string outputConfigDebugFilePath = GetGeneratedConfigDebugAssetPath();
            string generatedShaderName = GetGeneratedShaderNameForFind();

            try
            {
                string[] fileLines = File.ReadAllLines(m_OriginalShaderFilePath);

                var indexAndLinesWithMatch = fileLines.Select((line, index) => new { index, line }).Where(a => Regex.IsMatch(a.line, includePropertyVariablesRegexPattern));
                if (indexAndLinesWithMatch.Count() != 1)
                {
                    throw new Exception("Shader should have one (and only one) include property file directive in .shader file!");
                }
                // Substitute the original properties hlsl file include with our custom hardcoded one:
                foreach (var indexAndLine in indexAndLinesWithMatch)
                {
                    fileLines[indexAndLine.index] = string.Format("    #include \"{0}\"", ouputPropertiesFilePath);
                }

                indexAndLinesWithMatch = fileLines.Select((line, index) => new { index, line }).Where(a => Regex.IsMatch(a.line, k_ShaderHeaderRegexPattern));
                if (indexAndLinesWithMatch.Count() != 1)
                {
                    throw new Exception("Shader should have a single Shader \"name\" directive in .shader file!");
                }
                // Substitute the heading name with ours:
                fileLines[indexAndLinesWithMatch.First().index] = string.Format("Shader \"{0}\"", generatedShaderName);

                // #define SHARED_SAMPLER_USED_NUM line:
                indexAndLinesWithMatch = fileLines.Select((line, index) => new { index, line }).Where(a => Regex.IsMatch(a.line, k_DefineSharedSamplerUsedNumRegexPattern));
                if (indexAndLinesWithMatch.Count() != 1)
                {
                    throw new Exception("Shader should have a single " + k_SharedSamplerUsedNumDefine + " define directive in .shader file!");
                }
                // Substitute the original SHARED_SAMPLER_USED_NUM define with ours:
                foreach (var indexAndLine in indexAndLinesWithMatch)
                {
                    fileLines[indexAndLine.index] = "    #define " + k_SharedSamplerUsedNumDefine + " " + numSharedSamplersEnabled;
                }

                // Write out the new custom shader:
                File.WriteAllLines(ouputShaderFilePath, fileLines);

                // Now handle generating the hardcoded configured properties file that we referenced in our generated shader file:
                fileLines = File.ReadAllLines(m_OriginalShaderPropertiesFilePath);
                for (int i = 0; i < fileLines.Length; i++)
                {
                    // UseMap declaration:
                    Match match = Regex.Match(fileLines[i], k_UseMapProperyRegexPattern, RegexOptions.ExplicitCapture);
                    float samplerSlot;
                    if (match.Success && m_SamplerSharingConfig.TryGetValue(match.Groups["name"].Value, out samplerSlot)
                        && (samplerSlot != k_UseMapUseOwnSamplerValue)) // make sure the sampler slot is not the "usesOwnSampler" one
                    {
                        // We have found a UseMap property that is handled by our sharing configuration, swap out the uniform property
                        // declaration by a define with an hardcoded value:
                        fileLines[i] = string.Format("#define {0} {1}", match.Groups["name"].Value, samplerSlot);
                        continue;
                    }

                    // #define MapName_USES_OWN_SAMPLER 0 line:
                    match = Regex.Match(fileLines[i], k_DefineUsesOwnSamplerRegexPattern, RegexOptions.ExplicitCapture);
                    if (match.Success && m_SamplerSharingConfig.TryGetValue(match.Groups["basename"].Value + "UseMap", out samplerSlot)
                        && (samplerSlot == k_UseMapUseOwnSamplerValue))
                    {
                        // The sampler slot is the "usesOwnSampler" one: configure the MapName_USES_OWN_SAMPLER define with a value of "1":
                        fileLines[i] = "#define " + match.Groups["mapname"].Value + "_USES_OWN_SAMPLER 1";
                        continue;
                    }
                    // #define SHARED_SAMPLER_USED_NUM line:
                    match = Regex.Match(fileLines[i], k_DefineSharedSamplerUsedNumRegexPattern, RegexOptions.ExplicitCapture);
                    if (match.Success)
                    {
                        fileLines[i] = "#define " + k_SharedSamplerUsedNumDefine + " " + numSharedSamplersEnabled;
                        continue;
                    }
                }
                // Write out the new custom properties:
                File.WriteAllLines(ouputPropertiesFilePath, fileLines);

                // Write out the config:
                File.WriteAllText(outputConfigDebugFilePath, m_FinalConfigString);
                if (m_FinalConfigStringPrint != null)
                {
                    File.WriteAllText(outputConfigFilePath, m_FinalConfigStringPrint);
                }

            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                return false;
            }

            // Finally, also output the meta information about the state of the property and shader files we
            // used to generate our custom shader:
            TextureSamplerSharingShaderInfo shaderInfo = ScriptableObject.CreateInstance<TextureSamplerSharingShaderInfo>();
            shaderInfo.ShaderFileChecksum = GetFileCheckSum(m_OriginalShaderFilePath);
            shaderInfo.PropertiesFileChecksum = GetFileCheckSum(m_OriginalShaderPropertiesFilePath);
            //shaderInfo.SetLastChecksumDateUTC(DateTime.UtcNow);
            shaderInfo.LastChecksumDateUTC = DateTime.UtcNow;
            AssetDatabase.CreateAsset(shaderInfo, ouputInfoAssetFilePath);
            //AssetDatabase.SaveAssets(); // not needed

            if (assignGeneratedShader)
            {
                AssetDatabase.Refresh(); // TODO: Needed for our new files to be picked up but should probably use ShaderUtil CreateShaderAsset, RegisterShader.
                Shader generatedShader = GetGeneratedShaderAtPath(ouputShaderFilePath);
                if (generatedShader != null)
                {
                    material.shader = generatedShader;
                }
                else
                {
                    return false;
                }
            }
            return true;
        } // GenerateShader

    } // TextureSamplerSharingShaderGenerator
}