#import <Foundation/Foundation.h>

char* FPCSharpUnityNsStringToUnity(const NSString* str);

extern "C"
{
    unsigned long FPCSharpUnityGetCliArgumentCount() {
        auto args = [[NSProcessInfo processInfo] arguments];
        if (args == NULL) return 0;
        return args.count;
    }

    char* FPCSharpUnityGetCliArgument(unsigned long index) {
        auto arr = [[NSProcessInfo processInfo] arguments];
        return FPCSharpUnityNsStringToUnity(arr[index]);
    }
}

char* FPCSharpUnityNsStringToUnity(const NSString* str) {
    if (str == NULL) return NULL;
    
    // Return a null-terminated UTF8 representation of the string.
    auto pointer = [str UTF8String];
    auto unityStr = (char*) malloc(strlen(pointer) + 1);
    strcpy(unityStr, pointer);
    return unityStr;
}
