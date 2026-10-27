using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using GarmentLotManager.Common;
using GarmentLotManager.Models;
using GarmentLotManager.Services;

namespace GarmentLotManager.Pages;

public class IndexModel : PageModel
{
    private readonly IDataService _dataService;

    public IndexModel(IDataService dataService)
    {
        _dataService = dataService;
    }

    [BindProperty(SupportsGet = true)]
    public string? FromDateStr { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? ToDateStr { get; set; }

    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }

    public DashboardSummary Summary { get; set; } = new();
    public List<Lot> RecentLots { get; set; } = new();
    public bool HasDateFilter => FromDate.HasValue || ToDate.HasValue;

    public void OnGet()
    {
        FromDate = DateHelper.ParseDate(FromDateStr);
        ToDate = DateHelper.ParseDate(ToDateStr);

        // If parsed, format back cleanly as dd/MM/yyyy
        if (FromDate.HasValue) FromDateStr = FromDate.Value.ToString("dd/MM/yyyy");
        if (ToDate.HasValue) ToDateStr = ToDate.Value.ToString("dd/MM/yyyy");

        // Fetch filtered summary and lots
        Summary = _dataService.GetDashboardSummary(FromDate, ToDate);
        var lots = _dataService.GetLots(FromDate, ToDate);
        
        // If filtered, show all matching lots; if no filter, take top 6 recent
        RecentLots = HasDateFilter ? lots : lots.Take(6).ToList();
    }
}
