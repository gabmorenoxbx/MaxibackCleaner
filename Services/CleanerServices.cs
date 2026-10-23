using System;
using System.Collections.Generic;
using System.IO;

namespace MaxibackCleaner.Services
{
    public class CleanerService
    {
        private readonly AppLogger logger;

        public CleanerService(AppLogger appLogger)
        {
            logger = appLogger;
        }

        // Método compatible con el código anterior de la aplicación.
        public int CleanOldFiles(string folderPath, int retentionMonths = 3)
        {
            if (retentionMonths != 3 &&
                retentionMonths != 6 &&
                retentionMonths != 12)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(retentionMonths),
                    "La retención debe ser de 3, 6 o 12 meses.");
            }

            return CleanOldFiles(folderPath, retentionMonths + "months");
        }

        // Nuevo método: permite las siete frecuencias.
        public int CleanOldFiles(string folderPath, string frequency)
        {
            if (string.IsNullOrWhiteSpace(folderPath))
                throw new ArgumentException("La ruta está vacía.");

            if (string.IsNullOrWhiteSpace(frequency))
                throw new ArgumentException("La frecuencia está vacía.");

            string root = Path.GetFullPath(folderPath);

            if (!Directory.Exists(root))
                throw new DirectoryNotFoundException(
                    "La carpeta no existe: " + root);

            string driveRoot = Path.GetPathRoot(root) ?? "";

            if (string.Equals(
                root.TrimEnd('\\', '/'),
                driveRoot.TrimEnd('\\', '/'),
                StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "No se permite limpiar la raíz de una unidad.");
            }

            string windowsFolder = Environment.GetFolderPath(
                Environment.SpecialFolder.Windows);

            string programFiles = Environment.GetFolderPath(
                Environment.SpecialFolder.ProgramFiles);

            string programFilesX86 = Environment.GetFolderPath(
                Environment.SpecialFolder.ProgramFilesX86);

            string programData = Environment.GetFolderPath(
                Environment.SpecialFolder.CommonApplicationData);

            if (IsInside(root, windowsFolder) ||
                IsInside(root, programFiles) ||
                IsInside(root, programFilesX86) ||
                IsInside(root, programData))
            {
                throw new InvalidOperationException(
                    "No se permite limpiar carpetas del sistema.");
            }

            DateTime cutoff = GetCutoff(frequency);
            int deletedCount = 0;

            var folders = new Stack<string>();
            folders.Push(root);

            logger.Write("Inicio de limpieza: " + root);
            logger.Write("Frecuencia: " + frequency);
            logger.Write("Fecha límite: " + cutoff);

            while (folders.Count > 0)
            {
                string currentFolder = folders.Pop();

                try
                {
                    foreach (string file in Directory.GetFiles(currentFolder))
                    {
                        try
                        {
                            FileInfo info = new FileInfo(file);

                            if (info.LastWriteTime < cutoff)
                            {
                                info.Delete();
                                deletedCount++;

                                logger.Write("Eliminado: " + file);
                            }
                        }
                        catch (Exception ex)
                        {
                            logger.Write(
                                "Error al procesar archivo " +
                                file + ": " + ex.Message);
                        }
                    }

                    foreach (string subfolder in
                             Directory.GetDirectories(currentFolder))
                    {
                        try
                        {
                            DirectoryInfo info = new DirectoryInfo(subfolder);

                            // No seguir enlaces a otras ubicaciones.
                            if ((info.Attributes &
                                 FileAttributes.ReparsePoint) == 0)
                            {
                                folders.Push(subfolder);
                            }
                        }
                        catch (Exception ex)
                        {
                            logger.Write(
                                "Error al acceder a carpeta " +
                                subfolder + ": " + ex.Message);
                        }
                    }
                }
                catch (Exception ex)
                {
                    logger.Write(
                        "Error al recorrer carpeta " +
                        currentFolder + ": " + ex.Message);
                }
            }

            logger.Write(
                "Fin de limpieza. Total eliminado: " + deletedCount);

            return deletedCount;
        }

        private static DateTime GetCutoff(string frequency)
        {
            DateTime now = DateTime.Now;

            switch (frequency.Trim().ToLowerInvariant())
            {
                case "1hour":
                    return now.AddHours(-1);

                case "1day":
                    return now.AddDays(-1);

                case "1week":
                    return now.AddDays(-7);

                case "1month":
                    return now.AddMonths(-1);

                case "3months":
                    return now.AddMonths(-3);

                case "6months":
                    return now.AddMonths(-6);

                case "12months":
                    return now.AddMonths(-12);

                default:
                    throw new ArgumentException(
                        "Frecuencia no válida: " + frequency);
            }
        }

        private static bool IsInside(string path, string protectedPath)
        {
            if (string.IsNullOrWhiteSpace(protectedPath))
                return false;

            string fullPath = Path.GetFullPath(path)
                .TrimEnd(Path.DirectorySeparatorChar,
                         Path.AltDirectorySeparatorChar);

            string fullProtected = Path.GetFullPath(protectedPath)
                .TrimEnd(Path.DirectorySeparatorChar,
                         Path.AltDirectorySeparatorChar);

            return fullPath.Equals(
                       fullProtected,
                       StringComparison.OrdinalIgnoreCase)
                   || fullPath.StartsWith(
                       fullProtected + Path.DirectorySeparatorChar,
                       StringComparison.OrdinalIgnoreCase)
                   || fullPath.StartsWith(
                       fullProtected + Path.AltDirectorySeparatorChar,
                       StringComparison.OrdinalIgnoreCase);
        }
    }
}