namespace Mstp.BacnetCapture
{
    partial class BacnetCapture
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(BacnetCapture));
            this.label1 = new System.Windows.Forms.Label();
            this.textBox1 = new System.Windows.Forms.TextBox();
            this.comboPort = new System.Windows.Forms.ComboBox();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.buttonGo = new System.Windows.Forms.Button();
            this.label4 = new System.Windows.Forms.Label();
            this.imageList = new System.Windows.Forms.ImageList(this.components);
            this.comboSpeed = new System.Windows.Forms.ComboBox();
            this.label5 = new System.Windows.Forms.Label();
            this.checksbox = new System.Windows.Forms.PictureBox();
            this.checkToken = new System.Windows.Forms.CheckBox();
            this.checkMaster = new System.Windows.Forms.CheckBox();
            this.checkTest = new System.Windows.Forms.CheckBox();
            this.checkData = new System.Windows.Forms.CheckBox();
            this.treeView = new Mstp.BacnetCapture.BufferedTreeView();
            ((System.ComponentModel.ISupportInitialize)(this.checksbox)).BeginInit();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 22);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(214, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "Wireshark must listen the following pipeline :";
            // 
            // textBox1
            // 
            this.textBox1.Location = new System.Drawing.Point(240, 19);
            this.textBox1.Name = "textBox1";
            this.textBox1.ReadOnly = true;
            this.textBox1.Size = new System.Drawing.Size(150, 20);
            this.textBox1.TabIndex = 1;
            this.textBox1.Text = "\\\\.\\pipe\\bacnet";
            this.textBox1.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // comboPort
            // 
            this.comboPort.FormattingEnabled = true;
            this.comboPort.Location = new System.Drawing.Point(77, 70);
            this.comboPort.Name = "comboPort";
            this.comboPort.Size = new System.Drawing.Size(86, 21);
            this.comboPort.TabIndex = 2;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(12, 73);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(61, 13);
            this.label2.TabIndex = 3;
            this.label2.Text = "Serial Port :";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(276, 73);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(44, 13);
            this.label3.TabIndex = 5;
            this.label3.Text = "Speed :";
            // 
            // buttonGo
            // 
            this.buttonGo.Location = new System.Drawing.Point(15, 121);
            this.buttonGo.Name = "buttonGo";
            this.buttonGo.Size = new System.Drawing.Size(377, 23);
            this.buttonGo.TabIndex = 6;
            this.buttonGo.Text = "Go";
            this.buttonGo.UseVisualStyleBackColor = true;
            this.buttonGo.Click += new System.EventHandler(this.buttonGo_Click);
            // 
            // label4
            // 
            this.label4.Location = new System.Drawing.Point(1, 154);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(123, 13);
            this.label4.TabIndex = 9;
            this.label4.Text = "Suppress filters";
            this.label4.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            // 
            // imageList
            // 
            this.imageList.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("imageList.ImageStream")));
            this.imageList.TransparentColor = System.Drawing.Color.Transparent;
            this.imageList.Images.SetKeyName(0, "Plc.png");
            this.imageList.Images.SetKeyName(1, "Stat2.png");
            this.imageList.Images.SetKeyName(2, "Loupe.png");
            // 
            // comboSpeed
            // 
            this.comboSpeed.FormattingEnabled = true;
            this.comboSpeed.Items.AddRange(new object[] {
            "9600",
            "19200",
            "38400",
            "57600",
            "76800",
            "115200"});
            this.comboSpeed.Location = new System.Drawing.Point(329, 70);
            this.comboSpeed.Name = "comboSpeed";
            this.comboSpeed.Size = new System.Drawing.Size(63, 21);
            this.comboSpeed.TabIndex = 11;
            this.comboSpeed.Text = "38400";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(188, 154);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(129, 13);
            this.label5.TabIndex = 12;
            this.label5.Text = "Sent\'s messages statistics";
            // 
            // checksbox
            // 
            this.checksbox.BackColor = System.Drawing.SystemColors.ButtonHighlight;
            this.checksbox.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.checksbox.Location = new System.Drawing.Point(15, 179);
            this.checksbox.Name = "checksbox";
            this.checksbox.Size = new System.Drawing.Size(100, 244);
            this.checksbox.TabIndex = 13;
            this.checksbox.TabStop = false;
            // 
            // checkToken
            // 
            this.checkToken.AutoSize = true;
            this.checkToken.BackColor = System.Drawing.SystemColors.ButtonHighlight;
            this.checkToken.Location = new System.Drawing.Point(22, 193);
            this.checkToken.Name = "checkToken";
            this.checkToken.Size = new System.Drawing.Size(57, 17);
            this.checkToken.TabIndex = 14;
            this.checkToken.Text = "Token";
            this.checkToken.UseVisualStyleBackColor = false;
            this.checkToken.CheckedChanged += new System.EventHandler(this.checkedFiltresValue_Changed);
            // 
            // checkMaster
            // 
            this.checkMaster.AutoSize = true;
            this.checkMaster.BackColor = System.Drawing.SystemColors.ButtonHighlight;
            this.checkMaster.Location = new System.Drawing.Point(22, 257);
            this.checkMaster.Name = "checkMaster";
            this.checkMaster.Size = new System.Drawing.Size(92, 17);
            this.checkMaster.TabIndex = 15;
            this.checkMaster.Text = "Master Polling";
            this.checkMaster.UseVisualStyleBackColor = false;
            this.checkMaster.CheckedChanged += new System.EventHandler(this.checkedFiltresValue_Changed);
            // 
            // checkTest
            // 
            this.checkTest.AutoSize = true;
            this.checkTest.BackColor = System.Drawing.SystemColors.ButtonHighlight;
            this.checkTest.Location = new System.Drawing.Point(22, 321);
            this.checkTest.Name = "checkTest";
            this.checkTest.Size = new System.Drawing.Size(47, 17);
            this.checkTest.TabIndex = 16;
            this.checkTest.Text = "Test";
            this.checkTest.UseVisualStyleBackColor = false;
            this.checkTest.CheckedChanged += new System.EventHandler(this.checkedFiltresValue_Changed);
            // 
            // checkData
            // 
            this.checkData.AutoSize = true;
            this.checkData.BackColor = System.Drawing.SystemColors.ButtonHighlight;
            this.checkData.Location = new System.Drawing.Point(22, 385);
            this.checkData.Name = "checkData";
            this.checkData.Size = new System.Drawing.Size(49, 17);
            this.checkData.TabIndex = 17;
            this.checkData.Text = "Data";
            this.checkData.UseVisualStyleBackColor = false;
            this.checkData.CheckedChanged += new System.EventHandler(this.checkedFiltresValue_Changed);
            // 
            // treeView
            // 
            this.treeView.ImageIndex = 0;
            this.treeView.ImageList = this.imageList;
            this.treeView.Location = new System.Drawing.Point(121, 179);
            this.treeView.Name = "treeView";
            this.treeView.SelectedImageIndex = 0;
            this.treeView.Size = new System.Drawing.Size(271, 244);
            this.treeView.TabIndex = 10;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(404, 626);
            this.Controls.Add(this.checkData);
            this.Controls.Add(this.checkTest);
            this.Controls.Add(this.checkMaster);
            this.Controls.Add(this.checkToken);
            this.Controls.Add(this.checksbox);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.comboSpeed);
            this.Controls.Add(this.treeView);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.buttonGo);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.comboPort);
            this.Controls.Add(this.textBox1);
            this.Controls.Add(this.label1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.Name = "Form1";
            this.Text = "Wireshark Mstp.BacnetCapture";
            this.Load += new System.EventHandler(this.Form1_Load);
            this.Resize += new System.EventHandler(this.Form1_Resize);
            ((System.ComponentModel.ISupportInitialize)(this.checksbox)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox textBox1;
        private System.Windows.Forms.ComboBox comboPort;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Button buttonGo;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.ImageList imageList;
        private System.Windows.Forms.ComboBox comboSpeed;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.PictureBox checksbox;
        private System.Windows.Forms.CheckBox checkToken;
        private System.Windows.Forms.CheckBox checkMaster;
        private System.Windows.Forms.CheckBox checkTest;
        private System.Windows.Forms.CheckBox checkData;
        private BufferedTreeView treeView;
    }
}

