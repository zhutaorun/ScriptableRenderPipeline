using System;
using System.Collections.Generic;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;
using NUnit.Framework.Internal.Builders;
using UnityEngine.Rendering;
using Attribute = System.Attribute;

namespace UnityEngine.TestTools.Graphics
{
    /// <summary>
    /// Marks a test which takes <c>GraphicsTestCase</c> instances as wanting to have them generated automatically by
    /// the scene/reference-image management feature in the framework.
    /// </summary>
    public class UseGraphicsTestCasesAttribute : UnityEngine.TestTools.UnityTestAttribute, ITestBuilder
    {
        string m_ReferenceImagePath = string.Empty;

        NUnitTestCaseBuilder _builder = new NUnitTestCaseBuilder();

        bool m_OutputAsString = false;

        public UseGraphicsTestCasesAttribute(bool outputAsString = false)
        {
            m_OutputAsString = outputAsString;
        }

        public UseGraphicsTestCasesAttribute(string referenceImagePath , bool outputAsString = false)
        {
            m_ReferenceImagePath = referenceImagePath;
            m_OutputAsString = outputAsString;
        }

        /// <summary>
        /// The <c>IGraphicsTestCaseProvider</c> which will be used to generate the <c>GraphicsTestCase</c> instances for the tests.
        /// </summary>
        public IGraphicsTestCaseProvider Provider
        {
            get
            {
#if UNITY_EDITOR
                return new UnityEditor.TestTools.Graphics.EditorGraphicsTestCaseProvider(m_ReferenceImagePath);
#else
                return new RuntimeGraphicsTestCaseProvider();
#endif
            }
        }

        public static ColorSpace ColorSpace
        {
            get
            {
                return QualitySettings.activeColorSpace;
            }
        }

        public static RuntimePlatform Platform
        {
            get
            {
                return Application.platform;
            }
        }

        public static GraphicsDeviceType GraphicsDevice
        {
            get
            {
                return SystemInfo.graphicsDeviceType;
            }
        }


        IEnumerable<TestMethod> ITestBuilder.BuildFrom(IMethodInfo method, Test suite)
        {
            List<TestMethod> results = new List<TestMethod>();

            IGraphicsTestCaseProvider provider = Provider;

            try
            {
                foreach (var testCase in provider.GetTestCases())
                {
                    TestCaseParameters parms = new TestCaseParameters( new object[]{ m_OutputAsString? testCase.ScenePath : (object) testCase } )
                    {
                        ExpectedResult = new object(),
                        HasExpectedResult = true,
                    };

                    TestMethod test = this._builder.BuildTestMethod(method, suite, parms);
                    if (test.parms != null)
                        test.parms.HasExpectedResult = false;

                    test.Name = System.IO.Path.GetFileNameWithoutExtension(testCase.ScenePath);

                    results.Add(test);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to generate graphics testcases!");
                Debug.LogException(ex);
                throw;
            }

            suite.Properties.Set("ColorSpace", ColorSpace);
            suite.Properties.Set("RuntimePlatform", Platform);
            suite.Properties.Set("GraphicsDevice", GraphicsDevice);

            Console.WriteLine("Generated {0} graphics test cases.", results.Count);
            return results;
        }

        public static IEnumerable<GraphicsTestCase> GraphicsTestCaseList()
        {
            return new UnityEditor.TestTools.Graphics.EditorGraphicsTestCaseProvider(null).GetTestCases();
        }

        public static GraphicsTestCase GetCaseFromScenePath(string scenePath, string referenceImagePath = null )
        {
            UseGraphicsTestCasesAttribute tmp = new UseGraphicsTestCasesAttribute( string.IsNullOrEmpty(referenceImagePath)? String.Empty : referenceImagePath);

            var provider = tmp.Provider;

            return provider.GetTestCaseFromPath(scenePath);
        }
    }
}
