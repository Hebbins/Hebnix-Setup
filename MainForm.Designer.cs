namespace Hebnix_Updater
{
    partial class MainForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            this.pictureBox2 = new System.Windows.Forms.PictureBox();
            this.discordJoin = new System.Windows.Forms.PictureBox();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.HebnixUpdateStatus = new System.Windows.Forms.Label();
            this.LiteStatus = new System.Windows.Forms.Label();
            this.HebnixStatus = new System.Windows.Forms.Label();
            this.LiteUpdateStatus = new System.Windows.Forms.Label();
            this.hebnixinstupdate = new System.Windows.Forms.Button();
            this.liteinstupdate = new System.Windows.Forms.Button();
            this.hebnixuninstall = new System.Windows.Forms.Button();
            this.liteuninstall = new System.Windows.Forms.Button();
            this.HebnixPB = new System.Windows.Forms.ProgressBar();
            this.LitePB = new System.Windows.Forms.ProgressBar();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox2)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.discordJoin)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.SuspendLayout();
            // 
            // pictureBox2
            // 
            this.pictureBox2.BackgroundImage = global::Hebnix_Updater.Properties.Resources.Hebnix_Lite;
            this.pictureBox2.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.pictureBox2.Cursor = System.Windows.Forms.Cursors.Default;
            this.pictureBox2.InitialImage = ((System.Drawing.Image)(resources.GetObject("pictureBox2.InitialImage")));
            this.pictureBox2.Location = new System.Drawing.Point(12, 271);
            this.pictureBox2.Name = "pictureBox2";
            this.pictureBox2.Size = new System.Drawing.Size(200, 200);
            this.pictureBox2.TabIndex = 7;
            this.pictureBox2.TabStop = false;
            // 
            // discordJoin
            // 
            this.discordJoin.BackgroundImage = global::Hebnix_Updater.Properties.Resources.discord_icon;
            this.discordJoin.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.discordJoin.Cursor = System.Windows.Forms.Cursors.Hand;
            this.discordJoin.Image = global::Hebnix_Updater.Properties.Resources.discord_icon;
            this.discordJoin.Location = new System.Drawing.Point(806, 9);
            this.discordJoin.Name = "discordJoin";
            this.discordJoin.Size = new System.Drawing.Size(53, 39);
            this.discordJoin.TabIndex = 6;
            this.discordJoin.TabStop = false;
            this.discordJoin.Click += new System.EventHandler(this.discordJoin_Click);
            // 
            // pictureBox1
            // 
            this.pictureBox1.BackgroundImage = global::Hebnix_Updater.Properties.Resources.Hebnix_2_0;
            this.pictureBox1.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.pictureBox1.Cursor = System.Windows.Forms.Cursors.Default;
            this.pictureBox1.InitialImage = ((System.Drawing.Image)(resources.GetObject("pictureBox1.InitialImage")));
            this.pictureBox1.Location = new System.Drawing.Point(12, 50);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(200, 200);
            this.pictureBox1.TabIndex = 0;
            this.pictureBox1.TabStop = false;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Verdana", 20.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(13, 9);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(198, 32);
            this.label1.TabIndex = 8;
            this.label1.Text = "Select Edition";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Verdana", 20.25F, System.Drawing.FontStyle.Underline, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.Location = new System.Drawing.Point(218, 50);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(107, 32);
            this.label2.TabIndex = 12;
            this.label2.Text = "Hebnix";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Font = new System.Drawing.Font("Verdana", 20.25F, System.Drawing.FontStyle.Underline, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label3.Location = new System.Drawing.Point(218, 271);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(166, 32);
            this.label3.TabIndex = 13;
            this.label3.Text = "Hebnix Lite";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Font = new System.Drawing.Font("Verdana", 12F);
            this.label4.Location = new System.Drawing.Point(221, 82);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(568, 36);
            this.label4.TabIndex = 14;
            this.label4.Text = "All Hebnix features, including Plugin SDK. Plugins, Workshop Maps, \r\nSpoofer, Ite" +
    "m Swapper and any future features pushed to Hebnix.";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Font = new System.Drawing.Font("Verdana", 12F);
            this.label5.Location = new System.Drawing.Point(221, 303);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(626, 36);
            this.label5.TabIndex = 15;
            this.label5.Text = "A lighter version of Hebnix with only access to the Plugin SDK and Plugins.\r\nComp" +
    "letely EAC Compliant.";
            // 
            // HebnixUpdateStatus
            // 
            this.HebnixUpdateStatus.AutoSize = true;
            this.HebnixUpdateStatus.BackColor = System.Drawing.Color.MidnightBlue;
            this.HebnixUpdateStatus.Font = new System.Drawing.Font("Verdana", 12F);
            this.HebnixUpdateStatus.Location = new System.Drawing.Point(224, 126);
            this.HebnixUpdateStatus.Name = "HebnixUpdateStatus";
            this.HebnixUpdateStatus.Size = new System.Drawing.Size(153, 18);
            this.HebnixUpdateStatus.TabIndex = 16;
            this.HebnixUpdateStatus.Text = "Updating Pending";
            // 
            // LiteStatus
            // 
            this.LiteStatus.AutoSize = true;
            this.LiteStatus.BackColor = System.Drawing.Color.Maroon;
            this.LiteStatus.Font = new System.Drawing.Font("Verdana", 12F);
            this.LiteStatus.Location = new System.Drawing.Point(223, 373);
            this.LiteStatus.Name = "LiteStatus";
            this.LiteStatus.Size = new System.Drawing.Size(116, 18);
            this.LiteStatus.TabIndex = 17;
            this.LiteStatus.Text = "Installed: No";
            // 
            // HebnixStatus
            // 
            this.HebnixStatus.AutoSize = true;
            this.HebnixStatus.BackColor = System.Drawing.Color.Green;
            this.HebnixStatus.Font = new System.Drawing.Font("Verdana", 12F);
            this.HebnixStatus.Location = new System.Drawing.Point(224, 152);
            this.HebnixStatus.Name = "HebnixStatus";
            this.HebnixStatus.Size = new System.Drawing.Size(123, 18);
            this.HebnixStatus.TabIndex = 18;
            this.HebnixStatus.Text = "Installed: Yes";
            // 
            // LiteUpdateStatus
            // 
            this.LiteUpdateStatus.AutoSize = true;
            this.LiteUpdateStatus.BackColor = System.Drawing.Color.MidnightBlue;
            this.LiteUpdateStatus.Font = new System.Drawing.Font("Verdana", 12F);
            this.LiteUpdateStatus.Location = new System.Drawing.Point(223, 347);
            this.LiteUpdateStatus.Name = "LiteUpdateStatus";
            this.LiteUpdateStatus.Size = new System.Drawing.Size(153, 18);
            this.LiteUpdateStatus.TabIndex = 19;
            this.LiteUpdateStatus.Text = "Updating Pending";
            // 
            // hebnixinstupdate
            // 
            this.hebnixinstupdate.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(47)))), ((int)(((byte)(54)))), ((int)(((byte)(64)))));
            this.hebnixinstupdate.Cursor = System.Windows.Forms.Cursors.Hand;
            this.hebnixinstupdate.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(53)))), ((int)(((byte)(59)))), ((int)(((byte)(72)))));
            this.hebnixinstupdate.FlatAppearance.BorderSize = 3;
            this.hebnixinstupdate.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(52)))), ((int)(((byte)(59)))), ((int)(((byte)(69)))));
            this.hebnixinstupdate.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.hebnixinstupdate.Font = new System.Drawing.Font("Verdana", 11F);
            this.hebnixinstupdate.Location = new System.Drawing.Point(224, 215);
            this.hebnixinstupdate.Name = "hebnixinstupdate";
            this.hebnixinstupdate.Size = new System.Drawing.Size(117, 35);
            this.hebnixinstupdate.TabIndex = 20;
            this.hebnixinstupdate.Text = "Install";
            this.hebnixinstupdate.UseVisualStyleBackColor = false;
            this.hebnixinstupdate.Click += new System.EventHandler(this.hebnixinstupdate_Click);
            // 
            // liteinstupdate
            // 
            this.liteinstupdate.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(47)))), ((int)(((byte)(54)))), ((int)(((byte)(64)))));
            this.liteinstupdate.Cursor = System.Windows.Forms.Cursors.Hand;
            this.liteinstupdate.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(53)))), ((int)(((byte)(59)))), ((int)(((byte)(72)))));
            this.liteinstupdate.FlatAppearance.BorderSize = 3;
            this.liteinstupdate.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(52)))), ((int)(((byte)(59)))), ((int)(((byte)(69)))));
            this.liteinstupdate.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.liteinstupdate.Font = new System.Drawing.Font("Verdana", 11F);
            this.liteinstupdate.Location = new System.Drawing.Point(224, 436);
            this.liteinstupdate.Name = "liteinstupdate";
            this.liteinstupdate.Size = new System.Drawing.Size(117, 35);
            this.liteinstupdate.TabIndex = 21;
            this.liteinstupdate.Text = "Install";
            this.liteinstupdate.UseVisualStyleBackColor = false;
            this.liteinstupdate.Click += new System.EventHandler(this.liteinstupdate_Click);
            // 
            // hebnixuninstall
            // 
            this.hebnixuninstall.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(47)))), ((int)(((byte)(54)))), ((int)(((byte)(64)))));
            this.hebnixuninstall.Cursor = System.Windows.Forms.Cursors.Hand;
            this.hebnixuninstall.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(53)))), ((int)(((byte)(59)))), ((int)(((byte)(72)))));
            this.hebnixuninstall.FlatAppearance.BorderSize = 3;
            this.hebnixuninstall.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(52)))), ((int)(((byte)(59)))), ((int)(((byte)(69)))));
            this.hebnixuninstall.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.hebnixuninstall.Font = new System.Drawing.Font("Verdana", 11F);
            this.hebnixuninstall.Location = new System.Drawing.Point(347, 215);
            this.hebnixuninstall.Name = "hebnixuninstall";
            this.hebnixuninstall.Size = new System.Drawing.Size(117, 35);
            this.hebnixuninstall.TabIndex = 22;
            this.hebnixuninstall.Text = "Uninstall";
            this.hebnixuninstall.UseVisualStyleBackColor = false;
            this.hebnixuninstall.Click += new System.EventHandler(this.hebnixuninstall_Click);
            // 
            // liteuninstall
            // 
            this.liteuninstall.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(47)))), ((int)(((byte)(54)))), ((int)(((byte)(64)))));
            this.liteuninstall.Cursor = System.Windows.Forms.Cursors.Hand;
            this.liteuninstall.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(53)))), ((int)(((byte)(59)))), ((int)(((byte)(72)))));
            this.liteuninstall.FlatAppearance.BorderSize = 3;
            this.liteuninstall.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(52)))), ((int)(((byte)(59)))), ((int)(((byte)(69)))));
            this.liteuninstall.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.liteuninstall.Font = new System.Drawing.Font("Verdana", 11F);
            this.liteuninstall.Location = new System.Drawing.Point(347, 436);
            this.liteuninstall.Name = "liteuninstall";
            this.liteuninstall.Size = new System.Drawing.Size(117, 35);
            this.liteuninstall.TabIndex = 23;
            this.liteuninstall.Text = "Uninstall";
            this.liteuninstall.UseVisualStyleBackColor = false;
            this.liteuninstall.Visible = false;
            this.liteuninstall.Click += new System.EventHandler(this.liteuninstall_Click);
            // 
            // HebnixPB
            // 
            this.HebnixPB.Location = new System.Drawing.Point(224, 186);
            this.HebnixPB.Name = "HebnixPB";
            this.HebnixPB.Size = new System.Drawing.Size(635, 23);
            this.HebnixPB.TabIndex = 24;
            this.HebnixPB.Visible = false;
            // 
            // LitePB
            // 
            this.LitePB.Location = new System.Drawing.Point(224, 407);
            this.LitePB.Name = "LitePB";
            this.LitePB.Size = new System.Drawing.Size(635, 23);
            this.LitePB.TabIndex = 25;
            this.LitePB.Visible = false;
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(11)))), ((int)(((byte)(12)))), ((int)(((byte)(16)))));
            this.ClientSize = new System.Drawing.Size(871, 485);
            this.Controls.Add(this.LitePB);
            this.Controls.Add(this.HebnixPB);
            this.Controls.Add(this.liteuninstall);
            this.Controls.Add(this.hebnixuninstall);
            this.Controls.Add(this.liteinstupdate);
            this.Controls.Add(this.hebnixinstupdate);
            this.Controls.Add(this.LiteUpdateStatus);
            this.Controls.Add(this.HebnixStatus);
            this.Controls.Add(this.LiteStatus);
            this.Controls.Add(this.HebnixUpdateStatus);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.pictureBox2);
            this.Controls.Add(this.discordJoin);
            this.Controls.Add(this.pictureBox1);
            this.ForeColor = System.Drawing.Color.White;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "MainForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Hebnix Manager";
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox2)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.discordJoin)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.PictureBox discordJoin;
        private System.Windows.Forms.PictureBox pictureBox2;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label HebnixUpdateStatus;
        private System.Windows.Forms.Label LiteStatus;
        private System.Windows.Forms.Label HebnixStatus;
        private System.Windows.Forms.Label LiteUpdateStatus;
        private System.Windows.Forms.Button hebnixinstupdate;
        private System.Windows.Forms.Button liteinstupdate;
        private System.Windows.Forms.Button hebnixuninstall;
        private System.Windows.Forms.Button liteuninstall;
        private System.Windows.Forms.ProgressBar HebnixPB;
        private System.Windows.Forms.ProgressBar LitePB;
    }
}

