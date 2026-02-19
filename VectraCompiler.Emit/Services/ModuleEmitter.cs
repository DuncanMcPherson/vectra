using VectraCompiler.Bind.Models.Symbols;
using VectraCompiler.Emit.Models;
using VectraCompiler.Lower.Models;
using VectraCompiler.Package.Models;

namespace VectraCompiler.Emit.Services;

public sealed class ModuleEmitter
{
    private static readonly byte[] VbcMagic = "VBC"u8.ToArray();
    private static readonly byte[] VdlMagic = "VDL"u8.ToArray();
    private static readonly byte[] VdiMagic = "VDI"u8.ToArray();
    private static readonly byte[] VdsMagic = "VDS"u8.ToArray();
    private static readonly byte[] Version = [0x01, 0x00];

    private readonly LoweredModule _module;
    private readonly ConstantPool _pool;

    // ReSharper disable once ConvertToPrimaryConstructor
    public ModuleEmitter(LoweredModule module)
    {
        _module = module;
        _pool = new ConstantPool();
    }

    public async Task EmitAsync(string outputDir, CancellationToken ct = default)
    {
        PrePopulatePool();
        var emittedBodies = new Dictionary<CallableSymbol, IReadOnlyList<byte>>();
        foreach (var (callable, body) in _module.LoweredBodies)
        {
            ct.ThrowIfCancellationRequested();
            var bodyEmitter = new MethodBodyEmitter(callable, _pool);
            emittedBodies[callable] = bodyEmitter.Emit(body);
        }

        var moduleDir = Path.Combine(outputDir);
        Directory.CreateDirectory(moduleDir);

        switch (_module.ModuleType)
        {
            case ModuleType.Executable:
                await WriteVbcAsync(moduleDir, emittedBodies, ct);
                break;
            case ModuleType.Library:
                await WriteVdlAsync(moduleDir, emittedBodies, ct);
                await WriteVdiAsync(moduleDir, ct);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        await WriteVdsAsync(moduleDir, ct);
    }

    private void PrePopulatePool()
    {
        foreach (var type in _module.Types)
        {
            _pool.AddType(type);
            if (_module.LoweredBodies.Keys.All(c => c.SourceFilePath != type.SourceFilePath))
                continue;

            var memberScope = _module.LoweredBodies.Keys
                .OfType<MethodSymbol>()
                .Where(m => m.SourceFilePath == type.SourceFilePath);
            foreach (var method in memberScope)
                _pool.AddMethod(method);
        }
    }

    private async Task WriteVbcAsync(
        string outputDir,
        Dictionary<CallableSymbol, IReadOnlyList<byte>> bodies,
        CancellationToken ct)
    {
        var path = Path.Combine(outputDir, $"{_module.ModuleName}.vbc");
        await using var stream = File.Create(path);
        await using var writer = new BinaryWriter(stream);

        WriteFileHeader(writer, VbcMagic);
        ct.ThrowIfCancellationRequested();
        WriteImportsTable(writer);
        ct.ThrowIfCancellationRequested();
        WriteConstantPool(writer);
        ct.ThrowIfCancellationRequested();
        WriteTypeDefinitions(writer);
        ct.ThrowIfCancellationRequested();
        WriteMethodBodies(writer, bodies);
    }

    private async Task WriteVdlAsync(
        string outputDir,
        Dictionary<CallableSymbol, IReadOnlyList<byte>> bodies,
        CancellationToken ct)
    {
        var path = Path.Combine(outputDir, $"{_module.ModuleName}.vdl");
        await using var stream = File.Create(path);
        await using var writer = new BinaryWriter(stream);

        WriteFileHeader(writer, VdlMagic);
        ct.ThrowIfCancellationRequested();
        WriteImportsTable(writer);
        ct.ThrowIfCancellationRequested();
        WriteConstantPool(writer);
        ct.ThrowIfCancellationRequested();
        WriteTypeDefinitions(writer);
        ct.ThrowIfCancellationRequested();
        WriteMethodBodies(writer, bodies);
    }

    private async Task WriteVdiAsync(string outputDir, CancellationToken ct)
    {
        var path = Path.Combine(outputDir, $"{_module.ModuleName}.vdi");
        await using var stream = File.Create(path);
        await using var writer = new BinaryWriter(stream);

        WriteFileHeader(writer, VdiMagic);
        ct.ThrowIfCancellationRequested();

        // VDI only contains public type and member definitions — no bodies
        writer.Write((ushort)_module.Types.Count);
        foreach (var type in _module.Types)
        {
            WriteString(writer, type.FullName);

            // Write public members (fields, properties, methods)
            var memberScope = _module.LoweredBodies.Keys
                .OfType<MethodSymbol>()
                .Where(m => m.SourceFilePath == type.SourceFilePath)
                .ToList();

            writer.Write((ushort)memberScope.Count);
            foreach (var method in memberScope)
            {
                ct.ThrowIfCancellationRequested();
                WriteString(writer, method.Name);
                WriteString(writer, method.ReturnType.Name);
                writer.Write((ushort)method.Parameters.Count);
                foreach (var param in method.Parameters)
                {
                    ct.ThrowIfCancellationRequested();
                    WriteString(writer, param.Name);
                    WriteString(writer, param.Type.Name);
                }
            }
        }
    }

    private async Task WriteVdsAsync(string outputDir, CancellationToken ct)
    {
        var path = Path.Combine(outputDir, $"{_module.ModuleName}.vds");
        await using var stream = File.Create(path);
        await using var writer = new BinaryWriter(stream);

        WriteFileHeader(writer, VdsMagic);
        ct.ThrowIfCancellationRequested();

        // Emit source file paths and symbol span info for debugger use
        var callables = _module.LoweredBodies.Keys.ToList();
        writer.Write((ushort)callables.Count);
        foreach (var callable in callables)
        {
            ct.ThrowIfCancellationRequested();
            WriteString(writer, callable.Name);
            WriteString(writer, callable.SourceFilePath ?? string.Empty);

            if (callable.DeclarationSpan is { } span)
            {
                writer.Write(span.StartLine);
                writer.Write(span.StartColumn);
                writer.Write(span.EndLine);
                writer.Write(span.EndColumn);
            }
            else
            {
                writer.Write(0);
                writer.Write(0);
            }

            // Write parameter debug info
            writer.Write((ushort)callable.Parameters.Count);
            foreach (var param in callable.Parameters)
            {
                ct.ThrowIfCancellationRequested();
                WriteString(writer, param.Name);
                writer.Write((ushort)param.SlotIndex);
                if (param.DeclarationSpan is { } paramSpan)
                {
                    writer.Write(paramSpan.StartLine);
                    writer.Write(paramSpan.StartColumn);
                    writer.Write(paramSpan.EndLine);
                    writer.Write(paramSpan.EndColumn);
                }
                else
                {
                    writer.Write(0);
                    writer.Write(0);
                }
            }
        }
    }

    private void WriteFileHeader(BinaryWriter writer, byte[] magic)
    {
        writer.Write(magic);
        writer.Write(Version);
    }

    private void WriteImportsTable(BinaryWriter writer)
    {
        var importedModules = _module.References;

        writer.Write((ushort)importedModules.Count);
        foreach (var import in importedModules)
            WriteString(writer, import);
    }
    
    private void WriteConstantPool(BinaryWriter writer)
    {
        writer.Write((ushort)_pool.Entries.Count);
        foreach (var entry in _pool.Entries)
        {
            writer.Write((byte)entry.Kind);
            switch (entry.Kind)
            {
                case ConstantKind.Number:
                    writer.Write(entry.NumericValue!.Value);
                    break;
                default:
                    WriteString(writer, entry.Name);
                    break;
            }
        }
    }

    private void WriteTypeDefinitions(BinaryWriter writer)
    {
        writer.Write((ushort)_module.Types.Count);
        foreach (var type in _module.Types)
        {
            var typeIndex = _pool.AddType(type);
            writer.Write(typeIndex);

            var fields = _module.LoweredBodies.Keys
                .OfType<MethodSymbol>()
                .Where(m => m.SourceFilePath == type.SourceFilePath)
                .ToList();

            // Write method count and their pool indices
            writer.Write((ushort)fields.Count);
            foreach (var method in fields)
            {
                var methodIndex = _pool.AddMethod(method);
                writer.Write(methodIndex);
                writer.Write((ushort)method.Parameters.Count);
            }
        }
    }
    
    private void WriteMethodBodies(
        BinaryWriter writer,
        Dictionary<CallableSymbol, IReadOnlyList<byte>> bodies)
    {
        writer.Write((ushort)bodies.Count);
        foreach (var (callable, bodyBytes) in bodies)
        {
            // Write which pool entry this body belongs to
            ushort callableIndex = callable switch
            {
                ConstructorSymbol ctor => _pool.AddConstructor(ctor),
                MethodSymbol method    => _pool.AddMethod(method),
                _                     => throw new InvalidOperationException(
                    $"Unexpected callable type: {callable.GetType().Name}")
            };

            writer.Write(callableIndex);
            writer.Write((ushort)callable.Parameters.Count); // local slot count hint
            writer.Write((ushort)bodyBytes.Count);
            foreach (var b in bodyBytes)
                writer.Write(b);
        }
    }

    private static void WriteString(BinaryWriter writer, string value)
    {
        // Length-prefixed UTF8 string
        var bytes = System.Text.Encoding.UTF8.GetBytes(value);
        writer.Write((ushort)bytes.Length);
        writer.Write(bytes);
    }
}