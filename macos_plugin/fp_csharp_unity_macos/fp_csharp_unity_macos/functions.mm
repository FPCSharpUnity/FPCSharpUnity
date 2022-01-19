#import <Foundation/Foundation.h>
#import <AppKit/NSApplication.h>
#import <AppKit/AppKit.h>

extern "C"
{
    void FPCSharpUnityMacOSWindowSetTitle(const char* title) {
        NSApp.mainWindow.title = [NSString stringWithUTF8String:title];
    }
}
