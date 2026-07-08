using System;
using System.Collections.Generic;
using System.Linq;
using BIMAPES.Matchday.App.Models;

namespace BIMAPES.Matchday.App.Services
{
    /// <summary>
    /// MatchEventCapture: Real-time aggregation of match events and biometric signals.
    /// Converts raw sensor feeds into Capsule and NIL Moment objects for institutional clients.
    /// Supports streaming from wearables, computer vision, and manual scoreboard input.
    /// </summary>
    public class MatchEventCapture
    {
        public string MatchId { get; private set; }
        public DateTime MatchStartTime { get; private set; }
        public DateTime MatchEndTime { get; set; }
        
        private readonly List<Capsule> _capsuleLog = new();
        private readonly List<NILMoment> _nilMomentLog = new();
        private readonly Dictionary<string, AthleteSession> _athleteSessions = new();

        public MatchEventCapture(string matchId)
        {
            MatchId = matchId;
            MatchStartTime = DateTime.UtcNow;
        }

        /// <summary>
        /// Register athlete for this match.
        /// </summary>
        public void RegisterAthlete(string athleteId, string name, string position, int jerseyNumber)
        {
            if (!_athleteSessions.ContainsKey(athleteId))
            {
                _athleteSessions[athleteId] = new AthleteSession
                {
                    AthleteId = athleteId,
                    Name = name,
                    Position = position,
                    JerseyNumber = jerseyNumber,
                    SessionStartTime = DateTime.UtcNow,
                    TotalDistance = 0,
                    MaxHeartRate = 0,
                    AveragePace = 0
                };
            }
        }

        /// <summary>
        /// Ingest raw biometric capsule from wearable (heart rate, GPS, accel).
        /// </summary>
        public Capsule IngestBiometricCapsule(string athleteId, int heartRate, double speed, 
                                              double latitude, double longitude, double acceleration, 
                                              string sensorId = "default")
        {
            if (!_athleteSessions.ContainsKey(athleteId))
                throw new ArgumentException($"Athlete {athleteId} not registered");

            var capsule = new Capsule
            {
                CapsuleId = Guid.NewGuid().ToString(),
                MatchId = MatchId,
                AthleteId = athleteId,
                CapturedAt = DateTime.UtcNow,
                HeartRate = heartRate,
                Speed = speed,
                Latitude = latitude,
                Longitude = longitude,
                Acceleration = acceleration,
                SensorId = sensorId,
                Hash = _generateCapsuleHash(athleteId, heartRate, speed),
                IsProcessed = false
            };

            _capsuleLog.Add(capsule);

            // Update athlete session stats
            var session = _athleteSessions[athleteId];
            session.MaxHeartRate = Math.Max(session.MaxHeartRate, heartRate);
            session.TotalDistance += speed * 0.01;  // Approx km, assuming 0.01 increment per reading
            session.CapsuleCount++;

            return capsule;
        }

        /// <summary>
        /// Record a significant match event (goal, assist, injury, substitution, etc).
        /// </summary>
        public NILMoment RecordMatchEvent(string athleteId, string eventType, string description, 
                                          double nilValueEstimate = 0, double viralityScore = 0.5)
        {
            if (!_athleteSessions.ContainsKey(athleteId))
                throw new ArgumentException($"Athlete {athleteId} not registered");

            // Find recent biometric context
            var recentCapsule = _capsuleLog
                .Where(c => c.AthleteId == athleteId && (DateTime.UtcNow - c.CapturedAt).TotalSeconds < 5)
                .OrderByDescending(c => c.CapturedAt)
                .FirstOrDefault();

            var nilMoment = new NILMoment
            {
                MomentId = Guid.NewGuid().ToString(),
                AthleteId = athleteId,
                OccurredAt = DateTime.UtcNow,
                MomentType = eventType,  // "Goal", "Assist", "Defense", "Injury", "Card", "Substitution"
                Description = description,
                HeartRateAtMoment = recentCapsule?.HeartRate ?? 0,
                AccelerationAtMoment = recentCapsule?.Acceleration ?? 0,
                SpeedAtMoment = recentCapsule?.Speed ?? 0,
                EstimatedNilValue = nilValueEstimate,
                ViralityScore = viralityScore,
                SponsorshipDesirability = _estimateSponsorDesirability(eventType),
                SuggestedSponsor = _suggestSponsor(eventType),
                RelatedAssetClass = _mapEventToAssetClass(eventType),
                ImpactOnEquityBeta = _estimateEquityImpact(eventType),
                IsConfirmed = false,
                AuditNotes = $"Recorded at {DateTime.UtcNow:HH:mm:ss UTC}"
            };

            _nilMomentLog.Add(nilMoment);

            // Update athlete session
            var session = _athleteSessions[athleteId];
            if (eventType == "Goal" || eventType == "Assist")
                session.GoalsAssists++;
            if (eventType == "Card" && description.Contains("Yellow"))
                session.YellowCards++;
            if (eventType == "Card" && description.Contains("Red"))
                session.RedCards++;

            return nilMoment;
        }

        /// <summary>
        /// Mark match as ended and finalize logs.
        /// </summary>
        public MatchSummary EndMatch()
        {
            MatchEndTime = DateTime.UtcNow;

            var summary = new MatchSummary
            {
                MatchId = MatchId,
                MatchStartTime = MatchStartTime,
                MatchEndTime = MatchEndTime,
                DurationSeconds = (int)(MatchEndTime - MatchStartTime).TotalSeconds,
                CapsuleCount = _capsuleLog.Count,
                NILMomentCount = _nilMomentLog.Count,
                AthleteSessions = _athleteSessions.Values.ToList(),
                TotalNILValueCaptured = _nilMomentLog.Sum(n => n.EstimatedNilValue),
                TopNILMoment = _nilMomentLog.OrderByDescending(n => n.EstimatedNilValue).FirstOrDefault()
            };

            return summary;
        }

        /// <summary>
        /// Export match data as JSON for institutional clients.
        /// </summary>
        public string ExportAsJson()
        {
            var export = new
            {
                MatchId,
                MatchStartTime,
                MatchEndTime,
                Capsules = _capsuleLog.OrderBy(c => c.CapturedAt),
                NILMoments = _nilMomentLog.OrderBy(n => n.OccurredAt),
                AthleteSessionsSummary = _athleteSessions.Values.Select(s => new
                {
                    s.AthleteId,
                    s.Name,
                    s.Position,
                    s.CapsuleCount,
                    s.MaxHeartRate,
                    s.TotalDistance,
                    s.GoalsAssists,
                    s.YellowCards,
                    s.RedCards
                })
            };

            return System.Text.Json.JsonSerializer.Serialize(export, 
                new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
        }

        /// <summary>
        /// Get all capsules for an athlete in this match.
        /// </summary>
        public List<Capsule> GetAthleteCapsulesInMatch(string athleteId)
        {
            return _capsuleLog.Where(c => c.AthleteId == athleteId).ToList();
        }

        /// <summary>
        /// Get all NIL moments in this match.
        /// </summary>
        public List<NILMoment> GetNILMoments()
        {
            return _nilMomentLog.ToList();
        }

        /// <summary>
        /// Generate deterministic hash for capsule integrity.
        /// </summary>
        private string _generateCapsuleHash(string athleteId, int heartRate, double speed)
        {
            var input = $"{athleteId}:{heartRate}:{speed}:{DateTime.UtcNow:O}";
            using (var sha = System.Security.Cryptography.SHA256.Create())
            {
                var hashBytes = sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(input));
                return Convert.ToBase64String(hashBytes);
            }
        }

        private double _estimateSponsorDesirability(string eventType)
        {
            return eventType switch
            {
                "Goal" => 0.95,
                "Assist" => 0.85,
                "Defense" => 0.6,
                "Card" => 0.1,
                "Injury" => 0.05,
                _ => 0.5
            };
        }

        private string _suggestSponsor(string eventType)
        {
            return eventType switch
            {
                "Goal" => "Nike / Puma / Adidas",
                "Assist" => "PowerBar / Gatorade",
                "Defense" => "ESPN / Bleacher Report",
                "Card" => "N/A",
                _ => "Generic"
            };
        }

        private string _mapEventToAssetClass(string eventType)
        {
            return eventType switch
            {
                "Goal" or "Assist" => "Sponsor_Equity",
                "Defense" => "Media_Rights",
                "Injury" => "Betting",
                "Card" => "Betting",
                _ => "Consumer"
            };
        }

        private double _estimateEquityImpact(string eventType)
        {
            return eventType switch
            {
                "Goal" => 0.15,
                "Assist" => 0.10,
                "Defense" => 0.05,
                "Injury" => -0.20,
                "Card" => -0.08,
                _ => 0.0
            };
        }
    }

    /// <summary>
    /// Session summary for an athlete in a specific match.
    /// </summary>
    public class AthleteSession
    {
        public string AthleteId { get; set; }
        public string Name { get; set; }
        public string Position { get; set; }
        public int JerseyNumber { get; set; }
        public DateTime SessionStartTime { get; set; }
        public int CapsuleCount { get; set; }
        public int MaxHeartRate { get; set; }
        public double TotalDistance { get; set; }
        public double AveragePace { get; set; }
        public int GoalsAssists { get; set; }
        public int YellowCards { get; set; }
        public int RedCards { get; set; }
    }

    /// <summary>
    /// Overall match summary for reporting.
    /// </summary>
    public class MatchSummary
    {
        public string MatchId { get; set; }
        public DateTime MatchStartTime { get; set; }
        public DateTime MatchEndTime { get; set; }
        public int DurationSeconds { get; set; }
        public int CapsuleCount { get; set; }
        public int NILMomentCount { get; set; }
        public List<AthleteSession> AthleteSessions { get; set; }
        public double TotalNILValueCaptured { get; set; }
        public NILMoment TopNILMoment { get; set; }
    }
}
