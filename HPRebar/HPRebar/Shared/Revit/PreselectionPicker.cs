using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;

namespace HPRebar.Shared.Revit;

/// <summary>
/// Finds the elements a command works on: the ones already selected that the command's filter accepts, or, when
/// there are none, the ones the user picks. Selecting first and then clicking the button skips the pick prompt.
/// </summary>
public static class PreselectionPicker
{
    /// <summary>
    /// The selected elements the filter accepts or, when none is selected, the elements the user picks; empty when
    /// the pick is cancelled with Esc.
    /// </summary>
    public static IReadOnlyList<Element> PickElements(UIDocument uiDocument, ISelectionFilter filter, string prompt)
    {
        RequireArguments(uiDocument, filter);

        var preselected = Preselected(uiDocument, filter);
        if (preselected.Count > 0)
        {
            return preselected;
        }

        try
        {
            return uiDocument.Selection.PickObjects(ObjectType.Element, filter, prompt)
                .Select(reference => uiDocument.Document.GetElement(reference))
                .Where(element => element is not null)
                .ToList();
        }
        catch (Autodesk.Revit.Exceptions.OperationCanceledException)
        {
            return Array.Empty<Element>();
        }
    }

    /// <summary>
    /// The one selected element the filter accepts or, when there is none or more than one, the element the user
    /// picks (two candidates are never guessed between); null when the pick is cancelled with Esc.
    /// </summary>
    public static Element? PickElement(UIDocument uiDocument, ISelectionFilter filter, string prompt)
    {
        RequireArguments(uiDocument, filter);

        var preselected = Preselected(uiDocument, filter);
        if (preselected.Count == 1)
        {
            return preselected[0];
        }

        try
        {
            var reference = uiDocument.Selection.PickObject(ObjectType.Element, filter, prompt);
            return reference is null ? null : uiDocument.Document.GetElement(reference);
        }
        catch (Autodesk.Revit.Exceptions.OperationCanceledException)
        {
            return null;
        }
    }

    private static void RequireArguments(UIDocument uiDocument, ISelectionFilter filter)
    {
        if (uiDocument is null)
        {
            throw new ArgumentNullException(nameof(uiDocument));
        }

        if (filter is null)
        {
            throw new ArgumentNullException(nameof(filter));
        }
    }

    private static List<Element> Preselected(UIDocument uiDocument, ISelectionFilter filter) =>
        uiDocument.Selection.GetElementIds()
            .Select(id => uiDocument.Document.GetElement(id))
            .Where(element => element is not null && filter.AllowElement(element))
            .ToList();
}
