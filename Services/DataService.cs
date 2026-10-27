using System.Text.Json;
using GarmentLotManager.Models;

namespace GarmentLotManager.Services;

public class GarmentDataStore
{
    public List<Karigar> Karigars { get; set; } = new();
    public List<Lot> Lots { get; set; } = new();
    public List<MasterItem> MasterItems { get; set; } = new();
    public List<MasterMerchant> MasterMerchants { get; set; } = new();
    public List<MasterCuttingMaster> MasterCuttingMasters { get; set; } = new();
    public List<MasterInterlock> MasterInterlocks { get; set; } = new();
}

public class DataService : IDataService
{
    private readonly string _filePath;
    private readonly object _lock = new();
    private GarmentDataStore _dataStore = new();

    public DataService(IWebHostEnvironment env)
    {
        var appDataDir = Path.Combine(env.ContentRootPath, "App_Data");
        if (!Directory.Exists(appDataDir))
        {
            Directory.CreateDirectory(appDataDir);
        }

        _filePath = Path.Combine(appDataDir, "garment_data.json");
        LoadData();
    }

    private void LoadData()
    {
        lock (_lock)
        {
            if (File.Exists(_filePath))
            {
                try
                {
                    var json = File.ReadAllText(_filePath);
                    var store = JsonSerializer.Deserialize<GarmentDataStore>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (store != null)
                    {
                        _dataStore = store;
                        EnsureDefaults();
                        return;
                    }
                }
                catch
                {
                    // Fall back to seed
                }
            }

            SeedInitialData();
            SaveDataToFile();
        }
    }

    private void EnsureDefaults()
    {
        if (_dataStore.MasterItems == null || !_dataStore.MasterItems.Any())
        {
            _dataStore.MasterItems = new List<MasterItem>
            {
                new MasterItem { Id = 1, Name = "Shirt" },
                new MasterItem { Id = 2, Name = "Plazo" },
                new MasterItem { Id = 3, Name = "Pant" },
                new MasterItem { Id = 4, Name = "Both (Shirt + Pant)" },
                new MasterItem { Id = 5, Name = "Kurti" },
                new MasterItem { Id = 6, Name = "T-Shirt" }
            };
        }

        if (_dataStore.MasterMerchants == null || !_dataStore.MasterMerchants.Any())
        {
            _dataStore.MasterMerchants = new List<MasterMerchant>
            {
                new MasterMerchant { Id = 1, Name = "Gupta Garments", Phone = "9820012345" },
                new MasterMerchant { Id = 2, Name = "R.K. Brothers", Phone = "9820054321" },
                new MasterMerchant { Id = 3, Name = "Balaji Textiles", Phone = "9820098765" },
                new MasterMerchant { Id = 4, Name = "Sanjay Trading", Phone = "9820011223" },
                new MasterMerchant { Id = 5, Name = "Aman Creations", Phone = "9820044556" }
            };
        }

        if (_dataStore.MasterCuttingMasters == null || !_dataStore.MasterCuttingMasters.Any())
        {
            _dataStore.MasterCuttingMasters = new List<MasterCuttingMaster>
            {
                new MasterCuttingMaster { Id = 1, Name = "Master Aslam", DefaultRate = 2.00m },
                new MasterCuttingMaster { Id = 2, Name = "Master Akram", DefaultRate = 2.00m },
                new MasterCuttingMaster { Id = 3, Name = "Rasheed Cutting", DefaultRate = 2.00m },
                new MasterCuttingMaster { Id = 4, Name = "Javed Master", DefaultRate = 2.00m }
            };
        }

        if (_dataStore.MasterInterlocks == null || !_dataStore.MasterInterlocks.Any())
        {
            _dataStore.MasterInterlocks = new List<MasterInterlock>
            {
                new MasterInterlock { Id = 1, Name = "Babu Interlock", DefaultRate = 2.00m },
                new MasterInterlock { Id = 2, Name = "Raju Interlock", DefaultRate = 2.00m },
                new MasterInterlock { Id = 3, Name = "Star Interlock", DefaultRate = 2.00m },
                new MasterInterlock { Id = 4, Name = "Alam Interlock", DefaultRate = 2.00m }
            };
        }
    }

    private void SeedInitialData()
    {
        _dataStore = new GarmentDataStore();
        EnsureDefaults();

        // 6 Karigars in English
        var k1 = new Karigar { Id = 1, Name = "Ramesh Kumar", Age = 28, Phone = "9876543210", DefaultRate = 30.00m, Notes = "Master Tailor" };
        var k2 = new Karigar { Id = 2, Name = "Salim Ansari", Age = 34, Phone = "9876543211", DefaultRate = 30.00m, Notes = "Stitching Specialist" };
        var k3 = new Karigar { Id = 3, Name = "Abdul Kalam", Age = 40, Phone = "9876543212", DefaultRate = 30.00m, Notes = "Finishing & Stitching" };
        var k4 = new Karigar { Id = 4, Name = "Dinesh Verma", Age = 25, Phone = "9876543213", DefaultRate = 30.00m, Notes = "Speed Worker" };
        var k5 = new Karigar { Id = 5, Name = "Raees Ahmed", Age = 31, Phone = "9876543214", DefaultRate = 30.00m, Notes = "Quality Stitching" };
        var k6 = new Karigar { Id = 6, Name = "Imran Sheikh", Age = 29, Phone = "9876543215", DefaultRate = 30.00m, Notes = "All-Rounder Worker" };

        _dataStore.Karigars.AddRange(new[] { k1, k2, k3, k4, k5, k6 });

        // Sample Lot with Start & End Date
        var sampleLot = new Lot
        {
            Id = 1,
            LotNumber = "Lot #1",
            StartDate = DateTime.Today.AddDays(-2),
            EndDate = DateTime.Today,
            MerchantName = "Gupta Garments",
            ItemName = "Shirt",
            Color = "Navy Blue",
            TotalPieces = 450,
            MerchantRatePerPiece = 50.00m,
            CuttingMasterName = "Master Aslam",
            CuttingRatePerPiece = 2.00m,
            InterlockMasterName = "Babu Interlock",
            InterlockRatePerPiece = 2.00m,
            OtherExpense = 0.00m,
            Status = "Completed",
            Notes = "450 Pieces, Rate 50, Cutting 2, Stitching 30, Interlock 2",
            StitchingEntries = new List<LotStitchingEntry>
            {
                new LotStitchingEntry { KarigarId = 1, KarigarName = "Ramesh Kumar", PiecesCount = 100, RatePerPiece = 30.00m },
                new LotStitchingEntry { KarigarId = 2, KarigarName = "Salim Ansari", PiecesCount = 80, RatePerPiece = 30.00m },
                new LotStitchingEntry { KarigarId = 3, KarigarName = "Abdul Kalam", PiecesCount = 90, RatePerPiece = 30.00m },
                new LotStitchingEntry { KarigarId = 4, KarigarName = "Dinesh Verma", PiecesCount = 60, RatePerPiece = 30.00m },
                new LotStitchingEntry { KarigarId = 5, KarigarName = "Raees Ahmed", PiecesCount = 70, RatePerPiece = 30.00m },
                new LotStitchingEntry { KarigarId = 6, KarigarName = "Imran Sheikh", PiecesCount = 50, RatePerPiece = 30.00m }
            }
        };

        _dataStore.Lots.Add(sampleLot);
    }

    private void SaveDataToFile()
    {
        lock (_lock)
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(_dataStore, options);
            File.WriteAllText(_filePath, json);
        }
    }

    // Karigar methods
    public List<Karigar> GetKarigars()
    {
        lock (_lock)
        {
            return _dataStore.Karigars.OrderBy(k => k.Name).ToList();
        }
    }

    public Karigar? GetKarigarById(int id)
    {
        lock (_lock)
        {
            return _dataStore.Karigars.FirstOrDefault(k => k.Id == id);
        }
    }

    public void SaveKarigar(Karigar karigar)
    {
        lock (_lock)
        {
            if (karigar.Id == 0)
            {
                karigar.Id = _dataStore.Karigars.Any() ? _dataStore.Karigars.Max(k => k.Id) + 1 : 1;
                karigar.CreatedAt = DateTime.Now;
                _dataStore.Karigars.Add(karigar);
            }
            else
            {
                var existing = _dataStore.Karigars.FirstOrDefault(k => k.Id == karigar.Id);
                if (existing != null)
                {
                    existing.Name = karigar.Name;
                    existing.Age = karigar.Age;
                    existing.Phone = karigar.Phone;
                    existing.DefaultRate = karigar.DefaultRate;
                    existing.Notes = karigar.Notes;
                }
            }
            SaveDataToFile();
        }
    }

    public void DeleteKarigar(int id)
    {
        lock (_lock)
        {
            var item = _dataStore.Karigars.FirstOrDefault(k => k.Id == id);
            if (item != null)
            {
                _dataStore.Karigars.Remove(item);
                SaveDataToFile();
            }
        }
    }

    // Lot methods
    public List<Lot> GetLots(DateTime? fromDate = null, DateTime? toDate = null, bool includeDiscarded = false)
    {
        lock (_lock)
        {
            var query = _dataStore.Lots.AsEnumerable();
            if (!includeDiscarded)
            {
                query = query.Where(l => l.Status != "Discarded");
            }
            if (fromDate.HasValue)
            {
                query = query.Where(l => l.StartDate.HasValue && l.StartDate.Value.Date >= fromDate.Value.Date);
            }
            if (toDate.HasValue)
            {
                query = query.Where(l => l.StartDate.HasValue && l.StartDate.Value.Date <= toDate.Value.Date);
            }
            return query.OrderByDescending(l => l.StartDate).ThenByDescending(l => l.Id).ToList();
        }
    }

    public Lot? GetLotById(int id)
    {
        lock (_lock)
        {
            return _dataStore.Lots.FirstOrDefault(l => l.Id == id);
        }
    }

    public void SaveLot(Lot lot)
    {
        lock (_lock)
        {
            if (lot.Id == 0)
            {
                lot.Id = _dataStore.Lots.Any() ? _dataStore.Lots.Max(l => l.Id) + 1 : 1;
                if (string.IsNullOrWhiteSpace(lot.LotNumber))
                {
                    lot.LotNumber = $"Lot #{lot.Id}";
                }
                _dataStore.Lots.Add(lot);
            }
            else
            {
                var existing = _dataStore.Lots.FirstOrDefault(l => l.Id == lot.Id);
                if (existing != null)
                {
                    if (!string.IsNullOrWhiteSpace(lot.LotNumber)) existing.LotNumber = lot.LotNumber;
                    existing.StartDate = lot.StartDate;
                    existing.EndDate = lot.EndDate;
                    existing.MerchantName = lot.MerchantName;
                    existing.ItemName = lot.ItemName;
                    existing.Color = lot.Color;
                    if (!string.IsNullOrEmpty(lot.PhotoUrl))
                    {
                        existing.PhotoUrl = lot.PhotoUrl;
                    }
                    existing.TotalPieces = lot.TotalPieces;
                    existing.MerchantRatePerPiece = lot.MerchantRatePerPiece;
                    existing.CuttingMasterName = lot.CuttingMasterName;
                    existing.CuttingRatePerPiece = lot.CuttingRatePerPiece;
                    existing.InterlockMasterName = lot.InterlockMasterName;
                    existing.InterlockRatePerPiece = lot.InterlockRatePerPiece;
                    existing.OtherExpense = lot.OtherExpense;
                    existing.OtherExpenseNote = lot.OtherExpenseNote;
                    existing.Status = lot.Status;
                    existing.Notes = lot.Notes;
                    if (lot.StitchingEntries != null && lot.StitchingEntries.Any())
                    {
                        existing.StitchingEntries = lot.StitchingEntries;
                    }
                }
            }

            // Ensure Merchant is in Master list
            if (!string.IsNullOrWhiteSpace(lot.MerchantName) && !_dataStore.MasterMerchants.Any(m => m.Name.Equals(lot.MerchantName, StringComparison.OrdinalIgnoreCase)))
            {
                var newId = _dataStore.MasterMerchants.Any() ? _dataStore.MasterMerchants.Max(m => m.Id) + 1 : 1;
                _dataStore.MasterMerchants.Add(new MasterMerchant { Id = newId, Name = lot.MerchantName });
            }

            SaveDataToFile();
        }
    }

    public void DiscardLot(int id)
    {
        lock (_lock)
        {
            var lot = _dataStore.Lots.FirstOrDefault(l => l.Id == id);
            if (lot != null)
            {
                lot.Status = "Discarded";
                SaveDataToFile();
            }
        }
    }

    public void RestoreLot(int id)
    {
        lock (_lock)
        {
            var lot = _dataStore.Lots.FirstOrDefault(l => l.Id == id);
            if (lot != null)
            {
                lot.Status = "In Progress";
                SaveDataToFile();
            }
        }
    }

    public void MarkLotReady(int id, DateTime endDate)
    {
        lock (_lock)
        {
            var lot = _dataStore.Lots.FirstOrDefault(l => l.Id == id);
            if (lot != null)
            {
                lot.Status = "Ready";
                lot.EndDate = endDate;
                SaveDataToFile();
            }
        }
    }

    public void DeleteLot(int id)
    {
        DiscardLot(id);
    }

    // Master Items
    public List<MasterItem> GetMasterItems()
    {
        lock (_lock)
        {
            EnsureDefaults();
            return _dataStore.MasterItems.OrderBy(i => i.Name).ToList();
        }
    }

    public MasterItem? GetMasterItemById(int id)
    {
        lock (_lock)
        {
            return _dataStore.MasterItems.FirstOrDefault(i => i.Id == id);
        }
    }

    public void SaveMasterItem(MasterItem item)
    {
        lock (_lock)
        {
            if (item.Id == 0)
            {
                item.Id = _dataStore.MasterItems.Any() ? _dataStore.MasterItems.Max(i => i.Id) + 1 : 1;
                _dataStore.MasterItems.Add(item);
            }
            else
            {
                var existing = _dataStore.MasterItems.FirstOrDefault(i => i.Id == item.Id);
                if (existing != null) existing.Name = item.Name;
            }
            SaveDataToFile();
        }
    }

    public void DeleteMasterItem(int id)
    {
        lock (_lock)
        {
            var item = _dataStore.MasterItems.FirstOrDefault(i => i.Id == id);
            if (item != null)
            {
                _dataStore.MasterItems.Remove(item);
                SaveDataToFile();
            }
        }
    }

    // Master Merchants
    public List<MasterMerchant> GetMasterMerchants()
    {
        lock (_lock)
        {
            EnsureDefaults();
            return _dataStore.MasterMerchants.OrderBy(m => m.Name).ToList();
        }
    }

    public MasterMerchant? GetMasterMerchantById(int id)
    {
        lock (_lock)
        {
            return _dataStore.MasterMerchants.FirstOrDefault(m => m.Id == id);
        }
    }

    public void SaveMasterMerchant(MasterMerchant merchant)
    {
        lock (_lock)
        {
            if (merchant.Id == 0)
            {
                merchant.Id = _dataStore.MasterMerchants.Any() ? _dataStore.MasterMerchants.Max(m => m.Id) + 1 : 1;
                _dataStore.MasterMerchants.Add(merchant);
            }
            else
            {
                var existing = _dataStore.MasterMerchants.FirstOrDefault(m => m.Id == merchant.Id);
                if (existing != null)
                {
                    existing.Name = merchant.Name;
                    existing.Phone = merchant.Phone;
                    existing.Notes = merchant.Notes;
                }
            }
            SaveDataToFile();
        }
    }

    public void DeleteMasterMerchant(int id)
    {
        lock (_lock)
        {
            var item = _dataStore.MasterMerchants.FirstOrDefault(m => m.Id == id);
            if (item != null)
            {
                _dataStore.MasterMerchants.Remove(item);
                SaveDataToFile();
            }
        }
    }

    // Master Cutting Masters
    public List<MasterCuttingMaster> GetMasterCuttingMasters()
    {
        lock (_lock)
        {
            EnsureDefaults();
            return _dataStore.MasterCuttingMasters.OrderBy(c => c.Name).ToList();
        }
    }

    public MasterCuttingMaster? GetMasterCuttingMasterById(int id)
    {
        lock (_lock)
        {
            return _dataStore.MasterCuttingMasters.FirstOrDefault(c => c.Id == id);
        }
    }

    public void SaveMasterCuttingMaster(MasterCuttingMaster cuttingMaster)
    {
        lock (_lock)
        {
            if (cuttingMaster.Id == 0)
            {
                cuttingMaster.Id = _dataStore.MasterCuttingMasters.Any() ? _dataStore.MasterCuttingMasters.Max(c => c.Id) + 1 : 1;
                _dataStore.MasterCuttingMasters.Add(cuttingMaster);
            }
            else
            {
                var existing = _dataStore.MasterCuttingMasters.FirstOrDefault(c => c.Id == cuttingMaster.Id);
                if (existing != null)
                {
                    existing.Name = cuttingMaster.Name;
                    existing.DefaultRate = cuttingMaster.DefaultRate;
                    existing.Phone = cuttingMaster.Phone;
                }
            }
            SaveDataToFile();
        }
    }

    public void DeleteMasterCuttingMaster(int id)
    {
        lock (_lock)
        {
            var item = _dataStore.MasterCuttingMasters.FirstOrDefault(c => c.Id == id);
            if (item != null)
            {
                _dataStore.MasterCuttingMasters.Remove(item);
                SaveDataToFile();
            }
        }
    }

    // Master Interlock
    public List<MasterInterlock> GetMasterInterlocks()
    {
        lock (_lock)
        {
            EnsureDefaults();
            return _dataStore.MasterInterlocks.OrderBy(i => i.Name).ToList();
        }
    }

    public MasterInterlock? GetMasterInterlockById(int id)
    {
        lock (_lock)
        {
            return _dataStore.MasterInterlocks.FirstOrDefault(i => i.Id == id);
        }
    }

    public void SaveMasterInterlock(MasterInterlock interlock)
    {
        lock (_lock)
        {
            if (interlock.Id == 0)
            {
                interlock.Id = _dataStore.MasterInterlocks.Any() ? _dataStore.MasterInterlocks.Max(i => i.Id) + 1 : 1;
                _dataStore.MasterInterlocks.Add(interlock);
            }
            else
            {
                var existing = _dataStore.MasterInterlocks.FirstOrDefault(i => i.Id == interlock.Id);
                if (existing != null)
                {
                    existing.Name = interlock.Name;
                    existing.DefaultRate = interlock.DefaultRate;
                    existing.Phone = interlock.Phone;
                }
            }
            SaveDataToFile();
        }
    }

    public void DeleteMasterInterlock(int id)
    {
        lock (_lock)
        {
            var item = _dataStore.MasterInterlocks.FirstOrDefault(i => i.Id == id);
            if (item != null)
            {
                _dataStore.MasterInterlocks.Remove(item);
                SaveDataToFile();
            }
        }
    }

    // Simple string list helpers
    public List<string> GetMerchantNames()
    {
        lock (_lock)
        {
            EnsureDefaults();
            return _dataStore.MasterMerchants.Select(m => m.Name).Distinct().OrderBy(m => m).ToList();
        }
    }

    public List<string> GetItemNamesList()
    {
        lock (_lock)
        {
            EnsureDefaults();
            return _dataStore.MasterItems.Select(i => i.Name).Distinct().ToList();
        }
    }

    public List<string> GetCuttingMasterNames()
    {
        lock (_lock)
        {
            EnsureDefaults();
            return _dataStore.MasterCuttingMasters.Select(c => c.Name).Distinct().OrderBy(c => c).ToList();
        }
    }

    public List<string> GetInterlockNames()
    {
        lock (_lock)
        {
            EnsureDefaults();
            return _dataStore.MasterInterlocks.Select(i => i.Name).Distinct().OrderBy(i => i).ToList();
        }
    }

    // Step updates
    public void UpdateLotCutting(int lotId, string masterName, decimal ratePerPiece)
    {
        lock (_lock)
        {
            var lot = _dataStore.Lots.FirstOrDefault(l => l.Id == lotId);
            if (lot != null)
            {
                lot.CuttingMasterName = masterName;
                lot.CuttingRatePerPiece = ratePerPiece;
                SaveDataToFile();
            }
        }
    }

    public void UpdateLotInterlock(int lotId, string masterName, decimal ratePerPiece)
    {
        lock (_lock)
        {
            var lot = _dataStore.Lots.FirstOrDefault(l => l.Id == lotId);
            if (lot != null)
            {
                lot.InterlockMasterName = masterName;
                lot.InterlockRatePerPiece = ratePerPiece;
                SaveDataToFile();
            }
        }
    }

    public void UpdateLotStitching(int lotId, List<LotStitchingEntry> entries)
    {
        lock (_lock)
        {
            var lot = _dataStore.Lots.FirstOrDefault(l => l.Id == lotId);
            if (lot != null)
            {
                lot.StitchingEntries = entries;
                SaveDataToFile();
            }
        }
    }

    // Analytics & Reports
    public DashboardSummary GetDashboardSummary(DateTime? fromDate = null, DateTime? toDate = null)
    {
        lock (_lock)
        {
            var lots = GetLots(fromDate, toDate);
            return new DashboardSummary
            {
                TotalLots = lots.Count,
                ActiveLots = lots.Count(l => l.Status.Contains("In Progress") || l.Status.Contains("चल रहा")),
                CompletedLots = lots.Count(l => l.Status.Contains("Completed") || l.Status.Contains("पूरा")),
                TotalPieces = lots.Sum(l => l.TotalPieces),
                TotalRevenue = lots.Sum(l => l.MerchantTotalRevenue),
                TotalExpense = lots.Sum(l => l.TotalExpense),
                TotalProfit = lots.Sum(l => l.NetProfit),
                TotalCuttingCost = lots.Sum(l => l.CuttingTotalCost),
                TotalStitchingCost = lots.Sum(l => l.StitchingTotalCost),
                TotalInterlockCost = lots.Sum(l => l.InterlockTotalCost),
                TotalKarigars = _dataStore.Karigars.Count
            };
        }
    }

    public (int TotalPieces, decimal TotalEarnings, List<(Lot Lot, int Pieces, decimal Amount)> History) GetKarigarLedger(int karigarId)
    {
        lock (_lock)
        {
            var history = new List<(Lot Lot, int Pieces, decimal Amount)>();
            int totalPieces = 0;
            decimal totalEarnings = 0;

            foreach (var lot in _dataStore.Lots)
            {
                var entry = lot.StitchingEntries.FirstOrDefault(e => e.KarigarId == karigarId);
                if (entry != null && entry.PiecesCount > 0)
                {
                    totalPieces += entry.PiecesCount;
                    totalEarnings += entry.TotalAmount;
                    history.Add((lot, entry.PiecesCount, entry.TotalAmount));
                }
            }

            return (totalPieces, totalEarnings, history);
        }
    }

    public string ExportDataJson()
    {
        lock (_lock)
        {
            return JsonSerializer.Serialize(_dataStore, new JsonSerializerOptions { WriteIndented = true });
        }
    }

    public bool ImportDataJson(string json)
    {
        lock (_lock)
        {
            try
            {
                var store = JsonSerializer.Deserialize<GarmentDataStore>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (store != null)
                {
                    _dataStore = store;
                    SaveDataToFile();
                    return true;
                }
            }
            catch
            {
                return false;
            }

            return false;
        }
    }
}
