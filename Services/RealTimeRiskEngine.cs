using System;
using System.Collections.Generic;
using System.Linq;
using BIMAPES.Matchday.App.Models;

namespace BIMAPES.Matchday.App.Services
{
    /// <summary>
    /// RealTimeRiskEngine: Live Composite Risk Score (CRS) calculation engine.
    /// Ingests athlete biometric capsules and NIL events, outputs institutional risk signals.
    /// Designed for sub-100ms latency on enterprise clients.
    /// </summary>
    public class RealTimeRiskEngine
    {
        private readonly Dictionary<string, List<Capsule>> _athleteCapsuleBuffer = new();
        private readonly Dictionary<string, List<NILMoment>> _athleteNILBuffer = new();
        private readonly Dictionary<string, RiskState> _currentRiskStates = new();

        /// <summary>
        /// Ingest live capsule and update athlete risk model.
        /// </summary>
        public RiskState IngestCapsule(Capsule capsule, string athleteId)
        {
            if (!_athleteCapsuleBuffer.ContainsKey(athleteId))
                _athleteCapsuleBuffer[athleteId] = new List<Capsule>();

            _athleteCapsuleBuffer[athleteId].Add(capsule);

            // Keep rolling window (last 60 seconds)
            var cutoff = DateTime.UtcNow.AddSeconds(-60);
            _athleteCapsuleBuffer[athleteId] = _athleteCapsuleBuffer[athleteId]
                .Where(c => c.CapturedAt >= cutoff)
                .ToList();

            return _recalculateRiskState(athleteId);
        }

        /// <summary>
        /// Ingest NIL moment and update risk state.
        /// </summary>
        public RiskState IngestNILMoment(NILMoment nilMoment, string athleteId)
        {
            if (!_athleteNILBuffer.ContainsKey(athleteId))
                _athleteNILBuffer[athleteId] = new List<NILMoment>();

            _athleteNILBuffer[athleteId].Add(nilMoment);

            // Keep 24-hour history
            var cutoff = DateTime.UtcNow.AddHours(-24);
            _athleteNILBuffer[athleteId] = _athleteNILBuffer[athleteId]
                .Where(n => n.CreatedAt >= cutoff)
                .ToList();

            return _recalculateRiskState(athleteId);
        }

        /// <summary>
        /// Recalculate Composite Risk Score (CRS) for athlete.
        /// </summary>
        private RiskState _recalculateRiskState(string athleteId)
        {
            var prevState = _currentRiskStates.ContainsKey(athleteId)
                ? _currentRiskStates[athleteId]
                : null;

            var capsules = _athleteCapsuleBuffer.ContainsKey(athleteId)
                ? _athleteCapsuleBuffer[athleteId]
                : new List<Capsule>();

            var nilMoments = _athleteNILBuffer.ContainsKey(athleteId)
                ? _athleteNILBuffer[athleteId]
                : new List<NILMoment>();

            var newState = new RiskState
            {
                AthleteId = athleteId,
                AssessedAt = DateTime.UtcNow,
                PreviousCrsScore = prevState?.CompositeRiskScore ?? 0.0,
                CompositeRiskScore = _calculateCRS(capsules, nilMoments),
                InjuryRiskFactor = _calculateInjuryRisk(capsules),
                PerformanceDeclineFactor = _calculatePerformanceDecline(capsules),
                ReputationalRiskFactor = _calculateReputationalRisk(nilMoments),
                MarketVolatilityFactor = _calculateMarketVolatility(nilMoments),
                CrossAssetExposures = _mapCrossAssetExposures(nilMoments),
                SuggestedHedges = _generateHedgeActions(athleteId, capsules, nilMoments),
            };

            newState.CrsDelta = newState.CompositeRiskScore - newState.PreviousCrsScore;
            newState.Trend = newState.CrsDelta > 0.05 ? "Deteriorating"
                           : newState.CrsDelta < -0.05 ? "Improving"
                           : "Stable";

            newState.RiskLevel = newState.CompositeRiskScore switch
            {
                >= 0.75 => "Critical",
                >= 0.5 => "High",
                >= 0.25 => "Moderate",
                _ => "Low"
            };

            _currentRiskStates[athleteId] = newState;
            return newState;
        }

        /// <summary>
        /// Calculate CRS as weighted average of sub-factors.
        /// Formula: CRS = 0.3*InjuryRisk + 0.25*PerfDecline + 0.25*RepRisk + 0.2*MarketVol
        /// </summary>
        private double _calculateCRS(List<Capsule> capsules, List<NILMoment> nilMoments)
        {
            if (capsules.Count == 0 && nilMoments.Count == 0)
                return 0.0;

            var injuryRisk = _calculateInjuryRisk(capsules);
            var perfDecline = _calculatePerformanceDecline(capsules);
            var repRisk = _calculateReputationalRisk(nilMoments);
            var marketVol = _calculateMarketVolatility(nilMoments);

            var crs = (0.3 * injuryRisk) + (0.25 * perfDecline) + (0.25 * repRisk) + (0.2 * marketVol);
            return Math.Min(1.0, Math.Max(0.0, crs));
        }

        /// <summary>
        /// Injury Risk: Heart rate volatility + acceleration spikes + fatigue indicators.
        /// </summary>
        private double _calculateInjuryRisk(List<Capsule> capsules)
        {
            if (capsules.Count == 0) return 0.0;

            var avgHeartRate = capsules.Average(c => c.HeartRate);
            var heartRateVariance = capsules.Count > 1
                ? capsules.Select(c => Math.Pow(c.HeartRate - avgHeartRate, 2)).Average()
                : 0;

            var maxAcceleration = capsules.Select(c => Math.Abs(c.Acceleration)).Max();
            var injuryRisk = Math.Min(1.0,
                (Math.Sqrt(heartRateVariance) / 50.0) * 0.5 +  // HR volatility
                Math.Min(1.0, maxAcceleration / 20.0) * 0.5);   // Acceleration spike

            return injuryRisk;
        }

        /// <summary>
        /// Performance Decline: Fatigue score + recovery time needed.
        /// </summary>
        private double _calculatePerformanceDecline(List<Capsule> capsules)
        {
            if (capsules.Count == 0) return 0.0;

            var avgHeartRate = capsules.Average(c => c.HeartRate);
            var timespan = (capsules.Max(c => c.CapturedAt) - capsules.Min(c => c.CapturedAt)).TotalSeconds;

            // If HR stays elevated and time > 30 min, fatigue is high
            var fatigueFactor = avgHeartRate > 150 && timespan > 1800 ? 0.8 : 
                                avgHeartRate > 130 && timespan > 1200 ? 0.5 : 0.2;

            return Math.Min(1.0, fatigueFactor);
        }

        /// <summary>
        /// Reputational Risk: Inverse correlation of NIL moment virality.
        /// High virality + negative sentiment = high reputational risk.
        /// </summary>
        private double _calculateReputationalRisk(List<NILMoment> nilMoments)
        {
            if (nilMoments.Count == 0) return 0.0;

            var recentMoments = nilMoments.Where(n => n.CreatedAt > DateTime.UtcNow.AddMinutes(-60)).ToList();
            if (recentMoments.Count == 0) return 0.0;

            var avgNILValue = recentMoments.Average(n => n.EstimatedNilValue);
            var volatility = recentMoments.Any() ? recentMoments.Select(n => n.ViralityScore).Max() : 0;

            // High volatility in NIL value = reputational risk
            var repRisk = volatility > 0.8 && avgNILValue < 50000 ? 0.7 : 
                          volatility > 0.5 ? 0.4 : 0.1;

            return Math.Min(1.0, repRisk);
        }

        /// <summary>
        /// Market Volatility: Coefficient of variation in NIL valuations.
        /// </summary>
        private double _calculateMarketVolatility(List<NILMoment> nilMoments)
        {
            if (nilMoments.Count < 2) return 0.0;

            var values = nilMoments.Select(n => n.EstimatedNilValue).ToList();
            var mean = values.Average();
            var variance = values.Select(v => Math.Pow(v - mean, 2)).Average();
            var stdDev = Math.Sqrt(variance);
            var cv = mean > 0 ? stdDev / mean : 0;

            return Math.Min(1.0, cv);
        }

        /// <summary>
        /// Map NIL moments to cross-asset exposures.
        /// </summary>
        private List<CrossAssetExposure> _mapCrossAssetExposures(List<NILMoment> nilMoments)
        {
            var exposures = new List<CrossAssetExposure>();

            foreach (var moment in nilMoments.Where(n => !string.IsNullOrEmpty(n.RelatedAssetClass)))
            {
                exposures.Add(new CrossAssetExposure
                {
                    AssetClass = moment.RelatedAssetClass,
                    RelatedEntity = moment.SuggestedSponsor ?? "Unknown",
                    BetaExposure = moment.ImpactOnEquityBeta,
                    CorrelationToAthlete = moment.SponsorshipDesirability,
                    ImpactIfAthleteFails = moment.EstimatedNilValue * 0.5  // Rough estimate
                });
            }

            return exposures;
        }

        /// <summary>
        /// Generate institutional hedge action recommendations.
        /// </summary>
        private List<HedgeAction> _generateHedgeActions(string athleteId, List<Capsule> capsules, List<NILMoment> nilMoments)
        {
            var actions = new List<HedgeAction>();

            var crs = _calculateCRS(capsules, nilMoments);
            var marketVol = _calculateMarketVolatility(nilMoments);
            var repRisk = _calculateReputationalRisk(nilMoments);

            // If CRS is high, recommend exposure reduction
            if (crs > 0.6)
            {
                actions.Add(new HedgeAction
                {
                    Strategy = "Reduce_Exposure",
                    ActionDescription = $"Reduce position in {athleteId} sponsorship equities by 30-50%",
                    RecommendedNotional = 500000,
                    EstimatedEffectiveness = 0.75,
                    Priority = crs > 0.8 ? "Urgent" : "High",
                    SuggestedExecutionTime = DateTime.UtcNow.AddMinutes(5)
                });
            }

            // If market volatility is high, recommend vol hedge
            if (marketVol > 0.6)
            {
                actions.Add(new HedgeAction
                {
                    Strategy = "Volatility_Hedge",
                    ActionDescription = "Execute VIX call spread or collar on related equity indices",
                    RecommendedNotional = 250000,
                    EstimatedEffectiveness = 0.6,
                    Priority = "Medium",
                    SuggestedExecutionTime = DateTime.UtcNow.AddMinutes(10)
                });
            }

            // If reputational risk is elevated, recommend rotation
            if (repRisk > 0.5)
            {
                actions.Add(new HedgeAction
                {
                    Strategy = "Rotation_Trade",
                    ActionDescription = "Rotate from {athleteId} into competitor athlete NIL positions",
                    RecommendedNotional = 300000,
                    EstimatedEffectiveness = 0.65,
                    Priority = "High",
                    SuggestedExecutionTime = DateTime.UtcNow.AddMinutes(15)
                });
            }

            return actions;
        }

        /// <summary>
        /// Get current risk state for athlete.
        /// </summary>
        public RiskState GetCurrentRiskState(string athleteId)
        {
            return _currentRiskStates.ContainsKey(athleteId)
                ? _currentRiskStates[athleteId]
                : null;
        }
    }
}
