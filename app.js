const tasks = [];
let animationStarted = false;

const svg = d3.select("#graph");
const width = +svg.attr("width");
const height = +svg.attr("height");

const tooltip = d3.select("#tooltip");

const taskNameInput = document.getElementById("task-name");
const taskDurationInput = document.getElementById("task-duration");
const addTaskBtn = document.getElementById("add-task-btn");
const startBtn = document.getElementById("start-btn");
const tasksContainer = document.getElementById("tasks-container");

addTaskBtn.onclick = () => {
  const name = taskNameInput.value.trim();
  const duration = +taskDurationInput.value;
  if (!name || !duration || duration <= 0) {
    alert("Please enter valid name and duration!");
    return;
  }
  if (tasks.find(t => t.name === name)) {
    alert("Task name must be unique!");
    return;
  }
  const newTask = { id: tasks.length + 1, name, duration, prerequisites: [], finishTime: -1 };
  tasks.push(newTask);
  taskNameInput.value = "";
  taskDurationInput.value = "";
  renderTasks();
};

function renderTasks() {
  tasksContainer.innerHTML = "";
  tasks.forEach((task, index) => {
    const taskDiv = document.createElement("div");
    taskDiv.className = "task-item";
    taskDiv.setAttribute("data-index", index);
    taskDiv.innerHTML = `
      <div>
        <strong>${task.name}</strong> (Duration: ${task.duration})
        <button class="edit-btn">✏️</button>
        <button class="delete-btn">🗑️</button>
        <br/>
        <div class="task-prereq-list" id="prereq-list-${task.id}">
        Prerequisites:
        ${task.prerequisites.map(pr => `
          <span class="prereq-item" data-taskid="${pr.id}">
            ${pr.name}
            <span class="prereq-remove" data-taskid="${pr.id}">&times;</span>
          </span>
        `).join("")}
        </div>
      </div>
      <div>
        <select id="select-prereq-${task.id}">
          <option value="">Add Prerequisite</option>
          ${tasks.filter(t => t.id !== task.id && !task.prerequisites.includes(t))
            .map(t => `<option value="${t.id}">${t.name}</option>`).join("")}
        </select>
      </div>
    `;
    
    console.log(tasks);

    const prereqListDiv = taskDiv.querySelector(".task-prereq-list");
    if (task.prerequisites.length === 0) {
      prereqListDiv.style.display = "none";
    } else {
      prereqListDiv.style.display = "block";
    }

    
    taskDiv.querySelector(".delete-btn").onclick = () => {
      tasks.splice(index, 1);
      renderTasks();
      drawGraph();
    };

    taskDiv.querySelector(".edit-btn").onclick = () => {
      const newName = prompt("New task name:", task.name);
      const newDuration = prompt("New duration:", task.duration);
      if (newName && newDuration) {
        task.name = newName;
        task.duration = parseInt(newDuration);
        renderTasks();
        drawGraph();
      }
    };

    tasksContainer.appendChild(taskDiv);

    // Add event for adding prereq
    const select = document.getElementById(`select-prereq-${task.id}`);
    select.onchange = () => {
      const selectedId = +select.value;
      if (selectedId) {
        const prereqTask = tasks.find(t => t.id === selectedId);
        // Avoid circular or duplicates
        if (!task.prerequisites.some(p => p.id === prereqTask.id)) {
          task.prerequisites.push(prereqTask);
          renderTasks();
        }
      }
    };

    // Add event for removing prereq
    taskDiv.querySelectorAll(".prereq-remove").forEach(el => {
      el.onclick = (e) => {
        e.stopPropagation();
        const removeId = +el.getAttribute("data-taskid");
        task.prerequisites = task.prerequisites.filter(p => p.id !== removeId);
        renderTasks();
      };
    });
  });
}

function computeFinishTimes() {
  // Reset finish times
  tasks.forEach(t => t.finishTime = -1);

  function compute(task) {
    if (task.finishTime !== -1) return task.finishTime;
    if (task.prerequisites.length === 0) {
      task.finishTime = task.duration;
    } else {
      task.finishTime = task.duration + Math.max(...task.prerequisites.map(p => compute(p)));
    }
    return task.finishTime;
  }
  tasks.forEach(task => compute(task));
}

function getLevel(task, memo = new Map()) {
  if (memo.has(task.id)) return memo.get(task.id);
  if (task.prerequisites.length === 0) {
    memo.set(task.id, 0);
    return 0;
  }
  const lvl = 1 + Math.max(...task.prerequisites.map(p => getLevel(p, memo)));
  memo.set(task.id, lvl);
  return lvl;
}

function getTaskPath(task) {
  if (task.prerequisites.length === 0) return "start";
  const sorted = task.prerequisites.slice().sort((a,b) => a.finishTime - b.finishTime);
  return sorted.map(p => p.name).join(" → ") + " → " + task.name;
}

function drawGraph() {
  svg.selectAll("*").remove();

  computeFinishTimes();

  // Compute levels for layout
  const levels = new Map();
  tasks.forEach(t => levels.set(t.id, getLevel(t)));

  // Position nodes by level
  const nodesByLevel = [];
  const maxLevel = Math.max(...levels.values());
  for(let i = 0; i <= maxLevel; i++) {
    nodesByLevel[i] = tasks.filter(t => levels.get(t.id) === i);
  }

  // Calculate positions
  const nodeWidth = 120, nodeHeight = 40;
  const horizontalSpacing = 160, verticalSpacing = 120;
  const positions = new Map();

  nodesByLevel.forEach((nodes, level) => {
    nodes.forEach((node, i) => {
      positions.set(node.id, {
        x: 50 + i * horizontalSpacing,
        y: 50 + level * verticalSpacing
      });
    });
  });

  // Draw links with animation
  const links = [];
  tasks.forEach(task => {
    task.prerequisites.forEach(pr => {
      links.push({ source: positions.get(pr.id), target: positions.get(task.id) });
    });
  });

  const linkSelection = svg.selectAll(".link")
    .data(links)
    .enter()
    .append("path")
    .attr("class", "link")
    .attr("fill", "none")
    .attr("stroke", "#00bfa5")
    .attr("stroke-width", 3)
    .attr("d", d => {
      const sx = d.source.x + nodeWidth / 2;
      const sy = d.source.y + nodeHeight;
      const tx = d.target.x + nodeWidth / 2;
      const ty = d.target.y;
      return `M${sx},${sy} L${sx},${sy} L${sx},${sy}`; // Start as zero length for animation
    });

  // Animate links drawing
  linkSelection
    .transition()
    .duration(1500)
    .attrTween("d", function(d) {
      const sx = d.source.x + nodeWidth / 2;
      const sy = d.source.y + nodeHeight;
      const tx = d.target.x + nodeWidth / 2;
      const ty = d.target.y;
      const interpolate = d3.interpolateString(
        `M${sx},${sy} L${sx},${sy}`,
        `M${sx},${sy} L${tx},${ty}`
      );
      return t => interpolate(t);
    });

  // Draw nodes
  const nodeGroup = svg.selectAll(".node")
    .data(tasks)
    .enter()
    .append("g")
    .attr("class", "node")
    .attr("transform", d => {
      const pos = positions.get(d.id);
      return `translate(${pos.x},${pos.y})`;
    })
    .style("cursor", "pointer");

  nodeGroup.append("rect")
    .attr("width", nodeWidth)
    .attr("height", nodeHeight)
    .attr("rx", 10)
    .attr("ry", 10)
    .attr("fill", "#009688")
    .attr("stroke", "#004d40")
    .attr("stroke-width", 2);

  nodeGroup.append("text")
    .attr("x", nodeWidth / 2)
    .attr("y", nodeHeight / 2 + 5)
    .attr("text-anchor", "middle")
    .attr("fill", "#e0f7fa")
    .style("font-weight", "600")
    .text(d => d.name);

  // Tooltip on click
  nodeGroup.on("click", (event, d) => {
    tooltip
      .style("opacity", 1)
      .html(`<strong>${d.name}</strong><br/>Duration: ${d.duration}h<br/>Finish Time: ${d.finishTime}<br/>Path: ${getTaskPath(d)}`)
      .style("left", (event.pageX + 15) + "px")
      .style("top", (event.pageY + 15) + "px");
  });

  // Hide tooltip on background click
  svg.on("click", (event) => {
    if (event.target.tagName === "svg") {
      tooltip.style("opacity", 0);
    }
  });
}

startBtn.onclick = () => {
  if (tasks.length === 0) {
    alert("Add some tasks first!");
    return;
  }
  animationStarted = false; // Reset state to allow re-run
  drawGraph();
};