using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using HPKata.Core.Stairs.Models;

namespace HPKata.AutoCad.Controllers
{
    /// <summary>
    /// Thread-safe in-memory cache holding active stair, lintel, and section layouts across AutoCAD commands.
    /// </summary>
    public static class StairTakeoffRegistry
    {
        private static readonly ConcurrentDictionary<string, StairReinforcementModel> StairCache = new(StringComparer.OrdinalIgnoreCase);
        private static readonly ConcurrentDictionary<string, LintelModel> LintelCache = new(StringComparer.OrdinalIgnoreCase);
        private static readonly ConcurrentDictionary<string, GenericSectionModel> SectionCache = new(StringComparer.OrdinalIgnoreCase);

        public static IReadOnlyCollection<StairReinforcementModel> GetAllStairs() => StairCache.Values.ToList();
        public static IReadOnlyCollection<LintelModel> GetAllLintels() => LintelCache.Values.ToList();
        public static IReadOnlyCollection<GenericSectionModel> GetAllSections() => SectionCache.Values.ToList();

        public static void RegisterStair(StairReinforcementModel model)
        {
            if (model != null && !string.IsNullOrWhiteSpace(model.FlightMark))
            {
                StairCache[model.FlightMark] = model;
            }
        }

        public static void RegisterLintel(LintelModel lintel)
        {
            if (lintel != null && !string.IsNullOrWhiteSpace(lintel.LintelMark))
            {
                LintelCache[lintel.LintelMark] = lintel;
            }
        }

        public static void RegisterSection(GenericSectionModel section)
        {
            if (section != null && !string.IsNullOrWhiteSpace(section.ComponentMark))
            {
                SectionCache[section.ComponentMark] = section;
            }
        }

        public static void Clear()
        {
            StairCache.Clear();
            LintelCache.Clear();
            SectionCache.Clear();
        }
    }
}
