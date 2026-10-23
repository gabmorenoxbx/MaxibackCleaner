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
                        // No mostrar ventanas durante la limpieza automática.
                    }

                    Environment.ExitCode = 1;
                }

                return;
            }

            ApplicationConfiguration.Initialize();
            Application.Run(new Form1());
        }
    }
}