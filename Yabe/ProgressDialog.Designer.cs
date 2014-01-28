namespace Yabe
{
    partial class ProgressDialog
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
            this.m_Label = new System.Windows.Forms.Label();
            this.m_progessbar = new System.Windows.Forms.ProgressBar();
            this.m_CancelButton = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // m_Label
            // 
            this.m_Label.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.m_Label.Location = new System.Drawing.Point(12, 9);
            this.m_Label.Name = "m_Label";
            this.m_Label.Size = new System.Drawing.Size(300, 17);
            this.m_Label.TabIndex = 0;
            this.m_Label.Text = "Progress";
            this.m_Label.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // m_progessbar
            // 
            this.m_progessbar.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.m_progessbar.Location = new System.Drawing.Point(12, 29);
            this.m_progessbar.Name = "m_progessbar";
            this.m_progessbar.Size = new System.Drawing.Size(300, 23);
            this.m_progessbar.TabIndex = 1;
            // 
            // m_CancelButton
            // 
            this.m_CancelButton.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
            this.m_CancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.m_CancelButton.Location = new System.Drawing.Point(125, 60);
            this.m_CancelButton.Name = "m_CancelButton";
            this.m_CancelButton.Size = new System.Drawing.Size(75, 23);
            this.m_CancelButton.TabIndex = 3;
            this.m_CancelButton.Text = "Cancel";
            this.m_CancelButton.UseVisualStyleBackColor = true;
            this.m_CancelButton.Click += new System.EventHandler(this.m_CancelButton_Click);
            // 
            // ProgressDialog
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.m_CancelButton;
            this.ClientSize = new System.Drawing.Size(324, 95);
            this.Controls.Add(this.m_CancelButton);
            this.Controls.Add(this.m_progessbar);
            this.Controls.Add(this.m_Label);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "ProgressDialog";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "ProgressDialog";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.ProgressDialog_FormClosing);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Label m_Label;
        private System.Windows.Forms.ProgressBar m_progessbar;
        private System.Windows.Forms.Button m_CancelButton;
    }
}