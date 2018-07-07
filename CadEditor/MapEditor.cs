﻿using System;
using System.Drawing;
using System.Windows.Forms;

namespace CadEditor
{
    public class MapEditor
    {
        public static void Render(Graphics g, Image[] bigBlocks, Rectangle? visibleRect,Screen[] screens, int scrNo, float CurScale, bool ShowBorder, bool showBlocksAxis, int LeftMargin, int TopMargin, int WIDTH, int HEIGHT)
        {
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
            g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;

            bool verticalScreen = ConfigScript.getScreenVertical();
            int SIZE = WIDTH * HEIGHT;

            for (int layerIndex = 0; layerIndex < screens[scrNo].layers.Length; layerIndex++)
            {
                var layer = screens[scrNo].layers[layerIndex];
                bool needRenderLayer = layer != null && layer.showLayer;
                if (!needRenderLayer)
                {
                    continue;
                }

                int TILE_SIZE_X = (int)(bigBlocks[0].Width* CurScale);
                int TILE_SIZE_Y = (int)(bigBlocks[0].Height* CurScale);

                for (int i = 0; i < SIZE; i++)
                {
                    int bigBlockNo = ConfigScript.getBigTileNoFromScreen(layer.data, i);
                    Rectangle tileRect;
                    if (verticalScreen)
                        tileRect = new Rectangle(i / WIDTH * TILE_SIZE_X + LeftMargin, (i % WIDTH) * TILE_SIZE_Y + TopMargin, TILE_SIZE_X, TILE_SIZE_Y);
                    else
                        tileRect = new Rectangle((i % WIDTH) * TILE_SIZE_X + LeftMargin, i / WIDTH * TILE_SIZE_Y + TopMargin, TILE_SIZE_X, TILE_SIZE_Y);

                    if (visibleRect == null || visibleRect.Value.Contains(tileRect) || visibleRect.Value.IntersectsWith(tileRect))
                    {
                        if (bigBlockNo > -1 && bigBlockNo < bigBlocks.Length)
                        {
                            g.DrawImage(bigBlocks[bigBlockNo], tileRect);
                            if (showBlocksAxis)
                            {
                                g.DrawRectangle(new Pen(Color.FromArgb(255, 255, 255, 255)), tileRect);
                            }
                        }
                        //else
                        //    g.FillRectangle(Brushes.White, tileRect);
                    }
                }
            }

            if (ShowBorder)
            {
                int TILE_SIZE_X = (int)(bigBlocks[0].Width * CurScale);
                int TILE_SIZE_Y = (int)(bigBlocks[0].Height * CurScale);
                if (verticalScreen)
                    g.DrawRectangle(new Pen(Color.Green, 4.0f), new Rectangle(0, TILE_SIZE_Y, TILE_SIZE_X * HEIGHT, TILE_SIZE_Y * WIDTH));
                else
                    g.DrawRectangle(new Pen(Color.Green, 4.0f), new Rectangle(TILE_SIZE_X, 0, TILE_SIZE_X * WIDTH, TILE_SIZE_Y * HEIGHT));
            }

            //Additional rendering  //float to int!
            ConfigScript.renderToMainScreen(g, (int)CurScale);
        }

        public static void RenderAllBlocks(Graphics g, PictureBox parentControl, Image[] bigBlocks, int blockWidth, int blockHeight, Rectangle? visibleRect, float CurScale, int activeBlock, bool showBlocksAxis)
        {
            int TILE_SIZE_X = (int)(blockWidth * CurScale);
            int TILE_SIZE_Y = (int)(blockHeight * CurScale);
            int WIDTH = parentControl.Width / TILE_SIZE_X;
            if (WIDTH == 0)
            {
                return;
            }

            for (int i = 0; i < bigBlocks.Length; i++)
            {
                int bigBlockNo = i;
                Rectangle tileRect = new Rectangle((i % WIDTH) * TILE_SIZE_X, i / WIDTH * TILE_SIZE_Y, TILE_SIZE_X, TILE_SIZE_Y);

                if (visibleRect == null || visibleRect.Value.Contains(tileRect) || visibleRect.Value.IntersectsWith(tileRect))
                {
                    if (bigBlockNo > -1 && bigBlockNo < bigBlocks.Length)
                        g.DrawImage(bigBlocks[bigBlockNo], tileRect);
                    else
                        g.FillRectangle(Brushes.White, tileRect);

                    if (showBlocksAxis)
                    {
                        g.DrawRectangle(new Pen(Color.FromArgb(255, 255, 255, 255)), tileRect);
                    }

                    if (i == activeBlock)
                    {
                        g.DrawRectangle(new Pen(Brushes.Red, 3.0f), tileRect);
                    }
                }
            }
        }

        public static Image ScreenToImage(Image[] bigBlocks, Screen[] screens, int scrNo, float CurScale, bool ShowBorder, int LeftMargin, int TopMargin, int WIDTH, int HEIGHT)
        {
            bool verticalScreen = ConfigScript.getScreenVertical();
            int TILE_SIZE_X = (int)(bigBlocks[0].Width * CurScale);
            int TILE_SIZE_Y = (int)(bigBlocks[0].Height * CurScale);

            Image result;
            if (verticalScreen)
                result = new Bitmap(HEIGHT * TILE_SIZE_Y, WIDTH * TILE_SIZE_X);
            else
                result = new Bitmap(WIDTH * TILE_SIZE_X, HEIGHT * TILE_SIZE_Y);

            using (var g = Graphics.FromImage(result))
            {
                Render(g, bigBlocks, null, screens, scrNo, CurScale, ShowBorder, false, LeftMargin, TopMargin, WIDTH, HEIGHT);
            }
            return result;
        }

        /*private static PictureBox mapScreen;
        private static PictureBox activeBlock;
        private static Panel pnView;
        private static ImageList bigBlocks;*/
    }
}
