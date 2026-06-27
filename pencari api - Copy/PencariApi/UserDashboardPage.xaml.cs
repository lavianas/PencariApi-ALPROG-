using PencariApi.Models;
using PencariApi.Services;
using System.Globalization;

namespace PencariApi;

public partial class UserDashboardPage : ContentPage
{
    private double _currentLatitude = -7.2575;
    private double _currentLongitude = 112.7381;

    private FireStation? _nearestStation;
    private IncidentReport? _selectedReport;

    private readonly string _username;

    public UserDashboardPage()
    {
        InitializeComponent();

        _username = Preferences.Get("current_username", "user_demo");

        PriorityPicker.SelectedIndex = 1;

        LocationLabel.Text = $"Lokasi default Surabaya: {_currentLatitude:F6}, {_currentLongitude:F6}";

        _ = LoadMyReportsAsync();
        _ = UpdateNearestStationAsync();
    }

    private async Task LoadMyReportsAsync()
    {
        List<IncidentReport> reports = await DatabaseService.GetReportsByUserAsync(_username);
        MyReportsCollection.ItemsSource = reports;
    }

    private async Task UpdateNearestStationAsync()
    {
        _nearestStation = await DatabaseService.GetNearestFireStationAsync(
            _currentLatitude,
            _currentLongitude
        );

        if (_nearestStation == null)
        {
            NearestStationNameLabel.Text = "Pos tidak ditemukan";
            NearestStationAddressLabel.Text = "-";
            NearestStationDistanceLabel.Text = "Jarak: -";
            return;
        }

        double distance = DatabaseService.CalculateDistanceKm(
            _currentLatitude,
            _currentLongitude,
            _nearestStation.Latitude,
            _nearestStation.Longitude
        );

        NearestStationNameLabel.Text = _nearestStation.Name;
        NearestStationAddressLabel.Text = _nearestStation.Address;
        NearestStationDistanceLabel.Text = $"Jarak sekitar: {distance:F2} km";
    }

    private async void OnGetLocationClicked(object sender, EventArgs e)
    {
        try
        {
            GeolocationRequest request = new GeolocationRequest(
                GeolocationAccuracy.Medium,
                TimeSpan.FromSeconds(10)
            );

            Location? location = await Geolocation.Default.GetLocationAsync(request);

            if (location == null)
            {
                await DisplayAlert(
                    "Lokasi Tidak Ditemukan",
                    "Lokasi tidak bisa didapatkan. Sistem memakai default Surabaya.",
                    "OK"
                );

                return;
            }

            _currentLatitude = location.Latitude;
            _currentLongitude = location.Longitude;

            LocationLabel.Text = $"Lokasi kamu: {_currentLatitude:F6}, {_currentLongitude:F6}";

            await UpdateNearestStationAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlert(
                "Error Lokasi",
                $"Gagal mengambil lokasi. Sistem memakai default Surabaya.\n\n{ex.Message}",
                "OK"
            );

            LocationLabel.Text = $"Lokasi default Surabaya: {_currentLatitude:F6}, {_currentLongitude:F6}";

            await UpdateNearestStationAsync();
        }
    }

    private async void OnOpenMyLocationMapClicked(object sender, EventArgs e)
    {
        await OpenGoogleMapsAsync(
            _currentLatitude,
            _currentLongitude,
            "Lokasi Laporan User"
        );
    }

    private async void OnOpenNearestStationMapClicked(object sender, EventArgs e)
    {
        if (_nearestStation == null)
        {
            await DisplayAlert(
                "Info",
                "Pos pemadam terdekat belum tersedia.",
                "OK"
            );

            return;
        }

        await OpenGoogleMapsAsync(
            _nearestStation.Latitude,
            _nearestStation.Longitude,
            _nearestStation.Name
        );
    }

    private void OnMyReportSelected(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is IncidentReport report)
        {
            _selectedReport = report;
        }
    }

    private async void OnOpenSelectedReportMapClicked(object sender, EventArgs e)
    {
        if (_selectedReport == null)
        {
            await DisplayAlert(
                "Pilih Laporan",
                "Pilih salah satu laporan dulu dari riwayat.",
                "OK"
            );

            return;
        }

        await OpenGoogleMapsAsync(
            _selectedReport.Latitude,
            _selectedReport.Longitude,
            _selectedReport.Address
        );
    }

    private async Task OpenGoogleMapsAsync(double latitude, double longitude, string label)
    {
        try
        {
            string lat = latitude.ToString(CultureInfo.InvariantCulture);
            string lon = longitude.ToString(CultureInfo.InvariantCulture);

            string url = $"https://www.google.com/maps/search/?api=1&query={lat},{lon}";

            await Launcher.Default.OpenAsync(url);
        }
        catch (Exception ex)
        {
            await DisplayAlert(
                "Error Maps",
                $"Gagal membuka Google Maps.\n\n{ex.Message}",
                "OK"
            );
        }
    }

    private async void OnSubmitReportClicked(object sender, EventArgs e)
    {
        string name = NameEntry.Text?.Trim() ?? "";
        string phone = PhoneEntry.Text?.Trim() ?? "";
        string address = AddressEntry.Text?.Trim() ?? "";
        string detail = DetailEditor.Text?.Trim() ?? "";
        string priority = PriorityPicker.SelectedItem?.ToString() ?? "Sedang";

        if (string.IsNullOrWhiteSpace(name) ||
            string.IsNullOrWhiteSpace(phone) ||
            string.IsNullOrWhiteSpace(address) ||
            string.IsNullOrWhiteSpace(detail))
        {
            await DisplayAlert(
                "Data Belum Lengkap",
                "Nama, nomor HP, alamat, dan detail kejadian wajib diisi.",
                "OK"
            );

            return;
        }

        IncidentReport report = new IncidentReport
        {
            ReporterUsername = _username,
            ReporterName = name,
            Phone = phone,
            Address = address,
            Detail = detail,
            Priority = priority,
            Status = "Menunggu",
            Latitude = _currentLatitude,
            Longitude = _currentLongitude
        };

        await DatabaseService.SaveIncidentReportAsync(report);

        await DisplayAlert(
            "Laporan Terkirim",
            "Laporan berhasil dikirim, tersimpan ke database, dan bisa dibuka di Google Maps.",
            "OK"
        );

        NameEntry.Text = "";
        PhoneEntry.Text = "";
        AddressEntry.Text = "";
        DetailEditor.Text = "";
        PriorityPicker.SelectedIndex = 1;

        await LoadMyReportsAsync();
        await UpdateNearestStationAsync();
    }

    private void OnOpenNlpChatClicked(object sender, EventArgs e)
    {
        Application.Current!.Windows[0].Page = new ChatBotPage("User");
    }

    private void OnLogoutClicked(object sender, EventArgs e)
    {
        Application.Current!.Windows[0].Page = new AuthPage();
    }
}