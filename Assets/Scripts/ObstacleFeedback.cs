using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public sealed class ObstacleFeedback : MonoBehaviour
{
    [SerializeField, Min(0)] private int coinPenalty = 1;
    [SerializeField, Min(0.1f)] private float hitCooldown = 0.65f;
    [SerializeField, Min(1f)] private float pulseScale = 1.12f;
    [SerializeField] private Color hitFlashColor = new Color(1f, 0.74f, 0.22f);

    private Renderer[] renderers;
    private Color[] normalColors;
    private Vector3 normalScale;
    private float nextHitTime;

    private void Awake()
    {
        normalScale = transform.localScale;
        CacheRenderers();
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

        GameManager gameManager = FindFirstObjectByType<GameManager>();
        if (gameManager != null)
        {
            gameManager.RegisterObstacleHit(coinPenalty);
        }
        else
        {
            LevelManager levelManager = FindFirstObjectByType<LevelManager>();
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
        for (int i = 0; i < renderers.Length; i++)
        {
            Material material = renderers[i].material;
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

            Material material = renderers[i].material;
            if (!material.HasProperty("_Color"))
            {
                continue;
            }

            material.color = color.HasValue ? color.Value : normalColors[i];
        }
    }
}
