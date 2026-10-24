
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Windows.Forms;

namespace MaxibackCleaner
{
    public class WindowsOptimizationForm : Form
    {
        private readonly ComboBox cmbDrive = new();
        private readonly TextBox txtResults = new();
        private readonly Label lblActionStatus = new();
        private readonly System.Windows.Forms.Timer diagnosticTimer = new();

        private readonly Button btnOptimize = new();
        private readonly Button btnStorage = new();
        private readonly Button btnRepair = new();
        private readonly Button btnStartup = new();
        private readonly Button btnRecycle = new();
        private readonly Button btnRefresh = new();
        private readonly Button btnHardware = new();

        private readonly Label lblCpuValue = new();
        private readonly Label lblCpuDetail = new();
        private readonly Label lblRamValue = new();
        private readonly Label lblRamDetail = new();
        private readonly Label lblDriveValue = new();
        private readonly Label lblDriveDetail = new();
        private readonly Label lblPageTitle = new();

        private readonly FlowLayoutPanel contentFlow = new();
        private Panel? statsPanel;
        private Panel? actionsPanel;
        private Panel? diagnosticsPanel;

        private ulong previousIdle;
        private ulong previousKernel;
        private ulong previousUser;
        private bool hasCpuSample;
        private bool recycleRunning;

        private static readonly Color BackgroundColor = Color.FromArgb(15, 18, 25);
        private static readonly Color CardColor = Color.FromArgb(27, 32, 43);
        private static readonly Color InputColor = Color.FromArgb(36, 42, 55);
        private static readonly Color AccentColor = Color.FromArgb(63, 132, 245);
        private static readonly Color TextColor = Color.FromArgb(235, 239, 246);
        private static readonly Color MutedColor = Color.FromArgb(153, 164, 181);
        private static readonly Color BorderColor = Color.FromArgb(52, 60, 76);
        private static readonly Color SuccessColor = Color.FromArgb(50, 190, 130);

        private const int EM_GETFIRSTVISIBLELINE = 0x00CE;
        private const int EM_LINESCROLL = 0x00B6;

        public WindowsOptimizationForm()
        {
            BuildInterface();
            LoadDrives();

            diagnosticTimer.Interval = 1000;
            diagnosticTimer.Tick += (_, _) => ShowDiagnostics();
            diagnosticTimer.Start();

            Shown += (_, _) => ShowDiagnostics();
        }

        private void BuildInterface()
        {
            Text = "Maxiback Optimize";
            StartPosition = FormStartPosition.CenterParent;
            Size = new Size(1180, 850);
            MinimumSize = new Size(650, 500);
            Font = new Font("Segoe UI", 9F);
            BackColor = BackgroundColor;
            ForeColor = TextColor;

            var main = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                BackColor = BackgroundColor,
                Margin = Padding.Empty,
                Padding = Padding.Empty
            };

            main.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            main.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            main.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));

            contentFlow.Dock = DockStyle.Fill;
            contentFlow.FlowDirection = FlowDirection.TopDown;
            contentFlow.WrapContents = false;
            contentFlow.AutoScroll = true;
            contentFlow.BackColor = BackgroundColor;
            contentFlow.Padding = new Padding(24, 20, 24, 20);
            contentFlow.Margin = Padding.Empty;

            var header = BuildHeader();
            statsPanel = BuildStatsPanel();
            actionsPanel = BuildActionsPanel();
            diagnosticsPanel = BuildDiagnosticsPanel();

            contentFlow.Controls.Add(header);
            contentFlow.Controls.Add(statsPanel);
            contentFlow.Controls.Add(actionsPanel);
            contentFlow.Controls.Add(diagnosticsPanel);
            contentFlow.Resize += (_, _) => AdjustSectionWidths();

            var statusBar = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = CardColor,
                Padding = new Padding(16, 0, 16, 0),
                Margin = Padding.Empty
            };

            lblActionStatus.Text = "Listo. Sistema preparado.";
            lblActionStatus.Dock = DockStyle.Fill;
            lblActionStatus.TextAlign = ContentAlignment.MiddleLeft;
            lblActionStatus.ForeColor = MutedColor;
            lblActionStatus.Font = new Font("Segoe UI", 9F);

            statusBar.Controls.Add(lblActionStatus);
            main.Controls.Add(contentFlow, 0, 0);
            main.Controls.Add(statusBar, 0, 1);
            Controls.Add(main);

            btnOptimize.Click += BtnOptimize_Click;
            btnStorage.Click += BtnStorage_Click;
            btnRepair.Click += BtnRepair_Click;
            btnStartup.Click += BtnStartup_Click;
            btnRecycle.Click += BtnRecycle_Click;
            btnRefresh.Click += (_, _) => ShowDiagnostics();
            btnHardware.Click += BtnHardware_Click;

            FormClosing += (_, _) => diagnosticTimer.Stop();

            ShowSection("optimization");
            AdjustSectionWidths();
        }

        public void ShowSection(string section)
        {
            bool diagnostics = string.Equals(
                section, "diagnostics", StringComparison.OrdinalIgnoreCase);

            if (diagnostics)
            {
                lblPageTitle.Text = "Diagnóstico del sistema";
                statsPanel!.Visible = true;
                actionsPanel!.Visible = false;
                diagnosticsPanel!.Visible = true;
            }
            else
            {
                lblPageTitle.Text = "Optimización de Windows";
                statsPanel!.Visible = true;
                actionsPanel!.Visible = true;
                diagnosticsPanel!.Visible = false;
            }

            contentFlow.AutoScrollPosition = new Point(0, 0);
            AdjustSectionWidths();

            if (diagnostics)
                ShowDiagnostics();
        }

        private Panel BuildHeader()
        {
            var header = new Panel
            {
                Height = 76,
                Margin = new Padding(0, 0, 0, 14),
                BackColor = BackgroundColor
            };

            lblPageTitle.Text = "Optimización de Windows";
            lblPageTitle.Location = new Point(0, 0);
            lblPageTitle.Size = new Size(700, 38);
            lblPageTitle.Font = new Font("Segoe UI", 21F, FontStyle.Bold);
            lblPageTitle.ForeColor = TextColor;

            var description = new Label
            {
                Text = "Administrá el rendimiento y el mantenimiento de tu equipo.",
                Location = new Point(2, 42),
                Size = new Size(750, 25),
                Font = new Font("Segoe UI", 9.5F),
                ForeColor = MutedColor
            };

            header.Controls.Add(lblPageTitle);
            header.Controls.Add(description);
            return header;
        }

        private Panel BuildStatsPanel()
        {
            var outer = new Panel
            {
                Height = 145,
                Margin = new Padding(0, 0, 0, 18),
                BackColor = BackgroundColor
            };

            var grid = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 1,
                BackColor = BackgroundColor,
                Margin = Padding.Empty
            };

            for (int i = 0; i < 3; i++)
                grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.333F));

            grid.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            grid.Controls.Add(CreateMetricCard(
                "PROCESADOR", lblCpuValue, lblCpuDetail, "CPU"), 0, 0);

            grid.Controls.Add(CreateMetricCard(
                "MEMORIA RAM", lblRamValue, lblRamDetail, "RAM"), 1, 0);

            grid.Controls.Add(CreateMetricCard(
                "UNIDAD DEL SISTEMA", lblDriveValue, lblDriveDetail, "DISCO"), 2, 0);

            outer.Controls.Add(grid);
            return outer;
        }

        private Panel CreateMetricCard(
            string title, Label value, Label detail, string marker)
        {
            var card = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = CardColor,
                Margin = new Padding(0, 0, 12, 0),
                Padding = new Padding(15)
            };

            var heading = new Label
            {
                Text = title,
                Dock = DockStyle.Top,
                Height = 25,
                ForeColor = MutedColor,
                Font = new Font("Segoe UI", 8F, FontStyle.Bold)
            };

            var valueRow = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = CardColor
            };

            value.Text = "--";
            value.Location = new Point(0, 8);
            value.Size = new Size(150, 40);
            value.Font = new Font("Segoe UI", 23F, FontStyle.Bold);
            value.ForeColor = TextColor;

            var markerLabel = new Label
            {
                Text = marker,
                Dock = DockStyle.Right,
                Width = 44,
                ForeColor = AccentColor,
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleRight
            };

            detail.Text = "Calculando...";
            detail.Dock = DockStyle.Bottom;
            detail.Height = 24;
            detail.ForeColor = MutedColor;
            detail.Font = new Font("Segoe UI", 8.5F);

            valueRow.Controls.Add(value);
            valueRow.Controls.Add(markerLabel);
            card.Controls.Add(valueRow);
            card.Controls.Add(detail);
            card.Controls.Add(heading);

            return card;
        }

        private Panel BuildActionsPanel()
        {
            var outer = new Panel
            {
                Height = 260,
                Margin = new Padding(0, 0, 0, 18),
                BackColor = CardColor,
                Padding = new Padding(18)
            };

            var heading = new Label
            {
                Text = "Herramientas y mantenimiento",
                Dock = DockStyle.Top,
                Height = 35,
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                ForeColor = TextColor
            };

            var grid = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 4,
                BackColor = CardColor,
                Padding = new Padding(0, 5, 0, 0)
            };

            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

            for (int i = 0; i < 4; i++)
                grid.RowStyles.Add(new RowStyle(SizeType.Percent, 25));

            ConfigureButton(btnOptimize, "Optimizar HDD / SSD", true);
            ConfigureButton(btnStorage, "Sensor de almacenamiento", false);
            ConfigureButton(btnRepair, "Reparar Windows (DISM + SFC)", false);
            ConfigureButton(btnStartup, "Aplicaciones de inicio", false);
            ConfigureButton(btnRecycle, "Vaciar papelera", false);
            ConfigureButton(btnRefresh, "Actualizar diagnóstico", false);
            ConfigureButton(btnHardware, "Ver componentes del equipo", false);

            grid.Controls.Add(btnOptimize, 0, 0);
            grid.Controls.Add(btnStorage, 1, 0);
            grid.Controls.Add(btnRepair, 0, 1);
            grid.Controls.Add(btnStartup, 1, 1);
            grid.Controls.Add(btnRecycle, 0, 2);
            grid.Controls.Add(btnRefresh, 1, 2);
            grid.Controls.Add(btnHardware, 0, 3);
            grid.SetColumnSpan(btnHardware, 2);

            outer.Controls.Add(grid);
            outer.Controls.Add(heading);
            return outer;
        }

        private Panel BuildDiagnosticsPanel()
        {
            var outer = new Panel
            {
                Height = 360,
                Margin = new Padding(0, 0, 0, 12),
                BackColor = CardColor,
                Padding = new Padding(18)
            };

            var heading = new Label
            {
                Text = "Diagnóstico del sistema",
                Dock = DockStyle.Top,
                Height = 34,
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                ForeColor = TextColor
            };

            var description = new Label
            {
                Text = "Información actualizada automáticamente cada segundo.",
                Dock = DockStyle.Top,
                Height = 27,
                ForeColor = MutedColor,
                Font = new Font("Segoe UI", 8.5F)
            };

            txtResults.Dock = DockStyle.Fill;
            txtResults.Multiline = true;
            txtResults.ReadOnly = true;
            txtResults.ScrollBars = ScrollBars.Vertical;
            txtResults.WordWrap = false;
            txtResults.Font = new Font("Consolas", 9F);
            txtResults.BackColor = InputColor;
            txtResults.ForeColor = TextColor;
            txtResults.BorderStyle = BorderStyle.FixedSingle;

            outer.Controls.Add(txtResults);
            outer.Controls.Add(description);
            outer.Controls.Add(heading);
            return outer;
        }

        private void ConfigureButton(Button button, string text, bool primary)
        {
            button.Text = text;
            button.Dock = DockStyle.Fill;
            button.Margin = new Padding(5);
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = primary ? 0 : 1;
            button.FlatAppearance.BorderColor = BorderColor;
            button.BackColor = primary ? AccentColor : CardColor;
            button.ForeColor = primary ? Color.White : TextColor;
            button.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            button.Cursor = Cursors.Hand;
            button.UseVisualStyleBackColor = false;

            button.FlatAppearance.MouseOverBackColor = primary
                ? Color.FromArgb(43, 112, 220)
                : Color.FromArgb(39, 46, 60);

            button.FlatAppearance.MouseDownBackColor = Color.FromArgb(45, 60, 82);
        }

        private void AdjustSectionWidths()
        {
            if (contentFlow.IsDisposed)
                return;

            int width = Math.Max(400, contentFlow.ClientSize.Width - 65);

            foreach (Control control in contentFlow.Controls)
                control.Width = width;
        }

        private void LoadDrives()
        {
            cmbDrive.Items.Clear();

            string windowsDrive =
                Path.GetPathRoot(Environment.SystemDirectory) ?? @"C:\";

            foreach (DriveInfo drive in DriveInfo.GetDrives())
            {
                if (drive.DriveType != DriveType.Fixed)
                    continue;

                cmbDrive.Items.Add(drive.Name);

                if (string.Equals(drive.Name, windowsDrive,
                    StringComparison.OrdinalIgnoreCase))
                    cmbDrive.SelectedItem = drive.Name;
            }

            if (cmbDrive.SelectedIndex < 0 && cmbDrive.Items.Count > 0)
                cmbDrive.SelectedIndex = 0;
        }

        private void BtnOptimize_Click(object? sender, EventArgs e)
        {
            using var dialog = new Form
            {
                Text = "Optimizar unidad",
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                ClientSize = new Size(390, 155),
                MaximizeBox = false,
                MinimizeBox = false,
                BackColor = BackgroundColor,
                ForeColor = TextColor
            };

            var label = new Label
            {
                Text = "Seleccioná la unidad que querés optimizar:",
                Location = new Point(18, 15),
                Size = new Size(350, 25),
                ForeColor = TextColor
            };

            var driveCombo = new ComboBox
            {
                Location = new Point(18, 45),
                Size = new Size(350, 28),
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = InputColor,
                ForeColor = TextColor
            };

            foreach (object item in cmbDrive.Items)
                driveCombo.Items.Add(item);

            if (cmbDrive.SelectedIndex >= 0)
                driveCombo.SelectedIndex = cmbDrive.SelectedIndex;
            else if (driveCombo.Items.Count > 0)
                driveCombo.SelectedIndex = 0;

            var run = new Button
            {
                Text = "Optimizar unidad",
                Location = new Point(208, 95),
                Size = new Size(160, 36),
                BackColor = AccentColor,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                DialogResult = DialogResult.OK
            };

            run.FlatAppearance.BorderSize = 0;
            dialog.Controls.Add(label);
            dialog.Controls.Add(driveCombo);
            dialog.Controls.Add(run);
            dialog.AcceptButton = run;

            if (dialog.ShowDialog(this) != DialogResult.OK ||
                driveCombo.SelectedItem == null)
                return;

            string drive = driveCombo.SelectedItem.ToString()!.TrimEnd('\\');

            if (MessageBox.Show(
                $"Se iniciará la optimización de {drive}.\n\n" +
                "Windows elegirá el procedimiento adecuado para HDD o SSD.\n\n¿Querés continuar?",
                "Confirmar optimización",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            try
            {
                LaunchElevated("defrag.exe", $"{drive} /O /U /V");
                SetStatus($"Se inició la optimización de {drive}.");
            }
            catch (Exception ex)
            {
                ShowError("No se pudo iniciar la optimización.", ex);
            }
        }

        private void BtnStorage_Click(object? sender, EventArgs e)
        {
            try
            {
                OpenWindowsPage("ms-settings:storagesense");
                SetStatus("Se abrió el sensor de almacenamiento.");
            }
            catch (Exception ex)
            {
                ShowError("No se pudo abrir la configuración.", ex);
            }
        }

        private void BtnRepair_Click(object? sender, EventArgs e)
        {
            if (MessageBox.Show(
                "Se abrirá una consola como administrador para ejecutar DISM y SFC.\n\n" +
                "El proceso puede tardar bastante. ¿Querés continuar?",
                "Reparar Windows",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning) != DialogResult.Yes)
                return;

            try
            {
                LaunchElevated("cmd.exe",
                    "/k \"DISM.exe /Online /Cleanup-image /RestoreHealth && sfc /scannow\"");
                SetStatus("Se inició la reparación de Windows.");
            }
            catch (Exception ex)
            {
                ShowError("No se pudo iniciar la reparación.", ex);
            }
        }

        private void BtnStartup_Click(object? sender, EventArgs e)
        {
            try
            {
                OpenWindowsPage("ms-settings:startupapps");
                SetStatus("Se abrió la administración de aplicaciones de inicio.");
            }
            catch
            {
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "taskmgr.exe",
                        UseShellExecute = true
                    });
                }
                catch (Exception ex)
                {
                    ShowError("No se pudo abrir el Administrador de tareas.", ex);
                }
            }
        }

        private async void BtnRecycle_Click(object? sender, EventArgs e)
        {
            if (recycleRunning)
                return;

            if (MessageBox.Show(
                "¿Querés vaciar la Papelera de reciclaje?\n\n" +
                "Los archivos eliminados podrían no poder recuperarse.",
                "Confirmar vaciado",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning) != DialogResult.Yes)
                return;

            recycleRunning = true;
            btnRecycle.Enabled = false;
            btnRefresh.Enabled = false;

            try
            {
                SetStatus("Consultando la papelera...");
                var before = await Task.Run(GetRecycleBinInfo);

                if (before.Result < 0)
                    throw new InvalidOperationException(
                        $"No se pudo consultar la papelera. HRESULT: 0x{before.Result:X8}");

                long initialCount = before.Info.i64NumItems;

                if (initialCount == 0)
                {
                    SetStatus("La papelera ya está vacía.");
                    MessageBox.Show("La papelera ya está vacía.", "Papelera",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                SetStatus($"Solicitando el vaciado de {initialCount} elementos...");

                const uint flags = 0x00000001 | 0x00000004;
                IntPtr ownerHandle = Handle;

                int result = await Task.Run(() =>
                    SHEmptyRecycleBin(ownerHandle, null, flags));

                if (result < 0)
                    throw new InvalidOperationException(
                        $"Windows devolvió HRESULT 0x{result:X8} al vaciar la papelera.");

                SetStatus("Verificando los elementos restantes...");
                var after = await Task.Run(GetRecycleBinInfo);

                if (after.Result < 0)
                    throw new InvalidOperationException(
                        $"No se pudo verificar el resultado. HRESULT: 0x{after.Result:X8}");

                long remaining = after.Info.i64NumItems;
                long difference = Math.Max(0, initialCount - remaining);

                if (remaining == 0)
                {
                    SetStatus($"Papelera vacía. Elementos iniciales: {initialCount}.");

                    MessageBox.Show(
                        $"Vaciado completado.\n\nElementos iniciales: {initialCount}\nRestantes: 0",
                        "Papelera de reciclaje",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
                else
                {
                    SetStatus($"Quedan {remaining} elementos en la papelera.");

                    MessageBox.Show(
                        $"Elementos iniciales: {initialCount}\n" +
                        $"Elementos restantes: {remaining}\nDiferencia: {difference}\n\n" +
                        "Windows todavía informa elementos en la papelera. " +
                        "Probá vaciarla desde el Explorador de archivos.",
                        "Vaciado parcial",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                ShowError("No se pudo completar el vaciado.", ex);
            }
            finally
            {
                recycleRunning = false;

                if (!IsDisposed)
                {
                    btnRecycle.Enabled = true;
                    btnRefresh.Enabled = true;
                }
            }
        }

        private static (int Result, SHQUERYRBINFO Info) GetRecycleBinInfo()
        {
            var info = new SHQUERYRBINFO
            {
                cbSize = (uint)Marshal.SizeOf<SHQUERYRBINFO>()
            };

            int result = SHQueryRecycleBin(null, ref info);
            return (result, info);
        }

        private void BtnHardware_Click(object? sender, EventArgs e)
        {
            try
            {
                btnHardware.Enabled = false;

                var hardwareForm = new HardwareInfoForm();

                hardwareForm.FormClosed += (_, _) =>
                {
                    if (!IsDisposed)
                        btnHardware.Enabled = true;
                };

                hardwareForm.Shown += async (_, _) =>
                {
                    try
                    {
                        await hardwareForm.LoadHardwareAsync();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(
                            "No se pudo consultar el hardware.\n\n" + ex.Message,
                            "Diagnóstico",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
                    }
                };

                hardwareForm.Show(this);
            }
            catch (Exception ex)
            {
                btnHardware.Enabled = true;
                ShowError("No se pudo abrir el diagnóstico.", ex);
            }
        }

        private static void LaunchElevated(string fileName, string arguments)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                UseShellExecute = true,
                Verb = "runas",
                WindowStyle = ProcessWindowStyle.Normal
            });
        }

        private static void OpenWindowsPage(string uri)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = uri,
                UseShellExecute = true
            });
        }

        private void ShowDiagnostics()
        {
            if (IsDisposed || txtResults.IsDisposed || !IsHandleCreated)
                return;

            var output = new StringBuilder();

            output.AppendLine("DIAGNÓSTICO DEL EQUIPO");
            output.AppendLine("======================");
            output.AppendLine("Actualización: " + DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"));
            output.AppendLine("Windows: " + Environment.OSVersion);
            output.AppendLine("Sistema operativo de 64 bits: " + Environment.Is64BitOperatingSystem);
            output.AppendLine("Procesadores lógicos: " + Environment.ProcessorCount);
            output.AppendLine();

            try
            {
                double? cpu = ReadCpuUsage();
                string cpuText = cpu.HasValue ? cpu.Value.ToString("0.0") + "%" : "Calculando...";

                output.AppendLine("CPU: " + cpuText);
                lblCpuValue.Text = cpuText;
                lblCpuDetail.Text = "Uso actual del procesador";
            }
            catch
            {
                output.AppendLine("CPU: No disponible");
                lblCpuValue.Text = "--";
                lblCpuDetail.Text = "Lectura no disponible";
            }

            try
            {
                var memory = new MemoryStatusEx
                {
                    Length = (uint)Marshal.SizeOf<MemoryStatusEx>()
                };

                if (GlobalMemoryStatusEx(ref memory))
                {
                    double total = ToGb(memory.TotalPhysical);
                    double available = ToGb(memory.AvailablePhysical);
                    double used = total - available;

                    output.AppendLine($"RAM total: {total:0.00} GB");
                    output.AppendLine($"RAM disponible: {available:0.00} GB");
                    output.AppendLine($"RAM utilizada: {used:0.00} GB");
                    output.AppendLine($"Uso de memoria: {memory.MemoryLoad}%");

                    lblRamValue.Text = memory.MemoryLoad + "%";
                    lblRamDetail.Text = $"{used:0.0} GB de {total:0.0} GB utilizados";
                }
            }
            catch
            {
                output.AppendLine("RAM: No disponible");
                lblRamValue.Text = "--";
                lblRamDetail.Text = "Lectura no disponible";
            }

            output.AppendLine();
            output.AppendLine("UNIDADES");
            output.AppendLine("--------");

            string systemDrive =
                Path.GetPathRoot(Environment.SystemDirectory) ?? @"C:\";

            foreach (DriveInfo drive in DriveInfo.GetDrives())
            {
                try
                {
                    if (!drive.IsReady)
                        continue;

                    output.AppendLine($"{drive.Name} | {drive.DriveType} | {drive.DriveFormat}");
                    output.AppendLine(
                        $"  Capacidad: {ToGb((ulong)drive.TotalSize):0.00} GB | " +
                        $"Libre: {ToGb((ulong)drive.AvailableFreeSpace):0.00} GB");

                    double used = drive.TotalSize > 0
                        ? 100.0 * (drive.TotalSize - drive.AvailableFreeSpace) / drive.TotalSize
                        : 0;

                    output.AppendLine($"  Ocupado: {used:0.0}%");

                    if (string.Equals(drive.Name, systemDrive,
                        StringComparison.OrdinalIgnoreCase))
                    {
                        lblDriveValue.Text = $"{used:0}%";
                        lblDriveDetail.Text =
                            $"{ToGb((ulong)drive.AvailableFreeSpace):0.0} GB libres";
                    }
                }
                catch
                {
                    output.AppendLine($"{drive.Name}: no disponible.");
                }
            }

            output.AppendLine();
            output.AppendLine("El diagnóstico se actualiza automáticamente cada segundo.");

            UpdateDiagnosticText(output.ToString());
        }

        private void UpdateDiagnosticText(string newText)
        {
            if (txtResults.Text == newText)
                return;

            int firstVisibleLine = SendMessage(
                txtResults.Handle, EM_GETFIRSTVISIBLELINE, IntPtr.Zero, IntPtr.Zero).ToInt32();

            int selectionStart = txtResults.SelectionStart;
            int selectionLength = txtResults.SelectionLength;

            txtResults.Text = newText;

            selectionStart = Math.Min(selectionStart, txtResults.TextLength);
            selectionLength = Math.Min(selectionLength, txtResults.TextLength - selectionStart);

            txtResults.Select(selectionStart, selectionLength);

            int newVisibleLine = SendMessage(
                txtResults.Handle, EM_GETFIRSTVISIBLELINE, IntPtr.Zero, IntPtr.Zero).ToInt32();

            int lineDifference = firstVisibleLine - newVisibleLine;

            if (lineDifference != 0)
            {
                SendMessage(
                    txtResults.Handle,
                    EM_LINESCROLL,
                    IntPtr.Zero,
                    new IntPtr(lineDifference));
            }
        }

        private double? ReadCpuUsage()
        {
            if (!GetSystemTimes(out FileTime idleTime,
                out FileTime kernelTime, out FileTime userTime))
                return null;

            ulong idle = idleTime.ToUInt64();
            ulong kernel = kernelTime.ToUInt64();
            ulong user = userTime.ToUInt64();

            if (!hasCpuSample)
            {
                previousIdle = idle;
                previousKernel = kernel;
                previousUser = user;
                hasCpuSample = true;
                return null;
            }

            ulong idleDelta = idle - previousIdle;
            ulong total = (kernel - previousKernel) + (user - previousUser);

            previousIdle = idle;
            previousKernel = kernel;
            previousUser = user;

            if (total == 0)
                return 0;

            ulong busy = total > idleDelta ? total - idleDelta : 0;
            return Math.Clamp(100.0 * busy / total, 0.0, 100.0);
        }

        private static double ToGb(ulong bytes) =>
            bytes / 1024d / 1024d / 1024d;

        private void SetStatus(string message)
        {
            if (!IsDisposed && !lblActionStatus.IsDisposed)
                lblActionStatus.Text = DateTime.Now.ToString("HH:mm") + "  •  " + message;
        }

        private void ShowError(string message, Exception ex)
        {
            SetStatus(message + " " + ex.Message);

            MessageBox.Show(
                message + "\n\n" + ex.Message,
                "Maxiback Optimize",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessage(
            IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GlobalMemoryStatusEx(ref MemoryStatusEx buffer);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetSystemTimes(
            out FileTime idleTime,
            out FileTime kernelTime,
            out FileTime userTime);

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        private static extern int SHEmptyRecycleBin(
            IntPtr hwnd, string? pszRootPath, uint dwFlags);

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        private static extern int SHQueryRecycleBin(
            string? pszRootPath, ref SHQUERYRBINFO pSHQueryRBInfo);

        [StructLayout(LayoutKind.Sequential)]
        private struct SHQUERYRBINFO
        {
            public uint cbSize;
            public long i64Size;
            public long i64NumItems;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct FileTime
        {
            public uint Low;
            public uint High;

            public ulong ToUInt64() => ((ulong)High << 32) | Low;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct MemoryStatusEx
        {
            public uint Length;
            public uint MemoryLoad;
            public ulong TotalPhysical;
            public ulong AvailablePhysical;
            public ulong TotalPageFile;
            public ulong AvailablePageFile;
            public ulong TotalVirtual;
            public ulong AvailableVirtual;
            public ulong AvailableExtendedVirtual;
        }
    }
}
