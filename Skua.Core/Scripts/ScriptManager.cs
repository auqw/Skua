using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Skua.Core.Interfaces;
using Skua.Core.Interfaces.Services;
using Skua.Core.Messaging;
using Skua.Core.Models;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Skua.Core.Scripts;

public partial class ScriptManager : ObservableObject, IScriptManager, IDisposable
{
    private static readonly Regex _versionRegex = new(@"^/\*[\s\S]*?version:\s*(\d+\.\d+\.\d+\.\d+)[\s\S]*?\*/", RegexOptions.Multiline | RegexOptions.Compiled);
    private static readonly Regex _accessibilityRegex = new(@"\b(public\s+|internal\s+|private\s+)?class\s+\w+", RegexOptions.Multiline | RegexOptions.Compiled);
    private static readonly string _cacheScriptsDir = Path.Combine(ClientFileSources.SkuaScriptsDIR, "Cached-Scripts");
    public ScriptManager(
        ILogService logger,
        Lazy<IScriptInterface> scriptInterface,
        Lazy<IScriptHandlers> handlers,
        Lazy<IScriptSkill> skills,
        Lazy<IScriptDrop> drops,
        Lazy<IScriptWait> wait,
        Lazy<IAuraMonitorService> auraMonitorService)
    {
        _lazyBot = scriptInterface;
        _lazyHandlers = handlers;
        _lazySkills = skills;
        _lazyDrops = drops;
        _lazyWait = wait;
        _lazyAuraMonitor = auraMonitorService;
        _logger = logger;
    }

    private readonly Lazy<IScriptInterface> _lazyBot;
    private readonly Lazy<IScriptHandlers> _lazyHandlers;
    private readonly Lazy<IScriptSkill> _lazySkills;
    private readonly Lazy<IScriptDrop> _lazyDrops;
    private readonly Lazy<IScriptWait> _lazyWait;
    private readonly Lazy<IAuraMonitorService> _lazyAuraMonitor;
    private readonly ILogService _logger;

    private IScriptHandlers Handlers => _lazyHandlers.Value;
    private IScriptSkill Skills => _lazySkills.Value;
    private IScriptDrop Drops => _lazyDrops.Value;
    private IScriptWait Wait => _lazyWait.Value;
    private IAuraMonitorService AuraMonitor => _lazyAuraMonitor.Value;

    private Thread? _currentScriptThread;
    private readonly object _threadLock = new();
    private readonly object _stateLock = new();
    private bool _stoppedByScript;
    private bool _runScriptStoppingBool;
    private readonly object _configuredLock = new();
    private readonly Dictionary<string, bool> _configured = new();
    private readonly object _refCacheLock = new();
    private readonly List<string> _refCache = new();
    private readonly ReaderWriterLockSlim _includedFilesLock = new();
    private readonly List<string> _includedFiles = new();
    private ScriptLoadContext? _currentLoadContext;

    [ObservableProperty]
    private bool _scriptRunning = false;

    [ObservableProperty]
    private string _loadedScript = string.Empty;

    [ObservableProperty]
    private string _compiledScript = string.Empty;

    public IScriptOptionContainer? Config { get; set; }

    public CancellationTokenSource? ScriptCts { get; private set; }

    public bool ShouldExit => ScriptCts?.IsCancellationRequested ?? false;

    public async Task<Exception?> StartScript()
    {
        lock (_threadLock)
        {
            if (ScriptRunning)
            {
                _logger.ScriptLog("Script already running.");
                return new Exception("Script already running.");
            }
            ScriptRunning = true;
        }

        try
        {
            await _lazyBot.Value.Auto.StopAsync();

            UnloadPreviousScript();

            string scriptContent = File.ReadAllText(LoadedScript);
            object? script = await Task.Run(() => Compile(scriptContent));

            LoadScriptConfig(script);

            // CoreBots scripts with Skip Options off show this window themselves at start.
            bool scriptShowsOptions = Config!.Options.Exists(o => o.Name == "SkipOption") && !Config.Get<bool>("SkipOption");
            if (Config.ResetOptions.Count > 0 && !scriptShowsOptions)
            {
                await Task.Run(Config.Configure);
                lock (_configuredLock)
                {
                    _configured[Config.Storage] = true;
                }
            }

            bool needsConfig;
            lock (_configuredLock)
            {
                needsConfig = _configured.TryGetValue(Config!.Storage, out bool b) && !b;
            }

            ManualResetEventSlim scriptReady = new(false);

            Handlers.Clear();

            lock (_stateLock)
            {
                _runScriptStoppingBool = false;
            }

            Thread scriptThread = new(() =>
            {
                Exception? exception = null;
                ScriptCts = new();
                scriptReady.Set();

                try
                {
                    script?.GetType().GetMethod("ScriptMain")?.Invoke(script, new object[] { _lazyBot.Value });
                }
                catch (Exception e)
                {
                    Exception actualException = e is TargetInvocationException && e.InnerException != null ? e.InnerException : e;

                    bool stoppedByScript;
                    lock (_stateLock)
                    {
                        stoppedByScript = _stoppedByScript;
                    }

                    if ((actualException is not OperationCanceledException || !stoppedByScript) && (e is not TargetInvocationException || !stoppedByScript))
                    {
                        exception = e;
                        Trace.WriteLine($"Error while running script:\r\nMessage: {(e.InnerException is not null ? e.InnerException.Message : e.Message)}\r\nStackTrace: {(e.InnerException is not null ? e.InnerException.StackTrace : e.StackTrace)}");

                        StrongReferenceMessenger.Default.Send<ScriptErrorMessage, int>(new(e), (int)MessageChannels.ScriptStatus);

                        lock (_stateLock)
                        {
                            _runScriptStoppingBool = true;
                        }
                    }
                }
                finally
                {
                    bool shouldSendStoppingMessage;
                    lock (_stateLock)
                    {
                        _stoppedByScript = false;
                        shouldSendStoppingMessage = _runScriptStoppingBool;
                    }

                    if (shouldSendStoppingMessage)
                    {
                        StrongReferenceMessenger.Default.Send<ScriptStoppingMessage, int>((int)MessageChannels.ScriptStatus);
                        try
                        {
                            Task<bool?> messageTask = Task.Run(async () => await StrongReferenceMessenger.Default.Send<ScriptStoppingRequestMessage, int>(new(exception), (int)MessageChannels.ScriptStatus));
                            if (messageTask.Wait(TimeSpan.FromSeconds(2)))
                            {
                                switch (messageTask.Result)
                                {
                                    case true:
                                        Trace.WriteLine("Script finished successfully.");
                                        break;

                                    case false:
                                        Trace.WriteLine("Script finished early or with errors.");
                                        break;

                                    default:
                                        break;
                                }
                            }
                            else
                            {
                                Trace.WriteLine("Script stopping message timed out.");
                            }
                        }
                        catch { }
                    }

                    script = null;
                    Skills.Stop();
                    Drops.Stop();

                    AuraMonitor.StopMonitoring();
                    UnloadPreviousScript();
                    ScriptCts?.Dispose();
                    ScriptCts = null;
                    StrongReferenceMessenger.Default.Send<ScriptStoppedMessage, int>((int)MessageChannels.ScriptStatus);
                    ScriptRunning = false;
                }
            })
            {
                Name = "Script Thread",
                IsBackground = true
            };

            lock (_threadLock)
            {
                _currentScriptThread = scriptThread;
                scriptThread.Start();
            }

            StrongReferenceMessenger.Default.Send<ScriptStartedMessage, int>((int)MessageChannels.ScriptStatus);

            if (needsConfig)
            {
                _ = Task.Run(() =>
                {
                    scriptReady.Wait();
                    Config!.Configure();
                    lock (_configuredLock)
                    {
                        _configured[Config!.Storage] = true;
                    }
                });
            }

            return null;
        }
        catch (Exception e)
        {
            lock (_threadLock)
            {
                ScriptRunning = false;
            }
            return e;
        }
    }

    public async Task RestartScriptAsync()
    {
        Trace.WriteLine("Restarting script");
        await StopScript(false);
        await Task.Run(async () =>
        {
            await Task.Delay(5000);
            await StartScript();
        });
    }

    public async ValueTask StopScript(bool runScriptStoppingEvent = true)
    {
        lock (_stateLock)
        {
            _runScriptStoppingBool = runScriptStoppingEvent;
            _stoppedByScript = true;
        }

        ScriptCts?.Cancel();

        if (Thread.CurrentThread.Name == "Script Thread")
        {
            ScriptCts?.Token.ThrowIfCancellationRequested();
            return;
        }

        await Wait.ForTrueAsync(() => ScriptCts == null, 30).ConfigureAwait(false);

        Thread? thread;
        lock (_threadLock)
        {
            thread = _currentScriptThread;
        }

        if (thread?.IsAlive == true)
        {
            await Task.Run(() =>
            {
                if (!thread.Join(TimeSpan.FromSeconds(5)))
                {
                    try
                    {
                        thread.Interrupt();
                        if (!thread.Join(TimeSpan.FromSeconds(3)))
                        {
                            // More aggressive termination attempts
                            try
                            {
                                // Mark the thread as background so it doesn't prevent app shutdown
                                if (!thread.IsBackground)
                                    thread.IsBackground = true;

                                // Give one final chance with a shorter timeout
                                if (!thread.Join(TimeSpan.FromSeconds(1)))
                                {
                                    _logger?.ScriptLog("Script thread is unresponsive and may continue running until process exit.");
                                }
                            }
                            catch (Exception termEx)
                            {
                                _logger?.ScriptLog($"Error during thread termination: {termEx.Message}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger?.ScriptLog($"Error stopping script thread: {ex.Message}");
                    }
                }
            }).ConfigureAwait(false);
        }

        OnPropertyChanged(nameof(ScriptRunning));
    }

    [RequiresUnreferencedCode("This method may require code that cannot be statically analyzed for trimming. Use with caution.")]
    public object? Compile(string source)
    {
        CheckScriptVersionRequirement(source);

        Stopwatch sw = Stopwatch.StartNew();

        _includedFilesLock.EnterWriteLock();
        try
        {
            _includedFiles.Clear();
        }
        finally
        {
            _includedFilesLock.ExitWriteLock();
        }

        HashSet<string> references = GetReferences();
        string final = ProcessSources(source, ref references);

        // Debug: Check if final source is empty or contains no classes (disabled for performance)
#if DEBUG_VERBOSE

#endif

        // Check if the processed source contains any class definitions
        if (string.IsNullOrWhiteSpace(final))
        {
            throw new ScriptCompileException($"Script file '{LoadedScript}' is empty after processing includes and references.", final);
        }

        // Use regex to check for class definitions more robustly
        if (!_accessibilityRegex.IsMatch(final))
        {
            throw new ScriptCompileException($"Script file '{LoadedScript}' contains no class definitions after processing includes and references. Scripts must contain at least one class.", final);
        }

        // Recursively discover all transitive includes
        DiscoverAllIncludes(references);

        ScriptLoadContext loadContext = new();
        lock (_stateLock)
        {
            _currentLoadContext = loadContext;
        }

        List<string> compiledIncludes = CompileIncludedFiles(references, loadContext);

        references.UnionWith(compiledIncludes);

        int cacheHash;
        _includedFilesLock.EnterReadLock();
        try
        {
            cacheHash = ComputeCacheHash(final, new List<string>(_includedFiles));
        }
        finally
        {
            _includedFilesLock.ExitReadLock();
        }
        CompiledScript = final;
        string scriptName = Path.GetFileNameWithoutExtension(LoadedScript);

        Compiler compiler = Ioc.Default.GetRequiredService<Compiler>();
        compiler.AddDefaultReferencesAndNamespaces();
        compiler.AllowReferencesInCode = true;

        if (references.Count > 0)
            compiler.AddAssemblies(references.ToArray());

        dynamic? assembly = compiler.CompileClass(final, cacheHash, loadContext, scriptName);

        sw.Stop();
        Trace.WriteLine($"Script compilation took {sw.ElapsedMilliseconds}ms.");

        GC.Collect(2, GCCollectionMode.Optimized, blocking: false);

        return compiler.Error
            ? throw new ScriptCompileException(compiler.ErrorMessage, compiler.GeneratedClassCodeWithLineNumbers)
            : (object?)assembly;
    }

    private HashSet<string> GetReferences()
    {
        HashSet<string> references = new();

        lock (_refCacheLock)
        {
            if (_refCache.Count == 0 && Directory.Exists(ClientFileSources.SkuaPluginsDIR))
            {
                foreach (string file in Directory.EnumerateFiles(ClientFileSources.SkuaPluginsDIR, "*.dll"))
                {
                    string path = Path.Combine(ClientFileSources.SkuaDIR, file);
                    if (CanLoadAssembly(path))
                    {
                        _refCache.Add(path);
                        references.Add(path);
                    }
                }
            }
            else
            {
                references.UnionWith(_refCache);
            }
        }

        return references;
    }

    private string ProcessSources(string source, ref HashSet<string> references)
    {
        int actualLineCount = source.AsSpan().Count('\n') + 1;
        Span<Range> lineRanges = actualLineCount <= 1024 ? stackalloc Range[actualLineCount] : new Range[actualLineCount];
        int lineCount = source.AsSpan().Split(lineRanges, '\n');

        List<string> linesToRemove = new();
        List<string> filesToInclude = new();
        ReadOnlySpan<char> sourceSpan = source.AsSpan();

        for (int i = 0; i < lineCount; i++)
        {
            ReadOnlySpan<char> line = sourceSpan[lineRanges[i]].Trim();

            if (!line.StartsWith("//cs_"))
                continue;

            string lineStr = new(line);
            string[] parts = lineStr.Split((char[])null!, 2, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2)
                continue;

            string cmd = parts[0][5..];
            switch (cmd)
            {
                case "ref":
                    string local = Path.Combine(ClientFileSources.SkuaScriptsDIR, parts[1].Replace("Scripts/", ""));
                    if (File.Exists(local))
                        references.Add(local);
                    else if (File.Exists(parts[1]))
                        references.Add(parts[1]);
                    break;

                case "include":
                    string localSource = Path.Combine(ClientFileSources.SkuaScriptsDIR, parts[1].Replace("Scripts/", ""));
                    if (File.Exists(localSource))
                        filesToInclude.Add(localSource);
                    else if (File.Exists(parts[1]))
                        filesToInclude.Add(parts[1]);
                    break;
            }
            linesToRemove.Add(lineStr);
        }

        if (filesToInclude.Count > 0)
        {
            _includedFilesLock.EnterWriteLock();
            try
            {
                _includedFiles.AddRange(filesToInclude);
            }
            finally
            {
                _includedFilesLock.ExitWriteLock();
            }
        }

        if (linesToRemove.Count == 0)
            return source.Trim();

        HashSet<string> linesToRemoveSet = new(linesToRemove);
        string[] sourceLines = source.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
        List<string> filteredLines = new();

        foreach (string sourceLine in sourceLines)
        {
            string trimmedLine = sourceLine.Trim();
            if (!linesToRemoveSet.Contains(trimmedLine))
            {
                filteredLines.Add(sourceLine);
            }
        }

        return string.Join(Environment.NewLine, filteredLines).Trim();
    }

    private void DiscoverAllIncludes(HashSet<string> references)
    {
        bool foundNewFiles = true;
        int iterationCount = 0;
#if DEBUG_VERBOSE

#endif

        // Keep iterating until no new files are found
        while (foundNewFiles && iterationCount < 10) // Safety limit
        {
            foundNewFiles = false;
            iterationCount++;
#if DEBUG_VERBOSE

#endif

            List<string> currentFiles;
            _includedFilesLock.EnterReadLock();
            try
            {
                currentFiles = new List<string>(_includedFiles);
            }
            finally
            {
                _includedFilesLock.ExitReadLock();
            }

            foreach (string currentFile in currentFiles)
            {
                if (!File.Exists(currentFile))
                {

                    continue;
                }

                try
                {
                    string fileContent = File.ReadAllText(currentFile);
                    List<string> newIncludes = ExtractIncludeDirectivesFromSource(fileContent, references);


                    if (newIncludes.Count > 0)
                    {
                        List<string> filesToAdd = new();
                        foreach (string include in newIncludes)
                        {
                            if (File.Exists(include))
                                filesToAdd.Add(include);
                        }

                        if (filesToAdd.Count > 0)
                        {
                            _includedFilesLock.EnterWriteLock();
                            try
                            {
                                HashSet<string> existingNames = new(StringComparer.OrdinalIgnoreCase);
                                foreach (string existing in _includedFiles)
                                {
                                    existingNames.Add(Path.GetFileName(existing));
                                }

                                foreach (string include in filesToAdd)
                                {
                                    if (existingNames.Add(Path.GetFileName(include)))
                                    {
                                        _includedFiles.Add(include);
                                        foundNewFiles = true;
                                    }
                                }
                            }
                            finally
                            {
                                _includedFilesLock.ExitWriteLock();
                            }
                        }
                    }
                }
                catch (Exception ex)
                {

                }
            }
        }

    }

    private List<string> ExtractIncludeDirectivesFromSource(string source, HashSet<string> references)
    {
        List<string> includes = new();
        ReadOnlySpan<char> sourceSpan = source.AsSpan();

        int start = 0;
        int newlinePos;
        while ((newlinePos = sourceSpan[start..].IndexOf('\n')) >= 0)
        {
            ReadOnlySpan<char> line = sourceSpan[start..(start + newlinePos)].Trim();

            if (line.StartsWith("using"))
                break;

            if (line.StartsWith("//cs_"))
            {
                string lineStr = new(line);
                string[] parts = lineStr.Split((char[])null!, 2, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 2)
                {
                    string cmd = parts[0][5..];
                    switch (cmd)
                    {
                        case "ref":
                            string refLocal = Path.Combine(ClientFileSources.SkuaScriptsDIR, parts[1].Replace("Scripts/", ""));
                            if (File.Exists(refLocal))
                                references.Add(refLocal);
                            else if (File.Exists(parts[1]))
                                references.Add(parts[1]);
                            break;

                        case "include":
                            string includeLocal = Path.Combine(ClientFileSources.SkuaScriptsDIR, parts[1].Replace("Scripts/", ""));
                            if (File.Exists(includeLocal))
                                includes.Add(includeLocal);
                            else if (File.Exists(parts[1]))
                                includes.Add(parts[1]);
                            break;
                    }
                }
            }

            start += newlinePos + 1;
        }

        return includes;
    }

    public void LoadScriptConfig(object? script)
    {
        if (script is null)
            return;

        IScriptOptionContainer opts = Config = Ioc.Default.GetRequiredService<IScriptOptionContainer>();
        Type t = script.GetType();
        FieldInfo? storageField = t.GetField("OptionsStorage");
        FieldInfo? optsField = t.GetField("Options");
        FieldInfo? multiOptsField = t.GetField("MultiOptions");
        FieldInfo? dontPreconfField = t.GetField("DontPreconfigure");
        if (multiOptsField is not null)
        {
            string[] optFieldNames = (string[])multiOptsField.GetValue(script)!;
            List<FieldInfo> multiOpts = new(optFieldNames.Length);
            foreach (string optField in optFieldNames)
            {
                FieldInfo? field = t.GetField(optField);
                if (field != null)
                    multiOpts.Add(field);
            }
            foreach (FieldInfo opt in multiOpts)
            {
                List<IOption> parsedOpt = (List<IOption>)opt.GetValue(script)!;
                parsedOpt.ForEach(o => o.Category = opt.Name.Replace('_', ' '));
                opts.MultipleOptions.Add(opt.Name, parsedOpt);
            }
        }
        if (optsField is not null)
            opts.Options.AddRange((List<IOption>)optsField.GetValue(script)!);
        if (storageField is not null)
            opts.Storage = (string)storageField.GetValue(script)!;
        lock (_configuredLock)
        {
            if (dontPreconfField is not null)
                _configured[opts.Storage] = (bool)dontPreconfField.GetValue(script)!;
            else if (optsField is not null)
                _configured[opts.Storage] = false;
        }

        opts.SetDefaults();
        opts.Load();
    }

    private static bool CanLoadAssembly(string path)
    {
        try
        {
            AssemblyName.GetAssemblyName(path);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static int ComputeCacheHash(string source, List<string> includedFiles)
    {
        using SHA256 sha256 = SHA256.Create();
        byte[] sourceBytes = Encoding.UTF8.GetBytes(source);
        byte[] sourceHash = sha256.ComputeHash(sourceBytes);
        using MemoryStream ms = new();
        ms.Write(sourceHash, 0, sourceHash.Length);

        foreach (string file in includedFiles.OrderBy(f => f))
        {
            if (File.Exists(file))
            {
                byte[] pathBytes = Encoding.UTF8.GetBytes(file);
                ms.Write(pathBytes, 0, pathBytes.Length);

                long ticks = File.GetLastWriteTimeUtc(file).Ticks;
                byte[] ticksBytes = BitConverter.GetBytes(ticks);
                ms.Write(ticksBytes, 0, ticksBytes.Length);
            }
        }

        byte[] combinedHash = sha256.ComputeHash(ms.ToArray());
        return BitConverter.ToInt32(combinedHash, 0);
    }

    private void CheckScriptVersionRequirement(string source)
    {
        Match match = _versionRegex.Match(source);
        if (match.Success)
        {
            string requiredVersionStr = match.Groups[1].Value;
            Version? currentVersion = Assembly.GetExecutingAssembly().GetName().Version;

            if (Version.TryParse(requiredVersionStr, out Version? requiredVersion) && currentVersion != null && currentVersion < requiredVersion)
            {
                throw new ScriptVersionException(requiredVersionStr, currentVersion.ToString());
            }
        }
    }

    private List<string> CompileIncludedFiles(HashSet<string> references, ScriptLoadContext loadContext)
    {
        ConcurrentDictionary<string, string> compiledPaths = new();
        ConcurrentDictionary<string, bool> compilationCompleted = new();
        object lockObj = new();

        ConcurrentDictionary<string, List<string>> dependencyGraph = new();
        ConcurrentDictionary<string, (string source, string fileName, int hash, string cachePath)> fileInfoCache = new();

        string cacheDir = _cacheScriptsDir;

        ConcurrentBag<string> validCachedFiles = new();

#if DEBUG_VERBOSE

#endif

        Dictionary<string, string> filenameLookup = new(StringComparer.OrdinalIgnoreCase);
        List<string> includedFilesSnapshot;
        _includedFilesLock.EnterReadLock();
        try
        {
            includedFilesSnapshot = new List<string>(_includedFiles);
            foreach (string file in _includedFiles)
            {
                string filename = Path.GetFileName(file);
                if (!filenameLookup.ContainsKey(filename))
                    filenameLookup[filename] = file;
            }
        }
        finally
        {
            _includedFilesLock.ExitReadLock();
        }

        Parallel.ForEach(includedFilesSnapshot, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, includedFile =>
        {
            string includeSource = File.ReadAllText(includedFile);
            CheckScriptVersionRequirement(includeSource);
            List<string> deps = ExtractIncludeDependencies(includeSource);
            List<string> normalizedDeps = new();
            foreach (string dep in deps)
            {
                if (filenameLookup.TryGetValue(Path.GetFileName(dep), out string? matchingFile))
                {
                    normalizedDeps.Add(matchingFile);
                }
                else
                {
                    bool containsDep;
                    _includedFilesLock.EnterReadLock();
                    try
                    {
                        containsDep = _includedFiles.Contains(dep);
                    }
                    finally
                    {
                        _includedFilesLock.ExitReadLock();
                    }
                    if (containsDep)
                    {
                        normalizedDeps.Add(dep);
                    }
                }
            }
            dependencyGraph[includedFile] = normalizedDeps;

            string includeFileName = Path.GetFileNameWithoutExtension(includedFile);
            using SHA256 sha256 = SHA256.Create();
            byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(includeSource));
            int includeHash = BitConverter.ToInt32(hashBytes, 0);
            string compiledPath = Path.Combine(cacheDir, $"{includeHash}-{includeFileName}.dll");

            if (File.Exists(compiledPath))
            {
                try
                {
                    AssemblyName.GetAssemblyName(compiledPath);
                    validCachedFiles.Add(includedFile);
                    compiledPaths[includedFile] = compiledPath;
                    compilationCompleted[includedFile] = true;
                    Compiler.RegisterCompiled(includeHash, compiledPath);
                    fileInfoCache[includedFile] = (string.Empty, includeFileName, includeHash, compiledPath);
                    return;
                }
                catch
                {
                    try
                    {
                        File.Delete(compiledPath);
                    }
                    catch
                    {
                    }
                }
            }

            fileInfoCache[includedFile] = (includeSource, includeFileName, includeHash, compiledPath);
        });

        HashSet<string> processed = new(validCachedFiles);
        HashSet<string> includedFilesSet;
        _includedFilesLock.EnterReadLock();
        try
        {
            includedFilesSet = new HashSet<string>(_includedFiles);
        }
        finally
        {
            _includedFilesLock.ExitReadLock();
        }

#if DEBUG_VERBOSE

        foreach (var kvp in dependencyGraph)
        {
            string fileName = Path.GetFileName(kvp.Key);
            string deps = kvp.Value.Count > 0 ? string.Join(", ", kvp.Value.Select(Path.GetFileName)) : "[none]";

        }
#endif

        HashSet<string> allReferencedFiles;
        _includedFilesLock.EnterReadLock();
        try
        {
            allReferencedFiles = new HashSet<string>(_includedFiles);
        }
        finally
        {
            _includedFilesLock.ExitReadLock();
        }

        List<string> newlyAddedFiles = new();
        foreach (KeyValuePair<string, List<string>> kvp in dependencyGraph)
        {
            foreach (string dep in kvp.Value)
            {
                if (File.Exists(dep) && !allReferencedFiles.Contains(dep))
                {
                    allReferencedFiles.Add(dep);
                    newlyAddedFiles.Add(dep);
                }
            }
        }

        if (newlyAddedFiles.Count > 0)
        {
            _includedFilesLock.EnterWriteLock();
            try
            {
                _includedFiles.AddRange(newlyAddedFiles);
            }
            finally
            {
                _includedFilesLock.ExitWriteLock();
            }
        }

        foreach (string newFile in newlyAddedFiles)
        {
            string includeSource = File.ReadAllText(newFile);
            CheckScriptVersionRequirement(includeSource);
            string includeFileName = Path.GetFileNameWithoutExtension(newFile);
            using SHA256 sha256 = SHA256.Create();
            byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(includeSource));
            int includeHash = BitConverter.ToInt32(hashBytes, 0);
            string compiledPath = Path.Combine(cacheDir, $"{includeHash}-{includeFileName}.dll");

            if (File.Exists(compiledPath))
            {
                try
                {
                    AssemblyName.GetAssemblyName(compiledPath);
                    validCachedFiles.Add(newFile);
                    compiledPaths[newFile] = compiledPath;
                    compilationCompleted[newFile] = true;
                    Compiler.RegisterCompiled(includeHash, compiledPath);
                    fileInfoCache[newFile] = (string.Empty, includeFileName, includeHash, compiledPath);
                }
                catch
                {
                    fileInfoCache[newFile] = (includeSource, includeFileName, includeHash, compiledPath);
                }
            }
            else
            {
                fileInfoCache[newFile] = (includeSource, includeFileName, includeHash, compiledPath);
            }
        }

        List<string> orderedFiles;
        _includedFilesLock.EnterReadLock();
        try
        {
            orderedFiles = SortByDependencyOrder(dependencyGraph, new List<string>(_includedFiles));
        }
        finally
        {
            _includedFilesLock.ExitReadLock();
        }

#if DEBUG_VERBOSE

        foreach (string file in orderedFiles)
        {

        }
#endif

        HashSet<string> compiledFiles = new(validCachedFiles);

        foreach (string cachedFile in validCachedFiles)
        {
            compilationCompleted[cachedFile] = true;
        }

        while (compiledFiles.Count < orderedFiles.Count)
        {
            List<string> readyBatch = new();

            foreach (string file in orderedFiles)
            {
                if (compiledFiles.Contains(file))
                    continue;

                bool allDepsReady = true;
                if (dependencyGraph.TryGetValue(file, out List<string>? fileDeps))
                {
                    foreach (string dep in fileDeps)
                    {
                        if (!compilationCompleted.GetValueOrDefault(dep, false))
                        {
                            allDepsReady = false;
                            break;
                        }
                    }
                }

                if (allDepsReady)
                    readyBatch.Add(file);
            }

            if (readyBatch.Count == 0)
                break;

#if DEBUG_VERBOSE

#endif

            Parallel.ForEach(readyBatch, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, file =>
            {
                try
                {
                    CompileIncludeRecursive(file, references, loadContext, compiledPaths, compilationCompleted, lockObj, fileInfoCache);
                }
                catch (Exception ex)
                {

                    throw;
                }
            });

            foreach (string file in readyBatch)
                compiledFiles.Add(file);
        }

        fileInfoCache.Clear();

        return compiledPaths.Values.ToList();
    }


    private void CompileIncludeRecursive(
        string includedFile,
        HashSet<string> references,
        ScriptLoadContext loadContext,
        ConcurrentDictionary<string, string> compiledPaths,
        ConcurrentDictionary<string, bool> compilationCompleted,
        object lockObj,
        ConcurrentDictionary<string, (string source, string fileName, int hash, string cachePath)> fileInfoCache)
    {
        if (compiledPaths.ContainsKey(includedFile))
            return;

        try
        {
            (string source, string fileName, int hash, string cachePath) info = fileInfoCache[includedFile];
            string includeSource = info.source;
            string includeFileName = info.fileName;
            int includeHash = info.hash;
            string compiledPath = info.cachePath;

            HashSet<string> includeReferences = new(references);

            lock (lockObj)
            {
                foreach (KeyValuePair<string, string> kvp in compiledPaths)
                {
                    if (File.Exists(kvp.Value))
                    {
                        try
                        {
                            AssemblyName.GetAssemblyName(kvp.Value);
                            includeReferences.Add(kvp.Value);
                        }
                        catch
                        {
                        }
                    }
                }
            }

            string processedInclude = ProcessIncludeDirectives(includeSource, ref includeReferences);

            Compiler includeCompiler = Ioc.Default.GetRequiredService<Compiler>();
            includeCompiler.AddDefaultReferencesAndNamespaces();
            includeCompiler.AllowReferencesInCode = true;

            if (includeReferences.Count > 0)
                includeCompiler.AddAssemblies(includeReferences.ToArray());

            dynamic? assembly = includeCompiler.CompileClass(processedInclude, includeHash, loadContext, includeFileName);

            if (includeCompiler.Error)
            {
                throw new ScriptCompileException(
                    $"Error compiling included file '{includedFile}':\n{includeCompiler.ErrorMessage}",
                    includeCompiler.GeneratedClassCodeWithLineNumbers);
            }

            if (assembly != null && File.Exists(compiledPath))
            {
                for (int i = 0; i < 10; i++)
                {
                    try
                    {
                        using FileStream fs = new(compiledPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                        if (fs.Length > 0)
                            break;
                    }
                    catch
                    {
                        Thread.Sleep(10);
                    }
                }

                lock (lockObj)
                {
                    compiledPaths[includedFile] = compiledPath;
                    compilationCompleted[includedFile] = true;

                    if (!Compiler.IsLoaded(compiledPath))
                    {
                        try
                        {
                            using FileStream stream = File.OpenRead(compiledPath);
                            Assembly loadedAssembly = loadContext.LoadFromStream(stream);
                            Compiler.RegisterLoaded(compiledPath, loadedAssembly);
                        }
                        catch
                        {
                        }
                    }
                }
            }
        }
        catch (Exception ex) when (ex is not ScriptCompileException)
        {
            throw new ScriptCompileException($"Failed to compile included file '{includedFile}': {ex.Message}", string.Empty);
        }
    }

    private List<string> SortByDependencyOrder(ConcurrentDictionary<string, List<string>> dependencyGraph, List<string> files)
    {
        List<string> result = new();
        HashSet<string> visited = new();
        HashSet<string> visiting = new();

        foreach (string file in files)
        {
            if (!visited.Contains(file))
            {
                VisitDependencies(file, dependencyGraph, visited, visiting, result, files);
            }
        }

        return result;
    }

    private void VisitDependencies(string file, ConcurrentDictionary<string, List<string>> dependencyGraph,
        HashSet<string> visited, HashSet<string> visiting, List<string> result, List<string> allFiles)
    {
        if (visiting.Contains(file))
            return;

        if (visited.Contains(file))
            return;

        visiting.Add(file);

        // Visit all dependencies first
        if (dependencyGraph.TryGetValue(file, out List<string>? dependencies))
        {
            foreach (string dependency in dependencies)
            {
                if (allFiles.Contains(dependency))
                {
                    VisitDependencies(dependency, dependencyGraph, visited, visiting, result, allFiles);
                }
            }
        }

        visiting.Remove(file);
        visited.Add(file);
        result.Add(file);
    }

    private List<string> GetCompilationOrder(ConcurrentDictionary<string, List<string>> dependencyGraph, List<string> files)
    {
        List<string> result = new();
        HashSet<string> visited = new();
        HashSet<string> visiting = new();

        foreach (string file in files)
        {
            if (!visited.Contains(file))
            {
                VisitFile(file, dependencyGraph, visited, visiting, result, files);
            }
        }

        return result;
    }

    private void VisitFile(string file, ConcurrentDictionary<string, List<string>> dependencyGraph,
        HashSet<string> visited, HashSet<string> visiting, List<string> result, List<string> allFiles)
    {
        if (visiting.Contains(file))
            return;

        if (visited.Contains(file))
            return;

        visiting.Add(file);

        if (dependencyGraph.TryGetValue(file, out List<string>? deps))
        {
            foreach (string dep in deps)
            {
                if (allFiles.Contains(dep) && !visited.Contains(dep))
                {
                    VisitFile(dep, dependencyGraph, visited, visiting, result, allFiles);
                }
            }
        }

        visiting.Remove(file);
        visited.Add(file);
        result.Add(file);
    }

    private void GetTransitiveDependencies(string file, ConcurrentDictionary<string, List<string>> dependencyGraph, HashSet<string> allDeps)
    {
        if (!dependencyGraph.TryGetValue(file, out List<string>? directDeps))
            return;

        foreach (string dep in directDeps)
        {
            if (allDeps.Add(dep))
            {
                GetTransitiveDependencies(dep, dependencyGraph, allDeps);
            }
        }
    }

    private List<string> ExtractIncludeDependencies(string source)
    {
        List<string> dependencies = new();
        ReadOnlySpan<char> sourceSpan = source.AsSpan();

        int start = 0;
        int newlinePos;
        while ((newlinePos = sourceSpan[start..].IndexOf('\n')) >= 0)
        {
            ReadOnlySpan<char> line = sourceSpan[start..(start + newlinePos)].Trim();

            if (line.StartsWith("using"))
                break;

            if (line.StartsWith("//cs_include "))
            {
                string lineStr = new(line);
                string[] parts = lineStr.Split((char[])null!, 2, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 2)
                {
                    string includePath = parts[1];
                    string localPath = Path.Combine(ClientFileSources.SkuaScriptsDIR, includePath.Replace("Scripts/", ""));
                    if (File.Exists(localPath))
                        dependencies.Add(localPath);
                    else if (File.Exists(includePath))
                        dependencies.Add(includePath);
                }
            }

            start += newlinePos + 1;
        }

        return dependencies;
    }


    private string ProcessIncludeDirectives(string source, ref HashSet<string> references)
    {
        List<string> linesToRemove = new();
        ReadOnlySpan<char> sourceSpan = source.AsSpan();

        int start = 0;
        int newlinePos;
        while ((newlinePos = sourceSpan[start..].IndexOf('\n')) >= 0)
        {
            ReadOnlySpan<char> line = sourceSpan[start..(start + newlinePos)].Trim();

            if (line.StartsWith("using"))
                break;

            if (!line.StartsWith("//cs_"))
            {
                start += newlinePos + 1;
                continue;
            }

            string lineStr = new(line);
            string[] parts = lineStr.Split((char[])null!, 2, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2)
            {
                start += newlinePos + 1;
                continue;
            }

            string cmd = parts[0][5..];
            if (cmd == "ref")
            {
                string local = Path.Combine(ClientFileSources.SkuaScriptsDIR, parts[1].Replace("Scripts/", ""));
                if (File.Exists(local))
                    references.Add(local);
                else if (File.Exists(parts[1]))
                    references.Add(parts[1]);
            }

            linesToRemove.Add(new string(sourceSpan[start..(start + newlinePos + 1)]));
            start += newlinePos + 1;
        }

        if (linesToRemove.Count == 0)
            return source.Trim();

        StringBuilder sb = new(source);
        foreach (string lineToRemove in linesToRemove)
        {
            sb.Replace(lineToRemove, "");
        }
        return sb.ToString().Trim();
    }

    private void UnloadPreviousScript()
    {
        ScriptLoadContext? context;
        lock (_stateLock)
        {
            context = _currentLoadContext;
            _currentLoadContext = null;
        }

        Compiler.ClearSessionRegistries();

        if (context is null)
            return;

        _ = Task.Run(() =>
        {
            try
            {
                WeakReference weak = new(context);
                context.Unload();

                for (int i = 0; i < 3 && weak.IsAlive; i++)
                {
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    Thread.Sleep(50);
                }
            }
            catch
            {
            }
        });
    }

    public void SetLoadedScript(string path)
    {
        LoadedScript = path;
    }

    public void Dispose()
    {
        Thread? thread;
        lock (_threadLock)
        {
            thread = _currentScriptThread;
        }

        if (thread?.IsAlive == true)
        {
            ScriptCts?.Cancel();
            if (!thread.Join(TimeSpan.FromSeconds(5)))
            {
                _logger?.ScriptLog("Script thread did not exit during disposal.");
            }
        }
        ScriptCts?.Dispose();
        _includedFilesLock?.Dispose();
    }
}
