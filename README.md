# Extra Builder Package
A package for quickly building and deploying projects to itch.io

# Requirements
- Buler
Requres for butler to be installed, and added to PATH.
For more information on butler check https://itch.io/docs/butler/

- csc.rsp
Requires a file named csc.rsp to be present in the Assets folder.
it should contain the following line (without quotes):
"-r:System.IO.Compression.FileSystem.dll"
For more information on csc.rsp file check https://docs.unity3d.com/Manual/dotnetProfileAssemblies.html