using System.Collections.Generic;

namespace TaskSchedulerDP
{
    class Task
    {
        public int Id;
        public string Name;
        public int Duration;
        public List<Task> Prerequisites = new();
        public int FinishTime = -1;

        public Dictionary<string, int> ResourceRequirements = new();
    }
}
