using System;
using System.Collections.ObjectModel;
using BIMAPES.Matchday.App.Models;

namespace BIMAPES.Matchday.App.Services
{
    /// <summary>
    /// Simulates capsule ingestion from Microchip v1.
    /// In production, this would connect to real hardware via USB/network.
    /// </summary>
    public class CapsuleService
    {
        private readonly Random _rng = new Random();

        /// <summary>
        /// Generate live capsules for demo purposes.
        /// Replace with real hardware integration.
        /// </summary>
        public ObservableCollection<Capsule> GetLiveCapsules()
        {
            var list = new ObservableCollection<Capsule>();

            for (int i = 0; i < 8; i++)
            {
                list.Add(new Capsule
                {
                    CapsuleId = $"C-{1000 + i}",
                    AthleteId = $"A-{i + 1}",
                    Timestamp = DateTime.UtcNow,
                    HeartRate = _rng.Next(120, 180),
                    Hrv = _rng.Next(30, 80),
                    AccelX = _rng.NextDouble() * 10,
                    AccelY = _rng.NextDouble() * 10,
                    AccelZ = _rng.NextDouble() * 10,
                    GpsSpeed = Math.Round(_rng.NextDouble() * 9, 2),
                    SpO2 = 95.0 + _rng.NextDouble() * 5,
                    Gsr = 1.0 + _rng.NextDouble() * 4,
                    Hash = Guid.NewGuid().ToString("N").Substring(0, 16),
                    IngestLatencyMs = _rng.Next(100, 300)
                });
            }

            return list;
        }

        /// <summary>
        /// Simulate real-time update (for live streaming).
        /// </summary>
        public Capsule GetNextCapsule(string athleteId)
        {
            return new Capsule
            {
                CapsuleId = Guid.NewGuid().ToString("N").Substring(0, 8),
                AthleteId = athleteId,
                Timestamp = DateTime.UtcNow,
                HeartRate = _rng.Next(120, 180),
                Hrv = _rng.Next(30, 80),
                AccelX = _rng.NextDouble() * 10,
                AccelY = _rng.NextDouble() * 10,
                AccelZ = _rng.NextDouble() * 10,
                GpsSpeed = Math.Round(_rng.NextDouble() * 9, 2),
                SpO2 = 95.0 + _rng.NextDouble() * 5,
                Gsr = 1.0 + _rng.NextDouble() * 4,
                Hash = Guid.NewGuid().ToString("N").Substring(0, 16),
                IngestLatencyMs = _rng.Next(100, 300)
            };
        }
    }
}
