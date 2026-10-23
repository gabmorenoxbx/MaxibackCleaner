
using System;
using System.Drawing;
using System.Windows.Forms;
using MaxibackCleaner.Services;

namespace MaxibackCleaner
{
    public class LicenseForm : Form
    {
        private readonly TextBox txtCode;
        private readonly Button btnActivate;

        public LicenseForm()
        {
            Text = "Activar Maxiback Cleaner";
            Size = new Size(440, 250);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = true;

            Label title = new Label
            {
                Text = "Licencia gratuita",
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(25, 20)
            };

            Label description = new Label
            {
                Text = "Ingresá el código gratuito para activar el programa.",
                AutoSize = false,
                Size = new Size(370, 40),
                Location = new Point(25, 65)
            };

            txtCode = new TextBox
            {
                Location = new Point(25, 112),
                Width = 370,
                PlaceholderText = "Código de licencia"
            };

            btnActivate = new Button
            {
                Text = "Activar",
                Location = new Point(285, 155),
                Size = new Size(110, 35)
            };

            btnActivate.Click += Activate_Click;
            AcceptButton = btnActivate;

            Controls.Add(title);
            Controls.Add(description);
            Controls.Add(txtCode);
            Controls.Add(btnActivate);
        }

        private void Activate_Click(object? sender, EventArgs e)
        {
            if (LicenseService.Activate(txtCode.Text))
            {
                MessageBox.Show(
                    "Licencia activada correctamente.",
                    "Maxiback Cleaner",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                DialogResult = DialogResult.OK;
                Close();
            }
            else
            {
                MessageBox.Show(
                    "El código ingresado no es válido.",
                    "Error de activación",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                txtCode.SelectAll();
                txtCode.Focus();
            }
        }
    }
}
