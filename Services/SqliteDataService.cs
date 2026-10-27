using System.Data;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using GarmentLotManager.Models;

namespace GarmentLotManager.Services;

public class SqliteDataService : IDataService
{
    private readonly string _dbPath;
    private readonly string _connectionString;
    private readonly string _backupJsonPath;
    private readonly object _lock = new();

    public SqliteDataService(IWebHostEnvironment env)
    {
        var appDataDir = Path.Combine(env.ContentRootPath, "App_Data");
        if (!Directory.Exists(appDataDir))
        {
            Directory.CreateDirectory(appDataDir);
        }

        _dbPath = Path.Combine(appDataDir, "garment.db");
        _connectionString = $"Data Source={_dbPath};";
        _backupJsonPath = Path.Combine(appDataDir, "garment_data.json");

        InitializeDatabase();
    }

    private SqliteConnection GetConnection()
    {
        var conn = new SqliteConnection(_connectionString);
        conn.Open();
        return conn;
    }

    private void InitializeDatabase()
    {
        lock (_lock)
        {
            using var conn = GetConnection();

            // Set journal mode to WAL for better concurrency and reliability
            using (var pragmaCmd = conn.CreateCommand())
            {
                pragmaCmd.CommandText = "PRAGMA journal_mode = WAL;";
                pragmaCmd.ExecuteNonQuery();
            }

            // Create Master Tables
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
                    CREATE TABLE IF NOT EXISTS master_items (
                        id INTEGER PRIMARY KEY AUTOINCREMENT,
                        name TEXT NOT NULL UNIQUE
                    );

                    CREATE TABLE IF NOT EXISTS master_merchants (
                        id INTEGER PRIMARY KEY AUTOINCREMENT,
                        name TEXT NOT NULL UNIQUE,
                        phone TEXT,
                        notes TEXT
                    );

                    CREATE TABLE IF NOT EXISTS master_cutting_masters (
                        id INTEGER PRIMARY KEY AUTOINCREMENT,
                        name TEXT NOT NULL UNIQUE,
                        default_rate REAL NOT NULL DEFAULT 2.0,
                        phone TEXT
                    );

                    CREATE TABLE IF NOT EXISTS master_interlocks (
                        id INTEGER PRIMARY KEY AUTOINCREMENT,
                        name TEXT NOT NULL UNIQUE,
                        default_rate REAL NOT NULL DEFAULT 2.0,
                        phone TEXT
                    );

                    CREATE TABLE IF NOT EXISTS karigars (
                        id INTEGER PRIMARY KEY AUTOINCREMENT,
                        name TEXT NOT NULL,
                        age INTEGER,
                        phone TEXT,
                        default_rate REAL NOT NULL DEFAULT 30.0,
                        notes TEXT,
                        created_at TEXT NOT NULL
                    );

                    CREATE TABLE IF NOT EXISTS lots (
                        id INTEGER PRIMARY KEY AUTOINCREMENT,
                        lot_number TEXT NOT NULL,
                        start_date TEXT NOT NULL,
                        end_date TEXT,
                        merchant_name TEXT NOT NULL,
                        merchant_phone TEXT,
                        item_name TEXT NOT NULL,
                        color TEXT,
                        photo_url TEXT,
                        total_pieces INTEGER NOT NULL DEFAULT 0,
                        merchant_rate REAL NOT NULL DEFAULT 0.0,
                        cutting_master_name TEXT,
                        cutting_rate REAL NOT NULL DEFAULT 0.0,
                        interlock_master_name TEXT,
                        interlock_rate REAL NOT NULL DEFAULT 0.0,
                        other_expense REAL NOT NULL DEFAULT 0.0,
                        other_expense_note TEXT,
                        status TEXT NOT NULL DEFAULT 'In Progress',
                        notes TEXT,
                        created_at TEXT NOT NULL
                    );

                    CREATE TABLE IF NOT EXISTS lot_stitching_entries (
                        id INTEGER PRIMARY KEY AUTOINCREMENT,
                        lot_id INTEGER NOT NULL,
                        karigar_id INTEGER NOT NULL,
                        karigar_name TEXT NOT NULL,
                        pieces_count INTEGER NOT NULL DEFAULT 0,
                        rate_per_piece REAL NOT NULL DEFAULT 0.0,
                        notes TEXT,
                        FOREIGN KEY (lot_id) REFERENCES lots(id) ON DELETE CASCADE
                    );
                ";
                cmd.ExecuteNonQuery();
            }

            // Check if database is empty; if so, attempt JSON migration or seed defaults
            using (var checkCmd = conn.CreateCommand())
            {
                checkCmd.CommandText = "SELECT COUNT(*) FROM master_items;";
                var count = Convert.ToInt64(checkCmd.ExecuteScalar());
                if (count == 0)
                {
                    MigrateOrSeed(conn);
                }
            }
        }
    }

    private void MigrateOrSeed(SqliteConnection conn)
    {
        if (File.Exists(_backupJsonPath))
        {
            try
            {
                var json = File.ReadAllText(_backupJsonPath);
                var store = JsonSerializer.Deserialize<GarmentDataStore>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (store != null)
                {
                    ImportStoreData(store, conn);
                    return;
                }
            }
            catch
            {
                // Fall back to seed
            }
        }

        SeedDefaults(conn);
    }

    private void SeedDefaults(SqliteConnection conn)
    {
        using var tx = conn.BeginTransaction();

        // Items
        var items = new[] { "Shirt", "Plazo", "Pant", "Both (Shirt + Pant)", "Kurti", "T-Shirt" };
        foreach (var itm in items)
        {
            using var cmd = conn.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = "INSERT OR IGNORE INTO master_items (name) VALUES (@name);";
            cmd.Parameters.AddWithValue("@name", itm);
            cmd.ExecuteNonQuery();
        }

        // Merchants
        var merchants = new[]
        {
            ("Gupta Garments", "9820012345"),
            ("R.K. Brothers", "9820054321"),
            ("Balaji Textiles", "9820098765"),
            ("Sanjay Trading", "9820011223"),
            ("Aman Creations", "9820044556")
        };
        foreach (var m in merchants)
        {
            using var cmd = conn.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = "INSERT OR IGNORE INTO master_merchants (name, phone) VALUES (@name, @phone);";
            cmd.Parameters.AddWithValue("@name", m.Item1);
            cmd.Parameters.AddWithValue("@phone", m.Item2);
            cmd.ExecuteNonQuery();
        }

        // Cutting Masters
        var cutting = new[]
        {
            ("Master Aslam", 2.0m),
            ("Master Akram", 2.0m),
            ("Rasheed Cutting", 2.0m),
            ("Javed Master", 2.0m)
        };
        foreach (var c in cutting)
        {
            using var cmd = conn.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = "INSERT OR IGNORE INTO master_cutting_masters (name, default_rate) VALUES (@name, @rate);";
            cmd.Parameters.AddWithValue("@name", c.Item1);
            cmd.Parameters.AddWithValue("@rate", (double)c.Item2);
            cmd.ExecuteNonQuery();
        }

        // Interlock Masters
        var interlocks = new[]
        {
            ("Babu Interlock", 2.0m),
            ("Raju Interlock", 2.0m),
            ("Star Interlock", 2.0m),
            ("Alam Interlock", 2.0m)
        };
        foreach (var i in interlocks)
        {
            using var cmd = conn.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = "INSERT OR IGNORE INTO master_interlocks (name, default_rate) VALUES (@name, @rate);";
            cmd.Parameters.AddWithValue("@name", i.Item1);
            cmd.Parameters.AddWithValue("@rate", (double)i.Item2);
            cmd.ExecuteNonQuery();
        }

        // Karigars (in English)
        var karigars = new[]
        {
            (1, "Ramesh Kumar", 28, "9876543210", 30.0m, "Master Tailor"),
            (2, "Salim Ansari", 34, "9876543211", 30.0m, "Stitching Specialist"),
            (3, "Abdul Kalam", 40, "9876543212", 30.0m, "Finishing & Stitching"),
            (4, "Dinesh Verma", 25, "9876543213", 30.0m, "Speed Worker"),
            (5, "Raees Ahmed", 31, "9876543214", 30.0m, "Quality Stitching"),
            (6, "Imran Sheikh", 29, "9876543215", 30.0m, "All-Rounder Worker")
        };
        foreach (var k in karigars)
        {
            using var cmd = conn.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = @"
                INSERT INTO karigars (id, name, age, phone, default_rate, notes, created_at)
                VALUES (@id, @name, @age, @phone, @rate, @notes, @created_at);
            ";
            cmd.Parameters.AddWithValue("@id", k.Item1);
            cmd.Parameters.AddWithValue("@name", k.Item2);
            cmd.Parameters.AddWithValue("@age", k.Item3);
            cmd.Parameters.AddWithValue("@phone", k.Item4);
            cmd.Parameters.AddWithValue("@rate", (double)k.Item5);
            cmd.Parameters.AddWithValue("@notes", k.Item6);
            cmd.Parameters.AddWithValue("@created_at", DateTime.Now.ToString("o"));
            cmd.ExecuteNonQuery();
        }

        // Sample Lot
        using (var cmd = conn.CreateCommand())
        {
            cmd.Transaction = tx;
            cmd.CommandText = @"
                INSERT INTO lots (id, lot_number, start_date, end_date, merchant_name, item_name, color, total_pieces,
                                  merchant_rate, cutting_master_name, cutting_rate, interlock_master_name, interlock_rate,
                                  other_expense, status, notes, created_at)
                VALUES (1, 'Lot #1', @start_date, @end_date, 'Gupta Garments', 'Shirt', 'Navy Blue', 450,
                        50.0, 'Master Aslam', 2.0, 'Babu Interlock', 2.0, 0.0, 'Completed',
                        '450 Pieces, Rate 50, Cutting 2, Stitching 30, Interlock 2', @created_at);
            ";
            cmd.Parameters.AddWithValue("@start_date", DateTime.Today.AddDays(-2).ToString("yyyy-MM-dd"));
            cmd.Parameters.AddWithValue("@end_date", DateTime.Today.ToString("yyyy-MM-dd"));
            cmd.Parameters.AddWithValue("@created_at", DateTime.Now.ToString("o"));
            cmd.ExecuteNonQuery();
        }

        // Sample Lot Stitching Entries (450 pieces distributed)
        var entries = new[]
        {
            (1, "Ramesh Kumar", 100, 30.0m),
            (2, "Salim Ansari", 80, 30.0m),
            (3, "Abdul Kalam", 90, 30.0m),
            (4, "Dinesh Verma", 60, 30.0m),
            (5, "Raees Ahmed", 70, 30.0m),
            (6, "Imran Sheikh", 50, 30.0m)
        };

        foreach (var e in entries)
        {
            using var cmd = conn.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = @"
                INSERT INTO lot_stitching_entries (lot_id, karigar_id, karigar_name, pieces_count, rate_per_piece)
                VALUES (1, @kid, @kname, @pcs, @rate);
            ";
            cmd.Parameters.AddWithValue("@kid", e.Item1);
            cmd.Parameters.AddWithValue("@kname", e.Item2);
            cmd.Parameters.AddWithValue("@pcs", e.Item3);
            cmd.Parameters.AddWithValue("@rate", (double)e.Item4);
            cmd.ExecuteNonQuery();
        }

        tx.Commit();
    }

    private void ImportStoreData(GarmentDataStore store, SqliteConnection conn)
    {
        using var tx = conn.BeginTransaction();

        if (store.MasterItems != null)
        {
            foreach (var itm in store.MasterItems)
            {
                using var cmd = conn.CreateCommand();
                cmd.Transaction = tx;
                cmd.CommandText = "INSERT OR IGNORE INTO master_items (id, name) VALUES (@id, @name);";
                cmd.Parameters.AddWithValue("@id", itm.Id);
                cmd.Parameters.AddWithValue("@name", itm.Name);
                cmd.ExecuteNonQuery();
            }
        }

        if (store.MasterMerchants != null)
        {
            foreach (var m in store.MasterMerchants)
            {
                using var cmd = conn.CreateCommand();
                cmd.Transaction = tx;
                cmd.CommandText = "INSERT OR IGNORE INTO master_merchants (id, name, phone, notes) VALUES (@id, @name, @phone, @notes);";
                cmd.Parameters.AddWithValue("@id", m.Id);
                cmd.Parameters.AddWithValue("@name", m.Name);
                cmd.Parameters.AddWithValue("@phone", (object?)m.Phone ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@notes", (object?)m.Notes ?? DBNull.Value);
                cmd.ExecuteNonQuery();
            }
        }

        if (store.MasterCuttingMasters != null)
        {
            foreach (var c in store.MasterCuttingMasters)
            {
                using var cmd = conn.CreateCommand();
                cmd.Transaction = tx;
                cmd.CommandText = "INSERT OR IGNORE INTO master_cutting_masters (id, name, default_rate, phone) VALUES (@id, @name, @rate, @phone);";
                cmd.Parameters.AddWithValue("@id", c.Id);
                cmd.Parameters.AddWithValue("@name", c.Name);
                cmd.Parameters.AddWithValue("@rate", (double)c.DefaultRate);
                cmd.Parameters.AddWithValue("@phone", (object?)c.Phone ?? DBNull.Value);
                cmd.ExecuteNonQuery();
            }
        }

        if (store.MasterInterlocks != null)
        {
            foreach (var i in store.MasterInterlocks)
            {
                using var cmd = conn.CreateCommand();
                cmd.Transaction = tx;
                cmd.CommandText = "INSERT OR IGNORE INTO master_interlocks (id, name, default_rate, phone) VALUES (@id, @name, @rate, @phone);";
                cmd.Parameters.AddWithValue("@id", i.Id);
                cmd.Parameters.AddWithValue("@name", i.Name);
                cmd.Parameters.AddWithValue("@rate", (double)i.DefaultRate);
                cmd.Parameters.AddWithValue("@phone", (object?)i.Phone ?? DBNull.Value);
                cmd.ExecuteNonQuery();
            }
        }

        if (store.Karigars != null)
        {
            foreach (var k in store.Karigars)
            {
                using var cmd = conn.CreateCommand();
                cmd.Transaction = tx;
                cmd.CommandText = @"
                    INSERT OR REPLACE INTO karigars (id, name, age, phone, default_rate, notes, created_at)
                    VALUES (@id, @name, @age, @phone, @rate, @notes, @created_at);
                ";
                cmd.Parameters.AddWithValue("@id", k.Id);
                cmd.Parameters.AddWithValue("@name", k.Name);
                cmd.Parameters.AddWithValue("@age", (object?)k.Age ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@phone", (object?)k.Phone ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@rate", (double)k.DefaultRate);
                cmd.Parameters.AddWithValue("@notes", (object?)k.Notes ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@created_at", k.CreatedAt.ToString("o"));
                cmd.ExecuteNonQuery();
            }
        }

        if (store.Lots != null)
        {
            foreach (var l in store.Lots)
            {
                using var cmd = conn.CreateCommand();
                cmd.Transaction = tx;
                cmd.CommandText = @"
                    INSERT OR REPLACE INTO lots (id, lot_number, start_date, end_date, merchant_name, merchant_phone,
                                                 item_name, color, photo_url, total_pieces, merchant_rate,
                                                 cutting_master_name, cutting_rate, interlock_master_name, interlock_rate,
                                                 other_expense, other_expense_note, status, notes, created_at)
                    VALUES (@id, @lot_num, @start_date, @end_date, @merch_name, @merch_phone,
                            @item_name, @color, @photo_url, @total_pieces, @merch_rate,
                            @cut_name, @cut_rate, @int_name, @int_rate,
                            @other_exp, @other_note, @status, @notes, @created_at);
                ";
                cmd.Parameters.AddWithValue("@id", l.Id);
                cmd.Parameters.AddWithValue("@lot_num", l.LotNumber);
                cmd.Parameters.AddWithValue("@start_date", l.StartDate.HasValue ? l.StartDate.Value.ToString("yyyy-MM-dd") : DateTime.Today.ToString("yyyy-MM-dd"));
                cmd.Parameters.AddWithValue("@end_date", l.EndDate.HasValue ? l.EndDate.Value.ToString("yyyy-MM-dd") : DBNull.Value);
                cmd.Parameters.AddWithValue("@merch_name", l.MerchantName);
                cmd.Parameters.AddWithValue("@merch_phone", (object?)l.MerchantPhone ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@item_name", l.ItemName);
                cmd.Parameters.AddWithValue("@color", (object?)l.Color ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@photo_url", (object?)l.PhotoUrl ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@total_pieces", l.TotalPieces);
                cmd.Parameters.AddWithValue("@merch_rate", (double)l.MerchantRatePerPiece);
                cmd.Parameters.AddWithValue("@cut_name", (object?)l.CuttingMasterName ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@cut_rate", (double)l.CuttingRatePerPiece);
                cmd.Parameters.AddWithValue("@int_name", (object?)l.InterlockMasterName ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@int_rate", (double)l.InterlockRatePerPiece);
                cmd.Parameters.AddWithValue("@other_exp", (double)l.OtherExpense);
                cmd.Parameters.AddWithValue("@other_note", (object?)l.OtherExpenseNote ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@status", l.Status);
                cmd.Parameters.AddWithValue("@notes", (object?)l.Notes ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@created_at", DateTime.Now.ToString("o"));
                cmd.ExecuteNonQuery();

                if (l.StitchingEntries != null)
                {
                    foreach (var se in l.StitchingEntries)
                    {
                        using var scmd = conn.CreateCommand();
                        scmd.Transaction = tx;
                        scmd.CommandText = @"
                            INSERT INTO lot_stitching_entries (lot_id, karigar_id, karigar_name, pieces_count, rate_per_piece, notes)
                            VALUES (@lot_id, @karigar_id, @karigar_name, @pieces, @rate, @notes);
                        ";
                        scmd.Parameters.AddWithValue("@lot_id", l.Id);
                        scmd.Parameters.AddWithValue("@karigar_id", se.KarigarId);
                        scmd.Parameters.AddWithValue("@karigar_name", se.KarigarName);
                        scmd.Parameters.AddWithValue("@pieces", se.PiecesCount);
                        scmd.Parameters.AddWithValue("@rate", (double)se.RatePerPiece);
                        scmd.Parameters.AddWithValue("@notes", (object?)se.Notes ?? DBNull.Value);
                        scmd.ExecuteNonQuery();
                    }
                }
            }
        }

        tx.Commit();
    }

    private void SyncBackupJson()
    {
        try
        {
            var store = new GarmentDataStore
            {
                MasterItems = GetMasterItems(),
                MasterMerchants = GetMasterMerchants(),
                MasterCuttingMasters = GetMasterCuttingMasters(),
                MasterInterlocks = GetMasterInterlocks(),
                Karigars = GetKarigars(),
                Lots = GetLots()
            };
            var json = JsonSerializer.Serialize(store, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_backupJsonPath, json);
        }
        catch
        {
            // Non-critical background sync
        }
    }

    #region Karigar Methods
    public List<Karigar> GetKarigars()
    {
        lock (_lock)
        {
            var list = new List<Karigar>();
            using var conn = GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT id, name, age, phone, default_rate, notes, created_at FROM karigars ORDER BY name ASC;";
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new Karigar
                {
                    Id = reader.GetInt32(0),
                    Name = reader.GetString(1),
                    Age = reader.IsDBNull(2) ? null : reader.GetInt32(2),
                    Phone = reader.IsDBNull(3) ? null : reader.GetString(3),
                    DefaultRate = (decimal)reader.GetDouble(4),
                    Notes = reader.IsDBNull(5) ? null : reader.GetString(5),
                    CreatedAt = DateTime.TryParse(reader.GetString(6), out var dt) ? dt : DateTime.Now
                });
            }
            return list;
        }
    }

    public Karigar? GetKarigarById(int id)
    {
        lock (_lock)
        {
            using var conn = GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT id, name, age, phone, default_rate, notes, created_at FROM karigars WHERE id = @id;";
            cmd.Parameters.AddWithValue("@id", id);
            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                return new Karigar
                {
                    Id = reader.GetInt32(0),
                    Name = reader.GetString(1),
                    Age = reader.IsDBNull(2) ? null : reader.GetInt32(2),
                    Phone = reader.IsDBNull(3) ? null : reader.GetString(3),
                    DefaultRate = (decimal)reader.GetDouble(4),
                    Notes = reader.IsDBNull(5) ? null : reader.GetString(5),
                    CreatedAt = DateTime.TryParse(reader.GetString(6), out var dt) ? dt : DateTime.Now
                };
            }
            return null;
        }
    }

    public void SaveKarigar(Karigar karigar)
    {
        lock (_lock)
        {
            using var conn = GetConnection();
            using var cmd = conn.CreateCommand();
            if (karigar.Id == 0)
            {
                cmd.CommandText = @"
                    INSERT INTO karigars (name, age, phone, default_rate, notes, created_at)
                    VALUES (@name, @age, @phone, @rate, @notes, @created_at);
                    SELECT last_insert_rowid();
                ";
                cmd.Parameters.AddWithValue("@name", karigar.Name);
                cmd.Parameters.AddWithValue("@age", (object?)karigar.Age ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@phone", (object?)karigar.Phone ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@rate", (double)karigar.DefaultRate);
                cmd.Parameters.AddWithValue("@notes", (object?)karigar.Notes ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@created_at", DateTime.Now.ToString("o"));
                karigar.Id = Convert.ToInt32(cmd.ExecuteScalar());
            }
            else
            {
                cmd.CommandText = @"
                    UPDATE karigars
                    SET name = @name, age = @age, phone = @phone, default_rate = @rate, notes = @notes
                    WHERE id = @id;
                ";
                cmd.Parameters.AddWithValue("@id", karigar.Id);
                cmd.Parameters.AddWithValue("@name", karigar.Name);
                cmd.Parameters.AddWithValue("@age", (object?)karigar.Age ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@phone", (object?)karigar.Phone ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@rate", (double)karigar.DefaultRate);
                cmd.Parameters.AddWithValue("@notes", (object?)karigar.Notes ?? DBNull.Value);
                cmd.ExecuteNonQuery();
            }
            SyncBackupJson();
        }
    }

    public void DeleteKarigar(int id)
    {
        lock (_lock)
        {
            using var conn = GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM karigars WHERE id = @id;";
            cmd.Parameters.AddWithValue("@id", id);
            cmd.ExecuteNonQuery();
            SyncBackupJson();
        }
    }
    #endregion

    #region Lot Methods
    public List<Lot> GetLots(DateTime? fromDate = null, DateTime? toDate = null, bool includeDiscarded = false)
    {
        lock (_lock)
        {
            var lots = new List<Lot>();
            using var conn = GetConnection();
            using var cmd = conn.CreateCommand();

            var query = @"
                SELECT id, lot_number, start_date, end_date, merchant_name, merchant_phone, item_name, color,
                       photo_url, total_pieces, merchant_rate, cutting_master_name, cutting_rate,
                       interlock_master_name, interlock_rate, other_expense, other_expense_note,
                       status, notes
                FROM lots
            ";

            var conditions = new List<string>();
            if (!includeDiscarded)
            {
                conditions.Add("status != 'Discarded'");
            }
            if (fromDate.HasValue)
            {
                conditions.Add("date(start_date) >= date(@fromDate)");
                cmd.Parameters.AddWithValue("@fromDate", fromDate.Value.ToString("yyyy-MM-dd"));
            }
            if (toDate.HasValue)
            {
                conditions.Add("date(start_date) <= date(@toDate)");
                cmd.Parameters.AddWithValue("@toDate", toDate.Value.ToString("yyyy-MM-dd"));
            }

            if (conditions.Any())
            {
                query += " WHERE " + string.Join(" AND ", conditions);
            }

            query += " ORDER BY id DESC;";
            cmd.CommandText = query;

            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    lots.Add(ReadLot(reader));
                }
            }

            // Load stitching entries for all loaded lots
            foreach (var lot in lots)
            {
                lot.StitchingEntries = GetStitchingEntriesForLot(lot.Id, conn);
            }

            return lots;
        }
    }

    public Lot? GetLotById(int id)
    {
        lock (_lock)
        {
            using var conn = GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT id, lot_number, start_date, end_date, merchant_name, merchant_phone, item_name, color,
                       photo_url, total_pieces, merchant_rate, cutting_master_name, cutting_rate,
                       interlock_master_name, interlock_rate, other_expense, other_expense_note,
                       status, notes
                FROM lots
                WHERE id = @id;
            ";
            cmd.Parameters.AddWithValue("@id", id);
            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                var lot = ReadLot(reader);
                lot.StitchingEntries = GetStitchingEntriesForLot(lot.Id, conn);
                return lot;
            }
            return null;
        }
    }

    private static Lot ReadLot(SqliteDataReader reader)
    {
        return new Lot
        {
            Id = reader.GetInt32(0),
            LotNumber = reader.GetString(1),
            StartDate = !reader.IsDBNull(2) && DateTime.TryParse(reader.GetString(2), out var sdt) ? sdt : (DateTime?)null,
            EndDate = !reader.IsDBNull(3) && DateTime.TryParse(reader.GetString(3), out var edt) ? edt : null,
            MerchantName = reader.GetString(4),
            MerchantPhone = reader.IsDBNull(5) ? null : reader.GetString(5),
            ItemName = reader.GetString(6),
            Color = reader.IsDBNull(7) ? "" : reader.GetString(7),
            PhotoUrl = reader.IsDBNull(8) ? null : reader.GetString(8),
            TotalPieces = reader.GetInt32(9),
            MerchantRatePerPiece = (decimal)reader.GetDouble(10),
            CuttingMasterName = reader.IsDBNull(11) ? "" : reader.GetString(11),
            CuttingRatePerPiece = (decimal)reader.GetDouble(12),
            InterlockMasterName = reader.IsDBNull(13) ? "" : reader.GetString(13),
            InterlockRatePerPiece = (decimal)reader.GetDouble(14),
            OtherExpense = (decimal)reader.GetDouble(15),
            OtherExpenseNote = reader.IsDBNull(16) ? null : reader.GetString(16),
            Status = reader.GetString(17),
            Notes = reader.IsDBNull(18) ? null : reader.GetString(18)
        };
    }

    private static List<LotStitchingEntry> GetStitchingEntriesForLot(int lotId, SqliteConnection conn)
    {
        var entries = new List<LotStitchingEntry>();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SELECT karigar_id, karigar_name, pieces_count, rate_per_piece, notes
            FROM lot_stitching_entries
            WHERE lot_id = @lotId
            ORDER BY id ASC;
        ";
        cmd.Parameters.AddWithValue("@lotId", lotId);
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            entries.Add(new LotStitchingEntry
            {
                KarigarId = reader.GetInt32(0),
                KarigarName = reader.GetString(1),
                PiecesCount = reader.GetInt32(2),
                RatePerPiece = (decimal)reader.GetDouble(3),
                Notes = reader.IsDBNull(4) ? null : reader.GetString(4)
            });
        }
        return entries;
    }

    public void SaveLot(Lot lot)
    {
        lock (_lock)
        {
            using var conn = GetConnection();
            using var tx = conn.BeginTransaction();

            if (lot.Id == 0)
            {
                using var idCmd = conn.CreateCommand();
                idCmd.Transaction = tx;
                idCmd.CommandText = "SELECT COALESCE(MAX(id), 0) + 1 FROM lots;";
                var nextId = Convert.ToInt32(idCmd.ExecuteScalar());

                if (string.IsNullOrWhiteSpace(lot.LotNumber))
                {
                    lot.LotNumber = $"Lot #{nextId}";
                }

                using var cmd = conn.CreateCommand();
                cmd.Transaction = tx;
                cmd.CommandText = @"
                    INSERT INTO lots (lot_number, start_date, end_date, merchant_name, merchant_phone, item_name,
                                      color, photo_url, total_pieces, merchant_rate, cutting_master_name,
                                      cutting_rate, interlock_master_name, interlock_rate, other_expense,
                                      other_expense_note, status, notes, created_at)
                    VALUES (@lot_number, @start_date, @end_date, @merchant_name, @merchant_phone, @item_name,
                            @color, @photo_url, @total_pieces, @merchant_rate, @cutting_master_name,
                            @cutting_rate, @interlock_master_name, @interlock_rate, @other_expense,
                            @other_expense_note, @status, @notes, @created_at);
                    SELECT last_insert_rowid();
                ";
                AddLotParameters(cmd, lot);
                cmd.Parameters.AddWithValue("@created_at", DateTime.Now.ToString("o"));
                lot.Id = Convert.ToInt32(cmd.ExecuteScalar());
            }
            else
            {
                using var cmd = conn.CreateCommand();
                cmd.Transaction = tx;
                cmd.CommandText = @"
                    UPDATE lots
                    SET lot_number = @lot_number, start_date = @start_date, end_date = @end_date,
                        merchant_name = @merchant_name, merchant_phone = @merchant_phone, item_name = @item_name,
                        color = @color, photo_url = @photo_url, total_pieces = @total_pieces,
                        merchant_rate = @merchant_rate, cutting_master_name = @cutting_master_name,
                        cutting_rate = @cutting_rate, interlock_master_name = @interlock_master_name,
                        interlock_rate = @interlock_rate, other_expense = @other_expense,
                        other_expense_note = @other_expense_note, status = @status, notes = @notes
                    WHERE id = @id;
                ";
                cmd.Parameters.AddWithValue("@id", lot.Id);
                AddLotParameters(cmd, lot);
                cmd.ExecuteNonQuery();
            }

            // ONLY replace stitching entries if lot.StitchingEntries has items!
            // When editing lot details from Manage page, stitching entries are preserved untouched!
            if (lot.StitchingEntries != null && lot.StitchingEntries.Any())
            {
                using var delCmd = conn.CreateCommand();
                delCmd.Transaction = tx;
                delCmd.CommandText = "DELETE FROM lot_stitching_entries WHERE lot_id = @lotId;";
                delCmd.Parameters.AddWithValue("@lotId", lot.Id);
                delCmd.ExecuteNonQuery();

                foreach (var entry in lot.StitchingEntries.Where(e => e.PiecesCount > 0))
                {
                    using var scmd = conn.CreateCommand();
                    scmd.Transaction = tx;
                    scmd.CommandText = @"
                        INSERT INTO lot_stitching_entries (lot_id, karigar_id, karigar_name, pieces_count, rate_per_piece, notes)
                        VALUES (@lot_id, @karigar_id, @karigar_name, @pieces_count, @rate_per_piece, @notes);
                    ";
                    scmd.Parameters.AddWithValue("@lot_id", lot.Id);
                    scmd.Parameters.AddWithValue("@karigar_id", entry.KarigarId);
                    scmd.Parameters.AddWithValue("@karigar_name", entry.KarigarName);
                    scmd.Parameters.AddWithValue("@pieces_count", entry.PiecesCount);
                    scmd.Parameters.AddWithValue("@rate_per_piece", (double)entry.RatePerPiece);
                    scmd.Parameters.AddWithValue("@notes", (object?)entry.Notes ?? DBNull.Value);
                    scmd.ExecuteNonQuery();
                }
            }

            tx.Commit();
            SyncBackupJson();
        }
    }

    private static void AddLotParameters(SqliteCommand cmd, Lot lot)
    {
        cmd.Parameters.AddWithValue("@lot_number", lot.LotNumber ?? "");
        cmd.Parameters.AddWithValue("@start_date", lot.StartDate.HasValue ? lot.StartDate.Value.ToString("yyyy-MM-dd") : DateTime.Today.ToString("yyyy-MM-dd"));
        cmd.Parameters.AddWithValue("@end_date", lot.EndDate.HasValue ? lot.EndDate.Value.ToString("yyyy-MM-dd") : DBNull.Value);
        cmd.Parameters.AddWithValue("@merchant_name", lot.MerchantName ?? "");
        cmd.Parameters.AddWithValue("@merchant_phone", (object?)lot.MerchantPhone ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@item_name", lot.ItemName ?? "");
        cmd.Parameters.AddWithValue("@color", (object?)lot.Color ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@photo_url", (object?)lot.PhotoUrl ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@total_pieces", lot.TotalPieces);
        cmd.Parameters.AddWithValue("@merchant_rate", (double)lot.MerchantRatePerPiece);
        cmd.Parameters.AddWithValue("@cutting_master_name", (object?)lot.CuttingMasterName ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@cutting_rate", (double)lot.CuttingRatePerPiece);
        cmd.Parameters.AddWithValue("@interlock_master_name", (object?)lot.InterlockMasterName ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@interlock_rate", (double)lot.InterlockRatePerPiece);
        cmd.Parameters.AddWithValue("@other_expense", (double)lot.OtherExpense);
        cmd.Parameters.AddWithValue("@other_expense_note", (object?)lot.OtherExpenseNote ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@status", lot.Status ?? "In Progress");
        cmd.Parameters.AddWithValue("@notes", (object?)lot.Notes ?? DBNull.Value);
    }

    public void DiscardLot(int id)
    {
        lock (_lock)
        {
            using var conn = GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "UPDATE lots SET status = 'Discarded' WHERE id = @id;";
            cmd.Parameters.AddWithValue("@id", id);
            cmd.ExecuteNonQuery();
            SyncBackupJson();
        }
    }

    public void RestoreLot(int id)
    {
        lock (_lock)
        {
            using var conn = GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "UPDATE lots SET status = 'In Progress' WHERE id = @id;";
            cmd.Parameters.AddWithValue("@id", id);
            cmd.ExecuteNonQuery();
            SyncBackupJson();
        }
    }

    public void MarkLotReady(int id, DateTime endDate)
    {
        lock (_lock)
        {
            using var conn = GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                UPDATE lots
                SET status = 'Ready', end_date = @end_date
                WHERE id = @id;
            ";
            cmd.Parameters.AddWithValue("@id", id);
            cmd.Parameters.AddWithValue("@end_date", endDate.ToString("yyyy-MM-dd"));
            cmd.ExecuteNonQuery();
            SyncBackupJson();
        }
    }

    public void DeleteLot(int id)
    {
        DiscardLot(id);
    }
    #endregion

    #region Master Items
    public List<MasterItem> GetMasterItems()
    {
        lock (_lock)
        {
            var list = new List<MasterItem>();
            using var conn = GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT id, name FROM master_items ORDER BY name ASC;";
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new MasterItem { Id = reader.GetInt32(0), Name = reader.GetString(1) });
            }
            return list;
        }
    }

    public MasterItem? GetMasterItemById(int id)
    {
        lock (_lock)
        {
            using var conn = GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT id, name FROM master_items WHERE id = @id;";
            cmd.Parameters.AddWithValue("@id", id);
            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                return new MasterItem { Id = reader.GetInt32(0), Name = reader.GetString(1) };
            }
            return null;
        }
    }

    public void SaveMasterItem(MasterItem item)
    {
        lock (_lock)
        {
            using var conn = GetConnection();
            using var cmd = conn.CreateCommand();
            if (item.Id == 0)
            {
                cmd.CommandText = "INSERT INTO master_items (name) VALUES (@name); SELECT last_insert_rowid();";
                cmd.Parameters.AddWithValue("@name", item.Name);
                item.Id = Convert.ToInt32(cmd.ExecuteScalar());
            }
            else
            {
                cmd.CommandText = "UPDATE master_items SET name = @name WHERE id = @id;";
                cmd.Parameters.AddWithValue("@id", item.Id);
                cmd.Parameters.AddWithValue("@name", item.Name);
                cmd.ExecuteNonQuery();
            }
            SyncBackupJson();
        }
    }

    public void DeleteMasterItem(int id)
    {
        lock (_lock)
        {
            using var conn = GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM master_items WHERE id = @id;";
            cmd.Parameters.AddWithValue("@id", id);
            cmd.ExecuteNonQuery();
            SyncBackupJson();
        }
    }
    #endregion

    #region Master Merchants
    public List<MasterMerchant> GetMasterMerchants()
    {
        lock (_lock)
        {
            var list = new List<MasterMerchant>();
            using var conn = GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT id, name, phone, notes FROM master_merchants ORDER BY name ASC;";
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new MasterMerchant
                {
                    Id = reader.GetInt32(0),
                    Name = reader.GetString(1),
                    Phone = reader.IsDBNull(2) ? null : reader.GetString(2),
                    Notes = reader.IsDBNull(3) ? null : reader.GetString(3)
                });
            }
            return list;
        }
    }

    public MasterMerchant? GetMasterMerchantById(int id)
    {
        lock (_lock)
        {
            using var conn = GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT id, name, phone, notes FROM master_merchants WHERE id = @id;";
            cmd.Parameters.AddWithValue("@id", id);
            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                return new MasterMerchant
                {
                    Id = reader.GetInt32(0),
                    Name = reader.GetString(1),
                    Phone = reader.IsDBNull(2) ? null : reader.GetString(2),
                    Notes = reader.IsDBNull(3) ? null : reader.GetString(3)
                };
            }
            return null;
        }
    }

    public void SaveMasterMerchant(MasterMerchant merchant)
    {
        lock (_lock)
        {
            using var conn = GetConnection();
            using var cmd = conn.CreateCommand();
            if (merchant.Id == 0)
            {
                cmd.CommandText = @"
                    INSERT INTO master_merchants (name, phone, notes)
                    VALUES (@name, @phone, @notes);
                    SELECT last_insert_rowid();
                ";
                cmd.Parameters.AddWithValue("@name", merchant.Name);
                cmd.Parameters.AddWithValue("@phone", (object?)merchant.Phone ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@notes", (object?)merchant.Notes ?? DBNull.Value);
                merchant.Id = Convert.ToInt32(cmd.ExecuteScalar());
            }
            else
            {
                cmd.CommandText = @"
                    UPDATE master_merchants
                    SET name = @name, phone = @phone, notes = @notes
                    WHERE id = @id;
                ";
                cmd.Parameters.AddWithValue("@id", merchant.Id);
                cmd.Parameters.AddWithValue("@name", merchant.Name);
                cmd.Parameters.AddWithValue("@phone", (object?)merchant.Phone ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@notes", (object?)merchant.Notes ?? DBNull.Value);
                cmd.ExecuteNonQuery();
            }
            SyncBackupJson();
        }
    }

    public void DeleteMasterMerchant(int id)
    {
        lock (_lock)
        {
            using var conn = GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM master_merchants WHERE id = @id;";
            cmd.Parameters.AddWithValue("@id", id);
            cmd.ExecuteNonQuery();
            SyncBackupJson();
        }
    }
    #endregion

    #region Master Cutting Masters
    public List<MasterCuttingMaster> GetMasterCuttingMasters()
    {
        lock (_lock)
        {
            var list = new List<MasterCuttingMaster>();
            using var conn = GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT id, name, default_rate, phone FROM master_cutting_masters ORDER BY name ASC;";
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new MasterCuttingMaster
                {
                    Id = reader.GetInt32(0),
                    Name = reader.GetString(1),
                    DefaultRate = (decimal)reader.GetDouble(2),
                    Phone = reader.IsDBNull(3) ? null : reader.GetString(3)
                });
            }
            return list;
        }
    }

    public MasterCuttingMaster? GetMasterCuttingMasterById(int id)
    {
        lock (_lock)
        {
            using var conn = GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT id, name, default_rate, phone FROM master_cutting_masters WHERE id = @id;";
            cmd.Parameters.AddWithValue("@id", id);
            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                return new MasterCuttingMaster
                {
                    Id = reader.GetInt32(0),
                    Name = reader.GetString(1),
                    DefaultRate = (decimal)reader.GetDouble(2),
                    Phone = reader.IsDBNull(3) ? null : reader.GetString(3)
                };
            }
            return null;
        }
    }

    public void SaveMasterCuttingMaster(MasterCuttingMaster cuttingMaster)
    {
        lock (_lock)
        {
            using var conn = GetConnection();
            using var cmd = conn.CreateCommand();
            if (cuttingMaster.Id == 0)
            {
                cmd.CommandText = @"
                    INSERT INTO master_cutting_masters (name, default_rate, phone)
                    VALUES (@name, @rate, @phone);
                    SELECT last_insert_rowid();
                ";
                cmd.Parameters.AddWithValue("@name", cuttingMaster.Name);
                cmd.Parameters.AddWithValue("@rate", (double)cuttingMaster.DefaultRate);
                cmd.Parameters.AddWithValue("@phone", (object?)cuttingMaster.Phone ?? DBNull.Value);
                cuttingMaster.Id = Convert.ToInt32(cmd.ExecuteScalar());
            }
            else
            {
                cmd.CommandText = @"
                    UPDATE master_cutting_masters
                    SET name = @name, default_rate = @rate, phone = @phone
                    WHERE id = @id;
                ";
                cmd.Parameters.AddWithValue("@id", cuttingMaster.Id);
                cmd.Parameters.AddWithValue("@name", cuttingMaster.Name);
                cmd.Parameters.AddWithValue("@rate", (double)cuttingMaster.DefaultRate);
                cmd.Parameters.AddWithValue("@phone", (object?)cuttingMaster.Phone ?? DBNull.Value);
                cmd.ExecuteNonQuery();
            }
            SyncBackupJson();
        }
    }

    public void DeleteMasterCuttingMaster(int id)
    {
        lock (_lock)
        {
            using var conn = GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM master_cutting_masters WHERE id = @id;";
            cmd.Parameters.AddWithValue("@id", id);
            cmd.ExecuteNonQuery();
            SyncBackupJson();
        }
    }
    #endregion

    #region Master Interlocks
    public List<MasterInterlock> GetMasterInterlocks()
    {
        lock (_lock)
        {
            var list = new List<MasterInterlock>();
            using var conn = GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT id, name, default_rate, phone FROM master_interlocks ORDER BY name ASC;";
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new MasterInterlock
                {
                    Id = reader.GetInt32(0),
                    Name = reader.GetString(1),
                    DefaultRate = (decimal)reader.GetDouble(2),
                    Phone = reader.IsDBNull(3) ? null : reader.GetString(3)
                });
            }
            return list;
        }
    }

    public MasterInterlock? GetMasterInterlockById(int id)
    {
        lock (_lock)
        {
            using var conn = GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT id, name, default_rate, phone FROM master_interlocks WHERE id = @id;";
            cmd.Parameters.AddWithValue("@id", id);
            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                return new MasterInterlock
                {
                    Id = reader.GetInt32(0),
                    Name = reader.GetString(1),
                    DefaultRate = (decimal)reader.GetDouble(2),
                    Phone = reader.IsDBNull(3) ? null : reader.GetString(3)
                };
            }
            return null;
        }
    }

    public void SaveMasterInterlock(MasterInterlock interlock)
    {
        lock (_lock)
        {
            using var conn = GetConnection();
            using var cmd = conn.CreateCommand();
            if (interlock.Id == 0)
            {
                cmd.CommandText = @"
                    INSERT INTO master_interlocks (name, default_rate, phone)
                    VALUES (@name, @rate, @phone);
                    SELECT last_insert_rowid();
                ";
                cmd.Parameters.AddWithValue("@name", interlock.Name);
                cmd.Parameters.AddWithValue("@rate", (double)interlock.DefaultRate);
                cmd.Parameters.AddWithValue("@phone", (object?)interlock.Phone ?? DBNull.Value);
                interlock.Id = Convert.ToInt32(cmd.ExecuteScalar());
            }
            else
            {
                cmd.CommandText = @"
                    UPDATE master_interlocks
                    SET name = @name, default_rate = @rate, phone = @phone
                    WHERE id = @id;
                ";
                cmd.Parameters.AddWithValue("@id", interlock.Id);
                cmd.Parameters.AddWithValue("@name", interlock.Name);
                cmd.Parameters.AddWithValue("@rate", (double)interlock.DefaultRate);
                cmd.Parameters.AddWithValue("@phone", (object?)interlock.Phone ?? DBNull.Value);
                cmd.ExecuteNonQuery();
            }
            SyncBackupJson();
        }
    }

    public void DeleteMasterInterlock(int id)
    {
        lock (_lock)
        {
            using var conn = GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM master_interlocks WHERE id = @id;";
            cmd.Parameters.AddWithValue("@id", id);
            cmd.ExecuteNonQuery();
            SyncBackupJson();
        }
    }
    #endregion

    #region Dropdown Helpers
    public List<string> GetMerchantNames()
    {
        return GetMasterMerchants().Select(m => m.Name).Distinct().OrderBy(n => n).ToList();
    }

    public List<string> GetItemNamesList()
    {
        return GetMasterItems().Select(i => i.Name).Distinct().ToList();
    }

    public List<string> GetCuttingMasterNames()
    {
        return GetMasterCuttingMasters().Select(c => c.Name).Distinct().OrderBy(n => n).ToList();
    }

    public List<string> GetInterlockNames()
    {
        return GetMasterInterlocks().Select(i => i.Name).Distinct().OrderBy(n => n).ToList();
    }
    #endregion

    #region Step Updates
    public void UpdateLotCutting(int lotId, string masterName, decimal ratePerPiece)
    {
        lock (_lock)
        {
            using var conn = GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                UPDATE lots
                SET cutting_master_name = @cname, cutting_rate = @crate
                WHERE id = @id;
            ";
            cmd.Parameters.AddWithValue("@id", lotId);
            cmd.Parameters.AddWithValue("@cname", (object?)masterName ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@crate", (double)ratePerPiece);
            cmd.ExecuteNonQuery();
            SyncBackupJson();
        }
    }

    public void UpdateLotInterlock(int lotId, string masterName, decimal ratePerPiece)
    {
        lock (_lock)
        {
            using var conn = GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                UPDATE lots
                SET interlock_master_name = @iname, interlock_rate = @irate
                WHERE id = @id;
            ";
            cmd.Parameters.AddWithValue("@id", lotId);
            cmd.Parameters.AddWithValue("@iname", (object?)masterName ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@irate", (double)ratePerPiece);
            cmd.ExecuteNonQuery();
            SyncBackupJson();
        }
    }

    public void UpdateLotStitching(int lotId, List<LotStitchingEntry> entries)
    {
        lock (_lock)
        {
            using var conn = GetConnection();
            using var tx = conn.BeginTransaction();

            using (var delCmd = conn.CreateCommand())
            {
                delCmd.Transaction = tx;
                delCmd.CommandText = "DELETE FROM lot_stitching_entries WHERE lot_id = @id;";
                delCmd.Parameters.AddWithValue("@id", lotId);
                delCmd.ExecuteNonQuery();
            }

            foreach (var e in entries.Where(e => e.PiecesCount > 0))
            {
                using var scmd = conn.CreateCommand();
                scmd.Transaction = tx;
                scmd.CommandText = @"
                    INSERT INTO lot_stitching_entries (lot_id, karigar_id, karigar_name, pieces_count, rate_per_piece, notes)
                    VALUES (@lot_id, @karigar_id, @karigar_name, @pieces, @rate, @notes);
                ";
                scmd.Parameters.AddWithValue("@lot_id", lotId);
                scmd.Parameters.AddWithValue("@karigar_id", e.KarigarId);
                scmd.Parameters.AddWithValue("@karigar_name", e.KarigarName);
                scmd.Parameters.AddWithValue("@pieces", e.PiecesCount);
                scmd.Parameters.AddWithValue("@rate", (double)e.RatePerPiece);
                scmd.Parameters.AddWithValue("@notes", (object?)e.Notes ?? DBNull.Value);
                scmd.ExecuteNonQuery();
            }

            tx.Commit();
            SyncBackupJson();
        }
    }
    #endregion

    #region Analytics & Reports
    public DashboardSummary GetDashboardSummary(DateTime? fromDate = null, DateTime? toDate = null)
    {
        var lots = GetLots(fromDate, toDate);
        var karigars = GetKarigars();

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
            TotalKarigars = karigars.Count
        };
    }

    public (int TotalPieces, decimal TotalEarnings, List<(Lot Lot, int Pieces, decimal Amount)> History) GetKarigarLedger(int karigarId)
    {
        var allLots = GetLots();
        var history = new List<(Lot Lot, int Pieces, decimal Amount)>();
        int totalPieces = 0;
        decimal totalEarnings = 0;

        foreach (var lot in allLots)
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
    #endregion

    #region Backup & Restore
    public string ExportDataJson()
    {
        var store = new GarmentDataStore
        {
            MasterItems = GetMasterItems(),
            MasterMerchants = GetMasterMerchants(),
            MasterCuttingMasters = GetMasterCuttingMasters(),
            MasterInterlocks = GetMasterInterlocks(),
            Karigars = GetKarigars(),
            Lots = GetLots()
        };
        return JsonSerializer.Serialize(store, new JsonSerializerOptions { WriteIndented = true });
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
                    using var conn = GetConnection();
                    ImportStoreData(store, conn);
                    SyncBackupJson();
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
    #endregion
}
