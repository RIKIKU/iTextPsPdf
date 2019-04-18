Param(
	$ModulePath,
	$Path
)
$ModulePath
$Path = "$($Path)iTextPsPdf.psd1"

$ManiParams = @{
	Path = $Path
	RootModule = 'iTextPsPdf.dll'
	Author = 'Kyle Smith' 
	Copyright = '(c)Kyle Smith 2019 . All rights reserved.' 
	ModuleVersion = "0.0.2"
	Description = 'Create PDF Files in powershell' 
	PowerShellVersion = '5.1' 
	DotNetFrameworkVersion = '4.5' 
	CmdletsToExport = '*' 
	VariablesToExport = '*' 
	Tags = 'PDF' 
	ProjectUri = 'https://github.com/users/RIKIKU/projects/1' 
	LicenseUri = 'https://github.com/RIKIKU/iTextPsPdf/blob/master/LICENSE'

	ReleaseNotes = 'Only has Export-PDF at this time.'
}
Write-Output $ManiParams

New-ModuleManifest @ManiParams 
New-ModuleManifest 

Import-Module .\iTextPsPdf.dll
$thing = Get-ChildItem
$thing = $thing | select Mode,LastWriteTime,Length,Name
Export-PDFTable -Path C:\Users\kyles\Source\Repos\iTextPsPdf\iTextPs\bin\Debug\test.pdf -FlipOrientation -PageSize A3 -Input $thing -Force