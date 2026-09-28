namespace HPRebar.KataExport.ViewModel;

/// <summary>One Kata column of the preview table: its letter, what it stands for and the cells written.</summary>
public sealed record KataPreviewColumn(string Letter, string Kind, string Row11, string Row19, string Row20, string Row21, string Row22, string Row23);
