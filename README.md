# CadEditorBucky

Fork of https://github.com/spiiin/CadEditor (obviously). Shout out to spiiin and all other original contributors!

## Objectives

### Main

To create a level editor that is tailored specifically to NES game Bucky O'Hare for my own hacking purposes.

### Detailed

- Source all data directly from the ROM. The way it works now is using premade bin files based on the original ROM, so if you make any changes that you actually want to see in the editor, you have to recreate the bins.
- Some levels do not load correctly; fix these if possible
- Strip down the code to essentials to make it easier for my personal understanding
- Make it function with newer .NET framework, Visual Studio version, etc.
- Stretch goal - modernize and clarify some aspects of the user experience

## Usage

### Building and Running

Currently I am not publishing any releases and building it myself. I build the CadEditor.sln solution to get all dependent binaries built. Then it should run from the .exe, or directly in VS debug mode.

### Using the Editor

Currently functions the same as the original editor, minus the configurations for other games. I will update this section if I change how it functions.