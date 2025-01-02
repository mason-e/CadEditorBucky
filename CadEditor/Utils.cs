﻿using System;
using System.Windows.Forms;
using System.Globalization;
using System.IO;
using System.Drawing;
using System.Linq;
using System.Collections.Generic;

namespace CadEditor
{
    public static class Utils
    {
        public static int parseInt(string value, int defaultVal = 0)
        {
            int ans;
            //try hex parsing
            if ((value.Length > 2) && (value[0] == '0') && ((value[1] == 'x') || (value[1] == 'X')))
            {
                var newStr = value.Substring(2);
                if (int.TryParse(newStr, NumberStyles.HexNumber, CultureInfo.CurrentCulture, out ans))
                  return ans;
                else 
                  return defaultVal;
            }
            if (int.TryParse(value, out ans))
                return ans;
            else
                return defaultVal;
        }

        public static int[] transpose(int[] matrix, int w, int h)
        {
            var result = new int[h * w];
            for (int i = 0; i < w; i++)
            {
                for (int j = 0; j < h; j++)
                {
                    result[j * w + i] = matrix[i * h + j];
                }
            }
            return result;
        }

        public static int getChrAddress(int id)
        {
            return ConfigScript.videoOffset.beginAddr + ConfigScript.videoOffset.recSize * id;
        }

        public static int getChrObjAddress(int id)
        {
            return ConfigScript.videoObjOffset.beginAddr + ConfigScript.videoObjOffset.recSize * id;
        }

        public static byte[] getVideoChunk(int videoPageId)
        {
            byte[] videoChunk = new byte[Globals.videoPageSize];
            int videoAddr = ConfigScript.getVideoPageAddr(videoPageId);
            Array.Copy(Globals.romdata, videoAddr, videoChunk, 0, Globals.videoPageSize);
            return videoChunk;
        }

        public static void setVideoChunk(int videoPageId, byte[] videoChunk)
        {
            //local version for cad & dwd
            int videoAddr = ConfigScript.getVideoPageAddr(videoPageId);
            for (int i = 0; i < Globals.videoPageSize; i++)
                Globals.romdata[videoAddr + i] = videoChunk[i];
        }

        public static byte[] getPalleteLinear(int palIndex)
        {
            int palSize = Globals.palLen;
            var palette = new byte[palSize];
            int addr = ConfigScript.palOffset.beginAddr + palIndex * ConfigScript.palOffset.recSize;
            for (int i = 0; i < palSize; i++)
                palette[i] = Globals.romdata[addr + i];
            return palette;
        }

        public static void setPalleteLinear(int palIndex, byte[] pallete)
        {
            int addr = ConfigScript.palOffset.beginAddr + palIndex * ConfigScript.palOffset.recSize;
            for (int i = 0; i < 16; i++)
                Globals.romdata[addr + i] = pallete[i];
        }

        public static GetLayoutFunc getLayoutLinearFunc()
        {
            return getLayoutLinear;
        }

        public static GetLayoutFunc getDefaultLayoutFunc()
        {
            return getDefaultLayout;
        }

        public static LevelLayerData getLayoutLinear(int curActiveLayout)
        {
            int layoutAddr = ConfigScript.getLayoutAddr(curActiveLayout);
            int scrollAddr = ConfigScript.getScrollAddr(curActiveLayout);
            int width =  ConfigScript.getLevelWidth(curActiveLayout);
            int height = ConfigScript.getLevelHeight(curActiveLayout);
            int[] layer = new int[width * height];
            for (int i = 0; i < width * height; i++)
                layer[i] = Globals.romdata[layoutAddr + i];
            bool needScrolls = ConfigScript.getScrollsOffsetFromLayout() != 0;
            var scrolls = new int[width * height];
            if (needScrolls)
            {
                for (int i = 0; i < width * height; i++)
                    scrolls[i] = Globals.romdata[scrollAddr + i];
            }
            return new LevelLayerData(width, height, layer, needScrolls ? scrolls : null, null);
        }

        public static bool setLayoutLinear(LevelLayerData curActiveLayerData, int curActiveLayout)
        {
            int layerAddr, scrollAddr, width, height;
            layerAddr = ConfigScript.getLayoutAddr(curActiveLayout);
            scrollAddr = ConfigScript.getScrollAddr(curActiveLayout); //darkwing duck specific
            width = curActiveLayerData.width;
            height = curActiveLayerData.height;
            for (int i = 0; i < width * height; i++)
            {
                Globals.romdata[layerAddr + i] = (byte)curActiveLayerData.layer[i];
            }
            if (curActiveLayerData.scroll != null)
            {
                for (int i = 0; i < width * height; i++)
                {
                    Globals.romdata[scrollAddr + i] = (byte)curActiveLayerData.scroll[i];
                }
            }
            return true;
        }

        public static LevelLayerData getDefaultLayout(int curActiveLayout)
        {
            int[] layer = new int[1];
            layer[0] = 1;
            return new LevelLayerData(1, 1, layer);
        }

        public static int getBigTileNoFromScreen(int[] screenData, int index)
        {
            if (index == -1)
                return -1;
            return screenData[index];
        }

        public static void setBigTileToScreen(int[] screenData, int index, int value)
        {
            screenData[index] = value;
        }

        //strip ints to bytes
        public static byte[] linearizeBigBlocks(BigBlock[] bigBlocks)
        {
            if ((bigBlocks == null)  || (bigBlocks.Length == 0))
            {
                return new byte[0];
            }
            byte[] result = new byte[bigBlocks.Length * bigBlocks[0].getSize()];
            for (int i = 0; i < bigBlocks.Length; i++)
            {
                int size = bigBlocks[i].getSize();
                var byteIndexes = bigBlocks[i].indexes.Select(old => (byte)old).ToArray();
                Array.Copy(byteIndexes, 0, result, i*size, size);
            }
            return result;
        }

        public static T[] unlinearizeBigBlocks<T>(byte[] data, int w, int h)
            where T : BigBlock
        {
            if ((data == null)  || (data.Length == 0))
            {
                return new T[0];
            }
            int size = w*h;
            T[] result = new T[data.Length / size];
            for (int i = 0; i < result.Length; i++)
            {
                result[i] = Activator.CreateInstance(typeof(T), w,h) as T;
                Array.Copy(data, i*size, result[i].indexes, 0, size);
            }
            return result;
        }

        public static bool getBit(byte b, int bit)
        {
            return (b & (1 << bit - 1)) != 0;
        }

        private static byte[] BitReverseTable =
        {
            0x00, 0x80, 0x40, 0xc0, 0x20, 0xa0, 0x60, 0xe0,
            0x10, 0x90, 0x50, 0xd0, 0x30, 0xb0, 0x70, 0xf0,
            0x08, 0x88, 0x48, 0xc8, 0x28, 0xa8, 0x68, 0xe8,
            0x18, 0x98, 0x58, 0xd8, 0x38, 0xb8, 0x78, 0xf8,
            0x04, 0x84, 0x44, 0xc4, 0x24, 0xa4, 0x64, 0xe4,
            0x14, 0x94, 0x54, 0xd4, 0x34, 0xb4, 0x74, 0xf4,
            0x0c, 0x8c, 0x4c, 0xcc, 0x2c, 0xac, 0x6c, 0xec,
            0x1c, 0x9c, 0x5c, 0xdc, 0x3c, 0xbc, 0x7c, 0xfc,
            0x02, 0x82, 0x42, 0xc2, 0x22, 0xa2, 0x62, 0xe2,
            0x12, 0x92, 0x52, 0xd2, 0x32, 0xb2, 0x72, 0xf2,
            0x0a, 0x8a, 0x4a, 0xca, 0x2a, 0xaa, 0x6a, 0xea,
            0x1a, 0x9a, 0x5a, 0xda, 0x3a, 0xba, 0x7a, 0xfa,
            0x06, 0x86, 0x46, 0xc6, 0x26, 0xa6, 0x66, 0xe6,
            0x16, 0x96, 0x56, 0xd6, 0x36, 0xb6, 0x76, 0xf6,
            0x0e, 0x8e, 0x4e, 0xce, 0x2e, 0xae, 0x6e, 0xee,
            0x1e, 0x9e, 0x5e, 0xde, 0x3e, 0xbe, 0x7e, 0xfe,
            0x01, 0x81, 0x41, 0xc1, 0x21, 0xa1, 0x61, 0xe1,
            0x11, 0x91, 0x51, 0xd1, 0x31, 0xb1, 0x71, 0xf1,
            0x09, 0x89, 0x49, 0xc9, 0x29, 0xa9, 0x69, 0xe9,
            0x19, 0x99, 0x59, 0xd9, 0x39, 0xb9, 0x79, 0xf9,
            0x05, 0x85, 0x45, 0xc5, 0x25, 0xa5, 0x65, 0xe5,
            0x15, 0x95, 0x55, 0xd5, 0x35, 0xb5, 0x75, 0xf5,
            0x0d, 0x8d, 0x4d, 0xcd, 0x2d, 0xad, 0x6d, 0xed,
            0x1d, 0x9d, 0x5d, 0xdd, 0x3d, 0xbd, 0x7d, 0xfd,
            0x03, 0x83, 0x43, 0xc3, 0x23, 0xa3, 0x63, 0xe3,
            0x13, 0x93, 0x53, 0xd3, 0x33, 0xb3, 0x73, 0xf3,
            0x0b, 0x8b, 0x4b, 0xcb, 0x2b, 0xab, 0x6b, 0xeb,
            0x1b, 0x9b, 0x5b, 0xdb, 0x3b, 0xbb, 0x7b, 0xfb,
            0x07, 0x87, 0x47, 0xc7, 0x27, 0xa7, 0x67, 0xe7,
            0x17, 0x97, 0x57, 0xd7, 0x37, 0xb7, 0x77, 0xf7,
            0x0f, 0x8f, 0x4f, 0xcf, 0x2f, 0xaf, 0x6f, 0xef,
            0x1f, 0x9f, 0x5f, 0xdf, 0x3f, 0xbf, 0x7f, 0xff
        };

        public static byte reverseBits(byte toReverse)
        {
            return BitReverseTable[toReverse];
        }

        public static void swap<T>(ref T a, ref T b)
        {
            T temp = a;
            a = b;
            b = temp;
        }

        public static bool saveDataToFile(string fn, byte[] data)
        {
            try
            {
                fn = ConfigScript.ConfigDirectory + fn;
                using (FileStream f = File.Open(fn, FileMode.Create))
                {
                    f.Write(data, 0, data.Length);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return false;
            }
            return true;
        }

        public static byte[] loadDataFromFile(string fn)
        {
            byte[] data = null;
            try
            {
                fn = ConfigScript.ConfigDirectory + fn;
                using (FileStream f = File.OpenRead(fn))
                {
                    int size = (int)new FileInfo(fn).Length;
                    data = new byte[size];
                    f.Read(data, 0, size);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            return data;
        }

        public static ObjRec[] readBlocksLinear(byte[] romdata, int addr, int w, int h, int count, bool withAttribs, bool transposeIndexes = false, int stride = 0)
        {
            var objects = new ObjRec[count];
            int blockSize = w * h;
            int pw = (int)Math.Ceiling(w / 2.0);
            int ph = (int)Math.Ceiling(h / 2.0);
            int palSize = pw * ph;
            int fullSize = withAttribs ? blockSize + palSize : blockSize;
            for (int i = 0; i < count; i++)
            {
                var indexes = new int[blockSize];
                var palBytes = new int[palSize];
                int baseAddr = addr + i * (fullSize + stride);
                Array.Copy(romdata, baseAddr, indexes, 0, blockSize);
                if (withAttribs)
                {
                    Array.Copy(romdata, baseAddr + blockSize, palBytes, 0, palSize);
                }
                if (transposeIndexes)
                {
                    indexes = transpose(indexes, w, h);
                }

                //todo: add flag to decode or not to decode type from palBytes
                objects[i] = new ObjRec(w, h, 0, indexes, palBytes);
            }
            return objects;
        }

        public static void writeBlocksLinear(ObjRec[] objects, byte[] romdata, int addr, int count, bool withAttribs, bool transposeIndexes = false, int stride = 0)
        {
            var block0 = objects[0];
            int bw = block0.w, bh = block0.h;
            int blockSize = block0.indexes.Length;
            int palSize = block0.palBytes.Length;
            int fullSize = blockSize + (withAttribs ? palSize : 0);
            for (int i = 0; i < count; i++)
            {
                var obj = objects[i];
                var indexes = obj.indexes;
                if (transposeIndexes)
                {
                    indexes = transpose(indexes, bw, bh);
                }
                int baseAddr = addr + i * (fullSize + stride);
                for (int bi = 0; bi < blockSize; bi++)
                {
                    romdata[baseAddr + bi] = (byte)indexes[bi];
                }
                if (withAttribs)
                {
                    for (int pi = 0; pi < palSize; pi++)
                    {
                        romdata[baseAddr + blockSize + pi] = (byte)obj.palBytes[pi];
                    }
                }
            }
        }

        public static ObjRec[] readBlocksFromAlignedArraysWithoutCropPal(byte[] romdata, int addr, int count)
        {
            //capcom version
            var objects = new ObjRec[count];
            for (int i = 0; i < count; i++)
            {
                byte c1, c2, c3, c4, typeColor;
                c1 = romdata[addr + i];
                c2 = romdata[addr + count * 1 + i];
                c3 = romdata[addr + count * 2 + i];
                c4 = romdata[addr + count * 3 + i];
                typeColor = romdata[addr + count * 4 + i];
                int pal = typeColor;
                objects[i] = new ObjRec(c1, c2, c3, c4, 0, pal);
            }
            return objects;
        }

        public static ObjRec[] readBlocksFromAlignedArrays(byte[] romdata, int addr, int count, bool readType)
        {
            //capcom version
            var objects = new ObjRec[count];
            for (int i = 0; i < count; i++)
            {
                byte c1, c2, c3, c4, typeColor;
                c1 = romdata[addr + i];
                c2 = romdata[addr + count*1 + i];
                c3 = romdata[addr + count*2 + i];
                c4 = romdata[addr + count*3 + i];
                typeColor = romdata[addr + count * 4 + i];
                int pal = typeColor & 0x3;
                int type = 0;
                if (readType)
                {
                    type = (typeColor & 0xF0) >> 4;
                }
                objects[i] = new ObjRec(c1, c2, c3, c4, type, pal);
            }
            return objects;
        }

        public static void writeBlocksToAlignedArrays(ObjRec[] objects, byte[] romdata, int addr, int count, bool withPal, bool writeType)
        {
            for (int i = 0; i < count; i++)
            {
                var obj = objects[i];
                romdata[addr + i] = (byte)obj.indexes[0];
                romdata[addr + count * 1 + i] = (byte)obj.indexes[1];
                romdata[addr + count * 2 + i] = (byte)obj.indexes[2];
                romdata[addr + count * 3 + i] = (byte)obj.indexes[3];
                if (withPal)
                {
                    int typeColor = obj.palBytes[0];
                    if (writeType)
                    {
                        typeColor |= ((obj.type & 0xF) << 4);
                    }
                    romdata[addr + count * 4 + i] = (byte)(typeColor);
                }
            }
        }

        public static byte[] readLinearBigBlockData(int hierLevel, int bigTileIndex)
        {
            return readLinearBigBlockData(hierLevel, bigTileIndex, -1);
        }


        public static byte[] readLinearBigBlockData(int hierLevel, int bigTileIndex, int tileSize)
        {
            //if tileSize == -1, try read it from config
            if (tileSize == -1)
            {
                tileSize = ConfigScript.isBlockSize4x4() ? 16 : 4;
            }

            int wordSize = 1;
            int size = ConfigScript.getBigBlocksCount(hierLevel, bigTileIndex) * tileSize * wordSize;

            byte[] bigBlockIndexes = new byte[size];
            var bigBlocksAddr = ConfigScript.getBigTilesAddr(hierLevel, bigTileIndex);
            for (int i = 0; i < size; i++)
                bigBlockIndexes[i] = Globals.romdata[bigBlocksAddr + i];
            return bigBlockIndexes;
        }

        public static BigBlock[] getBigBlocksCapcomDefault(int bigTileIndex)
        {
            var data = readLinearBigBlockData(0, bigTileIndex);
            return unlinearizeBigBlocks<BigBlock>(data, 2, 2);
        }

        public static void writeLinearBigBlockData(int hierLevel, int bigTileIndex, byte[] bigBlockIndexes)
        {
            int size = bigBlockIndexes.Length;
            int addr = ConfigScript.getBigTilesAddr(hierLevel, bigTileIndex);
            for (int i = 0; i < size; i++)
                Globals.romdata[addr + i] = bigBlockIndexes[i];
        }

        public static void setBigBlocksCapcomDefault(int bigTileIndex, BigBlock[] bigBlockIndexes)
        {
            var data = linearizeBigBlocks(bigBlockIndexes);
            writeLinearBigBlockData(0, bigTileIndex, data);
        }

        public static byte[] readDataFromAlignedArrays(byte[] romdata, int addr, int count)
        {
            byte[] data = new byte[count * 4];
            for (int i = 0; i < count; i++)
            {
                data[i * 4 + 0] = Globals.romdata[addr + count * 0 + i];
                data[i * 4 + 1] = Globals.romdata[addr + count * 1 + i];
                data[i * 4 + 2] = Globals.romdata[addr + count * 2 + i];
                data[i * 4 + 3] = Globals.romdata[addr + count * 3 + i];
            }
            return data;
        }

        public static void writeDataToAlignedArrays(byte[] data, byte[] romdata, int addr, int count)
        {
            for (int i = 0; i < count; i++)
            {
                Globals.romdata[addr + count * 0 + i] = data[i * 4 + 0];
                Globals.romdata[addr + count * 1 + i] = data[i * 4 + 1];
                Globals.romdata[addr + count * 2 + i] = data[i * 4 + 2];
                Globals.romdata[addr + count * 3 + i] = data[i * 4 + 3];
            }
        }

        public static byte[] readDataFromUnalignedArrays(byte[] romdata, int addr1, int addr2, int addr3, int addr4, int count)
        {
            byte[] data = new byte[count * 4];
            for (int i = 0; i < count; i++)
            {
                data[i * 4 + 0] = Globals.romdata[addr1 + i];
                data[i * 4 + 1] = Globals.romdata[addr2 + i];
                data[i * 4 + 2] = Globals.romdata[addr3 + i];
                data[i * 4 + 3] = Globals.romdata[addr4 + i];
            }
            return data;
        }

        public static void writeDataToUnalignedArrays(byte[] data, byte[] romdata, int addr1, int addr2, int addr3, int addr4, int count)
        {
            for (int i = 0; i < count; i++)
            {
                Globals.romdata[addr1 + i] = data[i * 4 + 0];
                Globals.romdata[addr2 + i] = data[i * 4 + 1];
                Globals.romdata[addr3 + i] = data[i * 4 + 2];
                Globals.romdata[addr4 + i] = data[i * 4 + 3];
            }
        }

        public static ObjRec[] getBlocksFromTiles16Pal1(int blockIndex)
        {
            return readBlocksLinearTiles16Pal1(Globals.romdata, ConfigScript.getTilesAddr(blockIndex), ConfigScript.getPalBytesAddr(blockIndex), ConfigScript.getBlocksCount(blockIndex));
        }

        public static void setBlocksFromTiles16Pal1(int blockIndex, ObjRec[] blocksData)
        {
            writeBlocksLinearTiles16Pal1(blocksData, Globals.romdata, ConfigScript.getTilesAddr(blockIndex), ConfigScript.getPalBytesAddr(blockIndex), ConfigScript.getBlocksCount(blockIndex));
        }

        public static ObjRec[] getBlocksLinear2x2withoutAttrib(int blockIndex)
        {
            return Utils.readBlocksLinear(Globals.romdata, ConfigScript.getTilesAddr(blockIndex), 2, 2, ConfigScript.getBlocksCount(blockIndex), false);
        }

        public static ObjRec[] getBlocksLinear4x2withoutAttrib(int blockIndex)
        {
            return Utils.readBlocksLinear(Globals.romdata, ConfigScript.getTilesAddr(blockIndex), 4, 2, ConfigScript.getBlocksCount(blockIndex), false);
        }

        public static void setBlocksLinearWithoutAttrib(int blockIndex, ObjRec[] blocksData)
        {
            writeBlocksLinear(blocksData, Globals.romdata, ConfigScript.getTilesAddr(blockIndex), ConfigScript.getBlocksCount(blockIndex), false);
        }

        public static ObjRec[] readBlocksLinearTiles16Pal1(byte[] romdata, int addr, int palBytesAddr, int count)
        {
            int BLOCK_W = 4;
            int BLOCK_H = 4;
            var objects = readBlocksLinear(romdata, addr, BLOCK_W, BLOCK_H, count, false);
            for (int i = 0; i < count; i++)
            {
                int palByte = romdata[palBytesAddr + i];
                var palBytes = new[] { (palByte >> 0) & 3, (palByte >> 2) & 3, (palByte >> 4) & 3, (palByte >> 6) & 3 };
                objects[i].palBytes = palBytes;
            }
            return objects;
        }

        public static void writeBlocksLinearTiles16Pal1(ObjRec[] objects, byte[] romdata, int addr, int palBytesAddr, int count)
        {
            writeBlocksLinear(objects, romdata, addr, count, false);
            for (int i = 0; i < count; i++)
            {
                var objPalBytes = objects[i].palBytes;
                int palByte = objPalBytes[0] | objPalBytes[1] << 2 | objPalBytes[2] << 4 | objPalBytes[3] << 6;
                romdata[palBytesAddr + i] = (byte)palByte;
            }
        }

        public static T[] mergeArrays<T>(T[] a, T[] b)
        {
            var c = new T[a.Length + b.Length];
            a.CopyTo(c, 0);
            b.CopyTo(c, a.Length);
            return c;
        }

        //
        public static int readWord(byte[] data, int addr)
        {
            return (short)(data[addr] << 8 | data[addr + 1]);
        }

        public static int readWordUnsigned(byte[] data, int addr)
        {
            return (data[addr] << 8 | data[addr + 1]);
        }

        public static int readWordLE(byte[] data, int addr)
        {
            return (short)(data[addr+1] << 8 | data[addr]);
        }

        public static int readWordUnsignedLE(byte[] data, int addr)
        {
            return data[addr + 1] << 8 | data[addr];
        }

        public static int readInt(byte[] data, int addr)
        {
            return data[addr] << 24 | data[addr + 1] << 16 | data[addr + 2] << 8 | data[addr + 3];
        }

        public static int readIntLE(byte[] data, int addr)
        {
            return data[addr + 3] << 24 | data[addr + 2] << 16 | data[addr + 1] << 8 | data[addr];
        }

        public static void writeWord(byte[] data, int addr, int word)
        {
            data[addr]     = (byte)(word >> 8);
            data[addr + 1] = (byte)(word & 0xFF);
        }

        public static void writeWordLE(byte[] data, int addr, int word)
        {
            data[addr+1] = (byte)(word >> 8);
            data[addr] = (byte)(word & 0xFF);
        }

        public static void writeInt(byte[] data, int addr, int word)
        {
            data[addr + 0] = (byte)(word >> 24);
            data[addr + 1] = (byte)((word&0x00FF0000) >> 16);
            data[addr + 2] = (byte)((word&0x0000FF00) >> 8 );
            data[addr + 3] = (byte)(word & 0xFF);
        }

        //for capcom unrom mappers, only for certain banks
        public static int getCapcomAnimAddr(int bank, int addr)
        {
            if (bank == 0x04)
                return 0x8000 + addr + 0x10;
            else if (bank == 0x05)
                return 0xC000 + addr + 0x10;
            return addr;
        }

        public static int makeAddrPtr(byte hi, byte lo)
        {
            return (hi << 8) | lo;
        }

        public static int getSignedFromByte(byte b)
        {
            if (b < 128)
                return b;
            else
                return -256 + b;
        }

        public static Screen[] loadScreensDiffSize()
        {
            var offsets = ConfigScript.screensOffset;
            int totalCount = 0;
            int count = offsets.Length;
            for (int i = 0; i < count; i++)
            {
                totalCount += offsets[i].recCount;
            }
            var screens = new Screen[totalCount];

            int  currentScreen = 0;
            for (int i = 0; i < count; i++)
            {
                for (int scrI = 0; scrI < offsets[i].recCount; scrI++)
                {
                    var screen = Globals.getScreen(offsets[i], scrI);
                    if (ConfigScript.loadPhysicsLayerFunc != null)
                    {
                        screen.physicsLayer =
                            new BlockLayer(ConfigScript.loadPhysicsLayerFunc(scrI))
                            {
                                showLayer = false
                            }; //render disabled by default; 
                    }

                    screens[currentScreen++] = screen;
                }
            }
            return screens;
        }

        //save screensData from firstScreenIndex to ConfigScript.screensOffset[currentOffset]
        public static void saveScreensToOffset(OffsetRec screensRec, Screen[] screensData, int firstScreenIndex, int currentOffsetIndex, int layerNo)
        {
            var arrayToSave = Globals.dumpdata != null ? Globals.dumpdata : Globals.romdata;
            int wordLen = ConfigScript.getWordLen();
            bool littleEndian = ConfigScript.isLittleEndian();
            //write back tiles
            int dataStride = ConfigScript.getScreenDataStride();
            for (int i = 0; i < screensRec.recCount; i++)
            {
                var curScrNo = firstScreenIndex + i;
                var curScreen = screensData[curScrNo];
                var dataToWrite = curScreen.layers[layerNo].data;
                if (ConfigScript.getScreenVertical())
                {
                    dataToWrite = Utils.transpose(dataToWrite, screensRec.width, screensRec.height);
                }
                int addr = screensRec.beginAddr + i * screensRec.recSize * (dataStride * wordLen);
                if (wordLen == 1)
                {
                    for (int x = 0; x < screensRec.recSize; x++)
                        arrayToSave[addr + x * dataStride] = (byte)ConfigScript.backConvertScreenTile(dataToWrite[x]);
                }
                else if (wordLen == 2)
                {
                    if (littleEndian)
                    {
                        for (int x = 0; x < screensRec.recSize; x++)
                            Utils.writeWordLE(arrayToSave, addr + x * (dataStride * wordLen), ConfigScript.backConvertScreenTile(dataToWrite[x]));
                    }
                    else
                    {
                        for (int x = 0; x < screensRec.recSize; x++)
                            Utils.writeWord(arrayToSave, addr + x * (dataStride * wordLen), ConfigScript.backConvertScreenTile(dataToWrite[x]));
                    }
                }

                //write physics info, if it present
                if (curScreen.physicsLayer != null)
                {
                    ConfigScript.savePhysicsLayerFunc?.Invoke(curScrNo, curScreen.physicsLayer.data);
                }
            }
        }

        public static void saveScreensDiffSize(Screen[] screensData)
        {
            int offsetsCount = ConfigScript.screensOffset.Length;
            int currentScreenIndex = 0;
            for (int currentOffsetIndex = 0; currentOffsetIndex < offsetsCount; currentOffsetIndex++)
            {
                saveScreensToOffset(ConfigScript.screensOffset[currentOffsetIndex], screensData, currentScreenIndex, currentOffsetIndex, 0);
                currentScreenIndex += ConfigScript.screensOffset[currentOffsetIndex].recCount;
            }
            //todo: save all layers
        }

        public static byte[] readVideoBankFromFile(string filename, int videoPageId)
        {
            try
            {
                filename = ConfigScript.ConfigDirectory + filename;

                using (FileStream f = File.OpenRead(filename))
                {

                    byte[] videodata = new byte[(int)f.Length];
                    f.Read(videodata, 0, (int)f.Length);
                    byte[] ans = new byte[0x1000];
                    int offset = videoPageId * 0x1000;
                    for (int i = 0; i < ans.Length; i++)
                        ans[i] = videodata[offset + i];
                    return ans;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            return null;
        }

        public static byte[] readBinFile(string filename)
        {
            try
            {
                filename = ConfigScript.ConfigDirectory + filename;
                using (FileStream f = File.OpenRead(filename))
                {
                    byte[] d = new byte[(int)f.Length];
                    f.Read(d, 0, (int)f.Length);
                    return d;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            return null;
        }

        public static byte[] readVideoBankFrom16Pointers(int[] ptrs)
        {
            byte[] videoChunk = new byte[Globals.videoPageSize];
            for (int i = 0; i < ptrs.Length; i++)
            {
                var ptr = ptrs[i];
                for (int pi = 0; pi < 256; pi++)
                  videoChunk[i*256 + pi] = Globals.romdata[ptr + pi];
            }
            return videoChunk;
        }

        public static  void defaultDrawObject(Graphics g, ObjectRec curObject, int listNo, bool isSelected, float curScale, ImageList objectSprites, bool inactive, int leftMargin, int topMargin)
        {
            int x = curObject.x, y = curObject.y;
            var myFont = new Font(FontFamily.GenericSansSerif, 6.0f);
            var selectRect = new Rectangle((int)(x * curScale) - 8 + leftMargin, (int)(y * curScale) - 8 + topMargin, 16, 16);

            if (curObject.type < objectSprites.Images.Count)
            {
                g.DrawImage(objectSprites.Images[curObject.type], new Point((int)(x * curScale) - 8 + leftMargin, (int)(y * curScale) - 8 + topMargin));
            }
            else
            {
                g.FillRectangle(Brushes.Black, new Rectangle((int)(x * curScale) - 8 + leftMargin, (int)(y * curScale) - 8 + topMargin, 16, 16));
                g.DrawString(curObject.type.ToString("X3"), myFont, Brushes.White, new Point((int)(x * curScale) - 8 + leftMargin, (int)(y * curScale) - 8 + topMargin));
            }


            if (isSelected)
            {
                g.DrawRectangle(new Pen(Brushes.Red, 2.0f), selectRect);
            }
            else
            {
                g.DrawRectangle(new Pen(Brushes.White, 1.0f), selectRect);
            }

            if (inactive)
            {
                g.FillRectangle(new SolidBrush(Color.FromArgb(128, 255, 255, 255)), selectRect);
                g.DrawRectangle(new Pen(Brushes.Black, 1.0f), selectRect);
            }
        }

        public static void defaultDrawObjectBig(Graphics g, ObjectRec curObject, int listNo, bool isSelected, float curScale, Image[] objectSpritesBig, bool inactive, int leftMargin, int topMargin)
        {
            int x = curObject.x, y = curObject.y;
            int xsize = objectSpritesBig[curObject.type].Size.Width;
            int ysize = objectSpritesBig[curObject.type].Size.Height;
            var rect = new Rectangle((int)((x - xsize/2) * curScale) + leftMargin, (int)((y - ysize/2) * curScale) + topMargin, (int)(xsize * curScale), (int)(ysize * curScale));
            if (curObject.type < objectSpritesBig.Length)
                g.DrawImage(objectSpritesBig[curObject.type], rect);
            if (isSelected)
                g.DrawRectangle(new Pen(Brushes.Red, 2.0f), rect);

            if (inactive)
            {
                g.FillRectangle(new SolidBrush(Color.FromArgb(128, 255, 255, 255)), rect);
                g.DrawRectangle(new Pen(Brushes.Black, 1.0f), rect);
            }
        }

        //wrapper for calling ling function from python.net
        public static bool seqEquals<T>(T seq1, T seq2)
            where T : IEnumerable<T>
        {
            return seq1.SequenceEqual(seq2);
        }

        public static string patchConfigTemplate(string configText, Dictionary<string, object> patchDict)
        {
            foreach (var kv in patchDict)
            {
                configText = configText.Replace(kv.Key, kv.Value.ToString());
            }
            return configText;
        }
    }
}
