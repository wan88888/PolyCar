using UnityEngine;

[RequireComponent(typeof(Collider))]
public sealed class CoinPickup : MonoBehaviour
{
    [SerializeField, Min(1)] private int value = 1;
    [SerializeField, Min(0f)] private float rotationSpeed = 150f;
    [SerializeField, Min(0f)] private float bobHeight = 0.18f;
    [SerializeField, Min(0f)] private float bobSpeed = 2.4f;
    [SerializeField, Min(1f)] private float highlightScale = 1.35f;
    [SerializeField] private Color highlightColor = new Color(0.45f, 0.95f, 1f);

    private LevelManager levelManager;
    private Renderer[] renderers;
    private Color[] normalColors;
    private Vector3 normalScale;
    private Vector3 startPosition;
    private bool hasStartPosition;
    private bool collected;
    private bool highlighted;

    public int Value => value;
    public bool IsCollected => collected;
    public Vector3 GuidancePosition => hasStartPosition ? startPosition : transform.position;

    private void Awake()
    {
        Collider pickupCollider = GetComponent<Collider>();
        pickupCollider.isTrigger = true;
        CacheRenderers();
        normalScale = transform.localScale;
    }

    private void Start()
    {
        RememberStartPosition();
        levelManager = FindFirstObjectByType<LevelManager>();
    }

    private void Update()
    {
        float rotationMultiplier = highlighted ? 1.45f : 1f;
        float activeBobHeight = highlighted ? bobHeight * 1.55f : bobHeight;
        transform.Rotate(Vector3.up, rotationSpeed * rotationMultiplier * Time.deltaTime, Space.World);
        transform.position = startPosition + Vector3.up * (Mathf.Sin(Time.time * bobSpeed) * activeBobHeight);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (collected || other.GetComponentInParent<CarController>() == null)
        {
            return;
        }

        collected = true;
        SetHighlighted(false);
        if (levelManager == null)
        {
            levelManager = FindFirstObjectByType<LevelManager>();
        }

        if (levelManager != null)
        {
            levelManager.CollectCoin(this);
        }

        AudioManager.Instance?.PlayCoin();
        gameObject.SetActive(false);
    }

    public void ResetPickup()
    {
        RememberStartPosition();
        collected = false;
        transform.position = startPosition;
        gameObject.SetActive(true);
        SetHighlighted(false);
    }

    public void SetHighlighted(bool active)
    {
        if (highlighted == active)
        {
            return;
        }

        highlighted = active;
        transform.localScale = active ? normalScale * highlightScale : normalScale;

        CacheRenderers();
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null)
            {
                continue;
            }

            Material material = renderers[i].material;
            if (material.HasProperty("_Color"))
            {
                material.color = active ? highlightColor : normalColors[i];
            }
        }
    }

    private void RememberStartPosition()
    {
        if (hasStartPosition)
        {
            return;
        }

        startPosition = transform.position;
        hasStartPosition = true;
    }

    private void CacheRenderers()
    {
        if (renderers != null)
        {
            return;
        }

        renderers = GetComponentsInChildren<Renderer>(true);
        normalColors = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
        {
            Material material = renderers[i].material;
            normalColors[i] = material.HasProperty("_Color") ? material.color : Color.white;
        }
    }
}
