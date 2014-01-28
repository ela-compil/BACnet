namespace Yabe
{
    partial class DeviceCommunicationControlDialog
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
            this.m_CancelButton = new System.Windows.Forms.Button();
            this.m_OKButton = new System.Windows.Forms.Button();
            this.m_disableCheck = new System.Windows.Forms.CheckBox();
            this.m_durationValue = new System.Windows.Forms.NumericUpDown();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.m_passwordText = new System.Windows.Forms.TextBox();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.m_communicationGroup = new System.Windows.Forms.GroupBox();
            this.m_reinitializeGroup = new System.Windows.Forms.GroupBox();
            this.label3 = new System.Windows.Forms.Label();
            this.m_StateCombo = new System.Windows.Forms.ComboBox();
            this.groupBox4 = new System.Windows.Forms.GroupBox();
            this.m_communicationRadio = new System.Windows.Forms.RadioButton();
            this.m_reinitializeRadio = new System.Windows.Forms.RadioButton();
            ((System.ComponentModel.ISupportInitialize)(this.m_durationValue)).BeginInit();
            this.groupBox1.SuspendLayout();
            this.m_communicationGroup.SuspendLayout();
            this.m_reinitializeGroup.SuspendLayout();
            this.groupBox4.SuspendLayout();
            this.SuspendLayout();
            // 
            // m_CancelButton
            // 
            this.m_CancelButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.m_CancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.m_CancelButton.Location = new System.Drawing.Point(141, 300);
            this.m_CancelButton.Name = "m_CancelButton";
            this.m_CancelButton.Size = new System.Drawing.Size(56, 23);
            this.m_CancelButton.TabIndex = 3;
            this.m_CancelButton.Text = "Cancel";
            this.m_CancelButton.UseVisualStyleBackColor = true;
            // 
            // m_OKButton
            // 
            this.m_OKButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.m_OKButton.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.m_OKButton.Location = new System.Drawing.Point(203, 300);
            this.m_OKButton.Name = "m_OKButton";
            this.m_OKButton.Size = new System.Drawing.Size(56, 23);
            this.m_OKButton.TabIndex = 2;
            this.m_OKButton.Text = "OK";
            this.m_OKButton.UseVisualStyleBackColor = true;
            // 
            // m_disableCheck
            // 
            this.m_disableCheck.AutoSize = true;
            this.m_disableCheck.CheckAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.m_disableCheck.Checked = true;
            this.m_disableCheck.CheckState = System.Windows.Forms.CheckState.Checked;
            this.m_disableCheck.Location = new System.Drawing.Point(57, 18);
            this.m_disableCheck.Name = "m_disableCheck";
            this.m_disableCheck.Size = new System.Drawing.Size(61, 17);
            this.m_disableCheck.TabIndex = 4;
            this.m_disableCheck.Text = "Disable";
            this.m_disableCheck.UseVisualStyleBackColor = true;
            // 
            // m_durationValue
            // 
            this.m_durationValue.Location = new System.Drawing.Point(104, 41);
            this.m_durationValue.Name = "m_durationValue";
            this.m_durationValue.Size = new System.Drawing.Size(56, 20);
            this.m_durationValue.TabIndex = 5;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(6, 43);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(92, 13);
            this.label1.TabIndex = 6;
            this.label1.Text = "Duration (minutes)";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(45, 22);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(53, 13);
            this.label2.TabIndex = 7;
            this.label2.Text = "Password";
            // 
            // m_passwordText
            // 
            this.m_passwordText.Location = new System.Drawing.Point(104, 19);
            this.m_passwordText.Name = "m_passwordText";
            this.m_passwordText.PasswordChar = '*';
            this.m_passwordText.Size = new System.Drawing.Size(121, 20);
            this.m_passwordText.TabIndex = 8;
            // 
            // groupBox1
            // 
            this.groupBox1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox1.Controls.Add(this.m_passwordText);
            this.groupBox1.Controls.Add(this.label2);
            this.groupBox1.Location = new System.Drawing.Point(12, 235);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(247, 57);
            this.groupBox1.TabIndex = 9;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Optional";
            // 
            // m_communicationGroup
            // 
            this.m_communicationGroup.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.m_communicationGroup.Controls.Add(this.m_disableCheck);
            this.m_communicationGroup.Controls.Add(this.m_durationValue);
            this.m_communicationGroup.Controls.Add(this.label1);
            this.m_communicationGroup.Enabled = false;
            this.m_communicationGroup.Location = new System.Drawing.Point(12, 154);
            this.m_communicationGroup.Name = "m_communicationGroup";
            this.m_communicationGroup.Size = new System.Drawing.Size(247, 75);
            this.m_communicationGroup.TabIndex = 10;
            this.m_communicationGroup.TabStop = false;
            this.m_communicationGroup.Text = "Communication";
            // 
            // m_reinitializeGroup
            // 
            this.m_reinitializeGroup.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.m_reinitializeGroup.Controls.Add(this.label3);
            this.m_reinitializeGroup.Controls.Add(this.m_StateCombo);
            this.m_reinitializeGroup.Location = new System.Drawing.Point(12, 91);
            this.m_reinitializeGroup.Name = "m_reinitializeGroup";
            this.m_reinitializeGroup.Size = new System.Drawing.Size(247, 57);
            this.m_reinitializeGroup.TabIndex = 11;
            this.m_reinitializeGroup.TabStop = false;
            this.m_reinitializeGroup.Text = "Reinitialize";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(66, 22);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(32, 13);
            this.label3.TabIndex = 8;
            this.label3.Text = "State";
            // 
            // m_StateCombo
            // 
            this.m_StateCombo.FormattingEnabled = true;
            this.m_StateCombo.Location = new System.Drawing.Point(104, 19);
            this.m_StateCombo.Name = "m_StateCombo";
            this.m_StateCombo.Size = new System.Drawing.Size(121, 21);
            this.m_StateCombo.TabIndex = 0;
            // 
            // groupBox4
            // 
            this.groupBox4.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox4.Controls.Add(this.m_communicationRadio);
            this.groupBox4.Controls.Add(this.m_reinitializeRadio);
            this.groupBox4.Location = new System.Drawing.Point(12, 12);
            this.groupBox4.Name = "groupBox4";
            this.groupBox4.Size = new System.Drawing.Size(247, 73);
            this.groupBox4.TabIndex = 12;
            this.groupBox4.TabStop = false;
            this.groupBox4.Text = "Action";
            // 
            // m_communicationRadio
            // 
            this.m_communicationRadio.AutoSize = true;
            this.m_communicationRadio.CheckAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.m_communicationRadio.Location = new System.Drawing.Point(21, 42);
            this.m_communicationRadio.Name = "m_communicationRadio";
            this.m_communicationRadio.Size = new System.Drawing.Size(97, 17);
            this.m_communicationRadio.TabIndex = 1;
            this.m_communicationRadio.Text = "Communication";
            this.m_communicationRadio.UseVisualStyleBackColor = true;
            this.m_communicationRadio.CheckedChanged += new System.EventHandler(this.reinitializeRadio_CheckedChanged);
            // 
            // m_reinitializeRadio
            // 
            this.m_reinitializeRadio.AutoSize = true;
            this.m_reinitializeRadio.CheckAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.m_reinitializeRadio.Checked = true;
            this.m_reinitializeRadio.Location = new System.Drawing.Point(43, 19);
            this.m_reinitializeRadio.Name = "m_reinitializeRadio";
            this.m_reinitializeRadio.Size = new System.Drawing.Size(75, 17);
            this.m_reinitializeRadio.TabIndex = 0;
            this.m_reinitializeRadio.TabStop = true;
            this.m_reinitializeRadio.Text = "Reinitialize";
            this.m_reinitializeRadio.UseVisualStyleBackColor = true;
            this.m_reinitializeRadio.CheckedChanged += new System.EventHandler(this.reinitializeRadio_CheckedChanged);
            // 
            // DeviceCommunicationControlDialog
            // 
            this.AcceptButton = this.m_OKButton;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.m_CancelButton;
            this.ClientSize = new System.Drawing.Size(271, 335);
            this.Controls.Add(this.groupBox4);
            this.Controls.Add(this.m_reinitializeGroup);
            this.Controls.Add(this.m_communicationGroup);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.m_CancelButton);
            this.Controls.Add(this.m_OKButton);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "DeviceCommunicationControlDialog";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Device Communication Control";
            this.Load += new System.EventHandler(this.DeviceCommunicationControlDialog_Load);
            ((System.ComponentModel.ISupportInitialize)(this.m_durationValue)).EndInit();
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.m_communicationGroup.ResumeLayout(false);
            this.m_communicationGroup.PerformLayout();
            this.m_reinitializeGroup.ResumeLayout(false);
            this.m_reinitializeGroup.PerformLayout();
            this.groupBox4.ResumeLayout(false);
            this.groupBox4.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button m_CancelButton;
        private System.Windows.Forms.Button m_OKButton;
        private System.Windows.Forms.CheckBox m_disableCheck;
        private System.Windows.Forms.NumericUpDown m_durationValue;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox m_passwordText;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.GroupBox m_communicationGroup;
        private System.Windows.Forms.GroupBox m_reinitializeGroup;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.ComboBox m_StateCombo;
        private System.Windows.Forms.GroupBox groupBox4;
        private System.Windows.Forms.RadioButton m_communicationRadio;
        private System.Windows.Forms.RadioButton m_reinitializeRadio;
    }
}