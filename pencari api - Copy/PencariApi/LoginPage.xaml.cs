namespace PencariApi;

public partial class LoginPage : ContentPage
{
    public LoginPage()
    {
        InitializeComponent();
    }

    private async void OnLoginClicked(object sender, EventArgs e)
    {
        string username = UsernameEntry.Text?.Trim() ?? "";
        string password = PasswordEntry.Text?.Trim() ?? "";

        if (username == "admin" && password == "12345")
        {
            ErrorLabel.Text = "";

            string role = "Operator";

            Application.Current!.Windows[0].Page = new FaceVerificationPage(username, role);
        }
        else
        {
            ErrorLabel.Text = "Username atau password salah!";

            await DisplayAlert(
                "Login Gagal",
                "Username atau password tidak sesuai.",
                "OK"
            );
        }
    }
}