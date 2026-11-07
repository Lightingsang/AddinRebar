using ETABSv1;

namespace HPFoundation.App.Etabs;

/// <summary>
///     An ETABS of our own. CreateObject is used, never GetObject: File.NewBlank on the ETABS the engineer has open
///     would discard their model.
/// </summary>
internal sealed class EtabsSession : IDisposable
{
    private readonly cOAPI _application;
    private bool _disposed;

    private EtabsSession(cOAPI application, cSapModel model)
    {
        _application = application;
        Model = model;
    }

    public cSapModel Model { get; }

    public static EtabsSession Start()
    {
        cHelper helper = new Helper();
        var application = helper.CreateObject(EtabsInstallation.ProgramPath);
        var session = new EtabsSession(application, application.SapModel);
        try
        {
            EtabsCall.Check(application.ApplicationStart(), "ApplicationStart");
            return session;
        }
        catch
        {
            session.Dispose();
            throw;
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _application.ApplicationExit(false);
    }
}

/// <summary>Every OAPI call answers an int; a non-zero one must stop the run.</summary>
internal static class EtabsCall
{
    public static void Check(int returnCode, string call)
    {
        if (returnCode != 0)
        {
            throw new InvalidOperationException($"ETABS returned {returnCode} from {call}.");
        }
    }
}
