using CadEditor;
using System;

public class SharedUtils 
{
  public static GetPalFunc readPalFromBin(string fname)
  {
      return (int _)=> { return Utils.readBinFile(fname); };
  }
  
  public static GetVideoChunkFunc getVideoChunk(string fname)
  {
     return (int _)=> { return Utils.readVideoBankFromFile(fname, 0); };
  }
  
  public static GetPalFunc readPalFromBin(string[] fname)
  {
      return (int x)=> { return Utils.readBinFile(fname[x]); };
  }
  
  public static GetVideoChunkFunc getVideoChunk(string[] fname)
  {
     return (int x)=> { return Utils.readVideoBankFromFile(fname[x], 0); };
  }
}