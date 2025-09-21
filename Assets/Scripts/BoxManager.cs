using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.IO;
using Newtonsoft.Json;

/// <summary>
/// Generates boxes, keeps local stack data, spawns / destroys visuals,
/// and keeps InventoryDatabase in sync (stored vs. taken).
/// </summary>
public class BoxManager : MonoBehaviour
{
    [Header("Size & Fill")]
    public int width = 10;
    public int height = 10;
    public int maxStackHeight = 10;
    public bool generateAtStart = true;

    [Header("Box Data Import")]
    public string boxDataFileName = "inventory.json";
    public string storedFileName = "stored.json";

    [Header("Grid Origin (bottom-left)")]
    public Vector2Int gridOrigin = new Vector2Int(0, 0);

    [Header("Prefabs / Parents")]
    public GameObject boxPrefab;
    public Transform boxBasePlane;

    // ── runtime refs ───────────────────────────────────────────────────────────
    private GridManager gridManager;
    //private InventoryDatabase db;

    // ── data ──────────────────────────────────────────────────────────────────
    private Dictionary<Vector2Int, Stack<BoxData>> boxStacks = new();
    private Dictionary<string, Vector3Int> boxLookup = new();
    private Dictionary<string, GameObject> visuals = new();

    // ── JSON data structures ──────────────────────────────────────────────────
    [System.Serializable]
    public class BoxData
    {
        public string id;
        public Vector3Int pos; // (x,y,z)
        public BoxData(string id, Vector3Int p) { this.id = id; pos = p; }
    }

    [System.Serializable]
    public class BoxJsonGrid { public int x; public int y; }

    [System.Serializable]
    public class BoxJsonData { public string id; public BoxJsonGrid grid; public int altitude; }

    [System.Serializable]
    public class BoxJsonRoot { public List<BoxJsonData> stored; }

    [System.Serializable]
    public class StoredJsonRoot { public List<string> taken = new(); }

    private HashSet<string> takenBoxIds = new();
    private BoxJsonRoot inventoryData = new(); // Keep in-memory for updates

    public List<(string boxId, Vector2Int from, Vector2Int to, bool isFinalTarget)> GetUnstackingTasks(string targetBoxId)
    {
        var tasks = new List<(string, Vector2Int, Vector2Int, bool)>();
        Vector3Int? pos = GetBoxPosition(targetBoxId);
        if (pos == null) return tasks;

        Vector2Int cell = new Vector2Int(pos.Value.x, pos.Value.y);
        if (!boxStacks.TryGetValue(cell, out var stack) || stack.Count == 0)
            return tasks;

        // Stack enumerates from top to bottom, so reverse to get bottom to top
        var stackList = new List<BoxData>(stack);
        stackList.Reverse(); // Now [0]=bottom, [^1]=top

        int targetIndex = stackList.FindIndex(b => b.id == targetBoxId);
        if (targetIndex == -1) return tasks;

        var cellsToExclude = new HashSet<Vector2Int> { cell };

        // Move all boxes above the target (higher index)
        for (int i = stackList.Count - 1; i > targetIndex; i--)
        {
            var box = stackList[i];
            Vector3Int? freeSpot = FindNearestFreeSpot(cell, cellsToExclude);
            if (freeSpot == null)
            {
                Debug.LogError("WAREHOUSE FULL! Cannot find a spot to move blocking boxes.");
                break; // No free spot, can't proceed
            }
            Vector2Int toCell = new Vector2Int(freeSpot.Value.x, freeSpot.Value.y);
            tasks.Add((box.id, cell, toCell, false));
        }

        // Finally, add the take task for the target box
        tasks.Add((targetBoxId, cell, cell, true));
        return tasks;
    }

    // ───────────────────────────────────────────────────────────────────────────
    void Start()
    {
        gridManager = FindAnyObjectByType<GridManager>();

        if (generateAtStart)
            FillWarehouseFromJson();
    }


    public void UpdateInventoryDataFromMemory()
    {
        if (inventoryData == null)
        {
            inventoryData = new BoxJsonRoot { stored = new List<BoxJsonData>() };
        }
        inventoryData.stored.Clear();

        // --- THE NEW, ROBUST LOGIC ---
        // Iterate through the master lookup dictionary. This is the simplest
        // and most reliable source of truth for every box's position.
        foreach (var boxEntry in boxLookup)
        {
            string boxId = boxEntry.Key;
            Vector3Int boxPosition = boxEntry.Value;

            inventoryData.stored.Add(new BoxJsonData
            {
                id = boxId,
                grid = new BoxJsonGrid { x = boxPosition.x, y = boxPosition.y },
                altitude = boxPosition.z
            });
        }
    }

    void OnApplicationQuit()
    {
        UpdateInventoryDataFromMemory();
        SaveInventoryJson();
    }



    public void FillWarehouseFromJson()
    {
        // Load taken box IDs from stored.json
        string storedPath = Path.Combine(Application.streamingAssetsPath, storedFileName);
        takenBoxIds = new HashSet<string>();
        if (File.Exists(storedPath))
        {
            string storedJson = File.ReadAllText(storedPath);
            var storedRoot = JsonConvert.DeserializeObject<StoredJsonRoot>(storedJson);
            if (storedRoot != null && storedRoot.taken != null)
                takenBoxIds = new HashSet<string>(storedRoot.taken);
        }

        // Load all box data from inventory.json
        string inventoryPath = Path.Combine(Application.streamingAssetsPath, boxDataFileName);
        if (!File.Exists(inventoryPath))
        {
            Debug.LogError($"Box data file not found: {inventoryPath}");
            return;
        }

        string json = File.ReadAllText(inventoryPath);
        inventoryData = JsonConvert.DeserializeObject<BoxJsonRoot>(json) ?? new BoxJsonRoot();


        int made = 0;
        foreach (var box in inventoryData.stored)
        {
            if (takenBoxIds.Contains(box.id))
                continue; // skip boxes already taken

            Vector3Int pos = new Vector3Int(box.grid.x, box.grid.y, box.altitude);
            if (AddBox(box.id, pos))
                made++;
        }
        Debug.Log($"📦 Loaded {made} boxes from JSON (excluding taken).");
    }

    public bool AddBox(string id, Vector3Int p)
    {
        Vector2Int cell = new(p.x, p.y);

        if (!boxStacks.ContainsKey(cell))
            boxStacks[cell] = new Stack<BoxData>();
        if (boxStacks[cell].Count >= maxStackHeight)
            return false;

        var data = new BoxData(id, p);
        boxStacks[cell].Push(data);
        boxLookup[id] = p;

        // visual
        if (boxPrefab)
        {
            GameObject go = Instantiate(boxPrefab, GridToWorld(p),
                                        Quaternion.identity, transform);
            go.name = id;
            visuals[id] = go;

            // use GetComponentInChildren instead of TryGetComponentInChildren
            TextMeshPro txt = go.GetComponentInChildren<TextMeshPro>();
            if (txt != null)
                txt.text = id;
        }
        return true;
    }

    /// <summary>
    /// Move the top box from one stack to another, update inventory.json.
    /// </summary>
    public bool MoveTopBox(Vector2Int fromCell, Vector2Int toCell)
    {
        if (!boxStacks.TryGetValue(fromCell, out var fromStack) || fromStack.Count == 0)
            return false;
        if (!boxStacks.ContainsKey(toCell))
            boxStacks[toCell] = new Stack<BoxData>();
        if (boxStacks[toCell].Count >= maxStackHeight)
            return false;

        BoxData box = fromStack.Pop();
        int newAltitude = boxStacks[toCell].Count;
        box.pos = new Vector3Int(toCell.x, toCell.y, newAltitude);
        boxLookup[box.id] = box.pos;
        boxStacks[toCell].Push(box);

        Debug.Log($"<color=cyan>BOX MANAGER: Moving box '{box.id}' FROM {fromCell} TO {toCell}. New position will be {box.pos}</color>");

        // Move visual
        if (visuals.TryGetValue(box.id, out var go))
            go.transform.position = GridToWorld(box.pos);

        SaveInventoryJson();

        return true;
    }


    public BoxData PeekTopBox(Vector2Int cell)
        => boxStacks.TryGetValue(cell, out var s) && s.Count > 0 ? s.Peek() : null;

    /// <summary>
    /// Remove the top box from a stack, mark as taken, update inventory.json and stored.json.
    /// </summary>
    public BoxData PopTopBox(Vector2Int cell)
    {
        if (!boxStacks.TryGetValue(cell, out var s) || s.Count == 0)
            return null;

        BoxData b = s.Pop();
        boxLookup.Remove(b.id);

        if (visuals.TryGetValue(b.id, out var go))
        {
            Destroy(go);
            visuals.Remove(b.id);
        }

        // Mark as taken and save
        MarkBoxAsTaken(b.id);

        SaveInventoryJson();

        return b;
    }

    // --- Inventory JSON helpers ---


    private void SaveInventoryJson()
    {
        // 1. Rebuild the data.
        UpdateInventoryDataFromMemory();

        // 2. Write the data to the file.
        string inventoryPath = Path.Combine(Application.streamingAssetsPath, boxDataFileName);
        string json = JsonConvert.SerializeObject(inventoryData, Formatting.Indented);
        File.WriteAllText(inventoryPath, json);

        // 3. --- THE CRITICAL FIX ---
        // Tell the Unity Editor to re-import the file we just changed.
        #if UNITY_EDITOR
        UnityEditor.AssetDatabase.Refresh();
        #endif
    }

    public List<string> GetAllBoxIds()
    {
        // Return a new list of all the keys from the boxLookup dictionary
        return new List<string>(boxLookup.Keys);
    }
    private void MarkBoxAsTaken(string id)
    {
        takenBoxIds.Add(id);
        SaveTakenBoxes();
    }

    private void SaveTakenBoxes()
    {
        var storedRoot = new StoredJsonRoot { taken = new List<string>(takenBoxIds) };
        string storedPath = Path.Combine(Application.streamingAssetsPath, storedFileName);
        string json = JsonConvert.SerializeObject(storedRoot, Formatting.Indented);
        File.WriteAllText(storedPath, json);
        Debug.Log($"💾 Updated stored.json: {takenBoxIds.Count} taken");

        // --- ALSO ADD THE FIX HERE FOR CONSISTENCY ---
        #if UNITY_EDITOR
        UnityEditor.AssetDatabase.Refresh();
        #endif
    }

    public Vector3Int? GetBoxPosition(string id)
        => boxLookup.TryGetValue(id, out var p) ? p : null;

    public Dictionary<string, Vector3Int> GetAllBoxes()
        => new(boxLookup);

    // ------------------------------------------------------------------------ //
    //  FREE-SPOT FINDER (used by ManipulatorController)                        //
    // ------------------------------------------------------------------------ //
    // In BoxManager.cs
    // REPLACE the entire FindNearestFreeSpot method with this version.

    public Vector3Int? FindNearestFreeSpot(Vector2Int from, HashSet<Vector2Int> excludeCells = null)
    {
        // --- THE FIX IS HERE ---
        // First, determine if the 'from' cell itself is excluded.
        bool isFromCellExcluded = excludeCells != null && excludeCells.Contains(from);

        // Now, check radius 0 (the current stack), but ONLY if it's NOT excluded.
        if (!isFromCellExcluded && boxStacks.ContainsKey(from) && boxStacks[from].Count < maxStackHeight)
        {
            // This was the source of the bug. It will no longer run if 'from' is excluded.
            return new Vector3Int(from.x, from.y, boxStacks[from].Count);
        }

        // If the 'from' cell was excluded or full, proceed with the search in expanding rings.
        int maxSearchRadius = Mathf.Max(width, height);

        // The rest of this logic is for searching outwards.
        for (int r = 1; r < maxSearchRadius; r++)
        {
            for (int dx = -r; dx <= r; dx++)
            {
                for (int dy = -r; dy <= r; dy++)
                {
                    // Only check the perimeter of the square
                    if (Mathf.Abs(dx) != r && Mathf.Abs(dy) != r) continue;

                    var c = new Vector2Int(from.x + dx, from.y + dy);

                    // Check if the candidate cell is excluded
                    if (excludeCells != null && excludeCells.Contains(c))
                    {
                        continue;
                    }

                    if (InsideGrid(c))
                    {
                        int stackHeight = boxStacks.ContainsKey(c) ? boxStacks[c].Count : 0;
                        if (stackHeight < maxStackHeight)
                        {
                            // Found the closest possible free spot in a different stack. Return it.
                            return new Vector3Int(c.x, c.y, stackHeight);
                        }
                    }
                }
            }
        }

        Debug.LogWarning("Could not find any free spot in the entire warehouse.");
        return null; // No spot found anywhere
    }

    // ------------------------------------------------------------------------ //
    //  HELPERS                                                                 //
    // ------------------------------------------------------------------------ //
    private bool InsideGrid(Vector2Int c)
    {
        // This is the key change. Now, BoxManager asks the master GridManager
        // if a coordinate is valid in the world, instead of using its own rules.
        if (gridManager == null) return false; // Safety check
        return gridManager.IsInsideGrid(c);
    }

    private Vector3 GridToWorld(Vector3Int p)
    {
        Vector3 basePos = gridManager
            ? gridManager.GridToWorld(p.x, p.y)
            : new Vector3(p.x + 0.5f, 0f, p.y + 0.5f);

        float boxSize = 1f;
        basePos.y = boxBasePlane.position.y + p.z * boxSize + boxSize / 2f;
        return basePos;
    }
}
