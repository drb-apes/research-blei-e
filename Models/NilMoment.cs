using System;

namespace BIMAPES.Matchday.App.Models
{
    /// <summary>
    /// Represents a captured NIL (Name, Image, Likeness) moment.
    /// Auto-detected or manually tagged high-value performance events.
    /// </summary>
    public class NilMoment
    {
        public string NilMomentId { get; set; }
        public string AthleteId { get; set; }
        public DateTime EventTime { get; set; }
        public string EventType { get; set; }  // "goal", "assist", "save", etc.
        public double EstimatedValue { get; set; }  // USD
        public string SponsorTag { get; set; }
        public double PerformanceSpike { get; set; } // Normalized amplitude
        public string CapsuleHash { get; set; }
        public bool IsAutoDetected { get; set; }
    }
}
