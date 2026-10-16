using System.Runtime.InteropServices;
using System.Text.Json;
using GreatJsonEscape.Processing;

namespace GreatJsonEscape.UI;

internal sealed class MainForm : Form
{
    private static readonly string[] SuccessBlurbs =
    [
        "Gotcha! That JSON won't be escaping on our watch.",
        "Caught, cuffed, and pretty printed. Nice work, detective.",
        "The great escape has been foiled. JSON is back in custody.",
        "Slippery one, but we wrangled every last backslash.",
        "Freedom denied. Your JSON is now fully legible and very sorry."
    ];

    private static readonly Random Rng = new();

    private readonly TextBox _inputBox;
    private readonly TextBox _outputBox;
    private readonly CheckBox _compactCheck;
    private readonly Button _catchButton;
    private readonly Button _copyButton;
    private readonly Label _statusLabel;

    public MainForm()
    {
        Text = "Great JSON Escape";
        Icon = LoadAppIcon();
        MinimumSize = new Size(700, 560);
        Size = new Size(900, 700);
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Segoe UI", 9F);

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(12),
            ColumnCount = 1,
            RowCount = 6
        };

        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var promptLabel = new Label
        {
            Text = "Quick, that JSON is getting away! Paste escaping JSON below.",
            AutoSize = true,
            Margin = new Padding(3, 3, 3, 8),
            Font = new Font("Segoe UI", 12F, FontStyle.Bold)
        };

        _inputBox = new TextBox
        {
            Dock = DockStyle.Fill,
            Multiline = true,
            ScrollBars = ScrollBars.Both,
            WordWrap = false,
            AcceptsTab = true,
            Font = new Font("Cascadia Mono", 9.5F, FontStyle.Regular, GraphicsUnit.Point)
        };

        var buttonRow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            Margin = new Padding(3, 8, 3, 8)
        };

        _catchButton = new Button
        {
            Text = "Catch it now",
            AutoSize = true,
            Padding = new Padding(14, 6, 14, 6)
        };
        _catchButton.Click += OnCatchClicked;

        _compactCheck = new CheckBox
        {
            Text = "Compact output",
            AutoSize = true,
            Margin = new Padding(14, 9, 3, 3)
        };

        _copyButton = new Button
        {
            Text = "Copy to clipboard",
            AutoSize = true,
            Enabled = false,
            Padding = new Padding(14, 6, 14, 6),
            Margin = new Padding(14, 3, 3, 3)
        };
        _copyButton.Click += OnCopyClicked;

        var clearButton = new Button
        {
            Text = "Clear",
            AutoSize = true,
            Padding = new Padding(14, 6, 14, 6),
            Margin = new Padding(14, 3, 3, 3)
        };
        clearButton.Click += OnClearClicked;

        buttonRow.Controls.Add(_catchButton);
        buttonRow.Controls.Add(_compactCheck);
        buttonRow.Controls.Add(_copyButton);
        buttonRow.Controls.Add(clearButton);

        var outputLabel = new Label
        {
            Text = "The recovered JSON:",
            AutoSize = true,
            Margin = new Padding(3, 3, 3, 4),
            Font = new Font("Segoe UI", 9F, FontStyle.Bold)
        };

        _outputBox = new TextBox
        {
            Dock = DockStyle.Fill,
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Both,
            WordWrap = false,
            BackColor = SystemColors.Window,
            Font = new Font("Cascadia Mono", 9.5F, FontStyle.Regular, GraphicsUnit.Point)
        };

        _statusLabel = new Label
        {
            AutoSize = true,
            Margin = new Padding(3, 8, 3, 3),
            Text = "Standing by. No JSON has escaped yet."
        };

        layout.Controls.Add(promptLabel, 0, 0);
        layout.Controls.Add(_inputBox, 0, 1);
        layout.Controls.Add(buttonRow, 0, 2);
        layout.Controls.Add(outputLabel, 0, 3);
        layout.Controls.Add(_outputBox, 0, 4);
        layout.Controls.Add(_statusLabel, 0, 5);

        Controls.Add(layout);
        AcceptButton = _catchButton;
    }

    private static Icon? LoadAppIcon()
    {
        var assembly = typeof(MainForm).Assembly;
        var name = Array.Find(
            assembly.GetManifestResourceNames(),
            n => n.EndsWith("HesGettingAway.ico", StringComparison.OrdinalIgnoreCase));

        if (name is null)
            return null;

        using var stream = assembly.GetManifestResourceStream(name);
        return stream is null ? null : new Icon(stream);
    }

    private void OnCatchClicked(object? sender, EventArgs e)
    {
        var input = _inputBox.Text;

        if (string.IsNullOrWhiteSpace(input))
        {
            SetStatus("There's nothing to chase. Paste some JSON first.", isError: true);
            _outputBox.Clear();
            _copyButton.Enabled = false;
            return;
        }

        try
        {
            var processor = new JsonProcessor(_compactCheck.Checked);
            _outputBox.Text = processor.Process(input);
            _copyButton.Enabled = _outputBox.TextLength > 0;
            SetStatus(SuccessBlurbs[Rng.Next(SuccessBlurbs.Length)], isError: false);
        }
        catch (JsonException jx)
        {
            _outputBox.Clear();
            _copyButton.Enabled = false;
            SetStatus("That one got away. JSON parsing error: " + jx.Message, isError: true);
        }
    }

    private void OnCopyClicked(object? sender, EventArgs e)
    {
        if (_outputBox.TextLength == 0)
            return;

        try
        {
            Clipboard.SetText(_outputBox.Text);
            SetStatus("Copied to the clipboard. Don't let it run off again.", isError: false);
        }
        catch (ExternalException)
        {
            SetStatus("The clipboard refused to cooperate. Try again in a moment.", isError: true);
        }
    }

    private void OnClearClicked(object? sender, EventArgs e)
    {
        _inputBox.Clear();
        _outputBox.Clear();
        _copyButton.Enabled = false;
        SetStatus("Standing by. No JSON has escaped yet.", isError: false);
        _inputBox.Focus();
    }

    private void SetStatus(string message, bool isError)
    {
        _statusLabel.Text = message;
        _statusLabel.ForeColor = isError ? Color.Firebrick : Color.DarkGreen;
    }
}
