using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;
using Debug = UnityEngine.Debug;

/// <summary>Reproducible environment acceptance captures and physics probes. No save data is touched.</summary>
public static class EnvironmentValidation
{
    public static readonly string[] Landmarks = { "", "", "Forest lookout", "Quarry excavator", "Sunset pavilion", "Fishing pier", "Canyon butte", "Alpine summit", "Refinery tanks", "Neon avenue" };
    [Serializable] public sealed class RouteResult
    {
        public int route, renderers, materials, colliders, cameraSamples, blockedViews, missingRoadSupport, missingShoulderSupport, intrusions, unsupportedProps, raisedSurface, shoulderSteps;
        public long triangles;
        public float elevationRange, maximumGrade;
        public double editorRenderMilliseconds;
        public string name;
        public List<string> issues = new List<string>();
    }
    [Serializable] private sealed class Report
    {
        public string method = "Static physics probes and 1280x720 editor camera renders; not human playtesting or standalone FPS.";
        public List<RouteResult> routes = new List<RouteResult>();
    }

    public static float MaximumGrade(Vector3[] points)
    {
        float maximum = 0;
        for (int i = 1; i < points.Length; i++)
        {
            var delta = points[i] - points[i - 1];
            maximum = Mathf.Max(maximum, Mathf.Abs(delta.y) / Mathf.Max(0.001f, new Vector2(delta.x, delta.z).magnitude));
        }
        return maximum;
    }

    public static bool IsEnvironmentCollider(Collider collider, Transform level)
    {
        if (collider == null || collider.isTrigger || !collider.transform.IsChildOf(level)) return false;
        var obstacles = level.Find("Obstacles");
        return obstacles == null || !collider.transform.IsChildOf(obstacles);
    }

    [MenuItem("Tools/PolyCar/Validate All Environments")]
    public static void Run()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/DriftPrototype.unity");
        string output = Path.GetFullPath("Review/Environment");
        Directory.CreateDirectory(output);
        var report = new Report();
        var maps = GameObject.Find("LevelMaps").transform;
        if (maps.childCount != 10) throw new InvalidOperationException("Expected ten routes");
        var canvas = GameObject.Find("Canvas");
        if (canvas != null) canvas.SetActive(false);
        foreach (var existing in Object.FindObjectsByType<CarController>(FindObjectsSortMode.None))
            if (existing.gameObject.layer != 31) existing.gameObject.SetActive(false);
        var camera = Camera.main;
        camera.GetComponent<CameraFollow>().enabled = false;
        camera.fieldOfView = 64; camera.farClipPlane = 1400;
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Vehicles/StarterCar.prefab");
        var car = Object.Instantiate(prefab); car.name = "Environment review car";
        car.GetComponent<CarController>().enabled = false;
        car.GetComponent<Rigidbody>().isKinematic = true;
        foreach (var collider in car.GetComponentsInChildren<Collider>()) collider.enabled = false;
        var overview = new Texture2D(1280, 1800, TextureFormat.RGB24, false);
        var views = new Texture2D(1280, 1800, TextureFormat.RGB24, false);
        var palettes = new HashSet<Color>();
        try
        {
            for (int index = 0; index < maps.childCount; index++)
            {
                for (int j = 0; j < maps.childCount; j++) maps.GetChild(j).gameObject.SetActive(false);
                var level = maps.GetChild(index); level.gameObject.SetActive(true);
                var course = level.GetComponent<RaceCourse>();
                var result = Inspect(level, index); report.routes.Add(result);
                var look = level.GetComponent<SceneLook>();
                if (look == null || look.sky == null) result.issues.Add("Missing level-owned atmosphere");
                else
                {
                    look.Restore(); look.Apply(camera);
                    Color horizon = look.sky.GetColor("_Horizon");
                    if (!palettes.Add(horizon)) result.issues.Add("Repeated atmosphere horizon palette");
                }
                Physics.SyncTransforms();
                ProbeCourse(level, course, index, result);
                var bounds = new Bounds(course.centerLine[0], Vector3.zero);
                foreach (var point in course.centerLine) bounds.Encapsulate(point);
                float extent = Mathf.Max(bounds.extents.x, bounds.extents.z) + 35;
                car.SetActive(false);
                camera.transform.position = bounds.center + new Vector3(-1.05f, 1.25f, -1.05f) * extent;
                camera.transform.LookAt(bounds.center);
                var photo = Capture(camera, Path.Combine(output, $"route-{index + 1:00}-overview.png"), 1280, 720, out result.editorRenderMilliseconds);
                CopyThumbnail(photo, overview, index); Object.DestroyImmediate(photo);
                float length = Length(course.centerLine);
                float corner = SharpestCorner(course.centerLine);
                var distances = new[] { Mathf.Min(20, length * .05f), length * .33f, corner, length * .83f };
                var routeSheet = new Texture2D(1280, 720, TextureFormat.RGB24, false);
                car.SetActive(true);
                for (int sample = 0; sample < distances.Length; sample++)
                {
                    HarborArtBuilder.Sample(course.centerLine, distances[sample], out var point, out var forward);
                    Vector3 horizontal = new Vector3(forward.x, 0, forward.z).normalized;
                    car.transform.SetPositionAndRotation(point + Vector3.up * .24f, Quaternion.LookRotation(forward));
                    camera.transform.position = point - horizontal * 8.5f + Vector3.up * 3.65f;
                    camera.transform.LookAt(point + horizontal * 4 + Vector3.up * 1.1f);
                    photo = Capture(camera, Path.Combine(output, $"route-{index + 1:00}-drive-{sample + 1}.png"), 1280, 720, out _);
                    CopySmall(photo, routeSheet, sample % 2 * 640, (1 - sample / 2) * 360);
                    if (sample == 2) CopyThumbnail(photo, views, index);
                    Object.DestroyImmediate(photo);
                }
                routeSheet.Apply(); File.WriteAllBytes(Path.Combine(output, $"route-{index + 1:00}-driving-sheet.png"), routeSheet.EncodeToPNG()); Object.DestroyImmediate(routeSheet);
                look?.Restore();
                Debug.Log($"ENVIRONMENT_ROUTE route={index + 1} elevation={result.elevationRange:F1} grade={result.maximumGrade:F3} triangles={result.triangles} blockedViews={result.blockedViews}/{result.cameraSamples} roadGaps={result.missingRoadSupport} shoulderGaps={result.missingShoulderSupport} intrusions={result.intrusions} unsupportedProps={result.unsupportedProps} issues={result.issues.Count}");
            }
            overview.Apply(); views.Apply();
            File.WriteAllBytes(Path.Combine(output, "all-route-overviews.png"), overview.EncodeToPNG());
            File.WriteAllBytes(Path.Combine(output, "all-route-driving-views.png"), views.EncodeToPNG());
        }
        finally
        {
            Object.DestroyImmediate(car); Object.DestroyImmediate(overview); Object.DestroyImmediate(views);
            File.WriteAllText(Path.Combine(output, "environment-report.json"), JsonUtility.ToJson(report, true));
            WriteReport(output, report);
        }
        int issues = report.routes.Sum(r => r.issues.Count);
        if (issues > 0) throw new InvalidOperationException($"Environment review found {issues} issues; see {output}/environment-report.md");
        Debug.Log("ENVIRONMENT_VALIDATION_OK routes=10 output=" + output);
    }

    public static RouteResult Inspect(Transform level, int index)
    {
        var result = new RouteResult { route = index + 1, name = CourseDifficultyBuilder.Names[index] };
        var course = level.GetComponent<RaceCourse>();
        if (course == null || course.centerLine == null || course.centerLine.Length < 2) throw new InvalidOperationException("Missing course geometry");
        result.elevationRange = course.centerLine.Max(p => p.y) - course.centerLine.Min(p => p.y);
        result.maximumGrade = MaximumGrade(course.centerLine);
        if (result.maximumGrade > .081f) result.issues.Add("Road grade exceeds 8 percent");
        if ((index == 2 && result.elevationRange < 9) || (index == 7 && result.elevationRange < 14)) result.issues.Add("Mountain route lacks meaningful elevation");
        var renderers = level.GetComponentsInChildren<Renderer>(true);
        result.renderers = renderers.Length;
        result.materials = renderers.SelectMany(r => r.sharedMaterials).Distinct().Count();
        result.colliders = level.GetComponentsInChildren<Collider>(true).Length;
        foreach (var renderer in renderers)
        {
            if (renderer.GetComponent<TextMesh>() != null) continue;
            foreach (var material in renderer.sharedMaterials)
                if (material == null || material.shader == null || !material.shader.isSupported || material.shader.name.Contains("InternalError"))
                    result.issues.Add("Broken material: " + renderer.name);
        }
        foreach (var filter in level.GetComponentsInChildren<MeshFilter>(true))
        {
            if (filter.sharedMesh == null) result.issues.Add("Missing mesh: " + filter.name);
            else result.triangles += filter.sharedMesh.triangles.Length / 3;
        }
        if (result.triangles > 900000) result.issues.Add("Route exceeds 900,000 triangle budget");
        if (index >= 2 && !level.GetComponentsInChildren<Transform>(true).Any(t => t.name.StartsWith(Landmarks[index], StringComparison.OrdinalIgnoreCase)))
            result.issues.Add("Missing theme landmark: " + Landmarks[index]);
        return result;
    }

    private static void ProbeCourse(Transform level, RaceCourse course, int index, RouteResult result)
    {
        float length = Length(course.centerLine), halfWidth = CourseDifficultyBuilder.Widths[index] / 2;
        var scenery = level.Find("Imported scenery");
        var blockers = new HashSet<string>();
        for (float distance = 4; distance < length - 4; distance += 8)
        {
            HarborArtBuilder.Sample(course.centerLine, distance, out var point, out var forward);
            forward.y = 0; forward.Normalize();
            var side = Vector3.Cross(Vector3.up, forward);
            if (!Supported(point, level, .5f)) result.missingRoadSupport++;
            foreach (var hit in Physics.RaycastAll(point + Vector3.up * 1, Vector3.down, 2, ~0, QueryTriggerInteraction.Ignore))
                if (IsEnvironmentCollider(hit.collider, level) && hit.point.y > point.y + .18f)
                { result.raisedSurface++; blockers.Add(hit.collider.name); break; }
            // Three lateral camera positions represent the line and the available drift margin.
            foreach (float lateral in new[] { -.55f, 0, .55f })
            {
                var target = point + side * (halfWidth * lateral) + Vector3.up * 1.1f;
                var position = point + side * (halfWidth * lateral) - forward * 8.5f + Vector3.up * 3.65f;
                result.cameraSamples++;
                var hits = Physics.RaycastAll(position, (target - position).normalized, Vector3.Distance(position, target), ~0, QueryTriggerInteraction.Ignore);
                foreach (var hit in hits)
                {
                    if (!IsEnvironmentCollider(hit.collider, level)) continue;
                    result.blockedViews++; blockers.Add(hit.collider.name); break;
                }
            }
            if (index >= 2)
            {
                foreach (float sign in new[] { -1f, 1f })
                {
                    if (!Supported(point + side * (halfWidth + .6f) * sign, level, .65f)) result.missingShoulderSupport++;
                    float previousSurface = float.NaN;
                    string previousSurfaceName = string.Empty;
                    for (float lateral = halfWidth + 2.5f; lateral <= halfWidth + 5.5f; lateral += .25f)
                    {
                        var rayStart = point + side * lateral * sign + Vector3.up * 2;
                        var surfaces = Physics.RaycastAll(rayStart, Vector3.down, 5, ~0, QueryTriggerInteraction.Ignore)
                            .Where(hit => IsEnvironmentCollider(hit.collider, level) && hit.collider is MeshCollider &&
                                (hit.collider.transform.IsChildOf(level.Find("Road")) || hit.collider.name.EndsWith("_Terrain")))
                            .OrderByDescending(hit => hit.point.y).ToArray();
                        if (surfaces.Length == 0) continue;
                        float surface = surfaces[0].point.y;
                        if (!float.IsNaN(previousSurface) && Mathf.Abs(surface - previousSurface) > .09f)
                        {
                            result.shoulderSteps++;
                            if(result.shoulderSteps<=3) Debug.Log($"SHOULDER_STEP route={index+1} point={rayStart-Vector3.up*2} lateral={lateral:F2} previousY={previousSurface:F3} currentY={surface:F3} previousCollider={previousSurfaceName} currentCollider={surfaces[0].collider.name}");
                        }
                        previousSurface = surface; previousSurfaceName=surfaces[0].collider.name;
                    }
                }
            }
            if (scenery != null)
            {
                var occupied = Physics.OverlapBox(point + Vector3.up * 1.4f, new Vector3(Mathf.Max(halfWidth - .6f, 1), 1.1f, .8f), Quaternion.LookRotation(forward), ~0, QueryTriggerInteraction.Ignore);
                foreach (var collider in occupied)
                    if (collider.transform.IsChildOf(scenery)) { result.intrusions++; blockers.Add(collider.name); }
            }
        }
        if (result.raisedSurface > 0) result.issues.Add($"Environment surface protrudes above road at {result.raisedSurface} samples: {string.Join("; ", blockers.Take(12))}");
        if (result.missingRoadSupport > 0) result.issues.Add($"Road support missing at {result.missingRoadSupport} samples");
        if (result.shoulderSteps > 0) result.issues.Add($"Abrupt outer shoulder steps at {result.shoulderSteps} lateral samples (more than 9 cm rise over 25 cm)");
        if (result.missingShoulderSupport > 0) result.issues.Add($"Shoulder support missing at {result.missingShoulderSupport} samples");
        if (result.blockedViews > 0) result.issues.Add($"Chase camera blocked at {result.blockedViews} samples: {string.Join("; ", blockers.Take(12))}");
        if (result.intrusions > 0) result.issues.Add($"Scenery collision intrudes into driving corridor at {result.intrusions} samples");
        // Grounded source models should have either terrain or an authored platform beneath their base.
        if (index < 2 || scenery == null) return;
        foreach (Transform prop in scenery.GetComponentsInChildren<Transform>(true))
        {
            if (!prop.name.StartsWith("Kenney_", StringComparison.Ordinal)) continue;
            var bounds = ExternalArtBuilder.BoundsOf(prop.gameObject);
            var basePoint = new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
            var hits = Physics.RaycastAll(basePoint + Vector3.up * .5f, Vector3.down, 1.5f, ~0, QueryTriggerInteraction.Ignore);
            bool supported = hits.Any(hit => !hit.collider.transform.IsChildOf(prop) && IsEnvironmentCollider(hit.collider, level));
            if (!supported)
            {
                result.unsupportedProps++;
                if (result.unsupportedProps <= 5) result.issues.Add("Unsupported model base: " + prop.name + " at " + basePoint);
            }
        }
    }

    private static bool Supported(Vector3 point, Transform level, float tolerance)
    {
        return Physics.RaycastAll(point + Vector3.up * 1, Vector3.down, 2, ~0, QueryTriggerInteraction.Ignore)
            .Any(hit => IsEnvironmentCollider(hit.collider, level) && Mathf.Abs(hit.point.y - point.y) <= tolerance);
    }
    private static float Length(Vector3[] points) { float length = 0; for (int i = 1; i < points.Length; i++) length += Vector3.Distance(points[i - 1], points[i]); return length; }
    private static float SharpestCorner(Vector3[] points)
    {
        float best = 0, at = 0, distance = 0;
        for (int i = 1; i < points.Length - 8; i++)
        {
            distance += Vector3.Distance(points[i - 1], points[i]);
            if (i < 8) continue;
            float turn = Vector3.Angle(points[i] - points[i - 8], points[i + 8] - points[i]);
            if (turn > best) { best = turn; at = distance; }
        }
        return at;
    }
    private static Texture2D Capture(Camera camera, string file, int width, int height, out double renderMilliseconds)
    {
        var target = new RenderTexture(width, height, 24) { antiAliasing = 4 };
        var previous = RenderTexture.active; camera.targetTexture = target;
        try
        {
            for (int i = 0; i < 3; i++) camera.Render();
            var watch = Stopwatch.StartNew(); for (int i = 0; i < 5; i++) camera.Render(); watch.Stop();
            renderMilliseconds = watch.Elapsed.TotalMilliseconds / 5;
            RenderTexture.active = target;
            var image = new Texture2D(width, height, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, width, height), 0, 0); image.Apply(); File.WriteAllBytes(file, image.EncodeToPNG()); return image;
        }
        finally { camera.targetTexture = null; RenderTexture.active = previous; target.Release(); Object.DestroyImmediate(target); }
    }
    private static void CopyThumbnail(Texture2D source, Texture2D destination, int index) => CopySmall(source, destination, index % 2 * 640, (4 - index / 2) * 360);
    private static void CopySmall(Texture2D source, Texture2D destination, int x, int y)
    {
        var colors = new Color[640 * 360];
        for (int row = 0; row < 360; row++) for (int col = 0; col < 640; col++) colors[row * 640 + col] = source.GetPixel(col * 2, row * 2);
        destination.SetPixels(x, y, 640, 360, colors);
    }
    private static void WriteReport(string output, Report report)
    {
        var rows = new List<string> { "route,elevation_m,max_grade,triangles,renderers,materials,colliders,camera_samples,blocked_views,road_gaps,shoulder_gaps,scenery_intrusions,unsupported_props,editor_render_ms" };
        var md = new List<string> { "# Route environment review", "", report.method, "", "Each route includes an overview and four driver-height captures: start, one-third, sharpest bend and final section. Camera probes sample the center and lateral drift margins every 8 m. Static probes complement the separate deterministic driving run.", "", "| Route | Elevation | Triangles | Camera blocked / probes | Issues |", "|---|---:|---:|---:|---:|" };
        foreach (var r in report.routes)
        {
            rows.Add(string.Join(",", r.route, r.elevationRange.ToString("F2", CultureInfo.InvariantCulture), r.maximumGrade.ToString("F3", CultureInfo.InvariantCulture), r.triangles, r.renderers, r.materials, r.colliders, r.cameraSamples, r.blockedViews, r.missingRoadSupport, r.missingShoulderSupport, r.intrusions, r.unsupportedProps, r.editorRenderMilliseconds.ToString("F2", CultureInfo.InvariantCulture)));
            md.Add($"| [{r.route:00} {r.name}](route-{r.route:00}-driving-sheet.png) | {r.elevationRange:F1} m | {r.triangles:N0} | {r.blockedViews} / {r.cameraSamples} | {r.issues.Count} |");
        }
        md.Add("");
        foreach (var r in report.routes.Where(r => r.issues.Count > 0)) { md.Add($"## {r.route:00} {r.name}"); md.Add(""); foreach (var issue in r.issues) md.Add("- " + issue); md.Add(""); }
        md.Add("Editor render timings are diagnostic CPU call timings, not end-to-end frame times or target-device FPS. Screenshots must also be visually reviewed for composition, clipping, shadow artifacts and route readability.");
        File.WriteAllLines(Path.Combine(output, "environment-budget.csv"), rows); File.WriteAllLines(Path.Combine(output, "environment-report.md"), md);
    }
}
