using UnityEngine;
using System.Collections;
using System.Collections.Generic;


public enum AgentState
{
    Idle,
    MovingToPickup,
    MovingToDropoff,
    MovingToDelivery,
    PickingUpFinalBox,
    MovingToSafeSpot
}

public class RobotArmAgent : MonoBehaviour
{

    public AgentState CurrentState { get; private set; } = AgentState.Idle;
    private string _task_boxId;
    private Vector2Int _task_fromPos;
    private Vector2Int _task_toPos;
    private AgentState _originalState;
    private Vector2Int? _originalTarget;

    public int agentId;
    public Vector2Int gridPosition;
    public Vector2Int? targetPosition = null;
    public float moveSpeed = 2f;
    public float waitDurationAtGoal = 2f;
    [Header("Deadlock Detection")]
    public int deadlockStuckThreshold = 5; // How many non-progress moves before being declared stuck.
    
    [HideInInspector] public int consecutiveNoProgressMoves = 0;
    [HideInInspector] public float lastDistanceToGoal = float.MaxValue;
    
    

    [HideInInspector] public EnvironmentManager envManager;

    private PerformanceTester performanceTester;

    private BoxManager boxManager;
    private Queue<Vector2Int> recentPositions = new();
    private const int loopMemory = 5;
    private GridManager gridManager;
    private bool isMoving = false;
    private AStarPathfinder pathfinder;

    void Start()
    {
        gridManager = FindAnyObjectByType<GridManager>();
        // This line is crucial for the deadlock logic. Make sure it's here.
        boxManager = FindAnyObjectByType<BoxManager>();
        transform.position = gridManager.GridToWorld(gridPosition.x, gridPosition.y);
        pathfinder = new AStarPathfinder(gridManager);
        performanceTester = FindAnyObjectByType<PerformanceTester>();
    }

    void Update()
    {
        if (!isMoving && targetPosition.HasValue && gridPosition == targetPosition.Value)
        {
            if (CurrentState == AgentState.MovingToSafeSpot)
            {
                Debug.Log($"<color=green>Agent {agentId} has reached safe spot. Resuming original task.</color>");
                CurrentState = _originalState;
                targetPosition = _originalTarget;
                consecutiveNoProgressMoves = 0;
                lastDistanceToGoal = float.MaxValue;
                return;
            }

            switch (CurrentState)
            {
                case AgentState.MovingToPickup:
                    Debug.Log($"🤖 Agent {agentId} arrived at pickup location {_task_fromPos}");
                    targetPosition = _task_toPos;
                    CurrentState = AgentState.MovingToDropoff;
                    break;
                case AgentState.MovingToDropoff:
                    Debug.Log($"<color=orange>📦 Agent {agentId} moved box {_task_boxId} to {_task_toPos}</color>");
                    boxManager.MoveTopBox(_task_fromPos, _task_toPos);
                    CompleteTask();
                    break;
                case AgentState.PickingUpFinalBox:
                    Debug.Log($"🤖 Agent {agentId} arrived to pick up final box at {_task_fromPos}");
                    var topBox = boxManager.PeekTopBox(_task_fromPos);
                    if (topBox != null && topBox.id == _task_boxId)
                    {
                        targetPosition = FindNearestEdgePoint(gridPosition);
                        CurrentState = AgentState.MovingToDelivery;
                        Debug.Log($"<color=cyan>🤖 Agent {agentId} picked up {_task_boxId}, heading to delivery point {targetPosition.Value}</color>");
                    }
                    else
                    {
                        Debug.LogWarning($"❌ Agent {agentId} tried to take box {_task_boxId} from {_task_fromPos}, but it was not on top!");
                        CompleteTask();
                    }
                    break;
                case AgentState.MovingToDelivery:
                    Debug.Log($"<color=lime><b>✅ Agent {agentId} DELIVERED box {_task_boxId} at edge {gridPosition}</b></color>");
                    boxManager.PopTopBox(_task_fromPos);
                    CompleteTask();
                    break;
            }
        }
    }

    public void MoveToSafeSpotToBreakDeadlock()
    {
        _originalState = this.CurrentState;
        _originalTarget = this.targetPosition;

        HashSet<Vector2Int> exclusionSet = new HashSet<Vector2Int>();
        foreach (var agent in envManager.agents)
        {
            exclusionSet.Add(agent.gridPosition);
        }
        if (_originalTarget.HasValue)
        {
            exclusionSet.Add(_originalTarget.Value);
        }

        Vector3Int? safeSpot = boxManager.FindNearestFreeSpot(this.gridPosition, exclusionSet);

        if (safeSpot.HasValue)
        {

            CurrentState = AgentState.MovingToSafeSpot;
            targetPosition = new Vector2Int(safeSpot.Value.x, safeSpot.Value.y);
            Debug.LogWarning($"<color=red>DEADLOCK BREAK! Agent {agentId} is moving to safe spot {targetPosition.Value} to clear traffic.</color>");
        }
        else
        {

            Debug.LogError($"<color=red>DEADLOCK CATASTROPHE! Agent {agentId} is stuck but no safe spot was found!</color>");

        }

        consecutiveNoProgressMoves = 0;
        lastDistanceToGoal = float.MaxValue;
    }

    private Vector2Int FindNearestEdgePoint(Vector2Int fromPos)
    {
        int gridWidth = gridManager.gridSize.x;
        int gridHeight = gridManager.gridSize.y;

        // Distances to the four edges
        int distToLeft = fromPos.x;
        int distToRight = (gridWidth - 1) - fromPos.x;
        int distToBottom = fromPos.y;
        int distToTop = (gridHeight - 1) - fromPos.y;

        // Find the minimum of these four distances
        int minDistance = Mathf.Min(distToLeft, distToRight, distToBottom, distToTop);

        // Return the coordinate on the closest edge
        if (minDistance == distToLeft) return new Vector2Int(0, fromPos.y);
        if (minDistance == distToRight) return new Vector2Int(gridWidth - 1, fromPos.y);
        if (minDistance == distToBottom) return new Vector2Int(fromPos.x, 0);
        // Otherwise, it must be the top edge
        return new Vector2Int(fromPos.x, gridHeight - 1);
    }

    private void CompleteTask()
    {
        Debug.Log($"🎉 Agent {agentId} has completed its task.");
        targetPosition = null;
        CurrentState = AgentState.Idle;
        _task_boxId = null;

        // Notify the environment/task manager that this agent is free
        if (envManager != null)
        {
            envManager.OnAgentTaskCompleted(this);
        }
    }


    public bool IsBusy()
    {
        // An agent is busy if it's not idle. Simple!
        return CurrentState != AgentState.Idle;
    }


    public Vector2Int? GetNextDesiredPosition()
    {
        // If we have no target or are already moving, do nothing.
        if (isMoving || targetPosition == null)
        {
            return null;
        }

        // If we are at our destination, the Update() loop will handle the logic.
        if (gridPosition == targetPosition)
        {
            return null;
        }

        // ─── RL/A* Pathfinding ───────────────────────
        int rlAction = envManager.qTableManager.GetBestAction(gridPosition, targetPosition.Value);
        if (rlAction >= 0)
        {
            Vector2Int next = gridPosition + ActionToDirection(rlAction);
            if (gridManager.IsInsideGrid(next) && !envManager.IsOccupied(next) && !recentPositions.Contains(next))
            {
                // Debug.Log($"🧠 Agent {agentId} RL → Action {rlAction} → {next}");
                if (performanceTester != null && performanceTester.IsRunning) performanceTester.RecordMove(true);
                return next;
            }
            else
            {
                // Debug.Log($"🔄 Agent {agentId} RL blocked at {next}, falling back to A*");
            }
        }

        Vector2Int goal = targetPosition.Value;
        HashSet<Vector2Int> occupied = new HashSet<Vector2Int>();
        foreach (var other in envManager.agents)
        {
            if (other != this)
                occupied.Add(other.gridPosition);
        }

        List<Vector2Int> path = pathfinder.FindPath(gridPosition, goal, occupied);
        if (path != null && path.Count > 1)
        {
            Vector2Int nextStep = path[1];
            // Debug.Log($"🧭 Agent {agentId} A* → Next: {nextStep}");
            if (performanceTester != null && performanceTester.IsRunning) performanceTester.RecordMove(false);
            return nextStep;
        }

        Debug.LogWarning($"⚠️ Agent {agentId} could not find path to {goal}");
        return null;
    }

    public void AssignBoxTask(Vector3Int boxPos, string boxId)
    {
        // This method simply converts the Vector3Int to a Vector2Int
        // and calls the main task assignment method.
        AssignTakeBoxTask(boxId, new Vector2Int(boxPos.x, boxPos.y));
    }

    public void AssignMoveBoxTask(string boxId, Vector2Int from, Vector2Int to)
    {
        Debug.Log($"✅ Agent {agentId} assigned MOVE task for {boxId} from {from} to {to}");
        _task_boxId = boxId;
        _task_fromPos = from;
        _task_toPos = to;
        targetPosition = from; // The first destination is the pickup location
        CurrentState = AgentState.MovingToPickup;

        lastDistanceToGoal = float.MaxValue;
        consecutiveNoProgressMoves = 0;
    }

    public void AssignTakeBoxTask(string boxId, Vector2Int from)
    {
        Debug.Log($"✅ Agent {agentId} assigned TAKE task for {boxId} from {from}");
        _task_boxId = boxId;
        _task_fromPos = from;
        targetPosition = from; // The destination is the box location
        CurrentState = AgentState.PickingUpFinalBox;

        lastDistanceToGoal = float.MaxValue;
        consecutiveNoProgressMoves = 0;
    }



    public void ApplyMove(Vector2Int nextPos)
    {

        if (performanceTester != null && performanceTester.IsRunning) performanceTester.RecordDistance(1); // REPORT DISTANCE
        gridPosition = nextPos;

        gridPosition = nextPos;
        StartCoroutine(MoveToPosition(gridManager.GridToWorld(nextPos.x, nextPos.y)));

        recentPositions.Enqueue(gridPosition);
        if (recentPositions.Count > loopMemory)
            recentPositions.Dequeue();
    }

    private Vector2Int ActionToDirection(int action)
    {
        return action switch
        {
            0 => Vector2Int.up,
            1 => Vector2Int.down,
            2 => Vector2Int.left,
            3 => Vector2Int.right,
            _ => Vector2Int.zero,
        };
    }
 

    private IEnumerator MoveToPosition(Vector3 target)
    {
        isMoving = true;
        while (Vector3.Distance(transform.position, target) > 0.01f)
        {
            transform.position = Vector3.MoveTowards(
                transform.position, target, moveSpeed * Time.deltaTime
            );
            yield return null;
        }
        transform.position = target;
        isMoving = false;
    }
}
