using CadEditor;
using System;
//css_include bucky_ohare/BuckyUtils.cs;

public class Data 
{ 
  public OffsetRec getScreensOffset()  { return new OffsetRec(0xcd78, 1 , 8*22, 8, 22);   }
  
  public OffsetRec getVideoOffset()     { return new OffsetRec(0x0 , 1   , 0x1000);  }
  public OffsetRec getPalOffset  ()     { return new OffsetRec(0x0 , 1   , 16); }
  public GetVideoPageAddrFunc getVideoPageAddrFunc() { return BuckyUtils.fakeVideoAddr(); }
  public GetVideoChunkFunc    getVideoChunkFunc()    { return BuckyUtils.getVideoChunk(new[] {"chr4(a).bin"}); }
  public SetVideoChunkFunc    setVideoChunkFunc()    { return null; }
  
  public OffsetRec getBlocksOffset()    { return new OffsetRec(0xc011, 1  , 0x1000);  }
  public int getBlocksCount()           { return 244; }
  public int getBigBlocksCount()        { return 244; }
  public int getPalBytesAddr()          { return 0xca21; }
  
  public GetPalFunc           getPalFunc()           { return BuckyUtils.readPalFromBin(new[] {"pal4(b).bin"}); }
}