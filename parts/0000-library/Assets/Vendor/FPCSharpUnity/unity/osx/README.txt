`fp_csharp_unity_macos.bundle` is built from source folder `repo_root/macos_plugin`.

If you change anything in that source folder you need to fire up XCode (which only runs on Mac) and rebuild
the bundle.

You can build the project in XCode by selecting "Product > Build" in the menu bar. After the build do 
"Product > Show Build Folder in Finder" and copy the resulting bundle from "Products/Debug" to this folder.

If you get the "Can't sign the bundle" error, make sure you have your Apple ID added to XCode 
(XCode > Preferences > Accounts) and then select your personal development team in 
`Project Settings > Build Settings > Signing > Development Team`.