using System.Collections.ObjectModel;
using BIMAPES.Matchday.App.Models;
using BIMAPES.Matchday.App.Services;

namespace BIMAPES.Matchday.App.ViewModels
{
    /// <summary>
    /// ViewModel for main window (live capture screen).
    /// Binds capsule data and indices to UI.
    /// </summary>
    public class MainViewModel
    {
        public ObservableCollection<Capsule> Capsules { get; set; }
        public ObservableCollection<Athlete> Athletes { get; set; }

        private readonly CapsuleService _capsuleService = new CapsuleService();
        private readonly IndicesService _indicesService = new IndicesService();

        public MainViewModel()
        {
            Capsules = _capsuleService.GetLiveCapsules();
            Athletes = new ObservableCollection<Athlete>();
            _initializeAthletes();
        }

        private void _initializeAthletes()
        {
            for (int i = 1; i <= 8; i++)
            {
                Athletes.Add(new Athlete
                {
                    AthleteId = $"A-{i}",
                    Name = $"Player {i}",
                    JerseyNumber = 10 + i,
                    Position = _getPosition(i),
                    IsActive = true
                });
            }
        }

        private string _getPosition(int index)
        {
            var positions = new[] { "GK", "DEF", "DEF", "MID", "MID", "MID", "FWD", "FWD" };
            return positions[index - 1];
        }
    }
}
