﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Linq;
using System.IO;

namespace CadEditor
{
    public partial class BigBlockEdit : Form
    {
        public BigBlockEdit()
        {
            InitializeComponent();
        }

        private void BigBlockEdit_Load(object sender, EventArgs e)
        {
            curHierarchyLevel = 0;
            curTileset = 0;
            curVideo = 0;
            curPallete = 0;
            dirty = false;
            updateSaveVisibility();
            curViewType = MapViewType.Tiles;

            initControls();
            reloadLevel();
            reloadBlocksPanel();

            readOnly = false; //must be read from config
            tbbSave.Enabled = !readOnly;
        }

        protected void reloadBlocksPanel()
        {
            if (smallBlocksImages == null)
            {
                return;
            }
            var sb0 = smallBlocksImages[0];
            int sbw = sb0[0].Width;
            int sbh = sb0[0].Height;
            UtilsGui.resizeBlocksScreen(sb0, blocksScreen, sbw, sbh, 1.0f);
            blocksScreen.Invalidate();
        }

        protected virtual void initControls()
        {
            UtilsGui.setCbItemsCount(cbHierarchyLevel, ConfigScript.getbigBlocksHierarchyCount());
            UtilsGui.setCbItemsCount(cbVideoNo, ConfigScript.videoOffset.recCount);
            UtilsGui.setCbItemsCount(cbBigBlock, ConfigScript.bigBlocksOffsets[curHierarchyLevel].recCount);
            UtilsGui.setCbItemsCount(cbPaletteNo, ConfigScript.palOffset.recCount);
            cbTileset.Items.Clear();
            for (int i = 0; i < ConfigScript.blocksOffset.recCount; i++)
            {
                var str = String.Format("Tileset{0}", i);
                cbTileset.Items.Add(str);
            }

            //generic version
            cbHierarchyLevel.SelectedIndex = 0;
            cbTileset.SelectedIndex = formMain.curActiveBlockNo;
            cbVideoNo.SelectedIndex = formMain.curActiveVideoNo;
            cbBigBlock.SelectedIndex = formMain.curActiveBigBlockNo;
            cbPaletteNo.SelectedIndex = formMain.curActivePalleteNo;
            cbViewType.SelectedIndex = Math.Min((int)formMain.curActiveViewType, cbViewType.Items.Count - 1);
        }

        protected void reloadLevel(bool reloadBigBlocks = true)
        {
            curActiveBlock = 0;
            if (reloadBigBlocks)
            {
                bigBlockIndexes = ConfigScript.getBigBlocksRecursive(curHierarchyLevel, curBigBlockNo);
            }
            setSmallBlocks();
            reloadBlocksPanel();
            mapScreen.Invalidate();
        }

        protected virtual void setSmallBlocks()
        {
            smallBlocksImages = new Image[4][];

            if (curHierarchyLevel == 0)
            {
                if (hasSmallBlocksPals())
                {
                    smallBlocksImages[0] = ConfigScript.videoNes.makeObjects(curVideo, curTileset, curPallete, curViewType);
                }
                else
                {
                    fillSmallBlockImageLists();
                }
            }
            else
            {
                smallBlocksImages[0] = ConfigScript.videoNes.makeBigBlocks(curVideo, curBigBlockNo, curTileset, ConfigScript.getBigBlocksRecursive(curHierarchyLevel-1, curBigBlockNo), curPallete, curViewType, MapViewType.Tiles, curHierarchyLevel-1);
            }
            reloadBlocksPanel();

            //prerender big blocks
            bigBlocksImages = ConfigScript.videoNes.makeBigBlocks(curVideo, curBigBlockNo, curTileset, bigBlockIndexes, curPallete, curViewType, MapViewType.Tiles, curHierarchyLevel);
            //
            int btc = ConfigScript.getBigBlocksCount(curHierarchyLevel, curBigBlockNo);
            int bblocksInRow = 16;
            int bblocksInCol = (btc / bblocksInRow) + 1;
            //
            mapScreen.Size = new Size(bigBlocksImages[0].Width* bblocksInRow, bigBlocksImages[0].Height*bblocksInCol);
        }

        private void fillSmallBlockImageLists()
        {
            for (int i = 0; i < 4; i++)
            {
                smallBlocksImages[i] = ConfigScript.videoNes.makeObjects(curVideo, curTileset, curPallete, curViewType, i);
            }
        }

        protected BigBlock[] bigBlockIndexes;

        //hardcode
        private int getBlockWidth()
        {
            return smallBlocksImages[0][0].Width;
        }

        private int getBlockHeight()
        {
            return smallBlocksImages[0][0].Height;
        }

        protected void mapScreen_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            int btc = ConfigScript.getBigBlocksCount(curHierarchyLevel, curBigBlockNo);
            int bblocksInRow = 16;

            var testBBlock = bigBlockIndexes[0];
            int bWidth = getBlockWidth();
            int bHeight = getBlockHeight();
            int bbWidth  =  bWidth  * testBBlock.width;
            int bbHeight =  bHeight * testBBlock.height;

            var pen = new Pen(Brushes.Magenta);

            for (int i = 0; i < btc; i++)
            {
                int xb = i % bblocksInRow;
                int yb = i / bblocksInRow;
                var rr = new Rectangle(xb * bbWidth, yb * bbHeight, bbWidth, bbHeight);
                g.DrawImage(bigBlocksImages[i], rr);
                g.DrawRectangle(pen, rr);
            }
        }

        private bool hasSmallBlocksPals()
        {
            return bigBlockIndexes[0].smallBlocksWithPal();
        }

        protected void mapScreen_MouseClick(object sender, MouseEventArgs e)
        {
            dirty = true; updateSaveVisibility();

            int btc = ConfigScript.getBigBlocksCount(curHierarchyLevel, curBigBlockNo);
            int bblocksInRow = 16;

            var testBBlock = bigBlockIndexes[0];
            int bWidth = getBlockWidth();
            int bHeight = getBlockHeight();
            int bbWidth  =  bWidth  * testBBlock.width;
            int bbHeight =  bHeight * testBBlock.height;

            int bx = e.X / bbWidth;
            int by = e.Y / bbHeight;
            int dx = (e.X % bbWidth) / bWidth;
            int dy = (e.Y % bbHeight) / bHeight;
            int bigBlockIndex = by * bblocksInRow + bx;
            int insideIndex   = dy * testBBlock.width + dx;
            //prevent out in bounds
            if (bigBlockIndex >= btc)
            {
                return;
            }
            if (e.Button == MouseButtons.Left)
            {
                if (bigBlockIndex < bigBlockIndexes.Length)
                    bigBlockIndexes[bigBlockIndex].indexes[insideIndex] = curActiveBlock;
            }
            else
            {
                //first action - change pal byte if it applicable
                if (!hasSmallBlocksPals())
                {
                    if (bigBlockIndex < bigBlockIndexes.Length)
                    {
                        var bbPal = bigBlockIndexes[bigBlockIndex] as BigBlockWithPal;
                        if (bbPal == null)
                        {
                            return;
                        }
                        //
                        int palByte = bbPal.palBytes[insideIndex];
                        if (++palByte > 3)
                        {
                            palByte = 0;
                        }
                        bbPal.palBytes[insideIndex] = palByte;
                        //
                    }
                }
                //second action - change cur active block to selected
                if (bigBlockIndex < bigBlockIndexes.Length)
                    curActiveBlock = bigBlockIndexes[bigBlockIndex].indexes[insideIndex];
                lbActive.Text = String.Format("({0:X})", curActiveBlock);
                blocksScreen.Invalidate();
            }

            //fix current big blocks image
            bigBlocksImages[bigBlockIndex] = bigBlockIndexes[bigBlockIndex].makeBigBlock(smallBlocksImages);
            mapScreen.Invalidate();
        }

        protected void buttonObjClick(Object button, EventArgs e)
        {
            int index = (int)((Button)button).Tag;
            lbActive.Text = String.Format("({0:X})", curActiveBlock);
            curActiveBlock = index;
        }

        protected int curActiveBlock;
        protected int curTileset;
        protected int curBigBlockNo;
        protected int curHierarchyLevel;

        //generic
        protected int curVideo;
        protected int curPallete;

        protected MapViewType curViewType;

        protected bool dirty;
        protected bool readOnly;

        protected FormMain formMain;

        Image[] bigBlocksImages; //prerendered for faster rendering;
        Image[][] smallBlocksImages;

        protected void updateSaveVisibility()
        {
            tbbSave.Enabled = dirty;
        }

        private void cbLevelPair_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (
                cbTileset.SelectedIndex == -1 ||
                cbVideoNo.SelectedIndex == -1 || 
                cbPaletteNo.SelectedIndex == -1 ||
                cbViewType.SelectedIndex == -1 || 
                cbBigBlock.SelectedIndex == -1 ||
                cbHierarchyLevel.SelectedIndex == -1
                )
            {
                return;
            }
            if (!readOnly && dirty && (sender == cbTileset || sender == cbHierarchyLevel))
            {
                DialogResult dr = MessageBox.Show("Tiles was changed. Do you want to save current tileset?", "Save", MessageBoxButtons.YesNoCancel);
                if (dr == DialogResult.Cancel)
                {
                    returnCbLevelIndexes();
                    return;
                }
                else if (dr == DialogResult.Yes)
                {
                    if (!saveToFile())
                    {
                        returnCbLevelIndexes();
                        return;
                    }
                }
                else
                {
                    dirty = false;
                    updateSaveVisibility();
                }
            }

            //generic version
            curHierarchyLevel = cbHierarchyLevel.SelectedIndex;
            curTileset = cbTileset.SelectedIndex;
            curBigBlockNo = cbBigBlock.SelectedIndex;
            curViewType = (MapViewType)cbViewType.SelectedIndex;

            curVideo = cbVideoNo.SelectedIndex;
            curPallete = cbPaletteNo.SelectedIndex;
            reloadLevel();
        }

        private void returnCbLevelIndexes()
        {
            cbTileset.SelectedIndexChanged -= cbLevelPair_SelectedIndexChanged;
            cbTileset.SelectedIndex = curTileset;
            cbTileset.SelectedIndexChanged += cbLevelPair_SelectedIndexChanged;

            cbHierarchyLevel.SelectedIndexChanged -= cbLevelPair_SelectedIndexChanged;
            cbHierarchyLevel.SelectedIndex = curTileset;
            cbHierarchyLevel.SelectedIndexChanged += cbLevelPair_SelectedIndexChanged;
        }

        protected void btSave_Click(object sender, EventArgs e)
        {
            saveToFile();
        }

        protected bool saveToFile()
        {
            ConfigScript.setBigBlocksHierarchy(curHierarchyLevel, curBigBlockNo, bigBlockIndexes);
            dirty = !Globals.flushToFile();
            updateSaveVisibility();
            return !dirty;
        }

        protected void BigBlockEdit_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!readOnly && dirty)
            {
                DialogResult dr = MessageBox.Show("Tiles was changed. Do you want to save current tileset?", "Save", MessageBoxButtons.YesNo);
                if (dr == DialogResult.Yes)
                    saveToFile();
            }
        }

        protected void btClear_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Are you sure want to clear all blocks?", "Clear", MessageBoxButtons.YesNo) != DialogResult.Yes)
                return;
            for (int i = 0; i < bigBlockIndexes.Length; i++)
            {
                var bb = bigBlockIndexes[i];
                for (int j = 0; j < bb.indexes.Length; j++)
                {
                    bb.indexes[j] = 0;
                }
            }
            dirty = true;
            updateSaveVisibility();
            bigBlocksImages = ConfigScript.videoNes.makeBigBlocks(curVideo, curBigBlockNo, curTileset, bigBlockIndexes, curPallete, curViewType, MapViewType.Tiles, curHierarchyLevel);
            mapScreen.Invalidate();
        }

        public void setFormMain(FormMain f)
        {
            formMain = f;
        }

        protected void mapScreen_MouseMove(object sender, MouseEventArgs e)
        {
            int bblocksInRow = 16;

            var testBBlock = bigBlockIndexes[0];
            int bWidth = getBlockWidth();
            int bHeight = getBlockHeight();
            int bbWidth  =  bWidth  * testBBlock.width;
            int bbHeight =  bHeight * testBBlock.height;

            int bx = e.X / bbWidth;
            int by = e.Y / bbHeight;
            int dx = (e.X % bbWidth) / bWidth;
            int dy = (e.Y % bbHeight) / bHeight;
            int ind = ((by * bblocksInRow + bx) * testBBlock.getSize() + (dy * testBBlock.width + dx)) / testBBlock.getSize();
            lbBigBlockNo.Text = String.Format("({0:X})", ind);
        }

        protected void mapScreen_MouseLeave(object sender, EventArgs e)
        {
            lbBigBlockNo.Text = "()";
        }

        private void blocksScreen_Paint(object sender, PaintEventArgs e)
        {
            MapEditor.renderAllBlocks(e.Graphics, blocksScreen, curActiveBlock, smallBlocksImages[0].Length, new MapEditor.RenderParams
            {
                bigBlocks = smallBlocksImages[0],
                visibleRect = UtilsGui.getVisibleRectangle(pnBlocks, blocksScreen),
                curScale = 1.0f,
                showBlocksAxis = false,
                renderBlockFunc = MapEditor.renderBlocksOnPanelFunc
            });
        }

        private void blocksScreen_MouseDown(object sender, MouseEventArgs e)
        {
            var p = blocksScreen.PointToClient(Cursor.Position);
            int x = p.X, y = p.Y;
            var sb0 = smallBlocksImages[0];
            int sbw = sb0[0].Width;
            int sbh = sb0[0].Height;
            int tileSizeX = (int)(sbw * 1.0f);
            int tileSizeY = (int)(sbh * 1.0f);
            int tx = x / tileSizeX, ty = y / tileSizeY;
            int maxtX = blocksScreen.Width / tileSizeX;
            int index = ty * maxtX + tx;
            if ((tx < 0) || (tx >= maxtX) || (index < 0) || (index > sb0.Length))
            {
                return;
            }

            curActiveBlock = index;
            lbActive.Text = String.Format("({0:X})", index);
            blocksScreen.Invalidate();
        }

        private void pnBlocks_SizeChanged(object sender, EventArgs e)
        {
            reloadBlocksPanel();
        }
    }
}
