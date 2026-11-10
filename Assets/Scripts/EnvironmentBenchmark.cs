#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;

/// <summary>Opt-in rendered route traversal for repeatable standalone frame-time measurements.
/// This is a camera/visual-car tour, not a physics or human driving test; normal launches never run it.</summary>
public sealed class EnvironmentBenchmark : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Launch()
    {
        var args = Environment.GetCommandLineArgs();
        int flag = Array.IndexOf(args, "--environment-benchmark");
        if (flag < 0) return;
        var runner = new GameObject("Environment benchmark").AddComponent<EnvironmentBenchmark>();
        runner.output = flag + 1 < args.Length ? args[flag + 1] : Path.Combine(Application.persistentDataPath, "EnvironmentReview");
        // Disable game flow before Start so benchmarking does not alter route unlocks or player records.
        foreach (var component in FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (component is LevelManager || component is MainMenuUI || component is GameManager || component is CarGarage) component.enabled = false;
    }

    private string output;
    private IEnumerator Start()
    {
        Directory.CreateDirectory(output);
        Application.runInBackground = true;
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = -1;
        Time.timeScale = 1;
        Screen.SetResolution(1920, 1080, FullScreenMode.Windowed);
        foreach (var canvas in FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None)) canvas.gameObject.SetActive(false);
        var maps = GameObject.Find("LevelMaps").transform;
        var camera = Camera.main;
        camera.GetComponent<CameraFollow>().enabled = false;
        var garage = FindFirstObjectByType<CarGarage>(FindObjectsInactive.Include);
        var visual = Instantiate(garage.SelectedPrefab);
        visual.GetComponent<CarController>().enabled = false;
        visual.GetComponent<Rigidbody>().isKinematic = true;
        foreach (var collider in visual.GetComponentsInChildren<Collider>()) collider.enabled = false;
        var rows = new List<string>{"route,frames,mean_fps,p95_frame_ms,p99_frame_ms,width,height"};
        File.WriteAllText(Path.Combine(output,"device.txt"), $"{SystemInfo.deviceModel}\n{SystemInfo.processorType}\n{SystemInfo.graphicsDeviceName}\n{SystemInfo.operatingSystem}\nUnity {Application.unityVersion}\nDevelopment player; VSync off; camera traversal, physics disabled; UI hidden. Excludes 2 s warm-up and screenshot frames.\n");
        for (int route = 0; route < maps.childCount; route++)
        {
            for (int i = 0; i < maps.childCount; i++) maps.GetChild(i).gameObject.SetActive(false);
            var level = maps.GetChild(route);
            level.gameObject.SetActive(true);
            var look = level.GetComponent<SceneLook>();
            if (look != null) { look.Restore(); look.Apply(camera); }
            var course = level.GetComponent<RaceCourse>();
            var cumulative = new float[course.centerLine.Length];
            for(int i=1;i<cumulative.Length;i++) cumulative[i]=cumulative[i-1]+Vector3.Distance(course.centerLine[i-1],course.centerLine[i]);
            float length=cumulative[cumulative.Length-1], elapsed=0;
            var frames=new List<float>();
            bool captured=false;
            while(elapsed<14)
            {
                float d=Mathf.Clamp01((elapsed-2)/12)*Mathf.Max(0,length-6);
                int segment=Array.BinarySearch(cumulative,d);
                if(segment<0)segment=~segment;
                segment=Mathf.Clamp(segment,1,cumulative.Length-1);
                float t=Mathf.InverseLerp(cumulative[segment-1],cumulative[segment],d);
                var p=Vector3.Lerp(course.centerLine[segment-1],course.centerLine[segment],t);
                var forward=(course.centerLine[segment]-course.centerLine[segment-1]).normalized;
                visual.transform.SetPositionAndRotation(p+Vector3.up*0.24f,Quaternion.LookRotation(forward));
                var heading=new Vector3(forward.x,0,forward.z).normalized;
                camera.transform.position=p-heading*8+Vector3.up*3.7f;
                camera.transform.LookAt(p+heading*5+Vector3.up*1.1f);
                camera.fieldOfView=64;
                yield return null;
                float dt=Time.unscaledDeltaTime;
                if(elapsed>2 && !captured) frames.Add(dt);
                elapsed+=dt;
                if(!captured && elapsed>13.9f) { ScreenCapture.CaptureScreenshot(Path.Combine(output,$"player-route-{route+1:00}.png")); captured=true; }
            }
            if(frames.Count==0) { File.WriteAllText(Path.Combine(output,"failed.txt"),"No measured frames on route "+(route+1)); Application.Quit(1); yield break; }
            frames.Sort();float total=0;foreach(var f in frames)total+=f;
            float p95=frames[Mathf.Min(frames.Count-1,(int)(frames.Count*.95f))]*1000;
            float p99=frames[Mathf.Min(frames.Count-1,(int)(frames.Count*.99f))]*1000;
            rows.Add(string.Join(",",route+1,frames.Count,(frames.Count/total).ToString("F1",CultureInfo.InvariantCulture),p95.ToString("F2",CultureInfo.InvariantCulture),p99.ToString("F2",CultureInfo.InvariantCulture),Screen.width,Screen.height));
            File.WriteAllLines(Path.Combine(output,"player-frame-times.csv"),rows);
            Debug.Log("ENVIRONMENT_PLAYER_ROUTE "+rows[rows.Count-1]);
            yield return null;
        }
        File.WriteAllText(Path.Combine(output,"complete.txt"), "All ten rendered route traversals completed. See frame timings; this does not replace physics validation or human playtesting.");
        Application.Quit();
    }
}
#endif
