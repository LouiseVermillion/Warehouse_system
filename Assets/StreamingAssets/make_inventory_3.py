import json
import random
import os
import math

# --- Configuration ---
# FULL movement grid
movement_width = 12
movement_height = 12

# INNER storage area
storage_width = 10
storage_height = 10
storage_offset_x = 1  # The storage area starts at x=1
storage_offset_y = 1  # The storage area starts at y=1

max_stack_height = 10
fill_ratio = 0.5
seed = 42

# Output paths (set to your project structure)
inventory_path = r"C:\Users\61meh\Warehouse_system\Assets\StreamingAssets\inventory.json"
stored_path = r"C:\Users\61meh\Warehouse_system\Assets\StreamingAssets\stored.json"
# ----------------------

random.seed(seed)

# Calculate fill ratio based on the STORAGE area, not the full grid
total_slots = storage_width * storage_height * max_stack_height
num_boxes = math.floor(total_slots * fill_ratio)

stored_boxes = []
id_counter = 0

while len(stored_boxes) < num_boxes:
    # Generate coordinates WITHIN the 0-9 range of the storage area
    local_x = random.randint(0, storage_width - 1)
    local_y = random.randint(0, storage_height - 1)

    # Add the offset to place it correctly in the world grid
    gx = local_x + storage_offset_x
    gy = local_y + storage_offset_y

    current_stack = sum(1 for b in stored_boxes if b["grid"]["x"] == gx and b["grid"]["y"] == gy)
    if current_stack >= max_stack_height:
        continue
    
    altitude = current_stack
    stored_boxes.append({
        "id": f"BOX_{id_counter:04d}",
        "grid": { "x": gx, "y": gy },
        "altitude": altitude
    })
    id_counter += 1

# Write inventory.json (boxes in warehouse)
inventory = {"stored": stored_boxes}
os.makedirs(os.path.dirname(inventory_path), exist_ok=True)
with open(inventory_path, "w") as f:
    json.dump(inventory, f, indent=2)

# Write stored.json (delivered boxes) -- initially empty
# THIS IS THE CORRECTED PART
delivered = {"taken": []} 
with open(stored_path, "w") as f:
    json.dump(delivered, f, indent=2)

print(f"✅ Inventory generated with {len(stored_boxes)} boxes.")
print(f"📦 Saved to: {inventory_path}")
print(f"✅ Stored (delivered) list created at: {stored_path} with the correct format.")