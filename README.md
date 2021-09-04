# Extra Builder Package
A package for quickly building and deploying projects to itch.io

# Requirements
### csc.rsp
Requires a file named csc.rsp to be present directly in the Assets folder in order to compile.
it should contain the following line (without quotes):

"-r:System.IO.Compression.FileSystem.dll"

For more information on csc.rsp file check https://docs.unity3d.com/Manual/dotnetProfileAssemblies.html

### Butler
Requires for butler to be installed, and added to PATH.
For more information on butler check https://itch.io/docs/butler/
