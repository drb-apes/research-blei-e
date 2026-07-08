using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using BIMAPES.Matchday.App.Models;
using BIMAPES.Matchday.App.Services;

namespace BIMAPES.Matchday.App.ViewModels
{
    /// <summary>
    /// ViewModel for portfolio alerts dashboard.
    /// Displays real-time CRS, hedge recommendations, and NIL exposure.
    /// Designed for hedge fund operations teams.
    /// </summary>
    public class EquityAlertViewModel : BaseViewModel
    {
        private readonly RealTimeRiskEngine _riskEngine = new();
        private readonly MatchEventCapture _matchCapture;

        private RiskState _selectedRiskState;
        private double _portfolioHedgeNotional;
        private int _urgentAlertCount;

        public ObservableCollection<RiskState> RiskStates { get; } = new();
        public ObservableCollection<HedgeAction> SuggestedHedges { get; } = new();
        public ObservableCollection<NILMoment> RecentNILMoments { get; } = new();

        public RiskState SelectedRiskState
        {
            get => _selectedRiskState;
            set
            {
                if (_selectedRiskState != value)
                {
                    _selectedRiskState = value;
                    OnPropertyChanged();
                    _updateHedgesForSelectedAthlete();
                }
            }
        }

        public double PortfolioHedgeNotional
        {
            get => _portfolioHedgeNotional;
            set { if (_portfolioHedgeNotional != value) { _portfolioHedgeNotional = value; OnPropertyChanged(); } }
        }

        public int UrgentAlertCount
        {
            get => _urgentAlertCount;
            set { if (_urgentAlertCount != value) { _urgentAlertCount = value; OnPropertyChanged(); } }
        }

        public EquityAlertViewModel(string matchId = null)
        {
            _matchCapture = matchId != null ? new MatchEventCapture(matchId) : null;
            _initializeDemo();
        }

        /// <summary>
        /// Ingest biometric capsule and update risk dashboard.
        /// </summary>
        public void IngestCapsule(string athleteId, Capsule capsule)
        {
            var riskState = _riskEngine.IngestCapsule(capsule, athleteId);
            _updateRiskStateDisplay(riskState);
        }

        /// <summary>
        /// Ingest NIL moment and trigger alerts.
        /// </summary>
        public void IngestNILMoment(string athleteId, NILMoment nilMoment)
        {
            var riskState = _riskEngine.IngestNILMoment(nilMoment, athleteId);
            _updateRiskStateDisplay(riskState);
            RecentNILMoments.Insert(0, nilMoment);

            // Keep only last 50 moments
            while (RecentNILMoments.Count > 50)
                RecentNILMoments.RemoveAt(RecentNILMoments.Count - 1);

            // Trigger alert if NIL value spike or CRS deterioration
            if (nilMoment.EstimatedNilValue > 100000 || riskState.CrsDelta > 0.1)
            {
                _triggerAlert(athleteId, riskState);
            }
        }

        /// <summary>
        /// Refresh dashboard with latest risk states.
        /// </summary>
        public void Refresh()
        {
            _updatePortfolioMetrics();
        }

        /// <summary>
        /// Execute hedge action via institutional API.
        /// </summary>
        public void ExecuteHedge(HedgeAction hedge)
        {
            hedge.SuggestedExecutionTime = DateTime.UtcNow;
            // TODO: Call institutional trading API (Bloomberg, Thomson Reuters, etc.)
            System.Diagnostics.Debug.WriteLine($"Hedge executed: {hedge.Strategy} | Notional: ${hedge.RecommendedNotional}");
        }

        /// <summary>
        /// Export dashboard state to JSON for auditors.
        /// </summary>
        public string ExportDashboardState()
        {
            var export = new
            {
                Timestamp = DateTime.UtcNow,
                PortfolioHedgeNotional,
                UrgentAlertCount,
                RiskStates = RiskStates.Select(rs => new
                {
                    rs.AthleteId,
                    rs.CompositeRiskScore,
                    rs.RiskLevel,
                    rs.Trend,
                    rs.SuggestedHedges.Count
                }),
                RecentNILMoments = RecentNILMoments.Take(10).Select(n => new
                {
                    n.AthleteId,
                    n.MomentType,
                    n.EstimatedNilValue,
                    n.ViralityScore
                }),
                SuggestedHedges = SuggestedHedges.Select(h => new
                {
                    h.Strategy,
                    h.RecommendedNotional,
                    h.Priority
                })
            };

            return System.Text.Json.JsonSerializer.Serialize(export,
                new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
        }

        // --- Private Methods ---

        private void _initializeDemo()
        {
            // Create demo athletes
            var athleteIds = new[] { "A-1", "A-2", "A-3", "A-4", "A-5" };

            foreach (var athleteId in athleteIds)
            {
                var mockRiskState = new RiskState
                {
                    AthleteId = athleteId,
                    CompositeRiskScore = new Random().NextDouble() * 0.7,
                    RiskLevel = "Low",
                    InjuryRiskFactor = new Random().NextDouble() * 0.3,
                    PerformanceDeclineFactor = new Random().NextDouble() * 0.2,
                    ReputationalRiskFactor = new Random().NextDouble() * 0.15,
                    MarketVolatilityFactor = new Random().NextDouble() * 0.2,
                    Trend = "Stable"
                };

                RiskStates.Add(mockRiskState);
            }

            _updatePortfolioMetrics();
        }

        private void _updateRiskStateDisplay(RiskState riskState)
        {
            var existing = RiskStates.FirstOrDefault(rs => rs.AthleteId == riskState.AthleteId);
            if (existing != null)
            {
                var index = RiskStates.IndexOf(existing);
                RiskStates[index] = riskState;
            }
            else
            {
                RiskStates.Add(riskState);
            }

            if (SelectedRiskState?.AthleteId == riskState.AthleteId)
                SelectedRiskState = riskState;
        }

        private void _updateHedgesForSelectedAthlete()
        {
            SuggestedHedges.Clear();

            if (SelectedRiskState?.SuggestedHedges != null)
            {
                foreach (var hedge in SelectedRiskState.SuggestedHedges)
                {
                    SuggestedHedges.Add(hedge);
                }
            }

            _updatePortfolioMetrics();
        }

        private void _updatePortfolioMetrics()
        {
            var totalHedgeNotional = 0.0;
            var urgentCount = 0;

            foreach (var riskState in RiskStates)
            {
                foreach (var hedge in riskState.SuggestedHedges)
                {
                    totalHedgeNotional += hedge.RecommendedNotional;
                    if (hedge.Priority == "Urgent")
                        urgentCount++;
                }
            }

            PortfolioHedgeNotional = totalHedgeNotional;
            UrgentAlertCount = urgentCount;
        }

        private void _triggerAlert(string athleteId, RiskState riskState)
        {
            var severity = riskState.RiskLevel switch
            {
                "Critical" => "🔴 CRITICAL",
                "High" => "🟠 HIGH",
                "Moderate" => "🟡 MODERATE",
                _ => "🟢 LOW"
            };

            var message = $"{severity} ALERT: {athleteId} | CRS: {riskState.CompositeRiskScore:F2} | Trend: {riskState.Trend}";
            System.Diagnostics.Debug.WriteLine(message);

            // TODO: Send Slack / Email notification
        }
    }

    /// <summary>
    /// Base ViewModel with INotifyPropertyChanged implementation.
    /// </summary>
    public abstract class BaseViewModel : System.ComponentModel.INotifyPropertyChanged
    {
        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
        }
    }
}
