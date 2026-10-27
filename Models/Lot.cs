using System.ComponentModel.DataAnnotations;

namespace GarmentLotManager.Models;

public class Lot
{
    public int Id { get; set; }

    [Display(Name = "Lot Number")]
    public string LotNumber { get; set; } = string.Empty;

    [Display(Name = "Start Date")]
    public DateTime? StartDate { get; set; }

    [Display(Name = "End Date")]
    public DateTime? EndDate { get; set; }

    [Display(Name = "Merchant Name")]
    public string MerchantName { get; set; } = string.Empty;

    public string? MerchantPhone { get; set; }

    [Display(Name = "Item Name")]
    public string ItemName { get; set; } = string.Empty;

    [Display(Name = "Color")]
    public string? Color { get; set; } = string.Empty;

    [Display(Name = "Photo")]
    public string? PhotoUrl { get; set; }

    [Display(Name = "Total Pieces")]
    public int TotalPieces { get; set; } = 0;

    [Display(Name = "Merchant Rate (₹/pc)")]
    public decimal MerchantRatePerPiece { get; set; } = 0.00m;

    [Display(Name = "Cutting Master")]
    public string? CuttingMasterName { get; set; } = string.Empty;

    [Range(0, 10000, ErrorMessage = "Cutting Rate must be valid")]
    [Display(Name = "Cutting Rate (₹/pc)")]
    public decimal CuttingRatePerPiece { get; set; } = 0.00m;

    [Display(Name = "Interlock Master")]
    public string? InterlockMasterName { get; set; } = string.Empty;

    [Range(0, 10000, ErrorMessage = "Interlock Rate must be valid")]
    [Display(Name = "Interlock Rate (₹/pc)")]
    public decimal InterlockRatePerPiece { get; set; } = 0.00m;

    [Range(0, 1000000, ErrorMessage = "Other expense must be valid")]
    [Display(Name = "Other Expense (₹)")]
    public decimal OtherExpense { get; set; } = 0.00m;

    [Display(Name = "Other Expense Note")]
    public string? OtherExpenseNote { get; set; }

    [Display(Name = "Status")]
    public string Status { get; set; } = "In Progress";

    [Display(Name = "Notes")]
    public string? Notes { get; set; }

    public List<LotStitchingEntry> StitchingEntries { get; set; } = new();

    // Calculated fields
    public int StitchedPiecesCount => StitchingEntries.Sum(e => e.PiecesCount);

    public int RemainingPiecesCount => TotalPieces - StitchedPiecesCount;

    // Total Revenue from Merchant (વ્યાપારી સે મિલને વાલી રકમ)
    public decimal MerchantTotalRevenue => TotalPieces * MerchantRatePerPiece;

    // Total Cutting Cost (કટિંગ માસ્ટર કા ખર્ચ)
    public decimal CuttingTotalCost => TotalPieces * CuttingRatePerPiece;

    // Total Stitching Cost (કારીગરોં કી સિલાઈ કા ખર્ચ)
    public decimal StitchingTotalCost => StitchingEntries.Sum(e => e.TotalAmount);

    // Total Interlock Cost (ઇન્ટરલોક કા ખર્ચ)
    public decimal InterlockTotalCost => TotalPieces * InterlockRatePerPiece;

    // Total Expenses (કુલ ખર્ચ = કટિંગ + સિલાઈ + ઇન્ટરલોક + અન્ય)
    public decimal TotalExpense => CuttingTotalCost + StitchingTotalCost + InterlockTotalCost + OtherExpense;

    // Net Profit (મેરા મુનાફા = વ્યાપારી સે આય - કુલ ખર્ચ)
    public decimal NetProfit => MerchantTotalRevenue - TotalExpense;

    // Profit Margin Percentage
    public decimal ProfitMarginPercentage => MerchantTotalRevenue > 0
        ? Math.Round((NetProfit / MerchantTotalRevenue) * 100, 2)
        : 0;
}
