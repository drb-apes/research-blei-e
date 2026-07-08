using BIMAPES.Matchday.App.Models;

namespace BIMAPES.Matchday.App.Services
{
    /// <summary>
    /// Computes BLEI-E factors from capsule data.
    /// Maps biometric signals to performance indices.
    /// </summary>
    public class IndicesService
    {
        /// <summary>
        /// Compute all indices from a single capsule.
        /// </summary>
        public Indices ComputeIndices(Capsule capsule)
        {
            // Normalize values to 0-1 range
            double hrNorm = (capsule.HeartRate - 60.0) / 180.0;  // Typical range: 60-240 bpm
            double hrvNorm = capsule.Hrv / 100.0;  // Typical range: 0-100 ms
            double accelMag = System.Math.Sqrt(
                capsule.AccelX * capsule.AccelX +
                capsule.AccelY * capsule.AccelY +
                capsule.AccelZ * capsule.AccelZ
            );
            double accelNorm = System.Math.Min(1.0, accelMag / 25.0);  // Typical max: ~25 m/s^2
            double speedNorm = System.Math.Min(1.0, capsule.GpsSpeed / 10.0);

            return new Indices
            {
                // Biomechanical indices
                Readiness = System.Math.Max(0, hrvNorm - (hrNorm * 0.2)),  // Higher HRV, lower HR = more ready
                AcuteLoad = hrNorm,  // HR as proxy for load
                Asymmetry = System.Math.Abs(capsule.AccelX - capsule.AccelY),  // Bilateral asymmetry
                InjuryRisk = capsule.HeartRate > 170 ? 0.7 : (capsule.HeartRate > 150 ? 0.3 : 0.1),
                SubstitutionProbability = hrNorm > 0.8 ? 0.6 : (hrNorm > 0.6 ? 0.3 : 0.1),
                
                // Market-equivalent factors (BLEI-E framework)
                PmfE = accelNorm,  // Physical Match Fitness (momentum)
                CseE = capsule.Gsr / 5.0,  // Cortisol/Stress (volatility)
                FbreE = speedNorm,  // Fan Behavior proxy (sentiment)
                VveE = (capsule.HeartRate > 160) ? 0.8 : 0.3,  // Virality spikes on high intensity
                SefeE = hrvNorm  // Skill execution (quality)
            };
        }
    }
}
