using System.Runtime.InteropServices;
using System.Text;

namespace Capri.Core;

/// <summary>Windows DPAPI per-user encrypted provider keys. Never serialize keys into settings.json.</summary>
public sealed class SecretStore
{
    private readonly string _directory;
    private const uint UiForbidden = 0x1;

    [StructLayout(LayoutKind.Sequential)]
    private struct DataBlob
    {
        public int Size;
        public IntPtr Data;
    }

    [DllImport("crypt32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CryptProtectData(ref DataBlob input, string? description,
        IntPtr optionalEntropy, IntPtr reserved, IntPtr prompt, uint flags, out DataBlob output);

    [DllImport("crypt32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CryptUnprotectData(ref DataBlob input, IntPtr description,
        IntPtr optionalEntropy, IntPtr reserved, IntPtr prompt, uint flags, out DataBlob output);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr LocalFree(IntPtr memory);

    public SecretStore(string? root = null)
    {
        _directory = Path.Combine(root ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CapriDesktop"),
            "credentials");
        Directory.CreateDirectory(_directory);
    }

    private static string CredentialId(string provider) =>
        string.Equals(provider, "fish", StringComparison.OrdinalIgnoreCase)
            ? "fish" :
        string.Equals(provider, "spotify", StringComparison.OrdinalIgnoreCase)
            ? "spotify" :
        string.Equals(provider, "roblox", StringComparison.OrdinalIgnoreCase)
            ? "roblox" :
        string.Equals(provider, "obs", StringComparison.OrdinalIgnoreCase)
            ? "obs" : ProviderCatalog.Get(provider).Id;

    private string FileFor(string provider) =>
        Path.Combine(_directory, CredentialId(provider) + ".dpapi");

    public void Save(string provider, string key)
    {
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("DPAPI requires Windows.");
        if (string.IsNullOrWhiteSpace(key) || key.Length > 4096)
            throw new ArgumentException("A valid API key is required.", nameof(key));

        var plain = Encoding.UTF8.GetBytes(key.Trim());
        try
        {
            var sealedBytes = Transform(plain, protect: true);
            var path = FileFor(provider);
            var temporary = path + ".tmp";
            File.WriteAllBytes(temporary, sealedBytes);
            File.Move(temporary, path, true);
        }
        finally
        {
            Array.Clear(plain);
        }
    }

    public string? Read(string provider)
    {
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("DPAPI requires Windows.");
        var path = FileFor(provider);
        if (!File.Exists(path)) return null;
        var decrypted = Transform(File.ReadAllBytes(path), protect: false);
        try { return Encoding.UTF8.GetString(decrypted); }
        finally { Array.Clear(decrypted); }
    }

    public bool HasKey(string provider) => File.Exists(FileFor(provider));

    public void Delete(string provider)
    {
        var path = FileFor(provider);
        if (File.Exists(path)) File.Delete(path);
    }

    private static byte[] Transform(byte[] data, bool protect)
    {
        var input = new DataBlob { Size = data.Length, Data = Marshal.AllocHGlobal(data.Length) };
        DataBlob output = default;
        try
        {
            Marshal.Copy(data, 0, input.Data, data.Length);
            var ok = protect
                ? CryptProtectData(ref input, "Capri Desktop API key", IntPtr.Zero, IntPtr.Zero,
                    IntPtr.Zero, UiForbidden, out output)
                : CryptUnprotectData(ref input, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero,
                    IntPtr.Zero, UiForbidden, out output);
            if (!ok) throw new InvalidOperationException(
                "Windows could not " + (protect ? "encrypt" : "decrypt") + " the API key.");
            var result = new byte[output.Size];
            Marshal.Copy(output.Data, result, 0, output.Size);
            return result;
        }
        finally
        {
            if (input.Data != IntPtr.Zero)
            {
                for (var i = 0; i < data.Length; i++) Marshal.WriteByte(input.Data, i, 0);
                Marshal.FreeHGlobal(input.Data);
            }
            if (output.Data != IntPtr.Zero) LocalFree(output.Data);
        }
    }
}
