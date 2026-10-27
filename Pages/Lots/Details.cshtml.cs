using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using GarmentLotManager.Models;
using GarmentLotManager.Services;

namespace GarmentLotManager.Pages.Lots;

public class DetailsModel : PageModel
{
    private readonly IDataService _dataService;

    public DetailsModel(IDataService dataService)
    {
        _dataService = dataService;
    }

    public Lot LotItem { get; set; } = new();

    public IActionResult OnGet(int id)
    {
        var lot = _dataService.GetLotById(id);
        if (lot == null)
        {
            TempData["ErrorMessage"] = "लॉट नहीं मिला!";
            return RedirectToPage("/Lots/Index");
        }

        LotItem = lot;
        return Page();
    }
}
