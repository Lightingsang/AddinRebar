var view = doc.ActiveView;
string disciplineText = view.get_Parameter(BuiltInParameter.VIEW_DISCIPLINE)?.AsValueString();
long templateId = view.ViewTemplateId?.Value ?? -1;
return new
{
    id = view.Id.Value,
    uniqueId = view.UniqueId,
    name = view.Name,
    viewType = view.ViewType.ToString(),
    isTemplate = view.IsTemplate,
    scale = view.Scale,
    detailLevel = view.DetailLevel.ToString(),
    discipline = disciplineText,
    level = view.GenLevel?.Name,
    viewTemplateId = templateId,
    cropBoxActive = view.CropBoxActive,
    canBePrinted = view.CanBePrinted,
};
