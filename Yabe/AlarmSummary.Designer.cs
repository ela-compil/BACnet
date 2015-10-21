namespace Yabe
{
    partial class AlarmSummary
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(AlarmSummary));
            this.TAlarmList = new System.Windows.Forms.TreeView();
            this.AckText = new System.Windows.Forms.TextBox();
            this.LblInfo = new System.Windows.Forms.Label();
            this.AckBt = new System.Windows.Forms.Button();
            this.PartialLabel = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // TAlarmList
            // 
            this.TAlarmList.Location = new System.Drawing.Point(24, 25);
            this.TAlarmList.Name = "TAlarmList";
            this.TAlarmList.ShowNodeToolTips = true;
            this.TAlarmList.Size = new System.Drawing.Size(281, 194);
            this.TAlarmList.TabIndex = 1;
            this.TAlarmList.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.TAlarmList_AfterSelect);
            // 
            // AckText
            // 
            this.AckText.Enabled = false;
            this.AckText.Location = new System.Drawing.Point(199, 261);
            this.AckText.Name = "AckText";
            this.AckText.Size = new System.Drawing.Size(106, 20);
            this.AckText.TabIndex = 2;
            this.AckText.Text = "Ack by Yabe";
            // 
            // LblInfo
            // 
            this.LblInfo.BackColor = System.Drawing.SystemColors.ButtonHighlight;
            this.LblInfo.Location = new System.Drawing.Point(36, 109);
            this.LblInfo.Name = "LblInfo";
            this.LblInfo.Size = new System.Drawing.Size(258, 15);
            this.LblInfo.TabIndex = 3;
            this.LblInfo.Text = "Service not available on this device";
            this.LblInfo.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // AckBt
            // 
            this.AckBt.Enabled = false;
            this.AckBt.Location = new System.Drawing.Point(24, 261);
            this.AckBt.Name = "AckBt";
            this.AckBt.Size = new System.Drawing.Size(145, 23);
            this.AckBt.TabIndex = 4;
            this.AckBt.Text = "Ack selected alarm";
            this.AckBt.UseVisualStyleBackColor = true;
            this.AckBt.Click += new System.EventHandler(this.AckBt_Click);
            // 
            // PartialLabel
            // 
            this.PartialLabel.AutoSize = true;
            this.PartialLabel.Location = new System.Drawing.Point(24, 226);
            this.PartialLabel.Name = "PartialLabel";
            this.PartialLabel.Size = new System.Drawing.Size(185, 13);
            this.PartialLabel.TabIndex = 5;
            this.PartialLabel.Text = "Partial list, all is not sent by the device";
            this.PartialLabel.Visible = false;
            // 
            // AlarmSummary
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(331, 296);
            this.Controls.Add(this.PartialLabel);
            this.Controls.Add(this.AckBt);
            this.Controls.Add(this.LblInfo);
            this.Controls.Add(this.AckText);
            this.Controls.Add(this.TAlarmList);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "AlarmSummary";
            this.Text = "Active Alarms on Device";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TreeView TAlarmList;
        private System.Windows.Forms.TextBox AckText;
        private System.Windows.Forms.Label LblInfo;
        private System.Windows.Forms.Button AckBt;
        private System.Windows.Forms.Label PartialLabel;
    }
}