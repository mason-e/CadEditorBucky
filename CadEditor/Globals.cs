﻿using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Windows.Forms;
using System.Globalization;
using System.Linq;

using System.Drawing;

namespace CadEditor
{
    public static class Globals
    {
        static Globals()
        {
        }

        public static bool loadData(string filename, string dumpfile, string configFilename)
        {
            try
            {
                int size = (int)new FileInfo(filename).Length;
                using (FileStream f = File.OpenRead(filename))
                {
                    romdata = new byte[size];
                    f.Read(romdata, 0, size);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Load rom error");
                return false;
            }

            try
            {
                if (dumpfile != "")
                {
                    int size = (int)new FileInfo(dumpfile).Length;
                    using (FileStream f = File.OpenRead(dumpfile))
                    {
                        dumpdata = new byte[size];
                        f.Read(dumpdata, 0, size);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Load dump error");
                return false;
            }
            
            try
            {
                ConfigScript.LoadFromFile(configFilename);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Load config error");
                return false;
            }

            return true;
        }

        public static bool flushToFile()
        {
            if (OpenFile.dumpName != "")
            {
                try
                {
                    using (FileStream f = File.OpenWrite(OpenFile.dumpName))
                    {
                        f.Write(Globals.dumpdata, 0, Globals.dumpdata.Length);
                        f.Seek(0, SeekOrigin.Begin);
                    }
                }

                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                    return false;
                }
            }
            try
            {
                using (FileStream f = File.OpenWrite(OpenFile.fileName))
                {
                    f.Write(Globals.romdata, 0, Globals.romdata.Length);
                    f.Seek(0, SeekOrigin.Begin);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return false;
            }
            return true;
        }

        public static int readBlockIndexFromMap(byte[] arrayWithData, int romAddr, int index)
        {
            int wordLen = ConfigScript.getWordLen();
            bool littleEndian = ConfigScript.isLittleEndian();
            int dataStride = ConfigScript.getScreenDataStride();
            if (wordLen == 1)
            {
                return ConfigScript.convertScreenTile(arrayWithData[romAddr + index * dataStride]);
            }
            else if (wordLen == 2)
            {
                if (littleEndian)
                {
                    return ConfigScript.convertScreenTile(Utils.readWordLE(arrayWithData, romAddr + index * (dataStride * wordLen)));
                }
                else
                {
                    return ConfigScript.convertScreenTile(Utils.readWord(arrayWithData, romAddr + index * (dataStride * wordLen)));
                }
            }
            return -1;
        }

        public static Screen getScreen(OffsetRec screenOffset,  int screenIndex)
        {
            var result = new int[Math.Max(64, screenOffset.recSize)];
            var arrayWithData = Globals.dumpdata != null ? Globals.dumpdata : Globals.romdata;
            int dataStride = ConfigScript.getScreenDataStride();
            int wordLen = ConfigScript.getWordLen();
            //bool littleEndian = ConfigScript.isLittleEndian();
            int beginAddr = screenOffset.beginAddr + screenIndex * screenOffset.recSize * dataStride * wordLen;
            for (int i = 0; i < screenOffset.recSize; i++)
                result[i] = readBlockIndexFromMap(arrayWithData, beginAddr, i);
            //TODO: read layer2

            int w = screenOffset.width;
            int h = screenOffset.height;
            if (ConfigScript.getScreenVertical())
            {
                Utils.swap(ref w, ref h);
                result = Utils.transpose(result, w, h);
            }

            return new Screen(new BlockLayer(result), w, h);
        }

        public static byte[] romdata;
        public static byte[] dumpdata;
        public static int chunksCount = 256;
        public static int videoPageSize = 4096;
        public static int palLen = 16;
    }
}