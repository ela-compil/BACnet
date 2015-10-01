namespace Yabe
{
    partial class SearchDialog
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SearchDialog));
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.label10 = new System.Windows.Forms.Label();
            this.m_localUdpEndpointsCombo = new System.Windows.Forms.ComboBox();
            this.m_AddUdpButton = new System.Windows.Forms.Button();
            this.m_PortValue = new System.Windows.Forms.NumericUpDown();
            this.label1 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.m_MaxInfoFramesValue = new System.Windows.Forms.NumericUpDown();
            this.label8 = new System.Windows.Forms.Label();
            this.m_MaxMasterValue = new System.Windows.Forms.NumericUpDown();
            this.label7 = new System.Windows.Forms.Label();
            this.m_SourceAddressValue = new System.Windows.Forms.NumericUpDown();
            this.label6 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.m_BaudValue = new System.Windows.Forms.NumericUpDown();
            this.m_AddSerialButton = new System.Windows.Forms.Button();
            this.label4 = new System.Windows.Forms.Label();
            this.m_SerialPortCombo = new System.Windows.Forms.ComboBox();
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.m_TimeoutValue = new System.Windows.Forms.NumericUpDown();
            this.m_RetriesValue = new System.Windows.Forms.NumericUpDown();
            this.groupBox4 = new System.Windows.Forms.GroupBox();
            this.m_PasswordText = new System.Windows.Forms.TextBox();
            this.label9 = new System.Windows.Forms.Label();
            this.label12 = new System.Windows.Forms.Label();
            this.m_PtpBaudRate = new System.Windows.Forms.NumericUpDown();
            this.m_AddPtpSerialButton = new System.Windows.Forms.Button();
            this.label13 = new System.Windows.Forms.Label();
            this.m_SerialPtpPortCombo = new System.Windows.Forms.ComboBox();
            this.groupBox5 = new System.Windows.Forms.GroupBox();
            this.m_EthernetInterfaceCombo = new System.Windows.Forms.ComboBox();
            this.label11 = new System.Windows.Forms.Label();
            this.m_AddEthernetButton = new System.Windows.Forms.Button();
            this.groupBox1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.m_PortValue)).BeginInit();
            this.groupBox2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.m_MaxInfoFramesValue)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.m_MaxMasterValue)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.m_SourceAddressValue)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.m_BaudValue)).BeginInit();
            this.groupBox3.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.m_TimeoutValue)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.m_RetriesValue)).BeginInit();
            this.groupBox4.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.m_PtpBaudRate)).BeginInit();
            this.groupBox5.SuspendLayout();
            this.SuspendLayout();
            // 
            // groupBox1
            // 
            this.groupBox1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox1.Controls.Add(this.label10);
            this.groupBox1.Controls.Add(this.m_localUdpEndpointsCombo);
            this.groupBox1.Controls.Add(this.m_AddUdpButton);
            this.groupBox1.Controls.Add(this.m_PortValue);
            this.groupBox1.Controls.Add(this.label1);
            this.groupBox1.Location = new System.Drawing.Point(12, 70);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(274, 82);
            this.groupBox1.TabIndex = 0;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "BACnet/IP over Udp";
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.Location = new System.Drawing.Point(10, 48);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(77, 13);
            this.label10.TabIndex = 8;
            this.label10.Text = "Local endpoint";
            // 
            // m_localUdpEndpointsCombo
            // 
            this.m_localUdpEndpointsCombo.DataBindings.Add(new System.Windows.Forms.Binding("Text", global::Yabe.Properties.Settings.Default, "DefaultUdpIp", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.m_localUdpEndpointsCombo.FormattingEnabled = true;
            this.m_localUdpEndpointsCombo.Location = new System.Drawing.Point(98, 45);
            this.m_localUdpEndpointsCombo.Name = "m_localUdpEndpointsCombo";
            this.m_localUdpEndpointsCombo.Size = new System.Drawing.Size(100, 21);
            this.m_localUdpEndpointsCombo.TabIndex = 7;
            this.m_localUdpEndpointsCombo.Text = global::Yabe.Properties.Settings.Default.DefaultUdpIp;
            // 
            // m_AddUdpButton
            // 
            this.m_AddUdpButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.m_AddUdpButton.Location = new System.Drawing.Point(208, 44);
            this.m_AddUdpButton.Name = "m_AddUdpButton";
            this.m_AddUdpButton.Size = new System.Drawing.Size(60, 23);
            this.m_AddUdpButton.TabIndex = 6;
            this.m_AddUdpButton.Text = "Add";
            this.m_AddUdpButton.UseVisualStyleBackColor = true;
            this.m_AddUdpButton.Click += new System.EventHandler(this.m_SearchIpButton_Click);
            // 
            // m_PortValue
            // 
            this.m_PortValue.DataBindings.Add(new System.Windows.Forms.Binding("Value", global::Yabe.Properties.Settings.Default, "DefaultUdpPort", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.m_PortValue.Hexadecimal = true;
            this.m_PortValue.Location = new System.Drawing.Point(98, 19);
            this.m_PortValue.Maximum = new decimal(new int[] {
            9999999,
            0,
            0,
            0});
            this.m_PortValue.Name = "m_PortValue";
            this.m_PortValue.Size = new System.Drawing.Size(60, 20);
            this.m_PortValue.TabIndex = 1;
            this.m_PortValue.Value = global::Yabe.Properties.Settings.Default.DefaultUdpPort;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(10, 21);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(26, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "Port";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(122, 21);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(45, 13);
            this.label3.TabIndex = 4;
            this.label3.Text = "Timeout";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(10, 21);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(40, 13);
            this.label2.TabIndex = 2;
            this.label2.Text = "Retries";
            // 
            // groupBox2
            // 
            this.groupBox2.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox2.Controls.Add(this.m_MaxInfoFramesValue);
            this.groupBox2.Controls.Add(this.label8);
            this.groupBox2.Controls.Add(this.m_MaxMasterValue);
            this.groupBox2.Controls.Add(this.label7);
            this.groupBox2.Controls.Add(this.m_SourceAddressValue);
            this.groupBox2.Controls.Add(this.label6);
            this.groupBox2.Controls.Add(this.label5);
            this.groupBox2.Controls.Add(this.m_BaudValue);
            this.groupBox2.Controls.Add(this.m_AddSerialButton);
            this.groupBox2.Controls.Add(this.label4);
            this.groupBox2.Controls.Add(this.m_SerialPortCombo);
            this.groupBox2.Location = new System.Drawing.Point(12, 158);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(274, 160);
            this.groupBox2.TabIndex = 1;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "BACnet/MSTP over serial";
            // 
            // m_MaxInfoFramesValue
            // 
            this.m_MaxInfoFramesValue.DataBindings.Add(new System.Windows.Forms.Binding("Value", global::Yabe.Properties.Settings.Default, "DefaultMaxInfoFrames", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.m_MaxInfoFramesValue.Location = new System.Drawing.Point(98, 122);
            this.m_MaxInfoFramesValue.Maximum = new decimal(new int[] {
            255,
            0,
            0,
            0});
            this.m_MaxInfoFramesValue.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.m_MaxInfoFramesValue.Name = "m_MaxInfoFramesValue";
            this.m_MaxInfoFramesValue.Size = new System.Drawing.Size(47, 20);
            this.m_MaxInfoFramesValue.TabIndex = 15;
            this.m_MaxInfoFramesValue.Value = global::Yabe.Properties.Settings.Default.DefaultMaxInfoFrames;
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(10, 124);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(64, 13);
            this.label8.TabIndex = 14;
            this.label8.Text = "Max Frames";
            // 
            // m_MaxMasterValue
            // 
            this.m_MaxMasterValue.DataBindings.Add(new System.Windows.Forms.Binding("Value", global::Yabe.Properties.Settings.Default, "DefaultMaxMaster", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.m_MaxMasterValue.Location = new System.Drawing.Point(98, 96);
            this.m_MaxMasterValue.Maximum = new decimal(new int[] {
            127,
            0,
            0,
            0});
            this.m_MaxMasterValue.Name = "m_MaxMasterValue";
            this.m_MaxMasterValue.Size = new System.Drawing.Size(47, 20);
            this.m_MaxMasterValue.TabIndex = 13;
            this.m_MaxMasterValue.Value = global::Yabe.Properties.Settings.Default.DefaultMaxMaster;
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(10, 98);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(62, 13);
            this.label7.TabIndex = 12;
            this.label7.Text = "Max Master";
            // 
            // m_SourceAddressValue
            // 
            this.m_SourceAddressValue.DataBindings.Add(new System.Windows.Forms.Binding("Value", global::Yabe.Properties.Settings.Default, "DefaultSourceAddress", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.m_SourceAddressValue.Location = new System.Drawing.Point(98, 70);
            this.m_SourceAddressValue.Maximum = new decimal(new int[] {
            127,
            0,
            0,
            0});
            this.m_SourceAddressValue.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            -2147483648});
            this.m_SourceAddressValue.Name = "m_SourceAddressValue";
            this.m_SourceAddressValue.Size = new System.Drawing.Size(47, 20);
            this.m_SourceAddressValue.TabIndex = 11;
            this.m_SourceAddressValue.Value = global::Yabe.Properties.Settings.Default.DefaultSourceAddress;
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(10, 72);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(82, 13);
            this.label6.TabIndex = 10;
            this.label6.Text = "Source Address";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(10, 46);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(32, 13);
            this.label5.TabIndex = 8;
            this.label5.Text = "Baud";
            // 
            // m_BaudValue
            // 
            this.m_BaudValue.DataBindings.Add(new System.Windows.Forms.Binding("Value", global::Yabe.Properties.Settings.Default, "DefaultBaudrate", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.m_BaudValue.Location = new System.Drawing.Point(98, 44);
            this.m_BaudValue.Maximum = new decimal(new int[] {
            1215752191,
            23,
            0,
            0});
            this.m_BaudValue.Name = "m_BaudValue";
            this.m_BaudValue.Size = new System.Drawing.Size(68, 20);
            this.m_BaudValue.TabIndex = 9;
            this.m_BaudValue.Value = global::Yabe.Properties.Settings.Default.DefaultBaudrate;
            // 
            // m_AddSerialButton
            // 
            this.m_AddSerialButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.m_AddSerialButton.Location = new System.Drawing.Point(208, 121);
            this.m_AddSerialButton.Name = "m_AddSerialButton";
            this.m_AddSerialButton.Size = new System.Drawing.Size(60, 23);
            this.m_AddSerialButton.TabIndex = 7;
            this.m_AddSerialButton.Text = "Add";
            this.m_AddSerialButton.UseVisualStyleBackColor = true;
            this.m_AddSerialButton.Click += new System.EventHandler(this.m_AddSerialButton_Click);
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(10, 22);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(26, 13);
            this.label4.TabIndex = 1;
            this.label4.Text = "Port";
            // 
            // m_SerialPortCombo
            // 
            this.m_SerialPortCombo.FormattingEnabled = true;
            this.m_SerialPortCombo.Location = new System.Drawing.Point(98, 19);
            this.m_SerialPortCombo.Name = "m_SerialPortCombo";
            this.m_SerialPortCombo.Size = new System.Drawing.Size(100, 21);
            this.m_SerialPortCombo.TabIndex = 0;
            // 
            // groupBox3
            // 
            this.groupBox3.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox3.Controls.Add(this.m_TimeoutValue);
            this.groupBox3.Controls.Add(this.label2);
            this.groupBox3.Controls.Add(this.label3);
            this.groupBox3.Controls.Add(this.m_RetriesValue);
            this.groupBox3.Location = new System.Drawing.Point(12, 12);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Size = new System.Drawing.Size(274, 52);
            this.groupBox3.TabIndex = 2;
            this.groupBox3.TabStop = false;
            this.groupBox3.Text = "General";
            // 
            // m_TimeoutValue
            // 
            this.m_TimeoutValue.DataBindings.Add(new System.Windows.Forms.Binding("Value", global::Yabe.Properties.Settings.Default, "DefaultTimeout", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.m_TimeoutValue.Increment = new decimal(new int[] {
            1000,
            0,
            0,
            0});
            this.m_TimeoutValue.Location = new System.Drawing.Point(168, 19);
            this.m_TimeoutValue.Maximum = new decimal(new int[] {
            60000,
            0,
            0,
            0});
            this.m_TimeoutValue.Minimum = new decimal(new int[] {
            100,
            0,
            0,
            0});
            this.m_TimeoutValue.Name = "m_TimeoutValue";
            this.m_TimeoutValue.Size = new System.Drawing.Size(60, 20);
            this.m_TimeoutValue.TabIndex = 5;
            this.m_TimeoutValue.Value = global::Yabe.Properties.Settings.Default.DefaultTimeout;
            // 
            // m_RetriesValue
            // 
            this.m_RetriesValue.DataBindings.Add(new System.Windows.Forms.Binding("Value", global::Yabe.Properties.Settings.Default, "DefaultRetries", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.m_RetriesValue.Location = new System.Drawing.Point(56, 19);
            this.m_RetriesValue.Name = "m_RetriesValue";
            this.m_RetriesValue.Size = new System.Drawing.Size(60, 20);
            this.m_RetriesValue.TabIndex = 3;
            this.m_RetriesValue.Value = global::Yabe.Properties.Settings.Default.DefaultRetries;
            // 
            // groupBox4
            // 
            this.groupBox4.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox4.Controls.Add(this.m_PasswordText);
            this.groupBox4.Controls.Add(this.label9);
            this.groupBox4.Controls.Add(this.label12);
            this.groupBox4.Controls.Add(this.m_PtpBaudRate);
            this.groupBox4.Controls.Add(this.m_AddPtpSerialButton);
            this.groupBox4.Controls.Add(this.label13);
            this.groupBox4.Controls.Add(this.m_SerialPtpPortCombo);
            this.groupBox4.Location = new System.Drawing.Point(12, 324);
            this.groupBox4.Name = "groupBox4";
            this.groupBox4.Size = new System.Drawing.Size(274, 108);
            this.groupBox4.TabIndex = 3;
            this.groupBox4.TabStop = false;
            this.groupBox4.Text = "BACnet/PTP over serial";
            // 
            // m_PasswordText
            // 
            this.m_PasswordText.Location = new System.Drawing.Point(98, 70);
            this.m_PasswordText.Name = "m_PasswordText";
            this.m_PasswordText.PasswordChar = '*';
            this.m_PasswordText.Size = new System.Drawing.Size(100, 20);
            this.m_PasswordText.TabIndex = 11;
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(10, 73);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(53, 13);
            this.label9.TabIndex = 10;
            this.label9.Text = "Password";
            // 
            // label12
            // 
            this.label12.AutoSize = true;
            this.label12.Location = new System.Drawing.Point(10, 46);
            this.label12.Name = "label12";
            this.label12.Size = new System.Drawing.Size(32, 13);
            this.label12.TabIndex = 8;
            this.label12.Text = "Baud";
            // 
            // m_PtpBaudRate
            // 
            this.m_PtpBaudRate.DataBindings.Add(new System.Windows.Forms.Binding("Value", global::Yabe.Properties.Settings.Default, "DefaultBaudrate", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.m_PtpBaudRate.Location = new System.Drawing.Point(98, 44);
            this.m_PtpBaudRate.Maximum = new decimal(new int[] {
            1215752191,
            23,
            0,
            0});
            this.m_PtpBaudRate.Name = "m_PtpBaudRate";
            this.m_PtpBaudRate.Size = new System.Drawing.Size(68, 20);
            this.m_PtpBaudRate.TabIndex = 9;
            this.m_PtpBaudRate.Value = global::Yabe.Properties.Settings.Default.DefaultBaudrate;
            // 
            // m_AddPtpSerialButton
            // 
            this.m_AddPtpSerialButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.m_AddPtpSerialButton.Location = new System.Drawing.Point(208, 69);
            this.m_AddPtpSerialButton.Name = "m_AddPtpSerialButton";
            this.m_AddPtpSerialButton.Size = new System.Drawing.Size(60, 23);
            this.m_AddPtpSerialButton.TabIndex = 7;
            this.m_AddPtpSerialButton.Text = "Add";
            this.m_AddPtpSerialButton.UseVisualStyleBackColor = true;
            this.m_AddPtpSerialButton.Click += new System.EventHandler(this.m_AddPtpSerialButton_Click);
            // 
            // label13
            // 
            this.label13.AutoSize = true;
            this.label13.Location = new System.Drawing.Point(10, 22);
            this.label13.Name = "label13";
            this.label13.Size = new System.Drawing.Size(26, 13);
            this.label13.TabIndex = 1;
            this.label13.Text = "Port";
            // 
            // m_SerialPtpPortCombo
            // 
            this.m_SerialPtpPortCombo.FormattingEnabled = true;
            this.m_SerialPtpPortCombo.Location = new System.Drawing.Point(98, 19);
            this.m_SerialPtpPortCombo.Name = "m_SerialPtpPortCombo";
            this.m_SerialPtpPortCombo.Size = new System.Drawing.Size(100, 21);
            this.m_SerialPtpPortCombo.TabIndex = 0;
            // 
            // groupBox5
            // 
            this.groupBox5.Controls.Add(this.m_EthernetInterfaceCombo);
            this.groupBox5.Controls.Add(this.label11);
            this.groupBox5.Controls.Add(this.m_AddEthernetButton);
            this.groupBox5.Location = new System.Drawing.Point(12, 438);
            this.groupBox5.Name = "groupBox5";
            this.groupBox5.Size = new System.Drawing.Size(274, 85);
            this.groupBox5.TabIndex = 4;
            this.groupBox5.TabStop = false;
            this.groupBox5.Text = "BACnet/Ethernet";
            // 
            // m_EthernetInterfaceCombo
            // 
            this.m_EthernetInterfaceCombo.FormattingEnabled = true;
            this.m_EthernetInterfaceCombo.Location = new System.Drawing.Point(13, 48);
            this.m_EthernetInterfaceCombo.Name = "m_EthernetInterfaceCombo";
            this.m_EthernetInterfaceCombo.Size = new System.Drawing.Size(255, 21);
            this.m_EthernetInterfaceCombo.TabIndex = 12;
            // 
            // label11
            // 
            this.label11.AutoSize = true;
            this.label11.Location = new System.Drawing.Point(10, 26);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(49, 13);
            this.label11.TabIndex = 12;
            this.label11.Text = "Interface";
            // 
            // m_AddEthernetButton
            // 
            this.m_AddEthernetButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.m_AddEthernetButton.Location = new System.Drawing.Point(208, 21);
            this.m_AddEthernetButton.Name = "m_AddEthernetButton";
            this.m_AddEthernetButton.Size = new System.Drawing.Size(60, 23);
            this.m_AddEthernetButton.TabIndex = 12;
            this.m_AddEthernetButton.Text = "Add";
            this.m_AddEthernetButton.UseVisualStyleBackColor = true;
            this.m_AddEthernetButton.Click += new System.EventHandler(this.m_AddEthernetButton_Click);
            // 
            // SearchDialog
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(298, 535);
            this.Controls.Add(this.groupBox5);
            this.Controls.Add(this.groupBox4);
            this.Controls.Add(this.groupBox3);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.groupBox1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "SearchDialog";
            this.ShowInTaskbar = false;
            this.Text = "Search";
            this.Load += new System.EventHandler(this.SearchDialog_Load);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.m_PortValue)).EndInit();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.m_MaxInfoFramesValue)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.m_MaxMasterValue)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.m_SourceAddressValue)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.m_BaudValue)).EndInit();
            this.groupBox3.ResumeLayout(false);
            this.groupBox3.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.m_TimeoutValue)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.m_RetriesValue)).EndInit();
            this.groupBox4.ResumeLayout(false);
            this.groupBox4.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.m_PtpBaudRate)).EndInit();
            this.groupBox5.ResumeLayout(false);
            this.groupBox5.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.Button m_AddUdpButton;
        private System.Windows.Forms.NumericUpDown m_TimeoutValue;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.NumericUpDown m_RetriesValue;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.NumericUpDown m_PortValue;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.GroupBox groupBox3;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.ComboBox m_SerialPortCombo;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.NumericUpDown m_BaudValue;
        private System.Windows.Forms.Button m_AddSerialButton;
        private System.Windows.Forms.NumericUpDown m_MaxMasterValue;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.NumericUpDown m_SourceAddressValue;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.NumericUpDown m_MaxInfoFramesValue;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.GroupBox groupBox4;
        private System.Windows.Forms.Label label12;
        private System.Windows.Forms.NumericUpDown m_PtpBaudRate;
        private System.Windows.Forms.Button m_AddPtpSerialButton;
        private System.Windows.Forms.Label label13;
        private System.Windows.Forms.ComboBox m_SerialPtpPortCombo;
        private System.Windows.Forms.TextBox m_PasswordText;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.ComboBox m_localUdpEndpointsCombo;
        private System.Windows.Forms.GroupBox groupBox5;
        private System.Windows.Forms.ComboBox m_EthernetInterfaceCombo;
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.Button m_AddEthernetButton;
    }
}