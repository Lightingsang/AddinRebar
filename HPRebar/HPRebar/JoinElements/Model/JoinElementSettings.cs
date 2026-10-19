using System.Collections.Generic;

namespace HPRebar.JoinElements.Model;

/// <summary>
/// Settings configured by the user for Auto Join execution.
/// </summary>
public sealed class JoinElementSettings
{
    public JoinScope Scope { get; set; } = JoinScope.ActiveView;
    public List<JoinCategoryPairOption> CategoryPairs { get; set; } = new();
    public bool UnjoinFirst { get; set; } = false; // False by default now that we use SmartSkip
    public bool SwitchOrderToPriority { get; set; } = true;
    public bool SmartSkip { get; set; } = true;
    public bool CleanJoinedNonIntersecting { get; set; } = true;
}
