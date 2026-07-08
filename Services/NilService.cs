using System;
using System.Collections.Generic;
using BIMAPES.Matchday.App.Models;

namespace BIMAPES.Matchday.App.Services
{
    /// <summary>
    /// Detects and manages NIL (Name, Image, Likeness) moments.
    /// Auto-detects high-value performance spikes.
    /// </summary>
    public class NilService
    {
        private readonly Random _rng = new Random();

        /// <summary>
        /// Detect NIL moments from athlete performance.
        /// </summary>
        public List<NilMoment> DetectNilMoments(List<Capsule> recentCapsules)
        {
            var moments = new List<NilMoment>();

            foreach (var capsule in recentCapsules)
            {
                // Simple spike detection: HR > 170 + high acceleration
                double accelMag = System.Math.Sqrt(
                    capsule.AccelX * capsule.AccelX +
                    capsule.AccelY * capsule.AccelY +
                    capsule.AccelZ * capsule.AccelZ
                );

                if (capsule.HeartRate > 170 && accelMag > 15)
                {
                    moments.Add(new NilMoment
                    {
                        NilMomentId = Guid.NewGuid().ToString("N").Substring(0, 8),
                        AthleteId = capsule.AthleteId,
                        EventTime = capsule.Timestamp,
                        EventType = _getEventType(capsule),
                        EstimatedValue = _rng.Next(5000, 25000),  // USD
                        PerformanceSpike = (capsule.HeartRate / 200.0) * (accelMag / 25.0),
                        CapsuleHash = capsule.Hash,
                        IsAutoDetected = true
                    });
                }
            }

            return moments;
        }

        /// <summary>
        /// Estimate event type from capsule features.
        /// </summary>
        private string _getEventType(Capsule capsule)
        {
            double accelMag = System.Math.Sqrt(
                capsule.AccelX * capsule.AccelX +
                capsule.AccelY * capsule.AccelY +
                capsule.AccelZ * capsule.AccelZ
            );

            if (accelMag > 20)
                return "explosive_action";  // Goal, sprint, collision
            else if (capsule.HeartRate > 180)
                return "high_intensity";
            else
                return "performance_spike";
        }
    }
}
