using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Reflection;
using System.Windows.Forms;
using MCAddonErsteller.Controls;
using MCAddonErsteller.Models;
using MCAddonErsteller.Services;

namespace MCAddonErsteller;

public sealed partial class MainForm : Form
{
  private static readonly Color WindowBack = Color.FromArgb(242, 244, 248);
  private static readonly Color CardBack = Color.FromArgb(218, 18, 20, 23);
  private static readonly Color BorderColor = Color.FromArgb(55, 58, 62);
  private static readonly Color TextMain = Color.FromArgb(235, 238, 242);
  private static readonly Color TextMuted = Color.FromArgb(170, 176, 184);
  private static readonly Color StatusDark = Color.FromArgb(15, 18, 27);

  private readonly MinecraftCheckBox _includeBpCheck = new();
  private readonly MinecraftCheckBox _includeRpCheck = new();
  private readonly MinecraftTextBox _bpPathText = new();
  private readonly MinecraftTextBox _rpPathText = new();
  private readonly Label _bpInfoLabel = new();
  private readonly Label _rpInfoLabel = new();
  private readonly MinecraftTextBox _packageNameText = new();
  private readonly MinecraftTextBox _versionText = new();
  private readonly MinecraftTextBox _outputPathText = new();
  private readonly TextBox _logText = new();
  private readonly MinecraftTextBox _fileNamePreviewText = new();
  private readonly ProgressBar _buildProgress = new();
  private readonly Label _buildProgressLabel = new();
  private readonly MinecraftButton _buildButton = new();
  private readonly Label _statusLabel = new();
  private readonly Label _statusDot = new();
  private readonly Label _versionLabel = new();

  public MainForm()
  {
    Text = "MC Addon Ersteller";
    StartPosition = FormStartPosition.CenterScreen;
    FormBorderStyle = FormBorderStyle.FixedSingle;
    MaximizeBox = false;
    MinimizeBox = true;
    ClientSize = new Size(920, 580);
    MinimumSize = Size;
    MaximumSize = Size;
    BackColor = WindowBack;
    BackgroundImage = Properties.Resources.background;
    BackgroundImageLayout = ImageLayout.Stretch;
    Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
    AutoScaleMode = AutoScaleMode.Dpi;

    ApplyAppIcon();
    BuildUi();
    UpdateFileNamePreview();
    SetDefaults();

    Shown += MainForm_Shown;
  }

  [LibraryImport("dwmapi.dll")]
  private static partial int DwmSetWindowAttribute(
    IntPtr hwnd,
    int attr,
    ref int attrValue,
    int attrSize
  );

  private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
  private const int DWMWA_USE_IMMERSIVE_DARK_MODE_OLD = 19;

  protected override void OnHandleCreated(EventArgs e)
  {
    base.OnHandleCreated(e);

    int useDarkMode = 1;

    int result = DwmSetWindowAttribute(
      Handle,
      DWMWA_USE_IMMERSIVE_DARK_MODE,
      ref useDarkMode,
      sizeof(int)
    );

    if (result != 0)
    {
      _ = DwmSetWindowAttribute(
        Handle,
        DWMWA_USE_IMMERSIVE_DARK_MODE_OLD,
        ref useDarkMode,
        sizeof(int)
      );
    }
  }

  private async void MainForm_Shown(object? sender, EventArgs e)
  {
    await CheckForUpdatesAsync();
  }

  private async Task CheckForUpdatesAsync()
  {
    try
    {
      SetStatusWorking("Prüfe Updates ...");

      UpdateResult update = await UpdateChecker.CheckForUpdateAsync();

      if (update.IsUpdateAvailable)
      {
        _versionLabel.Text = $"v{update.CurrentVersion} → v{update.LatestVersion}";
        SetStatusWorking("Update verfügbar");

        DialogResult result = MessageBox.Show(
          this,
          $"Eine neue Version ist verfügbar!\n\nAktuell: v{update.CurrentVersion}\nNeu: v{update.LatestVersion}\n\nMöchtest du das Release öffnen?",
          "Update verfügbar",
          MessageBoxButtons.YesNo,
          MessageBoxIcon.Information
        );

        if (result == DialogResult.Yes)
          UpdateChecker.OpenReleasePage();
      }
      else
      {
        _versionLabel.Text = $"v{update.CurrentVersion}";
        SetStatusReady("Bereit");
      }
    }
    catch
    {
      SetStatusReady("Bereit");
    }
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
    Controls.Add(CreateStatusFooter());
    Controls.Add(CreateHeader());

    Panel bpCard = CreateCard("Behavior Pack", "BP Ordner, ZIP, MCPACK oder MCADDON auswählen", Properties.Resources.bp_icon, new Point(18, 72), new Size(430, 175));
    BuildPackSelector(bpCard, isBehaviorPack: true);
    Controls.Add(bpCard);

    Panel rpCard = CreateCard("Resource Pack", "RP Ordner, ZIP, MCPACK oder MCADDON auswählen", Properties.Resources.rp_icon, new Point(472, 72), new Size(430, 175));
    BuildPackSelector(rpCard, isBehaviorPack: false);
    Controls.Add(rpCard);

    Panel outputCard = CreateCard("Ausgabe", "Name, Version und Speicherort der .mcaddon", Properties.Resources.folder_icon, new Point(18, 260), new Size(430, 218));
    BuildOutputArea(outputCard);
    _packageNameText.TextChanged += (_, _) => UpdateFileNamePreview();
    _versionText.TextChanged += (_, _) => UpdateFileNamePreview();
    Controls.Add(outputCard);

    Panel logCard = CreateCard("Build Log", "Hier siehst du, was der Launcher macht", Properties.Resources.log_icon, new Point(472, 260), new Size(430, 218));
    BuildLogArea(logCard);
    Controls.Add(logCard);

    _buildButton.Text = "MCADDON ERSTELLEN";
    _buildButton.Location = new Point(25, 495);
    _buildButton.Size = new Size(867, 40);
    StylePrimaryButton(_buildButton);
    _buildButton.Click += BuildButton_Click;
    Controls.Add(_buildButton);
  }

  private Panel CreateHeader()
  {
    Panel header = new()
    {
      Location = new Point(0, 0),
      Size = new Size(ClientSize.Width, 72),
      Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right,
      BackColor = Color.Transparent
    };

    PictureBox logo = new()
    {
      Image = Properties.Resources.app_icon_trans,
      SizeMode = PictureBoxSizeMode.Zoom,
      Location = new Point(24, 10),
      Size = new Size(55, 55),
      BackColor = Color.Transparent
    };

    Label title = new()
    {
      Text = "MC Addon Ersteller",
      AutoSize = true,
      ForeColor = Color.White,
      Font = new Font(FontManager.Noto, 13F, FontStyle.Regular, GraphicsUnit.Point),
      Location = new Point(85, 15),
      BackColor = Color.Transparent
    };

    Label subtitle = new()
    {
      Text = "BP/RP aus Ordnern oder ZIP-Dateien sauber zu einer .mcaddon packen",
      AutoSize = true,
      ForeColor = Color.FromArgb(210, 224, 245),
      Font = new Font(FontManager.Metropolis, 8F, FontStyle.Regular, GraphicsUnit.Point),
      Location = new Point(87, 40),
      BackColor = Color.Transparent
    };

    MinecraftButton tag = new()
    {
      Text = "MINECRAFT BEDROCK",
      ForeColor = Color.FromArgb(40, 40, 40),
      BackColor = Color.FromArgb(214, 214, 214),
      BorderLightColor = Color.FromArgb(230, 230, 230),
      BorderDarkColor = Color.FromArgb(190, 190, 190),
      TextAlign = ContentAlignment.MiddleCenter,
      Font = new Font(FontManager.Metropolis, 8F, FontStyle.Bold, GraphicsUnit.Point),
      Location = new Point(ClientSize.Width - 188, 24),
      Size = new Size(164, 28),
      Anchor = AnchorStyles.Top | AnchorStyles.Right,
      Enabled = false
    };

    header.Controls.Add(logo);
    header.Controls.Add(title);
    header.Controls.Add(subtitle);
    header.Controls.Add(tag);

    return header;
  }

  private Panel CreateStatusFooter()
  {
    Panel footer = new()
    {
      Location = new Point(0, ClientSize.Height - 28),
      Size = new Size(ClientSize.Width, 28),
      Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom,
      BackColor = Color.Transparent
    };

    Panel topLine = new()
    {
      Location = new Point(0, 0),
      Size = new Size(footer.Width, 1),
      Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top,
      BackColor = Color.FromArgb(55, 58, 62)
    };

    Panel bottomLine = new()
    {
      Location = new Point(0, footer.Height - 1),
      Size = new Size(footer.Width, 1),
      Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom,
      BackColor = Color.FromArgb(18, 19, 22)
    };

    _statusDot.Text = "●";
    _statusDot.ForeColor = Color.FromArgb(70, 220, 120);
    _statusDot.BackColor = Color.Transparent;
    _statusDot.Location = new Point(14, 5);
    _statusDot.Size = new Size(16, 18);
    _statusDot.Font = new Font(FontManager.Metropolis, 9F, FontStyle.Bold);

    _statusLabel.Text = "Bereit";
    _statusLabel.ForeColor = Color.FromArgb(228, 232, 240);
    _statusLabel.BackColor = Color.Transparent;
    _statusLabel.Location = new Point(34, 6);
    _statusLabel.Size = new Size(500, 18);
    _statusLabel.Font = new Font(FontManager.Metropolis, 8.5F, FontStyle.Regular);

    _versionLabel.Text = $"v{GetAppVersion()}";
    _versionLabel.ForeColor = Color.FromArgb(150, 163, 184);
    _versionLabel.BackColor = Color.Transparent;
    _versionLabel.TextAlign = ContentAlignment.MiddleRight;
    _versionLabel.Location = new Point(ClientSize.Width - 120, 6);
    _versionLabel.Size = new Size(100, 18);
    _versionLabel.Anchor = AnchorStyles.Top | AnchorStyles.Right;
    _versionLabel.Font = new Font(FontManager.Metropolis, 8.5F, FontStyle.Regular);

    footer.Controls.Add(topLine);
    footer.Controls.Add(bottomLine);
    footer.Controls.Add(_statusDot);
    footer.Controls.Add(_statusLabel);
    footer.Controls.Add(_versionLabel);

    return footer;
  }

  private static BorderPanel CreateCard(string title, string subtitle, Image icon, Point location, Size size)
  {
    const int padding = 5;
    const int iconSize = 60;
    const int headerHeight = 66;

    BorderPanel card = new()
    {
      Location = location,
      Size = size,
      BackColor = CardBack,
      BorderColor = BorderColor,
      BorderWidth = 1
    };

    PictureBox iconBox = new()
    {
      Image = icon,
      SizeMode = PictureBoxSizeMode.Zoom,
      Location = new Point(padding, 6),
      Size = new Size(iconSize, iconSize),
      BackColor = Color.Transparent
    };

    Label titleLabel = new()
    {
      Text = title,
      ForeColor = TextMain,
      Font = new Font(FontManager.Metropolis, 11.5F, FontStyle.Bold, GraphicsUnit.Point),
      Location = new Point(iconSize, 14),
      AutoSize = true,
      BackColor = Color.Transparent
    };

    Label subtitleLabel = new()
    {
      Text = subtitle,
      ForeColor = TextMuted,
      Font = new Font(FontManager.Metropolis, 8.5F, FontStyle.Regular, GraphicsUnit.Point),
      Location = new Point(iconSize + 1, 35),
      AutoSize = true,
      BackColor = Color.Transparent
    };

    Panel line = new()
    {
      Location = new Point(padding + 10, headerHeight),
      Size = new Size(size.Width - padding * 6, 1),
      BackColor = Color.FromArgb(52, 54, 58)
    };

    card.Controls.Add(iconBox);
    card.Controls.Add(titleLabel);
    card.Controls.Add(subtitleLabel);
    card.Controls.Add(line);

    return card;
  }

  private void BuildPackSelector(Panel parent, bool isBehaviorPack)
  {
    MinecraftCheckBox check = isBehaviorPack ? _includeBpCheck : _includeRpCheck;
    MinecraftTextBox path = isBehaviorPack ? _bpPathText : _rpPathText;
    Label info = isBehaviorPack ? _bpInfoLabel : _rpInfoLabel;

    check.Text = "";
    check.Location = new Point(16, 75);
    check.Size = new Size(18, 24);
    check.BackColor = CardBack;
    parent.Controls.Add(check);

    Label checkLabel = new()
    {
      Text = isBehaviorPack ? "BP benutzen" : "RP benutzen",
      Location = new Point(36, 80),
      Size = new Size(120, 24),
      ForeColor = TextMain,
      BackColor = Color.Transparent,
      Font = new Font(FontManager.Metropolis, 8.5F, FontStyle.Regular, GraphicsUnit.Point)
    };

    checkLabel.Click += (_, _) => check.Checked = !check.Checked;

    parent.Controls.Add(checkLabel);

    path.Location = new Point(16, 105);
    path.Size = new Size(300, 25);
    path.ReadOnly = true;
    path.PlaceholderText = "Noch nichts ausgewählt";
    StyleTextBox(path);
    parent.Controls.Add(path);

    Button folderButton = CreateSmallButton("Ordner", new Point(326, 105), new Size(88, 25));
    folderButton.Click += (_, _) => BrowseFolder(isBehaviorPack);
    parent.Controls.Add(folderButton);

    Button fileButton = CreateSmallButton("ZIP", new Point(326, 135), new Size(42, 25));
    fileButton.Click += (_, _) => BrowseFile(isBehaviorPack);
    parent.Controls.Add(fileButton);

    Button clearButton = CreateSmallButton("X", new Point(372, 135), new Size(42, 25));
    clearButton.Click += (_, _) => ClearPack(isBehaviorPack);
    parent.Controls.Add(clearButton);

    info.Text = "Manifest: -";
    info.Location = new Point(16, 143);
    info.Size = new Size(292, 34);
    info.ForeColor = TextMuted;
    info.BackColor = Color.Transparent;
    info.Font = new Font(FontManager.Metropolis, 8.4F, FontStyle.Regular, GraphicsUnit.Point);
    parent.Controls.Add(info);
  }

  private void BuildOutputArea(Panel parent)
  {
    AddFieldLabel(parent, "Addon Name", new Point(16, 80));

    _packageNameText.Location = new Point(112, 76);
    _packageNameText.Size = new Size(300, 25);
    _packageNameText.PlaceholderText = "z.B. MeinAddon";
    StyleTextBox(_packageNameText);
    parent.Controls.Add(_packageNameText);

    AddFieldLabel(parent, "Version", new Point(16, 113));

    _versionText.Location = new Point(112, 110);
    _versionText.Size = new Size(80, 25);
    _versionText.PlaceholderText = "1.0.0";
    StyleTextBox(_versionText);
    parent.Controls.Add(_versionText);

    Label fileNameLabel = new()
    {
      Text = "Dateiname:",
      Location = new Point(202, 115),
      Size = new Size(72, 22),
      ForeColor = TextMuted,
      BackColor = Color.Transparent,
      Font = new Font(FontManager.Metropolis, 8F, FontStyle.Regular, GraphicsUnit.Point)
    };

    parent.Controls.Add(fileNameLabel);

    _fileNamePreviewText.Location = new Point(274, 110);
    _fileNamePreviewText.Size = new Size(138, 25);
    _fileNamePreviewText.ReadOnly = true;
    StyleTextBox(_fileNamePreviewText);
    parent.Controls.Add(_fileNamePreviewText);

    AddFieldLabel(parent, "Ausgabe", new Point(16, 150));

    _outputPathText.Location = new Point(112, 145);
    _outputPathText.Size = new Size(225, 25);
    _outputPathText.ReadOnly = true;
    StyleTextBox(_outputPathText);
    parent.Controls.Add(_outputPathText);

    Button outputButton = CreateSmallButton("Wählen", new Point(345, 144), new Size(66, 27));
    outputButton.Click += (_, _) => BrowseOutputFolder();
    parent.Controls.Add(outputButton);

    Label bottomHint = new()
    {
      Text = "Die originalen BP/RP Dateien werden nicht verändert.",
      Location = new Point(16, 190),
      Size = new Size(388, 22),
      ForeColor = Color.FromArgb(95, 170, 235),
      BackColor = Color.Transparent,
      Font = new Font(FontManager.Metropolis, 8F, FontStyle.Regular, GraphicsUnit.Point)
    };

    parent.Controls.Add(bottomHint);
  }

  private void BuildLogArea(Panel parent)
  {
    _buildProgressLabel.Text = "Fortschritt: 0%";
    _buildProgressLabel.Location = new Point(16, 62);
    _buildProgressLabel.Size = new Size(160, 20);
    _buildProgressLabel.ForeColor = TextMuted;
    _buildProgressLabel.BackColor = Color.Transparent;
    _buildProgressLabel.Font = new Font(FontManager.Metropolis, 8.6F, FontStyle.Bold, GraphicsUnit.Point);
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
    _logText.BackColor = Color.FromArgb(16, 17, 20);
    _logText.ForeColor = Color.FromArgb(235, 238, 242);
    _logText.BorderStyle = BorderStyle.FixedSingle;
    _logText.Font = new Font("Consolas", 8.6F, FontStyle.Regular, GraphicsUnit.Point);
    parent.Controls.Add(_logText);
  }

  private static void AddFieldLabel(Control parent, string text, Point location)
  {
    Label label = new()
    {
      Text = text + ":",
      Location = location,
      Size = new Size(94, 22),
      ForeColor = TextMuted,
      BackColor = Color.Transparent,
      Font = new Font(FontManager.Metropolis, 8F, FontStyle.Regular, GraphicsUnit.Point)
    };

    parent.Controls.Add(label);
  }

  private static MinecraftButton CreateSmallButton(string text, Point location, Size size)
  {
    MinecraftButton button = new()
    {
      Text = text,
      Location = location,
      Size = size,
      Font = new Font(FontManager.Metropolis, 8.4F, FontStyle.Bold, GraphicsUnit.Point),
      ForeColor = Color.FromArgb(40, 40, 40),
      BackColor = Color.FromArgb(214, 214, 214),
      BorderLightColor = Color.FromArgb(230, 230, 230),
      BorderDarkColor = Color.FromArgb(190, 190, 190),
      BorderSize = 3,
      Cursor = Cursors.Hand
    };

    return button;
  }

  private static void StylePrimaryButton(Button button)
  {
    button.FlatStyle = FlatStyle.Flat;
    button.FlatAppearance.BorderSize = 0;
    button.BackColor = Color.FromArgb(91, 178, 88);      // Minecraft Grün
    button.ForeColor = Color.White;
    button.Cursor = Cursors.Hand;
    button.Font = new Font(FontManager.Metropolis, 11F, FontStyle.Bold, GraphicsUnit.Point);

    if (button is MinecraftButton mcButton)
    {
      mcButton.BorderLightColor = Color.FromArgb(115, 200, 105);
      mcButton.BorderDarkColor = Color.FromArgb(48, 105, 48);
      mcButton.BorderSize = 4;
    }
  }

  private static void StyleTextBox(MinecraftTextBox textBox)
  {
    textBox.BackColor = Color.FromArgb(16, 17, 20);
    textBox.ForeColor = TextMain;
    textBox.BorderStyle = BorderStyle.FixedSingle;
    textBox.Font = new Font(FontManager.Metropolis, 8.5F, FontStyle.Regular, GraphicsUnit.Point);
  }

  private void SetDefaults()
  {
    _packageNameText.Text = "MeinAddon";
    _versionText.Text = "1.0.0";

    string desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);

    _outputPathText.Text = string.IsNullOrWhiteSpace(desktop)
      ? Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
      : desktop;

    Log("Bereit. Wähle BP/RP als Ordner oder ZIP aus.");
  }

  private void UpdateFileNamePreview()
  {
    if (_fileNamePreviewText.IsDisposed)
      return;

    string safeName = FileNameTools.ToSafeFileName(_packageNameText.Text, "MeinAddon");
    string safeVersion = FileNameTools.VersionForFileName(_versionText.Text);

    _fileNamePreviewText.Text = $"{safeName}_v{safeVersion}.mcaddon";
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
      SelectedPath = Directory.Exists(_outputPathText.Text)
        ? _outputPathText.Text
        : Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory)
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
      SetStatusWorking("Erstelle MCADDON ...");

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
        Status = text => SetStatusTextOnly(text),
        StepDelayMilliseconds = 180,
        Progress = new Progress<double>(SetBuildProgress)
      };

      string outputPath = await McAddonBuilder.BuildAsync(options);

      SetBuildProgress(100);
      SetStatusReady("Fertig.");

      MessageBox.Show(this, $"MCADDON wurde erstellt:\n\n{outputPath}", "Fertig", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }
    catch (Exception ex)
    {
      SetStatusError("Fehler.");
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

    _buildProgress.Value = progress;
    _buildProgressLabel.Text = $"Fortschritt: {progress}%";
  }

  private void SetStatusTextOnly(string text)
  {
    if (InvokeRequired)
    {
      BeginInvoke(new Action(() => SetStatusTextOnly(text)));
      return;
    }

    _statusLabel.Text = text;
  }

  private void SetStatusReady(string text)
  {
    if (InvokeRequired)
    {
      BeginInvoke(new Action(() => SetStatusReady(text)));
      return;
    }

    _statusLabel.Text = text;
    _statusDot.ForeColor = Color.FromArgb(70, 220, 120);
  }

  private void SetStatusWorking(string text)
  {
    if (InvokeRequired)
    {
      BeginInvoke(new Action(() => SetStatusWorking(text)));
      return;
    }

    _statusLabel.Text = text;
    _statusDot.ForeColor = Color.FromArgb(245, 190, 70);
  }

  private void SetStatusError(string text)
  {
    if (InvokeRequired)
    {
      BeginInvoke(new Action(() => SetStatusError(text)));
      return;
    }

    _statusLabel.Text = text;
    _statusDot.ForeColor = Color.FromArgb(240, 90, 90);
  }

  private void UpdateStatus(string text)
  {
    if (text.Contains("Fehler", StringComparison.OrdinalIgnoreCase))
      SetStatusError(text);
    else
      SetStatusReady(text);
  }

  private static string GetAppVersion()
  {
    return Assembly
      .GetExecutingAssembly()
      .GetName()
      .Version?
      .ToString(3) ?? "1.0.2";
  }

  private sealed class LineStatusRenderer : ToolStripProfessionalRenderer
  {
    protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
    {
      e.Graphics.Clear(Color.Transparent);
    }

    protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
    {
      Rectangle r = e.ToolStrip.ClientRectangle;

      using Pen topLine = new(Color.FromArgb(65, 68, 74));
      using Pen bottomLine = new(Color.FromArgb(18, 19, 22));

      e.Graphics.DrawLine(topLine, 0, 0, r.Width, 0);
      e.Graphics.DrawLine(bottomLine, 0, r.Height - 1, r.Width, r.Height - 1);
    }
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