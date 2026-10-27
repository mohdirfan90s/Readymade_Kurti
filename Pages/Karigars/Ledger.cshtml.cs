using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using GarmentLotManager.Models;
using GarmentLotManager.Services;

namespace GarmentLotManager.Pages.Karigars;

public class LedgerModel : PageModel
{
    private readonly IDataService _dataService;

    public LedgerModel(IDataService dataService)
    {
        _dataService = dataService;
    }

    public Karigar KarigarItem { get; set; } = new();
    public int TotalPieces { get; set; }
    public decimal TotalEarnings { get; set; }
    public List<(Lot Lot, int Pieces, decimal Amount)> History { get; set; } = new();

    public IActionResult OnGet(int id)
    {
        var karigar = _dataService.GetKarigarById(id);
        if (karigar == null)
        {
            TempData["ErrorMessage"] = "कारीगर नहीं मिला!";
            return RedirectToPage("/Karigars/Index");
        }

        KarigarItem = karigar;
        var ledger = _dataService.GetKarigarLedger(id);
        TotalPieces = ledger.TotalPieces;
        TotalEarnings = ledger.TotalEarnings;
        History = ledger.History;

        return Page();
    }
}
