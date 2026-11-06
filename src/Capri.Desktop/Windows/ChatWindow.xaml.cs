using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Capri.Core;

namespace Capri.Desktop.Windows;

public partial class ChatWindow : Window
{
    public ObservableCollection<ChatTurn> Messages { get; } = new();
    private bool _busy;

    public ChatWindow()
    {
        InitializeComponent();
        DataContext = this;
        ModelBox.Text = App.Instance.Settings.OllamaModel;

        foreach (var turn in App.Instance.Store.LoadHistory())
            Messages.Add(turn);

        if (Messages.Count == 0)
            Add("assistant", "Hey, choom. This is the first build. I can chat with a local model, " +
                "but no screen vision, voice or desktop actions yet. We'll earn those upgrades.");

        Loaded += async (_, _) => await CheckBrainAsync();
    }

    private void Add(string role, string text)
    {
        Messages.Add(new ChatTurn
        {
            Role = role,
            Text = text,
            Timestamp = DateTimeOffset.Now
        });
        while (Messages.Count > 120) Messages.RemoveAt(0);
        App.Instance.Store.SaveHistory(Messages);
        Dispatcher.BeginInvoke(() => TranscriptScroll.ScrollToEnd());
    }

    private async Task CheckBrainAsync()
    {
        var available = await App.Instance.Brain.IsAvailableAsync();
        UpdateStatus(available ? "LOCAL BRAIN ONLINE" : "OLLAMA OFFLINE",
            available ? Colors.LightGreen : Colors.Orange);
    }

    private void UpdateStatus(string text, Color color)
    {
        StatusDisplay.Text = text;
        StatusDisplay.Foreground = new SolidColorBrush(color);
    }

    private void SaveModel_Click(object sender, RoutedEventArgs e)
    {
        var selected = ModelBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(selected)) return;
        App.Instance.Settings.OllamaModel = selected;
        App.Instance.Store.SaveSettings(App.Instance.Settings);
        UpdateStatus("MODEL SAVED", Colors.LightSkyBlue);
    }

    private void Prompt_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && (Keyboard.Modifiers & ModifierKeys.Shift) == 0)
        {
            e.Handled = true;
            _ = SendAsync();
        }
    }

    private void Send_Click(object sender, RoutedEventArgs e) => _ = SendAsync();

    private async Task SendAsync()
    {
        if (_busy) return;
        var prompt = PromptBox.Text.Trim();
        if (prompt.Length == 0) return;

        _busy = true;
        SendButton.IsEnabled = false;
        PromptBox.IsEnabled = false;
        PromptBox.Clear();
        Add("user", prompt);
        App.Instance.SetMood(CompanionMood.Thinking);
        UpdateStatus("THINKING", Colors.Plum);

        try
        {
            var answer = await App.Instance.Brain.ReplyAsync(Messages.ToArray());
            Add("assistant", answer);
            App.Instance.SetMood(CompanionMood.Reply);
            UpdateStatus("LOCAL BRAIN ONLINE", Colors.LightGreen);
        }
        catch (Exception exception)
        {
            App.Instance.SetMood(CompanionMood.Error);
            Add("system", "Local model failed: " + exception.Message +
                "\nCheck that Ollama is running and the chosen model is installed.");
            UpdateStatus("MODEL ERROR", Colors.OrangeRed);
        }
        finally
        {
            _busy = false;
            SendButton.IsEnabled = true;
            PromptBox.IsEnabled = true;
            PromptBox.Focus();
        }
    }

    private void Clear_Click(object sender, RoutedEventArgs e)
    {
        if (MessageBox.Show("Delete your locally saved conversation?", "Capri Desktop",
                MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            return;
        Messages.Clear();
        App.Instance.Store.SaveHistory(Messages);
        App.Instance.SetMood(CompanionMood.Idle);
    }
}
