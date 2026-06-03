using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using System.Drawing.Drawing2D;
using MCAddonErsteller.Models;
using MCAddonErsteller.Services;

namespace MCAddonErsteller;

public sealed class MainForm : Form
{
  private static readonly Color WindowBack = Color.FromArgb(242, 244, 248);
  private static readonly Color CardBack = Color.White;
  private static readonly Color BorderColor = Color.FromArgb(214, 221, 232);
  private static readonly Color TextMain = Color.FromArgb(24, 31, 42);
  private static readonly Color TextMuted = Color.FromArgb(94, 105, 120);
  private static readonly Color AccentBlue = Color.FromArgb(46, 134, 222);
  private static readonly Color AccentGold = Color.FromArgb(220, 171, 68);
  private static readonly Color StatusDark = Color.FromArgb(15, 18, 27);

  private readonly CheckBox _includeBpCheck = new();
  private readonly CheckBox _includeRpCheck = new();
  private readonly TextBox _bpPathText = new();
  private readonly TextBox _rpPathText = new();
  private readonly Label _bpInfoLabel = new();
  private readonly Label _rpInfoLabel = new();
  private readonly TextBox _packageNameText = new();
  private readonly TextBox _versionText = new();
  private readonly TextBox _outputPathText = new();
  private readonly TextBox _logText = new();
  private readonly ProgressBar _buildProgress = new();
  private readonly Label _buildProgressLabel = new();
  private readonly Button _buildButton = new();
  private readonly ToolStripStatusLabel _statusLabel = new();
  private readonly ToolStripStatusLabel _statusSpacer = new();
  private readonly ToolStripProgressBar _statusProgress = new();

  public MainForm()
  {
    Text = "MC Addon Ersteller";
    StartPosition = FormStartPosition.CenterScreen;
    FormBorderStyle = FormBorderStyle.FixedSingle;
    MaximizeBox = false;
    MinimizeBox = true;
    ClientSize = new Size(920, 620);
    MinimumSize = Size;
    MaximumSize = Size;
    BackColor = WindowBack;
    Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
    AutoScaleMode = AutoScaleMode.Dpi;

    ApplyAppIcon();
    BuildUi();
    SetDefaults();
  }


  private void ApplyAppIcon()
  {
    try
    {
      string? exePath = Application.ExecutablePath;
      if (!string.IsNullOrWhiteSpace(exePath))
      {
        using Icon? extractedIcon = Icon.ExtractAssociatedIcon(exePath);
        if (extractedIcon is not null)
          Icon = (Icon)extractedIcon.Clone();
      }
    }
    catch
    {
      // Fallback: Standard Windows-Icon verwenden.
    }
  }

  private void BuildUi()
  {
    Controls.Add(CreateStatusStrip());
    Controls.Add(CreateHeader());

    Panel bpCard = CreateCard("Behavior Pack", "BP Ordner, ZIP, MCPACK oder MCADDON auswählen", new Point(18, 104), new Size(430, 172));
    BuildPackSelector(bpCard, isBehaviorPack: true);
    Controls.Add(bpCard);

    Panel rpCard = CreateCard("Resource Pack", "RP Ordner, ZIP, MCPACK oder MCADDON auswählen", new Point(472, 104), new Size(430, 172));
    BuildPackSelector(rpCard, isBehaviorPack: false);
    Controls.Add(rpCard);

    Panel outputCard = CreateCard("Ausgabe", "Name, Version und Speicherort der .mcaddon", new Point(18, 292), new Size(430, 218));
    BuildOutputArea(outputCard);
    Controls.Add(outputCard);

    Panel logCard = CreateCard("Build Log", "Hier siehst du, was der Launcher macht", new Point(472, 292), new Size(430, 218));
    BuildLogArea(logCard);
    Controls.Add(logCard);

    _buildButton.Text = "MCADDON ERSTELLEN";
    _buildButton.Location = new Point(18, 526);
    _buildButton.Size = new Size(884, 42);
    _buildButton.Font = new Font("Segoe UI Semibold", 11F, FontStyle.Bold, GraphicsUnit.Point);
    StylePrimaryButton(_buildButton);
    _buildButton.Click += BuildButton_Click;
    Controls.Add(_buildButton);
  }

  private Control CreateHeader()
  {
    GradientHeaderPanel header = new()
    {
      Location = new Point(0, 0),
      Size = new Size(ClientSize.Width, 86),
      Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right,
      StartColor = Color.FromArgb(13, 17, 29),
      EndColor = Color.FromArgb(21, 38, 63)
    };

    Label title = new()
    {
      Text = "MC Addon Ersteller",
      AutoSize = true,
      ForeColor = Color.White,
      Font = new Font("Segoe UI Semibold", 18F, FontStyle.Bold, GraphicsUnit.Point),
      Location = new Point(20, 16),
      BackColor = Color.Transparent
    };

    Label subtitle = new()
    {
      Text = "BP/RP aus Ordnern oder ZIP-Dateien sauber zu einer .mcaddon packen",
      AutoSize = true,
      ForeColor = Color.FromArgb(210, 224, 245),
      Font = new Font("Segoe UI", 9.5F, FontStyle.Regular, GraphicsUnit.Point),
      Location = new Point(22, 52),
      BackColor = Color.Transparent
    };

    Label tag = new()
    {
      Text = "MINECRAFT BEDROCK",
      ForeColor = Color.FromArgb(25, 31, 42),
      BackColor = AccentGold,
      TextAlign = ContentAlignment.MiddleCenter,
      Font = new Font("Segoe UI Semibold", 8.5F, FontStyle.Bold, GraphicsUnit.Point),
      Location = new Point(736, 26),
      Size = new Size(164, 28)
    };

    header.Controls.Add(title);
    header.Controls.Add(subtitle);
    header.Controls.Add(tag);
    return header;
  }

  private StatusStrip CreateStatusStrip()
  {
    StatusStrip statusStrip = new()
    {
      BackColor = StatusDark,
      ForeColor = Color.FromArgb(228, 232, 240),
      SizingGrip = false,
      RenderMode = ToolStripRenderMode.Professional,
      Renderer = new DarkStatusRenderer()
    };

    _statusLabel.Text = "Bereit";
    _statusLabel.ForeColor = Color.FromArgb(228, 232, 240);
    _statusSpacer.Spring = true;
    _statusSpacer.Text = "";
    _statusProgress.Minimum = 0;
    _statusProgress.Maximum = 100;
    _statusProgress.Value = 0;
    _statusProgress.Visible = true;
    _statusProgress.Size = new Size(160, 16);

    ToolStripStatusLabel versionLabel = new()
    {
      Text = "v1.0.1",
      ForeColor = Color.FromArgb(150, 163, 184)
    };

    statusStrip.Items.Add(_statusLabel);
    statusStrip.Items.Add(_statusSpacer);
    statusStrip.Items.Add(_statusProgress);
    statusStrip.Items.Add(versionLabel);
    return statusStrip;
  }

  private Panel CreateCard(string title, string subtitle, Point location, Size size)
  {
    BorderPanel card = new()
    {
      Location = location,
      Size = size,
      BackColor = CardBack,
      BorderColor = BorderColor,
      BorderWidth = 1
    };

    Label titleLabel = new()
    {
      Text = title,
      ForeColor = TextMain,
      Font = new Font("Segoe UI Semibold", 11.5F, FontStyle.Bold, GraphicsUnit.Point),
      Location = new Point(16, 12),
      AutoSize = true
    };

    Label subtitleLabel = new()
    {
      Text = subtitle,
      ForeColor = TextMuted,
      Font = new Font("Segoe UI", 8.5F, FontStyle.Regular, GraphicsUnit.Point),
      Location = new Point(17, 38),
      AutoSize = true
    };

    card.Controls.Add(titleLabel);
    card.Controls.Add(subtitleLabel);
    return card;
  }

  private void BuildPackSelector(Panel parent, bool isBehaviorPack)
  {
    CheckBox check = isBehaviorPack ? _includeBpCheck : _includeRpCheck;
    TextBox path = isBehaviorPack ? _bpPathText : _rpPathText;
    Label info = isBehaviorPack ? _bpInfoLabel : _rpInfoLabel;

    check.Text = isBehaviorPack ? "BP benutzen" : "RP benutzen";
    check.Location = new Point(16, 66);
    check.Size = new Size(120, 24);
    check.ForeColor = TextMain;
    parent.Controls.Add(check);

    path.Location = new Point(16, 96);
    path.Size = new Size(292, 23);
    path.ReadOnly = true;
    path.PlaceholderText = "Noch nichts ausgewählt";
    parent.Controls.Add(path);

    Button folderButton = CreateSmallButton("Ordner", new Point(316, 94), new Size(88, 27));
    folderButton.Click += (_, _) => BrowseFolder(isBehaviorPack);
    parent.Controls.Add(folderButton);

    Button fileButton = CreateSmallButton("ZIP", new Point(316, 126), new Size(42, 27));
    fileButton.Click += (_, _) => BrowseFile(isBehaviorPack);
    parent.Controls.Add(fileButton);

    Button clearButton = CreateSmallButton("X", new Point(362, 126), new Size(42, 27));
    clearButton.Click += (_, _) => ClearPack(isBehaviorPack);
    parent.Controls.Add(clearButton);

    info.Text = "Manifest: -";
    info.Location = new Point(16, 130);
    info.Size = new Size(292, 34);
    info.ForeColor = TextMuted;
    info.Font = new Font("Segoe UI", 8.4F, FontStyle.Regular, GraphicsUnit.Point);
    parent.Controls.Add(info);
  }

  private void BuildOutputArea(Panel parent)
  {
    AddFieldLabel(parent, "Addon Name", new Point(16, 66));
    _packageNameText.Location = new Point(112, 63);
    _packageNameText.Size = new Size(292, 23);
    _packageNameText.PlaceholderText = "z.B. MeinAddon";
    parent.Controls.Add(_packageNameText);

    AddFieldLabel(parent, "Version", new Point(16, 100));
    _versionText.Location = new Point(112, 97);
    _versionText.Size = new Size(120, 23);
    _versionText.PlaceholderText = "1.0.0";
    parent.Controls.Add(_versionText);

    Label hint = new()
    {
      Text = "Dateiname: Name_v1_0_0.mcaddon",
      Location = new Point(242, 101),
      Size = new Size(170, 20),
      ForeColor = TextMuted,
      Font = new Font("Segoe UI", 8.2F, FontStyle.Regular, GraphicsUnit.Point)
    };
    parent.Controls.Add(hint);

    AddFieldLabel(parent, "Ausgabe", new Point(16, 136));
    _outputPathText.Location = new Point(112, 133);
    _outputPathText.Size = new Size(218, 23);
    _outputPathText.ReadOnly = true;
    parent.Controls.Add(_outputPathText);

    Button outputButton = CreateSmallButton("Wählen", new Point(338, 131), new Size(66, 27));
    outputButton.Click += (_, _) => BrowseOutputFolder();
    parent.Controls.Add(outputButton);

    Label bottomHint = new()
    {
      Text = "Die originalen BP/RP Dateien werden nicht verändert.",
      Location = new Point(16, 178),
      Size = new Size(388, 22),
      ForeColor = Color.FromArgb(88, 115, 150),
      Font = new Font("Segoe UI Semibold", 8.4F, FontStyle.Regular, GraphicsUnit.Point)
    };
    parent.Controls.Add(bottomHint);
  }

  private void BuildLogArea(Panel parent)
  {
    _buildProgressLabel.Text = "Fortschritt: 0%";
    _buildProgressLabel.Location = new Point(16, 62);
    _buildProgressLabel.Size = new Size(160, 20);
    _buildProgressLabel.ForeColor = TextMuted;
    _buildProgressLabel.Font = new Font("Segoe UI Semibold", 8.6F, FontStyle.Bold, GraphicsUnit.Point);
    parent.Controls.Add(_buildProgressLabel);

    _buildProgress.Location = new Point(176, 64);
    _buildProgress.Size = new Size(228, 17);
    _buildProgress.Minimum = 0;
    _buildProgress.Maximum = 100;
    _buildProgress.Value = 0;
    _buildProgress.Style = ProgressBarStyle.Continuous;
    parent.Controls.Add(_buildProgress);

    _logText.Location = new Point(16, 92);
    _logText.Size = new Size(388, 106);
    _logText.Multiline = true;
    _logText.ReadOnly = true;
    _logText.ScrollBars = ScrollBars.Vertical;
    _logText.BackColor = Color.FromArgb(248, 250, 252);
    _logText.ForeColor = TextMain;
    _logText.BorderStyle = BorderStyle.FixedSingle;
    _logText.Font = new Font("Consolas", 8.6F, FontStyle.Regular, GraphicsUnit.Point);
    parent.Controls.Add(_logText);
  }

  private void AddFieldLabel(Control parent, string text, Point location)
  {
    Label label = new()
    {
      Text = text,
      Location = location,
      Size = new Size(90, 22),
      ForeColor = TextMuted,
      Font = new Font("Segoe UI Semibold", 8.8F, FontStyle.Bold, GraphicsUnit.Point)
    };
    parent.Controls.Add(label);
  }

  private Button CreateSmallButton(string text, Point location, Size size)
  {
    Button button = new()
    {
      Text = text,
      Location = location,
      Size = size,
      Font = new Font("Segoe UI Semibold", 8.4F, FontStyle.Bold, GraphicsUnit.Point),
      Cursor = Cursors.Hand
    };
    StyleSecondaryButton(button);
    return button;
  }

  private static void StylePrimaryButton(Button button)
  {
    button.FlatStyle = FlatStyle.Flat;
    button.FlatAppearance.BorderSize = 0;
    button.BackColor = AccentBlue;
    button.ForeColor = Color.White;
    button.Cursor = Cursors.Hand;
  }

  private static void StyleSecondaryButton(Button button)
  {
    button.FlatStyle = FlatStyle.Flat;
    button.FlatAppearance.BorderColor = BorderColor;
    button.FlatAppearance.BorderSize = 1;
    button.BackColor = Color.FromArgb(247, 249, 252);
    button.ForeColor = TextMain;
  }

  private void SetDefaults()
  {
    _packageNameText.Text = "MeinAddon";
    _versionText.Text = "1.0.0";
    string desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
    _outputPathText.Text = string.IsNullOrWhiteSpace(desktop) ? Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) : desktop;
    Log("Bereit. Wähle BP/RP als Ordner oder ZIP aus.");
  }

  private void BrowseFolder(bool isBehaviorPack)
  {
    using FolderBrowserDialog dialog = new()
    {
      Description = isBehaviorPack ? "Behavior Pack Ordner auswählen" : "Resource Pack Ordner auswählen",
      ShowNewFolderButton = false
    };

    if (dialog.ShowDialog(this) == DialogResult.OK)
      SetPackPath(isBehaviorPack, dialog.SelectedPath);
  }

  private void BrowseFile(bool isBehaviorPack)
  {
    using OpenFileDialog dialog = new()
    {
      Title = isBehaviorPack ? "Behavior Pack Datei auswählen" : "Resource Pack Datei auswählen",
      Filter = "Bedrock Packs (*.zip;*.mcpack;*.mcaddon)|*.zip;*.mcpack;*.mcaddon|Alle Dateien (*.*)|*.*",
      Multiselect = false,
      CheckFileExists = true
    };

    if (dialog.ShowDialog(this) == DialogResult.OK)
      SetPackPath(isBehaviorPack, dialog.FileName);
  }

  private void BrowseOutputFolder()
  {
    using FolderBrowserDialog dialog = new()
    {
      Description = "Ausgabeordner auswählen",
      ShowNewFolderButton = true,
      SelectedPath = Directory.Exists(_outputPathText.Text) ? _outputPathText.Text : Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory)
    };

    if (dialog.ShowDialog(this) == DialogResult.OK)
      _outputPathText.Text = dialog.SelectedPath;
  }

  private void SetPackPath(bool isBehaviorPack, string path)
  {
    if (isBehaviorPack)
    {
      _bpPathText.Text = path;
      _includeBpCheck.Checked = true;
    }
    else
    {
      _rpPathText.Text = path;
      _includeRpCheck.Checked = true;
    }

    PreviewPack(isBehaviorPack, path);
  }

  private void ClearPack(bool isBehaviorPack)
  {
    if (isBehaviorPack)
    {
      _bpPathText.Clear();
      _bpInfoLabel.Text = "Manifest: -";
      _includeBpCheck.Checked = false;
    }
    else
    {
      _rpPathText.Clear();
      _rpInfoLabel.Text = "Manifest: -";
      _includeRpCheck.Checked = false;
    }

    UpdateStatus("Quelle entfernt.");
  }

  private void PreviewPack(bool isBehaviorPack, string path)
  {
    string role = isBehaviorPack ? "BP" : "RP";
    ResolvedPack? pack = null;

    try
    {
      pack = PackResolver.Resolve(path, role);
      string preview = $"Manifest: {pack.Manifest.Name} | v{pack.Manifest.Version} | {pack.Manifest.Kind}";

      if (isBehaviorPack)
        _bpInfoLabel.Text = preview;
      else
        _rpInfoLabel.Text = preview;

      if (string.IsNullOrWhiteSpace(_packageNameText.Text) || _packageNameText.Text.Equals("MeinAddon", StringComparison.OrdinalIgnoreCase))
        _packageNameText.Text = pack.Manifest.Name;

      if (string.IsNullOrWhiteSpace(_versionText.Text) || _versionText.Text == "1.0.0")
        _versionText.Text = pack.Manifest.Version;

      Log($"{role} ausgewählt: {pack.ArchiveFolderName}");
      UpdateStatus($"{role} erkannt.");
    }
    catch (Exception ex)
    {
      if (isBehaviorPack)
        _bpInfoLabel.Text = "Manifest: Fehler";
      else
        _rpInfoLabel.Text = "Manifest: Fehler";

      Log($"FEHLER {role}: {ex.Message}");
      UpdateStatus($"{role} Fehler.");
      MessageBox.Show(this, ex.Message, "Pack konnte nicht gelesen werden", MessageBoxButtons.OK, MessageBoxIcon.Warning);
    }
    finally
    {
      if (pack is not null)
        PackResolver.DeleteTemporaryDirectory(pack);
    }
  }

  private async void BuildButton_Click(object? sender, EventArgs e)
  {
    try
    {
      _buildButton.Enabled = false;
      SetBuildProgress(0);
      Log("──────────────── Build gestartet ────────────────");
      UpdateStatus("Erstelle MCADDON ...");

      BuildOptions options = new()
      {
        BehaviorPackPath = _bpPathText.Text,
        ResourcePackPath = _rpPathText.Text,
        IncludeBehaviorPack = _includeBpCheck.Checked,
        IncludeResourcePack = _includeRpCheck.Checked,
        PackageName = _packageNameText.Text,
        Version = _versionText.Text,
        OutputDirectory = _outputPathText.Text,
        Log = Log,
        Status = UpdateStatus,
        StepDelayMilliseconds = 180,
        Progress = new Progress<double>(SetBuildProgress)
      };

      string outputPath = await new McAddonBuilder().BuildAsync(options);
      SetBuildProgress(100);
      UpdateStatus("Fertig.");
      MessageBox.Show(this, $"MCADDON wurde erstellt:\n\n{outputPath}", "Fertig", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }
    catch (Exception ex)
    {
      UpdateStatus("Fehler.");
      Log("FEHLER: " + ex.Message);
      MessageBox.Show(this, ex.Message, "Build fehlgeschlagen", MessageBoxButtons.OK, MessageBoxIcon.Error);
    }
    finally
    {
      _buildButton.Enabled = true;
    }
  }

  private void Log(string message)
  {
    if (_logText.InvokeRequired)
    {
      _logText.BeginInvoke(new Action(() => Log(message)));
      return;
    }

    if (_logText.IsDisposed)
      return;

    string line = $"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}";
    _logText.AppendText(line);
  }

  private void SetBuildProgress(double value)
  {
    if (InvokeRequired)
    {
      BeginInvoke(new Action(() => SetBuildProgress(value)));
      return;
    }

    int progress = Math.Clamp((int)Math.Round(value), 0, 100);
    _statusProgress.Value = progress;
    _buildProgress.Value = progress;
    _buildProgressLabel.Text = $"Fortschritt: {progress}%";
  }

  private void UpdateStatus(string text)
  {
    if (InvokeRequired)
    {
      BeginInvoke(new Action(() => UpdateStatus(text)));
      return;
    }

    _statusLabel.Text = text;
  }

  private sealed class DarkStatusRenderer : ToolStripProfessionalRenderer
  {
    protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
    {
      e.Graphics.Clear(StatusDark);
    }

    protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
    {
      using Pen pen = new(Color.FromArgb(42, 50, 66));
      e.Graphics.DrawLine(pen, 0, 0, e.ToolStrip.Width, 0);
    }
  }

  private sealed class GradientHeaderPanel : Panel
  {
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color StartColor { get; init; } = Color.Black;

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color EndColor { get; init; } = Color.FromArgb(20, 30, 45);

    public GradientHeaderPanel()
    {
      DoubleBuffered = true;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
      using LinearGradientBrush brush = new(ClientRectangle, StartColor, EndColor, LinearGradientMode.Horizontal);
      e.Graphics.FillRectangle(brush, ClientRectangle);
      base.OnPaint(e);
    }
  }

  private sealed class BorderPanel : Panel
  {
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color BorderColor { get; init; } = Color.LightGray;

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int BorderWidth { get; init; } = 1;

    public BorderPanel()
    {
      DoubleBuffered = true;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
      base.OnPaint(e);
      using Pen pen = new(BorderColor, BorderWidth);
      Rectangle rect = new(0, 0, Width - 1, Height - 1);
      e.Graphics.DrawRectangle(pen, rect);
    }
  }
}
