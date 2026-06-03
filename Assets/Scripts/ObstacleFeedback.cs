using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public sealed class ObstacleFeedback : MonoBehaviour
{
    private static readonly int ColorId = Shader.PropertyToID("_Color");

    [SerializeField, Min(0)] private int coinPenalty = 1;
    [SerializeField, Min(0.1f)] private float hitCooldown = 0.65f;
    [SerializeField, Min(1f)] private float pulseScale = 1.12f;
    [SerializeField] private Color hitFlashColor = new Color(1f, 0.74f, 0.22f);

    private Renderer[] renderers;
    private Color[] normalColors;
    private MaterialPropertyBlock propertyBlock;
    private Vector3 normalScale;
    private float nextHitTime;
    private GameManager gameManager;
    private LevelManager levelManager;

    private void Awake()
    {
        normalScale = transform.localScale;
        CacheRenderers();
        gameManager = FindFirstObjectByType<GameManager>();
        levelManager = FindFirstObjectByType<LevelManager>();
    }

    private void OnCollisionEnter(Collision collision)
    {
        HandleHit(collision.collider);
    }

    private void OnTriggerEnter(Collider other)
    {
        HandleHit(other);
    }

    public void Configure(int penalty, Color flashColor, float scale)
    {
        coinPenalty = Mathf.Max(0, penalty);
        hitFlashColor = flashColor;
        pulseScale = Mathf.Max(1f, scale);
    }

    private void HandleHit(Collider other)
    {
        if (Time.time < nextHitTime || other.GetComponentInParent<CarController>() == null)
        {
            return;
        }

        nextHitTime = Time.time + hitCooldown;

        if (gameManager == null)
        {
            gameManager = FindFirstObjectByType<GameManager>();
        }

        if (gameManager != null)
        {
            gameManager.RegisterObstacleHit(coinPenalty);
        }
        else
        {
            if (levelManager == null)
            {
                levelManager = FindFirstObjectByType<LevelManager>();
            }

            if (levelManager != null)
            {
                levelManager.RegisterObstacleHit(coinPenalty);
            }

            AudioManager.Instance?.PlayCrash();
        }

        StopAllCoroutines();
        StartCoroutine(FlashHit());
    }

    private IEnumerator FlashHit()
    {
        SetRendererColor(hitFlashColor);
        transform.localScale = normalScale * pulseScale;
        yield return new WaitForSeconds(0.08f);
        transform.localScale = normalScale;
        SetRendererColor(null);
    }

    private void CacheRenderers()
    {
        if (renderers != null)
        {
            return;
        }

        renderers = GetComponentsInChildren<Renderer>(true);
        normalColors = new Color[renderers.Length];
        propertyBlock = new MaterialPropertyBlock();
        for (int i = 0; i < renderers.Length; i++)
        {
            Material material = renderers[i].sharedMaterial;
            normalColors[i] = material.HasProperty("_Color") ? material.color : Color.white;
        }
    }

    private void SetRendererColor(Color? color)
    {
        CacheRenderers();
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null)
            {
                continue;
            }

            renderers[i].GetPropertyBlock(propertyBlock);
            propertyBlock.SetColor(ColorId, color.HasValue ? color.Value : normalColors[i]);
            renderers[i].SetPropertyBlock(propertyBlock);
        }
    }
}
