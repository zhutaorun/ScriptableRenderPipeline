using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.TestTools.Graphics;
using UnityEngine.SceneManagement;
using UnityEngine.Events;
using System.IO;
using TestParameters = UnityEngine.TestTools.Graphics.TestParameters;

public class HDRP_GraphicTestRunner
{
    [PrebuildSetup("SetupGraphicsTestCases")]
    [UseGraphicsTestCases( TestParameters.ScenePath )]
    public IEnumerator Run(string scenePath)
    {
        GraphicsTestCase testCase = UseGraphicsTestCasesAttribute.GetCaseFromScenePath(scenePath);

        SceneManager.LoadScene(testCase.ScenePath);

        // Arbitrary wait for 5 frames for the scene to load, and other stuff to happen (like Realtime GI to appear ...)
        for (int i=0 ; i<5 ; ++i)
            yield return null;

        // Load the test settings
        var settings = GameObject.FindObjectOfType<HDRP_TestSettings>();

        var camera = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<Camera>();
        if (camera == null) camera = GameObject.FindObjectOfType<Camera>();
        if (camera == null)
        {
            Assert.Fail("Missing camera for graphic tests.");
        }

        Time.captureFramerate = settings.captureFramerate;

        if (settings.doBeforeTest != null)
        {
            settings.doBeforeTest.Invoke();

            // Wait again one frame, to be sure.
            yield return null;
        }

        for (int i=0 ; i<settings.waitFrames ; ++i)
            yield return null;

        ImageAssert.AreEqual(testCase.ReferenceImage, camera, (settings != null)?settings.ImageComparisonSettings:null);
    }

#if UNITY_EDITOR

    [TearDown]
    public void DumpImagesInEditor()
    {
        UnityEditor.TestTools.Graphics.ResultsUtility.ExtractImagesFromTestProperties(TestContext.CurrentContext.Test);
    }
#endif

}
