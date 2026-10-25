Maxiback Cleaner

Maxiback Cleaner es una aplicación de escritorio desarrollada en C# con Windows Forms para automatizar la limpieza de archivos temporales, basura digital y otros archivos innecesarios en Windows.

El proyecto surgió a partir de una necesidad de mantenimiento en equipos utilizados en bares con el sistema MaxiRest, con el objetivo de reducir la acumulación de archivos innecesarios y evitar tener que realizar limpiezas manuales periódicamente.

La aplicación permite seleccionar cualquier carpeta de destino, configurar la antigüedad de los archivos que se desean eliminar y programar tareas automáticas de limpieza según las necesidades del equipo.

Características principales

* 🗂️ Limpieza configurable: permite seleccionar la carpeta que se desea mantener.
* ⏱️ Antigüedad personalizable: configura cuándo un archivo se considera antiguo.
* ⚙️ Automatización: programa limpiezas periódicas mediante el Programador de tareas de Windows.
* 🧹 Limpieza manual: permite ejecutar el proceso cuando sea necesario.
* 📋 Registro de operaciones: facilita el seguimiento de las limpiezas y los errores.
* 🔔 Notificaciones: permite gestionar el funcionamiento desde la bandeja del sistema.
* 🛡️ Protecciones: incorpora validaciones para evitar la limpieza de ubicaciones críticas del sistema.
* 💽 USB booteable: prepara unidades USB a partir de ISO compatibles de Windows y escribe directamente imágenes ISO híbridas de Linux.
* 📊 Diagnóstico de hardware: informa frecuencia de CPU, velocidad de RAM configurada y sensores de reloj de CPU/GPU cuando el hardware los expone.

Tecnologías utilizadas

* C#
* .NET
* Windows Forms
* PowerShell
* Windows Task Scheduler
* WMI / System.Management
* LibreHardwareMonitorLib para sensores de frecuencia disponibles

## USB booteable: uso y limitaciones

1. Conectá la memoria USB y abrí Maxiback Cleaner normalmente; no hace falta ejecutar toda la aplicación como administrador.
2. Abrí **USB Booteable** y presioná **Actualizar**.
3. Seleccioná el disco físico por número, modelo y capacidad, y elegí una ISO.
4. Revisá el esquema de partición, destino, sistema de archivos y etiqueta.
5. Confirmá el borrado únicamente si verificaste que el disco seleccionado es la memoria correcta. Windows mostrará el aviso UAC para conceder permisos al proceso auxiliar que prepara el USB.

El proceso borra las particiones y los datos del disco seleccionado. Las ISO híbridas de Linux se escriben directamente; el resto se prepara mediante particionado y copia de archivos. La compatibilidad depende de la estructura de la ISO. Para Windows, FAT32 permite dividir `install.wim` en archivos `.swm` cuando es necesario; archivos individuales incompatibles o ISO no estándar pueden hacer que la creación falle. La aplicación principal permanece sin elevación; solo el asistente de creación se inicia como administrador después de confirmar la operación. Si se cancela el aviso UAC, no se modifica la memoria.

En **Optimización de Windows → Diagnóstico del sistema → Ver componentes del equipo** se muestra la frecuencia actual informada por Windows, la velocidad nominal/configurada de cada módulo RAM y los sensores de reloj de CPU/GPU que LibreHardwareMonitor pueda leer. La frecuencia de GPU no está disponible en todos los equipos/controladores; en ese caso el informe lo indicará.

Objetivo del proyecto

Desarrollar una herramienta liviana y configurable que permita automatizar el mantenimiento de archivos en equipos Windows, reducir tareas repetitivas y mantener ordenadas las carpetas que acumulan archivos temporales o innecesarios.
