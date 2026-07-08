namespace BIMAPES.Matchday.App.Models
{
    /// <summary>
    /// Represents a single biometric data packet ("capsule") from Microchip v1.
    /// Contains all sensor readings at a specific timestamp.
    /// </summary>
    public class Capsule
    {
        public string CapsuleId { get; set; }
        public string AthleteId { get; set; }
        public DateTime Timestamp { get; set; }
        
        // Vital signs
        public int HeartRate { get; set; }  // bpm
        public int Hrv { get; set; }        // ms
        
        // Movement
        public double AccelX { get; set; }  // m/s^2
        public double AccelY { get; set; }
        public double AccelZ { get; set; }
        public double GpsSpeed { get; set; } // m/s
        
        // Integrity
        public string Hash { get; set; }    // SHA256
        public int IngestLatencyMs { get; set; }
        
        // Additional metrics
        public double SpO2 { get; set; }    // %
        public double Gsr { get; set; }     // microSiemens
    }
}
