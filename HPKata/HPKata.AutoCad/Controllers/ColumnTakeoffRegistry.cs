using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using HPKata.Core.Columns.Models;

namespace HPKata.AutoCad.Controllers
{
    /// <summary>
    /// Thread-safe in-memory cache holding active recognized column layouts across AutoCAD commands.
    /// Bridges scanning, schedule table generation, and DWG persistence.
    /// </summary>
    public static class ColumnTakeoffRegistry
    {
        private static readonly ConcurrentDictionary<string, ColumnRebarLayout> Cache = new(StringComparer.OrdinalIgnoreCase);

        public static IReadOnlyCollection<ColumnRebarLayout> GetAll() => Cache.Values.ToList();

        public static void Register(ColumnRebarLayout layout)
        {
            if (layout != null && !string.IsNullOrWhiteSpace(layout.ColumnMark))
            {
                Cache[layout.ColumnMark] = layout;
            }
        }

        public static bool TryGet(string columnMark, out ColumnRebarLayout? layout)
        {
            if (string.IsNullOrWhiteSpace(columnMark))
            {
                layout = null;
                return false;
            }
            return Cache.TryGetValue(columnMark, out layout);
        }

        public static void Clear() => Cache.Clear();
    }
}
