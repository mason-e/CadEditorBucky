using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Reflection;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using CSScriptLibrary;

namespace CadEditor
{
    public static class PluginLoader
    {
        public static T loadPlugin<T>(string path)
        {
            string appPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            if (appPath == null)
            {
                return default(T);
            }
            Assembly currentAssembly = Assembly.LoadFile(Path.Combine(appPath, path));
            foreach (Type type in currentAssembly.GetTypes())
            {
                if (type.GetInterfaces().Contains(typeof(T)))
                    return (T)Activator.CreateInstance(type);
            }
            return default(T);
        }
    }

    //--------------------------------------------------------------------------------------------------------------
    public interface IPlugin
    {
        void addSubeditorButton(FormMain formMain);
        void addToolButton(FormMain formMain);
        void loadFromConfig(object asm, object data); //asm is CSScriptLibrary.AsmHelper
        string getName();
    }

    public interface IVideoPluginNes
    {
        void updateColorsFromConfig();

        Image[] makeBigBlocks(int videoNo, int bigBlockNo, int blockNo, int palleteNo, MapViewType smallObjectsViewType = MapViewType.Tiles,
            MapViewType curViewType = MapViewType.Tiles, int heirarchyLevel = 0);
        Image[] makeBigBlocks(int videoNo, int bigBlockNo,int blockNo, BigBlock[] bigBlockData, int palleteNo, MapViewType smallObjectsViewType = MapViewType.Tiles,
            MapViewType curViewType = MapViewType.Tiles, int heirarchyLevel = 0);

        Bitmap makeImage(int index, byte[] videoChunk, byte[] pallete, int subPalIndex, bool withAlpha = false);

        Bitmap makeObject(int index, ObjRec[] objects, Bitmap[][] objStrips, MapViewType drawType, int constantSubpal = -1);

        Color[] defaultNesColors { get; set; }

        string getName();
    }
}
