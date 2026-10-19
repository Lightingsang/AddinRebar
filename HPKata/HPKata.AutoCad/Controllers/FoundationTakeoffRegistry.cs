using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using HPKata.Core.Foundations.Models;

namespace HPKata.AutoCad.Controllers
{
    /// <summary>
    /// Thread-safe in-memory cache holding active foundation layouts across AutoCAD commands.
    /// </summary>
    public static class FoundationTakeoffRegistry
    {
        private static readonly ConcurrentDictionary<string, FoundationRebarLayout> Cache = new(StringComparer.OrdinalIgnoreCase);

        public static IReadOnlyCollection<FoundationRebarLayout> GetAll() => Cache.Values.ToList();

        public static void Register(FoundationRebarLayout layout)
        {
            if (layout != null && !string.IsNullOrWhiteSpace(layout.FoundationMark))
            {
                Cache[layout.FoundationMark] = layout;
            }
        }

        public static bool TryGet(string mark, out FoundationRebarLayout? layout)
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
