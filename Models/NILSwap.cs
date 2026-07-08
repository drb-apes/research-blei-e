using System;
using System.Collections.Generic;
using System.Linq;

namespace BIMAPES.Matchday.App.Models
{
    /// <summary>
    /// NILSwap: Structured derivative product for athlete NIL value hedging.
    /// Enables portfolio managers to swap athlete performance exposure without direct equity ownership.
    /// 
    /// Structure:
    /// - Fixed Leg: Locked NIL valuation at contract inception
    /// - Floating Leg: Current biometric-derived NIL value (recalculated hourly)
    /// - Notional: USD amount at risk
    /// - Tenor: Contract duration (30d, 90d, 180d, 1yr, etc)
    /// - Settlement: Cash-settled based on CRS triggers
    /// </summary>
    public class NILSwap
    {
        public string SwapId { get; set; } = Guid.NewGuid().ToString();
        public string TradeId { get; set; }  // Bloomberg CUSIP-style ID
        public string AthleteId { get; set; }
        public string AthleteName { get; set; }
        
        // Swap Parties
        public string BuyerParty { get; set; }  // Institution paying floating
        public string SellerParty { get; set; }  // Institution paying fixed
        
        // Contract Terms
        public DateTime TradeDate { get; set; }
        public DateTime EffectiveDate { get; set; }
        public DateTime MaturityDate { get; set; }
        public int TenorDays { get; set; }
        
        // Notional & Valuation
        public double NotionalUSD { get; set; }
        public double FixedLegNILValue { get; set; }  // Locked at inception
        public double FloatingLegNILValue { get; set; }  // Current, recalculated
        public double CurrentSwapValue { get; set; }  // Mark-to-market
        
        // Swap Legs
        public List<SwapCashFlow> FixedLegCashFlows { get; set; } = new();
        public List<SwapCashFlow> FloatingLegCashFlows { get; set; } = new();
        
        // NIL Index Reference
        public string NILIndexCode { get; set; }  // e.g., "BLEI-BKN" (BLEI-E Brooklyn Nets)
        public double IndexSpreadBps { get; set; }  // Basis points above/below index
        
        // Risk Metrics
        public double DeltaExposure { get; set; }  // Sensitivity to athlete performance
        public double VegaExposure { get; set; }  // Sensitivity to NIL volatility
        public double CurrencyExposure { get; set; }  // USD denominated
        public double CRSThreshold { get; set; }  // CRS level triggering early termination
        
        // Settlement & Status
        public NILSwapStatus Status { get; set; }  // Active, Terminated, Expired, Defaulted
        public List<NILSwapSettlement> Settlements { get; set; } = new();
        public DateTime? EarlyTerminationDate { get; set; }
        public string TerminationReason { get; set; }
        
        // Regulatory & Compliance
        public string CounterpartyId { get; set; }
        public string ClearingHouse { get; set; }  // ISDA agreement reference
        public double InitialMargin { get; set; }
        public double VariationMargin { get; set; }
        public DateTime LastMarginCall { get; set; }
        
        // Audit Trail
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string CreatedBy { get; set; }
        public DateTime? LastModified { get; set; }
        public string ModifiedBy { get; set; }
    }
    
    /// <summary>
    /// NILSwapStatus: Lifecycle stages of a NIL Swap contract.
    /// </summary>
    public enum NILSwapStatus
    {
        Pending = 0,      // Awaiting execution
        Active = 1,       // Live and mark-to-market
        Terminated = 2,   // Early termination via CRS trigger
        Expired = 3,      // Natural maturity reached
        Defaulted = 4,    // Counterparty default
        Settled = 5       // Final settlement completed
    }
    
    /// <summary>
    /// SwapCashFlow: Individual cash flow leg of a NIL Swap.
    /// </summary>
    public class SwapCashFlow
    {
        public string CashFlowId { get; set; } = Guid.NewGuid().ToString();
        public DateTime PaymentDate { get; set; }
        public double Amount { get; set; }
        public string Currency { get; set; } = "USD";
        public string PayerParty { get; set; }
        public string ReceiverParty { get; set; }
        public bool IsPaid { get; set; }
        public DateTime? ActualPaymentDate { get; set; }
        public double AccrualFactor { get; set; }  // Day count convention (ACT/360)
    }
    
    /// <summary>
    /// NILSwapSettlement: Settlement event for NIL Swap (coupon, termination, margin call).
    /// </summary>
    public class NILSwapSettlement
    {
        public string SettlementId { get; set; } = Guid.NewGuid().ToString();
        public DateTime SettlementDate { get; set; }
        public NILSwapSettlementType SettlementType { get; set; }
        public double FixedAmount { get; set; }
        public double FloatingAmount { get; set; }
        public double NetAmount { get; set; }
        public string SettledBy { get; set; }  // Party that initiated settlement
        public string ClearingRef { get; set; }  // Clearing house reference
        public bool IsSettled { get; set; }
    }
    
    /// <summary>
    /// NILSwapSettlementType: Categories of settlement events.
    /// </summary>
    public enum NILSwapSettlementType
    {
        Coupon = 0,          // Regular fixed/floating coupon payment
        CRSTrigger = 1,      // Early termination due to CRS breach
        Injury = 2,          // Injury event settlement
        Substitution = 3,    // Player substitution (reduced exposure)
        MarginCall = 4,      // Variation margin call
        Final = 5            // Maturity settlement
    }
}
