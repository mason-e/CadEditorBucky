﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.IO;
using System.Windows.Forms;
using System.Linq;

namespace CadEditor
{
    public partial class OpenFile : Form
    {
        public OpenFile()
        {
            InitializeComponent();
        }

        private void tbFileName_Click(object sender, EventArgs e)
        {
            ofOpenDialog.Filter = "";
            if (ofOpenDialog.ShowDialog() == DialogResult.OK)
            {
                tbFileName.Text = ofOpenDialog.FileName;
            }
        }

        private void tbConfigName_Click(object sender, EventArgs e)
        {
            ofOpenDialog.Filter = "Config files|*.cs";
            if (ofOpenDialog.ShowDialog() == DialogResult.OK)
            {
                cbConfigName.Text = ofOpenDialog.FileName;
                updateCbConfigInDirectory(cbConfigName.Text);
            }
        }

        private void updateCbConfigInDirectory(string text)
        {
            try
            {
                var dirName = Path.GetDirectoryName(text);
                if (dirName != null)
                {
                    cbConfigName.DropDownWidth = 600;
                    cbConfigName.Items.Clear();
                    cbConfigName.Items.AddRange(Directory.EnumerateFiles(dirName, "Settings_*.cs").ToArray());
                }
            }
            catch (Exception)
            {
                ;
            }
        }

        private void btOpen_Click(object sender, EventArgs e)
        {
            fileName = tbFileName.Text;
            configName = cbConfigName.Text;
            DialogResult = DialogResult.OK;
            Close();

            Properties.Settings.Default["FileName"] = fileName;
            Properties.Settings.Default["ConfigName"] = configName;
            Properties.Settings.Default.Save();
        }

        private void btClose_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        public static string fileName = "";
        public static string configName="";

        private void OpenFile_Load(object sender, EventArgs e)
        {
            if (Properties.Settings.Default["FileName"].ToString() != "")
                tbFileName.Text = Properties.Settings.Default["FileName"].ToString();
            if (Properties.Settings.Default["ConfigName"].ToString() != "")
            {
                cbConfigName.Text = Properties.Settings.Default["ConfigName"].ToString();
            }

            if (fileName == "" && ConfigScript.romName != "")
                tbFileName.Text = ConfigScript.romName;
            if (configName == "" && ConfigScript.cfgName != "")
            {
                cbConfigName.Text = ConfigScript.cfgName;
            }

            ofOpenDialog.InitialDirectory = Environment.CurrentDirectory;
            if (fileName != "")
                tbFileName.Text = fileName;
            if (configName != "")
            {
                cbConfigName.Text = configName;
            }

            updateCbConfigInDirectory(cbConfigName.Text);
        }
    }
}
