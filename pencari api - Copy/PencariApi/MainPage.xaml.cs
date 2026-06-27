namespace PencariApi;

public partial class MainPage : ContentPage
{
    private IDispatcherTimer? _refreshTimer;
    private readonly Random _random = new Random();
    private int _tickCount = 0;

    // Data lokasi kebakaran Surabaya (dummy realistis)
    private readonly string[][] _fireLocations = new[]
    {
        new[] { "Jl. Tunjungan", "Surabaya Pusat" },
        new[] { "Jl. Pemuda", "Genteng, Surabaya" },
        new[] { "Jl. Raya Darmo", "Wonokromo, Surabaya" },
        new[] { "Jl. Mayjen Sungkono", "Dukuh Pakis, Surabaya" },
        new[] { "Jl. Raya Gubeng", "Gubeng, Surabaya" },
        new[] { "Jl. Basuki Rahmat", "Tegalsari, Surabaya" },
        new[] { "Jl. Kertajaya", "Gubeng, Surabaya" },
        new[] { "Jl. Ahmad Yani", "Gayungan, Surabaya" },
        new[] { "Jl. Rungkut Industri", "Rungkut, Surabaya" },
        new[] { "Jl. Kenjeran", "Kenjeran, Surabaya" },
        new[] { "Jl. Rajawali", "Krembangan, Surabaya" },
        new[] { "Jl. Perak Timur", "Pabean Cantikan, Surabaya" }
    };

    private readonly string[] _statuses = { "Darurat", "Aktif", "Diproses", "Siaga" };
    private readonly string[] _priorities = { "Tinggi", "Sedang", "Rendah" };

    public MainPage()
    {
        InitializeComponent();

        // Load data pertama kali
        RefreshDashboardData();

        // Setup timer 1 menit
        _refreshTimer = Dispatcher.CreateTimer();
        _refreshTimer.Interval = TimeSpan.FromMinutes(1);
        _refreshTimer.Tick += OnTimerTick;
        _refreshTimer.Start();
    }

    private void OnTimerTick(object? sender, EventArgs e)
    {
        _tickCount++;
        RefreshDashboardData();
    }

    private void RefreshDashboardData()
    {
        // === STAT CARDS ===

        // Insiden Aktif: 3 - 12
        int incidentCount = _random.Next(3, 13);
        IncidentCountLabel.Text = incidentCount.ToString();

        // Kamera Online: 20 - 30 dari total 30
        int totalCameras = 30;
        int onlineCameras = _random.Next(20, totalCameras + 1);
        int percent = (int)Math.Round((double)onlineCameras / totalCameras * 100);
        CameraOnlineLabel.Text = onlineCameras.ToString();
        CameraTotalLabel.Text = $"/ {totalCameras}";
        CameraPercentLabel.Text = $"{percent}% terhubung";

        // Estimasi Respon: 4 - 15 menit
        int etaMinutes = _random.Next(4, 16);
        string etaFormatted = etaMinutes.ToString("D2");
        EtaMinuteLabel.Text = etaFormatted;

        // === PETA RUTE OPTIMAL ===

        int locationIndex = _random.Next(_fireLocations.Length);
        string street = _fireLocations[locationIndex][0];
        string area = _fireLocations[locationIndex][1];

        MapLocationStreetLabel.Text = street;
        MapLocationAreaLabel.Text = area;
        MapEtaLabel.Text = $"ETA {etaFormatted} menit";

        // === DETAIL INSIDEN ===

        DetailLocationLabel.Text = $"{street}, {area}";

        int statusIdx = _random.Next(_statuses.Length);
        string status = _statuses[statusIdx];
        DetailStatusLabel.Text = status;

        // Warna status dinamis
        switch (status)
        {
            case "Darurat":
                DetailStatusLabel.TextColor = Color.FromArgb("#FF3B1F");
                break;
            case "Aktif":
                DetailStatusLabel.TextColor = Color.FromArgb("#F59E0B");
                break;
            case "Diproses":
                DetailStatusLabel.TextColor = Color.FromArgb("#2563EB");
                break;
            default:
                DetailStatusLabel.TextColor = Color.FromArgb("#12B76A");
                break;
        }

        int priorityIdx = _random.Next(_priorities.Length);
        string priority = _priorities[priorityIdx];
        DetailPriorityLabel.Text = priority;

        switch (priority)
        {
            case "Tinggi":
                DetailPriorityLabel.TextColor = Color.FromArgb("#FF3B1F");
                break;
            case "Sedang":
                DetailPriorityLabel.TextColor = Color.FromArgb("#F59E0B");
                break;
            default:
                DetailPriorityLabel.TextColor = Color.FromArgb("#12B76A");
                break;
        }

        DetailEtaLabel.Text = $"{etaFormatted} menit";

        // === MONITORING KAMERA IoT ===

        bool cam01Online = _random.Next(100) < 90; // 90% chance online
        Camera01StatusLabel.Text = cam01Online ? "Online" : "Offline";
        Camera01StatusDot.Color = cam01Online
            ? Color.FromArgb("#20C997")
            : Color.FromArgb("#EF4444");

        bool cam02Online = _random.Next(100) < 85; // 85% chance online
        Camera02StatusLabel.Text = cam02Online ? "Online" : "Offline";
        Camera02StatusDot.Color = cam02Online
            ? Color.FromArgb("#20C997")
            : Color.FromArgb("#EF4444");

        // === ANALITIK ALGORITMA GENETIKA ===

        // Fitness: 0.850 - 0.985 (makin tinggi makin baik)
        double fitness = 0.850 + _random.NextDouble() * 0.135;
        FitnessValueLabel.Text = fitness.ToString("F3");

        // Fitness change: -1.5% sampai +3.5%
        double fitnessChange = -1.5 + _random.NextDouble() * 5.0;
        if (fitnessChange >= 0)
        {
            FitnessChangeLabel.Text = $"â†‘ {fitnessChange:F1}%";
            FitnessChangeLabel.TextColor = Color.FromArgb("#12B76A");
        }
        else
        {
            FitnessChangeLabel.Text = $"â†“ {Math.Abs(fitnessChange):F1}%";
            FitnessChangeLabel.TextColor = Color.FromArgb("#EF4444");
        }

        // Generasi: 10 - 95
        int generation = _random.Next(10, 96);
        GenerationValueLabel.Text = generation.ToString();

        // === TIMESTAMP UPDATE ===

        LastUpdatedLabel.Text = $"Terakhir diperbarui: {DateTime.Now:HH:mm:ss dd/MM/yyyy}";
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _refreshTimer?.Stop();
    }

    private void OnOpenNlpChatClicked(object sender, EventArgs e)
    {
        Application.Current!.Windows[0].Page = new ChatBotPage("Admin");
    }

    private void OnOpenAdminReportsClicked(object sender, EventArgs e)
    {
        Application.Current!.Windows[0].Page = new AdminReportsPage();
    }

    private void OnOpenAdminMapClicked(object sender, EventArgs e)
    {
        Application.Current!.Windows[0].Page = new AdminMapPage();
    }

    private void OnOpenCctvClicked(object sender, EventArgs e)
    {
        Application.Current!.Windows[0].Page = new SurabayaCctvPage();
    }
}