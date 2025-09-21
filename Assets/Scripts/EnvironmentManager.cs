using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class EnvironmentManager : MonoBehaviour
{
    public List<RobotArmAgent> agents = new List<RobotArmAgent>();
    public GridManager gridManager;
    public float stepInterval = 1f;
    public TaskManagerUI taskManagerUI;

    [HideInInspector] public QTableManager qTableManager;

    private PerformanceTester performanceTester;

    private Dictionary<Vector2Int, RobotArmAgent> inProgressTasks = new(); // goal → agent
    private TrainerConnector trainerConnector;
    private Queue<Vector2Int> taskQueue = new();
    private float stepTimer = 0f;
    private float Distance(Vector2Int a, Vector2Int b)
    {
        return Vector2Int.Distance(a, b);
    }


    void Start()
    {
        gridManager = Object.FindFirstObjectByType<GridManager>();
        qTableManager = GetComponent<QTableManager>();
        trainerConnector = GetComponent<TrainerConnector>();

        RobotArmAgent[] foundAgents = Object.FindObjectsByType<RobotArmAgent>(FindObjectsSortMode.None);
        foreach (var agent in foundAgents)
        {
            agents.Add(agent);
            agent.envManager = this;
        }

        performanceTester = FindAnyObjectByType<PerformanceTester>();
    }

    void Update()
    {
        stepTimer += Time.deltaTime;
        if (stepTimer >= stepInterval)
        {
            stepTimer = 0f;

            // Phase 1: Attempt to move agents and resolve immediate collisions.
            StepAllAgentsWithCollisionAvoidance();

            // Phase 2: The Supervisor checks for progress and resolves persistent deadlocks/loops.
            CheckForAndResolveDeadlocks();

            // This can remain as is.
            AssignPendingTasks();
        }
    }

    void StepAllAgentsWithCollisionAvoidance()
    {
        var proposedMoves = new Dictionary<RobotArmAgent, Vector2Int>();
        foreach (var agent in agents)
        {
            if (agent.IsBusy())
            {
                Vector2Int? desiredStep = agent.GetNextDesiredPosition();
                if (desiredStep.HasValue)
                {
                    proposedMoves[agent] = desiredStep.Value;
                }
            }
        }


        var approvedMoves = new Dictionary<RobotArmAgent, Vector2Int>();
        var reservedDestinations = new HashSet<Vector2Int>();
        var sortedProposals = proposedMoves.OrderBy(p => p.Key.agentId).ToList();

        foreach (var proposal in sortedProposals)
        {
            RobotArmAgent agent = proposal.Key;
            Vector2Int destination = proposal.Value;

            // Conflict A: Is another agent trying to move to the SAME destination?
            if (reservedDestinations.Contains(destination))
            {
                // Conflict! Someone already claimed this spot. This agent must wait.
                Debug.Log($"<color=orange>Conflict: Agent {agent.agentId} wanted {destination}, but it was already claimed. Waiting.</color>");
                continue; // This agent does not get to move.
            }

            // Conflict B: Is this agent trying to "swap places" with another agent?
            bool isSwapping = false;
            foreach (var otherProposal in sortedProposals)
            {
                RobotArmAgent otherAgent = otherProposal.Key;
                Vector2Int otherDestination = otherProposal.Value;
                if (agent == otherAgent) continue;

                // If the other agent's destination is my current spot, AND my destination is the other agent's current spot...
                if (otherDestination == agent.gridPosition && destination == otherAgent.gridPosition)
                {
                    isSwapping = true;
                    break;
                }
            }

            if (isSwapping)
            {
                // To resolve, we let only one agent move. Because we sorted the list by agentId,
                // the agent with the lower ID will have its proposal processed first and will succeed.
                // The agent with the higher ID will see this check as true and will be forced to wait.
                // This implicitly handles the tie-break.
                Debug.Log($"<color=orange>Conflict: Agent {agent.agentId} is trying to swap places. Waiting.</color>");
                continue;
            }

            // If we passed all checks, the move is approved.
            approvedMoves[agent] = destination;
            reservedDestinations.Add(destination);
        }

        // --- Step 3: Apply Approved Moves ---
        // Only the non-conflicting moves are executed.
        foreach (var move in approvedMoves)
        {
            move.Key.ApplyMove(move.Value);
        }
    }

    private void CheckForAndResolveDeadlocks()
    {
        foreach (var agent in agents)
        {
            if (!agent.IsBusy() || agent.CurrentState == AgentState.MovingToSafeSpot || agent.targetPosition == null)
            {
                agent.consecutiveNoProgressMoves = 0;
                agent.lastDistanceToGoal = float.MaxValue;
                continue;
            }

            float currentDistance = Vector2.Distance(agent.gridPosition, agent.targetPosition.Value);

            if (currentDistance < agent.lastDistanceToGoal - 0.01f)
            {
                agent.lastDistanceToGoal = currentDistance;
                agent.consecutiveNoProgressMoves = 0;
            }
            else
            {
                agent.consecutiveNoProgressMoves++;
            }

            if (agent.consecutiveNoProgressMoves > agent.deadlockStuckThreshold)
            {
                agent.MoveToSafeSpotToBreakDeadlock();
            }
        }
    }

    public void EnqueueTask(Vector2Int target)
    {
        taskQueue.Enqueue(target);
        Debug.Log($"📥 Task added to queue: {target}");
    }

    public void AssignPendingTasks()
    {
        Queue<Vector2Int> unassigned = new(taskQueue);
        taskQueue.Clear();

        foreach (var goal in unassigned)
        {
            RobotArmAgent current = inProgressTasks.ContainsKey(goal) ? inProgressTasks[goal] : null;
            RobotArmAgent better = FindNearestIdleAgent(goal);

            if (better != null && (current == null || Distance(better.gridPosition, goal) < Distance(current.gridPosition, goal)))
            {
                if (current != null)
                {
                    Debug.Log($"🔄 Task at {goal} reassigned from Agent {current.agentId} → Agent {better.agentId}");
                    current.targetPosition = null;
                }

                better.targetPosition = goal;
                inProgressTasks[goal] = better;
            }
            else
            {
                // Keep in queue if no better or no available
                taskQueue.Enqueue(goal);
            }
        }
    }


    public void OnAgentTaskCompleted(RobotArmAgent agent)
    {
        // If a performance test is running, let it handle the logic
        if (performanceTester != null && performanceTester.IsRunning)
        {
            performanceTester.OnTaskCompleted();
        }
        else if (taskManagerUI != null)
            taskManagerUI.AssignNextPendingTask();
    }


    private RobotArmAgent FindNearestIdleAgent(Vector2Int target)
    {
        RobotArmAgent closest = null;
        float bestDistance = float.MaxValue;

        foreach (var agent in agents)
        {
            if (agent.targetPosition != null || agent.IsBusy())
                continue;

            float dist = Vector2Int.Distance(agent.gridPosition, target);
            if (dist < bestDistance)
            {
                bestDistance = dist;
                closest = agent;
            }
        }

        return closest;
    }

    public int GetActionForAgent(Vector2Int currentPos, Vector2Int goalPos)
    {
        return qTableManager.GetBestAction(currentPos, goalPos);
    }

public bool IsOccupied(Vector2Int position)
    {
        foreach (var agent in agents)
        {
            if (agent.gridPosition == position)
                return true;
        }
        return false;
    }

    public Queue<Vector2Int> GetTaskQueue()
    {
        return new Queue<Vector2Int>(taskQueue); // return copy
    }
}
