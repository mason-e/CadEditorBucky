using CadEditor;
using System.Collections.Generic;
public class Data
{ 
  public OffsetRec getScreensOffset()     { return new OffsetRec(0xB047, 8, 64, 8, 8);   } 
  public string getBlocksFilename()       { return "banana_prince_5a.png"; }
  
  public bool isBigBlockEditorEnabled() { return false; }
  public bool isBlockEditorEnabled()    { return false; }
  public bool isEnemyEditorEnabled()    { return false; }
}