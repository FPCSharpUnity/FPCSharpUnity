// Empty cs file required for `compiler-extensions.asmdef` to work.
// `compiler-extensions.asmdef` contains references to Roslyn analyzers.
// Those analysers will be used by all assemblies that reference `compiler-extensions.asmdef`.
// If we remove compiler-extensions.asmdef, then analysers will be used by all packages and we don't want that.