using System;
using UnityEngine.Rendering;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
[GenerateHLSL]
public struct DensityVolumeData
{
    public Vector3 scattering; // [0, 1], prefer sRGB
    public float   extinction; // [0, 1], prefer sRGB
    public Vector3 textureTiling;
    public int     textureIndex; //
    public Vector3 textureScroll;

    public static DensityVolumeData GetNeutralValues()
    {
        DensityVolumeData data;

        data.scattering = Vector3.zero;
        data.extinction = 0;
        data.textureIndex = -1;

        data.textureTiling = Vector3.one;
        data.textureScroll = Vector3.zero;

        return data;
    }
} // struct VolumeProperties

public class VolumeRenderingUtils
{
    public static float MeanFreePathFromExtinction(float extinction)
    {
        return 1.0f / extinction;
    }

    public static float ExtinctionFromMeanFreePath(float meanFreePath)
    {
        return 1.0f / meanFreePath;
    }

    public static Vector3 AbsorptionFromExtinctionAndScattering(float extinction, Vector3 scattering)
    {
        return new Vector3(extinction, extinction, extinction) - scattering;
    }

    public static Vector3 ScatteringFromExtinctionAndAlbedo(float extinction, Vector3 albedo)
    {
        return extinction * albedo;
    }

    public static Vector3 AlbedoFromMeanFreePathAndScattering(float meanFreePath, Vector3 scattering)
    {
        return meanFreePath * scattering;
    }
}

<<<<<<< HEAD
[Serializable]
public struct DensityVolumeParameters
{
    public Color albedo;       // Single scattering albedo: [0, 1]. Alpha is ignored
    public float meanFreePath; // In meters: [1, 1000000]. Should be chromatic - this is an optimization!
    public float asymmetry;    // Controls the phase function: [-1, 1]

    public void Constrain()
    {
        albedo.r = Mathf.Clamp01(albedo.r);
        albedo.g = Mathf.Clamp01(albedo.g);
        albedo.b = Mathf.Clamp01(albedo.b);
        albedo.a = 1.0f;

        meanFreePath = Mathf.Clamp(meanFreePath, 1.0f, float.MaxValue);

        asymmetry = Mathf.Clamp(asymmetry, -1.0f, 1.0f);
    }

    public DensityVolumeData GetData()
    {
        DensityVolumeData data = new DensityVolumeData();

        data.extinction = VolumeRenderingUtils.ExtinctionFromMeanFreePath(meanFreePath);
        data.scattering = VolumeRenderingUtils.ScatteringFromExtinctionAndAlbedo(data.extinction, (Vector3)(Vector4)albedo);

        return data;
    }
} // class VolumeParameters

=======
>>>>>>> master
public struct DensityVolumeList
{
    public List<OrientedBBox>      bounds;
    public List<DensityVolumeData> density;
}

public class VolumetricLightingSystem
{
    public enum VolumetricLightingPreset
    {
        Off,
        Normal,
        Ultra,
        Count
    } // enum VolumetricLightingPreset
<<<<<<< HEAD

    [Serializable]
    public struct ControllerParameters
    {
        public float vBufferNearPlane;                 // Distance in meters
        public float vBufferFarPlane;                  // Distance in meters
        public float depthSliceDistributionUniformity; // Controls the exponential depth distribution: [0, 1]
    } // struct ControllerParameters

    public class VBuffer
    {
        public struct Parameters
        {
            public Vector4 resolution;
            public Vector2 sliceCount;
            public Vector4 depthEncodingParams;
            public Vector4 depthDecodingParams;

            public Parameters(int w, int h, int d, ControllerParameters controlParams)
            {
                resolution          = new Vector4(w, h, 1.0f / w, 1.0f / h);
                sliceCount          = new Vector2(d, 1.0f / d);
                depthEncodingParams = Vector4.zero; // C# doesn't allow function calls before all members have been init
                depthDecodingParams = Vector4.zero; // C# doesn't allow function calls before all members have been init

                Update(controlParams);
            }

            public void Update(ControllerParameters controlParams)
            {
                float n = controlParams.vBufferNearPlane;
                float f = controlParams.vBufferFarPlane;
                float c = 2 - 2 * controlParams.depthSliceDistributionUniformity; // remap [0, 1] -> [2, 0]

                depthEncodingParams = ComputeLogarithmicDepthEncodingParams(n, f, c);
                depthDecodingParams = ComputeLogarithmicDepthDecodingParams(n, f, c);
            }

        } // struct Parameters

        const int k_NumFrames     = 2; // Double-buffer history and feedback
        const int k_NumBuffers    = 4; // See the list below

        const int k_IndexDensity  = 0;
        const int k_IndexIntegral = 1;
        const int k_IndexHistory  = 2; // Depends on frame ID
        const int k_IndexFeedback = 3; // Depends on frame ID

        long                     m_ViewID      =   -1; // (m_ViewID > 0) if valid
        RenderTexture[]          m_Textures    = null;
        RenderTargetIdentifier[] m_Identifiers = null;
        Parameters[]             m_Params      = null; // For the current and the previous frame

        public long GetViewID()
        {
            return m_ViewID;
        }

        public bool IsValid()
        {
            return m_ViewID > 0 && m_Textures != null && m_Textures[0] != null;
        }

        public Parameters GetParameters(uint frameIndex)
        {
            return m_Params[frameIndex & 1];
        }

        public void SetParameters(Parameters parameters, uint frameIndex)
        {
            m_Params[frameIndex & 1] = parameters;
        }
=======

    public struct VBufferParameters
    {
        public Vector4 resolution;
        public Vector2 sliceCount;
        public Vector4 uvScaleAndLimit;    // Necessary to work with sub-allocation (resource aliasing) in the RTHandle system
        public Vector4 depthEncodingParams;
        public Vector4 depthDecodingParams;
>>>>>>> master

        public VBufferParameters(Vector3Int viewportResolution, Vector3Int bufferResolution, Vector2 depthRange, float depthDistributionUniformity)
        {
<<<<<<< HEAD
            Debug.Assert(IsValid());
            return m_Identifiers[k_IndexDensity];
=======
            int w = viewportResolution.x;
            int h = viewportResolution.y;
            int d = viewportResolution.z;

            // The depth is fixed for now.
            Vector2 uvScale = new Vector2((float)w / (float)bufferResolution.x,
                                          (float)h / (float)bufferResolution.y);

            // vp_scale = vp_dim / tex_dim.
            // clamp to (vp_dim - 0.5) / tex_dim = vp_scale - 0.5 * (1 / tex_dim) =
            // vp_scale - 0.5 * (vp_scale / vp_dim) = vp_scale * (1 - 0.5 / vp_dim).
            Vector2 uvLimit = new Vector2((w - 0.5f) / (float)bufferResolution.x,
                                          (h - 0.5f) / (float)bufferResolution.y);

            resolution          = new Vector4(w, h, 1.0f / w, 1.0f / h);
            sliceCount          = new Vector2(d, 1.0f / d);
            uvScaleAndLimit     = new Vector4(uvScale.x, uvScale.y, uvLimit.x, uvLimit.y);

            float n = depthRange.x;
            float f = depthRange.y;
            float c = 2 - 2 * depthDistributionUniformity; // remap [0, 1] -> [2, 0]

            depthEncodingParams = ComputeLogarithmicDepthEncodingParams(n, f, c);
            depthDecodingParams = ComputeLogarithmicDepthDecodingParams(n, f, c);
>>>>>>> master
        }
    } // struct Parameters

<<<<<<< HEAD
        public RenderTargetIdentifier GetLightingIntegralBuffer() // Of the current frame
        {
            Debug.Assert(IsValid());
            return m_Identifiers[k_IndexIntegral];
        }

        public RenderTargetIdentifier GetLightingHistoryBuffer(uint frameIndex) // From the previous frame
        {
            Debug.Assert(IsValid());
            return m_Identifiers[k_IndexHistory + (frameIndex & 1)];
        }

        public RenderTargetIdentifier GetLightingFeedbackBuffer(uint frameIndex) // For the next frame
        {
            Debug.Assert(IsValid());
            return m_Identifiers[k_IndexFeedback - (frameIndex & 1)];
        }

        public void Create(long viewID, int w, int h, int d, ControllerParameters controlParams)
        {
            Debug.Assert(viewID > 0);
            Debug.Assert(w > 0 && h > 0 && d > 0);
=======
    public VolumetricLightingPreset preset { get { return (VolumetricLightingPreset)Math.Min(ShaderConfig.s_VolumetricLightingPreset, (int)VolumetricLightingPreset.Count); } }

    static ComputeShader    m_VolumeVoxelizationCS      = null;
    static ComputeShader    m_VolumetricLightingCS      = null;

    List<OrientedBBox>      m_VisibleVolumeBounds       = null;
    List<DensityVolumeData> m_VisibleVolumeData         = null;
    public const int        k_MaxVisibleVolumeCount     = 512;

    // Static keyword is required here else we get a "DestroyBuffer can only be called from the main thread"
    static ComputeBuffer    s_VisibleVolumeBoundsBuffer = null;
    static ComputeBuffer    s_VisibleVolumeDataBuffer   = null;
>>>>>>> master

    // These two buffers do not depend on the frameID and are therefore shared by all views.
    RTHandleSystem.RTHandle m_DensityBufferHandle;
    RTHandleSystem.RTHandle m_LightingBufferHandle;

<<<<<<< HEAD
            m_ViewID      = viewID;
            m_Textures    = new RenderTexture[k_NumBuffers];
            m_Identifiers = new RenderTargetIdentifier[k_NumBuffers];
            m_Params      = new Parameters[k_NumFrames];

            for (int i = 0; i < k_NumBuffers; i++)
            {
                m_Textures[i]                   = new RenderTexture(w, h, 0, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear);
                m_Textures[i].hideFlags         = HideFlags.HideAndDontSave;
                m_Textures[i].filterMode        = FilterMode.Trilinear;   // Custom
                m_Textures[i].dimension         = TextureDimension.Tex3D; // TODO: request the thick 3D tiling layout
                m_Textures[i].volumeDepth       = d;
                m_Textures[i].enableRandomWrite = true;
                m_Textures[i].name              = CoreUtils.GetRenderTargetAutoName(w, h, RenderTextureFormat.ARGBHalf, String.Format("VBuffer{0}", i));
                m_Textures[i].Create();

                // TODO: clear the texture. Clearing 3D textures does not appear to work right now.

                m_Identifiers[i] = new RenderTargetIdentifier(m_Textures[i]);
            }

            // Start with the same parameters for both frames. Then incrementally update them.
            Parameters parameters = new Parameters(w, h, d, controlParams);
            m_Params[0] = parameters;
            m_Params[1] = parameters;
        }

        public void Destroy()
        {
            if (m_Textures != null)
            {
                for (int i = 0; i < k_NumBuffers; i++)
                {
                    if (m_Textures[i] != null)
                    {
                        m_Textures[i].Release();
                    }
                }
            }

            m_ViewID      =   -1;
            m_Textures    = null;
            m_Identifiers = null;
            m_Params      = null;
        }
    } // class VBuffer
=======
    public void Build(HDRenderPipelineAsset asset)
    {
        if (preset == VolumetricLightingPreset.Off) return;

        m_VolumeVoxelizationCS = asset.renderPipelineResources.volumeVoxelizationCS;
        m_VolumetricLightingCS = asset.renderPipelineResources.volumetricLightingCS;

        CreateBuffers();
    }

    // RTHandleSystem API expects a function which computes the resolution. We define it here.
    Vector2Int ComputeVBuffeResolutionXY(Vector2Int screenSize)
    {
        Vector3Int resolution = ComputeVBufferResolution(preset, screenSize.x, screenSize.y);

        // Since the buffers owned by the VolumetricLightingSystem may have different lifetimes compared
        // to those owned by the HDCamera, we need to make sure that the buffer resolution is the same
        // (in order to share the UV scale and the UV limit).
        if (m_LightingBufferHandle != null)
        {
            resolution.x = Math.Max(resolution.x, m_LightingBufferHandle.rt.width);
            resolution.y = Math.Max(resolution.y, m_LightingBufferHandle.rt.height);
        }

        return new Vector2Int(resolution.x, resolution.y);
    }

    // BufferedRTHandleSystem API expects an allocator function. We define it here.
    RTHandleSystem.RTHandle HistoryBufferAllocatorFunction(string viewName, int frameIndex, RTHandleSystem rtHandleSystem)
    {
        frameIndex &= 1;

        int d = ComputeVBufferSliceCount(preset);

        return rtHandleSystem.Alloc(scaleFunc:         ComputeVBuffeResolutionXY,
                                    slices:            d,
                                    dimension:         TextureDimension.Tex3D,
                                    colorFormat:       RenderTextureFormat.ARGBHalf,
                                    sRGB:              false,
                                    enableRandomWrite: true,
                                    enableMSAA:        false,
                                    /* useDynamicScale: true, // <- TODO */
                                    name: string.Format("{0}_VBufferHistory{1}", viewName, frameIndex)
        );
    }

    void CreateBuffers()
    {
        Debug.Assert(m_VolumetricLightingCS != null);
>>>>>>> master

        m_VisibleVolumeBounds       = new List<OrientedBBox>();
        m_VisibleVolumeData         = new List<DensityVolumeData>();
        s_VisibleVolumeBoundsBuffer = new ComputeBuffer(k_MaxVisibleVolumeCount, Marshal.SizeOf(typeof(OrientedBBox)));
        s_VisibleVolumeDataBuffer   = new ComputeBuffer(k_MaxVisibleVolumeCount, Marshal.SizeOf(typeof(DensityVolumeData)));

        int d = ComputeVBufferSliceCount(preset);

        m_DensityBufferHandle = RTHandles.Alloc(scaleFunc:         ComputeVBuffeResolutionXY,
                                                slices:            d,
                                                dimension:         TextureDimension.Tex3D,
                                                colorFormat:       RenderTextureFormat.ARGBHalf,
                                                sRGB:              false,
                                                enableRandomWrite: true,
                                                enableMSAA:        false,
                                                /* useDynamicScale: true, // <- TODO */
                                                name:              "VBufferDensity");

        m_LightingBufferHandle = RTHandles.Alloc(scaleFunc:         ComputeVBuffeResolutionXY,
                                                 slices:            d,
                                                 dimension:         TextureDimension.Tex3D,
                                                 colorFormat:       RenderTextureFormat.ARGBHalf,
                                                 sRGB:              false,
                                                 enableRandomWrite: true,
                                                 enableMSAA:        false,
                                                 /* useDynamicScale: true, // <- TODO */
                                                 name:              "VBufferIntegral");
    }

<<<<<<< HEAD
    static ComputeShader    m_VolumeVoxelizationCS      = null;
    static ComputeShader    m_VolumetricLightingCS      = null;

    List<VBuffer>           m_VBuffers                  = null;
    List<OrientedBBox>      m_VisibleVolumeBounds       = null;
    List<DensityVolumeData> m_VisibleVolumeData         = null;
    public const int        k_MaxVisibleVolumeCount     = 512;

    // Static keyword is required here else we get a "DestroyBuffer can only be called from the main thread"
    static ComputeBuffer    s_VisibleVolumeBoundsBuffer = null;
    static ComputeBuffer    s_VisibleVolumeDataBuffer   = null;
=======
    // For the initial allocation, no suballocation happens (the texture is full size).
    VBufferParameters ComputeVBufferParameters(HDCamera camera, bool isInitialAllocation)
    {
        Vector3Int viewportResolution = ComputeVBufferResolution(preset, camera.camera.pixelWidth, camera.camera.pixelHeight);
        Vector3Int bufferResolution; // Could be higher due to sub-allocation (resource aliasing) in the RTHandle system

        if (isInitialAllocation)
        {
            bufferResolution = viewportResolution;
        }
        else
        {
            // All V-Buffers of the current frame should have the same size (you have to double-buffer history, of course).
            bufferResolution = new Vector3Int(m_LightingBufferHandle.rt.width, m_LightingBufferHandle.rt.height, m_LightingBufferHandle.rt.volumeDepth);
        }

        var controller = VolumeManager.instance.stack.GetComponent<VolumetricLightingController>();

        // We must not allow the V-Buffer to extend outside of the camera's frustum.
        float n = camera.camera.nearClipPlane;
        float f = camera.camera.farClipPlane;
>>>>>>> master

        Vector2 vBufferDepthRange = controller.depthRange.value;
        vBufferDepthRange.y = Mathf.Clamp(vBufferDepthRange.y, n, f);                   // far
        vBufferDepthRange.x = Mathf.Clamp(vBufferDepthRange.x, n, vBufferDepthRange.y); // near
        float vBufferDepthDistributionUniformity = controller.depthDistributionUniformity.value;

        return new VBufferParameters(viewportResolution, bufferResolution, vBufferDepthRange, vBufferDepthDistributionUniformity);
    }

    public void InitializePerCameraData(HDCamera camera)
    {
        if (preset == VolumetricLightingPreset.Off) return;

        // Start with the same parameters for both frames. Then update them one by one every frame.
        var parameters          = ComputeVBufferParameters(camera, true);
        camera.vBufferParams    = new VBufferParameters[2];
        camera.vBufferParams[0] = parameters;
        camera.vBufferParams[1] = parameters;

        if (camera.camera.cameraType == CameraType.Game ||
            camera.camera.cameraType == CameraType.SceneView)
        {
            // We don't need reprojection for other view types, such as reflection and preview.
            camera.AllocHistoryFrameRT((int)HDCameraFrameHistoryType.VolumetricLighting, HistoryBufferAllocatorFunction);
        }
    }

<<<<<<< HEAD
    public void ResizeVBufferAndUpdateProperties(HDCamera camera, uint frameIndex)
=======
    // This function relies on being called once per camera per frame.
    // The results are undefined otherwise.
    public void UpdatePerCameraData(HDCamera camera)
>>>>>>> master
    {
        if (preset == VolumetricLightingPreset.Off) return;
        var visualEnvironment = VolumeManager.instance.stack.GetComponent<VisualEnvironment>();
        if (visualEnvironment == null || visualEnvironment.fogType != FogType.Volumetric) return;

        var controller = camera.camera.GetComponent<VolumetricLightingController>();

        if (camera.camera.cameraType == CameraType.SceneView)
        {
            // HACK: since it's not possible to add a component to a scene camera,
            // we take one from the "main" camera (if present).
            Camera mainCamera = Camera.main;

            if (mainCamera != null)
            {
                controller = mainCamera.GetComponent<VolumetricLightingController>();
            }
        }

        if (controller == null) return;

<<<<<<< HEAD
        int  screenWidth  = (int)camera.screenSize.x;
        int  screenHeight = (int)camera.screenSize.y;
        long viewID       = camera.GetViewID();

        Debug.Assert(viewID > 0);
=======
        var parameters = ComputeVBufferParameters(camera, false);
>>>>>>> master

        // Double-buffer. I assume the cost of copying is negligible (don't want to use the frame index).
        camera.vBufferParams[1] = camera.vBufferParams[0];
        camera.vBufferParams[0] = parameters;

        // Note: resizing of history buffer is automatic (handled by the BufferedRTHandleSystem).
    }

<<<<<<< HEAD
        if (vBuffer != null)
        {
            VBuffer.Parameters frameParams = vBuffer.GetParameters(frameIndex);

            // Found, check resolution.
            if (w == frameParams.resolution.x &&
                h == frameParams.resolution.y &&
                d == frameParams.sliceCount.x)
            {
                // The resolution matches.
                // Depth parameters may have changed, so update those.
                frameParams.Update(controller.parameters);
                vBuffer.SetParameters(frameParams, frameIndex);

                return;
            }
        }
        else
        {
            // Not found - grow the array.
            vBuffer = new VBuffer();
            m_VBuffers.Add(vBuffer);
        }

        vBuffer.Create(viewID, w, h, d, controller.parameters);
=======
    void DestroyBuffers()
    {
        RTHandles.Release(m_DensityBufferHandle);
        RTHandles.Release(m_LightingBufferHandle);

        CoreUtils.SafeRelease(s_VisibleVolumeBoundsBuffer);
        CoreUtils.SafeRelease(s_VisibleVolumeDataBuffer);

        m_VisibleVolumeBounds = null;
        m_VisibleVolumeData   = null;
>>>>>>> master
    }

    public void Cleanup()
    {
<<<<<<< HEAD
        Debug.Assert(viewID > 0);
=======
        if (preset == VolumetricLightingPreset.Off) return;
>>>>>>> master

        DestroyBuffers();

        m_VolumeVoxelizationCS = null;
        m_VolumetricLightingCS = null;
    }

    static int ComputeVBufferTileSize(VolumetricLightingPreset preset)
    {
        switch (preset)
        {
            case VolumetricLightingPreset.Normal:
                return 8;
            case VolumetricLightingPreset.Ultra:
                return 4;
            case VolumetricLightingPreset.Off:
                return 0;
            default:
                Debug.Assert(false, "Encountered an unexpected VolumetricLightingPreset.");
                return 0;
        }
    }

    static int ComputeVBufferSliceCount(VolumetricLightingPreset preset)
    {
        switch (preset)
        {
            case VolumetricLightingPreset.Normal:
                return 64;
            case VolumetricLightingPreset.Ultra:
                return 128;
            case VolumetricLightingPreset.Off:
                return 0;
            default:
                Debug.Assert(false, "Encountered an unexpected VolumetricLightingPreset.");
                return 0;
        }
    }

<<<<<<< HEAD
    // Since a single voxel corresponds to a tile (e.g. 8x8) of pixels,
    // the VBuffer can potentially extend past the boundaries of the viewport.
    // The function returns the fraction of the {width, height} of the VBuffer visible on screen.
    // Note: for performance reasons, the scale is unused (implicitly 1). The error is typically under 1%.
    static Vector2 ComputeVBufferResolutionAndScale(VolumetricLightingPreset preset,
                                                    int screenWidth, int screenHeight,
                                                    ref int w, ref int h, ref int d)
=======
    static Vector3Int ComputeVBufferResolution(VolumetricLightingPreset preset,
                                               int screenWidth, int screenHeight)
>>>>>>> master
    {
        int t = ComputeVBufferTileSize(preset);

        // ceil(ScreenSize / TileSize).
        int w = (screenWidth  + (t - 1)) / t;
        int h = (screenHeight + (t - 1)) / t;
        int d = ComputeVBufferSliceCount(preset);

        return new Vector3Int(w, h, d);
    }

    // See EncodeLogarithmicDepthGeneralized().
    static Vector4 ComputeLogarithmicDepthEncodingParams(float nearPlane, float farPlane, float c)
    {
        Vector4 depthParams = new Vector4();

        float n = nearPlane;
        float f = farPlane;

        c = Mathf.Max(c, 0.001f); // Avoid NaNs

        depthParams.y = 1.0f / Mathf.Log(c * (f - n) + 1, 2);
        depthParams.x = Mathf.Log(c, 2) * depthParams.y;
        depthParams.z = n - 1.0f / c; // Same
        depthParams.w = 0.0f;

        return depthParams;
    }

    // See DecodeLogarithmicDepthGeneralized().
    static Vector4 ComputeLogarithmicDepthDecodingParams(float nearPlane, float farPlane, float c)
    {
        Vector4 depthParams = new Vector4();

        float n = nearPlane;
        float f = farPlane;

        c = Mathf.Max(c, 0.001f); // Avoid NaNs

        depthParams.x = 1.0f / c;
        depthParams.y = Mathf.Log(c * (f - n) + 1, 2);
        depthParams.z = n - 1.0f / c; // Same
        depthParams.w = 0.0f;

        return depthParams;
    }

    void SetPreconvolvedAmbientLightProbe(CommandBuffer cmd, float anisotropy)
    {
        SphericalHarmonicsL2 probeSH = SphericalHarmonicMath.UndoCosineRescaling(RenderSettings.ambientProbe);
        ZonalHarmonicsL2     phaseZH = ZonalHarmonicsL2.GetCornetteShanksPhaseFunction(anisotropy);
        SphericalHarmonicsL2 finalSH = SphericalHarmonicMath.PremultiplyCoefficients(SphericalHarmonicMath.Convolve(probeSH, phaseZH));

        cmd.SetGlobalVectorArray(HDShaderIDs._AmbientProbeCoeffs, SphericalHarmonicMath.PackCoefficients(finalSH));
    }

    float CornetteShanksPhasePartConstant(float anisotropy)
    {
        float g = anisotropy;

        return (1.0f / (4.0f * Mathf.PI)) * 1.5f * (1.0f - g * g) / (2.0f + g * g);
    }

    public void PushGlobalParams(HDCamera camera, CommandBuffer cmd, uint frameIndex)
    {
        if (preset == VolumetricLightingPreset.Off) return;

        var visualEnvironment = VolumeManager.instance.stack.GetComponent<VisualEnvironment>();
<<<<<<< HEAD
        if (visualEnvironment.fogType != FogType.Volumetric) return;

        // VisualEnvironment sets global fog parameters: _GlobalAsymmetry, _GlobalScattering, _GlobalExtinction.

        VBuffer vBuffer = FindVBuffer(camera.GetViewID());
        if (vBuffer == null)
=======

        // VisualEnvironment sets global fog parameters: _GlobalAnisotropy, _GlobalScattering, _GlobalExtinction.

        if (visualEnvironment.fogType != FogType.Volumetric)
>>>>>>> master
        {
            // Set the neutral black texture.
            cmd.SetGlobalTexture(HDShaderIDs._VBufferLighting, CoreUtils.blackVolumeTexture);
            return;
        }

        // Get the interpolated anisotropy value.
        var fog = VolumeManager.instance.stack.GetComponent<VolumetricFog>();

        SetPreconvolvedAmbientLightProbe(cmd, fog.anisotropy);

<<<<<<< HEAD
        var currFrameParams = vBuffer.GetParameters(frameIndex);
        var prevFrameParams = vBuffer.GetParameters(frameIndex - 1);

        cmd.SetGlobalVector( HDShaderIDs._VBufferResolution,              currFrameParams.resolution);
        cmd.SetGlobalVector( HDShaderIDs._VBufferSliceCount,              currFrameParams.sliceCount);
        cmd.SetGlobalVector( HDShaderIDs._VBufferDepthEncodingParams,     currFrameParams.depthEncodingParams);
        cmd.SetGlobalVector( HDShaderIDs._VBufferDepthDecodingParams,     currFrameParams.depthDecodingParams);
        cmd.SetGlobalVector( HDShaderIDs._VBufferPrevResolution,          prevFrameParams.resolution);
        cmd.SetGlobalVector( HDShaderIDs._VBufferPrevSliceCount,          prevFrameParams.sliceCount);
        cmd.SetGlobalVector( HDShaderIDs._VBufferPrevDepthEncodingParams, prevFrameParams.depthEncodingParams);
        cmd.SetGlobalVector( HDShaderIDs._VBufferPrevDepthDecodingParams, prevFrameParams.depthDecodingParams);
        cmd.SetGlobalTexture(HDShaderIDs._VBufferLighting,                vBuffer.GetLightingIntegralBuffer());
=======
        var currFrameParams = camera.vBufferParams[0];
        var prevFrameParams = camera.vBufferParams[1];

        cmd.SetGlobalVector( HDShaderIDs._VBufferResolution,              currFrameParams.resolution);
        cmd.SetGlobalVector( HDShaderIDs._VBufferSliceCount,              currFrameParams.sliceCount);
        cmd.SetGlobalVector( HDShaderIDs._VBufferUvScaleAndLimit,         currFrameParams.uvScaleAndLimit);
        cmd.SetGlobalVector( HDShaderIDs._VBufferDepthEncodingParams,     currFrameParams.depthEncodingParams);
        cmd.SetGlobalVector( HDShaderIDs._VBufferDepthDecodingParams,     currFrameParams.depthDecodingParams);

        cmd.SetGlobalVector( HDShaderIDs._VBufferPrevResolution,          prevFrameParams.resolution);
        cmd.SetGlobalVector( HDShaderIDs._VBufferPrevSliceCount,          prevFrameParams.sliceCount);
        cmd.SetGlobalVector( HDShaderIDs._VBufferPrevUvScaleAndLimit,     prevFrameParams.uvScaleAndLimit);
        cmd.SetGlobalVector( HDShaderIDs._VBufferPrevDepthEncodingParams, prevFrameParams.depthEncodingParams);
        cmd.SetGlobalVector( HDShaderIDs._VBufferPrevDepthDecodingParams, prevFrameParams.depthDecodingParams);

        cmd.SetGlobalTexture(HDShaderIDs._VBufferLighting,                m_LightingBufferHandle);
>>>>>>> master
    }

    public DensityVolumeList PrepareVisibleDensityVolumeList(HDCamera camera, CommandBuffer cmd)
    {
        DensityVolumeList densityVolumes = new DensityVolumeList();

        if (preset == VolumetricLightingPreset.Off) return densityVolumes;

        var visualEnvironment = VolumeManager.instance.stack.GetComponent<VisualEnvironment>();
        if (visualEnvironment.fogType != FogType.Volumetric) return densityVolumes;

        VBuffer vBuffer = FindVBuffer(camera.GetViewID());
        if (vBuffer == null) return densityVolumes;

        using (new ProfilingSample(cmd, "Prepare Visible Density Volume List"))
        {
            Vector3 camPosition = camera.camera.transform.position;
            Vector3 camOffset   = Vector3.zero; // World-origin-relative

            if (ShaderConfig.s_CameraRelativeRendering != 0)
            {
                camOffset = camPosition; // Camera-relative
            }

            m_VisibleVolumeBounds.Clear();
            m_VisibleVolumeData.Clear();

            // Collect all visible finite volume data, and upload it to the GPU.
            DensityVolume[] volumes = DensityVolumeManager.manager.PrepareDensityVolumeData(cmd);

            for (int i = 0; i < Math.Min(volumes.Length, k_MaxVisibleVolumeCount); i++)
            {
                DensityVolume volume = volumes[i];

                // TODO: cache these?
                var obb = OrientedBBox.Create(volume.transform);

                // Handle camera-relative rendering.
                obb.center -= camOffset;

                // Frustum cull on the CPU for now. TODO: do it on the GPU.
                // TODO: account for custom near and far planes of the V-Buffer's frustum.
                // It's typically much shorter (along the Z axis) than the camera's frustum.
                if (GeometryUtils.Overlap(obb, camera.frustum, 6, 8))
                {
                    // TODO: cache these?
                    var data = volume.parameters.GetData();

                    m_VisibleVolumeBounds.Add(obb);
                    m_VisibleVolumeData.Add(data);
                }
            }

            s_VisibleVolumeBoundsBuffer.SetData(m_VisibleVolumeBounds);
            s_VisibleVolumeDataBuffer.SetData(m_VisibleVolumeData);

            // Fill the struct with pointers in order to share the data with the light loop.
            densityVolumes.bounds  = m_VisibleVolumeBounds;
            densityVolumes.density = m_VisibleVolumeData;

            return densityVolumes;
        }
    }

    public void VolumeVoxelizationPass(DensityVolumeList densityVolumes, HDCamera camera, CommandBuffer cmd, FrameSettings settings, uint frameIndex)
    {
        if (preset == VolumetricLightingPreset.Off) return;

        var visualEnvironment = VolumeManager.instance.stack.GetComponent<VisualEnvironment>();
        if (visualEnvironment.fogType != FogType.Volumetric) return;

<<<<<<< HEAD
        VBuffer vBuffer = FindVBuffer(camera.GetViewID());
        if (vBuffer == null) return;

=======
>>>>>>> master
        using (new ProfilingSample(cmd, "Volume Voxelization"))
        {
            int numVisibleVolumes = m_VisibleVolumeBounds.Count;

            if (numVisibleVolumes == 0)
            {
                // Clear the render target instead of running the shader.
                // Note: the clear must take the global fog into account!
                // CoreUtils.SetRenderTarget(cmd, vBuffer.GetDensityBuffer(), ClearFlag.Color, CoreUtils.clearColorAllBlack);
                // return;

                // Clearing 3D textures does not seem to work!
                // Use the workaround by running the full shader with 0 density
            }

            bool enableClustered = settings.lightLoopSettings.enableTileAndCluster;

            int kernel = m_VolumeVoxelizationCS.FindKernel(enableClustered ? "VolumeVoxelizationClustered"
                                                                           : "VolumeVoxelizationBruteforce");

<<<<<<< HEAD
            var     frameParams = vBuffer.GetParameters(frameIndex);
=======
            var     frameParams = camera.vBufferParams[0];
>>>>>>> master
            Vector4 resolution  = frameParams.resolution;
            float   vFoV        = camera.camera.fieldOfView * Mathf.Deg2Rad;

            // Compose the matrix which allows us to compute the world space view direction.
            Matrix4x4 transform   = HDUtils.ComputePixelCoordToWorldSpaceViewDirectionMatrix(vFoV, resolution, camera.viewMatrix, false);
<<<<<<< HEAD
=======

            Texture3D volumeAtlas = DensityVolumeManager.manager.volumeAtlas.volumeAtlas;
            Vector2 volumeAtlasDimensions = new Vector2(0.0f, 0.0f);

            if (volumeAtlas != null)
            {
                volumeAtlasDimensions.x = volumeAtlas.width / volumeAtlas.depth; // 1 / number of textures
                volumeAtlasDimensions.y = 1.0f / volumeAtlas.width;
            }
>>>>>>> master

            cmd.SetComputeTextureParam(m_VolumeVoxelizationCS, kernel, HDShaderIDs._VBufferDensity, m_DensityBufferHandle);
            cmd.SetComputeBufferParam( m_VolumeVoxelizationCS, kernel, HDShaderIDs._VolumeBounds,   s_VisibleVolumeBoundsBuffer);
            cmd.SetComputeBufferParam( m_VolumeVoxelizationCS, kernel, HDShaderIDs._VolumeData,     s_VisibleVolumeDataBuffer);
            cmd.SetComputeTextureParam(m_VolumeVoxelizationCS, kernel, HDShaderIDs._VolumeMaskAtlas, volumeAtlas);

            // TODO: set the constant buffer data only once.
            cmd.SetComputeMatrixParam( m_VolumeVoxelizationCS, HDShaderIDs._VBufferCoordToViewDirWS,  transform);
            cmd.SetComputeIntParam(    m_VolumeVoxelizationCS, HDShaderIDs._NumVisibleDensityVolumes, numVisibleVolumes);
            cmd.SetComputeVectorParam(m_VolumeVoxelizationCS, HDShaderIDs._VolumeMaskDimensions, volumeAtlasDimensions);

            int w = (int)resolution.x;
            int h = (int)resolution.y;

            int w = (int)resolution.x;
            int h = (int)resolution.y;

            // The shader defines GROUP_SIZE_1D = 8.
            cmd.DispatchCompute(m_VolumeVoxelizationCS, kernel, (w + 7) / 8, (h + 7) / 8, 1);
        }
    }

    // Ref: https://en.wikipedia.org/wiki/Close-packing_of_equal_spheres
    // The returned {x, y} coordinates (and all spheres) are all within the (-0.5, 0.5)^2 range.
    // The pattern has been rotated by 15 degrees to maximize the resolution along X and Y:
    // https://www.desmos.com/calculator/kcpfvltz7c
    static Vector2[] GetHexagonalClosePackedSpheres7()
    {
        Vector2[] coords = new Vector2[7];

        float r = 0.17054068870105443882f;
        float d = 2 * r;
        float s = r * Mathf.Sqrt(3);

        // Try to keep the weighted average as close to the center (0.5) as possible.
        //  (7)(5)    ( )( )    ( )( )    ( )( )    ( )( )    ( )(o)    ( )(x)    (o)(x)    (x)(x)
        // (2)(1)(3) ( )(o)( ) (o)(x)( ) (x)(x)(o) (x)(x)(x) (x)(x)(x) (x)(x)(x) (x)(x)(x) (x)(x)(x)
        //  (4)(6)    ( )( )    ( )( )    ( )( )    (o)( )    (x)( )    (x)(o)    (x)(x)    (x)(x)
        coords[0] = new Vector2( 0,  0);
        coords[1] = new Vector2(-d,  0);
        coords[2] = new Vector2( d,  0);
        coords[3] = new Vector2(-r, -s);
        coords[4] = new Vector2( r,  s);
        coords[5] = new Vector2( r, -s);
        coords[6] = new Vector2(-r,  s);

        // Rotate the sampling pattern by 15 degrees.
        const float cos15 = 0.96592582628906828675f;
        const float sin15 = 0.25881904510252076235f;

        for (int i = 0; i < 7; i++)
        {
            Vector2 coord = coords[i];

            coords[i].x = coord.x * cos15 - coord.y * sin15;
            coords[i].y = coord.x * sin15 + coord.y * cos15;
        }

        return coords;
    }

    public void VolumetricLightingPass(HDCamera camera, CommandBuffer cmd, FrameSettings settings, uint frameIndex)
    {
        if (preset == VolumetricLightingPreset.Off) return;

        var visualEnvironment = VolumeManager.instance.stack.GetComponent<VisualEnvironment>();
        if (visualEnvironment.fogType != FogType.Volumetric) return;

<<<<<<< HEAD
        VBuffer vBuffer = FindVBuffer(camera.GetViewID());
        if (vBuffer == null) return;

=======
>>>>>>> master
        using (new ProfilingSample(cmd, "Volumetric Lighting"))
        {
            // Only available in the Play Mode because all the frame counters in the Edit Mode are broken.
            bool enableClustered    = settings.lightLoopSettings.enableTileAndCluster;
            bool enableReprojection = Application.isPlaying && camera.camera.cameraType == CameraType.Game;

            int kernel;

            if (enableReprojection)
            {
                kernel = m_VolumetricLightingCS.FindKernel(enableClustered ? "VolumetricLightingClusteredReproj"
                                                                           : "VolumetricLightingBruteforceReproj");
            }
            else
            {
                kernel = m_VolumetricLightingCS.FindKernel(enableClustered ? "VolumetricLightingClustered"
                                                                           : "VolumetricLightingBruteforce");
            }

<<<<<<< HEAD
            var       frameParams = vBuffer.GetParameters(frameIndex);
=======
            var       frameParams = camera.vBufferParams[0];
>>>>>>> master
            Vector4   resolution  = frameParams.resolution;
            float     vFoV        = camera.camera.fieldOfView * Mathf.Deg2Rad;
            // Compose the matrix which allows us to compute the world space view direction.
            Matrix4x4 transform   = HDUtils.ComputePixelCoordToWorldSpaceViewDirectionMatrix(vFoV, resolution, camera.viewMatrix, false);

            Vector2[] xySeq = GetHexagonalClosePackedSpheres7();

            // This is a sequence of 7 equidistant numbers from 1/14 to 13/14.
            // Each of them is the centroid of the interval of length 2/14.
            // They've been rearranged in a sequence of pairs {small, large}, s.t. (small + large) = 1.
            // That way, the running average position is close to 0.5.
            // | 6 | 2 | 4 | 1 | 5 | 3 | 7 |
            // |   |   |   | o |   |   |   |
            // |   | o |   | x |   |   |   |
            // |   | x |   | x |   | o |   |
            // |   | x | o | x |   | x |   |
            // |   | x | x | x | o | x |   |
            // | o | x | x | x | x | x |   |
            // | x | x | x | x | x | x | o |
            // | x | x | x | x | x | x | x |
            float[] zSeq = {7.0f/14.0f, 3.0f/14.0f, 11.0f/14.0f, 5.0f/14.0f, 9.0f/14.0f, 1.0f/14.0f, 13.0f/14.0f};

            int sampleIndex = (int)frameIndex % 7;
<<<<<<< HEAD

            // TODO: should we somehow reorder offsets in Z based on the offset in XY? S.t. the samples more evenly cover the domain.
            // Currently, we assume that they are completely uncorrelated, but maybe we should correlate them somehow.
            Vector4 offset = new Vector4(xySeq[sampleIndex].x, xySeq[sampleIndex].y, zSeq[sampleIndex], frameIndex);
=======
>>>>>>> master

            // TODO: should we somehow reorder offsets in Z based on the offset in XY? S.t. the samples more evenly cover the domain.
            // Currently, we assume that they are completely uncorrelated, but maybe we should correlate them somehow.
            Vector4 offset = new Vector4(xySeq[sampleIndex].x, xySeq[sampleIndex].y, zSeq[sampleIndex], frameIndex);

            // Get the interpolated anisotropy value.
            var fog = VolumeManager.instance.stack.GetComponent<VolumetricFog>();

            // TODO: set 'm_VolumetricLightingPreset'.
            // TODO: set the constant buffer data only once.
            cmd.SetComputeMatrixParam( m_VolumetricLightingCS,         HDShaderIDs._VBufferCoordToViewDirWS, transform);
            cmd.SetComputeVectorParam( m_VolumetricLightingCS,         HDShaderIDs._VBufferSampleOffset,     offset);
            cmd.SetComputeFloatParam(  m_VolumetricLightingCS,         HDShaderIDs._CornetteShanksConstant,  CornetteShanksPhasePartConstant(fog.anisotropy));
            cmd.SetComputeTextureParam(m_VolumetricLightingCS, kernel, HDShaderIDs._VBufferDensity,          m_DensityBufferHandle);  // Read
            cmd.SetComputeTextureParam(m_VolumetricLightingCS, kernel, HDShaderIDs._VBufferLightingIntegral, m_LightingBufferHandle); // Write
            if (enableReprojection)
            {
<<<<<<< HEAD
            cmd.SetComputeTextureParam(m_VolumetricLightingCS, kernel, HDShaderIDs._VBufferLightingFeedback, vBuffer.GetLightingFeedbackBuffer(frameIndex)); // Write
            cmd.SetComputeTextureParam(m_VolumetricLightingCS, kernel, HDShaderIDs._VBufferLightingHistory,  vBuffer.GetLightingHistoryBuffer(frameIndex));  // Read
=======
            cmd.SetComputeTextureParam(m_VolumetricLightingCS, kernel, HDShaderIDs._VBufferLightingHistory,  camera.GetPreviousFrameRT((int)HDCameraFrameHistoryType.VolumetricLighting)); // Read
            cmd.SetComputeTextureParam(m_VolumetricLightingCS, kernel, HDShaderIDs._VBufferLightingFeedback, camera.GetCurrentFrameRT((int)HDCameraFrameHistoryType.VolumetricLighting));  // Write
>>>>>>> master
            }

            int w = (int)resolution.x;
            int h = (int)resolution.y;

            // The shader defines GROUP_SIZE_1D = 8.
            cmd.DispatchCompute(m_VolumetricLightingCS, kernel, (w + 7) / 8, (h + 7) / 8, 1);
        }
    }
} // class VolumetricLightingModule
} // namespace UnityEngine.Experimental.Rendering.HDPipeline
