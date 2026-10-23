
using System;
using MaxibackCleaner.Models;
using MaxibackCleaner.Services;
using System.Windows.Forms;

namespace MaxibackCleaner
{
    internal static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            // Las limpiezas automáticas no muestran ventanas.
            if (args.Length > 0 &&
                args[0].Equals(
                    "--clean",
                    StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    AppSettings settings = AppSettings.Load();
                    AppLogger logger = new AppLogger();
                    CleanerService cleaner = new CleanerService(logger);

                    string frequency;

                    if (args.Length > 1)
                    {
                        frequency = args[1];
                    }
                    else
                    {
                        frequency = settings.RetentionMonths + "months";
                    }

                    int deleted = cleaner.CleanOldFiles(
                        settings.FolderPath,
                        frequency);

                    logger.Write(
                        "Limpieza automática completada. " +
                        "Frecuencia: " + frequency +
                        ". Eliminados: " + deleted);
                }
                catch (Exception ex)
                {
                    try
                    {
                        new AppLogger().Write(
                            "Error en limpieza automática: " + ex);
                    }
                    catch
                    {
                        // No mostrar ventanas durante la limpieza.
                    }

                    Environment.ExitCode = 1;
                }

                return;
            }

            // Inicio normal de la aplicación.
            ApplicationConfiguration.Initialize();

            // 1. Verificar la licencia.
            if (!LicenseService.IsActivated())
            {
                using (LicenseForm licenseForm = new LicenseForm())
                {
                    if (licenseForm.ShowDialog() != DialogResult.OK)
                    {
                        return;
                    }
                }
            }

            // 2. Configurar el administrador la primera vez
            // o solicitar sus credenciales en los siguientes inicios.
            using (AdminLoginForm loginForm = new AdminLoginForm())
            {
                if (loginForm.ShowDialog() != DialogResult.OK)
                {
                    return;
                }
            }

            // 3. Abrir la ventana principal solo si pasó ambos controles.
            Application.Run(new Form1());
        }
    }
}
