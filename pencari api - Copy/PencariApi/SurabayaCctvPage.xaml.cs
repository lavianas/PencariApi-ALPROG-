using System.Collections.ObjectModel;

namespace PencariApi;

public partial class SurabayaCctvPage : ContentPage
{
    private const string BaseCctvServer = "http://36.66.208.101:5000";

    private readonly ObservableCollection<CctvMonitorItem> _cctvMonitors = new();

    private CctvMonitorItem? _selectedCctv;
    private string _currentUrl = BaseCctvServer;

    public SurabayaCctvPage()
    {
        InitializeComponent();

        LoadCctvMonitors();

        CctvCollection.ItemsSource = _cctvMonitors;
        CctvCountLabel.Text = $"{_cctvMonitors.Count} pemantau tersedia";

        if (_cctvMonitors.Count > 0)
        {
            CctvCollection.SelectedItem = _cctvMonitors[0];
            LoadCctv(_cctvMonitors[0]);
        }
        else
        {
            LoadMainCctvMap();
        }
    }

    private void LoadCctvMonitors()
    {
        _cctvMonitors.Clear();

        _cctvMonitors.Add(new CctvMonitorItem
        {
            Id = 1,
            Name = "ADITYAWARMAN INDRAGIRI TIMUR FIX",
            Location = "Area Adityawarman - Indragiri Timur, Surabaya",
            Description = "Pemantauan lalu lintas area Adityawarman.",
            Status = "ONLINE",
            StreamUrl = BaseCctvServer
        });

        _cctvMonitors.Add(new CctvMonitorItem
        {
            Id = 2,
            Name = "ADITYAWARMAN INDRAGIRI BARAT FIX",
            Location = "Area Adityawarman - Indragiri Barat, Surabaya",
            Description = "Pemantauan lalu lintas sisi barat Adityawarman.",
            Status = "ONLINE",
            StreamUrl = BaseCctvServer
        });

        _cctvMonitors.Add(new CctvMonitorItem
        {
            Id = 3,
            Name = "ADITYAWARMAN INDRAGIRI PTZ",
            Location = "Area Adityawarman - Indragiri, Surabaya",
            Description = "Pemantauan CCTV PTZ area Adityawarman.",
            Status = "ONLINE",
            StreamUrl = BaseCctvServer
        });

        _cctvMonitors.Add(new CctvMonitorItem
        {
            Id = 4,
            Name = "ADITYAWARMAN INDRAGIRI UTARA FIX",
            Location = "Area Adityawarman - Indragiri Utara, Surabaya",
            Description = "Pemantauan lalu lintas sisi utara.",
            Status = "ONLINE",
            StreamUrl = BaseCctvServer
        });

        _cctvMonitors.Add(new CctvMonitorItem
        {
            Id = 5,
            Name = "ADITYAWARMAN SUTOS PANOVU",
            Location = "Area SUTOS / Adityawarman, Surabaya",
            Description = "Pemantauan kawasan SUTOS dan sekitarnya.",
            Status = "ONLINE",
            StreamUrl = BaseCctvServer
        });

        _cctvMonitors.Add(new CctvMonitorItem
        {
            Id = 6,
            Name = "ADITYAWARMAN SUTOS SELATAN FIX",
            Location = "Area SUTOS Selatan, Surabaya",
            Description = "Pemantauan area selatan SUTOS.",
            Status = "ONLINE",
            StreamUrl = BaseCctvServer
        });

        _cctvMonitors.Add(new CctvMonitorItem
        {
            Id = 7,
            Name = "ALAS MALANG KENDUNG SELATAN PTZ",
            Location = "Area Alas Malang - Kendung Selatan, Surabaya",
            Description = "Pemantauan CCTV PTZ kawasan Alas Malang.",
            Status = "ONLINE",
            StreamUrl = BaseCctvServer
        });

        _cctvMonitors.Add(new CctvMonitorItem
        {
            Id = 8,
            Name = "BABATAN MENGANTI BARAT PTZ",
            Location = "Area Babatan Menganti Barat, Surabaya",
            Description = "Pemantauan kawasan Babatan Menganti Barat.",
            Status = "ONLINE",
            StreamUrl = BaseCctvServer
        });

        _cctvMonitors.Add(new CctvMonitorItem
        {
            Id = 9,
            Name = "BABATAN MENGANTI TIMUR PTZ",
            Location = "Area Babatan Menganti Timur, Surabaya",
            Description = "Pemantauan kawasan Babatan Menganti Timur.",
            Status = "ONLINE",
            StreamUrl = BaseCctvServer
        });

        _cctvMonitors.Add(new CctvMonitorItem
        {
            Id = 10,
            Name = "BALONGSARI PRAJA SELATAN FIX",
            Location = "Area Balongsari Praja Selatan, Surabaya",
            Description = "Pemantauan kawasan Balongsari Praja Selatan.",
            Status = "ONLINE",
            StreamUrl = BaseCctvServer
        });
    }

    private void OnCctvSelected(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is CctvMonitorItem selectedCctv)
        {
            LoadCctv(selectedCctv);
        }
    }

    private async void OnShowSelectedClicked(object sender, EventArgs e)
    {
        if (_selectedCctv == null)
        {
            await DisplayAlert(
                "Pilih CCTV",
                "Pilih salah satu CCTV terlebih dahulu.",
                "OK"
            );

            return;
        }

        LoadCctv(_selectedCctv);
    }

    private void LoadCctv(CctvMonitorItem cctv)
    {
        _selectedCctv = cctv;
        _currentUrl = BaseCctvServer;

        SelectedCctvNameLabel.Text = cctv.Name;
        SelectedCctvLocationLabel.Text = $"Lokasi: {cctv.Location}";
        SelectedCctvStatusLabel.Text = $"Status: {cctv.Status}";
        SelectedCctvStatusLabel.TextColor = Color.FromArgb("#12B76A");
        SelectedCctvModeLabel.Text = "Mode: Peta CCTV Surabaya WebView";
        SelectedCctvDescriptionLabel.Text = cctv.Description;
        SelectedCctvUrlLabel.Text = $"URL: {BaseCctvServer}";

        ShowLoading(true, "Memuat peta CCTV Surabaya...");

        CctvWebView.Source = new UrlWebViewSource
        {
            Url = BaseCctvServer
        };
    }

    private void LoadMainCctvMap()
    {
        _currentUrl = BaseCctvServer;

        SelectedCctvNameLabel.Text = "Peta CCTV Surabaya";
        SelectedCctvLocationLabel.Text = "Lokasi: Surabaya";
        SelectedCctvStatusLabel.Text = "Status: ONLINE";
        SelectedCctvStatusLabel.TextColor = Color.FromArgb("#12B76A");
        SelectedCctvModeLabel.Text = "Mode: Peta CCTV WebView";
        SelectedCctvDescriptionLabel.Text = "Menampilkan peta CCTV Surabaya dari server CCTV.";
        SelectedCctvUrlLabel.Text = $"URL: {BaseCctvServer}";

        ShowLoading(true, "Memuat peta CCTV Surabaya...");

        CctvWebView.Source = new UrlWebViewSource
        {
            Url = BaseCctvServer
        };
    }

    private void OnRefreshClicked(object sender, EventArgs e)
    {
        if (_selectedCctv != null)
        {
            LoadCctv(_selectedCctv);
        }
        else
        {
            LoadMainCctvMap();
        }
    }

    private async void OnOpenBrowserClicked(object sender, EventArgs e)
    {
        try
        {
            await Launcher.Default.OpenAsync(BaseCctvServer);
        }
        catch (Exception ex)
        {
            await DisplayAlert(
                "Gagal Membuka Browser",
                ex.Message,
                "OK"
            );
        }
    }

    private void OnBackDashboardClicked(object sender, EventArgs e)
    {
        Application.Current!.Windows[0].Page = new MainPage();
    }

    private void OnCctvNavigating(object sender, WebNavigatingEventArgs e)
    {
        ShowLoading(true, "Menghubungkan ke server CCTV Surabaya...");
    }

    private void OnCctvNavigated(object sender, WebNavigatedEventArgs e)
    {
        ShowLoading(false);

        /*
         * Jangan langsung tampilkan error.
         * Beberapa web CCTV tetap bisa tampil walaupun WebView mengembalikan status navigasi tidak sempurna.
         */
        SelectedCctvStatusLabel.Text = "Status: ONLINE";
        SelectedCctvStatusLabel.TextColor = Color.FromArgb("#12B76A");
    }

    private void ShowLoading(bool isLoading, string message = "Memuat CCTV...")
    {
        CctvLoadingFrame.IsVisible = isLoading;
        CctvLoadingIndicator.IsRunning = isLoading;
        CctvLoadingLabel.Text = message;
    }

    private class CctvMonitorItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Location { get; set; } = "";
        public string Description { get; set; } = "";
        public string Status { get; set; } = "";
        public string StreamUrl { get; set; } = "";

        public string ShortUrl
        {
            get
            {
                return "Server CCTV Surabaya";
            }
        }
    }
}