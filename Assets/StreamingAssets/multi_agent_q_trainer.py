import random
import json
import os

# === Settings ===
NUM_AGENTS = 3
GRID_WIDTH = 10
GRID_HEIGHT = 10
EPISODES_PER_PAIR = 1000
MAX_STEPS = 100

ALPHA = 0.05    # Learning rate
GAMMA = 0.95   # Discount factor

EPSILON_START = 1.0
EPSILON_END = 0.01
EPSILON_DECAY = 0.9995

ACTIONS = [0, 1, 2, 3]  # [UP, DOWN, LEFT, RIGHT]
q_table = {}

def get_key(agent_id, sx, sy, gx, gy):
    return f"{agent_id}_{sx}_{sy}_{gx}_{gy}"

def get_next_position(x, y, action):
    if action == 0: y += 1
    elif action == 1: y -= 1
    elif action == 2: x -= 1
    elif action == 3: x += 1
    return max(0, min(GRID_WIDTH - 1, x)), max(0, min(GRID_HEIGHT - 1, y))

# === Training ===
for agent_id in range(NUM_AGENTS):
    print(f"🔁 Training agent {agent_id}...")
    epsilon = EPSILON_START

    for sx in range(GRID_WIDTH):
        for sy in range(GRID_HEIGHT):
            for gx in range(GRID_WIDTH):
                for gy in range(GRID_HEIGHT):
                    if (sx, sy) == (gx, gy):
                        continue

                    for ep in range(EPISODES_PER_PAIR):
                        x, y = sx, sy

                        for step in range(MAX_STEPS):
                            state_key = get_key(agent_id, x, y, gx, gy)
                            q_table.setdefault(state_key, [0.0] * 4)

                            # Epsilon-greedy action selection
                            if random.random() < epsilon:
                                action = random.choice(ACTIONS)
                            else:
                                action = max(range(4), key=lambda a: q_table[state_key][a])

                            nx, ny = get_next_position(x, y, action)
                            next_key = get_key(agent_id, nx, ny, gx, gy)
                            q_table.setdefault(next_key, [0.0] * 4)

                            reward = 1.0 if (nx, ny) == (gx, gy) else -0.01

                            old_q = q_table[state_key][action]
                            max_next_q = max(q_table[next_key])
                            q_table[state_key][action] = old_q + ALPHA * (reward + GAMMA * max_next_q - old_q)

                            x, y = nx, ny
                            if (x, y) == (gx, gy):
                                break

                        # Decay epsilon per episode
                        epsilon = max(EPSILON_END, epsilon * EPSILON_DECAY)

    print(f"✅ Agent {agent_id} training complete.")

# === Save Q-table ===
output_path = os.path.join("Assets", "StreamingAssets", "qtable.json")
os.makedirs(os.path.dirname(output_path), exist_ok=True)

with open(output_path, "w") as f:
    json.dump(q_table, f, indent=2)

print(f"\n✅ All training complete. Saved {len(q_table)} entries to {output_path}")
