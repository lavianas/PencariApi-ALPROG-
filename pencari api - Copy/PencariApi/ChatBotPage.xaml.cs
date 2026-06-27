using PencariApi.Services;

namespace PencariApi;

public partial class ChatBotPage : ContentPage
{
    private readonly string _role;

    public ChatBotPage(string role = "Admin")
    {
        InitializeComponent();

        _role = role;
        RoleLabel.Text = $"AI Chat aktif sebagai {_role}";
    }

    private async void OnSendClicked(object sender, EventArgs e)
    {
        await SendMessageAsync();
    }

    private async void OnMessageCompleted(object sender, EventArgs e)
    {
        await SendMessageAsync();
    }

    private async Task SendMessageAsync()
    {
        string message = MessageEntry.Text?.Trim() ?? "";

        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        AddUserMessage(message);

        MessageEntry.Text = "";
        SendButton.IsEnabled = false;
        SendButton.Text = "...";

        AddBotMessage("Sedang memproses jawaban AI...");

        await Task.Delay(150);
        await ChatScrollView.ScrollToAsync(ChatContainer, ScrollToPosition.End, true);

        string aiReply = await GeminiChatService.AskAsync(message, _role);

        RemoveLastBotLoadingMessage();
        AddBotMessage(aiReply);

        SendButton.IsEnabled = true;
        SendButton.Text = "Kirim";

        await Task.Delay(150);
        await ChatScrollView.ScrollToAsync(ChatContainer, ScrollToPosition.End, true);
    }

    private void AddUserMessage(string text)
    {
        Frame bubble = new Frame
        {
            CornerRadius = 18,
            Padding = 16,
            HasShadow = false,
            BackgroundColor = Color.FromArgb("#FF5A14"),
            BorderColor = Color.FromArgb("#FF5A14"),
            HorizontalOptions = LayoutOptions.End,
            MaximumWidthRequest = 760,
            Content = new VerticalStackLayout
            {
                Spacing = 6,
                Children =
                {
                    new Label
                    {
                        Text = "Kamu",
                        FontSize = 12,
                        FontAttributes = FontAttributes.Bold,
                        TextColor = Colors.White
                    },
                    new Label
                    {
                        Text = text,
                        FontSize = 14,
                        TextColor = Colors.White,
                        LineBreakMode = LineBreakMode.WordWrap
                    }
                }
            }
        };

        ChatContainer.Children.Add(bubble);
    }

    private void AddBotMessage(string text)
    {
        Frame bubble = new Frame
        {
            CornerRadius = 18,
            Padding = 16,
            HasShadow = false,
            BackgroundColor = Colors.White,
            BorderColor = Color.FromArgb("#E5EAF2"),
            HorizontalOptions = LayoutOptions.Start,
            MaximumWidthRequest = 780,
            Content = new VerticalStackLayout
            {
                Spacing = 6,
                Children =
                {
                    new Label
                    {
                        Text = "PencariApi OPTIMA AI",
                        FontSize = 12,
                        FontAttributes = FontAttributes.Bold,
                        TextColor = Color.FromArgb("#FF5A14")
                    },
                    new Label
                    {
                        Text = text,
                        FontSize = 14,
                        TextColor = Color.FromArgb("#344054"),
                        LineBreakMode = LineBreakMode.WordWrap
                    }
                }
            }
        };

        ChatContainer.Children.Add(bubble);
    }

    private void RemoveLastBotLoadingMessage()
    {
        if (ChatContainer.Children.Count == 0)
        {
            return;
        }

        View lastChild = (View)ChatContainer.Children.Last();

        if (lastChild is Frame frame &&
            frame.Content is VerticalStackLayout stack &&
            stack.Children.Count >= 2 &&
            stack.Children[1] is Label label &&
            label.Text == "Sedang memproses jawaban AI...")
        {
            ChatContainer.Children.Remove(lastChild);
        }
    }

    private void OnBackClicked(object sender, EventArgs e)
    {
        if (_role.Equals("Admin", StringComparison.OrdinalIgnoreCase))
        {
            Application.Current!.Windows[0].Page = new MainPage();
        }
        else
        {
            Application.Current!.Windows[0].Page = new UserDashboardPage();
        }
    }
}