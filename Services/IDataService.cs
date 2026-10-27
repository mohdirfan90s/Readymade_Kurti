using GarmentLotManager.Models;

namespace GarmentLotManager.Services;

public interface IDataService
{
    // Karigar methods
    List<Karigar> GetKarigars();
    Karigar? GetKarigarById(int id);
    void SaveKarigar(Karigar karigar);
    void DeleteKarigar(int id);

    // Lot methods
    List<Lot> GetLots(DateTime? fromDate = null, DateTime? toDate = null, bool includeDiscarded = false);
    Lot? GetLotById(int id);
    void SaveLot(Lot lot);
    void DiscardLot(int id);
    void RestoreLot(int id);
    void MarkLotReady(int id, DateTime endDate);
    void DeleteLot(int id);

    // Item Master methods
    List<MasterItem> GetMasterItems();
    MasterItem? GetMasterItemById(int id);
    void SaveMasterItem(MasterItem item);
    void DeleteMasterItem(int id);

    // Merchant Master methods
    List<MasterMerchant> GetMasterMerchants();
    MasterMerchant? GetMasterMerchantById(int id);
    void SaveMasterMerchant(MasterMerchant merchant);
    void DeleteMasterMerchant(int id);

    // Cutting Master methods
    List<MasterCuttingMaster> GetMasterCuttingMasters();
    MasterCuttingMaster? GetMasterCuttingMasterById(int id);
    void SaveMasterCuttingMaster(MasterCuttingMaster cuttingMaster);
    void DeleteMasterCuttingMaster(int id);

    // Interlock Master methods
    List<MasterInterlock> GetMasterInterlocks();
    MasterInterlock? GetMasterInterlockById(int id);
    void SaveMasterInterlock(MasterInterlock interlock);
    void DeleteMasterInterlock(int id);

    // Simple list helpers for dropdowns
    List<string> GetMerchantNames();
    List<string> GetItemNamesList();
    List<string> GetCuttingMasterNames();
    List<string> GetInterlockNames();

    // Step updates
    void UpdateLotCutting(int lotId, string masterName, decimal ratePerPiece);
    void UpdateLotInterlock(int lotId, string masterName, decimal ratePerPiece);
    void UpdateLotStitching(int lotId, List<LotStitchingEntry> entries);

    // Analytics & Reports
    DashboardSummary GetDashboardSummary(DateTime? fromDate = null, DateTime? toDate = null);
    (int TotalPieces, decimal TotalEarnings, List<(Lot Lot, int Pieces, decimal Amount)> History) GetKarigarLedger(int karigarId);

    // Backup & Restore
    string ExportDataJson();
    bool ImportDataJson(string json);
}
