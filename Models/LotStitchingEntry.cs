using System.ComponentModel.DataAnnotations;

namespace GarmentLotManager.Models;

public class LotStitchingEntry
{
    public int KarigarId { get; set; }
    public string KarigarName { get; set; } = string.Empty;

    [Range(0, 100000, ErrorMessage = "पीस की संख्या सही डालें")]
    [Display(Name = "सीए गए पीस")]
    public int PiecesCount { get; set; } = 0;

    [Range(0, 10000, ErrorMessage = "सिलाई रेट सही डालें")]
    [Display(Name = "सिलाई रेट (प्रति पीस ₹)")]
    public decimal RatePerPiece { get; set; } = 30.00m;

    [Display(Name = "कुल सिलाई मजदूरी (₹)")]
    public decimal TotalAmount => PiecesCount * RatePerPiece;

    public string? Notes { get; set; }
}
