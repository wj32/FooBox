﻿namespace FooBoxClient
{
    partial class FormSysTray
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormSysTray));
            this.notifyFooBox = new System.Windows.Forms.NotifyIcon(this.components);
            this.contextMenuNotify = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.pauseToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.settingsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.changeUserToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.contextMenuNotify.SuspendLayout();
            this.SuspendLayout();
            // 
            // notifyFooBox
            // 
            this.notifyFooBox.ContextMenuStrip = this.contextMenuNotify;
            this.notifyFooBox.Icon = ((System.Drawing.Icon)(resources.GetObject("notifyFooBox.Icon")));
            this.notifyFooBox.Text = "FooBox";
            this.notifyFooBox.MouseClick += new System.Windows.Forms.MouseEventHandler(this.notifyFooBox_MouseClick);
            this.notifyFooBox.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.notifyFooBox_MouseDoubleClick);
            // 
            // contextMenuNotify
            // 
            this.contextMenuNotify.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.pauseToolStripMenuItem,
            this.settingsToolStripMenuItem,
            this.changeUserToolStripMenuItem,
            this.exitToolStripMenuItem});
            this.contextMenuNotify.Name = "contextMenuNotify";
            this.contextMenuNotify.Size = new System.Drawing.Size(150, 92);
            // 
            // pauseToolStripMenuItem
            // 
            this.pauseToolStripMenuItem.Name = "pauseToolStripMenuItem";
            this.pauseToolStripMenuItem.Size = new System.Drawing.Size(149, 22);
            this.pauseToolStripMenuItem.Text = "Pause syncing";
            this.pauseToolStripMenuItem.Click += new System.EventHandler(this.pauseToolStripMenuItem_Click);
            // 
            // settingsToolStripMenuItem
            // 
            this.settingsToolStripMenuItem.Name = "settingsToolStripMenuItem";
            this.settingsToolStripMenuItem.Size = new System.Drawing.Size(149, 22);
            this.settingsToolStripMenuItem.Text = "Settings";
            this.settingsToolStripMenuItem.Click += new System.EventHandler(this.settingsToolStripMenuItem_Click);
            // 
            // changeUserToolStripMenuItem
            // 
            this.changeUserToolStripMenuItem.Name = "changeUserToolStripMenuItem";
            this.changeUserToolStripMenuItem.Size = new System.Drawing.Size(149, 22);
            this.changeUserToolStripMenuItem.Text = "Change user";
            this.changeUserToolStripMenuItem.Click += new System.EventHandler(this.changeUserToolStripMenuItem_Click);
            // 
            // exitToolStripMenuItem
            // 
            this.exitToolStripMenuItem.Name = "exitToolStripMenuItem";
            this.exitToolStripMenuItem.Size = new System.Drawing.Size(149, 22);
            this.exitToolStripMenuItem.Text = "Exit";
            this.exitToolStripMenuItem.Click += new System.EventHandler(this.exitToolStripMenuItem_Click);
            // 
            // FormSysTray
            // 
            this.AllowDrop = true;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackgroundImage = global::FooBoxClient.Properties.Resources.back;
            this.ClientSize = new System.Drawing.Size(304, 281);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "FormSysTray";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.Text = "FooBox";
            this.TopMost = true;
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.FormSysTray_FormClosing);
            this.Load += new System.EventHandler(this.FormSysTray_Load);
            this.DragDrop += new System.Windows.Forms.DragEventHandler(this.FormSysTray_DragDrop);
            this.DragEnter += new System.Windows.Forms.DragEventHandler(this.FormSysTray_DragEnter);
            this.contextMenuNotify.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.NotifyIcon notifyFooBox;
        private System.Windows.Forms.ContextMenuStrip contextMenuNotify;
        private System.Windows.Forms.ToolStripMenuItem exitToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem changeUserToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem settingsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem pauseToolStripMenuItem;

    }
}