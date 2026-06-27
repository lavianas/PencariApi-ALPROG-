using PencariApi.Models;
using PencariApi.Services;
using System.Globalization;

namespace PencariApi;

public partial class AdminMapPage : ContentPage
{
    private List<IncidentReport> _reports = new();
    private List<FireStation> _stations = new();

    private IncidentReport? _selectedReport;
    private FireStation? _selectedStation;

    private string _currentMapUrl = "";
    private readonly int? _initialReportId;

    public AdminMapPage()
    {
        InitializeComponent();

        _ = LoadDataAsync();
    }

    public AdminMapPage(IncidentReport selectedReport)
    {
        InitializeComponent();

        _initialReportId = selectedReport.Id;

        _ = LoadDataAsync();
    }

    private async Task LoadDataAsync()
    {
        try
        {
            ShowMapLoading(true);

            _reports = await DatabaseService.GetIncidentReportsAsync();
            _stations = await DatabaseService.GetFireStationsAsync();

            ReportsCollection.ItemsSource = _reports;

            ReportCountLabel.Text = $"{_reports.Count} laporan tersedia";

            if (_reports.Count == 0)
            {
                ClearRouteInfo();
                LoadDefaultSurabayaMap();
                return;
            }

            IncidentReport reportToOpen;

            if (_initialReportId.HasValue)
            {
                reportToOpen = _reports.FirstOrDefault(r => r.Id == _initialReportId.Value)
                    ?? _reports.First();
            }
            else
            {
                reportToOpen = _reports.First();
            }

            ReportsCollection.SelectedItem = reportToOpen;

            await LoadRouteForReportAsync(reportToOpen);
        }
        catch (Exception ex)
        {
            ShowMapLoading(false);

            await DisplayAlert(
                "Error",
                $"Gagal memuat data peta.\n\n{ex.Message}",
                "OK"
            );
        }
    }

    private async void OnRefreshClicked(object sender, EventArgs e)
    {
        await LoadDataAsync();
    }

    private async void OnReportSelected(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is IncidentReport report)
        {
            await LoadRouteForReportAsync(report);
        }
    }

    private async void OnShowSelectedRouteClicked(object sender, EventArgs e)
    {
        if (_selectedReport == null)
        {
            await DisplayAlert(
                "Pilih Laporan",
                "Pilih salah satu laporan terlebih dahulu.",
                "OK"
            );

            return;
        }

        await LoadRouteForReportAsync(_selectedReport);
    }

    private async Task LoadRouteForReportAsync(IncidentReport report)
    {
        try
        {
            _selectedReport = report;

            FireStation? nearestStation = FindNearestStation(report);

            if (nearestStation == null)
            {
                await DisplayAlert(
                    "Pos Damkar Tidak Ada",
                    "Data pos pemadam belum tersedia di database.",
                    "OK"
                );

                return;
            }

            _selectedStation = nearestStation;

            double distanceKm = DatabaseService.CalculateDistanceKm(
                nearestStation.Latitude,
                nearestStation.Longitude,
                report.Latitude,
                report.Longitude
            );

            UpdateRouteInfo(report, nearestStation, distanceKm);

            string originLat = nearestStation.Latitude.ToString(CultureInfo.InvariantCulture);
            string originLon = nearestStation.Longitude.ToString(CultureInfo.InvariantCulture);
            string destLat = report.Latitude.ToString(CultureInfo.InvariantCulture);
            string destLon = report.Longitude.ToString(CultureInfo.InvariantCulture);

            _currentMapUrl =
                $"https://www.google.com/maps/dir/?api=1" +
                $"&origin={originLat},{originLon}" +
                $"&destination={destLat},{destLon}" +
                $"&travelmode=driving";

            ShowMapLoading(true);

            WebMap.Source = new UrlWebViewSource
            {
                Url = _currentMapUrl
            };

            await Task.Delay(1200);

            ShowMapLoading(false);
        }
        catch (Exception ex)
        {
            ShowMapLoading(false);

            await DisplayAlert(
                "Error Rute",
                $"Gagal menampilkan rute.\n\n{ex.Message}",
                "OK"
            );
        }
    }

    private FireStation? FindNearestStation(IncidentReport report)
    {
        if (_stations == null || _stations.Count == 0)
            return null;

        FireStation nearestStation = _stations[0];

        double nearestDistance = DatabaseService.CalculateDistanceKm(
            report.Latitude,
            report.Longitude,
            nearestStation.Latitude,
            nearestStation.Longitude
        );

        foreach (FireStation station in _stations)
        {
            double distance = DatabaseService.CalculateDistanceKm(
                report.Latitude,
                report.Longitude,
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

    private void UpdateRouteInfo(
        IncidentReport report,
        FireStation station,
        double distanceKm)
    {
        SelectedReportTitleLabel.Text = report.Address;
        ReporterLabel.Text = $"Pelapor: {report.ReporterName}";
        PhoneLabel.Text = $"Telepon: {report.Phone}";
        PriorityLabel.Text = $"Prioritas: {report.Priority}";

        AddressLabel.Text = $"Alamat: {report.Address}";
        CoordinateLabel.Text = $"Koordinat: {report.Latitude:F6}, {report.Longitude:F6}";
        DetailLabel.Text = $"Detail: {report.Detail}";

        StationNameLabel.Text = $"Pos Damkar: {station.Name}";
        StationAddressLabel.Text = $"Alamat Pos: {station.Address}";
        DistanceLabel.Text = $"Jarak garis lurus: {distanceKm:F2} km";

        RouteInfoLabel.Text =
            "Rute efektif dipilih dari pos pemadam terdekat menuju lokasi laporan melalui Google Maps.";
    }

    private void ClearRouteInfo()
    {
        _selectedReport = null;
        _selectedStation = null;
        _currentMapUrl = "";

        SelectedReportTitleLabel.Text = "Belum ada laporan";
        ReporterLabel.Text = "Pelapor: -";
        PhoneLabel.Text = "Telepon: -";
        PriorityLabel.Text = "Prioritas: -";

        AddressLabel.Text = "Alamat: -";
        CoordinateLabel.Text = "Koordinat: -";
        DetailLabel.Text = "Detail: -";

        StationNameLabel.Text = "Pos Damkar: -";
        StationAddressLabel.Text = "Alamat Pos: -";
        DistanceLabel.Text = "Jarak: -";
        RouteInfoLabel.Text = "Rute akan tampil setelah laporan dipilih.";
    }

    private void LoadDefaultSurabayaMap()
    {
        _currentMapUrl = "https://www.google.com/maps/@-7.2575,112.7521,12z";

        WebMap.Source = new UrlWebViewSource
        {
            Url = _currentMapUrl
        };

        ShowMapLoading(false);
    }

    private async void OnOpenBrowserClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_currentMapUrl))
        {
            await DisplayAlert(
                "Belum Ada Rute",
                "Pilih laporan terlebih dahulu agar rute Google Maps bisa dibuka.",
                "OK"
            );

            return;
        }

        try
        {
            await Launcher.Default.OpenAsync(_currentMapUrl);
        }
        catch (Exception ex)
        {
            await DisplayAlert(
                "Error Browser",
                $"Gagal membuka Google Maps di browser.\n\n{ex.Message}",
                "OK"
            );
        }
    }

    private void ShowMapLoading(bool isLoading)
    {
        MapLoadingFrame.IsVisible = isLoading;
        MapLoadingIndicator.IsRunning = isLoading;
    }

    private void OnBackDashboardClicked(object sender, EventArgs e)
    {
        Application.Current!.Windows[0].Page = new MainPage();
    }
}