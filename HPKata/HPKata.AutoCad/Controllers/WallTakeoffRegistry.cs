using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using HPKata.Core.Walls.Models;

namespace HPKata.AutoCad.Controllers
{
    /// <summary>
    /// Thread-safe in-memory cache holding active recognized shear wall models across AutoCAD commands.
    /// </summary>
    public static class WallTakeoffRegistry
    {
        private static readonly ConcurrentDictionary<string, ShearWallModel> Cache = new(StringComparer.OrdinalIgnoreCase);

        public static IReadOnlyCollection<ShearWallModel> GetAll() => Cache.Values.ToList();

        public static void Register(ShearWallModel wall)
        {
            if (wall != null && !string.IsNullOrWhiteSpace(wall.WallMark))
            {
                Cache[wall.WallMark] = wall;
            }
        }

        public static bool TryGet(string wallMark, out ShearWallModel? wall)
        {
            if (string.IsNullOrWhiteSpace(wallMark))
            {
                wall = null;
                return false;
            }
            return Cache.TryGetValue(wallMark, out wall);
        }

        public static void Clear() => Cache.Clear();
    }
}
