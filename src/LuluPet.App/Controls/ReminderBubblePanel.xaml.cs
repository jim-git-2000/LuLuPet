using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using LuluPet.Core.Reminders;

namespace LuluPet.App.Controls;

public partial class ReminderBubblePanel : System.Windows.Controls.UserControl
{
    private static readonly Regex IntegerRegex = new("^[0-9]+$");
    private bool _isApplyingSettings;

    public ReminderBubblePanel()
    {
        InitializeComponent();

        PomodoroToggle.CheckedChanged += (_, value) => RaiseBooleanChanged(PomodoroEnabledChanged, value);
        WaterToggle.CheckedChanged += (_, value) => RaiseBooleanChanged(WaterReminderEnabledChanged, value);
        StandToggle.CheckedChanged += (_, value) => RaiseBooleanChanged(StandReminderEnabledChanged, value);
        UpdateStatusText();
    }

    public event EventHandler<bool>? PomodoroEnabledChanged;

    public event EventHandler<int>? PomodoroMinutesChanged;

    public event EventHandler<int>? PomodoroRestMinutesChanged;

    public event EventHandler<bool>? WaterReminderEnabledChanged;

    public event EventHandler<int>? WaterReminderMinutesChanged;

    public event EventHandler<bool>? StandReminderEnabledChanged;

    public event EventHandler<int>? StandReminderMinutesChanged;

    public event EventHandler? CloseRequested;

    public void ApplySettings(ReminderSettings settings)
    {
        _isApplyingSettings = true;
        try
        {
            PomodoroToggle.IsChecked = settings.PomodoroEnabled;
            PomodoroMinutesTextBox.Text = settings.PomodoroMinutes.ToString();
            PomodoroRestMinutesTextBox.Text = settings.PomodoroRestMinutes.ToString();
            WaterToggle.IsChecked = settings.WaterReminderEnabled;
            WaterMinutesTextBox.Text = settings.WaterReminderMinutes.ToString();
            StandToggle.IsChecked = settings.StandReminderEnabled;
            StandMinutesTextBox.Text = settings.StandReminderMinutes.ToString();
        }
        finally
        {
            _isApplyingSettings = false;
        }

        UpdateStatusText();
    }

    public void ApplyScale(double scale)
    {
        Width = 300;
        Height = 292;
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    private void IntegerTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        e.Handled = !IntegerRegex.IsMatch(e.Text);
    }

    private void IntegerTextBox_Pasting(object sender, DataObjectPastingEventArgs e)
    {
        if (!e.DataObject.GetDataPresent(DataFormats.Text))
        {
            e.CancelCommand();
            return;
        }

        var text = e.DataObject.GetData(DataFormats.Text) as string;
        if (string.IsNullOrWhiteSpace(text) || !IntegerRegex.IsMatch(text))
        {
            e.CancelCommand();
        }
    }

    private void PomodoroMinutesTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        RaiseIntegerChanged(PomodoroMinutesTextBox.Text, min: 1, max: 240, PomodoroMinutesChanged);
    }

    private void PomodoroRestMinutesTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        RaiseIntegerChanged(PomodoroRestMinutesTextBox.Text, min: 1, max: 120, PomodoroRestMinutesChanged);
    }

    private void WaterMinutesTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        RaiseIntegerChanged(WaterMinutesTextBox.Text, min: 1, max: 240, WaterReminderMinutesChanged);
    }

    private void StandMinutesTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        RaiseIntegerChanged(StandMinutesTextBox.Text, min: 1, max: 240, StandReminderMinutesChanged);
    }

    private void RaiseBooleanChanged(EventHandler<bool>? handler, bool value)
    {
        if (_isApplyingSettings)
        {
            return;
        }

        handler?.Invoke(this, value);
        UpdateStatusText();
    }

    private void RaiseIntegerChanged(string text, int min, int max, EventHandler<int>? handler)
    {
        if (_isApplyingSettings || string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        if (!int.TryParse(text, out var value))
        {
            return;
        }

        value = Math.Clamp(value, min, max);
        handler?.Invoke(this, value);
        UpdateStatusText();
    }

    private void UpdateStatusText()
    {
        PomodoroStatusText.Text = PomodoroToggle.IsChecked ? "状态：专注待开始" : "状态：未开启";
        WaterStatusText.Text = WaterToggle.IsChecked ? "已开启" : "未开启";
        StandStatusText.Text = StandToggle.IsChecked ? "已开启" : "未开启";
    }
}
