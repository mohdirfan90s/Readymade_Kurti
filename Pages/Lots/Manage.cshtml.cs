using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using GarmentLotManager.Common;
using GarmentLotManager.Models;
using GarmentLotManager.Services;

namespace GarmentLotManager.Pages.Lots;

public class ManageModel : PageModel
{
    private readonly IDataService _dataService;
    private readonly IWebHostEnvironment _env;

    public ManageModel(IDataService dataService, IWebHostEnvironment env)
    {
        _dataService = dataService;
        _env = env;
    }

    [BindProperty]
    public Lot LotItem { get; set; } = new();

    [BindProperty]
    public string? StartDateStr { get; set; }

    [BindProperty]
    public string? EndDateStr { get; set; }

    [BindProperty]
    public IFormFile? PhotoFile { get; set; }

    [BindProperty]
    public string? CustomMerchantName { get; set; }

    [BindProperty]
    public string? CustomItemName { get; set; }

    public List<string> MerchantList { get; set; } = new();
    public List<string> ItemList { get; set; } = new();

    public IActionResult OnGet(int? id)
    {
        MerchantList = _dataService.GetMerchantNames();
        ItemList = _dataService.GetItemNamesList();

        if (id.HasValue && id.Value > 0)
        {
            var existing = _dataService.GetLotById(id.Value);
            if (existing == null)
            {
                TempData["ErrorMessage"] = "Lot not found!";
                return RedirectToPage("/Lots/Index");
            }
            LotItem = existing;
            StartDateStr = LotItem.StartDate.ToDisplayDate();
            EndDateStr = LotItem.EndDate.ToDisplayDate();
        }
        else
        {
            var nextId = _dataService.GetLots().Any() ? _dataService.GetLots().Max(l => l.Id) + 1 : 1;
            LotItem = new Lot
            {
                LotNumber = $"Lot #{nextId}",
                StartDate = null,
                EndDate = null,
                MerchantName = string.Empty,
                ItemName = string.Empty,
                Color = string.Empty,
                TotalPieces = 0,
                MerchantRatePerPiece = 0.00m,
                CuttingMasterName = string.Empty,
                CuttingRatePerPiece = 0.00m,
                InterlockMasterName = string.Empty,
                InterlockRatePerPiece = 0.00m
            };
            StartDateStr = string.Empty;
            EndDateStr = string.Empty;
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        MerchantList = _dataService.GetMerchantNames();
        ItemList = _dataService.GetItemNamesList();

        // Clear any automatic binding errors
        ModelState.Clear();

        // Parse custom date strings in dd/mm/yyyy format
        if (!string.IsNullOrWhiteSpace(StartDateStr))
        {
            LotItem.StartDate = DateHelper.ParseDate(StartDateStr);
        }
        else
        {
            LotItem.StartDate = null;
        }

        // If editing an existing lot, preserve existing work data (stitching entries, cutting, interlock, end date, status)
        if (LotItem.Id > 0)
        {
            var existing = _dataService.GetLotById(LotItem.Id);
            if (existing != null)
            {
                LotItem.EndDate = existing.EndDate;
                LotItem.Status = existing.Status;
                if (string.IsNullOrWhiteSpace(LotItem.CuttingMasterName)) LotItem.CuttingMasterName = existing.CuttingMasterName;
                if (LotItem.CuttingRatePerPiece <= 0) LotItem.CuttingRatePerPiece = existing.CuttingRatePerPiece;
                if (string.IsNullOrWhiteSpace(LotItem.InterlockMasterName)) LotItem.InterlockMasterName = existing.InterlockMasterName;
                if (LotItem.InterlockRatePerPiece <= 0) LotItem.InterlockRatePerPiece = existing.InterlockRatePerPiece;
                if (LotItem.OtherExpense <= 0) LotItem.OtherExpense = existing.OtherExpense;
                if (string.IsNullOrWhiteSpace(LotItem.OtherExpenseNote)) LotItem.OtherExpenseNote = existing.OtherExpenseNote;
                LotItem.StitchingEntries = existing.StitchingEntries; // Preserve all Karigar stitching records!
            }
        }

        // Handle Custom Merchant
        if (LotItem.MerchantName == "NEW")
        {
            if (!string.IsNullOrWhiteSpace(CustomMerchantName))
            {
                LotItem.MerchantName = CustomMerchantName.Trim();
                _dataService.SaveMasterMerchant(new MasterMerchant { Name = LotItem.MerchantName });
            }
            else
            {
                ModelState.AddModelError("CustomMerchantName", "નયે વ્યાપારી કા નામ લીખો (Please enter new merchant name)");
            }
        }

        // Handle Custom Item
        if (LotItem.ItemName == "NEW")
        {
            if (!string.IsNullOrWhiteSpace(CustomItemName))
            {
                LotItem.ItemName = CustomItemName.Trim();
                _dataService.SaveMasterItem(new MasterItem { Name = LotItem.ItemName });
            }
            else
            {
                ModelState.AddModelError("CustomItemName", "નઈ આઇટમ કા નામ લીખો (Please enter new item name)");
            }
        }

        if (PhotoFile != null && PhotoFile.Length > 0)
        {
            var uploadsDir = Path.Combine(_env.WebRootPath, "uploads");
            if (!Directory.Exists(uploadsDir)) Directory.CreateDirectory(uploadsDir);

            var fileName = $"lot_{DateTime.Now:yyyyMMddHHmmss}_{Path.GetFileName(PhotoFile.FileName)}";
            var filePath = Path.Combine(uploadsDir, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await PhotoFile.CopyToAsync(stream);
            }

            LotItem.PhotoUrl = $"/uploads/{fileName}";
        }

        // Explicit, clear business validation
        if (!LotItem.StartDate.HasValue)
        {
            ModelState.AddModelError("StartDateStr", "શરૂઆત તારીખ જરૂરી છે (Start Date required in dd/mm/yyyy)");
        }

        if (string.IsNullOrWhiteSpace(LotItem.MerchantName))
        {
            ModelState.AddModelError("LotItem.MerchantName", "વ્યાપારી ચુનો (Merchant Name is required)");
        }

        if (string.IsNullOrWhiteSpace(LotItem.ItemName))
        {
            ModelState.AddModelError("LotItem.ItemName", "ક્યા કપડા / આઇટમ બનેગા ચુનો (Item is required)");
        }

        if (LotItem.TotalPieces <= 0)
        {
            ModelState.AddModelError("LotItem.TotalPieces", "કિતને પીસ બનેંગે દર્જ કરો (Please enter Total Pieces >= 1)");
        }

        if (LotItem.MerchantRatePerPiece < 0)
        {
            ModelState.AddModelError("LotItem.MerchantRatePerPiece", "ભાવ સહી દર્જ કરો (Please enter valid rate)");
        }

        if (string.IsNullOrWhiteSpace(LotItem.LotNumber))
        {
            var nextId = _dataService.GetLots().Any() ? _dataService.GetLots().Max(l => l.Id) + 1 : 1;
            LotItem.LotNumber = $"Lot #{nextId}";
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        try
        {
            _dataService.SaveLot(LotItem);
            TempData["SuccessMessage"] = "✓ લોટ સેવ હો ગયા!";
            return RedirectToPage("/Lots/Index");
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, $"ડેટા સેવ કરને મેં સમસ્યા: {ex.Message}");
            return Page();
        }
    }
}
