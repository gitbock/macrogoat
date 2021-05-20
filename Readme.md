# MacroGoat

## What is it
MacroGoat is a dotnet core web application which let you sign your macro-enabled office files. 

## Why MacroGoat was created
This is a MVP (Minimum viable product / Beta Version) which I engineered, because I wanted to learn dotnet core. The basic idea is that an enterprise can host such 
a web application for its users. In the enterprise only **signed** office files with macros are allowed. Macros within non-signed office are not executed. 
 ![Macro disabled office](/img/office_macro_only_signed_allowed.png)


The idea is as follows:
1. The user tries to open a macro enabled office file. It is blocked, because it's not signed by a trusted authority.
2. The user can go to a self service website (MacroGoat) web application and upload his files
3. The web app first scans the files if there is any malware inside
4. If the file is clean, it's automatically signed with a certificate the enterprise trusts (included in the certificate store)
5. Finally the user can open the file and macros are executed, because the file is signed now



## Functions
MacroGoat provides three funtions
1. Verify Signature  
You may upload an office file to check the signature. If it's signed already, informatin about the certificate is shown.
![Verify](/img/verify_demo.png)

2. AdHoc Signer  
AdHoc Signer can be used without any authentication. Using AdHoc Signer you can upload an office file and a cert file containing the private key.
You can choose to analyse your office file with VirusTotal first. If the file is clean, the office file is signed by the uploaded certificate. 
![AdHoc1](/img/adhoc_demo1.png)
![AdHoc2](/img/adhoc_demo2.png)

3. Signing using preconfigured certificate  
After authentication, a user can upload office file. The office file is analysed and signed with the admin provided certificate. No need to upload a certificate.
![Signing1](/img/signing1.png)
![Signing2](/img/signing2.png)


You can configure the GUI part for the API usage  
![Settings1](/img/settings1.png)
![Settings2](/img/settings2.png)


## Architecture
MacroGoat consists of two applications

1. Gui  
This is a plain dotnet application which provides a nice GUI for the user to upload his files. The Gui basically provides a websites which makes use of the SignerApi (see 2.). The 
application provides a login mechanism to make use of the admin-provided profile. Using this profile, the user can upload his office files without the need of a certificate. The
ceritificate which is needed to sign the office file is provided in a profile the administrator already pre-configured for his users. The Gui part is excahngeable with any other 
web application. It basically calls the SignerAPI (see 2.)

2. SignerApi  
This is the core which analyses uploaded office files by VirusTotal and signs office files by a certificate either provided directly (AdHoc Signer) or by admin pre-configured files.
Instead of VirusTotal you may also think of an analyser service like cuckoo sandbox running in the enterprise's private stack in the futire.

![Architecture](/img/MacroGoat_Architecture.png)


## Security
The app is meant to run inside a trusted network. I implemented a few security measures
- CORS
- Rate Limiter for the API
- Encrypted storage of certificate PWs
- File Extension Check of uploaded files (by plain extension and by magic byte)

The app is still lacking
- API authentication


## Deployment
Because the SignerApi makes use of the signtool.exe of Microsoft as a wrapper, you need a backend which is capable of executing .exe files. By default, this means you need a 
windows backend which can run the signtool.exe. In my test deployment both applications (GUI + SignerApi) are running on the same IIS 10 on Windows Server 2019 in MS Azure. The 
Gui Website on port 443, the SignerApi on Port 8443.


## I found a bug
Yes, this is absolutely possible. I am not a programmer genius, I only wanted to learn dotnet core making use of this project. You may file in an issue.



