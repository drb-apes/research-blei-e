using System;
using System.Collections.Generic;

namespace BIMAPES.Matchday.App.Models
{
    /// <summary>
    /// RiskState: Institutional-grade Composite Risk Score (CRS) and hedge action engine.
    /// Maps athlete biometric + NIL signals to equity exposure and hedge strategies.
    /// </summary>
    public class RiskState
    {
        public string RiskStateId { get; set; } = Guid.NewGuid().ToString();
        public string AthleteId { get; set; }
        public DateTime AssessedAt { get; set; }
        
        // Composite Risk Score (CRS)
        public double CompositeRiskScore { get; set; }  // [0,1], where 1 = max risk
        public string RiskLevel { get; set; }  // "Low", "Moderate", "High", "Critical"
        
        // Risk State Analysis
        public double InjuryRiskFactor { get; set; }    // Biometric-derived injury probability
        public double PerformanceDeclineFactor { get; set; } // Fatigue/readiness decline
        public double ReputationalRiskFactor { get; set; }  // Sentiment/virality downside
        public double MarketVolatilityFactor { get; set; }  // NIL value volatility
        
        // Cross-Asset Mapping
        public List<CrossAssetExposure> CrossAssetExposures { get; set; } = new();
        
        // Hedge Action Engine
        public List<HedgeAction> SuggestedHedges { get; set; } = new();
        
        // Historical Context
        public double PreviousCrsScore { get; set; }
        public double CrsDelta { get; set; }  // Change since last assessment
        public string Trend { get; set; }  // "Improving", "Stable", "Deteriorating"
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
    
    public class CrossAssetExposure
    {
        public string ExposureId { get; set; } = Guid.NewGuid().ToString();
        public string AssetClass { get; set; }  // "Sponsor_Equity", "Media_Streaming", "Betting", "CPM_Consumer"
        public string RelatedEntity { get; set; }  // e.g., "Nike", "ESPN", "DraftKings", "Pepsi"
        public double BetaExposure { get; set; }  // Equity beta sensitivity
        public double CorrelationToAthlete { get; set; }  // [0,1]
        public double ImpactIfAthleteFails { get; set; }  // Expected loss %
    }
    
    public class HedgeAction
    {
        public string HedgeActionId { get; set; } = Guid.NewGuid().ToString();
        public string Strategy { get; set; }  // "Reduce_Exposure", "Volatility_Hedge", "Rotation_Trade"
        public string ActionDescription { get; set; }
        public double RecommendedNotional { get; set; }  // USD amount
        public double EstimatedEffectiveness { get; set; }  // [0,1]
        public string Priority { get; set; }  // "Low", "Medium", "High", "Urgent"
        public DateTime SuggestedExecutionTime { get; set; }
    }
}
