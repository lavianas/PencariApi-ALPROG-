using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Views;
using PencariApi.Models;
using PencariApi.Services;

namespace PencariApi;

public partial class AuthPage : ContentPage
{
    private const int RequiredRegisterSampleCount = 15;

    private string _selectedRole = "Admin";
    private bool _isLoginMode = true;

    private readonly List<string> _tempRegisterPhotoPaths = new();

    private bool _registerCameraReady = false;
    private bool _isCapturingRegisterSamples = false;
    private TaskCompletionSource<string?>? _registerCaptureTcs;

    public AuthPage()
    {
        InitializeComponent();

        UpdateRoleUI();
        UpdateModeUI();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        StopRegisterCameraPreview();
    }

    private void OnAdminClicked(object sender, EventArgs e)
    {
        _selectedRole = "Admin";
        UpdateRoleUI();
        UpdateModeUI();
    }

    private void OnUserClicked(object sender, EventArgs e)
    {
        _selectedRole = "User";
        UpdateRoleUI();
        UpdateModeUI();
    }

    private async void OnOpenRegisterClicked(object sender, EventArgs e)
    {
        _isLoginMode = false;
        ClearMessages();
        UpdateModeUI();

        await StartRegisterCameraPreviewAsync();
    }

    private void OnOpenLoginClicked(object sender, EventArgs e)
    {
        _isLoginMode = true;
        ClearMessages();
        UpdateModeUI();
        StopRegisterCameraPreview();
    }

    private void UpdateRoleUI()
    {
        if (_selectedRole == "Admin")
        {
            BtnAdmin.BackgroundColor = Color.FromArgb("#FFF4EB");
            BtnAdmin.TextColor = Color.FromArgb("#FF6A00");
            BtnAdmin.BorderColor = Color.FromArgb("#FFB067");

            BtnUser.BackgroundColor = Colors.White;
            BtnUser.TextColor = Color.FromArgb("#324B72");
            BtnUser.BorderColor = Color.FromArgb("#D7DFEA");
        }
        else
        {
            BtnUser.BackgroundColor = Color.FromArgb("#FFF4EB");
            BtnUser.TextColor = Color.FromArgb("#FF6A00");
            BtnUser.BorderColor = Color.FromArgb("#FFB067");

            BtnAdmin.BackgroundColor = Colors.White;
            BtnAdmin.TextColor = Color.FromArgb("#324B72");
            BtnAdmin.BorderColor = Color.FromArgb("#D7DFEA");
        }
    }

    private void UpdateModeUI()
    {
        LoginSection.IsVisible = _isLoginMode;
        RegisterSection.IsVisible = !_isLoginMode;

        if (_isLoginMode)
        {
            TitleLabel.Text = _selectedRole == "Admin" ? "Login Admin" : "Login User";

            SubtitleLabel.Text = _selectedRole == "Admin"
                ? "Masuk untuk mengakses dashboard dan mengelola sistem."
                : "Masuk untuk melihat informasi rute dan status insiden.";

            LoginButton.Text = _selectedRole == "Admin"
                ? "Masuk sebagai Admin"
                : "Masuk sebagai User";
        }
        else
        {
            TitleLabel.Text = "Registrasi Akun";

            SubtitleLabel.Text =
                "Daftarkan akun untuk mengakses sistem dengan 15 sampel wajah.";

            RegisterButton.Text = _selectedRole == "Admin"
                ? "Daftar sebagai Admin"
                : "Daftar sebagai User";
        }
    }

    private void ClearMessages()
    {
        LoginErrorLabel.Text = "";
        RegisterErrorLabel.Text = "";
    }

    private async void OnForgotPasswordClicked(object sender, EventArgs e)
    {
        await DisplayAlert(
            "Info",
            "Untuk versi lokal ini, fitur lupa password belum dihubungkan ke email.",
            "OK"
        );
    }

    private async Task StartRegisterCameraPreviewAsync()
    {
        if (_isLoginMode)
            return;

        try
        {
            RegisterPhotoStatusLabel.Text = "Meminta izin kamera...";
            RegisterPhotoStatusLabel.TextColor = Color.FromArgb("#4B5563");

            PermissionStatus permission =
                await Permissions.CheckStatusAsync<Permissions.Camera>();

            if (permission != PermissionStatus.Granted)
                permission = await Permissions.RequestAsync<Permissions.Camera>();

            if (permission != PermissionStatus.Granted)
            {
                _registerCameraReady = false;

                RegisterPhotoStatusLabel.Text =
                    "Izin kamera ditolak. Kamera registrasi tidak bisa dibuka.";

                RegisterPhotoStatusLabel.TextColor = Color.FromArgb("#EF4444");
                return;
            }

            RegisterPhotoStatusLabel.Text = "Mencari kamera perangkat...";

            var cameras =
                await RegisterCamera.GetAvailableCameras(CancellationToken.None);

            if (cameras == null || cameras.Count == 0)
            {
                _registerCameraReady = false;
                RegisterPhotoStatusLabel.Text = "Kamera tidak ditemukan.";
                RegisterPhotoStatusLabel.TextColor = Color.FromArgb("#EF4444");
                return;
            }

            RegisterCamera.SelectedCamera = cameras.First();

            RegisterPhotoStatusLabel.Text = "Menyalakan kamera live registrasi...";

            await RegisterCamera.StartCameraPreview(CancellationToken.None);

            _registerCameraReady = true;

            RegisterPhotoStatusLabel.Text =
                "Kamera live aktif. Arahkan wajah ke kamera, lalu ambil 15 sampel.";

            RegisterPhotoStatusLabel.TextColor = Color.FromArgb("#10B981");
        }
        catch (Exception ex)
        {
            _registerCameraReady = false;

            RegisterPhotoStatusLabel.Text = "Kamera registrasi gagal aktif.";
            RegisterPhotoStatusLabel.TextColor = Color.FromArgb("#EF4444");

            await DisplayAlert("Error Kamera", ex.Message, "OK");
        }
    }

    private void StopRegisterCameraPreview()
    {
        try
        {
            RegisterCamera.StopCameraPreview();
        }
        catch
        {
        }

        _registerCameraReady = false;
    }

    private async void OnTakeRegisterPhotoClicked(object sender, EventArgs e)
    {
        if (_isCapturingRegisterSamples)
            return;

        if (!_registerCameraReady)
            await StartRegisterCameraPreviewAsync();

        if (!_registerCameraReady)
        {
            await DisplayAlert(
                "Kamera Belum Siap",
                "Kamera belum aktif. Pastikan izin kamera sudah diberikan dan kamera tidak sedang dipakai aplikasi lain.",
                "OK"
            );

            return;
        }

        try
        {
            _isCapturingRegisterSamples = true;

            TakeRegisterPhotoButton.IsEnabled = false;
            RegisterButton.IsEnabled = false;
            RegisterErrorLabel.Text = "";

            ClearTempRegisterSamples();

            for (int i = 1; i <= RequiredRegisterSampleCount; i++)
            {
                RegisterPhotoStatusLabel.Text =
                    $"Mengambil sampel wajah {i}/{RequiredRegisterSampleCount}... tetap hadap kamera.";

                RegisterPhotoStatusLabel.TextColor = Color.FromArgb("#4B5563");

                string? samplePath = await CaptureRegisterSampleAsync();

                if (string.IsNullOrWhiteSpace(samplePath) || !File.Exists(samplePath))
                {
                    RegisterPhotoStatusLabel.Text =
                        $"Gagal mengambil sampel ke-{i}. Coba ulangi.";

                    RegisterPhotoStatusLabel.TextColor = Color.FromArgb("#EF4444");
                    return;
                }

                _tempRegisterPhotoPaths.Add(samplePath);

                await Task.Delay(250);
            }

            RegisterPhotoStatusLabel.Text =
                $"Berhasil mengambil {RequiredRegisterSampleCount} sampel wajah. Klik daftar untuk menyimpan akun.";

            RegisterPhotoStatusLabel.TextColor = Color.FromArgb("#12B76A");
        }
        catch (Exception ex)
        {
            RegisterPhotoStatusLabel.Text = "Gagal mengambil sampel wajah.";
            RegisterPhotoStatusLabel.TextColor = Color.FromArgb("#EF4444");

            await DisplayAlert("Error", ex.Message, "OK");
        }
        finally
        {
            _isCapturingRegisterSamples = false;

            TakeRegisterPhotoButton.IsEnabled = true;
            RegisterButton.IsEnabled = true;
        }
    }

    private async Task<string?> CaptureRegisterSampleAsync()
    {
        _registerCaptureTcs = new TaskCompletionSource<string?>();

        await RegisterCamera.CaptureImage(CancellationToken.None);

        Task completedTask = await Task.WhenAny(
            _registerCaptureTcs.Task,
            Task.Delay(TimeSpan.FromSeconds(8))
        );

        if (completedTask != _registerCaptureTcs.Task)
        {
            _registerCaptureTcs = null;
            return null;
        }

        string? result = await _registerCaptureTcs.Task;

        _registerCaptureTcs = null;

        return result;
    }

    private async void OnRegisterCameraMediaCaptured(
    object sender,
    MediaCapturedEventArgs e)
    {
        TaskCompletionSource<string?>? tcs = _registerCaptureTcs;

        if (tcs == null)
            return;

        try
        {
            if (e.Media == null)
            {
                tcs.TrySetResult(null);
                return;
            }

            string tempPath = Path.Combine(
                FileSystem.CacheDirectory,
                $"temp_register_face_{Guid.NewGuid()}.jpg"
            );

            if (e.Media.CanSeek)
                e.Media.Position = 0;

            using (FileStream localStream = File.Create(tempPath))
            {
                await e.Media.CopyToAsync(localStream);
                await localStream.FlushAsync();
            }

            if (!File.Exists(tempPath) || new FileInfo(tempPath).Length <= 100)
            {
                tcs.TrySetResult(null);
                return;
            }

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                RegisterPhotoPreview.Source = ImageSource.FromFile(tempPath);
            });

            tcs.TrySetResult(tempPath);
        }
        catch
        {
            tcs.TrySetResult(null);
        }
    }
    private async void OnRegisterCameraMediaCaptureFailed(
        object sender,
        MediaCaptureFailedEventArgs e)
    {
        _registerCaptureTcs?.TrySetResult(null);

        if (!_isCapturingRegisterSamples)
        {
            RegisterPhotoStatusLabel.Text = "Capture kamera registrasi gagal.";
            RegisterPhotoStatusLabel.TextColor = Color.FromArgb("#EF4444");

            await DisplayAlert("Capture Gagal", e.FailureReason, "OK");
        }
    }

    private void ClearTempRegisterSamples()
    {
        foreach (string path in _tempRegisterPhotoPaths)
        {
            try
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
            catch
            {
            }
        }

        _tempRegisterPhotoPaths.Clear();
    }

    private async void OnRegisterClicked(object sender, EventArgs e)
    {
        string fullName = RegisterFullNameEntry.Text?.Trim() ?? "";
        string email = RegisterEmailEntry.Text?.Trim() ?? "";
        string username = RegisterUsernameEntry.Text?.Trim() ?? "";
        string password = RegisterPasswordEntry.Text?.Trim() ?? "";
        string confirmPassword = RegisterConfirmPasswordEntry.Text?.Trim() ?? "";

        RegisterErrorLabel.Text = "";

        if (string.IsNullOrWhiteSpace(fullName) ||
            string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(username) ||
            string.IsNullOrWhiteSpace(password) ||
            string.IsNullOrWhiteSpace(confirmPassword))
        {
            RegisterErrorLabel.Text = "Semua field registrasi wajib diisi.";
            return;
        }

        if (password != confirmPassword)
        {
            RegisterErrorLabel.Text = "Konfirmasi password tidak sama.";
            return;
        }

        if (!TermsCheckBox.IsChecked)
        {
            RegisterErrorLabel.Text =
                "Kamu harus menyetujui syarat dan ketentuan.";

            return;
        }

        if (_tempRegisterPhotoPaths.Count < RequiredRegisterSampleCount)
        {
            RegisterErrorLabel.Text =
                $"Sampel wajah belum lengkap. Ambil {RequiredRegisterSampleCount} sampel foto terlebih dahulu.";

            return;
        }

        bool usernameExists =
            await DatabaseService.UsernameExistsAsync(username);

        if (usernameExists)
        {
            RegisterErrorLabel.Text = "Username sudah terdaftar.";
            return;
        }

        try
        {
            string faceSamplesDirectory = Path.Combine(
                FileSystem.AppDataDirectory,
                "face_samples",
                username
            );

            if (Directory.Exists(faceSamplesDirectory))
                Directory.Delete(faceSamplesDirectory, true);

            Directory.CreateDirectory(faceSamplesDirectory);

            string mainFacePath = "";

            for (int i = 0; i < _tempRegisterPhotoPaths.Count; i++)
            {
                string destinationPath = Path.Combine(
                    faceSamplesDirectory,
                    $"sample_{i + 1:00}.png"
                );

                File.Copy(_tempRegisterPhotoPaths[i], destinationPath, true);

                if (i == 0)
                    mainFacePath = destinationPath;
            }

            UserAccount newUser = new UserAccount
            {
                FullName = fullName,
                Email = email,
                Username = username,
                Password = password,
                Role = _selectedRole,
                FaceImagePath = mainFacePath,
                CreatedAt = DateTime.Now
            };

            await DatabaseService.SaveUserAsync(newUser);

            await DisplayAlert(
                "Registrasi Berhasil",
                $"Akun {_selectedRole} berhasil dibuat.\n15 sampel wajah berhasil disimpan.",
                "OK"
            );

            _isLoginMode = true;
            UpdateModeUI();
            StopRegisterCameraPreview();

            LoginUsernameEntry.Text = username;
            LoginPasswordEntry.Text = password;

            RegisterFullNameEntry.Text = "";
            RegisterEmailEntry.Text = "";
            RegisterUsernameEntry.Text = "";
            RegisterPasswordEntry.Text = "";
            RegisterConfirmPasswordEntry.Text = "";
            TermsCheckBox.IsChecked = false;
            RegisterPhotoPreview.Source = null;

            RegisterPhotoStatusLabel.Text =
                "Kamera registrasi sedang disiapkan. Belum ada sampel wajah tersimpan.";

            RegisterPhotoStatusLabel.TextColor = Color.FromArgb("#6E7F99");

            ClearTempRegisterSamples();
        }
        catch (Exception ex)
        {
            RegisterErrorLabel.Text = ex.Message;
        }
    }

    private async void OnLoginClicked(object sender, EventArgs e)
    {
        string username = LoginUsernameEntry.Text?.Trim() ?? "";
        string password = LoginPasswordEntry.Text?.Trim() ?? "";

        LoginErrorLabel.Text = "";

        if (string.IsNullOrWhiteSpace(username) ||
            string.IsNullOrWhiteSpace(password))
        {
            LoginErrorLabel.Text = "Username dan password wajib diisi.";
            return;
        }

        UserAccount? user = await DatabaseService.GetUserAsync(username);

        if (user == null)
        {
            LoginErrorLabel.Text = "Akun tidak ditemukan. Silakan registrasi dulu.";
            return;
        }

        if (user.Password != password)
        {
            LoginErrorLabel.Text = "Password salah.";
            return;
        }

        if (!user.Role.Equals(_selectedRole, StringComparison.OrdinalIgnoreCase))
        {
            LoginErrorLabel.Text =
                $"Role akun ini adalah {user.Role}, bukan {_selectedRole}.";

            return;
        }

        if (string.IsNullOrWhiteSpace(user.FaceImagePath) ||
            !File.Exists(user.FaceImagePath))
        {
            LoginErrorLabel.Text =
                "Foto wajah akun tidak ditemukan. Silakan registrasi ulang.";

            return;
        }

        Preferences.Set("current_username", user.Username);

        Application.Current!.Windows[0].Page =
            new FaceVerificationPage(user.Username, user.Role);
    }
}