namespace Bacnet.Room.Simulator
{
    partial class BacForm
    {
        /// <summary>
        /// Variable nécessaire au concepteur.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Nettoyage des ressources utilisées.
        /// </summary>
        /// <param name="disposing">true si les ressources managées doivent être supprimées ; sinon, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Code généré par le Concepteur Windows Form

        /// <summary>
        /// Méthode requise pour la prise en charge du concepteur - ne modifiez pas
        /// le contenu de cette méthode avec l'éditeur de code.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(BacForm));
            this.Set1 = new System.Windows.Forms.Button();
            this.Set2 = new System.Windows.Forms.Button();
            this.Set3 = new System.Windows.Forms.Button();
            this.ScreenOnOff = new System.Windows.Forms.Button();
            this.Set1Label = new System.Windows.Forms.Label();
            this.Set2Label = new System.Windows.Forms.Label();
            this.Set3Label = new System.Windows.Forms.Label();
            this.TempInt = new System.Windows.Forms.Label();
            this.TempSet = new System.Windows.Forms.Label();
            this.TmrUpdate = new System.Windows.Forms.Timer(this.components);
            this.panel1 = new System.Windows.Forms.Panel();
            this.pictureModeArret = new System.Windows.Forms.PictureBox();
            this.pictureModeChaud = new System.Windows.Forms.PictureBox();
            this.pictureModeFroid = new System.Windows.Forms.PictureBox();
            this.TempExt = new System.Windows.Forms.Label();
            this.Chauf3 = new System.Windows.Forms.PictureBox();
            this.Chauf2 = new System.Windows.Forms.PictureBox();
            this.Chauf1 = new System.Windows.Forms.PictureBox();
            this.Clim3 = new System.Windows.Forms.PictureBox();
            this.Clim2 = new System.Windows.Forms.PictureBox();
            this.Clim1 = new System.Windows.Forms.PictureBox();
            this.pictureBox3 = new System.Windows.Forms.PictureBox();
            this.pictureBox2 = new System.Windows.Forms.PictureBox();
            this.panel2 = new System.Windows.Forms.Panel();
            this.bacnetid = new System.Windows.Forms.Label();
            this.networkInterfaces = new System.Windows.Forms.ComboBox();
            this.IP = new System.Windows.Forms.Label();
            this.panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureModeArret)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureModeChaud)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureModeFroid)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.Chauf3)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.Chauf2)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.Chauf1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.Clim3)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.Clim2)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.Clim1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox3)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox2)).BeginInit();
            this.panel2.SuspendLayout();
            this.SuspendLayout();
            // 
            // Set1
            // 
            this.Set1.BackColor = System.Drawing.Color.Red;
            this.Set1.Location = new System.Drawing.Point(12, 159);
            this.Set1.Name = "Set1";
            this.Set1.Size = new System.Drawing.Size(34, 28);
            this.Set1.TabIndex = 0;
            this.Set1.Text = "1";
            this.Set1.UseVisualStyleBackColor = false;
            this.Set1.Click += new System.EventHandler(this.SetRef_Click);
            // 
            // Set2
            // 
            this.Set2.Location = new System.Drawing.Point(12, 198);
            this.Set2.Name = "Set2";
            this.Set2.Size = new System.Drawing.Size(34, 28);
            this.Set2.TabIndex = 1;
            this.Set2.Text = "2";
            this.Set2.UseVisualStyleBackColor = true;
            this.Set2.Click += new System.EventHandler(this.SetRef_Click);
            // 
            // Set3
            // 
            this.Set3.Location = new System.Drawing.Point(12, 241);
            this.Set3.Name = "Set3";
            this.Set3.Size = new System.Drawing.Size(34, 28);
            this.Set3.TabIndex = 2;
            this.Set3.Text = "3";
            this.Set3.UseVisualStyleBackColor = true;
            this.Set3.Click += new System.EventHandler(this.SetRef_Click);
            // 
            // ScreenOnOff
            // 
            this.ScreenOnOff.Location = new System.Drawing.Point(12, 12);
            this.ScreenOnOff.Name = "ScreenOnOff";
            this.ScreenOnOff.Size = new System.Drawing.Size(34, 125);
            this.ScreenOnOff.TabIndex = 3;
            this.ScreenOnOff.UseVisualStyleBackColor = true;
            this.ScreenOnOff.Click += new System.EventHandler(this.ScreenOnOff_Click);
            // 
            // Set1Label
            // 
            this.Set1Label.BackColor = System.Drawing.SystemColors.ButtonHighlight;
            this.Set1Label.Font = new System.Drawing.Font("Bell MT", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Set1Label.Location = new System.Drawing.Point(6, 147);
            this.Set1Label.Name = "Set1Label";
            this.Set1Label.Size = new System.Drawing.Size(189, 18);
            this.Set1Label.TabIndex = 5;
            this.Set1Label.Text = "Mode Confort";
            this.Set1Label.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            // 
            // Set2Label
            // 
            this.Set2Label.BackColor = System.Drawing.SystemColors.ButtonHighlight;
            this.Set2Label.Font = new System.Drawing.Font("Bell MT", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Set2Label.ForeColor = System.Drawing.SystemColors.ControlDark;
            this.Set2Label.Location = new System.Drawing.Point(6, 187);
            this.Set2Label.Name = "Set2Label";
            this.Set2Label.Size = new System.Drawing.Size(189, 18);
            this.Set2Label.TabIndex = 6;
            this.Set2Label.Text = "Mode Eco+";
            this.Set2Label.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            // 
            // Set3Label
            // 
            this.Set3Label.BackColor = System.Drawing.SystemColors.ButtonHighlight;
            this.Set3Label.Font = new System.Drawing.Font("Bell MT", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Set3Label.ForeColor = System.Drawing.SystemColors.ControlDark;
            this.Set3Label.Location = new System.Drawing.Point(6, 229);
            this.Set3Label.Name = "Set3Label";
            this.Set3Label.Size = new System.Drawing.Size(189, 18);
            this.Set3Label.TabIndex = 7;
            this.Set3Label.Text = "Mode Vacation";
            this.Set3Label.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            // 
            // TempInt
            // 
            this.TempInt.BackColor = System.Drawing.SystemColors.ButtonHighlight;
            this.TempInt.Font = new System.Drawing.Font("Bell MT", 36F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.TempInt.Location = new System.Drawing.Point(3, 53);
            this.TempInt.Name = "TempInt";
            this.TempInt.Size = new System.Drawing.Size(192, 54);
            this.TempInt.TabIndex = 9;
            this.TempInt.Text = "20°C";
            this.TempInt.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // TempSet
            // 
            this.TempSet.AutoSize = true;
            this.TempSet.BackColor = System.Drawing.SystemColors.ButtonHighlight;
            this.TempSet.Font = new System.Drawing.Font("Bell MT", 10F);
            this.TempSet.Location = new System.Drawing.Point(3, 0);
            this.TempSet.Name = "TempSet";
            this.TempSet.Size = new System.Drawing.Size(80, 17);
            this.TempSet.TabIndex = 11;
            this.TempSet.Text = "T Set : 21°C";
            // 
            // TmrUpdate
            // 
            this.TmrUpdate.Enabled = true;
            this.TmrUpdate.Interval = 1000;
            this.TmrUpdate.Tick += new System.EventHandler(this.TmrUpdate_Tick);
            // 
            // panel1
            // 
            this.panel1.BackColor = System.Drawing.SystemColors.ButtonHighlight;
            this.panel1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panel1.Controls.Add(this.pictureModeArret);
            this.panel1.Controls.Add(this.pictureModeChaud);
            this.panel1.Controls.Add(this.pictureModeFroid);
            this.panel1.Controls.Add(this.TempExt);
            this.panel1.Controls.Add(this.Chauf3);
            this.panel1.Controls.Add(this.Chauf2);
            this.panel1.Controls.Add(this.Chauf1);
            this.panel1.Controls.Add(this.Clim3);
            this.panel1.Controls.Add(this.Clim2);
            this.panel1.Controls.Add(this.Clim1);
            this.panel1.Controls.Add(this.TempSet);
            this.panel1.Controls.Add(this.pictureBox3);
            this.panel1.Controls.Add(this.Set1Label);
            this.panel1.Controls.Add(this.Set2Label);
            this.panel1.Controls.Add(this.Set3Label);
            this.panel1.Controls.Add(this.TempInt);
            this.panel1.Controls.Add(this.pictureBox2);
            this.panel1.Location = new System.Drawing.Point(5, 5);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(201, 260);
            this.panel1.TabIndex = 13;
            // 
            // pictureModeArret
            // 
            this.pictureModeArret.Image = global::Bacnet.Room.Simulator.Properties.Resources.HArret;
            this.pictureModeArret.Location = new System.Drawing.Point(164, 224);
            this.pictureModeArret.Name = "pictureModeArret";
            this.pictureModeArret.Size = new System.Drawing.Size(36, 35);
            this.pictureModeArret.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pictureModeArret.TabIndex = 23;
            this.pictureModeArret.TabStop = false;
            // 
            // pictureModeChaud
            // 
            this.pictureModeChaud.Image = global::Bacnet.Room.Simulator.Properties.Resources.H_Chaud;
            this.pictureModeChaud.Location = new System.Drawing.Point(163, 224);
            this.pictureModeChaud.Name = "pictureModeChaud";
            this.pictureModeChaud.Size = new System.Drawing.Size(36, 35);
            this.pictureModeChaud.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pictureModeChaud.TabIndex = 22;
            this.pictureModeChaud.TabStop = false;
            // 
            // pictureModeFroid
            // 
            this.pictureModeFroid.Image = global::Bacnet.Room.Simulator.Properties.Resources.H_Froid;
            this.pictureModeFroid.Location = new System.Drawing.Point(163, 224);
            this.pictureModeFroid.Name = "pictureModeFroid";
            this.pictureModeFroid.Size = new System.Drawing.Size(36, 35);
            this.pictureModeFroid.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pictureModeFroid.TabIndex = 21;
            this.pictureModeFroid.TabStop = false;
            // 
            // TempExt
            // 
            this.TempExt.BackColor = System.Drawing.SystemColors.ButtonHighlight;
            this.TempExt.Font = new System.Drawing.Font("Bell MT", 10F);
            this.TempExt.Location = new System.Drawing.Point(102, 0);
            this.TempExt.Name = "TempExt";
            this.TempExt.Size = new System.Drawing.Size(93, 17);
            this.TempExt.TabIndex = 20;
            this.TempExt.Text = "T Ext : 20°C";
            this.TempExt.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // Chauf3
            // 
            this.Chauf3.BackColor = System.Drawing.Color.Red;
            this.Chauf3.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.Chauf3.Location = new System.Drawing.Point(180, 103);
            this.Chauf3.Name = "Chauf3";
            this.Chauf3.Size = new System.Drawing.Size(5, 25);
            this.Chauf3.TabIndex = 19;
            this.Chauf3.TabStop = false;
            // 
            // Chauf2
            // 
            this.Chauf2.BackColor = System.Drawing.Color.Red;
            this.Chauf2.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.Chauf2.Location = new System.Drawing.Point(171, 108);
            this.Chauf2.Name = "Chauf2";
            this.Chauf2.Size = new System.Drawing.Size(5, 20);
            this.Chauf2.TabIndex = 18;
            this.Chauf2.TabStop = false;
            // 
            // Chauf1
            // 
            this.Chauf1.BackColor = System.Drawing.Color.Red;
            this.Chauf1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.Chauf1.Location = new System.Drawing.Point(163, 113);
            this.Chauf1.Name = "Chauf1";
            this.Chauf1.Size = new System.Drawing.Size(5, 15);
            this.Chauf1.TabIndex = 17;
            this.Chauf1.TabStop = false;
            // 
            // Clim3
            // 
            this.Clim3.BackColor = System.Drawing.Color.Blue;
            this.Clim3.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.Clim3.Location = new System.Drawing.Point(14, 103);
            this.Clim3.Name = "Clim3";
            this.Clim3.Size = new System.Drawing.Size(5, 25);
            this.Clim3.TabIndex = 15;
            this.Clim3.TabStop = false;
            // 
            // Clim2
            // 
            this.Clim2.BackColor = System.Drawing.Color.Blue;
            this.Clim2.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.Clim2.Location = new System.Drawing.Point(22, 108);
            this.Clim2.Name = "Clim2";
            this.Clim2.Size = new System.Drawing.Size(5, 20);
            this.Clim2.TabIndex = 14;
            this.Clim2.TabStop = false;
            // 
            // Clim1
            // 
            this.Clim1.BackColor = System.Drawing.Color.Blue;
            this.Clim1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.Clim1.Location = new System.Drawing.Point(30, 113);
            this.Clim1.Name = "Clim1";
            this.Clim1.Size = new System.Drawing.Size(5, 15);
            this.Clim1.TabIndex = 13;
            this.Clim1.TabStop = false;
            // 
            // pictureBox3
            // 
            this.pictureBox3.BackColor = System.Drawing.SystemColors.ControlLightLight;
            this.pictureBox3.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pictureBox3.Location = new System.Drawing.Point(27, 28);
            this.pictureBox3.Name = "pictureBox3";
            this.pictureBox3.Size = new System.Drawing.Size(141, 3);
            this.pictureBox3.TabIndex = 12;
            this.pictureBox3.TabStop = false;
            // 
            // pictureBox2
            // 
            this.pictureBox2.BackColor = System.Drawing.SystemColors.ControlLightLight;
            this.pictureBox2.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pictureBox2.Location = new System.Drawing.Point(28, 134);
            this.pictureBox2.Name = "pictureBox2";
            this.pictureBox2.Size = new System.Drawing.Size(141, 3);
            this.pictureBox2.TabIndex = 8;
            this.pictureBox2.TabStop = false;
            // 
            // panel2
            // 
            this.panel2.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panel2.Controls.Add(this.panel1);
            this.panel2.Location = new System.Drawing.Point(52, 12);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(213, 271);
            this.panel2.TabIndex = 14;
            // 
            // bacnetid
            // 
            this.bacnetid.Location = new System.Drawing.Point(12, 290);
            this.bacnetid.Name = "bacnetid";
            this.bacnetid.Size = new System.Drawing.Size(247, 18);
            this.bacnetid.TabIndex = 15;
            this.bacnetid.Text = "label1";
            this.bacnetid.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            // 
            // networkInterfaces
            // 
            this.networkInterfaces.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.networkInterfaces.FormattingEnabled = true;
            this.networkInterfaces.Location = new System.Drawing.Point(12, 330);
            this.networkInterfaces.Name = "networkInterfaces";
            this.networkInterfaces.Size = new System.Drawing.Size(247, 21);
            this.networkInterfaces.TabIndex = 16;
            this.networkInterfaces.SelectedIndexChanged += new System.EventHandler(this.networkInterfaces_SelectedIndexChanged);
            // 
            // IP
            // 
            this.IP.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F);
            this.IP.Location = new System.Drawing.Point(10, 308);
            this.IP.Name = "IP";
            this.IP.Size = new System.Drawing.Size(247, 18);
            this.IP.TabIndex = 17;
            this.IP.Text = "Bound IP Address";
            this.IP.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // BacForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(270, 357);
            this.Controls.Add(this.IP);
            this.Controls.Add(this.networkInterfaces);
            this.Controls.Add(this.bacnetid);
            this.Controls.Add(this.panel2);
            this.Controls.Add(this.ScreenOnOff);
            this.Controls.Add(this.Set3);
            this.Controls.Add(this.Set2);
            this.Controls.Add(this.Set1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Fixed3D;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.Name = "BacForm";
            this.Text = "Room Control Simul.";
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureModeArret)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureModeChaud)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureModeFroid)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.Chauf3)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.Chauf2)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.Chauf1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.Clim3)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.Clim2)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.Clim1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox3)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox2)).EndInit();
            this.panel2.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button Set1;
        private System.Windows.Forms.Button Set2;
        private System.Windows.Forms.Button Set3;
        private System.Windows.Forms.Button ScreenOnOff;
        private System.Windows.Forms.Label Set1Label;
        private System.Windows.Forms.Label Set2Label;
        private System.Windows.Forms.Label Set3Label;
        private System.Windows.Forms.PictureBox pictureBox2;
        private System.Windows.Forms.Label TempInt;
        private System.Windows.Forms.Label TempSet;
        private System.Windows.Forms.PictureBox pictureBox3;
        private System.Windows.Forms.Timer TmrUpdate;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.PictureBox Clim3;
        private System.Windows.Forms.PictureBox Clim2;
        private System.Windows.Forms.PictureBox Clim1;
        private System.Windows.Forms.PictureBox Chauf3;
        private System.Windows.Forms.PictureBox Chauf2;
        private System.Windows.Forms.PictureBox Chauf1;
        private System.Windows.Forms.Label TempExt;
        private System.Windows.Forms.Label bacnetid;
        private System.Windows.Forms.PictureBox pictureModeFroid;
        private System.Windows.Forms.PictureBox pictureModeChaud;
        private System.Windows.Forms.PictureBox pictureModeArret;
        private System.Windows.Forms.ComboBox networkInterfaces;
        private System.Windows.Forms.Label IP;
    }
}

