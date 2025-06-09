import matplotlib.pyplot as plt

class Task:
    def __init__(self, id, name, duration, prerequisites=None):
        self.id = id
        self.name = name
        self.duration = duration
        self.prerequisites = prerequisites if prerequisites else []
        self.finish_time = -1

tasks = {
    0: Task(0, "Task 1", 14),
    1: Task(1, "Task A", 4),
    2: Task(2, "Task B", 5, [1]),
    3: Task(3, "Task C", 6, [2]),
    4: Task(4, "Task D", 4, [3]),
    5: Task(5, "Task E", 3, [4]),
    6: Task(6, "Task G", 2, [4]),
    7: Task(7, "Task H", 3, [5, 6]),
    8: Task(8, "Task J", 2, [7]),
}

def compute_finish_time(task_id):
    task = tasks[task_id]
    if task.finish_time != -1:
        return task.finish_time
    if not task.prerequisites:
        task.finish_time = task.duration
    else:
        task.finish_time = task.duration + max(compute_finish_time(pid) for pid in task.prerequisites)
    return task.finish_time

for task_id in tasks:
    compute_finish_time(task_id)

def get_level(task):
    if not task.prerequisites:
        return 0
    return 1 + max(get_level(tasks[pid]) for pid in task.prerequisites)

levels = {t.id: get_level(t) for t in tasks.values()}
max_level = max(levels.values())

positions = {}
horizontal_spacing = 2
vertical_spacing = 2
level_counts = [0] * (max_level + 1)

for t in sorted(tasks.values(), key=lambda t: levels[t.id]):
    lvl = levels[t.id]
    x = level_counts[lvl] * horizontal_spacing
    y = -lvl * vertical_spacing
    positions[t.id] = (x, y)
    level_counts[lvl] += 1

plt.figure(figsize=(10, 6), dpi=120)
ax = plt.gca()

for task in tasks.values():
    for pid in task.prerequisites:
        x1, y1 = positions[pid]
        x2, y2 = positions[task.id]
        plt.arrow(
            x1 + 0.75, y1 - 0.1,
            x2 - x1, y2 - y1 + 0.3,
            head_width=0.15, head_length=0.3,
            fc='gray', ec='gray',
            length_includes_head=True
        )

for task_id, (x, y) in positions.items():
    task = tasks[task_id]
    box = plt.Rectangle((x, y), 1.5, 1, fc='lightblue', edgecolor='black')
    ax.add_patch(box)
    plt.text(
        x + 0.75, y + 0.5,
        f"{task.name}\nf(i)={task.finish_time}\nd={task.duration}",
        ha='center', va='center', fontsize=9
    )

plt.axis('equal')
plt.axis('off')
plt.tight_layout()
plt.show()
