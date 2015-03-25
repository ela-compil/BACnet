namespace Yabe
{
    partial class TrendLogDisplay
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(TrendLogDisplay));
            this.m_progressBar = new System.Windows.Forms.ProgressBar();
            this.m_zedGraphCtl = new ZedGraph.ZedGraphControl();
            this.m_progresslabel = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // m_progressBar
            // 
            this.m_progressBar.Location = new System.Drawing.Point(185, 127);
            this.m_progressBar.Name = "m_progressBar";
            this.m_progressBar.Size = new System.Drawing.Size(222, 23);
            this.m_progressBar.TabIndex = 0;
            // 
            // m_zedGraphCtl
            // 
            this.m_zedGraphCtl.Dock = System.Windows.Forms.DockStyle.Fill;
            this.m_zedGraphCtl.IsShowPointValues = true;
            this.m_zedGraphCtl.Location = new System.Drawing.Point(0, 0);
            this.m_zedGraphCtl.Name = "m_zedGraphCtl";
            this.m_zedGraphCtl.ScrollGrace = 0D;
            this.m_zedGraphCtl.ScrollMaxX = 0D;
            this.m_zedGraphCtl.ScrollMaxY = 0D;
            this.m_zedGraphCtl.ScrollMaxY2 = 0D;
            this.m_zedGraphCtl.ScrollMinX = 0D;
            this.m_zedGraphCtl.ScrollMinY = 0D;
            this.m_zedGraphCtl.ScrollMinY2 = 0D;
            this.m_zedGraphCtl.Size = new System.Drawing.Size(571, 317);
            this.m_zedGraphCtl.TabIndex = 1;
            // 
            // m_progresslabel
            // 
            this.m_progresslabel.BackColor = System.Drawing.SystemColors.Window;
            this.m_progresslabel.Location = new System.Drawing.Point(159, 107);
            this.m_progresslabel.Name = "m_progresslabel";
            this.m_progresslabel.Size = new System.Drawing.Size(274, 17);
            this.m_progresslabel.TabIndex = 2;
            this.m_progresslabel.Text = "Downloads in Progress (0%)";
            this.m_progresslabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // TrendLogDisplay
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(571, 317);
            this.Controls.Add(this.m_progresslabel);
            this.Controls.Add(this.m_progressBar);
            this.Controls.Add(this.m_zedGraphCtl);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "TrendLogDisplay";
            this.Text = "Yabe TrendLog Display";
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.ProgressBar m_progressBar;
        private ZedGraph.ZedGraphControl m_zedGraphCtl;
        private System.Windows.Forms.Label m_progresslabel;
    }
}