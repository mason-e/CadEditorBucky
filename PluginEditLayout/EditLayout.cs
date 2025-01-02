﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;

namespace CadEditor
{
    public partial class EditLayout : Form
    {
        public EditLayout()
        {
            InitializeComponent();
        }

        private void EditForm_Load(object sender, EventArgs e)
        {
            makeScreens();

            var addPath = "";
            if (!File.Exists("scroll_sprites//scrolls.png"))
                addPath = "..//";
            scrollSprites.Images.Clear();
            scrollSprites.Images.AddStrip(Image.FromFile(addPath + "scroll_sprites//scrolls.png"));
            doorSprites.Images.Clear();
            doorSprites.Images.AddStrip(Image.FromFile(addPath + "scroll_sprites//doors.png"));
            dirSprites.Images.Clear();
            dirSprites.Images.AddStrip(Image.FromFile(addPath + "scroll_sprites//dirs.png"));
            objPanel.Controls.Clear();
            objPanel.SuspendLayout();

            for (int i = 0; i < scrollSprites.Images.Count; i++)
            {
                var but = new Button();
                but.Size = new Size(32, 32);
                but.ImageList = scrollSprites;
                but.ImageIndex = i;
                but.Click += buttonScrollClick;
                objPanel.Controls.Add(but);
            }
            objPanel.ResumeLayout();

            doorsPanel.SuspendLayout();

            for (int i = 0; i < doorSprites.Images.Count; i++)
            {
                var but = new Button();
                but.Size = new Size(32, 32);
                but.ImageList = doorSprites;
                but.ImageIndex = i;
                but.Click += buttonDoorClick;
                doorsPanel.Controls.Add(but);
            }
            doorsPanel.ResumeLayout();

            blocksPanel.Controls.Clear();
            blocksPanel.SuspendLayout();
            for (int i = 0; i < ConfigScript.screensOffset[scrLevelNo].recCount; i++)
            {
                var but = new Button
                {
                    Size = new Size(64, 64),
                    ImageList = screenImages,
                    ImageIndex = i
                };
                but.Click += buttonBlockClick;
                blocksPanel.Controls.Add(but);

            }
            blocksPanel.ResumeLayout();

            UtilsGui.setCbItemsCount(cbVideoNo, ConfigScript.videoOffset.recCount);
            UtilsGui.setCbItemsCount(cbBigBlockNo, ConfigScript.bigBlocksOffsets[0].recCount);
            UtilsGui.setCbItemsCount(cbBlockNo, ConfigScript.blocksOffset.recCount);
            UtilsGui.setCbItemsCount(cbPaletteNo, ConfigScript.palOffset.recCount);
            cbVideoNo.SelectedIndex = 0;
            cbBigBlockNo.SelectedIndex = 0;
            cbBlockNo.SelectedIndex = 0;
            cbPaletteNo.SelectedIndex = 0;

            cbLayoutNo.Items.Clear();
            foreach (var lr in ConfigScript.getLevelRecs())
                cbLayoutNo.Items.Add(String.Format("0x{0:X} ({1}x{2})", lr.layoutAddr, lr.width, lr.height));

            cbLayoutNo.SelectedIndex = 0;

            cbShowScrolls.Visible = ConfigScript.isShowScrollsInLayout();
        }

        private void reloadLevelLayer()
        {
            curLevelLayerData = ConfigScript.getLayout(curActiveLayout);
            curActiveBlock = 0;
            pbMap.Invalidate();
        }

        private void makeScreens()
        {
            screenImages.Images.Clear();
            screenImages.Images.Add(makeBlackScreen(64,64,0));
            for (int scrNo = 0; scrNo < 256/*ConfigScript.screensOffset[scrLevelNo].recCount*/; scrNo++)
                screenImages.Images.Add(makeBlackScreen(64, 64, scrNo + 1));
        }

        private Image makeBlackScreen(int w, int h, int no)
        {
            var b = new Bitmap(w, h);
            using (var g = Graphics.FromImage(b))
            {
                g.FillRectangle(Brushes.Black, new Rectangle(0, 0, w, h));
                g.DrawRectangle(new Pen(Color.Green, w/32.0f), new Rectangle(0, 0, w, h));
                if (no % 256 != 0)
                  g.DrawString(String.Format("{0:X}", no), new Font("Arial", w/8.0f), Brushes.White, new Point(0, 0));
            }
            return b;
        }

        private void pb_Paint(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            int w = curWidth;
            int h = curHeight;
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    int index = curLevelLayerData.layer[y * w + x];
                    int scroll = curLevelLayerData.scroll != null ? curLevelLayerData.scroll[y * w + x] : 0;
                    g.DrawImage(screenImages.Images[index % 256], new Rectangle(x*64, y*64, 64, 64));
                    if (showScrolls)
                      g.DrawString(String.Format("{0:X}", scroll), new Font("Arial", 8), new SolidBrush(Color.Red), new Rectangle(x * 64 + 24, y * 64 + 24, 32, 16));
                }
            }
        }

        private void changeScroll(int index)
        {
            if (curLevelLayerData.scroll != null)
            {
                var scrollByteArray = ConfigScript.getScrollByteArray();
                if (curActiveBlock < scrollByteArray.Length)
                {
                    curLevelLayerData.scroll[index] = scrollByteArray[curActiveBlock];
                }
            }
        }

        private void pb_MouseUp(object sender, MouseEventArgs e)
        {
            int dx = e.X / 64;
            int dy = e.Y / 64;
            if (dx >= curLevelLayerData.width || dy >= curLevelLayerData.height)
                return;
            dirty = true;
            int index = dy * curLevelLayerData.width + dx;

            if (drawMode == MapDrawMode.Screens)
                curLevelLayerData.layer[index] = curActiveBlock & 0xFF;
            else if (drawMode == MapDrawMode.Scrolls)
                changeScroll(index);
            else if (drawMode == MapDrawMode.Doors)
            {
                if (curLevelLayerData.scroll != null)
                {
                    curLevelLayerData.scroll[index] = (curActiveBlock & 0x1F) | (curLevelLayerData.scroll[index] & 0xE0);
                }
               
            }
            pbMap.Invalidate();
        }

        private int curActiveBlock;
        private MapDrawMode drawMode = MapDrawMode.Screens;
        private bool dirty;
        private bool showScrolls;
        private LevelLayerData curLevelLayerData;

        private int curActiveLayout;

        private int curVideoNo;
        private int curBigBlockNo;
        private int curBlockNo;
        private int curPalleteNo;

        private int curWidth = 1;
        private int curHeight = 1;

        //
        private int scrLevelNo = 0;
        

        private void cbLevel_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!UtilsGui.askToSave(ref dirty, saveToFile, returnCbLevelIndex))
                return;
            if (cbLayoutNo.SelectedIndex == -1)
                return;

            curActiveLayout = cbLayoutNo.SelectedIndex;
            curWidth = ConfigScript.getLevelWidth(curActiveLayout);
            curHeight = ConfigScript.getLevelHeight(curActiveLayout);

            drawMode = MapDrawMode.Screens;
            curActiveBlock = 0;
            activeBlock.Image = screenImages.Images[0];

            updatePanelsVisibility();
            cbLayoutNo.Items.Clear();
            foreach (var lr in ConfigScript.getLevelRecs())
                cbLayoutNo.Items.Add(String.Format("0x{0:X} ({1}x{2})", lr.layoutAddr, lr.width, lr.height));
            UtilsGui.setCbIndexWithoutUpdateLevel(cbLayoutNo, cbLevel_SelectedIndexChanged, curActiveLayout);
            reloadLevelLayer();
        }

        private void updatePanelsVisibility()
        {
            bool showScroll = ConfigScript.isShowScrollsInLayout();
            pnDoors.Visible = showScroll;
            pnSelectScroll.Visible = showScroll;
            pnGeneric.Visible = true;
        }

        private void buttonBlockClick(Object button, EventArgs e)
        {
            int index = ((Button)button).ImageIndex;
            activeBlock.Image = screenImages.Images[index];
            curActiveBlock = index;
            drawMode = MapDrawMode.Screens;
        }

        private void buttonScrollClick(Object button, EventArgs e)
        {
            int index = ((Button)button).ImageIndex;
            activeBlock.Image = scrollSprites.Images[index];
            curActiveBlock = index;
            drawMode = MapDrawMode.Scrolls;
        }

        private void buttonDoorClick(Object button, EventArgs e)
        {
            int index = ((Button)button).ImageIndex;
            activeBlock.Image = doorSprites.Images[index];
            curActiveBlock = index;
            drawMode = MapDrawMode.Doors;
        }

        private void EditForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (dirty)
            {
                DialogResult dr = MessageBox.Show("Level was changed. Do you want to save current level?", "Save", MessageBoxButtons.YesNo);
                if (dr == DialogResult.Yes)
                    saveToFile();
            }
        }

        private bool saveToFile()
        {
            if (!ConfigScript.setLayout(curLevelLayerData, curActiveLayout))
            {
                return false;
            }

            dirty = !Globals.flushToFile();
            return !dirty; 
        }

        private void returnCbLevelIndex()
        {
            UtilsGui.setCbIndexWithoutUpdateLevel(cbLayoutNo, cbLevel_SelectedIndexChanged, curActiveLayout);
        }

        private void btSave_Click(object sender, EventArgs e)
        {
            saveToFile();
        }

        private void cbShowScrolls_CheckedChanged(object sender, EventArgs e)
        {
            showScrolls = cbShowScrolls.Checked;
            pbMap.Invalidate();
        }

        private Bitmap makeLevelImage()
        {
            var answer = new Bitmap(curWidth*512, curHeight*512);
            using (var g = Graphics.FromImage(answer))
            {
                for (int w = 0; w < curWidth; w++)
                {
                    for (int h = 0; h < curHeight; h++)
                    {
                        int scrNo = curLevelLayerData.layer[h*curWidth + w] - 1;
                        Bitmap scr = scrNo >= 0 ? ConfigScript.videoNes.makeScreen(scrNo, scrLevelNo, curVideoNo, curBigBlockNo, curBlockNo, curPalleteNo) : VideoHelper.emptyScreen(512,512,false);
                        g.DrawImage(scr, new Point(w*512,h*512));
                    }
                }
            }
            return answer;
        }

        private void cbVideoNo_SelectedIndexChanged(object sender, EventArgs e)
        {
            curVideoNo = cbVideoNo.SelectedIndex + 0x90;
            curBigBlockNo = cbBigBlockNo.SelectedIndex;
            curBlockNo = cbBlockNo.SelectedIndex;
            curPalleteNo = cbPaletteNo.SelectedIndex;
        }
    }        
    enum MapDrawMode
    {
        Screens,
        Scrolls,
        Doors
    };
}
