using System;
using System.Drawing;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace EasyCopyPaste
{
    public partial class SettingsForm : Form
    {
        private CheckBox autoStartCheck;
        private Label statusLabel;
        private Button saveButton;
        private Panel mainPanel;

        public SettingsForm()
        {
            InitializeComponents();
            this.FormClosing += SettingsForm_FormClosing;
        }

        public void SetCurrentSettings(bool autoStart)
        {
            autoStartCheck.Checked = autoStart;
        }

        private void InitializeComponents()
        {
            this.Text = "Enhanced Copy Paste Settings";
            this.Size = new Size(400, 300);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.White;

            mainPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(20)
            };

            // Title
            var titleLabel = new Label
            {
                Text = "Settings",
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                Location = new Point(20, 20),
                AutoSize = true,
                ForeColor = Color.FromArgb(0, 120, 212)
            };

            // Auto Start
            autoStartCheck = new CheckBox
            {
                Text = "Start with Windows",
                Location = new Point(20, 70),
                AutoSize = true,
                Font = new Font("Segoe UI", 10)
            };

            // Instructions
            var instructionsLabel = new Label
            {
                Text = "How to use:\n" +
                      "• Select text to copy automatically\n" +
                      "• Middle-click to paste\n" +
                      "• Right-click tray icon to access menu",
                Location = new Point(20, 110),
                Size = new Size(340, 80),
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.Gray
            };

            // Status Label
            statusLabel = new Label
            {
                Location = new Point(20, 200),
                AutoSize = true,
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.FromArgb(0, 120, 212),
                Visible = false
            };

            // Save Button
            saveButton = new Button
            {
                Text = "Save",
                Location = new Point(150, 220),
                Size = new Size(100, 35),
                BackColor = Color.FromArgb(0, 120, 212),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            saveButton.Click += SaveButton_Click;

            // Add controls to panel
            mainPanel.Controls.AddRange(new Control[] {
                titleLabel,
                autoStartCheck,
                instructionsLabel,
                statusLabel,
                saveButton
            });

            // Add panel to form
            this.Controls.Add(mainPanel);

            // Add shadow and modern look
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterScreen;
        }

        private void SaveButton_Click(object sender, EventArgs e)
        {
            ShowStatus("Settings saved!");
            System.Threading.Tasks.Task.Delay(500).ContinueWith(_ =>
            {
                if (!this.IsDisposed && this.InvokeRequired)
                {
                    this.Invoke(new Action(() =>
                    {
                        this.DialogResult = DialogResult.OK;
                        this.Close();
                    }));
                }
            });
        }

        private void ShowStatus(string message)
        {
            statusLabel.Text = message;
            statusLabel.Visible = true;
            statusLabel.ForeColor = Color.FromArgb(0, 120, 212);
            Application.DoEvents();
        }

        private void SettingsForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing && this.DialogResult != DialogResult.OK)
            {
                this.DialogResult = DialogResult.Cancel;
            }
        }

        public bool AutoStart => autoStartCheck.Checked;

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ClassStyle |= 0x200; 
                return cp;
            }
        }
    }
}