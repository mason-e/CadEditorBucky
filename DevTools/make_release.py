#from __future__ import print_function
import os, glob, shutil, zipfile

VERSION        = "53"
RELEASE_FOLDER = "../Release/cad_editor_v%s" % VERSION
ZIP_NAME       = "../Release/cad_editor_v%s.zip" % VERSION

COPY_FILE_LIST = [
  "CadEditor.exe",
  "Be.Windows.Forms.HexBox.dll",
  "CSScriptLibrary.dll",
  "NDesk.Options.dll",
  "Newtonsoft.Json.dll",
  "lzkn1.dll",
  
  "Config.cs",
  "readme.txt",
  "changelog.txt",
  "cad_editor_supported_games.txt",
  "cad_editor_structures.txt",
  
  "Plugin*.dll",
]

COPY_FOLDER_LIST = [
  "Scripts/",
  "obj_sprites/",
  "scroll_sprites/",
  "shared_settings/",
  "settings_nes/",
  "bt_objects/", #copied from PluginBattetoadsRaceEditor
]

def removeAndCreate(path):
  if os.path.exists(path):
    print("Release folder already exists. Removing...")
    shutil.rmtree(path)
  print("Create release folder...")
  os.makedirs(path)
  os.makedirs(os.path.join(path, "exportTmx/"))
  
def zipdir(path, ziph):
    # ziph is zipfile handle
    absPath = os.path.abspath(path)
    for root, dirs, files in os.walk(path):
        for file in files:
            fullPath = os.path.join(root, file)
            shortPath = fullPath.replace(absPath, "")
            ziph.write(fullPath, arcname =shortPath)

def makeRelease():
  print("Make release CadEditor v" + VERSION)
  absRoot = os.path.abspath(RELEASE_FOLDER)
  print("Release folder: ", absRoot)
  removeAndCreate(absRoot)
  
  os.chdir("../CadEditor")
  
  print("Copying files:")
  for fn in COPY_FILE_LIST:
    if fn.find("*")==-1:
      print("  copy ", fn)
      shutil.copy(fn, os.path.join(absRoot, fn))
    else:
      fileList2 = glob.glob(fn)
      for fn2 in fileList2:
        print("  copy ", fn2)
        shutil.copy(fn2, absRoot)
  print("Copying folders:")
  for dir in COPY_FOLDER_LIST:
    if dir.find("*") == -1:
      print("  copy ", dir)
      shutil.copytree(dir, os.path.join(absRoot, dir))
    else:
      dirList2 = glob.glob(dir)
      for dir2 in dirList2:
        print("  copy ", dir2)
        shutil.copytree(dir2, os.path.join(absRoot, dir2))
  print("")
  
  print("Making release archive")
  zipf = zipfile.ZipFile(ZIP_NAME, 'w', zipfile.ZIP_DEFLATED)
  zipdir(absRoot, zipf)
  zipf.close()
  print("DONE!")
  print("Release folder: %s"% RELEASE_FOLDER)
  print("Release zip   : %s"% ZIP_NAME)
    
#-----------------------------------------------------------------------
if __name__ == "__main__":
  makeRelease()
      
  