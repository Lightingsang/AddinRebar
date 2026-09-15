int maxTasks = Math.Min(5000, Math.Max(1, args.Int("maxTasks", 500)));
bool includeSelection = args.Bool("includeSelection", false);
var rows = new List<object>();

void Walk(SavedItem item, int level)
{
    if (rows.Count >= maxTasks) return;
    ct.ThrowIfCancellationRequested();
    if (item is TimelinerTask task)
    {
        rows.Add(new
        {
            name = task.DisplayName,
            level,
            status = task.TaskStatus.ToString(),
            taskType = task.SimulationTaskTypeName,
            plannedStart = task.PlannedStartDate,
            plannedEnd = task.PlannedEndDate,
            actualStart = task.ActualStartDate,
            actualEnd = task.ActualEndDate,
            selection = includeSelection && !task.Selection.IsClear ? task.Selection.DisplayString : null,
        });
        foreach (var child in task.Children) Walk(child, level + 1);
    }
    else if (item is GroupItem group)
    {
        foreach (var child in group.Children) Walk(child, level + 1);
    }
}

var timeliner = doc.GetTimeliner();
foreach (var item in timeliner.Tasks) Walk(item, 0);
log($"{rows.Count} task(s) of {timeliner.TaskTotalTasks()}");
return new { total = (int)timeliner.TaskTotalTasks(), shown = rows.Count, tasks = rows };
