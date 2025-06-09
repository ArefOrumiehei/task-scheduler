using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace TaskSchedulerDP
{
    public partial class Form1 : Form
    {
        Dictionary<int, Task> tasks = new();
        Dictionary<string, int> resourceCapacities = new();
        private DataGridView dataGridView;

        private Dictionary<string, int> currentResourceUsage = new();
        private Dictionary<Task, int> startTimes = new();

        private void InitializeResources()
        {
            resourceCapacities["Engineer"] = 1;
            resourceCapacities["Machine"] = 1;
            resourceCapacities["Electrician"] = 1;
            resourceCapacities["Installer"] = 1;
            resourceCapacities["Painter"] = 1;
        }


        public Form1()
        {
            InitializeComponent();
            InitializeResources();
            InitializeTasks();
            //ComputeFinishTimes();
            ScheduleTasksWithResources();
            InitializeDataGridView();
            PopulateDataGrid();
            DoubleBuffered = true;
            Paint += Form1_Paint;
        }

        private void InitializeDataGridView()
        {
            dataGridView = new DataGridView
            {
                Location = new Point(10, 10),
                Width = 400,
                Height = 200,
                ReadOnly = true,
                AllowUserToAddRows = false,
                ColumnCount = 5
            };
            dataGridView.Columns[0].Name = "ID";
            dataGridView.Columns[1].Name = "Name";
            dataGridView.Columns[2].Name = "Duration";
            dataGridView.Columns[3].Name = "StartTime";
            dataGridView.Columns[4].Name = "FinishTime";


            this.Controls.Add(dataGridView);
        }

        private void PopulateDataGrid()
        {
            dataGridView.Rows.Clear();

            foreach (var task in tasks.Values.OrderBy(t => t.Id))
            {
                int start = startTimes.ContainsKey(task) ? startTimes[task] : -1;
                dataGridView.Rows.Add(task.Id, task.Name, task.Duration, start, task.FinishTime);
            }
        }

        private void InitializeTasks()
        {
            tasks[1] = new Task { Id = 1, Name = "A1", Duration = 4 };
            tasks[1].ResourceRequirements["Engineer"] = 1;
            tasks[1].ResourceRequirements["Machine"] = 1;

            tasks[2] = new Task { Id = 2, Name = "A2", Duration = 5, Prerequisites = new List<Task> { tasks[1] } };
            tasks[2].ResourceRequirements["Engineer"] = 1;

            tasks[3] = new Task { Id = 3, Name = "A3", Duration = 6, Prerequisites = new List<Task> { tasks[2] } };
            tasks[3].ResourceRequirements["Machine"] = 1;
            tasks[3].ResourceRequirements["Engineer"] = 1;

            tasks[4] = new Task { Id = 4, Name = "A4", Duration = 4, Prerequisites = new List<Task> { tasks[3] } };
            tasks[4].ResourceRequirements["Engineer"] = 1;

            tasks[5] = new Task { Id = 5, Name = "A5", Duration = 3, Prerequisites = new List<Task> { tasks[4] } };
            tasks[5].ResourceRequirements["Engineer"] = 1;
            tasks[5].ResourceRequirements["Electrician"] = 1;

            tasks[6] = new Task { Id = 6, Name = "A6", Duration = 2, Prerequisites = new List<Task> { tasks[4] } };
            tasks[6].ResourceRequirements["Installer"] = 1;
            tasks[6].ResourceRequirements["Engineer"] = 1;

            tasks[7] = new Task { Id = 7, Name = "A7", Duration = 3, Prerequisites = new List<Task> { tasks[5], tasks[6] } };
            tasks[7].ResourceRequirements["Painter"] = 1;

            tasks[8] = new Task { Id = 8, Name = "A8", Duration = 2, Prerequisites = new List<Task> { tasks[7] } };
            tasks[8].ResourceRequirements["Installer"] = 1;
            tasks[8].ResourceRequirements["Electrician"] = 1;
        }

        private bool HasEnoughResources(Task task)
        {
            foreach (var req in task.ResourceRequirements)
            {
                int available = resourceCapacities[req.Key] - currentResourceUsage[req.Key];
                if (available < req.Value)
                    return false;
            }
            return true;
        }

        private void ReserveResources(Task task)
        {
            foreach (var req in task.ResourceRequirements)
                currentResourceUsage[req.Key] += req.Value;
        }

        private void FreeResources(Task task)
        {
            foreach (var req in task.ResourceRequirements)
                currentResourceUsage[req.Key] -= req.Value;
        }

        private void ScheduleTasksWithResources()
        {
            // Initialize all resource usage to 0 at the beginning
            foreach (var resource in resourceCapacities)
                currentResourceUsage[resource.Key] = 0;

            int currentTime = 0;
            HashSet<Task> completed = new();             // Tasks that are fully completed
            HashSet<Task> running = new();               // Tasks currently running
            Dictionary<Task, int> remainingTime = new(); // Time left for running tasks

            while (completed.Count < tasks.Count)
            {
                // Check and process tasks that have finished at this time step
                foreach (var task in running.ToList())
                {
                    remainingTime[task]--;
                    if (remainingTime[task] == 0)
                    {
                        running.Remove(task);
                        completed.Add(task);
                        FreeResources(task);
                        task.FinishTime = currentTime;
                    }
                }

                // Find all tasks that are ready to start (prerequisites completed, not running or done)
                var candidates = tasks.Values
                    .Where(t => !completed.Contains(t) && !running.Contains(t))
                    .Where(t => t.Prerequisites.All(p => completed.Contains(p)))
                    .OrderByDescending(t => t.Duration); // Optional: prioritize longer tasks

                // Try to start each candidate task if enough resources are available
                foreach (var task in candidates)
                {
                    if (HasEnoughResources(task))
                    {
                        ReserveResources(task);
                        running.Add(task);
                        remainingTime[task] = task.Duration;
                        startTimes[task] = currentTime;
                    }
                }

                // Move to the next time unit
                currentTime++;
            }
        }

        private void Form1_Paint(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            Font font = new("Arial", 10);
            Brush textBrush = Brushes.Black;
            int separatorY = 0;
            // Prepare positions for DAG layout
            int offsetX = separatorY;
            int nodeWidth = 75;
            int nodeHeight = 70;
            int horizontalSpacing = 50;
            int verticalSpacing = 50;

            Dictionary<Task, Point> positions = new();

            // Level (depth) for each task (based on longest prerequisite chain)
            Dictionary<Task, int> levels = new();
            foreach (var task in tasks.Values)
                levels[task] = GetLevel(task);

            int maxLevel = levels.Values.Max();
            for (int level = 0; level <= maxLevel; level++)
            {
                var tasksAtLevel = tasks.Values.Where(t => levels[t] == level).ToList();
                for (int i = 0; i < tasksAtLevel.Count; i++)
                {
                    var t = tasksAtLevel[i];
                    int x = level * (nodeWidth + horizontalSpacing) + 50;
                    int y = 300 + i * (nodeHeight + verticalSpacing);
                    positions[t] = new Point(x, y);
                }
            }

            // Draw edges (arrows)
            Pen arrowPen = new(Color.Brown, 2);
            AdjustableArrowCap arrowCap = new(5, 5);
            arrowPen.CustomEndCap = arrowCap;

            foreach (var task in tasks.Values)
            {
                var to = positions[task];
                foreach (var prereq in task.Prerequisites)
                {
                    var from = positions[prereq];
                    Point start = new(from.X + nodeWidth, from.Y + nodeHeight / 2);
                    Point end = new(to.X, to.Y + nodeHeight / 2);
                    g.DrawLine(arrowPen, start, end);
                }
            }

            // Draw entry arrow ONLY to the first task (Id = 1)
            var firstTask = tasks[1];
            var to2 = positions[firstTask];
            Point start2 = new(to2.X - 50, to2.Y + nodeHeight / 2);
            Point end2 = new(to2.X, to2.Y + nodeHeight / 2);
            g.DrawLine(arrowPen, start2, end2);


            // Draw task boxes
            Brush boxBrush = new SolidBrush(Color.LightSteelBlue);
            Pen boxPen = Pens.Black;

            foreach (var task in tasks.Values)
            {
                var pos = positions[task];
                Rectangle rect = new(pos.X, pos.Y, nodeWidth, nodeHeight);
                g.FillRectangle(boxBrush, rect);
                g.DrawRectangle(boxPen, rect);
                string text = $"{task.Name}\nS: {startTimes[task]}\nF: {task.FinishTime}";
                var textSize = g.MeasureString(text, font);
                g.DrawString(text, font, textBrush,
                    pos.X + (nodeWidth - textSize.Width) / 2,
                    pos.Y + (nodeHeight - textSize.Height) / 2);
            }
        }

        // Helper method to compute depth (level) of a task
        private int GetLevel(Task task)
        {
            if (task.Prerequisites.Count == 0)
                return 0;
            return 1 + task.Prerequisites.Max(p => GetLevel(p));
        }
    }
}
