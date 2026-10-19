using System;
using System.Collections.Generic;
using HPKata.Core.Models;

namespace HPKata.Core.Stairs.Models
{
    /// <summary>
    /// Holds the detailed reinforcement elements for a staircase flight including
    /// bottom tension bars, scissor crank crossing bars, top negative bars, and nosing ties.
    /// </summary>
    public class StairReinforcementModel
    {
        public string FlightMark { get; set; } = "ST1";
        public StairFlightModel? Flight { get; set; }
        public List<RebarItem> ScheduleItems { get; set; } = new();
        public double ConcreteVolumeM3 { get; set; }
        public double FormworkAreaM2 { get; set; }
        public double TotalSteelWeightKg { get; set; }

        public static StairReinforcementModel FromFlight(StairFlightModel flight)
        {
            if (flight == null) throw new ArgumentNullException(nameof(flight));
            return new StairReinforcementModel
            {
                FlightMark = flight.FlightMark,
                Flight = flight
            };
        }
    }
}
