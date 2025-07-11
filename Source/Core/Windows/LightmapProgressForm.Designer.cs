namespace CodeImp.DoomBuilder.Windows
{
	partial class LightmapProgressForm
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
			this.progressbar = new System.Windows.Forms.ProgressBar();
			this.labelprogress = new System.Windows.Forms.Label();
			this.textboxoutput = new System.Windows.Forms.RichTextBox();
			this.buttoncancel = new System.Windows.Forms.Button();
			this.SuspendLayout();
			// 
			// progressbar
			// 
			this.progressbar.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.progressbar.Location = new System.Drawing.Point(8, 24);
			this.progressbar.Name = "progressbar";
			this.progressbar.Size = new System.Drawing.Size(480, 24);
			this.progressbar.TabIndex = 0;
			// 
			// labelprogress
			// 
			this.labelprogress.AutoSize = true;
			this.labelprogress.Location = new System.Drawing.Point(8, 8);
			this.labelprogress.Name = "labelprogress";
			this.labelprogress.Size = new System.Drawing.Size(51, 13);
			this.labelprogress.TabIndex = 1;
			this.labelprogress.Text = "Progress:";
			// 
			// textboxoutput
			// 
			this.textboxoutput.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.textboxoutput.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
			this.textboxoutput.CausesValidation = false;
			this.textboxoutput.Font = new System.Drawing.Font("Lucida Console", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.textboxoutput.ForeColor = System.Drawing.Color.Gainsboro;
			this.textboxoutput.Location = new System.Drawing.Point(8, 56);
			this.textboxoutput.Name = "textboxoutput";
			this.textboxoutput.ReadOnly = true;
			this.textboxoutput.ScrollBars = System.Windows.Forms.RichTextBoxScrollBars.Vertical;
			this.textboxoutput.Size = new System.Drawing.Size(480, 176);
			this.textboxoutput.TabIndex = 2;
			this.textboxoutput.Text = "";
			this.textboxoutput.WordWrap = false;
			// 
			// buttoncancel
			// 
			this.buttoncancel.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
			this.buttoncancel.Location = new System.Drawing.Point(200, 240);
			this.buttoncancel.Name = "buttoncancel";
			this.buttoncancel.Size = new System.Drawing.Size(115, 32);
			this.buttoncancel.TabIndex = 3;
			this.buttoncancel.Text = "Cancel";
			this.buttoncancel.UseVisualStyleBackColor = true;
			this.buttoncancel.Click += new System.EventHandler(this.buttoncancel_Click);
			// 
			// LightmapProgressForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(497, 281);
			this.Controls.Add(this.buttoncancel);
			this.Controls.Add(this.textboxoutput);
			this.Controls.Add(this.labelprogress);
			this.Controls.Add(this.progressbar);
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "LightmapProgressForm";
			this.ShowIcon = false;
			this.ShowInTaskbar = false;
			this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "Build Lightmaps";
			this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.LightmapProgressForm_FormClosing);
			this.Load += new System.EventHandler(this.LightmapProgressForm_Load);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.ProgressBar progressbar;
		private System.Windows.Forms.Label labelprogress;
		private System.Windows.Forms.RichTextBox textboxoutput;
		private System.Windows.Forms.Button buttoncancel;
	}
}