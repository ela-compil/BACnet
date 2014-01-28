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
            ((System.ComponentModel.ISupportInitialize)(this.m_durationValue)).BeginInit();
            this.SuspendLayout();
            // 
            // m_CancelButton
            // 
            this.m_CancelButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.m_CancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.m_CancelButton.Location = new System.Drawing.Point(90, 88);
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
            this.m_OKButton.Location = new System.Drawing.Point(152, 88);
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
            this.m_disableCheck.Location = new System.Drawing.Point(59, 12);
            this.m_disableCheck.Name = "m_disableCheck";
            this.m_disableCheck.Size = new System.Drawing.Size(61, 17);
            this.m_disableCheck.TabIndex = 4;
            this.m_disableCheck.Text = "Disable";
            this.m_disableCheck.UseVisualStyleBackColor = true;
            // 
            // m_durationValue
            // 
            this.m_durationValue.Location = new System.Drawing.Point(106, 35);
            this.m_durationValue.Name = "m_durationValue";
            this.m_durationValue.Size = new System.Drawing.Size(56, 20);
            this.m_durationValue.TabIndex = 5;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(8, 37);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(92, 13);
            this.label1.TabIndex = 6;
            this.label1.Text = "Duration (minutes)";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(47, 64);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(53, 13);
            this.label2.TabIndex = 7;
            this.label2.Text = "Password";
            // 
            // m_passwordText
            // 
            this.m_passwordText.Location = new System.Drawing.Point(106, 61);
            this.m_passwordText.Name = "m_passwordText";
            this.m_passwordText.PasswordChar = '*';
            this.m_passwordText.Size = new System.Drawing.Size(100, 20);
            this.m_passwordText.TabIndex = 8;
            // 
            // DeviceCommunicationControlDialog
            // 
            this.AcceptButton = this.m_OKButton;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.m_CancelButton;
            this.ClientSize = new System.Drawing.Size(220, 123);
            this.Controls.Add(this.m_passwordText);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.m_durationValue);
            this.Controls.Add(this.m_disableCheck);
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
            ((System.ComponentModel.ISupportInitialize)(this.m_durationValue)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button m_CancelButton;
        private System.Windows.Forms.Button m_OKButton;
        private System.Windows.Forms.CheckBox m_disableCheck;
        private System.Windows.Forms.NumericUpDown m_durationValue;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox m_passwordText;
    }
}