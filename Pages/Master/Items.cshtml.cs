using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using GarmentLotManager.Models;
using GarmentLotManager.Services;

namespace GarmentLotManager.Pages.Master;

public class ItemsModel : PageModel
{
    private readonly IDataService _dataService;

    public ItemsModel(IDataService dataService)
    {
        _dataService = dataService;
    }

    public List<MasterItem> Items { get; set; } = new();

    [BindProperty]
    public MasterItem NewItem { get; set; } = new();

    public bool IsUnlocked { get; set; }

    public IActionResult OnGet()
    {
        IsUnlocked = CheckAuth();
        if (IsUnlocked)
        {
            Items = _dataService.GetMasterItems();
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

        if (!string.IsNullOrWhiteSpace(NewItem.Name))
        {
            _dataService.SaveMasterItem(NewItem);
            TempData["SuccessMessage"] = "✓ આઇટમ સેવ હો ગયા!";
        }
        return RedirectToPage();
    }

    public IActionResult OnPostDelete(int id)
    {
        if (!CheckAuth()) return RedirectToPage();

        _dataService.DeleteMasterItem(id);
        TempData["SuccessMessage"] = "✓ આઇટમ હટા દિયા ગયા!";
        return RedirectToPage();
    }

    private bool CheckAuth()
    {
        return Request.Cookies["master_passcode"] == "123abc";
    }
}
