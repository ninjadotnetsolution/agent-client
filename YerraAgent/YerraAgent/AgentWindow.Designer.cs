
namespace YerraAgent
{
    partial class AgentWindow
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(AgentWindow));
            this.txtAgentID = new System.Windows.Forms.TextBox();
            this.labelAgentID = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // txtAgentID
            // 
            this.txtAgentID.BackColor = System.Drawing.SystemColors.GrayText;
            this.txtAgentID.Font = new System.Drawing.Font("Microsoft Sans Serif", 15.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtAgentID.ForeColor = System.Drawing.SystemColors.Window;
            this.txtAgentID.Location = new System.Drawing.Point(167, 69);
            this.txtAgentID.Name = "txtAgentID";
            this.txtAgentID.ReadOnly = true;
            this.txtAgentID.Size = new System.Drawing.Size(328, 31);
            this.txtAgentID.TabIndex = 0;
            // 
            // labelAgentID
            // 
            this.labelAgentID.AutoSize = true;
            this.labelAgentID.Font = new System.Drawing.Font("Microsoft Sans Serif", 15.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelAgentID.ForeColor = System.Drawing.SystemColors.Window;
            this.labelAgentID.Location = new System.Drawing.Point(45, 75);
            this.labelAgentID.Name = "labelAgentID";
            this.labelAgentID.Size = new System.Drawing.Size(116, 25);
            this.labelAgentID.TabIndex = 1;
            this.labelAgentID.Text = "Agent ID :";
            // 
            // AgentWindow
            // 
            this.AccessibleRole = System.Windows.Forms.AccessibleRole.Animation;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.DimGray;
            this.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Center;
            this.ClientSize = new System.Drawing.Size(592, 155);
            this.Controls.Add(this.labelAgentID);
            this.Controls.Add(this.txtAgentID);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.Name = "AgentWindow";
            this.ShowInTaskbar = false;
            this.Text = "AgentWindow";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox txtAgentID;
        private System.Windows.Forms.Label labelAgentID;

        public void setAgentID(string agentID)
        {
            txtAgentID.Text = agentID;
        }
    }
}