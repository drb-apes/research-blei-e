namespace BIMAPES.Matchday.App.Models
{
    /// <summary>
    /// Computed performance indices from capsule data.
    /// Maps biometric signals to financial/trading factors.
    /// </summary>
    public class Indices
    {
        // Biomechanical factors
        public double Readiness { get; set; }              // 0-1 scale
        public double AcuteLoad { get; set; }             // 0-1 scale
        public double Asymmetry { get; set; }             // -1 to 1 (bilateral difference)
        public double InjuryRisk { get; set; }            // 0-1 scale
        public double SubstitutionProbability { get; set; } // 0-1 scale
        
        // Market-equivalent factors
        public double PmfE { get; set; }   // Physical Match Fitness (momentum)
        public double CseE { get; set; }   // Cortisol Stress Exposure (volatility)
        public double FbreE { get; set; }  // Fan Behavior & Revenue (sentiment)
        public double VveE { get; set; }   // Virality Velocity (media beta)
        public double SefeE { get; set; }  // Skill Execution (quality)
    }
}
