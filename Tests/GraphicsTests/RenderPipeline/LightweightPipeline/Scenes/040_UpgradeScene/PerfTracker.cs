using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PerfTracker : MonoBehaviour {

    const float clipLength = 20.0f;

    float elapsed = 0.0f;
    float duration = 0.0f;
    int frames = 0;
    List<float> msData = new List<float>();

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		float dt = Time.deltaTime;
        if (elapsed >= clipLength)
        {
            if (duration >= clipLength)
            {
                float averageFps = frames / duration;
                float averageMs = 1000.0f * duration / frames;
                msData.Sort();
                float[] msArray = msData.ToArray();
                Debug.LogError("AVG FPS: " + averageFps.ToString() + ", AVG MS: " + averageMs.ToString());
                float medMs = msArray[msArray.Length / 2];
                float medFps = 1.0f / medMs;
                medMs *= 1000.0f;
                Debug.LogError("MED FPS: " + medFps.ToString() + ", MED MS: " + medMs.ToString());

                Application.Quit();
            }

            msData.Add(dt);

            duration += dt;
            ++frames;
        }

        elapsed += dt;
	}
}
