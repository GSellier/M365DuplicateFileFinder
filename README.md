# M365DuplicateFileFinder

A .NET library to identify duplicate files stored in Microsoft 365.

Currently, the code only works for a user's own OneDrive files. SharePoint support will be added later.

[M365DuplicateFileFinder](M365DuplicateFileFinder/) is the folder containing the main project.  
[M365DuplicateFileFinder.Console](M365DuplicateFileFinder.Console/) contains a project for a simple console application showing how to use the library.
[M365DuplicateFileFinder.PowerShell-Windows](M365DuplicateFileFinder.PowerShell-Windows/) contains a script that uses the library to produce a CSV file, and an Excel workbook that can help understand the results.