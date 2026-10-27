using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using GarmentLotManager.Models;
using GarmentLotManager.Services;

namespace GarmentLotManager.Pages.Karigars;

public class KarigarListItemViewModel
{
    public Karigar Karigar { get; set; } = new();
    public int TotalPieces { get; set; }
    public decimal TotalEarnings { get; set; }
    public int TotalLotsCount { get; set; }
}

public class IndexModel : PageModel
{
    private readonly IDataService _dataService;

    public IndexModel(IDataService dataService)
    {
        _dataService = dataService;
    }

    public List<KarigarListItemViewModel> KarigarList { get; set; } = new();

    public void OnGet()
    {
        var karigars = _dataService.GetKarigars();
        KarigarList = karigars.Select(k =>
        {
            var ledger = _dataService.GetKarigarLedger(k.Id);
            return new KarigarListItemViewModel
            {
                Karigar = k,
                TotalPieces = ledger.TotalPieces,
                TotalEarnings = ledger.TotalEarnings,
                TotalLotsCount = ledger.History.Count
            };
        }).OrderByDescending(k => k.TotalEarnings).ToList();
    }

    public IActionResult OnPostDelete(int id)
    {
        _dataService.DeleteKarigar(id);
        TempData["SuccessMessage"] = "कारीगर सफलतापूर्वक हटा दिया गया!";
        return RedirectToPage();
    }
}
