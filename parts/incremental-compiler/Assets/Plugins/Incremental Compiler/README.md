## `Macros.dll`

*Validate References* is turned off because `Macros.dll` uses `JetBrains.Annotations`
internally. However we do not want to enforce a particular version of annotations DLL
and because the annotations are only used by IDE, things work even with the wrong version.