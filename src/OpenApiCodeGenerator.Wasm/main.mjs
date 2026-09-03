// Entry module for the WebAssembly runtime. The docs playground imports
// `dotnet.js` directly and drives generation through the exported
// `OpenApiCodeGenerator.Wasm.WasmInterop.Generate` method, so this module
// only needs to keep the runtime alive.
import createDotnetRuntime from './dotnet.js';

await createDotnetRuntime();
