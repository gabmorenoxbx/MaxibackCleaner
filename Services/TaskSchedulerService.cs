using System;
using System.Diagnostics;
using System.IO;

namespace MaxibackCleaner.Services
{
    public class TaskSchedulerService
    {
        public void ConfigureTasks(string frequency = "3months")
        {
            string[] validFrequencies =
            {
                "1hour",
                "1day",
                "1week",
                "1month",
                "3months",
                "6months",
                "12months"
            };

            if (Array.IndexOf(validFrequencies, frequency) < 0)
            {
                throw new ArgumentException(
                    "Frecuencia no válida: " + frequency);
            }

            string exePath = Environment.ProcessPath
                ?? throw new InvalidOperationException(
                    "No se pudo determinar la ruta del ejecutable.");

            string appFolder = Path.Combine(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.LocalApplicationData),
                "MaxibackCleaner");

            Directory.CreateDirectory(appFolder);

            string scriptPath = Path.Combine(
                appFolder,
                "ConfigureTasks.ps1");

            string escapedExe = exePath.Replace("'", "''");

            string script = $@"
$ErrorActionPreference = 'Stop'

$exe = '{escapedExe}'
$user = [System.Security.Principal.WindowsIdentity]::GetCurrent().Name
$frequency = '{frequency}'

$cleanAction = New-ScheduledTaskAction `
    -Execute $exe `
    -Argument ('--clean ' + $frequency)

switch ($frequency) {{
    '1hour' {{
        $trigger = New-ScheduledTaskTrigger `
            -Once `
            -At (Get-Date).AddMinutes(1) `
            -RepetitionInterval (New-TimeSpan -Hours 1) `
            -RepetitionDuration (New-TimeSpan -Days 3650)
    }}
    '1day' {{
        $trigger = New-ScheduledTaskTrigger `
            -Daily -At '08:00'
    }}
    '1week' {{
        $trigger = New-ScheduledTaskTrigger `
            -Weekly -DaysOfWeek Monday -At '08:00'
    }}
    '1month' {{
        $trigger = New-ScheduledTaskTrigger `
            -Monthly -DaysOfMonth 1 -At '08:00'
    }}
    '3months' {{
        $trigger = New-ScheduledTaskTrigger `
            -Monthly -DaysOfMonth 1 `
            -MonthsOfYear January,April,July,October `
            -At '08:00'
    }}
    '6months' {{
        $trigger = New-ScheduledTaskTrigger `
            -Monthly -DaysOfMonth 1 `
            -MonthsOfYear January,July `
            -At '08:00'
    }}
    '12months' {{
        $trigger = New-ScheduledTaskTrigger `
            -Monthly -DaysOfMonth 1 `
            -MonthsOfYear January `
            -At '08:00'
    }}
}}

$principal = New-ScheduledTaskPrincipal `
    -UserId $user `
    -LogonType S4U `
    -RunLevel Limited

$settings = New-ScheduledTaskSettingsSet `
    -StartWhenAvailable `
    -MultipleInstances IgnoreNew

$task = New-ScheduledTask `
    -Action $cleanAction `
    -Trigger $trigger `
    -Principal $principal `
    -Settings $settings

Register-ScheduledTask `
    -TaskName 'Maxiback Cleaner - Limpieza' `
    -InputObject $task `
    -Force | Out-Null

$uiAction = New-ScheduledTaskAction -Execute $exe

$logonTrigger = New-ScheduledTaskTrigger -AtLogOn -User $user

$uiPrincipal = New-ScheduledTaskPrincipal `
    -UserId $user `
    -LogonType Interactive `
    -RunLevel Limited

$uiSettings = New-ScheduledTaskSettingsSet `
    -MultipleInstances IgnoreNew

$uiTask = New-ScheduledTask `
    -Action $uiAction `
    -Trigger $logonTrigger `
    -Principal $uiPrincipal `
    -Settings $uiSettings

Register-ScheduledTask `
    -TaskName 'Maxiback Cleaner - Inicio' `
    -InputObject $uiTask `
    -Force | Out-Null
";

            File.WriteAllText(scriptPath, script);

            var startInfo = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                UseShellExecute = true,
                Verb = "runas",
                WindowStyle = ProcessWindowStyle.Hidden
            };

            startInfo.ArgumentList.Add("-NoProfile");
            startInfo.ArgumentList.Add("-ExecutionPolicy");
            startInfo.ArgumentList.Add("Bypass");
            startInfo.ArgumentList.Add("-File");
            startInfo.ArgumentList.Add(scriptPath);

            try
            {
                using Process? process = Process.Start(startInfo);

                if (process == null)
                {
                    throw new InvalidOperationException(
                        "No se pudo iniciar PowerShell como administrador.");
                }

                process.WaitForExit();

                if (process.ExitCode != 0)
                {
                    throw new InvalidOperationException(
                        "Windows no pudo registrar las tareas. " +
                        "Revisá los permisos y el script generado.");
                }
            }
            catch (System.ComponentModel.Win32Exception ex)
                when (ex.NativeErrorCode == 1223)
            {
                throw new InvalidOperationException(
                    "Se canceló la solicitud de permisos de administrador.");
            }
        }
    }
}