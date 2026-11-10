using UnityEngine;

/// <summary>Ordered gates for a timed route. Coins remain optional pickups.</summary>
public sealed class RaceCourse : MonoBehaviour
{
    public Transform[] gates;
    public Vector3[] centerLine;
    public float gateHalfWidth = 7f;
    public float goldTime = 90f;
    public int silverScore = 300;
    public int goldScore = 1000;
    public int laps = 1;
    [Range(1,10)] public int difficulty=1;
    public string skillFocus="Learn the line";

    public void HighlightGate(int next)
    {
        var block = new MaterialPropertyBlock();
        for (int i = 0; i < gates.Length; i++)
        {
            Color color = i == next ? new Color(1f,0.64f,0.12f) : new Color(0.12f,0.48f,0.52f);
            foreach (var renderer in gates[i].GetComponentsInChildren<MeshRenderer>())
            {
                if (!renderer.name.Contains("beacon") && !renderer.name.Contains("stripe")) continue;
                renderer.GetPropertyBlock(block); block.SetColor("_BaseColor", color); renderer.SetPropertyBlock(block);
            }
        }
    }

    public static bool CrossedGate(Vector3 previous, Vector3 current, Transform gate, float halfWidth)
    {
        Vector3 a = gate.InverseTransformPoint(previous);
        Vector3 b = gate.InverseTransformPoint(current);
        if (a.z >= 0f || b.z < 0f) return false;
        float t = -a.z / (b.z - a.z);
        Vector3 crossing = Vector3.Lerp(a, b, t);
        return Mathf.Abs(crossing.x) <= halfWidth && Mathf.Abs(crossing.y) <= 3f;
    }

    public static string Medal(float seconds, int score, int recoveries, float goldSeconds, int silverPoints, int goldPoints)
    {
        if (score >= goldPoints && seconds <= goldSeconds && recoveries == 0) return "GOLD";
        return score >= silverPoints ? "SILVER" : "BRONZE";
    }
}
