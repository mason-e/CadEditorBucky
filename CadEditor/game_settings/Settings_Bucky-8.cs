using CadEditor;
using System;
//css_include bucky_ohare/BuckyUtils.cs;

public class Data 
{ 
  public OffsetRec getScreensOffset()  { return new OffsetRec(0x3858, 26 , 8*6, 8, 6);   }
  
  public OffsetRec getVideoOffset()     { return new OffsetRec(0x0 , 3   , 0x1000);  }
  public OffsetRec getPalOffset  ()     { return new OffsetRec(0x0 , 3   , 16); }
  public GetVideoChunkFunc    getVideoChunkFunc()    { return BuckyUtils.getVideoChunk(new[] {"chr8(a).bin", "chr8(b).bin", "chr8(c).bin"}); }
  public SetVideoChunkFunc    setVideoChunkFunc()    { return null; }
  
  public OffsetRec getBlocksOffset()    { return new OffsetRec(0x30e2, 1  , 0x1000);  }
  public int getBlocksCount()           { return 244; }
  public int getBigBlocksCount()        { return 244; }
  public int getPalBytesAddr()          { return 0x3742; }
  
  public GetPalFunc           getPalFunc()           { return BuckyUtils.readPalFromBin(new[] {"pal8(a).bin", "pal8(b).bin", "pal8(c).bin"}); }
}