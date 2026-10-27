using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using GarmentLotManager.Common;
using GarmentLotManager.Models;
using GarmentLotManager.Services;

namespace GarmentLotManager.Pages.Lots;

public class IndexModel : PageModel
{
    private readonly IDataService _dataService;

    public IndexModel(IDataService dataService)
    {
        _dataService = dataService;
    }

    public List<Lot> Lots { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? FromDateStr { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? ToDateStr { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? LotNumber { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? SelectedMerchant { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? SelectedItem { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? SearchColor { get; set; }

    [BindProperty(SupportsGet = true)]
    public bool ShowDiscarded { get; set; } = false;

    public List<string> MerchantList { get; set; } = new();
    public List<string> ItemList { get; set; } = new();

    public bool HasFilter => !string.IsNullOrWhiteSpace(FromDateStr) ||
                             !string.IsNullOrWhiteSpace(ToDateStr) ||
                             !string.IsNullOrWhiteSpace(LotNumber) ||
                             !string.IsNullOrWhiteSpace(SelectedMerchant) ||
                             !string.IsNullOrWhiteSpace(SelectedItem) ||
                             !string.IsNullOrWhiteSpace(SearchColor);

    public bool IsAdvancedFilterActive => !string.IsNullOrWhiteSpace(SelectedMerchant) ||
                                          !string.IsNullOrWhiteSpace(SelectedItem) ||
                                          !string.IsNullOrWhiteSpace(SearchColor);

    public void OnGet()
    {
        MerchantList = _dataService.GetMerchantNames();
        ItemList = _dataService.GetItemNamesList();

        var all = _dataService.GetLots(includeDiscarded: true);

        if (ShowDiscarded)
        {
            all = all.Where(l => l.Status == "Discarded").ToList();
        }
        else
        {
            all = all.Where(l => l.Status != "Discarded").ToList();
        }

        // Date Range Filtering (by StartDate)
        var fromDate = DateHelper.ParseDate(FromDateStr);
        var toDate = DateHelper.ParseDate(ToDateStr);

        if (fromDate.HasValue)
        {
            all = all.Where(l => l.StartDate.HasValue && l.StartDate.Value.Date >= fromDate.Value.Date).ToList();
        }

        if (toDate.HasValue)
        {
            all = all.Where(l => l.StartDate.HasValue && l.StartDate.Value.Date <= toDate.Value.Date).ToList();
        }

        // Lot Number Search
        if (!string.IsNullOrWhiteSpace(LotNumber))
        {
            var term = LotNumber.Trim();
            all = all.Where(l => l.LotNumber.Contains(term, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        // Merchant Filter (Dropdown)
        if (!string.IsNullOrWhiteSpace(SelectedMerchant))
        {
            all = all.Where(l => l.MerchantName.Equals(SelectedMerchant.Trim(), StringComparison.OrdinalIgnoreCase)).ToList();
        }

        // Item Filter (Dropdown)
        if (!string.IsNullOrWhiteSpace(SelectedItem))
        {
            all = all.Where(l => l.ItemName.Equals(SelectedItem.Trim(), StringComparison.OrdinalIgnoreCase)).ToList();
        }

        // Color Filter (Text)
        if (!string.IsNullOrWhiteSpace(SearchColor))
        {
            var term = SearchColor.Trim();
            all = all.Where(l => !string.IsNullOrWhiteSpace(l.Color) && l.Color.Contains(term, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        Lots = all;
    }

    public IActionResult OnPostDiscard(int id)
    {
        var lot = _dataService.GetLotById(id);
        if (lot != null && lot.Status == "Ready")
        {
            if (TempData != null) TempData["ErrorMessage"] = "⚠️ તૈયાર (Ready) લોટ કો રદ્દ (Cancel) નહીં કિયા જા સકતા! (Ready lots cannot be canceled)";
            return RedirectToPage(new { ShowDiscarded });
        }

        _dataService.DiscardLot(id);
        if (TempData != null) TempData["SuccessMessage"] = "✓ લોટ રદ્દ (Cancel) કિયા ગયા!";
        return RedirectToPage(new { ShowDiscarded });
    }

    public IActionResult OnPostRestore(int id)
    {
        _dataService.RestoreLot(id);
        if (TempData != null) TempData["SuccessMessage"] = "✓ લોટ વાપસ સક્રિય હો ગયા!";
        return RedirectToPage(new { ShowDiscarded = false });
    }

    public IActionResult OnPostMarkReady(int id, string readyEndDate)
    {
        var endDate = DateHelper.ParseDate(readyEndDate) ?? DateTime.Today;
        _dataService.MarkLotReady(id, endDate);
        if (TempData != null) TempData["SuccessMessage"] = $"✓ લોટ તૈયાર (Ready) માર્ક હો ગયા! ખતમ તારીખ: {endDate:dd/MM/yyyy}";
        return RedirectToPage(new { ShowDiscarded });
    }
}
