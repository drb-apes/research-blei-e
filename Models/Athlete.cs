namespace BIMAPES.Matchday.App.Models
{
    /// <summary>
    /// Athlete profile and metadata.
    /// </summary>
    public class Athlete
    {
        public string AthleteId { get; set; }
        public string Name { get; set; }
        public int JerseyNumber { get; set; }
        public string Position { get; set; }
        public DateTime DateOfBirth { get; set; }
        public double Height { get; set; }  // cm
        public double Weight { get; set; }  // kg
        public string PhotoUrl { get; set; }
        
        // Current status
        public bool IsActive { get; set; }
        public DateTime LastCapsuleTime { get; set; }
        public Indices CurrentIndices { get; set; }
    }
}
