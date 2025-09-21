using System.Collections.Generic;
using UnityEngine;
using System.IO;
using Newtonsoft.Json;

/// <summary>
/// Loads a relative-offset Q-table (dx,dy) and returns the best action
/// for any robot – table is shared by all agents and grid sizes.
/// </summary>
public class QTableManager : MonoBehaviour
{
    private Dictionary<string, float[]> qTable = new();

    [Header("Q-Table Settings")]
    public string qTableFileName = "qtable_relative.json";
    public int windowRadius = 4;              // must match training

    void Awake() => LoadQTable();

    // ────────────────────────────────────────────────────────────────────────────
    public void LoadQTable()
    {
        string filePath = Path.Combine(Application.streamingAssetsPath,
                                       qTableFileName);
        if (!File.Exists(filePath))
        {
            Debug.LogError($"Q-table not found: {filePath}");
            return;
        }
        string json = File.ReadAllText(filePath);
        qTable = JsonConvert.DeserializeObject<Dictionary<string, float[]>>(json);
        Debug.Log($"Loaded relative Q-table – {qTable.Count} states.");
    }

    /// <summary>
    /// Returns action 0..3 (↑ ↓ ← →).  If state is missing, returns −1 so
    /// caller can fall back to A* or random.
    /// </summary>
    public int GetBestAction(Vector2Int current, Vector2Int goal)
    {
        int dx = Mathf.Clamp(goal.x - current.x, -windowRadius, windowRadius);
        int dy = Mathf.Clamp(goal.y - current.y, -windowRadius, windowRadius);
        string key = $"{dx}_{dy}";

        if (!qTable.TryGetValue(key, out float[] qVals))
            return -1;                         // unseen → fallback

        int best = 0;
        float bestVal = qVals[0];
        for (int a = 1; a < qVals.Length; ++a)
            if (qVals[a] > bestVal) { best = a; bestVal = qVals[a]; }

        return best;
    }
}
