using System;
using System.Collections.Generic;
using HPKata.Core.Models;
using HPKata.Core.Stairs.Calculators;
using HPKata.Core.Stairs.Models;

namespace HPKata.Core.Stairs.Persistence
{
    /// <summary>
    /// Aggregates staircase flights, lintels, and cross-sections into standard List of RebarItem records.
    /// </summary>
    public static class StairScheduleAggregator
    {
        public static List<RebarItem> AggregateFlight(StairFlightModel flight)
        {
            if (flight == null) throw new ArgumentNullException(nameof(flight));
            return StairRebarCalculator.CalculateStairRebar(flight);
        }

        public static List<RebarItem> AggregateLintel(LintelModel lintel)
        {
            if (lintel == null) throw new ArgumentNullException(nameof(lintel));
            return LintelRebarCalculator.CalculateLintelRebar(lintel);
        }

        public static List<RebarItem> AggregateSection(GenericSectionModel section, double lengthMm = 1000.0, int memberCount = 1)
        {
            if (section == null) throw new ArgumentNullException(nameof(section));
            return GenericSectionCalculator.SectionToRebarItems(section, lengthMm, memberCount);
        }

        public static List<RebarItem> AggregateReinforcement(StairReinforcementModel model)
        {
            if (model == null) throw new ArgumentNullException(nameof(model));
            if (model.Flight != null)
                return AggregateFlight(model.Flight);
            return new List<RebarItem>(model.ScheduleItems);
        }
    }
}
