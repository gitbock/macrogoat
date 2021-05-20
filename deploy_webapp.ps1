$webapp_gui = "macrogoat"
$webapp_gui_local_dir = "d:\daten\entwicklung\macrogoat\macrogoat"
$webapp_gui_archivepath = "C:\macrogoat\$webapp_gui.zip"


$webapp_api = "macrogoat-api"
$webapp_api_local_dir = "d:\daten\entwicklung\macrogoat\signerapi"




$rg_name = "bsys-security"



# create zip file for deploy 
#Compress-Archive -Path $webapp_gui_local_dir -DestinationPath $webapp_gui_archivepath -Update
Connect-AzAccount
Publish-AzWebApp -Name $webapp_gui -ResourceGroupName $rg_name -ArchivePath $webapp_gui_archivepath


