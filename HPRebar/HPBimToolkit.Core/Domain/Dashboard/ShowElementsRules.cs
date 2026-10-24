using HPBimToolkit.Core.Contracts;

namespace HPBimToolkit.Core.Domain.Dashboard;

/// <summary>Validates a show request before anything touches Revit, so a bad request costs no trip to the API thread.</summary>
public static class ShowElementsRules
{
    /// <summary>Bounds the work an id list from the page can cause.</summary>
    public const int MaxRequestedIds = 10_000;

    /// <exception cref="BridgeException">BAD_REQUEST when the request names nothing or too many ids.</exception>
    public static ShowElementsSource SourceOf(ShowElementsRequest request)
    {
        if (request.CurrentSelection)
        {
            return ShowElementsSource.CurrentSelection;
        }

        if (request.Ids is { Count: > 0 } ids)
        {
            return ids.Count <= MaxRequestedIds
                ? ShowElementsSource.Ids
                : throw new BridgeException(BridgeErrorCodes.BadRequest, $"At most {MaxRequestedIds} ids can be shown at once.");
        }

        return request.CategoryId is not null
            ? ShowElementsSource.Category
            : throw new BridgeException(BridgeErrorCodes.BadRequest, "Give categoryId, a non-empty ids list or currentSelection.");
    }
}
