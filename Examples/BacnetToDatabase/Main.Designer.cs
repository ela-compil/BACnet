namespace BacnetToDatabase
{
    partial class Main
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
            this.components = new System.ComponentModel.Container();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.m_SearchButton = new System.Windows.Forms.Button();
            this.m_TransferButton = new System.Windows.Forms.Button();
            this.m_list = new System.Windows.Forms.ListView();
            this.columnHeader1 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader2 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.m_delayedStart = new System.Windows.Forms.Timer(this.components);
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // groupBox1
            // 
            this.groupBox1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox1.Controls.Add(this.m_list);
            this.groupBox1.Controls.Add(this.m_TransferButton);
            this.groupBox1.Controls.Add(this.m_SearchButton);
            this.groupBox1.Location = new System.Drawing.Point(12, 12);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(381, 259);
            this.groupBox1.TabIndex = 0;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Devices";
            // 
            // m_SearchButton
            // 
            this.m_SearchButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.m_SearchButton.Location = new System.Drawing.Point(300, 229);
            this.m_SearchButton.Name = "m_SearchButton";
            this.m_SearchButton.Size = new System.Drawing.Size(75, 23);
            this.m_SearchButton.TabIndex = 0;
            this.m_SearchButton.Text = "Search";
            this.m_SearchButton.UseVisualStyleBackColor = true;
            this.m_SearchButton.Click += new System.EventHandler(this.m_SearchButton_Click);
            // 
            // m_TransferButton
            // 
            this.m_TransferButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.m_TransferButton.Location = new System.Drawing.Point(170, 229);
            this.m_TransferButton.Name = "m_TransferButton";
            this.m_TransferButton.Size = new System.Drawing.Size(124, 23);
            this.m_TransferButton.TabIndex = 1;
            this.m_TransferButton.Text = "Transfer To SQL CE Local DB";
            this.m_TransferButton.UseVisualStyleBackColor = true;
            this.m_TransferButton.Click += new System.EventHandler(this.m_TransferButton_Click);
            // 
            // m_list
            // 
            this.m_list.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.m_list.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader1,
            this.columnHeader2});
            this.m_list.FullRowSelect = true;
            this.m_list.GridLines = true;
            this.m_list.Location = new System.Drawing.Point(6, 19);
            this.m_list.Name = "m_list";
            this.m_list.Size = new System.Drawing.Size(369, 205);
            this.m_list.TabIndex = 2;
            this.m_list.UseCompatibleStateImageBehavior = false;
            this.m_list.View = System.Windows.Forms.View.Details;
            // 
            // columnHeader1
            // 
            this.columnHeader1.Text = "IP";
            this.columnHeader1.Width = 120;
            // 
            // columnHeader2
            // 
            this.columnHeader2.Text = "Name";
            this.columnHeader2.Width = 150;
            // 
            // m_delayedStart
            // 
            this.m_delayedStart.Enabled = true;
            this.m_delayedStart.Interval = 500;
            this.m_delayedStart.Tick += new System.EventHandler(this.m_delayedStart_Tick);
            // 
            // Main
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(405, 283);
            this.Controls.Add(this.groupBox1);
            this.Name = "Main";
            this.Text = "Bacnet To Database";
            this.groupBox1.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Button m_TransferButton;
        private System.Windows.Forms.Button m_SearchButton;
        private System.Windows.Forms.ListView m_list;
        private System.Windows.Forms.ColumnHeader columnHeader1;
        private System.Windows.Forms.ColumnHeader columnHeader2;
        private System.Windows.Forms.Timer m_delayedStart;
    }
}

