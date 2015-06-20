namespace Yabe
{
    partial class NotificationEditor
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(NotificationEditor));
            this.RecipientsTab = new System.Windows.Forms.TabControl();
            this.PopupMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.deleteToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.addNewToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.btReadWrite = new System.Windows.Forms.Button();
            this.labelRecipient = new System.Windows.Forms.Label();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.label4 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.P_Off = new System.Windows.Forms.TextBox();
            this.P_Fault = new System.Windows.Forms.TextBox();
            this.P_Normal = new System.Windows.Forms.TextBox();
            this.labelEmpty = new System.Windows.Forms.Label();
            this.PopupMenu.SuspendLayout();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // RecipientsTab
            // 
            this.RecipientsTab.ContextMenuStrip = this.PopupMenu;
            this.RecipientsTab.Location = new System.Drawing.Point(36, 24);
            this.RecipientsTab.Name = "RecipientsTab";
            this.RecipientsTab.SelectedIndex = 0;
            this.RecipientsTab.Size = new System.Drawing.Size(424, 430);
            this.RecipientsTab.TabIndex = 0;
            // 
            // PopupMenu
            // 
            this.PopupMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.deleteToolStripMenuItem,
            this.addNewToolStripMenuItem});
            this.PopupMenu.Name = "Menu";
            this.PopupMenu.Size = new System.Drawing.Size(124, 48);
            // 
            // deleteToolStripMenuItem
            // 
            this.deleteToolStripMenuItem.Name = "deleteToolStripMenuItem";
            this.deleteToolStripMenuItem.Size = new System.Drawing.Size(123, 22);
            this.deleteToolStripMenuItem.Text = "Delete";
            this.deleteToolStripMenuItem.Click += new System.EventHandler(this.deleteToolStripMenuItem_Click);
            // 
            // addNewToolStripMenuItem
            // 
            this.addNewToolStripMenuItem.Name = "addNewToolStripMenuItem";
            this.addNewToolStripMenuItem.Size = new System.Drawing.Size(123, 22);
            this.addNewToolStripMenuItem.Text = "Add New";
            this.addNewToolStripMenuItem.Click += new System.EventHandler(this.addNewToolStripMenuItem_Click);
            // 
            // btReadWrite
            // 
            this.btReadWrite.Location = new System.Drawing.Point(205, 527);
            this.btReadWrite.Name = "btReadWrite";
            this.btReadWrite.Size = new System.Drawing.Size(114, 37);
            this.btReadWrite.TabIndex = 1;
            this.btReadWrite.Text = "Write && Read back";
            this.btReadWrite.UseVisualStyleBackColor = true;
            this.btReadWrite.Click += new System.EventHandler(this.btReadWrite_Click);
            // 
            // labelRecipient
            // 
            this.labelRecipient.Location = new System.Drawing.Point(36, 6);
            this.labelRecipient.Name = "labelRecipient";
            this.labelRecipient.Size = new System.Drawing.Size(424, 19);
            this.labelRecipient.TabIndex = 2;
            this.labelRecipient.Text = "Recipient List";
            this.labelRecipient.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.label4);
            this.groupBox1.Controls.Add(this.label3);
            this.groupBox1.Controls.Add(this.label2);
            this.groupBox1.Controls.Add(this.P_Off);
            this.groupBox1.Controls.Add(this.P_Fault);
            this.groupBox1.Controls.Add(this.P_Normal);
            this.groupBox1.Location = new System.Drawing.Point(36, 460);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(424, 51);
            this.groupBox1.TabIndex = 3;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Priority";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(304, 22);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(59, 13);
            this.label4.TabIndex = 7;
            this.label4.Text = "To_Normal";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(172, 22);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(49, 13);
            this.label3.TabIndex = 6;
            this.label3.Text = "To_Fault";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(16, 22);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(73, 13);
            this.label2.TabIndex = 5;
            this.label2.Text = "To_OffNormal";
            // 
            // P_Off
            // 
            this.P_Off.Location = new System.Drawing.Point(92, 19);
            this.P_Off.Name = "P_Off";
            this.P_Off.Size = new System.Drawing.Size(33, 20);
            this.P_Off.TabIndex = 4;
            // 
            // P_Fault
            // 
            this.P_Fault.Location = new System.Drawing.Point(227, 19);
            this.P_Fault.Name = "P_Fault";
            this.P_Fault.Size = new System.Drawing.Size(33, 20);
            this.P_Fault.TabIndex = 3;
            // 
            // P_Normal
            // 
            this.P_Normal.Location = new System.Drawing.Point(369, 19);
            this.P_Normal.Name = "P_Normal";
            this.P_Normal.Size = new System.Drawing.Size(33, 20);
            this.P_Normal.TabIndex = 2;
            // 
            // labelEmpty
            // 
            this.labelEmpty.BackColor = System.Drawing.SystemColors.ButtonHighlight;
            this.labelEmpty.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F);
            this.labelEmpty.Location = new System.Drawing.Point(205, 178);
            this.labelEmpty.Name = "labelEmpty";
            this.labelEmpty.Size = new System.Drawing.Size(153, 66);
            this.labelEmpty.TabIndex = 4;
            this.labelEmpty.Text = "Empty list";
            // 
            // NotificationEditor
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(491, 576);
            this.ContextMenuStrip = this.PopupMenu;
            this.Controls.Add(this.labelEmpty);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.labelRecipient);
            this.Controls.Add(this.btReadWrite);
            this.Controls.Add(this.RecipientsTab);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.Name = "NotificationEditor";
            this.Text = "Notification Editor";
            this.PopupMenu.ResumeLayout(false);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TabControl RecipientsTab;
        private System.Windows.Forms.ContextMenuStrip PopupMenu;
        private System.Windows.Forms.ToolStripMenuItem deleteToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem addNewToolStripMenuItem;
        private System.Windows.Forms.Button btReadWrite;
        private System.Windows.Forms.Label labelRecipient;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox P_Off;
        private System.Windows.Forms.TextBox P_Fault;
        private System.Windows.Forms.TextBox P_Normal;
        private System.Windows.Forms.Label labelEmpty;
    }
}