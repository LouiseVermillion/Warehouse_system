using System.Collections.Generic;
using UnityEngine;
using System.IO;
using Newtonsoft.Json;

[DefaultExecutionOrder(-100)]            // load before other scripts
public class InventoryDatabase : MonoBehaviour
{
    public string fileName = "inventory.json";

    // PUBLIC read-only accessors
    public IReadOnlyList<BoxRecord> Stored => stored;
    public IReadOnlyList<BoxRecord> Taken => taken;

    [System.Serializable]
    public struct BoxRecord
    {
        public string id;
        public Vector2Int grid;           // x,y under the 2-D grid
        public int altitude;              // z in the stack

        public BoxRecord(string id, Vector2Int g, int z)
        { this.id = id; grid = g; altitude = z; }
    }

    // ── internal lists ────────────────────────────────────────────────────────
    private List<BoxRecord> stored = new();    // boxes currently in warehouse
    private List<BoxRecord> taken = new();    // boxes already dispatched

    // ── singleton shortcut (optional) ─────────────────────────────────────────
    public static InventoryDatabase I { get; private set; }

    void Awake()
    {
        if (I != null && I != this) { Destroy(gameObject); return; }
        I = this;
        DontDestroyOnLoad(gameObject);
        Load();
    }

    // ── CRUD called by BoxManager ─────────────────────────────────────────────
    public void RegisterBox(string id, Vector2Int grid, int altitude)
    {
        stored.Add(new BoxRecord(id, grid, altitude));
    }

    public void UpdateAltitude(string id, int newAlt)
    {
        int i = stored.FindIndex(b => b.id == id);
        if (i >= 0)
        {
            BoxRecord r = stored[i];
            r.altitude = newAlt;
            stored[i] = r;
        }
    }

    public void MoveToTaken(string id)
    {
        int i = stored.FindIndex(b => b.id == id);
        if (i < 0) return;
        taken.Add(stored[i]);
        stored.RemoveAt(i);
    }

    // ── save / load ───────────────────────────────────────────────────────────
    [System.Serializable]
    private class Wrapper { public List<BoxRecord> stored; public List<BoxRecord> taken; }

    public void Save()
    {
        var w = new Wrapper { stored = stored, taken = taken };
        string json = JsonConvert.SerializeObject(w, Formatting.Indented);

        string path = Path.Combine(Application.streamingAssetsPath, fileName);
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        File.WriteAllText(path, json);
        Debug.Log($"💾 Inventory saved: {stored.Count} stored, {taken.Count} taken");
    }

    public void Load()
    {
        string path = Path.Combine(Application.streamingAssetsPath, fileName);
        if (!File.Exists(path)) { Debug.Log("ℹ️ No inventory file yet."); return; }

        var w = JsonConvert.DeserializeObject<Wrapper>(File.ReadAllText(path));
        stored = w.stored ?? new();
        taken = w.taken ?? new();
        Debug.Log($"📦 Inventory loaded: {stored.Count} stored, {taken.Count} taken");
    }

    void OnApplicationQuit() => Save();
}
