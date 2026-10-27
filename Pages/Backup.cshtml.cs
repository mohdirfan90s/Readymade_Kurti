using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using GarmentLotManager.Services;
using System.Text;

namespace GarmentLotManager.Pages;

public class BackupModel : PageModel
{
    private readonly IDataService _dataService;

    public BackupModel(IDataService dataService)
    {
        _dataService = dataService;
    }

    public int TotalLotsCount { get; set; }
    public int TotalKarigarsCount { get; set; }

    [BindProperty]
    public IFormFile? BackupFile { get; set; }

    public void OnGet()
    {
        TotalLotsCount = _dataService.GetLots().Count;
        TotalKarigarsCount = _dataService.GetKarigars().Count;
    }

    public IActionResult OnGetDownload()
    {
        var json = _dataService.ExportDataJson();
        var bytes = Encoding.UTF8.GetBytes(json);
        var fileName = $"GarmentManager_Backup_{DateTime.Now:yyyyMMdd_HHmmss}.json";
        return File(bytes, "application/json", fileName);
    }

    public async Task<IActionResult> OnPostImportAsync()
    {
        if (BackupFile == null || BackupFile.Length == 0)
        {
            TempData["ErrorMessage"] = "कृपया एक वैध बैकअप JSON फाइल चुनें!";
            return RedirectToPage();
        }

        using var reader = new StreamReader(BackupFile.OpenReadStream());
        var json = await reader.ReadToEndAsync();

        bool success = _dataService.ImportDataJson(json);
        if (success)
        {
            TempData["SuccessMessage"] = "बैकअप सफलतापूर्वक रिस्टोर हो गया!";
        }
        else
        {
            TempData["ErrorMessage"] = "बैकअप फाइल को पढ़ने में समस्या आई। कृपया सही JSON फाइल अपलोड करें।";
        }

        return RedirectToPage();
    }
}
