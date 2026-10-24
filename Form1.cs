using System;
using System.Collections.Generic;
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
        private ComboBox cmbMonths = null!;
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

        private Panel sidebar = null!;
        private Panel contentPanel = null!;
        private Panel cleaningView = null!;
        private WindowsOptimizationForm optimizationView = null!;

        private bool exitRequested;
        private string currentSection = "cleaning";

        private readonly string frequencyFile = Path.Combine(
            Environment.GetFolderPath(
                Environment.SpecialFolder.LocalApplicationData),
            "MaxibackCleaner",
            "frequency.txt");

        private readonly Color backgroundColor =
            Color.FromArgb(15, 18, 25);

        private readonly Color sidebarColor =
            Color.FromArgb(20, 24, 33);

        private readonly Color cardColor =
            Color.FromArgb(27, 32, 43);

        private readonly Color inputColor =
            Color.FromArgb(36, 42, 55);

        private readonly Color textColor =
            Color.FromArgb(235, 239, 246);

        private readonly Color mutedColor =
            Color.FromArgb(153, 164, 181);

        private readonly Color accentColor =
            Color.FromArgb(63, 132, 245);

        private readonly Color successColor =
            Color.FromArgb(50, 190, 130);

        private readonly Color dangerColor =
            Color.FromArgb(220, 80, 90);

        public Form1()
        {
            InitializeComponent();

            // Icono de la ventana, la barra de tareas y la bandeja del sistema.
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
                {
                    return new Icon(path);
                }

                // Plan B: el icono incrustado en el propio .exe
                Icon? exeIcon =
                    Icon.ExtractAssociatedIcon(Application.ExecutablePath);

                if (exeIcon != null)
                {
                    return exeIcon;
                }
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

            // LOGO DE LA APLICACIÓN
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
                    using (Image original = Image.FromFile(logoPath))
                    {
                        // Copiar la imagen para no mantener bloqueado el archivo.
                        logo.Image = new Bitmap(original);
                    }
                }
                catch (Exception ex)
                {
                    logger.Write(
                        "No se pudo cargar logo.jpg: " + ex.Message);
                }
            }

            // NOMBRE DE LA APLICACIÓN
            var brand = new Label
            {
                Text = "MAXIBACK\nOPTIMIZE",
                Dock = DockStyle.Top,
                Height = 65,
                Font = new Font(
                    "Segoe UI",
                    17F,
                    FontStyle.Bold),
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
                Font = new Font(
                    "Segoe UI",
                    8.5F,
                    FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft
            };

            var nav = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 174,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = false,
                BackColor = sidebarColor,
                Margin = Padding.Empty,
                Padding = Padding.Empty
            };

            btnCleaningNav = CreateNavButton("   Limpieza");

            btnOptimizationNav = CreateNavButton(
                "   Optimización de Windows");

            btnDiagnosticsNav = CreateNavButton(
                "   Diagnóstico del sistema");

            btnCleaningNav.Click += (_, _) =>
                ShowSection("cleaning");

            btnOptimizationNav.Click += (_, _) =>
                ShowSection("optimization");

            btnDiagnosticsNav.Click += (_, _) =>
                ShowSection("diagnostics");

            nav.Controls.Add(btnCleaningNav);
            nav.Controls.Add(btnOptimizationNav);
            nav.Controls.Add(btnDiagnosticsNav);

            // INFORMACIÓN INFERIOR
            var sidebarFooter = new Label
            {
                Text = "Limpieza y mantenimiento\nVersión de escritorio",
                Dock = DockStyle.Bottom,
                Height = 52,
                ForeColor = mutedColor,
                Font = new Font("Segoe UI", 8.5F),
                TextAlign = ContentAlignment.BottomLeft
            };

            // ENLACE A GITHUB
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
                    Process.Start(
                        new ProcessStartInfo
                        {
                            FileName =
                                "https://github.com/gabmorenoxbx",
                            UseShellExecute = true
                        });
                }
                catch (Exception ex)
                {
                    logger.Write(
                        "No se pudo abrir GitHub: " + ex.Message);
                }
            };

            // Agregar cada control una sola vez.
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
                Font = new Font(
                    "Segoe UI",
                    9.2F,
                    FontStyle.Bold),
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
            currentSection = section;

            bool showCleaning = section == "cleaning";
            bool showOptimization = section == "optimization";
            bool showDiagnostics = section == "diagnostics";

            cleaningView.Visible = showCleaning;
            optimizationView.Visible = !showCleaning;

            if (showOptimization)
            {
                optimizationView.ShowSection("optimization");
            }
            else if (showDiagnostics)
            {
                optimizationView.ShowSection("diagnostics");
            }

            SetNavButtonState(btnCleaningNav, showCleaning);
            SetNavButtonState(btnOptimizationNav, showOptimization);
            SetNavButtonState(btnDiagnosticsNav, showDiagnostics);

            if (showCleaning)
            {
                cleaningView.BringToFront();
                contentPanel.Controls.SetChildIndex(cleaningView, 0);
                txtFolder.Focus();
            }
            else
            {
                optimizationView.BringToFront();
            }
        }

        private void SetNavButtonState(
            Button button,
            bool selected)
        {
            button.BackColor = selected
                ? Color.FromArgb(35, 48, 70)
                : sidebarColor;

            button.ForeColor = selected
                ? Color.White
                : textColor;
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
            {
                main.RowStyles.Add(
                    new RowStyle(SizeType.AutoSize));
            }

            var header = new Panel
            {
                Dock = DockStyle.Top,
                Height = 78,
                BackColor = backgroundColor,
                Margin = new Padding(0, 0, 0, 18)
            };

            var title = new Label
            {
                Text = "Limpieza",
                Location = new Point(0, 0),
                AutoSize = true,
                Font = new Font(
                    "Segoe UI",
                    23F,
                    FontStyle.Bold),
                ForeColor = textColor
            };

            var subtitle = new Label
            {
                Text = "Administrá la limpieza automática de archivos y temporales.",
                Location = new Point(2, 43),
                AutoSize = true,
                Font = new Font("Segoe UI", 9.5F),
                ForeColor = mutedColor
            };

            header.Controls.Add(title);
            header.Controls.Add(subtitle);

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
                "Se eliminarán los archivos y carpetas que superen el período elegido.");

            cmbMonths = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = inputColor,
                ForeColor = textColor,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10F),
                Width = 220,
                Height = 36
            };

            cmbMonths.Items.AddRange(new object[]
            {
                "1 hora",
                "1 día",
                "1 semana",
                "1 mes",
                "3 meses",
                "6 meses",
                "12 meses"
            });

            cmbMonths.SelectedItem = LoadFrequencyLabel();

            if (cmbMonths.SelectedIndex < 0)
            {
                cmbMonths.SelectedIndex = 4;
            }

            lblFrequencyInfo = new Label
            {
                Text = "La frecuencia elegida se utiliza para configurar la limpieza automática.",
                Dock = DockStyle.Top,
                AutoSize = true,
                ForeColor = mutedColor,
                Font = new Font("Segoe UI", 9F),
                Margin = new Padding(0, 12, 0, 0)
            };

            var retentionLayout = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                WrapContents = true,
                FlowDirection = FlowDirection.LeftToRight,
                BackColor = cardColor,
                Margin = new Padding(0, 15, 0, 0)
            };

            retentionLayout.Controls.Add(cmbMonths);
            retentionCard.Controls.Add(lblFrequencyInfo);
            retentionCard.Controls.Add(retentionLayout);

            var actionCard = CreateCard(
                "ACCIONES",
                "Guardá la configuración o ejecutá una limpieza manual.");

            var actionLayout = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                WrapContents = true,
                FlowDirection = FlowDirection.LeftToRight,
                BackColor = cardColor,
                Margin = new Padding(0, 15, 0, 0)
            };

            btnSave = CreateButton(
                "Guardar configuración",
                accentColor);

            btnSave.Width = 190;
            btnSave.Height = 42;
            btnSave.Click += BtnSave_Click;

            btnClean = CreateButton(
                "Limpiar ahora",
                successColor);

            btnClean.Width = 150;
            btnClean.Height = 42;
            btnClean.Click += BtnClean_Click;

            btnTemp = CreateButton(
                "Limpiar temporales",
                inputColor);

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
        }

        private Panel CreateCard(
            string heading,
            string description)
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
                Font = new Font(
                    "Segoe UI",
                    10F,
                    FontStyle.Bold),
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

        private Button CreateButton(
            string text,
            Color color)
        {
            var button = new Button
            {
                Text = text,
                BackColor = color,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font(
                    "Segoe UI",
                    9.5F,
                    FontStyle.Bold),
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
                        {
                            return pair.Key;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Write(
                    "No se pudo leer la frecuencia guardada: " +
                    ex.Message);
            }

            return settings.RetentionMonths switch
            {
                6 => "6 meses",
                12 => "12 meses",
                1 => "1 mes",
                _ => "3 meses"
            };
        }

        private static Dictionary<string, string> GetFrequencyMap()
        {
            return new Dictionary<string, string>
            {
                ["1 hora"] = "1hour",
                ["1 día"] = "1day",
                ["1 semana"] = "1week",
                ["1 mes"] = "1month",
                ["3 meses"] = "3months",
                ["6 meses"] = "6months",
                ["12 meses"] = "12months"
            };
        }

        private string GetSelectedFrequency()
        {
            string label =
                cmbMonths.SelectedItem?.ToString() ?? "3 meses";

            var map = GetFrequencyMap();

            return map.TryGetValue(label, out string? value)
                ? value
                : "3months";
        }

        private void SaveConfiguration()
        {
            if (string.IsNullOrWhiteSpace(txtFolder.Text))
            {
                throw new InvalidOperationException(
                    "Seleccioná una carpeta para limpiar.");
            }

            if (!Directory.Exists(txtFolder.Text))
            {
                throw new DirectoryNotFoundException(
                    "La carpeta seleccionada no existe.");
            }

            settings.FolderPath = txtFolder.Text.Trim();

            settings.RetentionMonths =
                cmbMonths.SelectedItem?.ToString() switch
                {
                    "6 meses" => 6,
                    "12 meses" => 12,
                    "1 mes" => 1,
                    "1 semana" => 0,
                    "1 día" => 0,
                    "1 hora" => 0,
                    _ => 3
                };

            settings.Save();

            Directory.CreateDirectory(
                Path.GetDirectoryName(frequencyFile)!);

            File.WriteAllText(
                frequencyFile,
                GetSelectedFrequency());
        }

        private void BtnBrowse_Click(
            object? sender,
            EventArgs e)
        {
            using var dialog = new FolderBrowserDialog
            {
                Description =
                    "Seleccioná la carpeta que querés limpiar",
                ShowNewFolderButton = false
            };

            if (Directory.Exists(txtFolder.Text))
            {
                dialog.SelectedPath = txtFolder.Text;
            }

            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                txtFolder.Text = dialog.SelectedPath;
            }
        }

        private void BtnSave_Click(
            object? sender,
            EventArgs e)
        {
            try
            {
                SaveConfiguration();
                taskScheduler.ConfigureTasks(GetSelectedFrequency());

                lblStatus.Text =
                    "Configuración guardada y tarea automática actualizada.";

                lblStatus.ForeColor = successColor;

                logger.Write(
                    "Configuración guardada. Frecuencia: " +
                    GetSelectedFrequency());
            }
            catch (Exception ex)
            {
                lblStatus.Text =
                    "No se pudo guardar la configuración.";

                lblStatus.ForeColor = dangerColor;

                MessageBox.Show(
                    this,
                    ex.Message,
                    "Error al guardar",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);

                logger.Write(
                    "Error al guardar configuración: " + ex);
            }
        }

        private void BtnClean_Click(
            object? sender,
            EventArgs e)
        {
            try
            {
                SaveConfiguration();

                string frequency = GetSelectedFrequency();

                var result = MessageBox.Show(
                    this,
                    $"Se eliminarán los archivos y carpetas con una antigüedad superior a {cmbMonths.SelectedItem} en:\n\n{settings.FolderPath}\n\n¿Querés continuar?",
                    "Confirmar limpieza",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);

                if (result != DialogResult.Yes)
                {
                    return;
                }

                cleaner.CleanOldFiles(
                    settings.FolderPath,
                    frequency);

                lblStatus.Text =
                    "La limpieza finalizó. Revisá el registro para más detalles.";

                lblStatus.ForeColor = successColor;
            }
            catch (Exception ex)
            {
                lblStatus.Text =
                    "Ocurrió un error durante la limpieza.";

                lblStatus.ForeColor = dangerColor;

                MessageBox.Show(
                    this,
                    ex.Message,
                    "Error de limpieza",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);

                logger.Write(
                    "Error en limpieza manual: " + ex);
            }
        }

        private void BtnTemp_Click(
            object? sender,
            EventArgs e)
        {
            RunTemporaryCleanup(true);
        }

        private void RunTemporaryCleanup(bool showConfirmation)
        {
            if (showConfirmation)
            {
                var answer = MessageBox.Show(
                    this,
                    "Se intentará eliminar archivos temporales de Windows y del usuario. Algunos archivos en uso no podrán eliminarse.\n\n¿Continuar?",
                    "Limpiar temporales",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);

                if (answer != DialogResult.Yes)
                {
                    return;
                }
            }

            try
            {
                cleaner.CleanTemporaryFiles();

                lblStatus.Text =
                    "Se ejecutó la limpieza de temporales. Revisá el registro para ver el detalle.";

                lblStatus.ForeColor = successColor;
            }
            catch (Exception ex)
            {
                lblStatus.Text =
                    "No se pudo completar la limpieza de temporales.";

                lblStatus.ForeColor = dangerColor;

                logger.Write(
                    "Error al limpiar temporales: " + ex);

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
                {
                    trayIcon.Visible = false;
                }

                Close();
            });

            trayIcon = new NotifyIcon
            {
                Text = "Maxiback Optimize",
                Icon = Icon ?? SystemIcons.Application,
                ContextMenuStrip = trayMenu,
                Visible = true
            };

            trayIcon.DoubleClick +=
                (_, _) => ShowWithAdminLogin();
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
                {
                    return;
                }

                if (DateTime.TryParse(
                        settings.LastAlertDate,
                        out DateTime lastAlertDate) &&
                    lastAlertDate.Date == DateTime.Today)
                {
                    return;
                }

                string frequency = File.Exists(frequencyFile)
                    ? File.ReadAllText(frequencyFile).Trim()
                    : GetSelectedFrequency();

                if (!HasFilesReachingLimit(
                        settings.FolderPath,
                        GetCutoff(frequency)))
                {
                    return;
                }

                trayIcon?.ShowBalloonTip(
                    5000,
                    "Maxiback Optimize",
                    "Hay archivos que alcanzaron la antigüedad configurada. Podés ejecutar la limpieza.",
                    ToolTipIcon.Info);

                settings.LastAlertDate =
                    DateTime.Today.ToString("yyyy-MM-dd");

                settings.Save();
            }
            catch (Exception ex)
            {
                logger.Write(
                    "Error al revisar próxima limpieza: " +
                    ex.Message);
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
                    {
                        return true;
                    }
                }

                foreach (string directory in Directory.GetDirectories(folder))
                {
                    var info = new DirectoryInfo(directory);

                    if ((info.Attributes &
                         FileAttributes.ReparsePoint) != 0)
                    {
                        continue;
                    }

                    if (HasFilesReachingLimit(directory, cutoff))
                    {
                        return true;
                    }
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

                cleaner.CleanOldFiles(
                    settings.FolderPath,
                    frequency);

                trayIcon?.ShowBalloonTip(
                    4000,
                    "Maxiback Optimize",
                    "La limpieza automática finalizó.",
                    ToolTipIcon.Info);
            }
            catch (Exception ex)
            {
                logger.Write(
                    "Error en limpieza automática: " + ex);
            }
        }

        private void ShowWithAdminLogin()
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(ShowWithAdminLogin));
                return;
            }

            if (!Visible)
            {
                using var login = new AdminLoginForm();

                if (login.ShowDialog() != DialogResult.OK)
                {
                    return;
                }
            }

            Show();
            WindowState = FormWindowState.Normal;
            Activate();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);

            if (WindowState == FormWindowState.Minimized)
            {
                Hide();
            }
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
}