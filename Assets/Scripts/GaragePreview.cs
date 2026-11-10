using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RawImage))]
public sealed class GaragePreview : MonoBehaviour
{
    private CarGarage garage;
    private GameObject displayedPrefab;
    private GameObject model;
    private Camera previewCamera;
    private RenderTexture target;
    private GameObject studio;

    private void Start()
    {
        garage = FindFirstObjectByType<CarGarage>();
        studio = new GameObject("Garage preview studio");
        studio.transform.position = new Vector3(0f,-500f,0f);
        var cameraObject = new GameObject("Preview camera"); cameraObject.transform.SetParent(studio.transform, false);
        previewCamera = cameraObject.AddComponent<Camera>();
        cameraObject.transform.localPosition = new Vector3(5.4f,3.1f,6.5f);
        cameraObject.transform.LookAt(studio.transform.position + Vector3.up * 0.3f);
        previewCamera.clearFlags = CameraClearFlags.SolidColor; previewCamera.backgroundColor = Color.clear;
        previewCamera.cullingMask = 1 << 31; previewCamera.fieldOfView = 22f; previewCamera.farClipPlane = 25f;
        target = new RenderTexture(1200,520,24); target.antiAliasing = 4; previewCamera.targetTexture = target;
        GetComponent<RawImage>().texture = target;
        var light = new GameObject("Studio key").AddComponent<Light>(); light.transform.SetParent(studio.transform,false);
        light.gameObject.layer = 31;
        light.type = LightType.Directional; light.intensity = 1.5f; light.color = new Color(1f,0.9f,0.75f);
        light.transform.localRotation = Quaternion.Euler(45,-40,0); light.cullingMask = 1 << 31;
    }

    private void Update()
    {
        if (garage == null) return;
        if (displayedPrefab != garage.SelectedPrefab)
        {
            if (model != null) Destroy(model);
            displayedPrefab = garage.SelectedPrefab;
            // Instantiate inactive so presentation does not create particles or interact with gameplay.
            var holder = new GameObject("Preview model"); holder.SetActive(false); holder.transform.SetParent(studio.transform,false);
            model = holder;
            var clone = Instantiate(displayedPrefab, holder.transform);
            clone.GetComponent<CarController>().enabled = false;
            clone.GetComponent<CarPresentation>().enabled = false;
            clone.GetComponent<Rigidbody>().isKinematic = true;
            foreach (var collider in clone.GetComponentsInChildren<Collider>()) collider.enabled = false;
            foreach (var child in clone.GetComponentsInChildren<Transform>(true)) child.gameObject.layer = 31;
            holder.SetActive(true);
        }
        if (model != null) model.transform.localRotation = Quaternion.Euler(0f, Mathf.Sin(Time.unscaledTime * 0.3f) * 18f, 0f);
    }
    private void OnEnable() { if (previewCamera != null) previewCamera.enabled = true; }
    private void OnDisable() { if (previewCamera != null) previewCamera.enabled = false; }
    private void OnDestroy()
    {
        if (studio != null) Destroy(studio);
        if (target != null) { target.Release(); Destroy(target); }
    }
}
