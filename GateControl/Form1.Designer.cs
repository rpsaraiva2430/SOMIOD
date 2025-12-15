namespace GateControl
{
    partial class Form1
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.Button btnCreateApp;
        private System.Windows.Forms.Button btnOpenGate;
        private System.Windows.Forms.Button btnCloseGate;
        private System.Windows.Forms.TextBox txtBaseUri;
        private System.Windows.Forms.Label lblBaseUri;
        private System.Windows.Forms.TextBox txtStatus;
        private System.Windows.Forms.Label lblStatus;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.btnCreateApp = new System.Windows.Forms.Button();
            this.btnOpenGate = new System.Windows.Forms.Button();
            this.btnCloseGate = new System.Windows.Forms.Button();
            this.txtBaseUri = new System.Windows.Forms.TextBox();
            this.lblBaseUri = new System.Windows.Forms.Label();
            this.txtStatus = new System.Windows.Forms.TextBox();
            this.lblStatus = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // btnCreateApp
            // 
            this.btnCreateApp.Location = new System.Drawing.Point(15, 55);
            this.btnCreateApp.Name = "btnCreateApp";
            this.btnCreateApp.Size = new System.Drawing.Size(120, 30);
            this.btnCreateApp.TabIndex = 0;
            this.btnCreateApp.Text = "Create App (gate-remote)";
            this.btnCreateApp.UseVisualStyleBackColor = true;
            this.btnCreateApp.Click += new System.EventHandler(this.BtnCreateApp_Click);
            // 
            // btnOpenGate
            // 
            this.btnOpenGate.Location = new System.Drawing.Point(150, 55);
            this.btnOpenGate.Name = "btnOpenGate";
            this.btnOpenGate.Size = new System.Drawing.Size(120, 30);
            this.btnOpenGate.TabIndex = 1;
            this.btnOpenGate.Text = "Open Gate";
            this.btnOpenGate.UseVisualStyleBackColor = true;
            this.btnOpenGate.Click += new System.EventHandler(this.BtnOpenGate_Click);
            // 
            // btnCloseGate
            // 
            this.btnCloseGate.Location = new System.Drawing.Point(285, 55);
            this.btnCloseGate.Name = "btnCloseGate";
            this.btnCloseGate.Size = new System.Drawing.Size(120, 30);
            this.btnCloseGate.TabIndex = 2;
            this.btnCloseGate.Text = "Close Gate";
            this.btnCloseGate.UseVisualStyleBackColor = true;
            this.btnCloseGate.Click += new System.EventHandler(this.BtnCloseGate_Click);
            // 
            // txtBaseUri
            // 
            this.txtBaseUri.Location = new System.Drawing.Point(85, 18);
            this.txtBaseUri.Name = "txtBaseUri";
            this.txtBaseUri.Size = new System.Drawing.Size(320, 20);
            this.txtBaseUri.TabIndex = 3;
            this.txtBaseUri.Text = "http://localhost:8080";
            // 
            // lblBaseUri
            // 
            this.lblBaseUri.AutoSize = true;
            this.lblBaseUri.Location = new System.Drawing.Point(12, 21);
            this.lblBaseUri.Name = "lblBaseUri";
            this.lblBaseUri.Size = new System.Drawing.Size(52, 13);
            this.lblBaseUri.TabIndex = 4;
            this.lblBaseUri.Text = "Base URL";
            // 
            // txtStatus
            // 
            this.txtStatus.Location = new System.Drawing.Point(15, 110);
            this.txtStatus.Multiline = true;
            this.txtStatus.Name = "txtStatus";
            this.txtStatus.ReadOnly = true;
            this.txtStatus.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtStatus.Size = new System.Drawing.Size(390, 150);
            this.txtStatus.TabIndex = 5;
            // 
            // lblStatus
            // 
            this.lblStatus.AutoSize = true;
            this.lblStatus.Location = new System.Drawing.Point(12, 94);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(37, 13);
            this.lblStatus.TabIndex = 6;
            this.lblStatus.Text = "Status";
            // 
            // Form1
            // 
            this.ClientSize = new System.Drawing.Size(424, 281);
            this.Controls.Add(this.lblStatus);
            this.Controls.Add(this.txtStatus);
            this.Controls.Add(this.lblBaseUri);
            this.Controls.Add(this.txtBaseUri);
            this.Controls.Add(this.btnCloseGate);
            this.Controls.Add(this.btnOpenGate);
            this.Controls.Add(this.btnCreateApp);
            this.Name = "Form1";
            this.Text = "Gate Remote - SOMIOD";
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion
    }
}

