using UnityEngine;
using System.Collections.Generic;
using System.Linq; // Required for Shuffle
using System.IO;

public class PerformanceTester : MonoBehaviour
{

    [Header("Simulation Settings")] // A new header for clarity
    [Tooltip("The number of random tasks to run. Set to 0 to run all tasks.")]
    public int taskLimit = 20;

    [Header("References")]
    public BoxManager boxManager;
    public EnvironmentManager envManager;
    public TaskManagerUI taskManagerUI; // We'll use its FindNearestIdleAgent logic

    // --- Private State ---
    private bool isRunning = false;
    private Queue<string> masterTaskQueue;

    // --- Performance Metrics ---
    private float startTime;
    private float totalCompletionTime;
    private int tasksCompleted;
    private int totalDistanceTraveled;
    private int rlMoves;
    private int aStarMoves;

    // A public property to allow other scripts to check if a simulation is active
    public bool IsRunning => isRunning;

    public void StartSimulation()
    {
        Debug.Log("<color=green><b>BUTTON CLICKED! StartSimulation method was called.</b></color>"); // ADD THIS LINE
        if (isRunning)
        {
            Debug.LogWarning("Simulation is already running!");
            return;
        }

        Debug.Log("=============== 🚀 STARTING PERFORMANCE TEST 🚀 ===============");

        // 1. Reset metrics
        isRunning = true;
        startTime = Time.time;
        totalCompletionTime = 0;
        tasksCompleted = 0;
        totalDistanceTraveled = 0;
        rlMoves = 0;
        aStarMoves = 0;

        // 2. Get and shuffle tasks
        List<string> allBoxIds = boxManager.GetAllBoxIds();
        // Simple shuffle algorithm
        var rnd = new System.Random();
        var shuffledTasks = allBoxIds.OrderBy(item => rnd.Next());

        if (taskLimit > 0)
        {
            masterTaskQueue = new Queue<string>(shuffledTasks.Take(taskLimit));
        }
        else // If taskLimit is 0, use all tasks
        {
            masterTaskQueue = new Queue<string>(shuffledTasks);
        }
        // --- END OF CHANGE ---

        Debug.Log($"Generated {masterTaskQueue.Count} tasks for the simulation.");

        // 3. Kick off initial tasks
        AssignAvailableTasks();
    }

    void Update()
    {
        if (!isRunning) return;

        // Check for end condition
        if (masterTaskQueue.Count == 0 && AreAllAgentsIdle())
        {
            EndSimulation();
        }
    }

    private void EndSimulation()
    {
        isRunning = false;
        totalCompletionTime = Time.time - startTime;

        // Print results to the console AND save them to a file
        PrintResults();
        SaveResultsToFile();
    }


    private void PrintResults()
    {
        Debug.Log("=============== ✅ PERFORMANCE TEST COMPLETE ✅ ===============");
        Debug.Log($"<b>Total Simulation Time:</b> {totalCompletionTime:F2} seconds");
        Debug.Log($"<b>Total Tasks Completed:</b> {tasksCompleted}");
        float throughput = (totalCompletionTime > 0) ? tasksCompleted / (totalCompletionTime / 60f) : 0;
        Debug.Log($"<b>Throughput:</b> {throughput:F2} tasks per minute");
        Debug.Log($"<b>Total Agent Distance Traveled:</b> {totalDistanceTraveled} units");
        Debug.Log($"<b>RL Moves:</b> {rlMoves}");
        Debug.Log($"<b>A* Fallback Moves:</b> {aStarMoves}");
        float rlRatio = (rlMoves + aStarMoves > 0) ? (float)rlMoves / (rlMoves + aStarMoves) * 100f : 100f;
        Debug.Log($"<b>RL Success Rate:</b> {rlRatio:F2}%");
        Debug.Log("=============================================================");
    }

    // This will be called by EnvironmentManager when a task is finished
    public void OnTaskCompleted()
    {
        tasksCompleted++;
        AssignAvailableTasks(); // Try to assign a new task
    }

    private void AssignAvailableTasks()
    {
        // Keep assigning tasks as long as there are tasks in the queue AND there are idle agents
        while (masterTaskQueue.Count > 0)
        {
            // Find the best idle agent for the NEXT task in the queue
            Vector3Int? boxPos = boxManager.GetBoxPosition(masterTaskQueue.Peek());
            if (!boxPos.HasValue)
            {
                Debug.LogWarning($"Could not find position for box {masterTaskQueue.Peek()}. Removing from queue.");
                masterTaskQueue.Dequeue(); // Remove invalid task
                continue; // Try the next task
            }

            RobotArmAgent agent = taskManagerUI.FindNearestIdleAgent(new Vector2Int(boxPos.Value.x, boxPos.Value.y));

            if (agent != null)
            {
                string boxId = masterTaskQueue.Dequeue();
                Debug.Log($"<color=yellow>Assigning task {boxId} to Agent {agent.agentId}</color>");

                var unstackingTasks = boxManager.GetUnstackingTasks(boxId);
                taskManagerUI.pendingTasks = new Queue<(string, Vector2Int, Vector2Int, bool)>(unstackingTasks);
                taskManagerUI.AssignNextPendingTask();
            }
            else
            {
                break;
            }
        }
    }

    private bool AreAllAgentsIdle()
    {
        foreach (var agent in envManager.agents)
        {
            if (agent.IsBusy()) return false;
        }
        return true;
    }

    public void RecordMove(bool wasRL) { if (wasRL) rlMoves++; else aStarMoves++; }
    public void RecordDistance(int distance) { totalDistanceTraveled += distance; }

    private void SaveResultsToFile()
    {
        // 1. Create the result object with the final data
        SimulationResult result = new SimulationResult
        {
            timestamp = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            totalSimulationTime = this.totalCompletionTime,
            tasksCompleted = this.tasksCompleted,
            throughput = (totalCompletionTime > 0) ? tasksCompleted / (totalCompletionTime / 60f) : 0,
            totalDistanceTraveled = this.totalDistanceTraveled,
            rlMoves = this.rlMoves,
            aStarMoves = this.aStarMoves,
            rlSuccessRate = (rlMoves + aStarMoves > 0) ? (float)rlMoves / (rlMoves + aStarMoves) * 100f : 100f
        };

        // 2. Define the path and file name
        string path = Application.persistentDataPath;
        // We'll name the file with a timestamp to ensure every test run gets its own unique file
        string fileName = $"PerformanceResult_{System.DateTime.Now:yyyyMMdd_HHmmss}.json";
        string fullPath = Path.Combine(path, fileName);

        // 3. Convert the object to a JSON string
        string json = JsonUtility.ToJson(result, true); // 'true' for pretty print

        // 4. Write the file
        File.WriteAllText(fullPath, json);

        Debug.Log($"<color=lime><b>📈 Results saved to: {fullPath}</b></color>");
    }


}

[System.Serializable]
public class SimulationResult
{
    public string timestamp;
    public float totalSimulationTime;
    public int tasksCompleted;
    public float throughput; // tasks per minute
    public int totalDistanceTraveled;
    public int rlMoves;
    public int aStarMoves;
    public float rlSuccessRate;
}