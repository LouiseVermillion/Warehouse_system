import random
import json
import os

GRID_WIDTH = 10
GRID_HEIGHT = 10
EPISODES_PER_PAIR = 50
MAX_STEPS = 100

ALPHA = 0.1    # Learning rate
GAMMA = 0.95   # Discount factor
EPSILON = 0.1  # Exploration chance

ACTIONS = [0, 1, 2, 3]  # [UP, DOWN, LEFT, RIGHT]
q_table = {}            # Map of: "agentId_sx_sy_gx_gy" -> [Q_up, Q_down, Q_left, Q_right]

def get_state_key(agent_id, sx, sy, gx, gy):
    return f"{agent_id}_{sx}_{sy}_{gx}_{gy}"

def get_next_position(x, y, action):
    # 0=UP, 1=DOWN, 2=LEFT, 3=RIGHT
    if action == 0:
        y += 1
    elif action == 1:
        y -= 1
    elif action == 2:
        x -= 1
    elif action == 3:
        x += 1

    # Clamp to grid boundaries
    x = max(0, min(GRID_WIDTH - 1, x))
    y = max(0, min(GRID_HEIGHT - 1, y))
    return x, y

def ensure_q_entry(key):
    """Make sure q_table has an entry for this state key."""
    if key not in q_table:
        q_table[key] = [0.0] * len(ACTIONS)

def main():
    agent_id = 0

    # Train for every (start, goal) pair
    for start_x in range(GRID_WIDTH):
        for start_y in range(GRID_HEIGHT):
            for goal_x in range(GRID_WIDTH):
                for goal_y in range(GRID_HEIGHT):
                    # Skip trivial case of same cell
                    if (start_x, start_y) == (goal_x, goal_y):
                        continue

                    # Multiple episodes per pair
                    for episode in range(EPISODES_PER_PAIR):
                        x, y = start_x, start_y

                        for step in range(MAX_STEPS):
                            state_key = get_state_key(agent_id, x, y, goal_x, goal_y)
                            ensure_q_entry(state_key)

                            # Epsilon-greedy selection
                            if random.random() < EPSILON:
                                action = random.choice(ACTIONS)
                            else:
                                q_values = q_table[state_key]
                                action = max(range(len(q_values)), key=lambda i: q_values[i])

                            nx, ny = get_next_position(x, y, action)
                            next_state_key = get_state_key(agent_id, nx, ny, goal_x, goal_y)
                            ensure_q_entry(next_state_key)

                            # Simple reward logic
                            if (nx, ny) == (goal_x, goal_y):
                                reward = 1.0
                            else:
                                reward = -0.01

                            # Q-update
                            old_q = q_table[state_key][action]
                            max_next_q = max(q_table[next_state_key])
                            new_q = old_q + ALPHA * (reward + GAMMA * max_next_q - old_q)
                            q_table[state_key][action] = new_q

                            # Move agent
                            x, y = nx, ny

                            if (x, y) == (goal_x, goal_y):
                                # Reached goal, end episode
                                break

    # Save Q-table in Unity's StreamingAssets folder
    filename = os.path.join("Assets", "StreamingAssets", "qtable.json")
    os.makedirs(os.path.dirname(filename), exist_ok=True)

    with open(filename, "w") as f:
        json.dump(q_table, f, indent=2)

    print(f"Training complete. Q-table saved to: {filename}")

if __name__ == "__main__":
    main()
