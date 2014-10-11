namespace FooBoxClient
{
    partial class FormStart
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
            this.labelName = new System.Windows.Forms.Label();
            this.labelWelcome = new System.Windows.Forms.Label();
            this.labelServerLocation = new System.Windows.Forms.Label();
            this.textBoxServerLoc = new System.Windows.Forms.TextBox();
            this.textBoxUsername = new System.Windows.Forms.TextBox();
            this.labelUsername = new System.Windows.Forms.Label();
            this.textBoxPassword = new System.Windows.Forms.TextBox();
            this.labelPassword = new System.Windows.Forms.Label();
            this.textBoxDirLoc = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.buttonBrowse = new System.Windows.Forms.Button();
            this.labelError = new System.Windows.Forms.Label();
            this.buttonCancel = new System.Windows.Forms.Button();
            this.buttonNext = new System.Windows.Forms.Button();
            this.textBoxServerPort = new System.Windows.Forms.TextBox();
            this.labelServerPort = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // labelName
            // 
            this.labelName.Font = new System.Drawing.Font("Microsoft Sans Serif", 36F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelName.Location = new System.Drawing.Point(12, 19);
            this.labelName.Name = "labelName";
            this.labelName.Size = new System.Drawing.Size(432, 51);
            this.labelName.TabIndex = 0;
            this.labelName.Text = "FooBox";
            this.labelName.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // labelWelcome
            // 
            this.labelWelcome.AutoSize = true;
            this.labelWelcome.Font = new System.Drawing.Font("Microsoft Sans Serif", 15.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelWelcome.Location = new System.Drawing.Point(17, 94);
            this.labelWelcome.Name = "labelWelcome";
            this.labelWelcome.Size = new System.Drawing.Size(321, 25);
            this.labelWelcome.TabIndex = 1;
            this.labelWelcome.Text = "Welcome to FooBox client setup";
            // 
            // labelServerLocation
            // 
            this.labelServerLocation.AutoSize = true;
            this.labelServerLocation.Location = new System.Drawing.Point(21, 153);
            this.labelServerLocation.Name = "labelServerLocation";
            this.labelServerLocation.Size = new System.Drawing.Size(167, 13);
            this.labelServerLocation.TabIndex = 2;
            this.labelServerLocation.Text = "Enter the name or ip of the server:";
            // 
            // textBoxServerLoc
            // 
            this.textBoxServerLoc.Location = new System.Drawing.Point(194, 149);
            this.textBoxServerLoc.Name = "textBoxServerLoc";
            this.textBoxServerLoc.Size = new System.Drawing.Size(245, 20);
            this.textBoxServerLoc.TabIndex = 3;
            this.textBoxServerLoc.Text = "localhost:7348";
            // 
            // textBoxUsername
            // 
            this.textBoxUsername.Location = new System.Drawing.Point(194, 201);
            this.textBoxUsername.Name = "textBoxUsername";
            this.textBoxUsername.Size = new System.Drawing.Size(245, 20);
            this.textBoxUsername.TabIndex = 5;
            // 
            // labelUsername
            // 
            this.labelUsername.AutoSize = true;
            this.labelUsername.Location = new System.Drawing.Point(130, 204);
            this.labelUsername.Name = "labelUsername";
            this.labelUsername.Size = new System.Drawing.Size(58, 13);
            this.labelUsername.TabIndex = 4;
            this.labelUsername.Text = "Username:";
            // 
            // textBoxPassword
            // 
            this.textBoxPassword.Location = new System.Drawing.Point(194, 227);
            this.textBoxPassword.Name = "textBoxPassword";
            this.textBoxPassword.Size = new System.Drawing.Size(245, 20);
            this.textBoxPassword.TabIndex = 7;
            // 
            // labelPassword
            // 
            this.labelPassword.AutoSize = true;
            this.labelPassword.Location = new System.Drawing.Point(130, 230);
            this.labelPassword.Name = "labelPassword";
            this.labelPassword.Size = new System.Drawing.Size(56, 13);
            this.labelPassword.TabIndex = 6;
            this.labelPassword.Text = "Password:";
            // 
            // textBoxDirLoc
            // 
            this.textBoxDirLoc.Location = new System.Drawing.Point(194, 253);
            this.textBoxDirLoc.Name = "textBoxDirLoc";
            this.textBoxDirLoc.Size = new System.Drawing.Size(164, 20);
            this.textBoxDirLoc.TabIndex = 9;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(47, 256);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(141, 13);
            this.label3.TabIndex = 8;
            this.label3.Text = "Select a directory to sync to:";
            // 
            // buttonBrowse
            // 
            this.buttonBrowse.Location = new System.Drawing.Point(364, 251);
            this.buttonBrowse.Name = "buttonBrowse";
            this.buttonBrowse.Size = new System.Drawing.Size(75, 23);
            this.buttonBrowse.TabIndex = 10;
            this.buttonBrowse.Text = "Browse";
            this.buttonBrowse.UseVisualStyleBackColor = true;
            this.buttonBrowse.Click += new System.EventHandler(this.buttonBrowse_Click);
            // 
            // labelError
            // 
            this.labelError.AutoSize = true;
            this.labelError.ForeColor = System.Drawing.Color.Red;
            this.labelError.Location = new System.Drawing.Point(19, 153);
            this.labelError.Name = "labelError";
            this.labelError.Size = new System.Drawing.Size(0, 13);
            this.labelError.TabIndex = 11;
            // 
            // buttonCancel
            // 
            this.buttonCancel.Location = new System.Drawing.Point(364, 282);
            this.buttonCancel.Name = "buttonCancel";
            this.buttonCancel.Size = new System.Drawing.Size(75, 23);
            this.buttonCancel.TabIndex = 12;
            this.buttonCancel.Text = "Cancel";
            this.buttonCancel.UseVisualStyleBackColor = true;
            this.buttonCancel.Click += new System.EventHandler(this.buttonCancel_Click);
            // 
            // buttonNext
            // 
            this.buttonNext.Location = new System.Drawing.Point(279, 282);
            this.buttonNext.Name = "buttonNext";
            this.buttonNext.Size = new System.Drawing.Size(79, 23);
            this.buttonNext.TabIndex = 13;
            this.buttonNext.Text = "Next >";
            this.buttonNext.UseVisualStyleBackColor = true;
            this.buttonNext.Click += new System.EventHandler(this.buttonNext_Click);
            // 
            // textBoxServerPort
            // 
            this.textBoxServerPort.Location = new System.Drawing.Point(194, 175);
            this.textBoxServerPort.Name = "textBoxServerPort";
            this.textBoxServerPort.Size = new System.Drawing.Size(245, 20);
            this.textBoxServerPort.TabIndex = 15;
            this.textBoxServerPort.Text = "7348";
            // 
            // labelServerPort
            // 
            this.labelServerPort.AutoSize = true;
            this.labelServerPort.Location = new System.Drawing.Point(157, 178);
            this.labelServerPort.Name = "labelServerPort";
            this.labelServerPort.Size = new System.Drawing.Size(29, 13);
            this.labelServerPort.TabIndex = 14;
            this.labelServerPort.Text = "Port:";
            // 
            // FormStart
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(451, 317);
            this.Controls.Add(this.textBoxServerPort);
            this.Controls.Add(this.labelServerPort);
            this.Controls.Add(this.buttonNext);
            this.Controls.Add(this.buttonCancel);
            this.Controls.Add(this.labelError);
            this.Controls.Add(this.buttonBrowse);
            this.Controls.Add(this.textBoxDirLoc);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.textBoxPassword);
            this.Controls.Add(this.labelPassword);
            this.Controls.Add(this.textBoxUsername);
            this.Controls.Add(this.labelUsername);
            this.Controls.Add(this.textBoxServerLoc);
            this.Controls.Add(this.labelServerLocation);
            this.Controls.Add(this.labelWelcome);
            this.Controls.Add(this.labelName);
            this.Name = "FormStart";
            this.Text = "FooBox Setup";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label labelName;
        private System.Windows.Forms.Label labelWelcome;
        private System.Windows.Forms.Label labelServerLocation;
        private System.Windows.Forms.TextBox textBoxServerLoc;
        private System.Windows.Forms.TextBox textBoxUsername;
        private System.Windows.Forms.Label labelUsername;
        private System.Windows.Forms.TextBox textBoxPassword;
        private System.Windows.Forms.Label labelPassword;
        private System.Windows.Forms.TextBox textBoxDirLoc;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Button buttonBrowse;
        private System.Windows.Forms.Label labelError;
        private System.Windows.Forms.Button buttonCancel;
        private System.Windows.Forms.Button buttonNext;
        private System.Windows.Forms.TextBox textBoxServerPort;
        private System.Windows.Forms.Label labelServerPort;
    }
}

