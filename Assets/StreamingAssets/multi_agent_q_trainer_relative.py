"""
multi_agent_q_trainer_relative.py
Keeps exactly the same "dx_dy" keys that Unity expects.
"""

import json, os, random, collections, math

# ── paths ────────────────────────────────────────────────────────────────────
BASE_DIR = r"C:\Users\61meh\Warehouse_system\Assets\StreamingAssets"
OUT_FILE = os.path.join(BASE_DIR, "qtable_relative.json")
os.makedirs(BASE_DIR, exist_ok=True)

# ── parameters ───────────────────────────────────────────────────────────────
WINDOW_RADIUS  = 4
EPISODES       = 40000
MAX_STEPS      = 200

GAMMA          = 0.95

REWARD_STEP    = -0.10
REWARD_GOAL    =  1.00
TURN_PENALTY   = -0.30
SHAPE_COEF     =  0.01          # gentler shaping

EPS_START      = 1.0
EPS_END        = 0.05
EPS_DECAY      = 0.9994

ACTIONS = (0, 1, 2, 3)          # N,S,W,E
# ─────────────────────────────────────────────────────────────────────────────


def key(dx: int, dy: int) -> str:
    return f"{dx}_{dy}"


def step(x: int, y: int, a: int) -> tuple[int, int]:
    if a == 0:   y += 1
    elif a == 1: y -= 1
    elif a == 2: x -= 1
    else:        x += 1
    x = max(-WINDOW_RADIUS, min(WINDOW_RADIUS, x))
    y = max(-WINDOW_RADIUS, min(WINDOW_RADIUS, y))
    return x, y


def optimistic_init(dx: int, dy: int) -> list[float]:
    """
    Initial Q-values: a small negative proportional to remaining distance.
    Same for all 4 actions.
    """
    base = -0.05 * (abs(dx) + abs(dy))
    return [base, base, base, base]


def train() -> dict[str, list[float]]:
    q: dict[str, list[float]] = {}
    visits: dict[str, list[int]] = {}
    eps = EPS_START

    for gx in range(-WINDOW_RADIUS, WINDOW_RADIUS + 1):
        for gy in range(-WINDOW_RADIUS, WINDOW_RADIUS + 1):
            if gx == 0 and gy == 0:
                continue

            for _ in range(EPISODES):
                sx = sy = 0
                prev_a = None

                for _ in range(MAX_STEPS):
                    dx, dy = gx - sx, gy - sy
                    s_key = key(dx, dy)
                    if s_key not in q:
                        q[s_key] = optimistic_init(dx, dy)
                        visits[s_key] = [0, 0, 0, 0]

                    # ε-greedy
                    if random.random() < eps:
                        a = random.choice(ACTIONS)
                    else:
                        a = max(ACTIONS, key=lambda i: q[s_key][i])

                    nx, ny = step(sx, sy, a)
                    ndx, ndy = gx - nx, gy - ny
                    ns_key = key(ndx, ndy)
                    if ns_key not in q:
                        q[ns_key] = optimistic_init(ndx, ndy)
                        visits[ns_key] = [0, 0, 0, 0]

                    # reward
                    turned = prev_a is not None and a != prev_a
                    base   = REWARD_GOAL if (nx, ny) == (gx, gy) else REWARD_STEP
                    shape  = 0.0 if (nx, ny) == (gx, gy) else SHAPE_COEF * (
                             abs(dx) + abs(dy) - abs(ndx) - abs(ndy))
                    reward = base + shape + (TURN_PENALTY if turned else 0.0)

                    # per-(s,a) learning rate
                    visits[s_key][a] += 1
                    alpha = 1.0 / visits[s_key][a]

                    # Q-update
                    best_next = max(q[ns_key])
                    q[s_key][a] += alpha * (reward + GAMMA * best_next - q[s_key][a])

                    sx, sy = nx, ny
                    prev_a = a
                    if (sx, sy) == (gx, gy):
                        break

                eps = max(EPS_END, eps * EPS_DECAY)

    print(f"✅ Training finished — {len(q)} states")
    return q


if __name__ == "__main__":
    table = train()
    with open(OUT_FILE, "w", encoding="utf-8") as fp:
        json.dump(table, fp, indent=2)
    print(f"📦 Q-table saved to: {OUT_FILE}")
