using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using GarmentLotManager.Models;
using GarmentLotManager.Services;

namespace GarmentLotManager.Pages.Karigars;

public class ManageModel : PageModel
{
    private readonly IDataService _dataService;

    public ManageModel(IDataService dataService)
    {
        _dataService = dataService;
    }

    [BindProperty]
    public Karigar KarigarItem { get; set; } = new();

    public IActionResult OnGet(int? id)
    {
        if (id.HasValue && id.Value > 0)
        {
            var existing = _dataService.GetKarigarById(id.Value);
            if (existing == null)
            {
                TempData["ErrorMessage"] = "कारीगर नहीं मिला!";
                return RedirectToPage("/Karigars/Index");
            }
            KarigarItem = existing;
        }
        else
        {
            KarigarItem = new Karigar
            {
                DefaultRate = 30.00m
            };
        }

        return Page();
    }

    public IActionResult OnPost()
    {
        ModelState.Clear();
        if (string.IsNullOrWhiteSpace(KarigarItem.Name))
        {
            ModelState.AddModelError("KarigarItem.Name", "કારીગર કા નામ અનિવાર્ય હૈ (Karigar name is required)");
            return Page();
        }

        _dataService.SaveKarigar(KarigarItem);
        TempData["SuccessMessage"] = $"✓ કારીગર {KarigarItem.Name} સેવ હો ગયા!";
        return RedirectToPage("/Karigars/Index");
    }
}
