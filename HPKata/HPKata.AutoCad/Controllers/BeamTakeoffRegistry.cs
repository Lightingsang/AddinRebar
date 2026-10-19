using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using HPKata.Core.Beams.Models;

namespace HPKata.AutoCad.Controllers
{
    /// <summary>
    /// Thread-safe in-memory cache holding active recognized continuous beam runs across AutoCAD commands.
    /// Bridges scanning, schedule table generation, and DWG persistence.
    /// </summary>
    public static class BeamTakeoffRegistry
    {
        private static readonly ConcurrentDictionary<string, BeamRebarLayout> Cache = new(StringComparer.OrdinalIgnoreCase);

        public static IReadOnlyCollection<BeamRebarLayout> GetAll() => Cache.Values.ToList();

        public static void Register(BeamRebarLayout layout)
        {
            if (layout != null && !string.IsNullOrWhiteSpace(layout.BeamName))
            {
                Cache[layout.BeamName] = layout;
            }
        }

        public static bool TryGet(string beamName, out BeamRebarLayout? layout)
        {
            if (string.IsNullOrWhiteSpace(beamName))
            {
                layout = null;
                return false;
            }
            return Cache.TryGetValue(beamName, out layout);
        }

        public static void Clear() => Cache.Clear();
    }
}
