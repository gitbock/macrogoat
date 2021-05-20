$certFilePath = "C:\macrogoat"
$certFileName = "macrogoat_demo_cert.pfx"
$certStoreLocation = "Cert:\CurrentUser\My"
$pwd = "Test123"

$cert = New-SelfSignedCertificate `
    -KeyFriendlyName "MacroGoat Demo Cert" `
    -KeyDescription "Demo Certificate for signing Macro enabled office files" `
    -KeyAlgorithm "RSA" `
    -DnsName macrogoat@macrogoat.test `
    -NotBefore (Get-Date).AddYears(-1) `
    -NotAfter (Get-Date).AddYears(50) `
    -CertStoreLocation $certStoreLocation `
    -KeyExportPolicy Exportable `
    -KeyProtection None `
    -Type CodeSigning `
    -Subject "MacroGoat Demo Cert"



$cert | Export-PfxCertificate -FilePath "$certFilePath\$certFileName" -Password (ConvertTo-SecureString -String $pwd -AsPlainText -Force)