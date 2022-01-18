Functional Programming with C# in Unity Game Engine Library
============================

Bunch of tools to enable functional programming mostly for Unity games. The `core` part of
the library can be used without Unity, though a lot of the design of things is made considering the
usage in Unity game engine.

Requirements
------------

This library requires at least Unity 2020. It brings its own compiler that can be found at https://github.com/FPCSharpUnity/Unity3D.IncrementalCompiler .

Because we need the new compiler this only works where Mono VM or il2cpp is used. It does not support Mono AOT.

Design Considerations
---------------------

[AOT compilation restrictions](https://docs.unity3d.com/Manual/ScriptingRestrictions.html#AOT) were taken in mind when designing this library.

Disclaimer
----------

You are free to use this for your own games. Patches and improvements are welcome. A mention in your game credits would be nice.

If you are considering using this it would be also nice if you contacted me (Artūras Šlajus, x11+fpcsharpunity@arturaz.net) so we could create a community around this.