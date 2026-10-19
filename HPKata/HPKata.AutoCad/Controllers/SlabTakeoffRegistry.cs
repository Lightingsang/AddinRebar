using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using HPKata.Core.Slabs.Models;

namespace HPKata.AutoCad.Controllers
{
    /// <summary>
    /// Thread-safe in-memory cache holding active slab layouts across AutoCAD commands.
    /// </summary>
    public static class SlabTakeoffRegistry
    {
        private static readonly ConcurrentDictionary<string, SlabRebarLayout> Cache = new(StringComparer.OrdinalIgnoreCase);

        public static IReadOnlyCollection<SlabRebarLayout> GetAll() => Cache.Values.ToList();

        public static void Register(SlabRebarLayout layout)
        {
            if (layout != null && !string.IsNullOrWhiteSpace(layout.PanelMark))
            {
                Cache[layout.PanelMark] = layout;
            }
        }

        public static bool TryGet(string mark, out SlabRebarLayout? layout)
        {
            if (string.IsNullOrWhiteSpace(mark))
            {
                layout = null;
                return false;
            }
            return Cache.TryGetValue(mark, out layout);
        }

        public static void Clear() => Cache.Clear();
    }
}
