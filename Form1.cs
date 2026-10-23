using System;
using System.IO;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using MaxibackCleaner.Models;
using MaxibackCleaner.Services;

namespace MaxibackCleaner
{
    public partial class Form1 : Form
    {
        private readonly TaskSchedulerService taskScheduler = new();
        private readonly AppSettings settings;
        private readonly AppLogger logger;
        private readonly CleanerService cleaner;

        private TextBox txtFolder = null!;
        private ComboBox cmbMonths = null!;
        private Label lblStatus = null!;

        private NotifyIcon trayIcon = null!;
        private System.Windows.Forms.Timer timer = null!;

        private bool exitRequested = false;

        private bool darkMode = false;

        private readonly string themeFile = Path.Combine(
            Environment.GetFolderPath(
                Environment.SpecialFolder.LocalApplicationData),
            "MaxibackCleaner",
            "theme.txt");

        private Button btnTheme = null!;


        private readonly string frequencyFile = Path.Combine(
            Environment.GetFolderPath(
                Environment.SpecialFolder.LocalApplicationData),
            "MaxibackCleaner",
            "frequency.txt");

        private static readonly Dictionary<string, string> Frequencies = new()
        {
            { "1 hora", "1hour" },
            { "1 día", "1day" },
            { "1 semana", "1week" },
            { "1 mes", "1month" },
            { "3 meses", "3months" },
            { "6 meses", "6months" },
            { "12 meses", "12months" }
        };

        public Form1()
        {
            InitializeComponent();

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

        private void BuildInterface()
        {
            Text = "Maxiback Cleaner";
            Width = 590;
            Height = 340;
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;

            var lblFolder = new Label
            {
                Text = "Carpeta que se limpiará:",
                Left = 20,
                Top = 20,
                Width = 250
            };

            txtFolder = new TextBox
            {
                Left = 20,
                Top = 48,
                Width = 390,
                Text = settings.FolderPath
            };

            var btnBrowse = new Button
            {
                Text = "Examinar",
                Left = 420,
                Top = 46,
                Width = 120
            };

            var lblMonths = new Label
            {
                Text = "Eliminar archivos con más de:",
                Left = 20,
                Top = 88,
                Width = 220
            };

            cmbMonths = new ComboBox
            {
                Left = 240,
                Top = 84,
                Width = 170,
                DropDownStyle = ComboBoxStyle.DropDownList
            };

            foreach (string label in Frequencies.Keys)
                cmbMonths.Items.Add(label);

            cmbMonths.SelectedItem = LoadFrequencyLabel();

            var btnSave = new Button
            {
                Text = "Guardar configuración",
                Left = 20,
                Top = 130,
                Width = 170
            };

            var btnClean = new Button
            {
                Text = "Limpiar ahora",
                Left = 200,
                Top = 130,
                Width = 140
            };

            lblStatus = new Label
            {
                Text = "Aplicación lista.",
                Left = 20,
                Top = 185,
                Width = 520,
                Height = 70
            };

            btnTheme = new Button
            {
                Text = "🌙 Modo oscuro",
                Left = 380,
                Top = 225,
                Width = 160,
                Height = 35
            };

            btnTheme.Click += (s, e) =>
            {
                darkMode = !darkMode;
                ApplyTheme();
                SaveTheme();
            };

            Controls.Add(btnTheme);

            // Cargar el tema guardado al iniciar.
            LoadTheme();


            btnBrowse.Click += BtnBrowse_Click;
            btnSave.Click += BtnSave_Click;
            btnClean.Click += BtnClean_Click;

            Controls.AddRange(new Control[]
            {
                lblFolder, txtFolder, btnBrowse,
                lblMonths, cmbMonths, btnSave, btnClean, lblStatus
            });
        }

        private string LoadFrequencyLabel()
        {
            try
            {
                if (File.Exists(frequencyFile))
                {
                    string key = File.ReadAllText(frequencyFile).Trim();

                    foreach (var item in Frequencies)
                    {
                        if (item.Value == key)
                            return item.Key;
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Write("No se pudo leer la frecuencia: " + ex.Message);
            }

            return settings.RetentionMonths switch
            {
                6 => "6 meses",
                12 => "12 meses",
                _ => "3 meses"
            };
        }

        private string GetSelectedFrequency()
        {
            string selected = cmbMonths.SelectedItem?.ToString()
                              ?? "3 meses";

            return Frequencies.TryGetValue(selected, out string? frequency)
                ? frequency
                : "3months";
        }

        private void SaveConfiguration()
        {
            string path = Path.GetFullPath(txtFolder.Text.Trim());

            if (!Directory.Exists(path))
                throw new DirectoryNotFoundException(
                    "La carpeta seleccionada no existe.");

            settings.FolderPath = path;

            // Compatibilidad con la configuración antigua.
            settings.RetentionMonths = GetSelectedFrequency() switch
            {
                "6months" => 6,
                "12months" => 12,
                _ => 3
            };

            settings.Save();

            File.WriteAllText(
                frequencyFile,
                GetSelectedFrequency());
        }
        private void ShowWithAdminLogin()
        {
            // Si la ventana ya está visible, no abrir otro login encima.
            if (Visible)
            {
                Activate();
                return;
            }

            // Pedir autenticación cada vez que se abre desde la bandeja.
            using (AdminLoginForm loginForm = new AdminLoginForm())
            {
                if (loginForm.ShowDialog() != DialogResult.OK)
                {
                    Hide();
                    return;
                }
            }

            Show();
            WindowState = FormWindowState.Normal;
            Activate();
        }

        private void LoadTheme()
        {
            try
            {
                if (File.Exists(themeFile))
                {
                    darkMode = File.ReadAllText(themeFile).Trim()
                        .Equals("dark", StringComparison.OrdinalIgnoreCase);
                }
            }
            catch (Exception ex)
            {
                logger.Write("No se pudo cargar el tema: " + ex.Message);
            }

            ApplyTheme();
        }

        private void SaveTheme()
        {
            try
            {
                Directory.CreateDirectory(
                    Path.GetDirectoryName(themeFile)!);

                File.WriteAllText(
                    themeFile,
                    darkMode ? "dark" : "light");
            }
            catch (Exception ex)
            {
                logger.Write("No se pudo guardar el tema: " + ex.Message);
            }
        }

        private void ApplyTheme()
        {
            Color background = darkMode
                ? Color.FromArgb(35, 35, 40)
                : Color.WhiteSmoke;

            Color foreground = darkMode
                ? Color.WhiteSmoke
                : Color.FromArgb(30, 30, 30);

            Color inputBackground = darkMode
                ? Color.FromArgb(55, 55, 62)
                : Color.White;

            BackColor = background;
            ForeColor = foreground;

            ApplyThemeToControls(
                Controls,
                background,
                foreground,
                inputBackground);

            if (btnTheme != null)
            {
                btnTheme.Text = darkMode
                    ? "☀ Modo claro"
                    : "🌙 Modo oscuro";
            }
        }

        private void ApplyThemeToControls(
            Control.ControlCollection controls,
            Color background,
            Color foreground,
            Color inputBackground)
        {
            foreach (Control control in controls)
            {
                control.ForeColor = foreground;

                if (control is TextBox || control is ComboBox)
                {
                    control.BackColor = inputBackground;
                }
                else if (control is Button button)
                {
                    button.BackColor = darkMode
                        ? Color.FromArgb(65, 65, 75)
                        : Color.FromArgb(230, 230, 235);

                    button.FlatStyle = FlatStyle.Flat;
                    button.FlatAppearance.BorderColor = darkMode
                        ? Color.FromArgb(90, 90, 100)
                        : Color.FromArgb(200, 200, 200);
                }
                else
                {
                    control.BackColor = background;
                }

                if (control.HasChildren)
                {
                    ApplyThemeToControls(
                        control.Controls,
                        background,
                        foreground,
                        inputBackground);
                }
            }
        }

        private void ConfigureTray()
        {
            var menu = new ContextMenuStrip();
            menu.Items.Add("Abrir Maxiback Cleaner", null, (s, e) =>
            {
                ShowWithAdminLogin();
            });
            menu.Items.Add("Limpiar ahora", null, (s, e) =>
            {
                RunAutomaticCleanup();
            });

            menu.Items.Add("Salir", null, (s, e) =>
            {
                exitRequested = true;
                trayIcon.Visible = false;
                Close();
            });

            trayIcon = new NotifyIcon
            {
                Icon = SystemIcons.Application,
                Text = "Maxiback Cleaner",
                ContextMenuStrip = menu,
                Visible = true
            };
            trayIcon.DoubleClick += (s, e) =>
            {
                ShowWithAdminLogin();
            };

        }

        private void ConfigureTimer()
        {
            timer = new System.Windows.Forms.Timer
            {
                Interval = 30 * 60 * 1000
            };

            timer.Tick += (s, e) => CheckUpcomingCleanup();
            timer.Start();
        }

        private void BtnBrowse_Click(object? sender, EventArgs e)
        {
            using var dialog = new FolderBrowserDialog
            {
                Description = "Seleccioná la carpeta de Maxiback"
            };

            if (Directory.Exists(txtFolder.Text))
                dialog.SelectedPath = txtFolder.Text;

            if (dialog.ShowDialog() == DialogResult.OK)
                txtFolder.Text = dialog.SelectedPath;
        }

        private void BtnSave_Click(object? sender, EventArgs e)
        {
            try
            {
                SaveConfiguration();

                taskScheduler.ConfigureTasks(GetSelectedFrequency());

                logger.Write(
                    "Configuración guardada. Frecuencia: "
                    + GetSelectedFrequency());

                lblStatus.Text =
                    "Configuración guardada.\n" +
                    "Frecuencia: " + cmbMonths.SelectedItem +
                    ". Tarea automática registrada.";
            }
            catch (Exception ex)
            {
                logger.Write(
                    "Error al guardar o programar tareas: " + ex);

                MessageBox.Show(
                    "No se pudo guardar la configuración o programar " +
                    "las tareas.\n\n" + ex.Message,
                    "Error de configuración",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);

                lblStatus.Text = "No se pudo completar la configuración.";
            }
        }

        private void BtnClean_Click(object? sender, EventArgs e)
        {
            try
            {
                SaveConfiguration();

                string frequency = GetSelectedFrequency();

                var answer = MessageBox.Show(
                    "Se eliminarán los archivos modificados hace más de "
                    + cmbMonths.SelectedItem + ".\n\n"
                    + "Carpeta: " + settings.FolderPath
                    + "\n\nEsta acción no se puede deshacer. ¿Continuar?",
                    "Confirmar limpieza",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);

                if (answer != DialogResult.Yes)
                    return;

                int deleted = cleaner.CleanOldFiles(
                    settings.FolderPath,
                    frequency);

                lblStatus.Text =
                    $"Limpieza finalizada. Eliminados: {deleted}";
            }
            catch (Exception ex)
            {
                logger.Write("Error en limpieza manual: " + ex);
                MessageBox.Show(ex.Message, "Error de limpieza");
            }
        }

        private static DateTime GetCutoff(DateTime reference, string frequency)
        {
            return frequency switch
            {
                "1hour" => reference.AddHours(-1),
                "1day" => reference.AddDays(-1),
                "1week" => reference.AddDays(-7),
                "1month" => reference.AddMonths(-1),
                "3months" => reference.AddMonths(-3),
                "6months" => reference.AddMonths(-6),
                "12months" => reference.AddMonths(-12),
                _ => reference.AddMonths(-3)
            };
        }

        private void CheckUpcomingCleanup()
        {
            try
            {
                if (!Directory.Exists(settings.FolderPath))
                    return;

                DateTime now = DateTime.Now;
                string frequency = GetSelectedFrequency();

                DateTime currentCutoff = GetCutoff(now, frequency);
                DateTime futureCutoff = GetCutoff(
                    now.AddDays(1), frequency);

                bool found = HasFilesReachingLimit(
                    settings.FolderPath,
                    currentCutoff,
                    futureCutoff);

                if (!found)
                    return;

                string today = now.ToString("yyyy-MM-dd");

                if (settings.LastAlertDate == today)
                    return;

                trayIcon.BalloonTipTitle = "Maxiback Cleaner";
                trayIcon.BalloonTipText =
                    "Hay archivos que alcanzarán la antigüedad configurada " +
                    "durante las próximas 24 horas.";
                trayIcon.BalloonTipIcon = ToolTipIcon.Info;
                trayIcon.ShowBalloonTip(10000);

                settings.LastAlertDate = today;
                settings.Save();

                logger.Write("Se mostró una alerta de antigüedad de archivos.");
            }
            catch (Exception ex)
            {
                logger.Write(
                    "Error al comprobar los archivos próximos al límite: " + ex);
            }
        }

        private bool HasFilesReachingLimit(
            string root,
            DateTime currentCutoff,
            DateTime futureCutoff)
        {
            var folders = new Stack<string>();
            folders.Push(root);

            while (folders.Count > 0)
            {
                string folder = folders.Pop();

                try
                {
                    foreach (string file in Directory.GetFiles(folder))
                    {
                        try
                        {
                            DateTime modified = File.GetLastWriteTime(file);

                            if (modified >= currentCutoff &&
                                modified < futureCutoff)
                            {
                                return true;
                            }
                        }
                        catch (Exception ex)
                        {
                            logger.Write(
                                "No se pudo comprobar " + file + ": "
                                + ex.Message);
                        }
                    }

                    foreach (string subfolder in Directory.GetDirectories(folder))
                    {
                        try
                        {
                            var info = new DirectoryInfo(subfolder);

                            if ((info.Attributes &
                                 FileAttributes.ReparsePoint) == 0)
                            {
                                folders.Push(subfolder);
                            }
                        }
                        catch (Exception ex)
                        {
                            logger.Write(
                                "No se pudo revisar la carpeta "
                                + subfolder + ": " + ex.Message);
                        }
                    }
                }
                catch (Exception ex)
                {
                    logger.Write(
                        "No se pudo recorrer " + folder + ": " + ex.Message);
                }
            }

            return false;
        }

        private void RunAutomaticCleanup()
        {
            try
            {
                string frequency = GetSelectedFrequency();

                int deleted = cleaner.CleanOldFiles(
                    settings.FolderPath,
                    frequency);

                logger.Write(
                    "Limpieza desde la bandeja. Frecuencia: " +
                    frequency + ". Eliminados: " + deleted);

                if (Visible)
                {
                    lblStatus.Text =
                        $"Limpieza finalizada. Eliminados: {deleted}";
                }
            }
            catch (Exception ex)
            {
                logger.Write("Error de limpieza: " + ex);

                if (Visible)
                    MessageBox.Show(ex.Message, "Error de limpieza");
            }
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);

            if (WindowState == FormWindowState.Minimized)
                Hide();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (!exitRequested)
            {
                e.Cancel = true;
                Hide();

                trayIcon.BalloonTipTitle = "Maxiback Cleaner";
                trayIcon.BalloonTipText =
                    "La aplicación continúa funcionando en segundo plano.";
                trayIcon.ShowBalloonTip(3000);

                return;
            }

            timer.Stop();
            timer.Dispose();
            trayIcon.Visible = false;
            trayIcon.Dispose();

            base.OnFormClosing(e);
        }
    }
}