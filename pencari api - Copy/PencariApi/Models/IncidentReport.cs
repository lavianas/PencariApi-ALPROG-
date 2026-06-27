using SQLite;

namespace PencariApi.Models;

public class IncidentReport
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public string ReporterUsername { get; set; } = string.Empty;

    public string ReporterName { get; set; } = string.Empty;

    public string Phone { get; set; } = string.Empty;

    public string Address { get; set; } = string.Empty;

    public string Detail { get; set; } = string.Empty;

    public string Priority { get; set; } = "Sedang";

    public string Status { get; set; } = "Menunggu";

    public double Latitude { get; set; }

    public double Longitude { get; set; }

    public string NearestStationName { get; set; } = string.Empty;

    public string NearestStationAddress { get; set; } = string.Empty;

    public double DistanceKm { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;
}