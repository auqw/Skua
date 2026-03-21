using CommunityToolkit.Mvvm.Messaging;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Skua.Core.Interfaces;
using Skua.Core.Scripts;
using Skua.Core.Services;
using Skua.Core.Utils;
using System.Reflection;

namespace Skua.Core.AppStartup;

public static class Services
{
    public static IServiceCollection AddCommonServices(this IServiceCollection services)
    {
        services.AddTransient(typeof(Lazy<>), typeof(LazyInstance<>));

        services.AddSingleton(typeof(IMessenger), WeakReferenceMessenger.Default);
        services.AddSingleton(typeof(StrongReferenceMessenger), StrongReferenceMessenger.Default);

        services.AddSingleton<IDecamelizer, Decamelizer>();
        services.AddSingleton<IGetScriptsService, GetScriptsService>();
        services.AddSingleton<IProcessService, ProcessStartService>();

        return services;
    }


    private static List<PortableExecutableReference>? _cachedBaseReferences;
    private static readonly object _referenceCacheLock = new();

    public static Compiler CreateCompiler(IServiceProvider s)
    {
        Compiler compiler = new();

        if (_cachedBaseReferences == null)
        {
            lock (_referenceCacheLock)
            {
                if (_cachedBaseReferences == null)
                {
                    string[] refPaths = {
                        typeof(object).GetTypeInfo().Assembly.Location,
                        typeof(Console).GetTypeInfo().Assembly.Location,
                        typeof(object).Assembly.Location,
                        typeof(Enumerable).Assembly.Location,
                        typeof(ScriptManager).Assembly.Location,
                        Path.Combine(Path.GetDirectoryName(typeof(System.Runtime.GCSettings).GetTypeInfo().Assembly.Location)!, "System.Runtime.dll")
                    };

                    List<PortableExecutableReference> refs = AppDomain.CurrentDomain
                        .GetAssemblies()
                        .Where(a => !a.IsDynamic)
                        .Select(a => a.Location)
                        .Where(s => !string.IsNullOrEmpty(s))
                        .Where(s => !s.Contains("xunit"))
                        .Select(s => MetadataReference.CreateFromFile(s))
                        .ToList();

                    string? regexPath = typeof(System.Text.RegularExpressions.Regex).Assembly.Location;
                    if (!string.IsNullOrEmpty(regexPath))
                    {
                        refs.Add(MetadataReference.CreateFromFile(regexPath));
                    }

                    refs.AddRange(refPaths.Select(s => MetadataReference.CreateFromFile(s)));
                    _cachedBaseReferences = refs;
                }
            }
        }

        compiler.AddAssemblies(_cachedBaseReferences);
        compiler.AddNamespaces(new[]
        {
            "System",
            "System.Collections",
            "System.Collections.Generic",
            "System.Diagnostics",
            "System.Drawing",
            "System.Dynamic",
            "System.Globalization",
            "System.IO",
            "System.Linq",
            "System.Net",
            "System.Net.Http",
            "System.Reflection",
            "System.Runtime.CompilerServices",
            "System.Text",
            "System.Text.RegularExpressions",
            "System.Threading",
            "System.Threading.Tasks",
            "System.Timers",
            "System.Windows.Forms",
            "Skua.Core",
            "Skua.Core.Interfaces",
            "Skua.Core.Models",
            "Skua.Core.Models.Items",
            "Skua.Core.Models.Monsters",
            "Skua.Core.Models.Players",
            "Skua.Core.Models.Quests",
            "Skua.Core.Models.Servers",
            "Skua.Core.Models.Shops",
            "Skua.Core.Models.Skills",
            "Skua.Core.Models.Auras",
            "Skua.Core.Models.Factions",
            "Skua.Core.Options",
            "Skua.Core.Utils",
            "CommunityToolkit.Mvvm.DependencyInjection",
            "Newtonsoft.Json",
            "Newtonsoft.Json.Linq",
        });
        compiler.SaveGeneratedCode = true;
        return compiler;
    }
}
