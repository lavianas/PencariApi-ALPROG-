using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Views;
using PencariApi.Models;
using PencariApi.Services;

namespace PencariApi;

public partial class FaceVerificationPage : ContentPage
{
    private const double VerificationThreshold = 45.0;

    private readonly string _username;
    private readonly string _role;

    private UserAccount? _currentUser;

    private bool _cameraReady = false;
    private bool _isVerifying = false;
    private bool _isNavigating = false;
    private bool _hasStartedCamera = false;

    public FaceVerificationPage(string username, string role)
    {
        InitializeComponent();

        _username = username;
        _role = role;

        LabelRole.Text = $"User: {_username} | Role: {_role}";
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (_hasStartedCamera)
            return;

        _hasStartedCamera = true;

        await Task.Delay(800);
        await LoadUserAndStartCameraAsync();
    }

    private async Task LoadUserAndStartCameraAsync()
    {
        try
        {
            ShowCameraLoading(true, "Mengambil data user...");

            _currentUser = await DatabaseService.GetUserAsync(_username);

            if (_currentUser == null)
            {
                ShowCameraLoading(false);

                await DisplayAlert(
                    "Data User Tidak Ada",
                    "Data user tidak ditemukan. Silakan login ulang.",
                    "OK"
                );

                await NavigateToAuthPageAsync();
                return;
            }

            await StartCameraPreviewAsync();
        }
        catch (Exception ex)
        {
            ShowCameraLoading(false);

            StatusLabel.Text = "Gagal membuka halaman verifikasi.";
            StatusLabel.TextColor = Color.FromArgb("#EF4444");

            await DisplayAlert("Error", ex.Message, "OK");
        }
    }

    private async Task StartCameraPreviewAsync()
    {
        try
        {
            _cameraReady = false;

            VerifyButton.IsEnabled = false;
            RefreshCameraButton.IsEnabled = false;
            CancelButton.IsEnabled = true;

            ShowCameraLoading(true, "Meminta izin kamera...");
            StatusLabel.Text = "Meminta izin kamera...";
            StatusLabel.TextColor = Color.FromArgb("#475467");

            PermissionStatus permission =
                await Permissions.CheckStatusAsync<Permissions.Camera>();

            if (permission != PermissionStatus.Granted)
                permission = await Permissions.RequestAsync<Permissions.Camera>();

            if (permission != PermissionStatus.Granted)
            {
                ShowCameraLoading(false);

                StatusLabel.Text = "Izin kamera ditolak.";
                StatusLabel.TextColor = Color.FromArgb("#EF4444");

                VerifyButton.IsEnabled = false;
                RefreshCameraButton.IsEnabled = true;

                await DisplayAlert(
                    "Izin Kamera Ditolak",
                    "Aplikasi membutuhkan izin kamera untuk verifikasi wajah.",
                    "OK"
                );

                return;
            }

            ShowCameraLoading(true, "Mencari perangkat kamera...");
            StatusLabel.Text = "Mencari perangkat kamera...";

            var cameras = await CameraView.GetAvailableCameras(CancellationToken.None);

            if (cameras == null || cameras.Count == 0)
            {
                ShowCameraLoading(false);

                StatusLabel.Text = "Kamera tidak ditemukan.";
                StatusLabel.TextColor = Color.FromArgb("#EF4444");

                VerifyButton.IsEnabled = false;
                RefreshCameraButton.IsEnabled = true;

                await DisplayAlert(
                    "Kamera Tidak Ditemukan",
                    "Pastikan kamera laptop/PC aktif dan tidak sedang dipakai aplikasi lain.",
                    "OK"
                );

                return;
            }

            CameraView.SelectedCamera = cameras.First();

            try
            {
                CameraView.StopCameraPreview();
            }
            catch
            {
            }

            await Task.Delay(500);

            ShowCameraLoading(true, "Menyalakan live preview kamera...");
            StatusLabel.Text = "Menyalakan live preview kamera...";

            await CameraView.StartCameraPreview(CancellationToken.None);

            await Task.Delay(1000);

            _cameraReady = true;

            ShowCameraLoading(false);

            VerifyButton.IsEnabled = true;
            RefreshCameraButton.IsEnabled = true;
            CancelButton.IsEnabled = true;

            StatusLabel.Text = "Kamera aktif. Arahkan wajah ke kamera.";
            StatusLabel.TextColor = Color.FromArgb("#12B76A");
        }
        catch (Exception ex)
        {
            _cameraReady = false;

            ShowCameraLoading(false);

            VerifyButton.IsEnabled = false;
            RefreshCameraButton.IsEnabled = true;
            CancelButton.IsEnabled = true;

            StatusLabel.Text = "Kamera gagal aktif.";
            StatusLabel.TextColor = Color.FromArgb("#EF4444");

            await DisplayAlert(
                "Error Kamera",
                $"Kamera gagal aktif.\n\n{ex.Message}",
                "OK"
            );
        }
    }

    private async void OnRefreshCameraClicked(object sender, EventArgs e)
    {
        await RestartCameraAsync();
    }

    private async Task RestartCameraAsync()
    {
        try
        {
            _cameraReady = false;

            VerifyButton.IsEnabled = false;
            RefreshCameraButton.IsEnabled = false;

            ShowCameraLoading(true, "Restart kamera...");

            try
            {
                CameraView.StopCameraPreview();
            }
            catch
            {
            }

            await Task.Delay(700);

            await StartCameraPreviewAsync();
        }
        catch (Exception ex)
        {
            ShowCameraLoading(false);

            VerifyButton.IsEnabled = false;
            RefreshCameraButton.IsEnabled = true;

            await DisplayAlert(
                "Refresh Kamera Gagal",
                ex.Message,
                "OK"
            );
        }
    }

    private async void OnVerifyClicked(object sender, EventArgs e)
    {
        await VerifyFromLiveCameraAsync();
    }

    private async Task VerifyFromLiveCameraAsync()
    {
        if (_isVerifying || _isNavigating)
            return;

        if (_currentUser == null)
        {
            await DisplayAlert(
                "Data User Error",
                "Data user tidak ditemukan. Silakan login ulang.",
                "OK"
            );

            await NavigateToAuthPageAsync();
            return;
        }

        if (!_cameraReady)
        {
            await DisplayAlert(
                "Kamera Belum Siap",
                "Kamera belum aktif. Klik Refresh Kamera atau kembali ke login.",
                "OK"
            );

            return;
        }

        List<string> registeredFaceSamples = GetRegisteredFaceSamplePaths(_currentUser);

        if (registeredFaceSamples.Count == 0)
        {
            await DisplayAlert(
                "Sampel Wajah Tidak Ada",
                "File sampel wajah tidak ditemukan. Silakan registrasi ulang akun.",
                "OK"
            );

            await NavigateToAuthPageAsync();
            return;
        }

        try
        {
            _isVerifying = true;

            VerifyButton.IsEnabled = false;
            RefreshCameraButton.IsEnabled = false;
            CancelButton.IsEnabled = false;

            StatusLabel.Text = "Mengambil gambar wajah...";
            StatusLabel.TextColor = Color.FromArgb("#475467");

            await CameraView.CaptureImage(CancellationToken.None);
        }
        catch (Exception ex)
        {
            ResetButton();

            StatusLabel.Text = "Capture wajah gagal.";
            StatusLabel.TextColor = Color.FromArgb("#EF4444");

            await DisplayAlert("Capture Gagal", ex.Message, "OK");
        }
    }

    private async void OnCameraMediaCaptured(object sender, MediaCapturedEventArgs e)
    {
        try
        {
            if (_isNavigating)
                return;

            if (_currentUser == null)
            {
                ResetButton();
                await NavigateToAuthPageAsync();
                return;
            }

            if (e.Media == null)
            {
                ResetButton();

                StatusLabel.Text = "Gagal mengambil gambar.";
                StatusLabel.TextColor = Color.FromArgb("#EF4444");

                return;
            }

            StatusLabel.Text = "Menyimpan hasil kamera...";
            StatusLabel.TextColor = Color.FromArgb("#475467");

            string capturedPath = Path.Combine(
                FileSystem.CacheDirectory,
                $"verify_face_{Guid.NewGuid()}.jpg"
            );

            if (e.Media.CanSeek)
                e.Media.Position = 0;

            using (FileStream fileStream = File.Create(capturedPath))
            {
                await e.Media.CopyToAsync(fileStream);
                await fileStream.FlushAsync();
            }

            if (!File.Exists(capturedPath) || new FileInfo(capturedPath).Length <= 100)
            {
                ResetButton();

                StatusLabel.Text = "Foto hasil kamera rusak atau kosong.";
                StatusLabel.TextColor = Color.FromArgb("#EF4444");

                await DisplayAlert(
                    "Foto Tidak Valid",
                    "Foto hasil kamera kosong atau gagal disimpan. Klik Refresh Kamera lalu coba lagi.",
                    "OK"
                );

                return;
            }

            StatusLabel.Text = "Memeriksa wajah...";
            StatusLabel.TextColor = Color.FromArgb("#475467");

            List<string> registeredFaceSamples = GetRegisteredFaceSamplePaths(_currentUser);

            int validSampleCount = OpenCvFaceService.CountValidFaceSamples(registeredFaceSamples);

            if (validSampleCount == 0)
            {
                ResetButton();

                StatusLabel.Text = "Sampel wajah registrasi tidak valid.";
                StatusLabel.TextColor = Color.FromArgb("#EF4444");

                await DisplayAlert(
                    "Sampel Wajah Tidak Valid",
                    "Sistem tidak menemukan sampel wajah yang valid.\n\nSilakan registrasi akun baru dan ambil 15 sampel wajah dengan posisi wajah di tengah kamera.",
                    "OK"
                );

                await NavigateToAuthPageAsync();
                return;
            }

            bool faceDetected = OpenCvFaceService.HasPossibleFace(capturedPath);

            if (!faceDetected)
            {
                ResetButton();

                StatusLabel.Text = "Tidak ada wajah terdeteksi.";
                StatusLabel.TextColor = Color.FromArgb("#EF4444");

                await DisplayAlert(
                    "WAJAH TIDAK TERDETEKSI",
                    "Arahkan wajah ke tengah kamera. Jangan terlalu jauh, terlalu gelap, atau tertutup.",
                    "OK"
                );

                return;
            }

            double similarity = OpenCvFaceService.CalculateBestSimilarity(
                capturedPath,
                registeredFaceSamples
            );

            if (similarity >= VerificationThreshold)
            {
                _isNavigating = true;

                StatusLabel.Text =
                    $"Akses diterima. Sampel valid: {validSampleCount} | Kemiripan: {similarity:F1}%";

                StatusLabel.TextColor = Color.FromArgb("#12B76A");

                await DisplayAlert(
                    "AKSES DITERIMA",
                    $"Identitas valid.\nSampel valid: {validSampleCount}\nKemiripan: {similarity:F1}%\n\nMasuk dashboard...",
                    "OK"
                );

                await NavigateToDashboardAsync();
            }
            else
            {
                ResetButton();

                StatusLabel.Text =
                    $"Akses ditolak. Sampel valid: {validSampleCount} | Kemiripan: {similarity:F1}%";

                StatusLabel.TextColor = Color.FromArgb("#EF4444");

                await DisplayAlert(
                    "AKSES DITOLAK",
                    $"Wajah tidak cocok.\nSampel valid: {validSampleCount}\nKemiripan hanya: {similarity:F1}%",
                    "OK"
                );
            }
        }
        catch (Exception ex)
        {
            ResetButton();

            StatusLabel.Text = "Verifikasi gagal.";
            StatusLabel.TextColor = Color.FromArgb("#EF4444");

            await DisplayAlert("Error Verifikasi", ex.Message, "OK");
        }
    }

    private async void OnCameraMediaCaptureFailed(object sender, MediaCaptureFailedEventArgs e)
    {
        ResetButton();

        StatusLabel.Text = "Gagal mengambil gambar kamera.";
        StatusLabel.TextColor = Color.FromArgb("#EF4444");

        await DisplayAlert(
            "Capture Gagal",
            e.FailureReason,
            "OK"
        );
    }

    private List<string> GetRegisteredFaceSamplePaths(UserAccount user)
    {
        List<string> samples = new List<string>();

        string faceSamplesDirectory = Path.Combine(
            FileSystem.AppDataDirectory,
            "face_samples",
            user.Username
        );

        if (Directory.Exists(faceSamplesDirectory))
        {
            samples.AddRange(Directory.GetFiles(faceSamplesDirectory, "*.jpg"));
            samples.AddRange(Directory.GetFiles(faceSamplesDirectory, "*.jpeg"));
            samples.AddRange(Directory.GetFiles(faceSamplesDirectory, "*.png"));

            samples = samples
                .Where(path => File.Exists(path))
                .Where(path => new FileInfo(path).Length > 100)
                .OrderBy(path => path)
                .ToList();

            if (samples.Count > 0)
                return samples;
        }

        if (!string.IsNullOrWhiteSpace(user.FaceImagePath) &&
            File.Exists(user.FaceImagePath) &&
            new FileInfo(user.FaceImagePath).Length > 100)
        {
            samples.Add(user.FaceImagePath);
        }

        return samples;
    }

    private async Task NavigateToDashboardAsync()
    {
        try
        {
            StopCamera();

            await Task.Delay(300);

            Preferences.Set("current_username", _username);

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                if (_role.Equals("Admin", StringComparison.OrdinalIgnoreCase))
                    Application.Current!.Windows[0].Page = new MainPage();
                else
                    Application.Current!.Windows[0].Page = new UserDashboardPage();
            });
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error Navigasi", ex.Message, "OK");
        }
    }

    private async Task NavigateToAuthPageAsync()
    {
        try
        {
            StopCamera();

            await Task.Delay(300);

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                Application.Current!.Windows[0].Page = new AuthPage();
            });
        }
        catch
        {
        }
    }

    private async void OnCancelClicked(object sender, EventArgs e)
    {
        await NavigateToAuthPageAsync();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        StopCamera();
    }

    private void StopCamera()
    {
        try
        {
            CameraView.StopCameraPreview();
        }
        catch
        {
        }

        _cameraReady = false;
    }

    private void ResetButton()
    {
        _isVerifying = false;

        VerifyButton.IsEnabled = true;
        RefreshCameraButton.IsEnabled = true;
        CancelButton.IsEnabled = true;
    }

    private void ShowCameraLoading(bool isLoading, string message = "Memuat kamera...")
    {
        CameraLoadingOverlay.IsVisible = isLoading;
        CameraLoadingIndicator.IsRunning = isLoading;
        CameraLoadingLabel.Text = message;
    }
}