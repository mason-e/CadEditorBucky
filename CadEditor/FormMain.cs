﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.IO;
using System.Windows.Forms;

namespace CadEditor
{
    public partial class FormMain : Form
    {
        public FormMain()
        {
            InitializeComponent();
        }

        readonly float[] scaleFactors = { 0.25f, 0.5f, 1.0f, 2.0f, 3.0f, 4.0f };

        private void setDefaultScale()
        {
            curScale = 1;
            if (ConfigScript.getDefaultScale() > 0)
            {
                curScale = ConfigScript.getDefaultScale();
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            KeyPreview = true;
            if (OpenFile.fileName == "" || OpenFile.configName == "")
            {
                if (!openFile())
                {
                    Close();
                    return;
                }
            }
            else
            {
                if (!Globals.loadData(OpenFile.fileName, OpenFile.configName))
                {
                    Close();
                    return;
                }
                setDefaultScale();
                fileLoaded = true;

                resetControls();
            }

            subeditorsDict = new Dictionary<ToolStripButton, Func<Form>> { 
                 { bttBlocks,       makeBlocksEditor },
            };
        }

        private Form makeBlocksEditor()
        {
            var f = new BlockEdit();
            f.setFormMain(this);
            return f;
        }

        private void resetScreens()
        {
            screens = ConfigScript.loadScreens();
            int count = screens.Length;

            int oldScreenNo = cbScreenNo.SelectedIndex;
            cbScreenNo.Items.Clear();
            for (int i = 0; i < count; i++)
                cbScreenNo.Items.Add(String.Format("{0:X}", i + 1));

            if (oldScreenNo == -1)
                cbScreenNo.SelectedIndex = 0;
            else if (oldScreenNo < cbScreenNo.Items.Count)
                cbScreenNo.SelectedIndex = oldScreenNo;
        }

        private void resetControls()
        {
            clearSubeditorButtons();
            ConfigScript.plugins.ForEach((p) => p.addToolButton(this));
            ConfigScript.plugins.ForEach((p) => p.addSubeditorButton(this));

            resetScreens();

            UtilsGui.setCbItemsCount(cbVideoNo, ConfigScript.videoOffset.recCount);
            UtilsGui.setCbItemsCount(cbBigBlockNo, ConfigScript.bigBlocksOffsets[0].recCount);
            UtilsGui.setCbItemsCount(cbBlockNo, ConfigScript.blocksOffset.recCount);
            UtilsGui.setCbItemsCount(cbPaletteNo, ConfigScript.palOffset.recCount);
            UtilsGui.setCbIndexWithoutUpdateLevel(cbVideoNo, cbLevel_SelectedIndexChanged);
            UtilsGui.setCbIndexWithoutUpdateLevel(cbBigBlockNo, cbLevel_SelectedIndexChanged);
            UtilsGui.setCbIndexWithoutUpdateLevel(cbBlockNo, cbLevel_SelectedIndexChanged);
            UtilsGui.setCbIndexWithoutUpdateLevel(cbPaletteNo, cbLevel_SelectedIndexChanged);
            UtilsGui.setCbIndexWithoutUpdateLevel(cbViewType, cbLevel_SelectedIndexChanged);

            cbGroup.Items.Clear();
            foreach (var g in ConfigScript.getGroups())
            {
                cbGroup.Items.Add(g.name);
            }
            dirty = false; updateSaveVisibility();
            showNeiScreens = true;
            showAxis = true;
            showBrush = true;
            curActiveLayer = 0;

            reloadGameType();
            changeLevelIndex(true);

            bttBlocks.Enabled = true;

            tsLayer1.Enabled = true;

            curActiveLayer = 0;

            pnGroups.Visible = ConfigScript.getGroups().Length > 0;

            updateScaleMenuItemsChecked(Array.FindIndex(scaleFactors, el => el == curScale)); //float comparasion with == is danger
            
            resetMapScreenSize();
        }

        void resetMapScreenSize()
        {
            if (bigBlocks.Length > 0)
            {
                var screen = getActiveScreen();
                mapScreen.Size = new Size((int)((screen.width + 2) * bigBlocks[0].Width * curScale), (int)(screen.height * bigBlocks[0].Height * curScale));
            }
        }

        public void reloadLevel(bool reloadScreens = true, bool rebuildBlocks = false)
        {
            setBlocks(rebuildBlocks);
            if (reloadScreens)
                resetScreens();
            mapScreen.Invalidate();
        }

        private void setBlocks(bool needRebuildBlocks)
        {
            //if using pictures
            if (ConfigScript.usePicturesInstedBlocks)
            {
                if (needRebuildBlocks)
                {
                    //get block size from image
                    int w = ConfigScript.getBlocksPicturesWidth();
                    int h = 32;
                    bigBlocks = UtilsGDI.setBlocksForPictures(curScale, w, h, curActiveViewType);
                }
            }
            else
            {
                MapViewType smallObjectsType =
                    curActiveViewType == MapViewType.SmallObjNumbers ? MapViewType.ObjNumbers :
                      curActiveViewType == MapViewType.ObjType ? MapViewType.ObjType : MapViewType.Tiles;

                if (needRebuildBlocks)
                {
                    bigBlocks = ConfigScript.videoNes.makeBigBlocks(curActiveVideoNo, curActiveBigBlockNo, curActiveBlockNo, curActivePalleteNo, smallObjectsType, curActiveViewType, ConfigScript.getbigBlocksHierarchyCount() - 1);
                }
            }

            curActiveBlock = 0;
            updateBlocksImages();
        }

        private void updateBlocksImages()
        {
            if (bigBlocks.Length > 0)
            {
                UtilsGui.resizeBlocksScreen(bigBlocks, blocksScreen, bigBlocks[0].Width, bigBlocks[0].Height, curScale);
                blocksScreen.Invalidate();
            }
        }

        private void renderNeighbornLine(Graphics g, int scrNo, int line, int x)
        {
            Screen prevScreen = screens[scrNo];
            int width = prevScreen.width;
            int height = prevScreen.height;
            int tileSizeX = (int)(bigBlocks[0].Width * curScale);
            int tileSizeY = (int)(bigBlocks[0].Height * curScale);
            int size = width * height;
            int[] indexesPrev = prevScreen.layers[0].data;
            for (int i = 0; i < size; i++)
            {
                if (i % width == line)
                {
                    int bigBlockNo = ConfigScript.getBigTileNoFromScreen(indexesPrev, i);
                    if ((bigBlockNo >= 0) && (bigBlockNo < bigBlocks.Length))
                        g.DrawImage(bigBlocks[bigBlockNo], new Rectangle(x, i / width * tileSizeY, tileSizeX, tileSizeY));
                }
            }
        }

        private void pictureBox1_Paint(object sender, PaintEventArgs e)
        {
            if (!fileLoaded)
                return;
            var g = e.Graphics;

            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
            g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;

            var screen = getActiveScreen();

            int width = screen.width;
            int height = screen.height;
            int tileSizeX = (int)(bigBlocks[0].Width * curScale);
            int tileSizeY = (int)(bigBlocks[0].Height * curScale);
            var visibleRect = UtilsGui.getVisibleRectangle(pnView, mapScreen);
            MapEditor.render(e.Graphics, screens, screenNo, new MapEditor.RenderParams
            {
                bigBlocks = bigBlocks,
                visibleRect = visibleRect,
                curScale = curScale,
                showBlocksAxis = showAxis,
                showBorder = true,
                width = width,
                height = height,
                additionalRenderEnabled = additionalRenderEnabled,
                leftMargin = tileSizeX,
                topMargin = 0
            });

            if (showNeiScreens && (screenNo > 0) && screen.layers[0].showLayer)
            {
                renderNeighbornLine(g, screenNo - 1, (width - 1), 0);
            }
            if (showNeiScreens && (screenNo < ConfigScript.screensOffset[0].recCount - 1) && screen.layers[0].showLayer)
            {
                renderNeighbornLine(g, screenNo + 1, 0 , (width + 1) * tileSizeX);
            }

            //show brush
            bool altPressed = ModifierKeys == Keys.Alt;
            if (showBrush && curActiveBlock != -1 && (curDx != Outside || curDy != Outside) && !altPressed)
            {
                var tx = (curDx + 1) * tileSizeX;
                var ty = curDy * tileSizeY;
                var tileRect = new Rectangle(tx, ty, tileSizeX, tileSizeY);
                g.DrawImage(bigBlocks[curActiveBlock], tileRect);
            }

            if (altPressed && selectionRect)
            {
                int x = Math.Min(selectionMouseX, selectionBeginMouseX);
                int y = Math.Min(selectionMouseY, selectionBeginMouseY);
                int w = Math.Abs(selectionMouseX - selectionBeginMouseX);
                int h = Math.Abs(selectionMouseY - selectionBeginMouseY);
                g.DrawRectangle(new Pen(Brushes.Black, 2.0f), new Rectangle(x, y, w, h));
            }
        }

        //editor globals
        private int curActiveBlock;

        //generic
        private bool dirty;
        private bool showNeiScreens;
        private bool showBrush;

        public static bool fileLoaded;

        const int Outside = -10;
        private int curDx = Outside;
        private int curDy = Outside;
        private bool curClicked;

        private int curActiveLayer;

        //select rect if alt pressed
        private int selectionBeginX, selectionBeginY, selectionEndX, selectionEndY;
        private int selectionBeginMouseX, selectionBeginMouseY, selectionMouseX, selectionMouseY;
        private bool selectionRect;

        private Dictionary<ToolStripButton, Func<Form>> subeditorsDict;

        private void mapScreen_MouseClick(object sender, MouseEventArgs ea)
        {
            var ee = ea.Location;
            if (ee.X < 0) { ee.X += 32768 * 2; }
            if (ee.Y < 0) { ee.Y += 32768 * 2; }

            var screen = getActiveScreen();

            int width = screen.width;

            int dx = ee.X / (int) (bigBlocks[0].Width * curScale) - 1;
            int dy = ee.Y / (int) (bigBlocks[0].Height * curScale);

            if (ea.Button == MouseButtons.Right)
            {
                if (dx == width || dx == -1)
                    return;
                int index = dy * width + dx;
                var layer = getActiveLayer(screens[screenNo]);
                curActiveBlock = ConfigScript.getBigTileNoFromScreen(layer.data, index);
                if (curActiveBlock != -1)
                {
                    activeBlock.Image = bigBlocks[curActiveBlock];
                    lbActiveBlock.Text = String.Format("Label: {0:X}", curActiveBlock);
                }
                blocksScreen.Invalidate();
            }
        }

        private void mapScreen_MouseMove(object sender, MouseEventArgs ea)
        {
            var ee = ea.Location;
            if (ee.X < 0) { ee.X += 32768 * 2; }
            if (ee.Y < 0) { ee.Y += 32768 * 2; }

            if (selectionRect)
            {
                selectionMouseX = ee.X;
                selectionMouseY = ee.Y;
                mapScreen.Invalidate();
                return;
            }
            var screen = getActiveScreen();
            int width = screen.width;
            int dx = ee.X / (int) (bigBlocks[0].Width * curScale) - 1;
            int dy = ee.Y / (int) (bigBlocks[0].Height * curScale);
            lbCoords.Text = String.Format("Coords:({0},{1})", dx, dy);

            bool curDeltaChanged = curDx != dx || curDy != dy;
            if (curDeltaChanged)
            {
                curDx = dx;
                curDy = dy;
            }
            if (curClicked)
            {
                if (dx == width)
                {
                    if (screenNo < ConfigScript.screensOffset[0].recCount - 1)
                    {
                        int index = dy * width;
                        var layer = getActiveLayer(screens[screenNo + 1]);
                        curActiveBlock = ConfigScript.getBigTileNoFromScreen(layer.data, index);
                        ConfigScript.setBigTileToScreen(layer.data, index, curActiveBlock);
                        dirty = true; updateSaveVisibility();
                    }
                }
                else if (dx == -1)
                {
                    if (screenNo > 0)
                    {
                        int index = dy * width + (width - 1);

                        var layer = getActiveLayer(screens[screenNo - 1]);
                        ConfigScript.setBigTileToScreen(layer.data, index, curActiveBlock);
                        dirty = true; updateSaveVisibility();
                    }
                }
                else
                {
                    int index = dy * width + dx;
                    var layer = getActiveLayer(screens[screenNo]);
                    if (index < layer.data.Length)
                    {
                        ConfigScript.setBigTileToScreen(layer.data, index, curActiveBlock);
                    }
                    dirty = true; updateSaveVisibility();
                }
            }
            mapScreen.Invalidate();
        }

        private void mapScreen_MouseLeave(object sender, EventArgs e)
        {
            lbCoords.Text = "Coords:()";
            curDx = Outside;
            curDy = Outside;
            curClicked = false;
            mapScreen.Invalidate();
        }

        private void btSave_Click(object sender, EventArgs e)
        {
            saveToFile();
        }

        private bool saveToFile()
        {
            ConfigScript.saveScreens(screens);
            dirty = !Globals.flushToFile(); updateSaveVisibility();
            return !dirty;
        }

        private void cbLevel_SelectedIndexChanged(object sender, EventArgs ev)
        {
            if (!UtilsGui.askToSave(ref dirty, saveToFile, returnCbLevelIndex))
            {
                updateSaveVisibility();
                return;
            }
            updateSaveVisibility();
            bool senderIsScale = sender == bttScale;
            changeLevelIndex(!senderIsScale);
            var screen = getActiveScreen();
            if (senderIsScale)
            {
                mapScreen.Size = new Size((int)((screen.width + 2) * bigBlocks[0].Width * curScale), (int)(screen.height * bigBlocks[0].Height * curScale));
                updateBlocksImages();
            }
        }

        private void changeLevelIndex(bool reloadBlocks = false)
        {
            curActiveVideoNo = cbVideoNo.SelectedIndex;
            curActiveBigBlockNo = cbBigBlockNo.SelectedIndex;
            curActiveBlockNo = cbBlockNo.SelectedIndex;
            curActivePalleteNo = cbPaletteNo.SelectedIndex;
            curActiveViewType = (MapViewType)cbViewType.SelectedIndex;
            reloadLevel(true, reloadBlocks);
        }

        private void returnCbLevelIndex()
        {
        }

        private void FormMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!UtilsGui.askToSave(ref dirty, saveToFile, returnCbLevelIndex))
            {
                updateSaveVisibility();
                e.Cancel = true;
            }
        }

        private void btSubeditor_Click(object sender, EventArgs e)
        {
            var button = (ToolStripButton)sender;
            subeditorOpen(subeditorsDict[button](), button);
        }

        private void cbScreenNo_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cbScreenNo.SelectedIndex == -1)
                return;
            screenNo = cbScreenNo.SelectedIndex;
            resetMapScreenSize();
            mapScreen.Invalidate();
        }


        private void cbShowNeighborns_CheckedChanged(object sender, EventArgs e)
        {
            showNeiScreens = bttShowNei.Checked;
            mapScreen.Invalidate();
        }

        private bool openFile()
        {
            if (!UtilsGui.askToSave(ref dirty, saveToFile, returnCbLevelIndex))
            {
                updateSaveVisibility();
                return false;
            }
            updateSaveVisibility();
            var f = new OpenFile();
            if (f.ShowDialog() == DialogResult.OK)
            {
                if (!Globals.loadData(OpenFile.fileName, OpenFile.configName))
                {
                    Close();
                    return false;
                }
                setDefaultScale();
                fileLoaded = true;
                resetControls();
                setWindowText();
            }

            if (!fileLoaded)
            {
                return false;
            }

            return true;
            
        }

        public void reloadGameType()
        {
            pnGeneric.Visible = true;
        }

        private void btOpen_Click(object sender, EventArgs e)
        {
            if (openFile())
            {
                reloadGameType();
                changeLevelIndex();
            }
        }

        public void setDirty()
        {
            dirty = true;
            updateSaveVisibility();
        }

        private void updateSaveVisibility()
        {
            bttSave.Enabled = dirty;
        }

        private void cbShowAxis_CheckedChanged(object sender, EventArgs e)
        {
            showAxis = bttAxis.Checked;
            mapScreen.Invalidate();
            blocksScreen.Invalidate();
        }

        private void bttShowBrush_CheckedChanged(object sender, EventArgs e)
        {
            showBrush = bttShowBrush.Checked;
        }

        private FormClosedEventHandler subeditorClosed(ToolStripItem enabledAfterCloseButton)
        {
            return delegate
            { 
                enabledAfterCloseButton.Enabled = true;
                reloadLevel(true, true);
            };
        }

        public void subeditorOpen(Form f, ToolStripItem b, bool showDialog = false)
        {
            if (UtilsGui.askToSave(ref dirty, saveToFile, returnCbLevelIndex))
            {
                updateSaveVisibility();
                b.Enabled = false;
                f.FormClosed += subeditorClosed(b);
                if (showDialog)
                {
                  f.ShowDialog();
                }
                else
                {
                  f.Show();
                }
            }
        }

        public int curActiveVideoNo { get; private set; }
        public int curActiveBlockNo { get; private set; }
        public int curActiveBigBlockNo { get; private set; }
        public int curActivePalleteNo { get; private set; }

        public MapViewType curActiveViewType { get; private set; } = MapViewType.ObjType;
        public bool showAxis { get; private set; }
        public int screenNo { get; private set; }

        public bool additionalRenderEnabled { get; private set; } = true;

        public float curScale { get; private set; } = 2.0f;

        public Screen[] screens { get; private set; }

        public Image[] bigBlocks { get; private set; } = new Image[0];

        public Image[] getBigBlockImages()
        {
            return bigBlocks;
        }

        //warning! danger direct function. do not use it
        public void setScreens(Screen[] newScreens)
        {
            screens = newScreens;
        }

        private void updateScaleMenuItemsChecked(int index)
        {
            foreach (ToolStripMenuItem bttScaleDropDownItem in bttScale.DropDownItems)
            {
                bttScaleDropDownItem.Checked = false;
            }

            (bttScale.DropDownItems[index] as ToolStripMenuItem).Checked = true;
        }

        private void bttScale_DropDownItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            int curScaleItemIndex = bttScale.DropDownItems.IndexOf(e.ClickedItem);
            updateScaleMenuItemsChecked(curScaleItemIndex);
            curScale = scaleFactors[curScaleItemIndex];
            cbLevel_SelectedIndexChanged(bttScale, new EventArgs());
        }

        private void mapScreen_MouseDown(object sender, MouseEventArgs ea)
        {
            var ee = ea.Location;

            //hack to WinAPI very big coordinates - convert signed to unsigned
            if (ee.X < 0) { ee.X += 32768 * 2; }
            if (ee.Y < 0) { ee.Y += 32768 * 2; }

            if (ea.Button == MouseButtons.Left)
            {
                if (ModifierKeys == Keys.Alt)
                {
                    convertMouseToDxDy(ee, out selectionBeginX, out selectionBeginY);
                    selectionBeginMouseX = ee.X;
                    selectionBeginMouseY = ee.Y;
                    selectionRect = true;
                }
                else
                {
                    curClicked = true;
                    mapScreen_MouseMove(sender, ea);
                }
            }
        }

        private void mapScreen_MouseUp(object sender, MouseEventArgs ea)
        {
            var ee = ea.Location;

            //hack to WinAPI very big coordinates - convert signed to unsigned
            if (ee.X < 0) { ee.X += 32768 * 2; }
            if (ee.Y < 0) { ee.Y += 32768 * 2; }

            if (selectionRect)
            {
                convertMouseToDxDy(ee, out selectionEndX, out selectionEndY);
                if (selectionEndX < selectionBeginX)
                {
                    Utils.swap(ref selectionBeginX, ref selectionEndX);
                }
                if (selectionEndY < selectionBeginY)
                {
                    Utils.swap(ref selectionBeginY, ref selectionEndY);
                }
                int deltaX = selectionEndX - selectionBeginX + 1;
                int deltaY = selectionEndY - selectionBeginY + 1;
                int [][] tiles = new int[deltaY][];
                for (int arrs = 0; arrs < tiles.Length; arrs++)
                    tiles[arrs] = new int[deltaX];
                var curScreen = screens[screenNo];
                for (int i = 0; i < deltaX; i++)
                {
                    for (int j = 0; j < deltaY; j++)
                    {
                        int index = (selectionBeginY + j)*curScreen.width + (selectionBeginX + i);
                        tiles[j][i] = curScreen.layers[curActiveLayer].data[index];
                    }
                }
            }
            selectionRect = false;
            curClicked = false;
        }

        private void convertMouseToDxDy(Point e, out int dx, out int dy)
        {
            dx = e.X / (int)(bigBlocks[0].Width * curScale) - 1;
            dy = e.Y / (int)(bigBlocks[0].Height * curScale);
        }

        public void reloadCallback()
        {
            bttReload_Click(null, new EventArgs());
        }

        private void bttReload_Click(object sender, EventArgs e)
        {
            if (UtilsGui.askToSave(ref dirty, saveToFile, returnCbLevelIndex))
            {
                if (!Globals.loadData(OpenFile.fileName, OpenFile.configName))
                {
                    return;
                }
                resetControls();
                reloadLevel(true, true);
                mapScreen.Invalidate();
            }
        }

        private void cbAdvanced_CheckedChanged(object sender, EventArgs e)
        {
            pnAdvancedParams.Visible = cbAdvanced.Checked;
        }

        private void FormMain_KeyDown(object sender, KeyEventArgs e)
        {
            
        }

        private void blocksScreen_Paint(object sender, PaintEventArgs e)
        {
            if (!fileLoaded)
                return;
            var g = e.Graphics;
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
            g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;
            
            var renderParams = new MapEditor.RenderParams
            {
                bigBlocks = bigBlocks,
                visibleRect = UtilsGui.getVisibleRectangle(pnBlocks, blocksScreen),
                curScale = curScale,
                showBlocksAxis = showAxis,
                renderBlockFunc = MapEditor.renderBlocksOnPanelFunc
            };

            int blocksCount = bigBlocks.Length;
            MapEditor.renderAllBlocks(g, blocksScreen, curActiveBlock, blocksCount, renderParams);
        }

        private void blocksScreen_MouseDown(object sender, MouseEventArgs e)
        {
            var p = blocksScreen.PointToClient(Cursor.Position);
            int x = p.X, y = p.Y;
            int tileSizeX = (int)(bigBlocks[0].Width * curScale);
            int tileSizeY = (int)(bigBlocks[0].Height * curScale);
            int tx = x / tileSizeX, ty = y / tileSizeY;
            int maxtX = blocksScreen.Width / tileSizeX;
            int index = ty * maxtX + tx;
            int maxIndex = bigBlocks.Length;
            if ((tx < 0) || (tx >= maxtX) || (index < 0) || (index >= maxIndex))
            {
                return;
            }

            activeBlock.Image = bigBlocks[index];
            curActiveBlock = index;
            lbActiveBlock.Text = String.Format("Label: ({0:X})", index);
            blocksScreen.Invalidate();
        }

        private void pnBlocks_SizeChanged(object sender, EventArgs e)
        {
            updateBlocksImages();
        }

        private void FormMain_Resize(object sender, EventArgs e)
        {
            splitContainer1.Width = Width - 21;
            splitContainer1.Height = Height - 81;
        }

        private void FormMain_LocationChanged(object sender, EventArgs e)
        {
            OnResize(e);
        }

        private void splitContainer1_Panel1_Resize(object sender, EventArgs e)
        {
            pnBlocks.Width = splitContainer1.Panel1.Width - pnElements.Width - 10;
            pnBlocks.Height = splitContainer1.Panel1.Height - 10;
            blocksScreen.Width = pnBlocks.Width;
            updateBlocksImages();
        }

        public void clearSubeditorButtons()
        {
            toolStrip1.Items.Clear();
            ToolStripItem[] items = {
                bttOpen,
                bttSave,
                bttReload,
                toolStripSeparator1,
                //place for plugin
                sToolButtons,

                bttBlocks,
                toolStripSeparator2,

                toolStripSeparator3,

                bttShowNei,
                bttAxis,
                bttShowBrush,
                toolStripSeparator4,

                bttScale,
                toolStripSeparator5,

                tbbShowPluginInfo,
            };

            toolStrip1.Items.AddRange(items);
        }

        public void addSubeditorButton(ToolStripItem item)
        {
          toolStrip1.Items.Insert(toolStrip1.Items.IndexOf(bttBlocks)+1, item);
        }

        private void bttScale_ButtonClick(object sender, EventArgs e)
        {
            bttScale.ShowDropDown();
        }

        public void addToolButton(ToolStripItem item)
        {
            toolStrip1.Items.Insert(toolStrip1.Items.IndexOf(toolStripSeparator1) + 1, item);

        }

        private void tbbShowPluginInfo_Click(object sender, EventArgs e)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("Loaded Plugins:\n\n");
            foreach (var p in ConfigScript.plugins)
            {
                sb.Append(p.getName() + "\n");
            }
            if (ConfigScript.videoNes != null)
                sb.Append(ConfigScript.videoNes.getName() + "\n");
            MessageBox.Show(sb.ToString());
        }

        private void cbGroup_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cbGroup.SelectedIndex < 0)
                return;
            GroupRec g = ConfigScript.getGroup(cbGroup.SelectedIndex);
            UtilsGui.setCbIndexWithoutUpdateLevel(cbVideoNo, cbLevel_SelectedIndexChanged, g.videoNo);
            UtilsGui.setCbIndexWithoutUpdateLevel(cbBigBlockNo, cbLevel_SelectedIndexChanged, g.bigBlockNo);
            UtilsGui.setCbIndexWithoutUpdateLevel(cbBlockNo, cbLevel_SelectedIndexChanged, g.blockNo);
            UtilsGui.setCbIndexWithoutUpdateLevel(cbPaletteNo, cbLevel_SelectedIndexChanged, g.palNo);
            cbLevel_SelectedIndexChanged(cbVideoNo, new EventArgs());
            if (g.firstScreen < 0 || g.firstScreen <= cbScreenNo.Items.Count)
              cbScreenNo.SelectedIndex = g.firstScreen - 1;
        }

        private void setWindowText()
        {
            Text = String.Format("CAD Editor v5.3 - {0}", OpenFile.fileName);
        }

        private Screen getActiveScreen()
        {
            return screens[screenNo];
        }

        private BlockLayer getActiveLayer(Screen curScreen)
        {
            return curScreen.layers[curActiveLayer];
        }
    }
}
