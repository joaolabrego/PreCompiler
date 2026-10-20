using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace HarbourCompiler;

/// <summary>Pré-processador Harbour nativo. Não executa nem compila o texto produzido.</summary>
public sealed class ClipperPreCompiler : IDisposable
{
    private readonly object _sync = new();
    private readonly string? _includePath;
    private readonly NativeModule _module;
    private readonly ProcessFunction _process;
    private readonly FreeFunction _free;
    private readonly VersionFunction _version;
    private bool _disposed;

    public string LastDiagnostics { get; private set; } = "";

    public ClipperPreCompiler(string binDir, string? includePath = null)
    {
        _includePath = string.IsNullOrWhiteSpace(includePath)
            ? null
            : Path.GetFullPath(includePath);

        string libraryName = OperatingSystem.IsWindows()
            ? "HarbourPreprocessor.dll"
            : OperatingSystem.IsMacOS()
                ? "libHarbourPreprocessor.dylib"
                : "libHarbourPreprocessor.so";

        string libraryPath = Path.GetFullPath(Path.Combine(binDir, libraryName));
        _module = new NativeModule(NativeLibrary.Load(libraryPath));

        try
        {
            _process = Marshal.GetDelegateForFunctionPointer<ProcessFunction>(
                NativeLibrary.GetExport(_module.DangerousGetHandle(), "hbpp_process"));
            _free = Marshal.GetDelegateForFunctionPointer<FreeFunction>(
                NativeLibrary.GetExport(_module.DangerousGetHandle(), "hbpp_free_buffer"));
            _version = Marshal.GetDelegateForFunctionPointer<VersionFunction>(
                NativeLibrary.GetExport(_module.DangerousGetHandle(), "hbpp_version"));
        }
        catch
        {
            _module.Dispose();
            throw;
        }
    }

    public string Version
    {
        get
        {
            lock (_sync)
            {
                ObjectDisposedException.ThrowIf(_disposed, this);
                return Marshal.PtrToStringUTF8(_version()) ?? "";
            }
        }
    }

    public string Execute(string dslSourceCode, string rulesText)
    {
        ArgumentNullException.ThrowIfNull(dslSourceCode);
        ArgumentNullException.ThrowIfNull(rulesText);
        if (dslSourceCode.Contains('\0'))
            throw new ArgumentException("O código contém um caractere NUL.", nameof(dslSourceCode));
        if (rulesText.Contains('\0'))
            throw new ArgumentException("As regras contêm um caractere NUL.", nameof(rulesText));

        lock (_sync)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            byte[] source = Encoding.UTF8.GetBytes(dslSourceCode);
            byte[] rules = Encoding.UTF8.GetBytes(rulesText);
            IntPtr output = IntPtr.Zero;
            IntPtr diagnostics = IntPtr.Zero;

            try
            {
                int status = _process(
                    source, (nuint)source.Length,
                    rules, (nuint)rules.Length,
                    _includePath,
                    out output, out nuint outputLength,
                    out diagnostics, out nuint diagnosticsLength);

                LastDiagnostics = diagnostics == IntPtr.Zero
                    ? ""
                    : Marshal.PtrToStringUTF8(diagnostics, checked((int)diagnosticsLength)) ?? "";

                if (status != 0)
                    throw new InvalidOperationException(
                        string.IsNullOrWhiteSpace(LastDiagnostics)
                            ? $"Falha no pré-processador nativo (código {status})."
                            : LastDiagnostics.TrimEnd());

                return output == IntPtr.Zero
                    ? ""
                    : Marshal.PtrToStringUTF8(output, checked((int)outputLength)) ?? "";
            }
            finally
            {
                if (output != IntPtr.Zero)
                    _free(output);
                if (diagnostics != IntPtr.Zero)
                    _free(diagnostics);
            }
        }
    }

    public void Dispose()
    {
        lock (_sync)
        {
            if (_disposed)
                return;

            _disposed = true;
            _module.Dispose();
        }
    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int ProcessFunction(
        [In] byte[] source, nuint sourceLength,
        [In] byte[] rules, nuint rulesLength,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string? includePath,
        out IntPtr output, out nuint outputLength,
        out IntPtr diagnostics, out nuint diagnosticsLength);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void FreeFunction(IntPtr buffer);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate IntPtr VersionFunction();

    private sealed class NativeModule : SafeHandle
    {
        public NativeModule(IntPtr module) : base(IntPtr.Zero, ownsHandle: true)
        {
            SetHandle(module);
        }

        public override bool IsInvalid => handle == IntPtr.Zero;

        protected override bool ReleaseHandle()
        {
            NativeLibrary.Free(handle);
            return true;
        }
    }
}
