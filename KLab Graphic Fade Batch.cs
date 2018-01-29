using System.Windows.Forms;
using System.IO;
using System;
using System.Collections.Generic;
using SoundForge;

namespace SoundForgeProForm
{
    public partial class GraphicFadeBatchForm : Form
    {
        private IScriptableApp forgeApp = null;
        List<string> extensions = new List<string>();
        bool isRunning = false;
        bool cancelRequest = false;
        DataGridViewCellStyle errorStyle = null;
        readonly string EffectName = "Graphic Fade";

        public GraphicFadeBatchForm(IScriptableApp forge)
        {
            InitializeComponent();
            initializeExtensions();
            forgeApp = forge;
            updatePresets();
            errorStyle = new DataGridViewCellStyle(fileList.DefaultCellStyle);
            errorStyle.BackColor = System.Drawing.Color.Red;
        }

        private void updatePresets()
        {
            fadeInPresetComboBox.Items.Clear();
            fadeOutPresetComboBox.Items.Clear();
            ISfGenericEffect effect = forgeApp.FindEffect(EffectName);
            if (effect == null)
            {
                MessageBox.Show(string.Format("Effect '{0}' not found.",
                                              EffectName),
                                "error",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Error);
                return;
            }

            foreach (ISfGenericPreset preset in effect.Presets)
            {
                fadeInPresetComboBox.Items.Add(preset.Name);
                fadeOutPresetComboBox.Items.Add(preset.Name);
            }

            fadeInPresetComboBox.SelectedIndex = 0;
            fadeOutPresetComboBox.SelectedIndex = 0;
        }

        private void initializeExtensions()
        {
            extensions.Clear();
            extensions.Add("avi");
            extensions.Add("wav");
            extensions.Add("w64");
            extensions.Add("mpg");
            extensions.Add("mpeg");
            extensions.Add("mxf");
            extensions.Add("ac3");
            extensions.Add("mp3");
            extensions.Add("mp4");
            //extensions.Add("mp4");
            extensions.Add("wmv");
            extensions.Add("wma");
            extensions.Add("vox");
            extensions.Add("dig");
            extensions.Add("ivc");
            //extensions.Add("MP4");
            extensions.Add("flac");
            extensions.Add("raw");
            extensions.Add("msv");
            extensions.Add("pca");
            extensions.Add("aa3");
            extensions.Add("aif");
            extensions.Add("au");
            extensions.Add("ogg");
            extensions.Add("dls");
            extensions.Add("gig");
            extensions.Add("sf2");
        }

        private string getDialogFilterString()
        {
            string extensionsString = "";
            foreach (string ext in extensions)
            {
                extensionsString += string.Format("*.{0};", ext);
            }
            return string.Format("media files({0})|{1}", extensionsString, extensionsString);
        }

        private void addFile(string file)
        {
            string ext = Path.GetExtension(file);
            if (extensions.Contains(ext.Substring(1)) == false)
            {
                return;
            }
            foreach (DataGridViewRow row in fileList.Rows)
            {
                string path = Path.Combine((string)row.Cells[1].Value,
                                           (string)row.Cells[0].Value);
                if (file.Equals(path))
                {
                    return;
                }
            }
            fileList.Rows.Add(new string[] { Path.GetFileName(file), Path.GetDirectoryName(file) });
        }

        private void addFiles(string[] files)
        {
            foreach (string file in files)
            {
                addFile(file);
            }
        }

        private void runFadeEffectProcess()
        {
            if (isRunning)
            {
                return;
            }
            try
            {
                if (fadeInPresetComboBox.Items.Count <= 0)
                {
                    MessageBox.Show("Fade in Preset is empty.",
                                    "Warning",
                                    MessageBoxButtons.OK,
                                    MessageBoxIcon.Warning);
                    fadeInPresetComboBox.Focus();
                    return;
                }
                if (fadeOutPresetComboBox.Items.Count <= 0)
                {
                    MessageBox.Show("Fade in Preset is empty.",
                                    "Warning",
                                    MessageBoxButtons.OK,
                                    MessageBoxIcon.Warning);
                    fadeOutPresetComboBox.Focus();
                    return;
                }
                isRunning = true;

                List<string> files = new List<string>();
                foreach (DataGridViewRow row in fileList.Rows)
                {
                    files.Add(Path.Combine((string)row.Cells[1].Value,
                                           (string)row.Cells[0].Value));
                }
                if (files.Count <= 0)
                {
                    return;
                }

                progressBar.Maximum = files.Count;
                int count = 0;
                foreach (string file in files)
                {
                    fileList.Rows[count].ErrorText = string.Empty;
                    if (cancelRequest)
                    {
                        break;
                    }

                    ISfFileHost filehost = null;
                    int undo = 0;
                    try
                    {
                        filehost = forgeApp.FindFile(file);
                        if (filehost == null)
                        {
                            filehost = forgeApp.OpenFile(file,
                                                         false,
                                                         false);
                        }
                        if (filehost.WaitForDoneOrCancel() == SfStatus.Fail)
                        {
                            fileList.Rows[count].ErrorText = string.Format("{0} can not be opened.",
                                                                           file);
                            continue;
                        }
                        undo = filehost.BeginUndo("Fade In and Out");
                        SfStatus status = SfStatus.NotAvailable;
                        Int64 fadetime = filehost.SecondsToPosition((double)fadeInTimeNumericUpDown.Value);
                        if (fadetime <= filehost.Length)
                        {
                            filehost.DoEffect(EffectName,
                                              fadeInPresetComboBox.Text,
                                              new SfAudioSelection(0,
                                                                   fadetime),
                                              EffectOptions.EffectOnly);
                            status = filehost.WaitForDoneOrCancel();
                            if (status == SfStatus.Success)
                            {
                                fadetime = filehost.SecondsToPosition((double)fadeOutTimeNumericUpDown.Value);
                                if (fadetime <= filehost.Length)
                                {
                                    filehost.DoEffect(EffectName,
                                                      fadeOutPresetComboBox.Text,
                                                      new SfAudioSelection(filehost.Length - fadetime,
                                                                           fadetime),
                                                      EffectOptions.EffectOnly);
                                    status = filehost.WaitForDoneOrCancel();
                                }
                                else
                                {
                                    fileList.Rows[count].ErrorText = string.Format("The fade out time({0}) is longer than total time({1}).",
                                                                                   fadetime,
                                                                                   filehost.Length);
                                    status = SfStatus.Fail;
                                }
                            }
                        }
                        else
                        {
                            fileList.Rows[count].ErrorText = string.Format("The fade in time({0}) is longer than total time({1}).",
                                                                           fadetime,
                                                                           filehost.Length);
                            status = SfStatus.Fail;
                        }
                        filehost.WaitForDoneOrCancel();
                        if (status != SfStatus.Success)
                        {
                            if (fileList.Rows[count].ErrorText == string.Empty)
                            {
                                fileList.Rows[count].ErrorText = string.Format("error code({0}).",
                                                                           status);
                            }
                        }
                        filehost.EndUndo(undo,
                                         (status != SfStatus.Success));
                        filehost.WaitForDoneOrCancel();
                        filehost.Save(SaveOptions.WaitForDoneOrCancel);
                        filehost.WaitForDoneOrCancel();
                        filehost.Close(CloseOptions.SaveChanges);
                        filehost.WaitForDoneOrCancel();
                    }
                    catch (Exception e)
                    {
                        forgeApp.SetStatusText(e.ToString());
                        filehost.EndUndo(undo,
                                         true);
                        filehost.Close(CloseOptions.DiscardChanges);
                        fileList.Rows[count].ErrorText = e.ToString();
                    }
                    finally
                    {
                        count++;
                        progressBar.Value++;
                        progressBar.Refresh();
                    }
                }
            }
            finally
            {
                isRunning = false;
                cancelRequest = false;
            }
        }

        private void addFilesButton_Click(object sender, System.EventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Multiselect = true;
            dialog.Filter = getDialogFilterString();
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                addFiles(dialog.FileNames);
            }
        }

        private void addFolderButton_Click(object sender, System.EventArgs e)
        {
            FolderBrowserDialog dialog = new FolderBrowserDialog();
            dialog.ShowNewFolderButton = false;
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                string path = dialog.SelectedPath;
                List<string> list = new List<string>();
                foreach (string ext in extensions)
                {
                    list.AddRange(Directory.GetFiles(path,
                                                     string.Format("*.{0}", ext),
                                                     SearchOption.TopDirectoryOnly));
                }
                addFiles(list.ToArray());
            }
        }

        private void removeButton_Click(object sender, System.EventArgs e)
        {
            foreach (DataGridViewRow row in fileList.SelectedRows)
            {
              fileList.Rows.Remove(row);
            }
        }

        private void closeButton_Click(object sender, System.EventArgs e)
        {
            if (isRunning)
            {
                return;
            }
            Close();
        }

        private void cancelButton_Click(object sender, System.EventArgs e)
        {
            if (isRunning == false)
            {
                return;
            }

            cancelRequest = true;
            if (forgeApp.CurrentFile != null)
            {
                forgeApp.CurrentFile.CancelProcessing();
            }
        }

        private void runButton_Click(object sender, System.EventArgs e)
        {
            try
            {
                closeButton.Enabled = false;
                runButton.Enabled = false;
                addFilesButton.Enabled = false;
                addFolderButton.Enabled = false;
                removeButton.Enabled = false;
                fadeInPresetComboBox.Enabled = false;
                fadeOutPresetComboBox.Enabled = false;
                fadeInTimeNumericUpDown.Enabled = false;
                fadeOutTimeNumericUpDown.Enabled = false;

                runFadeEffectProcess();
            }
            finally
            {
                fadeOutTimeNumericUpDown.Enabled = true;
                fadeInTimeNumericUpDown.Enabled = true;
                fadeOutPresetComboBox.Enabled = true;
                fadeInPresetComboBox.Enabled = true;
                removeButton.Enabled = true;
                addFolderButton.Enabled = true;
                addFilesButton.Enabled = true;
                runButton.Enabled = true;
                closeButton.Enabled = true;

                progressBar.Value = 0;
            }
        }

        private void fileList_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.Copy;
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }

        private void fileList_DragDrop(object sender, DragEventArgs e)
        {
            addFiles((string[])e.Data.GetData(DataFormats.FileDrop, false));
        }

        private void fileList_KeyDown(object sender, KeyEventArgs e)
        {
            if ((e.KeyCode == Keys.Delete) || (e.KeyCode == Keys.Back))
            {
                foreach (DataGridViewRow row in fileList.SelectedRows)
                {
                    fileList.Rows.RemoveAt(row.Index);
                }
                
            }
        }
    }
}
﻿namespace SoundForgeProForm
{
    partial class GraphicFadeBatchForm
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
            this.fadeInPresetLabel = new System.Windows.Forms.Label();
            this.fadeInPresetComboBox = new System.Windows.Forms.ComboBox();
            this.cancelButton = new System.Windows.Forms.Button();
            this.closeButton = new System.Windows.Forms.Button();
            this.runButton = new System.Windows.Forms.Button();
            this.fileList = new System.Windows.Forms.DataGridView();
            this.filenameColumnHeader = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.directoryPathColumnHeader = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.addFilesButton = new System.Windows.Forms.Button();
            this.addFolderButton = new System.Windows.Forms.Button();
            this.removeButton = new System.Windows.Forms.Button();
            this.fadeInTimeLabel = new System.Windows.Forms.Label();
            this.fadeInTimeNumericUpDown = new System.Windows.Forms.NumericUpDown();
            this.fadeOutTimeNumericUpDown = new System.Windows.Forms.NumericUpDown();
            this.fadeOutTimeLabel = new System.Windows.Forms.Label();
            this.progressBar = new System.Windows.Forms.ProgressBar();
            this.fadeOutPresetComboBox = new System.Windows.Forms.ComboBox();
            this.fadeOutPresetLabel = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.fileList)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.fadeInTimeNumericUpDown)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.fadeOutTimeNumericUpDown)).BeginInit();
            this.SuspendLayout();
            // 
            // fadeInPresetLabel
            // 
            this.fadeInPresetLabel.AutoSize = true;
            this.fadeInPresetLabel.Location = new System.Drawing.Point(44, 32);
            this.fadeInPresetLabel.Name = "fadeInPresetLabel";
            this.fadeInPresetLabel.Size = new System.Drawing.Size(155, 24);
            this.fadeInPresetLabel.TabIndex = 255;
            this.fadeInPresetLabel.Text = "Fade in Preset";
            // 
            // fadeInPresetComboBox
            // 
            this.fadeInPresetComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.fadeInPresetComboBox.FormattingEnabled = true;
            this.fadeInPresetComboBox.Location = new System.Drawing.Point(239, 25);
            this.fadeInPresetComboBox.Name = "fadeInPresetComboBox";
            this.fadeInPresetComboBox.Size = new System.Drawing.Size(544, 32);
            this.fadeInPresetComboBox.Sorted = true;
            this.fadeInPresetComboBox.TabIndex = 0;
            // 
            // cancelButton
            // 
            this.cancelButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.cancelButton.Location = new System.Drawing.Point(16, 620);
            this.cancelButton.Name = "cancelButton";
            this.cancelButton.Size = new System.Drawing.Size(174, 51);
            this.cancelButton.TabIndex = 8;
            this.cancelButton.Text = "cancel";
            this.cancelButton.UseVisualStyleBackColor = true;
            this.cancelButton.Click += new System.EventHandler(this.cancelButton_Click);
            // 
            // closeButton
            // 
            this.closeButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.closeButton.Location = new System.Drawing.Point(1136, 620);
            this.closeButton.Name = "closeButton";
            this.closeButton.Size = new System.Drawing.Size(174, 51);
            this.closeButton.TabIndex = 10;
            this.closeButton.Text = "close";
            this.closeButton.UseVisualStyleBackColor = true;
            this.closeButton.Click += new System.EventHandler(this.closeButton_Click);
            // 
            // runButton
            // 
            this.runButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.runButton.Location = new System.Drawing.Point(956, 620);
            this.runButton.Name = "runButton";
            this.runButton.Size = new System.Drawing.Size(174, 51);
            this.runButton.TabIndex = 9;
            this.runButton.Text = "run";
            this.runButton.UseVisualStyleBackColor = true;
            this.runButton.Click += new System.EventHandler(this.runButton_Click);
            // 
            // fileList
            // 
            this.fileList.AllowDrop = true;
            this.fileList.AllowUserToAddRows = false;
            this.fileList.AllowUserToResizeRows = false;
            this.fileList.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.fileList.BackgroundColor = System.Drawing.SystemColors.Window;
            this.fileList.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.fileList.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.filenameColumnHeader,
            this.directoryPathColumnHeader});
            this.fileList.Location = new System.Drawing.Point(12, 215);
            this.fileList.Name = "fileList";
            this.fileList.ReadOnly = true;
            this.fileList.RowHeadersWidth = 64;
            this.fileList.RowTemplate.Height = 24;
            this.fileList.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.fileList.Size = new System.Drawing.Size(1298, 399);
            this.fileList.TabIndex = 7;
            this.fileList.DragDrop += new System.Windows.Forms.DragEventHandler(this.fileList_DragDrop);
            this.fileList.DragEnter += new System.Windows.Forms.DragEventHandler(this.fileList_DragEnter);
            this.fileList.KeyDown += new System.Windows.Forms.KeyEventHandler(this.fileList_KeyDown);
            // 
            // filenameColumnHeader
            // 
            this.filenameColumnHeader.HeaderText = "filename";
            this.filenameColumnHeader.MinimumWidth = 100;
            this.filenameColumnHeader.Name = "filenameColumnHeader";
            this.filenameColumnHeader.ReadOnly = true;
            this.filenameColumnHeader.Width = 200;
            // 
            // directoryPathColumnHeader
            // 
            this.directoryPathColumnHeader.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.directoryPathColumnHeader.HeaderText = "directory";
            this.directoryPathColumnHeader.Name = "directoryPathColumnHeader";
            this.directoryPathColumnHeader.ReadOnly = true;
            // 
            // addFilesButton
            // 
            this.addFilesButton.Location = new System.Drawing.Point(12, 160);
            this.addFilesButton.Name = "addFilesButton";
            this.addFilesButton.Size = new System.Drawing.Size(151, 49);
            this.addFilesButton.TabIndex = 4;
            this.addFilesButton.Text = "add Files";
            this.addFilesButton.UseVisualStyleBackColor = true;
            this.addFilesButton.Click += new System.EventHandler(this.addFilesButton_Click);
            // 
            // addFolderButton
            // 
            this.addFolderButton.Location = new System.Drawing.Point(169, 160);
            this.addFolderButton.Name = "addFolderButton";
            this.addFolderButton.Size = new System.Drawing.Size(151, 49);
            this.addFolderButton.TabIndex = 5;
            this.addFolderButton.Text = "add Folder";
            this.addFolderButton.UseVisualStyleBackColor = true;
            this.addFolderButton.Click += new System.EventHandler(this.addFolderButton_Click);
            // 
            // removeButton
            // 
            this.removeButton.Location = new System.Drawing.Point(326, 160);
            this.removeButton.Name = "removeButton";
            this.removeButton.Size = new System.Drawing.Size(151, 49);
            this.removeButton.TabIndex = 6;
            this.removeButton.Text = "remove";
            this.removeButton.UseVisualStyleBackColor = true;
            this.removeButton.Click += new System.EventHandler(this.removeButton_Click);
            // 
            // fadeInTimeLabel
            // 
            this.fadeInTimeLabel.AutoSize = true;
            this.fadeInTimeLabel.Location = new System.Drawing.Point(804, 33);
            this.fadeInTimeLabel.Name = "fadeInTimeLabel";
            this.fadeInTimeLabel.Size = new System.Drawing.Size(194, 24);
            this.fadeInTimeLabel.TabIndex = 256;
            this.fadeInTimeLabel.Text = "Fade in Time (sec)";
            // 
            // fadeInTimeNumericUpDown
            // 
            this.fadeInTimeNumericUpDown.DecimalPlaces = 3;
            this.fadeInTimeNumericUpDown.Increment = new decimal(new int[] {
            1,
            0,
            0,
            65536});
            this.fadeInTimeNumericUpDown.Location = new System.Drawing.Point(1035, 25);
            this.fadeInTimeNumericUpDown.Name = "fadeInTimeNumericUpDown";
            this.fadeInTimeNumericUpDown.Size = new System.Drawing.Size(120, 31);
            this.fadeInTimeNumericUpDown.TabIndex = 1;
            this.fadeInTimeNumericUpDown.Value = new decimal(new int[] {
            5,
            0,
            0,
            65536});
            // 
            // fadeOutTimeNumericUpDown
            // 
            this.fadeOutTimeNumericUpDown.DecimalPlaces = 3;
            this.fadeOutTimeNumericUpDown.Increment = new decimal(new int[] {
            1,
            0,
            0,
            65536});
            this.fadeOutTimeNumericUpDown.Location = new System.Drawing.Point(1035, 72);
            this.fadeOutTimeNumericUpDown.Name = "fadeOutTimeNumericUpDown";
            this.fadeOutTimeNumericUpDown.Size = new System.Drawing.Size(120, 31);
            this.fadeOutTimeNumericUpDown.TabIndex = 3;
            this.fadeOutTimeNumericUpDown.Value = new decimal(new int[] {
            5,
            0,
            0,
            65536});
            // 
            // fadeOutTimeLabel
            // 
            this.fadeOutTimeLabel.AutoSize = true;
            this.fadeOutTimeLabel.Location = new System.Drawing.Point(789, 80);
            this.fadeOutTimeLabel.Name = "fadeOutTimeLabel";
            this.fadeOutTimeLabel.Size = new System.Drawing.Size(209, 24);
            this.fadeOutTimeLabel.TabIndex = 258;
            this.fadeOutTimeLabel.Text = "Fade out Time (sec)";
            // 
            // progressBar
            // 
            this.progressBar.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.progressBar.Location = new System.Drawing.Point(206, 620);
            this.progressBar.Name = "progressBar";
            this.progressBar.Size = new System.Drawing.Size(286, 51);
            this.progressBar.TabIndex = 259;
            // 
            // fadeOutPresetComboBox
            // 
            this.fadeOutPresetComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.fadeOutPresetComboBox.FormattingEnabled = true;
            this.fadeOutPresetComboBox.Location = new System.Drawing.Point(239, 72);
            this.fadeOutPresetComboBox.Name = "fadeOutPresetComboBox";
            this.fadeOutPresetComboBox.Size = new System.Drawing.Size(544, 32);
            this.fadeOutPresetComboBox.Sorted = true;
            this.fadeOutPresetComboBox.TabIndex = 2;
            // 
            // fadeOutPresetLabel
            // 
            this.fadeOutPresetLabel.AutoSize = true;
            this.fadeOutPresetLabel.Location = new System.Drawing.Point(29, 80);
            this.fadeOutPresetLabel.Name = "fadeOutPresetLabel";
            this.fadeOutPresetLabel.Size = new System.Drawing.Size(170, 24);
            this.fadeOutPresetLabel.TabIndex = 261;
            this.fadeOutPresetLabel.Text = "Fade out Preset";
            // 
            // GraphicFadeBatchForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(13F, 24F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1322, 683);
            this.Controls.Add(this.fadeOutPresetComboBox);
            this.Controls.Add(this.fadeOutPresetLabel);
            this.Controls.Add(this.progressBar);
            this.Controls.Add(this.fadeOutTimeNumericUpDown);
            this.Controls.Add(this.fadeOutTimeLabel);
            this.Controls.Add(this.fadeInTimeNumericUpDown);
            this.Controls.Add(this.fadeInTimeLabel);
            this.Controls.Add(this.removeButton);
            this.Controls.Add(this.addFolderButton);
            this.Controls.Add(this.addFilesButton);
            this.Controls.Add(this.fileList);
            this.Controls.Add(this.runButton);
            this.Controls.Add(this.closeButton);
            this.Controls.Add(this.cancelButton);
            this.Controls.Add(this.fadeInPresetComboBox);
            this.Controls.Add(this.fadeInPresetLabel);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "GraphicFadeBatchForm";
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
            this.Text = "KLab Graphic Fade Batch 1.0.0";
            ((System.ComponentModel.ISupportInitialize)(this.fileList)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.fadeInTimeNumericUpDown)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.fadeOutTimeNumericUpDown)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.Label fadeInPresetLabel;
        private System.Windows.Forms.ComboBox fadeInPresetComboBox;
        private System.Windows.Forms.Button cancelButton;
        private System.Windows.Forms.Button closeButton;
        private System.Windows.Forms.Button runButton;
        private System.Windows.Forms.DataGridView fileList;
        private System.Windows.Forms.Button addFilesButton;
        private System.Windows.Forms.Button addFolderButton;
        private System.Windows.Forms.Button removeButton;
        private System.Windows.Forms.Label fadeInTimeLabel;
        private System.Windows.Forms.NumericUpDown fadeInTimeNumericUpDown;
        private System.Windows.Forms.NumericUpDown fadeOutTimeNumericUpDown;
        private System.Windows.Forms.Label fadeOutTimeLabel;
        private System.Windows.Forms.ProgressBar progressBar;
        private System.Windows.Forms.ComboBox fadeOutPresetComboBox;
        private System.Windows.Forms.Label fadeOutPresetLabel;
        private System.Windows.Forms.DataGridViewTextBoxColumn filenameColumnHeader;
        private System.Windows.Forms.DataGridViewTextBoxColumn directoryPathColumnHeader;
    }
}

﻿public class EntryPoint
{
    public void FromSoundForge(SoundForge.IScriptableApp app)
    {
        SoundForgeProForm.GraphicFadeBatchForm form = new SoundForgeProForm.GraphicFadeBatchForm(app);
        form.ShowDialog();
    }
} //EntryPoint
