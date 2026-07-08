using System;

namespace BIMAPES.Matchday.App.Models
{
    /// <summary>
    /// NILMoment: High-value performance event flagged for sponsorship/valuation.
    /// Captures moment type, value, and associated biometric spike.
    /// </summary>
    public class NILMoment
    {
        public string MomentId { get; set; } = Guid.NewGuid().ToString();
        public string AthleteId { get; set; }
        public DateTime OccurredAt { get; set; }
        
        // Moment Classification
        public string MomentType { get; set; }  // "Goal", "Assist", "Defense", "Viral", "Milestone", "Custom"
        public string Description { get; set; }
        
        // Biometric Context
        public int HeartRateAtMoment { get; set; }
        public double AccelerationAtMoment { get; set; }
        public double SpeedAtMoment { get; set; }
        
        // NIL Valuation
        public double EstimatedNilValue { get; set; }  // USD impact
        public double ViralityScore { get; set; }  // [0,1]
        public double SponsorshipDesirability { get; set; }  // [0,1]
        public string SuggestedSponsor { get; set; }  // AI recommendation
        
        // Market Impact
        public string RelatedAssetClass { get; set; }  // "Sponsor_Equity", "Media_Rights", "Betting", "Consumer"
        public double ImpactOnEquityBeta { get; set; }  // Delta to related equity
        
        // Status
        public bool IsConfirmed { get; set; } = false;
        public bool IsExported { get; set; } = false;
        public string AuditNotes { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
