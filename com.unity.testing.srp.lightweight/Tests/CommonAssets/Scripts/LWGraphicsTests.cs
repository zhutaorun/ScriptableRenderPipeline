using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.TestTools.Graphics;
using UnityEngine.SceneManagement;

public class LWGraphicsTests
{

    public const string lwPackagePath = "Packages/com.unity.testing.srp.lightweight/Tests/ReferenceImages";

    [UnityTest, Category("LightWeightRP")]
    [PrebuildSetup("SetupGraphicsTestCases")]
    [UseGraphicsTestCases(lwPackagePath)]
    public IEnumerator Run(GraphicsTestCase testCase)
    {
        SceneManager.LoadScene(testCase.ScenePath);

        // Always wait one frame for scene load
        yield return null;

        var camera = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<Camera>();
        var settings = Object.FindObjectOfType<LWGraphicsTestSettings>();
        Assert.IsNotNull(settings, "Invalid test scene, could not find an object with a LWGraphicsTestSettings component");

        for (int i = 0; i < settings.WaitFrames; i++)
            yield return null;


        if (!(settings is SGLWGraphicsTestSettings))
        {
            // Standard LWRP Test
            ImageAssert.AreEqual(testCase.ReferenceImage, camera, settings.ImageComparisonSettings);
        }
        else
        {
            GameObject sgRoot = GameObject.Find("sg_root");
            GameObject lwRoot = GameObject.Find("lw_root");

            Assert.IsNotNull(sgRoot, "Could not find the ShaderGraph root object");
            Assert.IsNotNull(lwRoot, "Could not find the Lightweight root object");

            GameObject lwCamOrigin = FindChild(lwRoot, "camera_origin");
            GameObject lwCamLookat = FindChild(lwRoot, "camera_lookat");
            GameObject sgCamOrigin = FindChild(sgRoot, "camera_origin");
            GameObject sgCamLookat = FindChild(sgRoot, "camera_lookat");

            // Do ShaderGraph comparison tests with LWRP
            Assert.IsNotNull(lwCamOrigin, "Lightweight camera origin transform in NULL");
            Assert.IsNotNull(lwCamLookat, "Lightweight camera lookat transform in NULL");
            Assert.IsNotNull(sgCamOrigin, "ShaderGraph camera origin transform in NULL");
            Assert.IsNotNull(sgCamLookat, "ShaderGraph camera lookat transform in NULL");

            // First test: ShaderGraph
            camera.transform.position = sgCamOrigin.transform.position;
            camera.transform.LookAt(sgCamLookat.transform);
            
            yield return null;
            yield return null;

            try
            {
                ImageAssert.AreEqual(testCase.ReferenceImage, camera, (settings != null) ? settings.ImageComparisonSettings : null);
            }
            catch (AssertionException)
            {
                Assert.Fail("Shader Graph Objects failed."); // Informs which ImageAssert failed.
            }

            // Second test: LWRP
            camera.transform.position = lwCamOrigin.transform.position;
            camera.transform.LookAt(lwCamLookat.transform);

            yield return null;
            yield return null;

            try
            {
                ImageAssert.AreEqual(testCase.ReferenceImage, camera, (settings != null) ? settings.ImageComparisonSettings : null);
            }
            catch (AssertionException)
            {
                Assert.Fail("Shader Graph Objects failed."); // Informs which ImageAssert failed.
            }
            
            Debug.Log("blah");
        }
    }

    GameObject FindChild(GameObject parent, string name)
    {
        for(int i = 0; i < parent.transform.childCount; ++i)
        {
            Transform child = parent.transform.GetChild(i);

            if(child.name == name)
                return child.gameObject;
        }

        return null;
    }

#if UNITY_EDITOR
    [TearDown]
    public void DumpImagesInEditor()
    {
        UnityEditor.TestTools.Graphics.ResultsUtility.ExtractImagesFromTestProperties(TestContext.CurrentContext.Test);
    }
#endif
}
