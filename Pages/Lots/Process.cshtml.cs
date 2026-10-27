using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using GarmentLotManager.Models;
using GarmentLotManager.Services;

namespace GarmentLotManager.Pages.Lots;

public class ProcessModel : PageModel
{
    private readonly IDataService _dataService;

    public ProcessModel(IDataService dataService)
    {
        _dataService = dataService;
    }

    public Lot LotItem { get; set; } = new();
    public List<string> CuttingMasters { get; set; } = new();
    public List<string> InterlockMasters { get; set; } = new();
    public List<Karigar> RegisteredKarigars { get; set; } = new();

    public IActionResult OnGet(int id)
    {
        var lot = _dataService.GetLotById(id);
        if (lot == null)
        {
            TempData["ErrorMessage"] = "Lot not found!";
            return RedirectToPage("/Lots/Index");
        }

        LotItem = lot;
        CuttingMasters = _dataService.GetCuttingMasterNames();
        InterlockMasters = _dataService.GetInterlockNames();
        RegisteredKarigars = _dataService.GetKarigars();

        return Page();
    }

    public IActionResult OnPostSaveCutting(int id, string cuttingMasterName, string? customCuttingMaster, decimal cuttingRate)
    {
        var finalName = cuttingMasterName == "NEW" && !string.IsNullOrWhiteSpace(customCuttingMaster)
            ? customCuttingMaster.Trim()
            : cuttingMasterName;

        if (cuttingMasterName == "NEW" && !string.IsNullOrWhiteSpace(customCuttingMaster))
        {
            _dataService.SaveMasterCuttingMaster(new MasterCuttingMaster { Name = finalName, DefaultRate = cuttingRate });
        }

        _dataService.UpdateLotCutting(id, finalName, cuttingRate);
        TempData["SuccessMessage"] = "✓ કટિંગ માસ્ટર ઔર રેટ સેવ હો ગયા!";
        return RedirectToPage(new { id });
    }

    public IActionResult OnPostSaveInterlock(int id, string interlockMasterName, string? customInterlockMaster, decimal interlockRate)
    {
        var finalName = interlockMasterName == "NEW" && !string.IsNullOrWhiteSpace(customInterlockMaster)
            ? customInterlockMaster.Trim()
            : interlockMasterName;

        if (interlockMasterName == "NEW" && !string.IsNullOrWhiteSpace(customInterlockMaster))
        {
            _dataService.SaveMasterInterlock(new MasterInterlock { Name = finalName, DefaultRate = interlockRate });
        }

        _dataService.UpdateLotInterlock(id, finalName, interlockRate);
        TempData["SuccessMessage"] = "✓ ઇન્ટરલોક ઔર રેટ સેવ હો ગયા!";
        return RedirectToPage(new { id });
    }

    public IActionResult OnPostSaveStitching(int id, List<int> karigarIds, List<int> pieces, List<decimal> rates)
    {
        var allKarigars = _dataService.GetKarigars();
        var entries = new List<LotStitchingEntry>();

        for (int i = 0; i < karigarIds.Count; i++)
        {
            int kId = karigarIds[i];
            int pcs = i < pieces.Count ? pieces[i] : 0;
            decimal r = i < rates.Count ? rates[i] : 30.00m;

            if (pcs > 0)
            {
                var k = allKarigars.FirstOrDefault(x => x.Id == kId);
                entries.Add(new LotStitchingEntry
                {
                    KarigarId = kId,
                    KarigarName = k != null ? k.Name : $"Karigar #{kId}",
                    PiecesCount = pcs,
                    RatePerPiece = r
                });
            }
        }

        _dataService.UpdateLotStitching(id, entries);
        TempData["SuccessMessage"] = "✓ કારીગર સિલાઈ કા હિસાબ સેવ હો ગયા!";
        return RedirectToPage(new { id });
    }

    public IActionResult OnPostMarkReady(int id, string readyEndDate)
    {
        var endDate = GarmentLotManager.Common.DateHelper.ParseDate(readyEndDate) ?? DateTime.Today;
        _dataService.MarkLotReady(id, endDate);
        TempData["SuccessMessage"] = $"✓ લોટ તૈયાર (Ready) માર્ક હો ગયા! ખતમ તારીખ: {endDate:dd/MM/yyyy}";
        return RedirectToPage(new { id });
    }
}
