
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using MaxibackCleaner.Models;
using MaxibackCleaner.Services;

namespace MaxibackCleaner
{
    public partial class Form1 : Form
    {
        private readonly TaskSchedulerService taskScheduler = new();

        private AppSettings settings;
        private AppLogger logger;
        private CleanerService cleaner;

        private TextBox txtFolder = null!;
        private SegmentedSelector cmbMonths = null!;
        private Label lblStatus = null!;
        private Label lblFrequencyInfo = null!;
        private NotifyIcon trayIcon = null!;
        private System.Windows.Forms.Timer timer = null!;

        private Button btnSave = null!;
        private Button btnClean = null!;
        private Button btnTemp = null!;

        private Button btnCleaningNav = null!;
        private Button btnOptimizationNav = null!;
        private Button btnDiagnosticsNav = null!;
        private Button btnUsbBootableNav = null!;

        private Panel sidebar = null!;
        private Panel contentPanel = null!;
        private Panel cleaningView = null!;
        private Panel usbBootableView = null!;
        private WindowsOptimizationForm optimizationView = null!;

        private bool exitRequested;

        private readonly string frequencyFile = Path.Combine(
            Environment.GetFolderPath(
                Environment.SpecialFolder.LocalApplicationData),
            "MaxibackCleaner",
            "frequency.txt");

        private readonly Color backgroundColor = Color.FromArgb(15, 18, 25);
        private readonly Color sidebarColor = Color.FromArgb(20, 24, 33);
        private readonly Color cardColor = Color.FromArgb(27, 32, 43);
        private readonly Color inputColor = Color.FromArgb(36, 42, 55);
        private readonly Color textColor = Color.FromArgb(235, 239, 246);
        private readonly Color mutedColor = Color.FromArgb(153, 164, 181);
        private readonly Color accentColor = Color.FromArgb(63, 132, 245);
        private readonly Color successColor = Color.FromArgb(50, 190, 130);
        private readonly Color dangerColor = Color.FromArgb(220, 80, 90);

        public Form1()
        {
            InitializeComponent();

            Icon = LoadAppIcon();

            settings = AppSettings.Load();
            logger = new AppLogger();
            cleaner = new CleanerService(logger);

            Directory.CreateDirectory(
                Path.GetDirectoryName(frequencyFile)!);

            BuildInterface();
            ConfigureTray();
            ConfigureTimer();
            CheckUpcomingCleanup();
        }

        private static Icon LoadAppIcon()
        {
            try
            {
                string path = Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    "logo.ico");

                if (File.Exists(path))
                    return new Icon(path);

                Icon? exeIcon =
                    Icon.ExtractAssociatedIcon(Application.ExecutablePath);

                if (exeIcon != null)
                    return exeIcon;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(
                    "No se pudo cargar el icono: " + ex.Message);
            }

            return SystemIcons.Application;
        }

        private void BuildInterface()
        {
            Text = "Maxiback Optimize";
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new Size(900, 620);
            Size = new Size(1120, 740);
            BackColor = backgroundColor;
            ForeColor = textColor;
            Font = new Font("Segoe UI", 10F);
            AutoScaleMode = AutoScaleMode.Dpi;

            Controls.Clear();

            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = backgroundColor,
                Margin = Padding.Empty,
                Padding = Padding.Empty
            };

            root.ColumnStyles.Add(
                new ColumnStyle(SizeType.Absolute, 220));

            root.ColumnStyles.Add(
                new ColumnStyle(SizeType.Percent, 100));

            root.RowStyles.Add(
                new RowStyle(SizeType.Percent, 100));

            sidebar = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = sidebarColor,
                Padding = new Padding(18, 24, 18, 18)
            };

            contentPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = backgroundColor,
                Padding = Padding.Empty
            };

            root.Controls.Add(sidebar, 0, 0);
            root.Controls.Add(contentPanel, 1, 0);
            Controls.Add(root);

            BuildSidebar();
            BuildMainContent();
            ShowSection("cleaning");
        }

        private void BuildSidebar()
        {
            sidebar.Controls.Clear();

            var logo = new PictureBox
            {
                Dock = DockStyle.Top,
                Height = 105,
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = sidebarColor,
                Margin = Padding.Empty
            };

            string logoPath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "logo.jpg");

            if (File.Exists(logoPath))
            {
                try
                {
                    using Image original = Image.FromFile(logoPath);
                    logo.Image = new Bitmap(original);
                }
                catch (Exception ex)
                {
                    logger.Write(
                        "No se pudo cargar logo.jpg: " + ex.Message);
                }
            }

            var brand = new Label
            {
                Text = "MAXIBACK\nOPTIMIZE",
                Dock = DockStyle.Top,
                Height = 65,
                Font = new Font("Segoe UI", 17F, FontStyle.Bold),
                ForeColor = textColor,
                TextAlign = ContentAlignment.MiddleCenter
            };

            var brandLine = new Panel
            {
                Dock = DockStyle.Top,
                Height = 2,
                BackColor = accentColor
            };

            var navTitle = new Label
            {
                Text = "MENÚ PRINCIPAL",
                Dock = DockStyle.Top,
                Height = 38,
                ForeColor = mutedColor,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft
            };

            var nav = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 226,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = false,
                BackColor = sidebarColor,
                Margin = Padding.Empty,
                Padding = Padding.Empty
            };

            btnCleaningNav = CreateNavButton("   Limpieza");
            btnOptimizationNav =
                CreateNavButton("   Optimización de Windows");
            btnDiagnosticsNav =
                CreateNavButton("   Diagnóstico del sistema");
            btnUsbBootableNav =
                CreateNavButton("   USB Booteable");

            btnCleaningNav.Click += (_, _) => ShowSection("cleaning");
            btnOptimizationNav.Click += (_, _) => ShowSection("optimization");
            btnDiagnosticsNav.Click += (_, _) => ShowSection("diagnostics");
            btnUsbBootableNav.Click += (_, _) => ShowSection("usb");

            nav.Controls.Add(btnCleaningNav);
            nav.Controls.Add(btnOptimizationNav);
            nav.Controls.Add(btnDiagnosticsNav);
            nav.Controls.Add(btnUsbBootableNav);

            var sidebarFooter = new Label
            {
                Text = "Limpieza y mantenimiento\nVersión de escritorio",
                Dock = DockStyle.Bottom,
                Height = 52,
                ForeColor = mutedColor,
                Font = new Font("Segoe UI", 8.5F),
                TextAlign = ContentAlignment.BottomLeft
            };

            var githubLink = new LinkLabel
            {
                Text = "GitHub: gabmorenoxbx",
                Dock = DockStyle.Bottom,
                Height = 22,
                ForeColor = Color.FromArgb(100, 160, 255),
                LinkColor = Color.FromArgb(100, 160, 255),
                ActiveLinkColor = Color.White,
                VisitedLinkColor = Color.FromArgb(100, 160, 255),
                Font = new Font("Segoe UI", 8.5F),
                TextAlign = ContentAlignment.MiddleLeft,
                LinkBehavior = LinkBehavior.HoverUnderline
            };

            githubLink.LinkClicked += (_, _) =>
            {
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "https://github.com/gabmorenoxbx",
                        UseShellExecute = true
                    });
                }
                catch (Exception ex)
                {
                    logger.Write(
                        "No se pudo abrir GitHub: " + ex.Message);
                }
            };

            sidebar.Controls.Add(sidebarFooter);
            sidebar.Controls.Add(githubLink);
            sidebar.Controls.Add(nav);
            sidebar.Controls.Add(navTitle);
            sidebar.Controls.Add(brandLine);
            sidebar.Controls.Add(brand);
            sidebar.Controls.Add(logo);
        }

        private Button CreateNavButton(string text)
        {
            var button = new Button
            {
                Text = text,
                Width = 184,
                Height = 46,
                Margin = new Padding(0, 0, 0, 7),
                FlatStyle = FlatStyle.Flat,
                TextAlign = ContentAlignment.MiddleLeft,
                BackColor = sidebarColor,
                ForeColor = textColor,
                Font = new Font("Segoe UI", 9.2F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                UseVisualStyleBackColor = false
            };

            button.FlatAppearance.BorderSize = 0;
            button.FlatAppearance.MouseOverBackColor =
                Color.FromArgb(35, 48, 70);

            return button;
        }

        private void ShowSection(string section)
        {
            bool showCleaning = section == "cleaning";
            bool showOptimization = section == "optimization";
            bool showDiagnostics = section == "diagnostics";
            bool showUsb = section == "usb";

            cleaningView.Visible = showCleaning;
            optimizationView.Visible = showOptimization || showDiagnostics;
            usbBootableView.Visible = showUsb;

            if (showOptimization)
                optimizationView.ShowSection("optimization");
            else if (showDiagnostics)
                optimizationView.ShowSection("diagnostics");

            SetNavButtonState(btnCleaningNav, showCleaning);
            SetNavButtonState(btnOptimizationNav, showOptimization);
            SetNavButtonState(btnDiagnosticsNav, showDiagnostics);
            SetNavButtonState(btnUsbBootableNav, showUsb);

            if (showCleaning)
            {
                cleaningView.BringToFront();
                contentPanel.Controls.SetChildIndex(cleaningView, 0);
                txtFolder.Focus();
            }
            else if (showUsb)
            {
                usbBootableView.BringToFront();
                contentPanel.Controls.SetChildIndex(usbBootableView, 0);
            }
            else
            {
                optimizationView.BringToFront();
                contentPanel.Controls.SetChildIndex(optimizationView, 0);
            }
        }

        private void SetNavButtonState(Button button, bool selected)
        {
            button.BackColor = selected
                ? Color.FromArgb(35, 48, 70)
                : sidebarColor;

            button.ForeColor = selected ? Color.White : textColor;
        }

        private void BuildMainContent()
        {
            contentPanel.Controls.Clear();

            cleaningView = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = backgroundColor,
                Padding = new Padding(30, 26, 30, 20),
                AutoScroll = true
            };

            var main = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 1,
                RowCount = 4,
                BackColor = backgroundColor,
                Margin = Padding.Empty,
                Padding = Padding.Empty
            };

            main.ColumnStyles.Add(
                new ColumnStyle(SizeType.Percent, 100));

            for (int i = 0; i < 4; i++)
                main.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            var header = new Panel
            {
                Dock = DockStyle.Top,
                Height = 78,
                BackColor = backgroundColor,
                Margin = new Padding(0, 0, 0, 18)
            };

            header.Controls.Add(new Label
            {
                Text = "Limpieza",
                Location = new Point(0, 0),
                AutoSize = true,
                Font = new Font("Segoe UI", 23F, FontStyle.Bold),
                ForeColor = textColor
            });

            header.Controls.Add(new Label
            {
                Text = "Administrá la limpieza automática de archivos y temporales.",
                Location = new Point(2, 43),
                AutoSize = true,
                Font = new Font("Segoe UI", 9.5F),
                ForeColor = mutedColor
            });

            var folderCard = CreateCard(
                "CARPETA A LIMPIAR",
                "Elegí la carpeta donde se encuentran los archivos que querés depurar.");

            var folderLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = cardColor,
                Margin = new Padding(0, 15, 0, 0)
            };

            folderLayout.ColumnStyles.Add(
                new ColumnStyle(SizeType.Percent, 100));

            folderLayout.ColumnStyles.Add(
                new ColumnStyle(SizeType.Absolute, 120));

            txtFolder = new TextBox
            {
                Dock = DockStyle.Top,
                Height = 34,
                Text = settings.FolderPath ?? "",
                BackColor = inputColor,
                ForeColor = textColor,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 10F),
                Margin = new Padding(0, 0, 10, 0)
            };

            var btnBrowse = CreateButton("Examinar", accentColor);
            btnBrowse.Dock = DockStyle.Top;
            btnBrowse.Height = 36;
            btnBrowse.Click += BtnBrowse_Click;

            folderLayout.Controls.Add(txtFolder, 0, 0);
            folderLayout.Controls.Add(btnBrowse, 1, 0);
            folderCard.Controls.Add(folderLayout);

            var retentionCard = CreateCard(
                "ANTIGÜEDAD DE LOS ARCHIVOS",
                "Esta antigüedad se aplica a la limpieza automática. " +
                "Limpiar ahora vacía inmediatamente toda la carpeta seleccionada.");

            cmbMonths = new SegmentedSelector(
                inputColor,
                accentColor,
                textColor)
            {
                Dock = DockStyle.Top,
                BackColor = cardColor,
                Margin = new Padding(0, 15, 0, 0)
            };

            cmbMonths.SetItems(
                "60 min",
                "24 h",
                "7 días",
                "1 mes",
                "3 meses",
                "6 meses",
                "1 año");

            cmbMonths.SelectedItem = LoadFrequencyLabel();

            if (cmbMonths.SelectedIndex < 0)
                cmbMonths.SelectedIndex = 4;

            lblFrequencyInfo = new Label
            {
                Text = "",
                Dock = DockStyle.Top,
                AutoSize = true,
                ForeColor = mutedColor,
                Font = new Font("Segoe UI", 9F),
                Margin = new Padding(0, 12, 0, 0)
            };

            cmbMonths.SelectionChanged += (_, _) => UpdateRetentionInfo();
            UpdateRetentionInfo();

            retentionCard.Controls.Add(lblFrequencyInfo);
            retentionCard.Controls.Add(cmbMonths);

            var actionCard = CreateCard(
                "ACCIONES",
                "Guardá la configuración automática o ejecutá una limpieza manual.");

            var actionLayout = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                WrapContents = true,
                FlowDirection = FlowDirection.LeftToRight,
                BackColor = cardColor,
                Margin = new Padding(0, 15, 0, 0)
            };

            btnSave = CreateButton("Guardar configuración", accentColor);
            btnSave.Width = 190;
            btnSave.Height = 42;
            btnSave.Click += BtnSave_Click;

            btnClean = CreateButton("Limpiar ahora", successColor);
            btnClean.Width = 150;
            btnClean.Height = 42;
            btnClean.Click += BtnClean_Click;

            btnTemp = CreateButton("Limpiar temporales", inputColor);
            btnTemp.Width = 180;
            btnTemp.Height = 42;
            btnTemp.ForeColor = textColor;
            btnTemp.Click += BtnTemp_Click;

            actionLayout.Controls.Add(btnSave);
            actionLayout.Controls.Add(btnClean);
            actionLayout.Controls.Add(btnTemp);
            actionCard.Controls.Add(actionLayout);

            var statusPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 48,
                BackColor = sidebarColor,
                Padding = new Padding(14, 0, 14, 0),
                Margin = new Padding(0, 4, 0, 0)
            };

            lblStatus = new Label
            {
                Text = "Listo.",
                Dock = DockStyle.Fill,
                ForeColor = mutedColor,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI", 9.5F)
            };

            statusPanel.Controls.Add(lblStatus);

            folderCard.Margin = new Padding(0, 0, 0, 16);
            retentionCard.Margin = new Padding(0, 0, 0, 16);
            actionCard.Margin = new Padding(0, 0, 0, 16);

            main.Controls.Add(header, 0, 0);
            main.Controls.Add(folderCard, 0, 1);
            main.Controls.Add(retentionCard, 0, 2);
            main.Controls.Add(actionCard, 0, 3);

            cleaningView.Controls.Add(main);
            cleaningView.Controls.Add(statusPanel);
            contentPanel.Controls.Add(cleaningView);

            optimizationView = new WindowsOptimizationForm
            {
                TopLevel = false,
                FormBorderStyle = FormBorderStyle.None,
                Dock = DockStyle.Fill,
                Visible = false
            };

            optimizationView.ShowSection("optimization");
            contentPanel.Controls.Add(optimizationView);
            optimizationView.Show();
            optimizationView.Visible = false;

            usbBootableView = BuildUsbBootableView();
            contentPanel.Controls.Add(usbBootableView);
            usbBootableView.Visible = false;
        }

        private Panel BuildUsbBootableView()
        {
            var view = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = backgroundColor,
                Padding = new Padding(30, 26, 30, 20),
                AutoScroll = true
            };

            var main = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 1,
                RowCount = 4,
                BackColor = backgroundColor,
                Margin = Padding.Empty,
                Padding = Padding.Empty
            };
            main.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            for (int i = 0; i < 4; i++)
                main.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            var header = new Panel
            {
                Dock = DockStyle.Top,
                Height = 78,
                BackColor = backgroundColor,
                Margin = new Padding(0, 0, 0, 18)
            };
            header.Controls.Add(new Label
            {
                Text = "USB Booteable",
                Location = new Point(0, 0),
                AutoSize = true,
                Font = new Font("Segoe UI", 23F, FontStyle.Bold),
                ForeColor = textColor
            });
            header.Controls.Add(new Label
            {
                Text = "Prepará una unidad USB de arranque a partir de una imagen ISO.",
                Location = new Point(2, 43),
                AutoSize = true,
                Font = new Font("Segoe UI", 9.5F),
                ForeColor = mutedColor
            });

            var deviceCard = CreateCard(
                "DISPOSITIVO USB",
                "Seleccioná cuidadosamente la unidad. El proceso real de creación todavía no está habilitado.");

            var deviceLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                ColumnCount = 2,
                RowCount = 2,
                BackColor = cardColor,
                Margin = new Padding(0, 15, 0, 0)
            };
            deviceLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            deviceLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));

            var cmbUsbDevice = new ComboBox
            {
                Name = "cmbUsbDevice",
                Dock = DockStyle.Top,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = inputColor,
                ForeColor = textColor,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10F),
                Margin = new Padding(0, 0, 10, 8)
            };
            cmbUsbDevice.Items.Add("Presioná Actualizar para buscar unidades USB");
            cmbUsbDevice.SelectedIndex = 0;

            var btnRefreshUsb = CreateButton("Actualizar", inputColor);
            btnRefreshUsb.ForeColor = textColor;
            btnRefreshUsb.Dock = DockStyle.Top;
            btnRefreshUsb.Height = 34;
            btnRefreshUsb.Click += (_, _) =>
            {
                cmbUsbDevice.Items.Clear();
                try
                {
                    foreach (DriveInfo drive in DriveInfo.GetDrives())
                    {
                        try
                        {
                            if (drive.IsReady && drive.DriveType == DriveType.Removable)
                            {
                                string label = string.IsNullOrWhiteSpace(drive.VolumeLabel)
                                    ? "Sin etiqueta"
                                    : drive.VolumeLabel;
                                cmbUsbDevice.Items.Add(
                                    $"{drive.Name}  |  {label}  |  {drive.TotalSize / (1024 * 1024 * 1024.0):0.0} GB");
                            }
                        }
                        catch (IOException) { }
                        catch (UnauthorizedAccessException) { }
                    }

                    if (cmbUsbDevice.Items.Count == 0)
                        cmbUsbDevice.Items.Add("No se detectaron unidades extraíbles");

                    cmbUsbDevice.SelectedIndex = 0;
                }
                catch (Exception ex)
                {
                    logger.Write("Error al buscar unidades USB: " + ex);
                    MessageBox.Show(this, ex.Message, "Error al buscar USB",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            var lblUsbWarning = new Label
            {
                Text = "Advertencia: crear un USB booteable puede borrar todos los datos de la unidad seleccionada.",
                Dock = DockStyle.Top,
                AutoSize = true,
                MaximumSize = new Size(760, 0),
                ForeColor = Color.FromArgb(255, 190, 95),
                Font = new Font("Segoe UI", 9F),
                Margin = new Padding(0, 8, 0, 0)
            };
            deviceLayout.Controls.Add(cmbUsbDevice, 0, 0);
            deviceLayout.Controls.Add(btnRefreshUsb, 1, 0);
            deviceLayout.Controls.Add(lblUsbWarning, 0, 1);
            deviceLayout.SetColumnSpan(lblUsbWarning, 2);
            deviceCard.Controls.Add(deviceLayout);

            var isoCard = CreateCard(
                "IMAGEN ISO",
                "Elegí el archivo ISO del sistema operativo que querés instalar.");

            var isoLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = cardColor,
                Margin = new Padding(0, 15, 0, 0)
            };
            isoLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            isoLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));

            var txtIsoPath = new TextBox
            {
                Name = "txtIsoPath",
                Dock = DockStyle.Top,
                ReadOnly = true,
                BackColor = inputColor,
                ForeColor = textColor,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 10F),
                Margin = new Padding(0, 0, 10, 0),
                PlaceholderText = "Seleccioná un archivo .iso..."
            };
            var btnBrowseIso = CreateButton("Elegir ISO", accentColor);
            btnBrowseIso.Dock = DockStyle.Top;
            btnBrowseIso.Height = 36;
            btnBrowseIso.Click += (_, _) =>
            {
                using var dialog = new OpenFileDialog
                {
                    Title = "Seleccionar imagen ISO",
                    Filter = "Imágenes ISO (*.iso)|*.iso|Todos los archivos (*.*)|*.*",
                    CheckFileExists = true,
                    Multiselect = false
                };
                if (dialog.ShowDialog(this) == DialogResult.OK)
                    txtIsoPath.Text = dialog.FileName;
            };
            isoLayout.Controls.Add(txtIsoPath, 0, 0);
            isoLayout.Controls.Add(btnBrowseIso, 1, 0);
            isoCard.Controls.Add(isoLayout);

            var optionsCard = CreateCard(
                "CONFIGURACIÓN DE ARRANQUE",
                "Los valores se muestran como opciones de configuración; todavía no modifican la unidad USB.");

            var options = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                ColumnCount = 2,
                RowCount = 4,
                BackColor = cardColor,
                Margin = new Padding(0, 15, 0, 0)
            };
            options.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            options.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

            ComboBox MakeCombo(string name, params string[] items)
            {
                var combo = new ComboBox
                {
                    Name = name,
                    Dock = DockStyle.Top,
                    DropDownStyle = ComboBoxStyle.DropDownList,
                    BackColor = inputColor,
                    ForeColor = textColor,
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Segoe UI", 9.5F),
                    Margin = new Padding(0, 4, 12, 12)
                };
                combo.Items.AddRange(items);
                if (combo.Items.Count > 0) combo.SelectedIndex = 0;
                return combo;
            }

            void AddOption(string labelText, Control control, int column, int row)
            {
                var wrapper = new Panel
                {
                    Dock = DockStyle.Fill,
                    AutoSize = true,
                    BackColor = cardColor,
                    Padding = new Padding(0, 0, 12, 0),
                    Margin = Padding.Empty
                };
                wrapper.Controls.Add(control);
                wrapper.Controls.Add(new Label
                {
                    Text = labelText,
                    Dock = DockStyle.Top,
                    AutoSize = true,
                    ForeColor = mutedColor,
                    Font = new Font("Segoe UI", 9F),
                    Margin = new Padding(0, 0, 0, 2)
                });
                options.Controls.Add(wrapper, column, row);
            }

            AddOption("Esquema de partición", MakeCombo("cmbPartition", "GPT", "MBR"), 0, 0);
            AddOption("Sistema de destino", MakeCombo("cmbTarget", "UEFI (no CSM)", "BIOS o UEFI-CSM"), 1, 0);
            AddOption("Sistema de archivos", MakeCombo("cmbFileSystem", "FAT32", "NTFS", "exFAT"), 0, 1);
            AddOption("Tamaño de clúster", MakeCombo("cmbClusterSize", "Predeterminado", "512 bytes", "1 KB", "2 KB", "4 KB", "8 KB", "16 KB", "32 KB", "64 KB"), 1, 1);
            AddOption("Etiqueta del volumen", new TextBox
            {
                Name = "txtVolumeLabel",
                Text = "MAXIBACK_USB",
                Dock = DockStyle.Top,
                BackColor = inputColor,
                ForeColor = textColor,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 10F),
                Margin = new Padding(0, 4, 12, 12)
            }, 0, 2);

            var lblModeNote = new Label
            {
                Text = "Compatibilidad entre ISO, FAT32/NTFS y UEFI/BIOS: se validará cuando se implemente el motor de creación.",
                Dock = DockStyle.Fill,
                ForeColor = mutedColor,
                Font = new Font("Segoe UI", 8.5F),
                AutoSize = true,
                MaximumSize = new Size(760, 0),
                Margin = new Padding(0, 5, 0, 12)
            };
            options.Controls.Add(lblModeNote, 1, 2);

            var actionCard = CreateCard(
                "CREACIÓN Y REGISTRO",
                "En esta versión solo está integrada la interfaz. No se formateará ni escribirá en ninguna unidad.");

            var progress = new ProgressBar
            {
                Name = "usbProgress",
                Dock = DockStyle.Top,
                Height = 18,
                Minimum = 0,
                Maximum = 100,
                Value = 0,
                Style = ProgressBarStyle.Continuous,
                Margin = new Padding(0, 12, 0, 10)
            };
            var lblProgress = new Label
            {
                Text = "En espera",
                Dock = DockStyle.Top,
                AutoSize = true,
                ForeColor = mutedColor,
                Font = new Font("Segoe UI", 9F),
                Margin = new Padding(0, 0, 0, 10)
            };
            var log = new ListBox
            {
                Name = "usbLog",
                Dock = DockStyle.Top,
                Height = 95,
                BackColor = inputColor,
                ForeColor = textColor,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Consolas", 9F),
                IntegralHeight = false,
                Margin = new Padding(0, 0, 0, 12)
            };
            log.Items.Add("Interfaz USB Booteable iniciada.");
            log.Items.Add("La creación real todavía no está implementada.");

            var btnCreateUsb = CreateButton("Crear USB booteable", successColor);
            btnCreateUsb.Width = 210;
            btnCreateUsb.Height = 42;
            btnCreateUsb.Click += (_, _) =>
            {
                MessageBox.Show(
                    this,
                    "La interfaz está preparada, pero la creación real todavía no está implementada. " +
                    "Por ahora no se borrará ni modificará ninguna unidad USB.",
                    "Función en desarrollo",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                log.Items.Add($"[{DateTime.Now:HH:mm:ss}] Solicitud de creación: función no habilitada.");
                log.TopIndex = log.Items.Count - 1;
            };

            actionCard.Controls.Add(btnCreateUsb);
            actionCard.Controls.Add(log);
            actionCard.Controls.Add(lblProgress);
            actionCard.Controls.Add(progress);

            deviceCard.Margin = new Padding(0, 0, 0, 16);
            isoCard.Margin = new Padding(0, 0, 0, 16);
            optionsCard.Margin = new Padding(0, 0, 0, 16);
            main.Controls.Add(header, 0, 0);
            main.Controls.Add(deviceCard, 0, 1);
            main.Controls.Add(isoCard, 0, 2);
            main.Controls.Add(optionsCard, 0, 3);
            main.Controls.Add(actionCard, 0, 4);
            main.RowCount = 5;
            main.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            view.Controls.Add(main);
            return view;
        }

        private Panel CreateCard(string heading, string description)
        {
            var card = new Panel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                BackColor = cardColor,
                Padding = new Padding(20),
                Margin = new Padding(0, 0, 0, 16)
            };

            var title = new Label
            {
                Text = heading,
                Dock = DockStyle.Top,
                AutoSize = true,
                ForeColor = textColor,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Margin = new Padding(0, 0, 0, 8)
            };

            var desc = new Label
            {
                Text = description,
                Dock = DockStyle.Top,
                AutoSize = true,
                MaximumSize = new Size(750, 0),
                ForeColor = mutedColor,
                Font = new Font("Segoe UI", 9F),
                Margin = new Padding(0, 8, 0, 0)
            };

            card.Controls.Add(desc);
            card.Controls.Add(title);

            return card;
        }

        private Button CreateButton(string text, Color color)
        {
            var button = new Button
            {
                Text = text,
                BackColor = color,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Padding = new Padding(10, 5, 10, 5),
                Margin = new Padding(0, 0, 10, 0),
                UseVisualStyleBackColor = false
            };

            button.FlatAppearance.BorderSize = 0;
            button.FlatAppearance.MouseOverBackColor =
                ControlPaint.Light(color, 0.12f);

            return button;
        }

        private void UpdateRetentionInfo()
        {
            string label = cmbMonths.SelectedItem ?? "3 meses";

            lblFrequencyInfo.Text =
                $"Limpieza automática: archivos con más de {label}. " +
                "Limpiar ahora vacía toda la carpeta inmediatamente.";
        }

        private string LoadFrequencyLabel()
        {
            try
            {
                if (File.Exists(frequencyFile))
                {
                    string key = File.ReadAllText(frequencyFile).Trim();

                    foreach (var pair in GetFrequencyMap())
                    {
                        if (pair.Value == key)
                            return pair.Key;
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Write(
                    "No se pudo leer la frecuencia guardada: " + ex.Message);
            }

            return settings.RetentionMonths switch
            {
                6 => "6 meses",
                12 => "1 año",
                1 => "1 mes",
                _ => "3 meses"
            };
        }

        private static Dictionary<string, string> GetFrequencyMap()
        {
            return new Dictionary<string, string>
            {
                ["60 min"] = "1hour",
                ["24 h"] = "1day",
                ["7 días"] = "1week",
                ["1 mes"] = "1month",
                ["3 meses"] = "3months",
                ["6 meses"] = "6months",
                ["1 año"] = "12months"
            };
        }

        private string GetSelectedFrequency()
        {
            string label = cmbMonths.SelectedItem ?? "3 meses";
            var map = GetFrequencyMap();

            return map.TryGetValue(label, out string? value)
                ? value
                : "3months";
        }

        private void SaveConfiguration()
        {
            string folder = txtFolder.Text.Trim();

            if (string.IsNullOrWhiteSpace(folder))
                throw new InvalidOperationException(
                    "Seleccioná una carpeta para limpiar.");

            if (!Directory.Exists(folder))
                throw new DirectoryNotFoundException(
                    "La carpeta seleccionada no existe: " + folder);

            settings.FolderPath = Path.GetFullPath(folder);

            settings.RetentionMonths = cmbMonths.SelectedItem switch
            {
                "6 meses" => 6,
                "1 año" => 12,
                "1 mes" => 1,
                "7 días" => 0,
                "24 h" => 0,
                "60 min" => 0,
                _ => 3
            };

            settings.Save();

            Directory.CreateDirectory(
                Path.GetDirectoryName(frequencyFile)!);

            File.WriteAllText(
                frequencyFile,
                GetSelectedFrequency());
        }

        private void BtnBrowse_Click(object? sender, EventArgs e)
        {
            using var dialog = new FolderBrowserDialog
            {
                Description = "Seleccioná la carpeta que querés limpiar",
                ShowNewFolderButton = false
            };

            if (Directory.Exists(txtFolder.Text))
                dialog.SelectedPath = txtFolder.Text;

            if (dialog.ShowDialog(this) == DialogResult.OK)
                txtFolder.Text = dialog.SelectedPath;
        }

        private void BtnSave_Click(object? sender, EventArgs e)
        {
            try
            {
                SaveConfiguration();
                taskScheduler.ConfigureTasks(GetSelectedFrequency());

                lblStatus.Text =
                    "Configuración guardada y tarea automática actualizada.";

                lblStatus.ForeColor = successColor;

                logger.Write(
                    "Configuración guardada. Ruta: " + settings.FolderPath +
                    ". Frecuencia: " + GetSelectedFrequency());

                MessageBox.Show(
                    this,
                    "La configuración se guardó correctamente.",
                    "Configuración guardada",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                lblStatus.Text = "No se pudo guardar la configuración.";
                lblStatus.ForeColor = dangerColor;

                logger.Write("Error al guardar configuración: " + ex);

                MessageBox.Show(
                    this,
                    ex.Message,
                    "Error al guardar",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }


        // LIMPIEZA MANUAL COMPLETA.
        // Elimina archivos y subcarpetas sin importar su antigüedad.
        // Conserva la carpeta principal seleccionada.

        private void BtnClean_Click(object? sender, EventArgs e)
        {
            string folder = txtFolder.Text.Trim();

            if (string.IsNullOrWhiteSpace(folder))
            {
                MessageBox.Show(
                    "Seleccioná una carpeta primero.",
                    "Maxiback Optimize",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            if (!Directory.Exists(folder))
            {
                MessageBox.Show(
                    "La carpeta seleccionada no existe.",
                    "Maxiback Optimize",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            DialogResult confirmation = MessageBox.Show(
                $"Se eliminará todo el contenido de esta carpeta:\n\n{folder}\n\n" +
                "Esta acción no se puede deshacer. La carpeta principal se conservará.\n\n¿Querés continuar?",
                "Confirmar limpieza completa",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (confirmation != DialogResult.Yes)
                return;

            try
            {
                Cursor = Cursors.WaitCursor;
                btnClean.Enabled = false;

                var result = cleaner.CleanAllContents(folder);

                string message =
                    $"Limpieza finalizada.\n\n" +
                    $"Elementos eliminados: {result.Deleted}\n" +
                    $"Elementos omitidos o con error: {result.Skipped}";

                lblStatus.Text = message.Replace("\n\n", " ");

                MessageBox.Show(
                    message,
                    "Resultado de limpieza",
                    MessageBoxButtons.OK,
                    result.Skipped > 0
                        ? MessageBoxIcon.Warning
                        : MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                logger.Write("Error en limpieza manual: " + ex);

                MessageBox.Show(
                    ex.ToString(),
                    "Error durante la limpieza",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            finally
            {
                Cursor = Cursors.Default;
                btnClean.Enabled = true;
            }
        }

        private void BtnTemp_Click(object? sender, EventArgs e)
        {
            RunTemporaryCleanup(true);
        }

        private void RunTemporaryCleanup(bool showConfirmation)
        {
            if (showConfirmation)
            {
                DialogResult answer = MessageBox.Show(
                    this,
                    "Se intentará eliminar archivos temporales de Windows " +
                    "y del usuario. Algunos archivos en uso no podrán eliminarse.\n\n" +
                    "¿Continuar?",
                    "Limpiar temporales",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);

                if (answer != DialogResult.Yes)
                    return;
            }

            try
            {
                var result = cleaner.CleanTemporaryFiles();

                lblStatus.Text =
                    $"Temporales eliminados: {result.Deleted}. " +
                    $"Omitidos: {result.Skipped}.";

                lblStatus.ForeColor =
                    result.Skipped > 0 ? dangerColor : successColor;

                if (showConfirmation)
                {
                    MessageBox.Show(
                        this,
                        $"Limpieza de temporales finalizada.\n\n" +
                        $"Eliminados: {result.Deleted}\n" +
                        $"Omitidos: {result.Skipped}",
                        "Resultado",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                lblStatus.Text =
                    "No se pudo completar la limpieza de temporales.";

                lblStatus.ForeColor = dangerColor;
                logger.Write("Error al limpiar temporales: " + ex);

                if (showConfirmation)
                {
                    MessageBox.Show(
                        this,
                        ex.Message,
                        "Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
            }
        }

        private static DateTime GetCutoff(string frequency)
        {
            return frequency switch
            {
                "1hour" => DateTime.Now.AddHours(-1),
                "1day" => DateTime.Now.AddDays(-1),
                "1week" => DateTime.Now.AddDays(-7),
                "1month" => DateTime.Now.AddMonths(-1),
                "3months" => DateTime.Now.AddMonths(-3),
                "6months" => DateTime.Now.AddMonths(-6),
                "12months" => DateTime.Now.AddMonths(-12),
                _ => DateTime.Now.AddMonths(-3)
            };
        }

        private void ConfigureTray()
        {
            var trayMenu = new ContextMenuStrip();

            trayMenu.Items.Add(
                "Abrir Maxiback Optimize",
                null,
                (_, _) => ShowWithAdminLogin());

            trayMenu.Items.Add(
                "Limpiar ahora",
                null,
                (_, _) => RunAutomaticCleanup());

            trayMenu.Items.Add(
                "Limpiar temporales",
                null,
                (_, _) => RunTemporaryCleanup(true));

            trayMenu.Items.Add(new ToolStripSeparator());

            trayMenu.Items.Add("Salir", null, (_, _) =>
            {
                exitRequested = true;

                if (trayIcon != null)
                    trayIcon.Visible = false;

                Close();
            });

            trayIcon = new NotifyIcon
            {
                Text = "Maxiback Optimize",
                Icon = Icon ?? SystemIcons.Application,
                ContextMenuStrip = trayMenu,
                Visible = true
            };

            trayIcon.DoubleClick += (_, _) => ShowWithAdminLogin();
        }

        private void ConfigureTimer()
        {
            timer = new System.Windows.Forms.Timer
            {
                Interval = 30 * 60 * 1000
            };

            timer.Tick += (_, _) => CheckUpcomingCleanup();
            timer.Start();
        }

        private void CheckUpcomingCleanup()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(settings.FolderPath) ||
                    !Directory.Exists(settings.FolderPath))
                    return;

                if (DateTime.TryParse(
                    settings.LastAlertDate,
                    out DateTime lastAlertDate) &&
                    lastAlertDate.Date == DateTime.Today)
                    return;

                string frequency = File.Exists(frequencyFile)
                    ? File.ReadAllText(frequencyFile).Trim()
                    : GetSelectedFrequency();

                if (!HasFilesReachingLimit(
                    settings.FolderPath,
                    GetCutoff(frequency)))
                    return;

                trayIcon?.ShowBalloonTip(
                    5000,
                    "Maxiback Optimize",
                    "Hay archivos que alcanzaron la antigüedad configurada. " +
                    "Podés ejecutar la limpieza.",
                    ToolTipIcon.Info);

                settings.LastAlertDate =
                    DateTime.Today.ToString("yyyy-MM-dd");

                settings.Save();
            }
            catch (Exception ex)
            {
                logger.Write(
                    "Error al revisar próxima limpieza: " + ex.Message);
            }
        }

        private bool HasFilesReachingLimit(
            string folder,
            DateTime cutoff)
        {
            try
            {
                foreach (string file in Directory.GetFiles(folder))
                {
                    if (File.GetLastWriteTime(file) <= cutoff)
                        return true;
                }

                foreach (string directory in Directory.GetDirectories(folder))
                {
                    var info = new DirectoryInfo(directory);

                    if ((info.Attributes & FileAttributes.ReparsePoint) != 0)
                        continue;

                    if (HasFilesReachingLimit(directory, cutoff))
                        return true;
                }
            }
            catch (UnauthorizedAccessException)
            {
            }
            catch (IOException)
            {
            }

            return false;
        }

        // Limpieza por antigüedad desde la bandeja del sistema.
        private void RunAutomaticCleanup()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(settings.FolderPath) ||
                    !Directory.Exists(settings.FolderPath))
                {
                    trayIcon?.ShowBalloonTip(
                        4000,
                        "Maxiback Optimize",
                        "Configurá una carpeta válida antes de limpiar.",
                        ToolTipIcon.Warning);

                    return;
                }

                string frequency = File.Exists(frequencyFile)
                    ? File.ReadAllText(frequencyFile).Trim()
                    : "3months";

                int deleted = cleaner.CleanOldFiles(
                    settings.FolderPath,
                    frequency);

                trayIcon?.ShowBalloonTip(
                    4000,
                    "Maxiback Optimize",
                    $"Limpieza por antigüedad finalizada. Eliminados: {deleted}.",
                    ToolTipIcon.Info);
            }
            catch (Exception ex)
            {
                logger.Write("Error en limpieza automática: " + ex);

                trayIcon?.ShowBalloonTip(
                    4000,
                    "Maxiback Optimize",
                    "No se pudo completar la limpieza. Revisá el registro.",
                    ToolTipIcon.Error);
            }
        }

        private void ShowWithAdminLogin()
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(ShowWithAdminLogin));
                return;
            }

            Show();
            WindowState = FormWindowState.Normal;
            Activate();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);

            if (WindowState == FormWindowState.Minimized)
                Hide();
        }

        protected override void OnFormClosing(
            FormClosingEventArgs e)
        {
            if (!exitRequested &&
                e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                Hide();

                trayIcon?.ShowBalloonTip(
                    2500,
                    "Maxiback Optimize",
                    "La aplicación continúa ejecutándose en segundo plano.",
                    ToolTipIcon.Info);

                return;
            }

            timer?.Stop();
            timer?.Dispose();

            if (trayIcon != null)
            {
                trayIcon.Visible = false;
                trayIcon.Dispose();
            }

            base.OnFormClosing(e);
        }
    }

    [ToolboxItem(false)]
    [DesignerCategory("Code")]
    internal sealed class SegmentedSelector : FlowLayoutPanel
    {
        private static readonly Color ButtonBorder =
            Color.FromArgb(52, 60, 76);

        private readonly List<Button> buttons = new();
        private readonly Color normalBack;
        private readonly Color selectedBack;
        private readonly Color hoverBack;
        private readonly Color normalFore;

        private int selectedIndex = -1;

        public event EventHandler? SelectionChanged;

        public SegmentedSelector(
            Color normalBack,
            Color selectedBack,
            Color normalFore)
        {
            this.normalBack = normalBack;
            this.selectedBack = selectedBack;
            this.normalFore = normalFore;

            hoverBack = ControlPaint.Light(normalBack, 0.15f);

            FlowDirection = FlowDirection.LeftToRight;
            WrapContents = true;
            AutoSize = true;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;
        }

        [Browsable(false)]
        [DesignerSerializationVisibility(
            DesignerSerializationVisibility.Hidden)]
        public int SelectedIndex
        {
            get => selectedIndex;
            set
            {
                if (value < 0 || value >= buttons.Count)
                    value = -1;

                if (value == selectedIndex)
                    return;

                selectedIndex = value;
                ApplyState();
                SelectionChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        [Browsable(false)]
        [DesignerSerializationVisibility(
            DesignerSerializationVisibility.Hidden)]
        public string? SelectedItem
        {
            get => selectedIndex >= 0
                ? buttons[selectedIndex].Text
                : null;

            set => SelectedIndex =
                buttons.FindIndex(b => b.Text == value);
        }

        public void SetItems(params string[] items)
        {
            SuspendLayout();

            foreach (Button old in buttons)
            {
                Controls.Remove(old);
                old.Dispose();
            }

            buttons.Clear();
            selectedIndex = -1;

            for (int i = 0; i < items.Length; i++)
            {
                int index = i;

                var button = new Button
                {
                    Text = items[i],
                    AutoSize = true,
                    AutoSizeMode = AutoSizeMode.GrowOnly,
                    MinimumSize = new Size(88, 38),
                    Padding = new Padding(12, 0, 12, 0),
                    Margin = new Padding(0, 0, 8, 8),
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font(
                        "Segoe UI",
                        9.5F,
                        FontStyle.Bold),
                    Cursor = Cursors.Hand,
                    UseVisualStyleBackColor = false
                };

                button.FlatAppearance.BorderSize = 1;
                button.Click += (_, _) => SelectedIndex = index;

                buttons.Add(button);
                Controls.Add(button);
            }

            ApplyState();
            ResumeLayout(true);
        }

        private void ApplyState()
        {
            for (int i = 0; i < buttons.Count; i++)
            {
                Button button = buttons[i];
                bool selected = i == selectedIndex;

                button.BackColor = selected
                    ? selectedBack
                    : normalBack;

                button.ForeColor = selected
                    ? Color.White
                    : normalFore;

                button.FlatAppearance.BorderColor = selected
                    ? selectedBack
                    : ButtonBorder;

                button.FlatAppearance.MouseOverBackColor = selected
                    ? selectedBack
                    : hoverBack;
            }
        }
    }
}
