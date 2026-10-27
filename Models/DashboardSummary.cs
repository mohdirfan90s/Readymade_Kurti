namespace GarmentLotManager.Models;

public class DashboardSummary
{
    public int TotalLots { get; set; }
    public int ActiveLots { get; set; }
    public int CompletedLots { get; set; }
    public int TotalPieces { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal TotalExpense { get; set; }
    public decimal TotalProfit { get; set; }
    public decimal TotalCuttingCost { get; set; }
    public decimal TotalStitchingCost { get; set; }
    public decimal TotalInterlockCost { get; set; }
    public int TotalKarigars { get; set; }
}
