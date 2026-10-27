namespace GarmentLotManager.Models;

public class MasterItem
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class MasterMerchant
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Notes { get; set; }
}

public class MasterCuttingMaster
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal DefaultRate { get; set; } = 2.00m;
    public string? Phone { get; set; }
}

public class MasterInterlock
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal DefaultRate { get; set; } = 2.00m;
    public string? Phone { get; set; }
}
