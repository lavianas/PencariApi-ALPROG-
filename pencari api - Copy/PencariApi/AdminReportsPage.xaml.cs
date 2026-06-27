using PencariApi.Models;
using PencariApi.Services;
using System.Globalization;
using ClosedXML.Excel;

namespace PencariApi;

public partial class AdminReportsPage : ContentPage
{
    private IncidentReport? _selectedReport;
    private FireStation? _selectedStation;

    public AdminReportsPage()
    {
        InitializeComponent();

        _ = LoadDataAsync();
    }

    private async Task LoadDataAsync()
    {
        List<IncidentReport> reports = await DatabaseService.GetIncidentReportsAsync();
        List<FireStation> stations = await DatabaseService.GetFireStationsAsync();

        ReportsCollection.ItemsSource = reports;
        StationsCollection.ItemsSource = stations;
    }

    private async void OnRefreshClicked(object sender, EventArgs e)
    {
        await LoadDataAsync();
    }

    private async void OnDownloadExcelClicked(object sender, EventArgs e)
    {
        try
        {
            var reports = await DatabaseService.GetIncidentReportsAsync();

            if (reports == null || reports.Count == 0)
            {
                await DisplayAlert("Info", "Tidak ada data laporan untuk diunduh.", "OK");
                return;
            }

            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Laporan Insiden");

                // Headers
                worksheet.Cell(1, 1).Value = "No";
                worksheet.Cell(1, 2).Value = "Waktu Lapor";
                worksheet.Cell(1, 3).Value = "Nama Pelapor";
                worksheet.Cell(1, 4).Value = "Telepon";
                worksheet.Cell(1, 5).Value = "Alamat Kejadian";
                worksheet.Cell(1, 6).Value = "Detail";
                worksheet.Cell(1, 7).Value = "Prioritas";
                worksheet.Cell(1, 8).Value = "Status";
                worksheet.Cell(1, 9).Value = "Pos Terdekat";
                worksheet.Cell(1, 10).Value = "Jarak (Km)";
                worksheet.Cell(1, 11).Value = "Latitude";
                worksheet.Cell(1, 12).Value = "Longitude";

                // Format Headers
                var headerRow = worksheet.Range("A1:L1");
                headerRow.Style.Font.Bold = true;
                headerRow.Style.Fill.BackgroundColor = XLColor.LightGray;
                headerRow.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                // Data Rows
                int row = 2;
                int no = 1;
                foreach (var report in reports)
                {
                    worksheet.Cell(row, 1).Value = no++;
                    worksheet.Cell(row, 2).Value = report.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss");
                    worksheet.Cell(row, 3).Value = report.ReporterName;
                    worksheet.Cell(row, 4).Value = report.Phone;
                    worksheet.Cell(row, 5).Value = report.Address;
                    worksheet.Cell(row, 6).Value = report.Detail;
                    worksheet.Cell(row, 7).Value = report.Priority;
                    worksheet.Cell(row, 8).Value = report.Status;
                    worksheet.Cell(row, 9).Value = report.NearestStationName;
                    worksheet.Cell(row, 10).Value = report.DistanceKm;
                    worksheet.Cell(row, 11).Value = report.Latitude;
                    worksheet.Cell(row, 12).Value = report.Longitude;
                    row++;
                }

                worksheet.Columns().AdjustToContents();

                string fileName = $"Laporan_Insiden_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                string filePath = Path.Combine(documentsPath, fileName);

                workbook.SaveAs(filePath);

                await DisplayAlert("Berhasil", $"Data Excel berhasil disimpan di:\n{filePath}", "Buka File");

                await Launcher.Default.OpenAsync(new OpenFileRequest("Buka Laporan Excel", new ReadOnlyFile(filePath)));
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Gagal membuat file Excel: {ex.Message}", "OK");
        }
    }

    private void OnReportSelected(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is IncidentReport report)
        {
            _selectedReport = report;
        }
    }

    private void OnStationSelected(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is FireStation station)
        {
            _selectedStation = station;
        }
    }

    private async void OnOpenReportMapClicked(object sender, EventArgs e)
    {
        if (_selectedReport == null)
        {
            await DisplayAlert(
                "Pilih Laporan",
                "Klik salah satu laporan dulu, lalu tekan Maps Laporan.",
                "OK"
            );

            return;
        }

        await OpenGoogleMapsAsync(
            _selectedReport.Latitude,
            _selectedReport.Longitude
        );
    }

    private async void OnOpenStationMapClicked(object sender, EventArgs e)
    {
        if (_selectedStation == null)
        {
            await DisplayAlert(
                "Pilih Pos Damkar",
                "Klik salah satu pos damkar dulu, lalu tekan Maps Damkar.",
                "OK"
            );

            return;
        }

        await OpenGoogleMapsAsync(
            _selectedStation.Latitude,
            _selectedStation.Longitude
        );
    }

    private async Task OpenGoogleMapsAsync(double latitude, double longitude)
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

    private void OnBackDashboardClicked(object sender, EventArgs e)
    {
        Application.Current!.Windows[0].Page = new MainPage();
    }

    private void OnOpenAdminMapPageClicked(object sender, EventArgs e)
    {
        if (_selectedReport != null)
        {
            Application.Current!.Windows[0].Page = new AdminMapPage(_selectedReport);
        }
        else
        {
            Application.Current!.Windows[0].Page = new AdminMapPage();
        }
    }
}