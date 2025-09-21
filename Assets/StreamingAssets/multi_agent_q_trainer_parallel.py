import random
import json
import os
import multiprocessing

# === Settings ===
NUM_AGENTS = 3
GRID_WIDTH = 10
GRID_HEIGHT = 10
EPISODES_PER_PAIR = 2000
MAX_STEPS = 100

ALPHA = 0.02
GAMMA = 0.95
REWARD_STEP = -0.1

EPSILON_START = 1.0
EPSILON_END = 0.05
EPSILON_DECAY = 0.9995

ACTIONS = [0, 1, 2, 3]

def get_key(agent_id, x, y, gx, gy):
    return f"{agent_id}_{x}_{y}_{gx}_{gy}"

def get_next_position(x, y, action):
    if action == 0: y += 1
    elif action == 1: y -= 1
    elif action == 2: x -= 1
    elif action == 3: x += 1
    return max(0, min(GRID_WIDTH - 1, x)), max(0, min(GRID_HEIGHT - 1, y))

def train_agent(agent_id):
    q_table = {}
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
                            key = get_key(agent_id, x, y, gx, gy)
                            q_table.setdefault(key, [0.0] * 4)

                            # Epsilon-greedy
                            if random.random() < epsilon:
                                action = random.choice(ACTIONS)
                            else:
                                action = max(range(4), key=lambda a: q_table[key][a])

                            nx, ny = get_next_position(x, y, action)
                            next_key = get_key(agent_id, nx, ny, gx, gy)
                            q_table.setdefault(next_key, [0.0] * 4)

                            reward = 1.0 if (nx, ny) == (gx, gy) else REWARD_STEP

                            old_q = q_table[key][action]
                            max_next_q = max(q_table[next_key])
                            q_table[key][action] = old_q + ALPHA * (reward + GAMMA * max_next_q - old_q)

                            x, y = nx, ny
                            if (x, y) == (gx, gy):
                                break

                        epsilon = max(EPSILON_END, epsilon * EPSILON_DECAY)

    print(f"✅ Agent {agent_id} done. Entries: {len(q_table)}")
    return q_table

def merge_q_tables(q_tables):
    final = {}
    for partial in q_tables:
        final.update(partial)
    return final

if __name__ == "__main__":
    print(f"🚀 Starting multi-agent training on {NUM_AGENTS} agents...")

    with multiprocessing.Pool(processes=NUM_AGENTS) as pool:
        results = pool.map(train_agent, range(NUM_AGENTS))

    merged_qtable = merge_q_tables(results)

    output_path = os.path.join("Assets", "StreamingAssets", "qtable.json")
    os.makedirs(os.path.dirname(output_path), exist_ok=True)

    with open(output_path, "w") as f:
        json.dump(merged_qtable, f, indent=2)

    print(f"\n✅ Training complete. Saved {len(merged_qtable)} entries to {output_path}")
