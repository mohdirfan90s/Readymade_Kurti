using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using GarmentLotManager.Models;
using GarmentLotManager.Services;

namespace GarmentLotManager.Pages.Master;

public class CuttingMastersModel : PageModel
{
    private readonly IDataService _dataService;

    public CuttingMastersModel(IDataService dataService)
    {
        _dataService = dataService;
    }

    public List<MasterCuttingMaster> CuttingMasters { get; set; } = new();

    [BindProperty]
    public MasterCuttingMaster NewMaster { get; set; } = new();

    public bool IsUnlocked { get; set; }

    public IActionResult OnGet()
    {
        IsUnlocked = CheckAuth();
        if (IsUnlocked)
        {
            CuttingMasters = _dataService.GetMasterCuttingMasters();
        }
        return Page();
    }

    public IActionResult OnPostUnlock(string passcode)
    {
        if (passcode == "123abc")
        {
            Response.Cookies.Append("master_passcode", "123abc", new CookieOptions { HttpOnly = false, SameSite = SameSiteMode.Lax });
            TempData["SuccessMessage"] = "✓ માસ્ટર મેનેજમેન્ટ અનલોક હો ગયા!";
            return RedirectToPage();
        }

        TempData["ErrorMessage"] = "⚠️ ગલત પાસકોડ! (Wrong passcode)";
        return RedirectToPage();
    }

    public IActionResult OnPostSave()
    {
        if (!CheckAuth()) return RedirectToPage();

        if (!string.IsNullOrWhiteSpace(NewMaster.Name))
        {
            _dataService.SaveMasterCuttingMaster(NewMaster);
            TempData["SuccessMessage"] = "✓ કટિંગ માસ્ટર સેવ હો ગયા!";
        }
        return RedirectToPage();
    }

    public IActionResult OnPostDelete(int id)
    {
        if (!CheckAuth()) return RedirectToPage();

        _dataService.DeleteMasterCuttingMaster(id);
        TempData["SuccessMessage"] = "✓ કટિંગ માસ્ટર હટા દિયા ગયા!";
        return RedirectToPage();
    }

    private bool CheckAuth()
    {
        return Request.Cookies["master_passcode"] == "123abc";
    }
}
