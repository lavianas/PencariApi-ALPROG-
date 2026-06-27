using PencariApi.Models;
using SQLite;

namespace PencariApi.Services;

public static class DatabaseService
{
    private static SQLiteAsyncConnection? _database;

    private static async Task InitAsync()
    {
        if (_database != null)
        {
            return;
        }

        string dbPath = Path.Combine(
            FileSystem.AppDataDirectory,
            "fire_optima_database_v2.db3"
        );

        _database = new SQLiteAsyncConnection(dbPath);

        await _database.CreateTableAsync<UserAccount>();
        await _database.CreateTableAsync<FireStation>();
        await _database.CreateTableAsync<IncidentReport>();

        await SeedFireStationsAsync();
        await SeedDummyReportsAsync();
    }

    public static async Task<int> SaveUserAsync(UserAccount user)
    {
        await InitAsync();

        UserAccount? existingUser = await GetUserAsync(user.Username);

        if (existingUser == null)
        {
            return await _database!.InsertAsync(user);
        }

        return await _database!.UpdateAsync(user);
    }

    public static async Task<UserAccount?> GetUserAsync(string username)
    {
        await InitAsync();

        return await _database!
            .Table<UserAccount>()
            .Where(u => u.Username == username)
            .FirstOrDefaultAsync();
    }

    public static async Task<List<UserAccount>> GetAllUsersAsync()
    {
        await InitAsync();

        return await _database!
            .Table<UserAccount>()
            .OrderByDescending(u => u.CreatedAt)
            .ToListAsync();
    }

    public static async Task<bool> UsernameExistsAsync(string username)
    {
        UserAccount? user = await GetUserAsync(username);
        return user != null;
    }

    public static async Task<List<FireStation>> GetFireStationsAsync()
    {
        await InitAsync();

        return await _database!
            .Table<FireStation>()
            .OrderBy(s => s.Name)
            .ToListAsync();
    }

    public static async Task<FireStation?> GetNearestFireStationAsync(double latitude, double longitude)
    {
        await InitAsync();

        List<FireStation> stations = await GetFireStationsAsync();

        if (stations.Count == 0)
        {
            return null;
        }

        FireStation nearestStation = stations[0];

        double nearestDistance = CalculateDistanceKm(
            latitude,
            longitude,
            nearestStation.Latitude,
            nearestStation.Longitude
        );

        foreach (FireStation station in stations)
        {
            double distance = CalculateDistanceKm(
                latitude,
                longitude,
                station.Latitude,
                station.Longitude
            );

            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestStation = station;
            }
        }

        return nearestStation;
    }

    public static double CalculateDistanceKm(
        double lat1,
        double lon1,
        double lat2,
        double lon2)
    {
        const double earthRadiusKm = 6371;

        double dLat = DegreesToRadians(lat2 - lat1);
        double dLon = DegreesToRadians(lon2 - lon1);

        double a =
            Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
            Math.Cos(DegreesToRadians(lat1)) *
            Math.Cos(DegreesToRadians(lat2)) *
            Math.Sin(dLon / 2) *
            Math.Sin(dLon / 2);

        double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

        return earthRadiusKm * c;
    }

    private static double DegreesToRadians(double degrees)
    {
        return degrees * Math.PI / 180;
    }

    public static async Task<int> SaveIncidentReportAsync(IncidentReport report)
    {
        await InitAsync();

        FireStation? nearestStation = await GetNearestFireStationAsync(
            report.Latitude,
            report.Longitude
        );

        if (nearestStation != null)
        {
            report.NearestStationName = nearestStation.Name;
            report.NearestStationAddress = nearestStation.Address;
            report.DistanceKm = CalculateDistanceKm(
                report.Latitude,
                report.Longitude,
                nearestStation.Latitude,
                nearestStation.Longitude
            );
        }

        report.CreatedAt = DateTime.Now;

        return await _database!.InsertAsync(report);
    }

    public static async Task<List<IncidentReport>> GetIncidentReportsAsync()
    {
        await InitAsync();

        return await _database!
            .Table<IncidentReport>()
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    public static async Task<List<IncidentReport>> GetReportsByUserAsync(string username)
    {
        await InitAsync();

        return await _database!
            .Table<IncidentReport>()
            .Where(r => r.ReporterUsername == username)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    public static async Task<int> UpdateReportStatusAsync(int reportId, string status)
    {
        await InitAsync();

        IncidentReport? report = await _database!
            .Table<IncidentReport>()
            .Where(r => r.Id == reportId)
            .FirstOrDefaultAsync();

        if (report == null)
        {
            return 0;
        }

        report.Status = status;

        return await _database!.UpdateAsync(report);
    }

    private static async Task SeedFireStationsAsync()
    {
        int count = await _database!
            .Table<FireStation>()
            .CountAsync();

        if (count > 0)
        {
            return;
        }

        List<FireStation> stations = new List<FireStation>
        {
            new FireStation
            {
                Name = "DPKP Surabaya Pusat",
                Address = "Jl. Pasar Turi No.21, Bubutan, Surabaya",
                Phone = "112 / 031-3536346",
                Area = "Surabaya Pusat",
                Latitude = -7.2459,
                Longitude = 112.7342
            },
            new FireStation
            {
                Name = "Pos Damkar Balas Klumprik",
                Address = "Balas Klumprik, Wiyung, Surabaya",
                Phone = "112",
                Area = "Surabaya Barat / Wiyung",
                Latitude = -7.3361,
                Longitude = 112.6625
            },
            new FireStation
            {
                Name = "UPTD PMK Wiyung",
                Address = "Jl. Raya Wiyung, Surabaya",
                Phone = "112",
                Area = "Surabaya Selatan",
                Latitude = -7.3088,
                Longitude = 112.6926
            }
        };

        await _database!.InsertAllAsync(stations);
    }

    private static async Task SeedDummyReportsAsync()
    {
        int count = await _database!
            .Table<IncidentReport>()
            .CountAsync();

        if (count > 0)
        {
            return;
        }

        List<IncidentReport> reports = new List<IncidentReport>
        {
            new IncidentReport
            {
                ReporterUsername = "dummy_user_1",
                ReporterName = "Budi Santoso",
                Phone = "081234567001",
                Address = "Jl. Tunjungan, Surabaya",
                Detail = "Terlihat asap tebal dari bangunan toko.",
                Priority = "Tinggi",
                Status = "Menunggu",
                Latitude = -7.2575,
                Longitude = 112.7381
            },
            new IncidentReport
            {
                ReporterUsername = "dummy_user_2",
                ReporterName = "Siti Aminah",
                Phone = "081234567002",
                Address = "Jl. Mayjen Sungkono, Surabaya",
                Detail = "Ada percikan api di area kabel listrik.",
                Priority = "Sedang",
                Status = "Diproses",
                Latitude = -7.2929,
                Longitude = 112.7071
            },
            new IncidentReport
            {
                ReporterUsername = "dummy_user_3",
                ReporterName = "Andi Wijaya",
                Phone = "081234567003",
                Address = "Area Balas Klumprik, Wiyung, Surabaya",
                Detail = "Laporan kebakaran kecil dekat area permukiman.",
                Priority = "Tinggi",
                Status = "Menunggu",
                Latitude = -7.3358,
                Longitude = 112.6631
            }
        };

        foreach (IncidentReport report in reports)
        {
            await SaveIncidentReportAsync(report);
        }
    }

    public static string GetDatabasePath()
    {
        return Path.Combine(
            FileSystem.AppDataDirectory,
            "fire_optima_database_v2.db3"
        );
    }
}