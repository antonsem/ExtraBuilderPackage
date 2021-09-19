# Extra Builder Package
A package for quickly building and deploying projects to itch.io

# Requirements
### csc.rsp
Requires a file named csc.rsp to be present directly in the Assets folder in order to compile.
it should contain the following line (without quotes):

"-r:System.IO.Compression.FileSystem.dll"

For more information on csc.rsp file check https://docs.unity3d.com/Manual/dotnetProfileAssemblies.html

### Butler
Requires for butler to be installed, and added to PATH in order to execute PushToItch.bat file.
For more information on butler check https://itch.io/docs/butler/

# How to use
For the package to compile successfully the csc.rsp file needs to be created
as described in the Requirements part. Butler should be installed only to
push to Itch using the PushToItch.bat file.

Once the package is compile two scriptable objects can be created: 
BuildSettings and Builder. Both can be found under the Create/ExtraTools 
menu.

### BuildSettings
To make a build create a BuildSettings object. To push to itch use 
PushToItch.bat file as the batch file. The settings are as follows:

- Build Name (%buildName): Name of the executable file including the extension.
  - E.g. build.exe, build.app etc. WebGL build do not require an extension.
- Directory (%directory): Directory for the build. The executable file will go here.
- Build Group (%buildGroup): https://docs.unity3d.com/ScriptReference/BuildTargetGroup.html
- Build Target (%buildTarget): https://docs.unity3d.com/ScriptReference/BuildTarget.html
- Scenes (%scenes): Scenes to be included to the build. These scened don't have to be in Unity's build settings.
- Create Zip File (%createZipFile): Should the zip file be created?
- %zipFile: Absolute path for the zip file. Set to empty string if zipping skipped or failed.
- %buildDirectory: Directory containing the executable. This directory will be zipped.
- %report: Latest generated report for this build.
- Use Batch File (%useBatchFile): Should the batch file be executed?
- Batch File (%batchFile): Batch file to be executed. Visible only if the useBatchFile is set to true.
- Batch Arguments (%batchArguments): Argument list to be passed to the batch file.
  - First argument will be passed instead of %1 in the batch file, second instead of %2 etc.
  - To pass one of the fields as an argument to the batch file add % before the field name (as shown in parentheses).
- Help: Short explanation for the batch file and arguments.
- Available Arguments: List of fields which can be used as arguments.
- Compile/Recompile: Will show the batch file with argument inserted in.
- Keep Current Build Target (%keepCurrentBuildTarget): Should the build target switch back to what it was before the build started?
- Automatically Save Report (%automaticallySaveReport): Should the report with build results be saved automatically after the build?
- Build: Builds the project according to settings above.
- Save Report: Visible only if there is a report to be saved.

### Builder
At the top there is a list with builder settings. Individual settings can be
added and removed from the list. Clicking on the "Build" button next to a
settings object will make a single build using that object.

- Keep Current Build Target: Should the build target switch back to what it was before the build started?
- Automatically Save Report: Should the report with build results be saved automatically after the build?
- Build All: Builds everything using the settings in the list.
- Save Report: Visible only if there is a report to be saved.