# IcarusSaveConverter

A command line program that converts Icarus prospect save files to and from human editable json files.

## Releases

Releases can be found [here](https://github.com/CrystalFerrai/IcarusSaveConverter/releases). There is no installer, just unzip the contents to a location on your hard drive.

You will need to have the .NET Runtime 8.0 x64 installed. You can find the latest .NET 8 downloads [here](https://dotnet.microsoft.com/en-us/download/dotnet/8.0). Look for ".NET Runtime" or ".NET Desktop Runtime" (which includes .NET Runtime). Download and install the x64 version for your OS.

## How to Use

Prerequisite: You should have some familiarity with using command line programs or you may struggle to run this. You will need to pass various command line arguments to the program to tell it what you want to do.

**BACKUP YOUR SAVE FILES BEFORE USING THIS PROGRAM.** If something goes wrong, there is no way to recover your save unless you have a backup.

### Step 1: Unpack a prospect save file into a directory of parts

This will output a directory full of json files containing all of the prospect save data converted into an editable format.
```
IcarusSaveConverter unpack path\to\prospectfile.json path\to\outputfolder
```

### Step 2: Make changes to the unpacked files

Using a text editor, make any changes you want to any of the unpacked files. Be careful that you do not break the json formatting of the files.

### Step 3: Convert the unpacked files back into a prospect save file

This will replace the specified save file with the modified one. Make sure you backed up the original first!
```
IcarusSaveConverter pack path\to\prospectfile.json path\to\outputfolder
```

### More options

To see the full list of options, run the program in a command window with no parameters. Here is what currently prints at the time of writing this:
```
Usage: IcarusSaveConverter [action] [prospect] [parts]

  action    The action to perform. Must be one of the following.
            unpack: Unpack and convert the prospect file to text.
            pack: Convert an unpacked prospect back into a prospect file.

  prospect  The path to a prospect file to either read or create depending
            on the specified action.

  parts     The path to a directory of unpacked prospect parts that will
            either be created or read depending on the specified action.
```

## How to Build

If you want to build, from source, follow these steps.
1. Clone the repo, including submodules.
    ```
    git clone --recursive https://github.com/CrystalFerrai/IcarusSaveConverter.git
    ```
2. Open the file `IcarusSaveConverter.sln` in Visual Studio.
3. Right click the solution in the Solution Explorer panel and select "Restore NuGet Dependencies".
4. Build the solution.

## Support

This is just one of my many free time projects. No support or documentation is offered beyond this readme. If you find a bug in the program, you can [submit as issue on Github](https://github.com/CrystalFerrai/IcarusSaveConverter/issues), but I make no promises about when or if I will address issues.
