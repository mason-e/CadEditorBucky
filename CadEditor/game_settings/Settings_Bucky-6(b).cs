using CadEditor;
using System;
//css_include bucky_ohare/BuckyUtils.cs;

public class Data 
{ 
  public OffsetRec getScreensOffset()  { return new OffsetRec(0xb65f, 1 , 8*46, 8, 46);   }
  
  public OffsetRec getVideoOffset()     { return new OffsetRec(0x0 , 1   , 0x1000);  }
  public OffsetRec getPalOffset  ()     { return new OffsetRec(0x0 , 1   , 16); }
  public GetVideoChunkFunc    getVideoChunkFunc()    { return BuckyUtils.getVideoChunk(new[] {"chr6(b).bin"}); }
  public SetVideoChunkFunc    setVideoChunkFunc()    { return null; }
  
  public OffsetRec getBlocksOffset()    { return new OffsetRec(0xadaf, 1  , 0x1000);  }
  public int getBlocksCount()           { return 244; }
  public int getBigBlocksCount()        { return 244; }
  public int getPalBytesAddr()          { return 0xb4cf; }
  
  public GetPalFunc           getPalFunc()           { return BuckyUtils.readPalFromBin(new[] {"pal6(b).bin"}); }
}