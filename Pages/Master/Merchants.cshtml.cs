using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using GarmentLotManager.Models;
using GarmentLotManager.Services;

namespace GarmentLotManager.Pages.Master;

public class MerchantsModel : PageModel
{
    private readonly IDataService _dataService;

    public MerchantsModel(IDataService dataService)
    {
        _dataService = dataService;
    }

    public List<MasterMerchant> Merchants { get; set; } = new();

    [BindProperty]
    public MasterMerchant NewMerchant { get; set; } = new();

    public bool IsUnlocked { get; set; }

    public IActionResult OnGet()
    {
        IsUnlocked = CheckAuth();
        if (IsUnlocked)
        {
            Merchants = _dataService.GetMasterMerchants();
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

        if (!string.IsNullOrWhiteSpace(NewMerchant.Name))
        {
            _dataService.SaveMasterMerchant(NewMerchant);
            TempData["SuccessMessage"] = "✓ વ્યાપારી સેવ હો ગયા!";
        }
        return RedirectToPage();
    }

    public IActionResult OnPostDelete(int id)
    {
        if (!CheckAuth()) return RedirectToPage();

        _dataService.DeleteMasterMerchant(id);
        TempData["SuccessMessage"] = "✓ વ્યાપારી હટા દિયા ગયા!";
        return RedirectToPage();
    }

    private bool CheckAuth()
    {
        return Request.Cookies["master_passcode"] == "123abc";
    }
}
