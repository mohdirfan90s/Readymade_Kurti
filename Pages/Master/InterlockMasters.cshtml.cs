using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using GarmentLotManager.Models;
using GarmentLotManager.Services;

namespace GarmentLotManager.Pages.Master;

public class InterlockMastersModel : PageModel
{
    private readonly IDataService _dataService;

    public InterlockMastersModel(IDataService dataService)
    {
        _dataService = dataService;
    }

    public List<MasterInterlock> InterlockMasters { get; set; } = new();

    [BindProperty]
    public MasterInterlock NewInterlock { get; set; } = new();

    public bool IsUnlocked { get; set; }

    public IActionResult OnGet()
    {
        IsUnlocked = CheckAuth();
        if (IsUnlocked)
        {
            InterlockMasters = _dataService.GetMasterInterlocks();
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

        if (!string.IsNullOrWhiteSpace(NewInterlock.Name))
        {
            _dataService.SaveMasterInterlock(NewInterlock);
            TempData["SuccessMessage"] = "✓ ઇન્ટરલોક સેન્ટર સેવ હો ગયા!";
        }
        return RedirectToPage();
    }

    public IActionResult OnPostDelete(int id)
    {
        if (!CheckAuth()) return RedirectToPage();

        _dataService.DeleteMasterInterlock(id);
        TempData["SuccessMessage"] = "✓ ઇન્ટરલોક સેન્ટર હટા દિયા ગયા!";
        return RedirectToPage();
    }

    private bool CheckAuth()
    {
        return Request.Cookies["master_passcode"] == "123abc";
    }
}
