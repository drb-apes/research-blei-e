using System;
using System.Collections.Generic;
using System.Linq;

namespace BIMAPES.Matchday.App.Models
{
    /// <summary>
    /// NILIndex: Real-time biometric-linked equity index for athlete performance tracking.
    /// Tracks athlete NIL valuation across multiple asset classes (sponsor equity, media, betting).
    /// 
    /// Index Methodology:
    /// - Base Level: 1000 (inception date)
    /// - Components: 20-50 top-tier athletes per league
    /// - Rebalancing: Monthly
    /// - Real-time Updates: Every performance event (goal, assist, injury, viral moment)
    /// </summary>
    public class NILIndex
    {
        public string IndexCode { get; set; }  // e.g., "BLEI-NBA", "BLEI-EPL", "BLEI-NFL"
        public string IndexName { get; set; }
        public string League { get; set; }  // NBA, EPL, NFL, MLB, MLS, etc.
        public string Country { get; set; }
        public DateTime InceptionDate { get; set; }
        
        // Index Level & Returns
        public double CurrentLevel { get; set; }
        public double PreviousClose { get; set; }
        public double DayChange { get; set; }
        public double DayChangePercent { get; set; }
        public double YearToDateReturn { get; set; }
        
        // Index Composition
        public int ComponentCount { get; set; }
        public double TotalMarketCap { get; set; }  // Sum of all component NIL values
        public List<NILIndexComponent> Components { get; set; } = new();
        
        // Volatility & Risk Metrics
        public double AnnualizedVolatility { get; set; }  // 252-day rolling vol
        public double Beta { get; set; }  // vs S&P 500
        public double Correlation { get; set; }  // vs sport-specific market drivers
        public double MaxDrawdown { get; set; }  // Peak-to-trough decline
        
        // Sector Breakdown
        public Dictionary<string, double> SectorWeights { get; set; } = new();  // % by asset class
        
        // Rebalancing Info
        public DateTime LastRebalanceDate { get; set; }
        public DateTime NextRebalanceDate { get; set; }
        public double RebalanceTurnover { get; set; }  // % of index reweighted
        
        // Dividend / Distribution
        public double TrailingYield { get; set; }  // If paying distributions
        public List<IndexDistribution> Distributions { get; set; } = new();
        
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// NILIndexComponent: Individual athlete in a NIL Index.
    /// </summary>
    public class NILIndexComponent
    {
        public string ComponentId { get; set; } = Guid.NewGuid().ToString();
        public string AthleteId { get; set; }
        public string AthleteName { get; set; }
        public string Team { get; set; }
        public string Position { get; set; }
        public int JerseyNumber { get; set; }
        
        // Valuation
        public double NILValue { get; set; }  // Current USD valuation
        public double NILValueChange { get; set; }  // Change since last update
        public double IndexWeight { get; set; }  // % of total index
        
        // Performance Metrics
        public double CompositeRiskScore { get; set; }  // Latest CRS from biometrics
        public double PerformanceScore { get; set; }  // [0,1] normalized
        public double ViralityScore { get; set; }  // Social media / virality metric
        
        // Recent Activity
        public int GoalsLastGame { get; set; }
        public int AssistsLastGame { get; set; }
        public int YellowCardsLastGame { get; set; }
        public int RedCardsLastGame { get; set; }
        public DateTime LastGameDate { get; set; }
        
        // Sector Allocation
        public List<SectorExposure> SectorExposures { get; set; } = new();
        
        public DateTime AddedToIndexDate { get; set; }
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// SectorExposure: Breakdown of an athlete's NIL by asset class.
    /// </summary>
    public class SectorExposure
    {
        public string SectorCode { get; set; }  // "SPONSOR_EQ", "MEDIA", "BETTING", "CONSUMER"
        public string SectorName { get; set; }
        public double ExposureAmount { get; set; }  // USD
        public double ExposurePercent { get; set; }  // % of athlete's total
        public double Beta { get; set; }  // Sensitivity to sector
    }

    /// <summary>
    /// IndexDistribution: Dividend or special distribution from NIL Index.
    /// </summary>
    public class IndexDistribution
    {
        public string DistributionId { get; set; } = Guid.NewGuid().ToString();
        public DateTime ExDate { get; set; }
        public DateTime PaymentDate { get; set; }
        public double AmountPerShare { get; set; }
        public string DistributionType { get; set; }  // "Dividend", "Special", "Return of Capital"
    }

    /// <summary>
    /// NILIndexLibrary: Pre-built demo indices with realistic data.
    /// </summary>
    public static class NILIndexLibrary
    {
        /// <summary>
        /// Get all available NIL indices.
        /// </summary>
        public static List<NILIndex> GetAllIndices()
        {
            return new List<NILIndex>
            {
                GetNBAIndex(),
                GetNFLIndex(),
                GetEPLIndex(),
                GetMLBIndex(),
                GetMLSIndex(),
                GetBrooklynNetsIndex(),
                GetLosAngelesLakersIndex(),
                GetManchesterUnitedIndex()
            };
        }

        /// <summary>
        /// NBA Aggregate NIL Index - Top 50 players across all teams.
        /// </summary>
        public static NILIndex GetNBAIndex()
        {
            var index = new NILIndex
            {
                IndexCode = "BLEI-NBA",
                IndexName = "BLEI-E NBA NIL Index",
                League = "NBA",
                Country = "USA",
                InceptionDate = new DateTime(2025, 6, 1),
                CurrentLevel = 1247.50,
                PreviousClose = 1198.75,
                DayChange = 48.75,
                DayChangePercent = 4.07,
                YearToDateReturn = 24.75,
                ComponentCount = 50,
                TotalMarketCap = 15_750_000_000,  // $15.75B aggregate
                AnnualizedVolatility = 0.28,
                Beta = 1.15,
                Correlation = 0.68,
                MaxDrawdown = -0.18,
                TrailingYield = 0.032,
                LastRebalanceDate = new DateTime(2026, 7, 1),
                NextRebalanceDate = new DateTime(2026, 8, 1),
                RebalanceTurnover = 0.15,
                Components = new List<NILIndexComponent>
                {
                    new NILIndexComponent
                    {
                        AthleteId = "NBA-LJ23",
                        AthleteName = "LeBron James",
                        Team = "Los Angeles Lakers",
                        Position = "SF",
                        JerseyNumber = 23,
                        NILValue = 850_000_000,
                        IndexWeight = 0.054,
                        CompositeRiskScore = 0.32,
                        PerformanceScore = 0.92,
                        ViralityScore = 0.88,
                        GoalsLastGame = 28,
                        AssistsLastGame = 9,
                        LastGameDate = DateTime.UtcNow.AddDays(-1),
                        SectorExposures = new List<SectorExposure>
                        {
                            new SectorExposure { SectorCode = "SPONSOR_EQ", SectorName = "Nike", ExposureAmount = 425_000_000, ExposurePercent = 0.50, Beta = 1.2 },
                            new SectorExposure { SectorCode = "MEDIA", SectorName = "ESPN / Turner", ExposureAmount = 255_000_000, ExposurePercent = 0.30, Beta = 0.9 },
                            new SectorExposure { SectorCode = "CONSUMER", SectorName = "CPG Brands", ExposureAmount = 170_000_000, ExposurePercent = 0.20, Beta = 1.1 }
                        }
                    },
                    new NILIndexComponent
                    {
                        AthleteId = "NBA-KC30",
                        AthleteName = "Stephen Curry",
                        Team = "Golden State Warriors",
                        Position = "PG",
                        JerseyNumber = 30,
                        NILValue = 720_000_000,
                        IndexWeight = 0.046,
                        CompositeRiskScore = 0.28,
                        PerformanceScore = 0.89,
                        ViralityScore = 0.91,
                        GoalsLastGame = 31,
                        AssistsLastGame = 6,
                        LastGameDate = DateTime.UtcNow.AddDays(-1),
                        SectorExposures = new List<SectorExposure>
                        {
                            new SectorExposure { SectorCode = "SPONSOR_EQ", SectorName = "Under Armour", ExposureAmount = 360_000_000, ExposurePercent = 0.50, Beta = 1.3 },
                            new SectorExposure { SectorCode = "BETTING", SectorName = "DraftKings", ExposureAmount = 216_000_000, ExposurePercent = 0.30, Beta = 1.5 },
                            new SectorExposure { SectorCode = "CONSUMER", SectorName = "Tech / Gaming", ExposureAmount = 144_000_000, ExposurePercent = 0.20, Beta = 1.4 }
                        }
                    },
                    new NILIndexComponent
                    {
                        AthleteId = "NBA-JT25",
                        AthleteName = "Jayson Tatum",
                        Team = "Boston Celtics",
                        Position = "SF",
                        JerseyNumber = 0,
                        NILValue = 580_000_000,
                        IndexWeight = 0.037,
                        CompositeRiskScore = 0.35,
                        PerformanceScore = 0.87,
                        ViralityScore = 0.82,
                        GoalsLastGame = 26,
                        AssistsLastGame = 4,
                        LastGameDate = DateTime.UtcNow.AddDays(-1),
                        SectorExposures = new List<SectorExposure>
                        {
                            new SectorExposure { SectorCode = "SPONSOR_EQ", SectorName = "Jordan Brand / Nike", ExposureAmount = 290_000_000, ExposurePercent = 0.50, Beta = 1.1 },
                            new SectorExposure { SectorCode = "MEDIA", SectorName = "NBA Media", ExposureAmount = 174_000_000, ExposurePercent = 0.30, Beta = 0.85 },
                            new SectorExposure { SectorCode = "CONSUMER", SectorName = "Fashion / Lifestyle", ExposureAmount = 116_000_000, ExposurePercent = 0.20, Beta = 1.2 }
                        }
                    }
                },
                SectorWeights = new Dictionary<string, double>
                {
                    { "SPONSOR_EQ", 0.48 },
                    { "MEDIA", 0.28 },
                    { "BETTING", 0.12 },
                    { "CONSUMER", 0.12 }
                },
                LastUpdated = DateTime.UtcNow
            };

            return index;
        }

        /// <summary>
        /// NFL Aggregate NIL Index - Top 25 players across all teams.
        /// </summary>
        public static NILIndex GetNFLIndex()
        {
            var index = new NILIndex
            {
                IndexCode = "BLEI-NFL",
                IndexName = "BLEI-E NFL NIL Index",
                League = "NFL",
                Country = "USA",
                InceptionDate = new DateTime(2025, 8, 1),
                CurrentLevel = 1125.25,
                PreviousClose = 1092.50,
                DayChange = 32.75,
                DayChangePercent = 3.00,
                YearToDateReturn = 12.52,
                ComponentCount = 25,
                TotalMarketCap = 8_450_000_000,
                AnnualizedVolatility = 0.32,
                Beta = 1.08,
                Correlation = 0.55,
                MaxDrawdown = -0.22,
                TrailingYield = 0.025,
                LastRebalanceDate = new DateTime(2026, 7, 1),
                NextRebalanceDate = new DateTime(2026, 8, 1),
                RebalanceTurnover = 0.20,
                Components = new List<NILIndexComponent>
                {
                    new NILIndexComponent
                    {
                        AthleteId = "NFL-PM12",
                        AthleteName = "Patrick Mahomes",
                        Team = "Kansas City Chiefs",
                        Position = "QB",
                        JerseyNumber = 15,
                        NILValue = 625_000_000,
                        IndexWeight = 0.074,
                        CompositeRiskScore = 0.25,
                        PerformanceScore = 0.94,
                        ViralityScore = 0.89,
                        GoalsLastGame = 3,  // TDs in last game
                        AssistsLastGame = 2,
                        LastGameDate = DateTime.UtcNow.AddDays(-3),
                        SectorExposures = new List<SectorExposure>
                        {
                            new SectorExposure { SectorCode = "SPONSOR_EQ", SectorName = "Adidas", ExposureAmount = 312_500_000, ExposurePercent = 0.50, Beta = 1.0 },
                            new SectorExposure { SectorCode = "BETTING", SectorName = "FanDuel / DraftKings", ExposureAmount = 187_500_000, ExposurePercent = 0.30, Beta = 1.6 },
                            new SectorExposure { SectorCode = "CONSUMER", SectorName = "Fast Food / CPG", ExposureAmount = 125_000_000, ExposurePercent = 0.20, Beta = 1.0 }
                        }
                    },
                    new NILIndexComponent
                    {
                        AthleteId = "NFL-TM10",
                        AthleteName = "Travis Kelce",
                        Team = "Kansas City Chiefs",
                        Position = "TE",
                        JerseyNumber = 87,
                        NILValue = 480_000_000,
                        IndexWeight = 0.057,
                        CompositeRiskScore = 0.30,
                        PerformanceScore = 0.91,
                        ViralityScore = 0.93,
                        GoalsLastGame = 2,
                        AssistsLastGame = 1,
                        LastGameDate = DateTime.UtcNow.AddDays(-3),
                        SectorExposures = new List<SectorExposure>
                        {
                            new SectorExposure { SectorCode = "SPONSOR_EQ", SectorName = "Nike", ExposureAmount = 240_000_000, ExposurePercent = 0.50, Beta = 1.1 },
                            new SectorExposure { SectorCode = "MEDIA", SectorName = "Streaming / Entertainment", ExposureAmount = 144_000_000, ExposurePercent = 0.30, Beta = 1.2 },
                            new SectorExposure { SectorCode = "CONSUMER", SectorName = "Entertainment / Tech", ExposureAmount = 96_000_000, ExposurePercent = 0.20, Beta = 1.3 }
                        }
                    }
                },
                SectorWeights = new Dictionary<string, double>
                {
                    { "SPONSOR_EQ", 0.45 },
                    { "BETTING", 0.28 },
                    { "MEDIA", 0.15 },
                    { "CONSUMER", 0.12 }
                },
                LastUpdated = DateTime.UtcNow
            };

            return index;
        }

        /// <summary>
        /// EPL Aggregate NIL Index - Top 30 players across all teams.
        /// </summary>
        public static NILIndex GetEPLIndex()
        {
            var index = new NILIndex
            {
                IndexCode = "BLEI-EPL",
                IndexName = "BLEI-E Premier League NIL Index",
                League = "EPL",
                Country = "UK",
                InceptionDate = new DateTime(2025, 5, 1),
                CurrentLevel = 1089.75,
                PreviousClose = 1055.25,
                DayChange = 34.50,
                DayChangePercent = 3.27,
                YearToDateReturn = 8.97,
                ComponentCount = 30,
                TotalMarketCap = 6_750_000_000,
                AnnualizedVolatility = 0.25,
                Beta = 0.92,
                Correlation = 0.58,
                MaxDrawdown = -0.15,
                TrailingYield = 0.020,
                LastRebalanceDate = new DateTime(2026, 6, 1),
                NextRebalanceDate = new DateTime(2026, 7, 1),
                RebalanceTurnover = 0.12,
                Components = new List<NILIndexComponent>
                {
                    new NILIndexComponent
                    {
                        AthleteId = "EPL-CR7",
                        AthleteName = "Cristiano Ronaldo",
                        Team = "Al Nassr",
                        Position = "ST",
                        JerseyNumber = 7,
                        NILValue = 520_000_000,
                        IndexWeight = 0.077,
                        CompositeRiskScore = 0.38,
                        PerformanceScore = 0.88,
                        ViralityScore = 0.95,
                        GoalsLastGame = 2,
                        AssistsLastGame = 1,
                        LastGameDate = DateTime.UtcNow.AddDays(-1),
                        SectorExposures = new List<SectorExposure>
                        {
                            new SectorExposure { SectorCode = "SPONSOR_EQ", SectorName = "Nike", ExposureAmount = 260_000_000, ExposurePercent = 0.50, Beta = 0.95 },
                            new SectorExposure { SectorCode = "MEDIA", SectorName = "BT Sport / Sky Sports", ExposureAmount = 156_000_000, ExposurePercent = 0.30, Beta = 0.8 },
                            new SectorExposure { SectorCode = "CONSUMER", SectorName = "Luxury / Fashion", ExposureAmount = 104_000_000, ExposurePercent = 0.20, Beta = 1.0 }
                        }
                    },
                    new NILIndexComponent
                    {
                        AthleteId = "EPL-MB10",
                        AthleteName = "Mbappe",
                        Team = "Paris Saint-Germain",
                        Position = "ST",
                        JerseyNumber = 10,
                        NILValue = 495_000_000,
                        IndexWeight = 0.073,
                        CompositeRiskScore = 0.32,
                        PerformanceScore = 0.93,
                        ViralityScore = 0.92,
                        GoalsLastGame = 3,
                        AssistsLastGame = 2,
                        LastGameDate = DateTime.UtcNow.AddDays(-1),
                        SectorExposures = new List<SectorExposure>
                        {
                            new SectorExposure { SectorCode = "SPONSOR_EQ", SectorName = "Nike", ExposureAmount = 247_500_000, ExposurePercent = 0.50, Beta = 1.0 },
                            new SectorExposure { SectorCode = "MEDIA", SectorName = "Amazon Prime / Sky", ExposureAmount = 148_500_000, ExposurePercent = 0.30, Beta = 0.85 },
                            new SectorExposure { SectorCode = "CONSUMER", SectorName = "Technology / Luxury", ExposureAmount = 99_000_000, ExposurePercent = 0.20, Beta = 1.1 }
                        }
                    }
                },
                SectorWeights = new Dictionary<string, double>
                {
                    { "SPONSOR_EQ", 0.50 },
                    { "MEDIA", 0.28 },
                    { "BETTING", 0.10 },
                    { "CONSUMER", 0.12 }
                },
                LastUpdated = DateTime.UtcNow
            };

            return index;
        }

        /// <summary>
        /// MLB Aggregate NIL Index - Top 20 players.
        /// </summary>
        public static NILIndex GetMLBIndex()
        {
            var index = new NILIndex
            {
                IndexCode = "BLEI-MLB",
                IndexName = "BLEI-E MLB NIL Index",
                League = "MLB",
                Country = "USA",
                InceptionDate = new DateTime(2025, 7, 1),
                CurrentLevel = 1001.50,
                PreviousClose = 995.75,
                DayChange = 5.75,
                DayChangePercent = 0.58,
                YearToDateReturn = 0.15,
                ComponentCount = 20,
                TotalMarketCap = 2_250_000_000,
                AnnualizedVolatility = 0.20,
                Beta = 0.85,
                Correlation = 0.45,
                MaxDrawdown = -0.10,
                TrailingYield = 0.015,
                LastRebalanceDate = new DateTime(2026, 7, 1),
                NextRebalanceDate = new DateTime(2026, 8, 1),
                RebalanceTurnover = 0.10,
                SectorWeights = new Dictionary<string, double>
                {
                    { "SPONSOR_EQ", 0.50 },
                    { "MEDIA", 0.30 },
                    { "BETTING", 0.10 },
                    { "CONSUMER", 0.10 }
                },
                LastUpdated = DateTime.UtcNow
            };

            return index;
        }

        /// <summary>
        /// MLS Aggregate NIL Index - Top 15 players.
        /// </summary>
        public static NILIndex GetMLSIndex()
        {
            var index = new NILIndex
            {
                IndexCode = "BLEI-MLS",
                IndexName = "BLEI-E MLS NIL Index",
                League = "MLS",
                Country = "USA",
                InceptionDate = new DateTime(2025, 9, 1),
                CurrentLevel = 987.25,
                PreviousClose = 985.50,
                DayChange = 1.75,
                DayChangePercent = 0.18,
                YearToDateReturn = -1.27,
                ComponentCount = 15,
                TotalMarketCap = 1_125_000_000,
                AnnualizedVolatility = 0.22,
                Beta = 0.72,
                Correlation = 0.40,
                MaxDrawdown = -0.12,
                TrailingYield = 0.012,
                LastRebalanceDate = new DateTime(2026, 7, 1),
                NextRebalanceDate = new DateTime(2026, 8, 1),
                RebalanceTurnover = 0.08,
                SectorWeights = new Dictionary<string, double>
                {
                    { "SPONSOR_EQ", 0.48 },
                    { "MEDIA", 0.32 },
                    { "BETTING", 0.08 },
                    { "CONSUMER", 0.12 }
                },
                LastUpdated = DateTime.UtcNow
            };

            return index;
        }

        /// <summary>
        /// Team-specific index: Brooklyn Nets NIL Index.
        /// </summary>
        public static NILIndex GetBrooklynNetsIndex()
        {
            var index = new NILIndex
            {
                IndexCode = "BLEI-BKN",
                IndexName = "BLEI-E Brooklyn Nets NIL Index",
                League = "NBA",
                Country = "USA",
                InceptionDate = new DateTime(2025, 10, 1),
                CurrentLevel = 1150.75,
                PreviousClose = 1125.50,
                DayChange = 25.25,
                DayChangePercent = 2.24,
                YearToDateReturn = 15.07,
                ComponentCount = 12,
                TotalMarketCap = 950_000_000,
                AnnualizedVolatility = 0.35,
                Beta = 1.20,
                Correlation = 0.75,
                MaxDrawdown = -0.20,
                TrailingYield = 0.028,
                SectorWeights = new Dictionary<string, double>
                {
                    { "SPONSOR_EQ", 0.50 },
                    { "MEDIA", 0.30 },
                    { "BETTING", 0.12 },
                    { "CONSUMER", 0.08 }
                },
                LastUpdated = DateTime.UtcNow
            };

            return index;
        }

        /// <summary>
        /// Team-specific index: Los Angeles Lakers NIL Index.
        /// </summary>
        public static NILIndex GetLosAngelesLakersIndex()
        {
            var index = new NILIndex
            {
                IndexCode = "BLEI-LAL",
                IndexName = "BLEI-E Los Angeles Lakers NIL Index",
                League = "NBA",
                Country = "USA",
                InceptionDate = new DateTime(2025, 10, 1),
                CurrentLevel = 1275.50,
                PreviousClose = 1198.75,
                DayChange = 76.75,
                DayChangePercent = 6.40,
                YearToDateReturn = 27.55,
                ComponentCount = 15,
                TotalMarketCap = 1_850_000_000,
                AnnualizedVolatility = 0.31,
                Beta = 1.18,
                Correlation = 0.82,
                MaxDrawdown = -0.16,
                TrailingYield = 0.035,
                SectorWeights = new Dictionary<string, double>
                {
                    { "SPONSOR_EQ", 0.52 },
                    { "MEDIA", 0.28 },
                    { "BETTING", 0.10 },
                    { "CONSUMER", 0.10 }
                },
                LastUpdated = DateTime.UtcNow
            };

            return index;
        }

        /// <summary>
        /// Team-specific index: Manchester United NIL Index.
        /// </summary>
        public static NILIndex GetManchesterUnitedIndex()
        {
            var index = new NILIndex
            {
                IndexCode = "BLEI-MUFC",
                IndexName = "BLEI-E Manchester United NIL Index",
                League = "EPL",
                Country = "UK",
                InceptionDate = new DateTime(2025, 10, 1),
                CurrentLevel = 1085.25,
                PreviousClose = 1050.75,
                DayChange = 34.50,
                DayChangePercent = 3.28,
                YearToDateReturn = 8.52,
                ComponentCount = 11,
                TotalMarketCap = 1_200_000_000,
                AnnualizedVolatility = 0.28,
                Beta = 0.98,
                Correlation = 0.72,
                MaxDrawdown = -0.18,
                TrailingYield = 0.022,
                SectorWeights = new Dictionary<string, double>
                {
                    { "SPONSOR_EQ", 0.48 },
                    { "MEDIA", 0.32 },
                    { "BETTING", 0.12 },
                    { "CONSUMER", 0.08 }
                },
                LastUpdated = DateTime.UtcNow
            };

            return index;
        }

        /// <summary>
        /// Get a specific index by code.
        /// </summary>
        public static NILIndex GetIndexByCode(string indexCode)
        {
            return indexCode switch
            {
                "BLEI-NBA" => GetNBAIndex(),
                "BLEI-NFL" => GetNFLIndex(),
                "BLEI-EPL" => GetEPLIndex(),
                "BLEI-MLB" => GetMLBIndex(),
                "BLEI-MLS" => GetMLSIndex(),
                "BLEI-BKN" => GetBrooklynNetsIndex(),
                "BLEI-LAL" => GetLosAngelesLakersIndex(),
                "BLEI-MUFC" => GetManchesterUnitedIndex(),
                _ => throw new ArgumentException($"Index {indexCode} not found")
            };
        }

        /// <summary>
        /// Calculate price of a NIL Swap based on index level.
        /// </summary>
        public static double CalculateSwapPrice(NILIndex index, double fixedNILValue, double notional)
        {
            // Swap value = (Current NIL Value - Fixed NIL Value) * Notional
            var spreadPercentage = (index.CurrentLevel - (index.PreviousClose + (fixedNILValue - index.PreviousClose))) / index.PreviousClose;
            return notional * spreadPercentage;
        }
    }
}
