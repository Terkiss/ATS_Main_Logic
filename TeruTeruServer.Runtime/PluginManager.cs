using System;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;
using System.Linq;
using TeruTeruServer.SDK.Interfaces;
using TeruTeruServer.SDK.Enums;
using System.Net.Sockets;

namespace TeruTeruServer.Runtime
{
    public class LogicProxy : ILogicService
    {
        private ILogicService _currentLogic;
        private readonly object _lock = new object();
        private int _consecutiveErrors = 0;
        private bool _isDisabled = false;
        private const int MAX_CONSECUTIVE_ERRORS = 10;

        public void UpdateLogic(ILogicService newLogic)
        {
            lock (_lock)
            {
                _currentLogic = newLogic;
                _consecutiveErrors = 0;
                _isDisabled = false;
            }
        }

        public void ProcessDirectProtocol(byte[] buffer, Socket socket)
        {
            lock (_lock)
            {
                if (_isDisabled || _currentLogic == null) return;
                try
                {
                    _currentLogic.ProcessDirectProtocol(buffer, socket);
                    _consecutiveErrors = 0;
                }
                catch (Exception ex)
                {
                    _consecutiveErrors++;
                    SDK.Util.TeruTeruLogger.LogError($"[LogicProxy] Error in ProcessDirectProtocol: {ex.Message} (Consecutive: {_consecutiveErrors})");
                    if (_consecutiveErrors >= MAX_CONSECUTIVE_ERRORS)
                    {
                        _isDisabled = true;
                        SDK.Util.TeruTeruLogger.LogError("[LogicProxy] Plugin disabled due to consecutive errors.");
                    }
                }
            }
        }

        public void ProcessJsonProtocol(string json, ProtocolSelect protocolSelect, Socket socket)
        {
            lock (_lock)
            {
                if (_isDisabled || _currentLogic == null) return;
                try
                {
                    _currentLogic.ProcessJsonProtocol(json, protocolSelect, socket);
                    _consecutiveErrors = 0;
                }
                catch (Exception ex)
                {
                    _consecutiveErrors++;
                    SDK.Util.TeruTeruLogger.LogError($"[LogicProxy] Error in ProcessJsonProtocol: {ex.Message} (Consecutive: {_consecutiveErrors})");
                    if (_consecutiveErrors >= MAX_CONSECUTIVE_ERRORS)
                    {
                        _isDisabled = true;
                        SDK.Util.TeruTeruLogger.LogError("[LogicProxy] Plugin disabled due to consecutive errors.");
                    }
                }
            }
        }
    }

    public class PluginManager
    {
        private readonly string _pluginPath;
        private readonly LogicProxy _proxy;
        private readonly IServiceProvider _serviceProvider;
        private FileSystemWatcher _watcher;
        private AssemblyLoadContext? _currentContext;
        private readonly System.Timers.Timer _debounceTimer;

        public PluginManager(string pluginPath, LogicProxy proxy, IServiceProvider serviceProvider)
        {
            _pluginPath = Path.GetFullPath(pluginPath);
            _proxy = proxy;
            _serviceProvider = serviceProvider;

            _debounceTimer = new System.Timers.Timer(500);
            _debounceTimer.AutoReset = false;
            _debounceTimer.Elapsed += (s, e) => ReloadPlugins();

            if (!Directory.Exists(_pluginPath)) Directory.CreateDirectory(_pluginPath);
        }

        public void StartMonitoring()
        {
            ReloadPlugins();

            _watcher = new FileSystemWatcher(_pluginPath, "*.dll");
            _watcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName;
            _watcher.Changed += (s, e) => TriggerReload();
            _watcher.Created += (s, e) => TriggerReload();
            _watcher.EnableRaisingEvents = true;

            Console.WriteLine($"[PluginManager] Monitoring plugins at: {_pluginPath}");
        }

        private void TriggerReload()
        {
            _debounceTimer.Stop();
            _debounceTimer.Start();
        }

        public void ReloadPlugins()
        {
            try
            {
                UnloadCurrentContext();

                var dllFiles = Directory.GetFiles(_pluginPath, "*.dll");
                if (dllFiles.Length == 0) return;

                _currentContext = new AssemblyLoadContext("LogicContext", isCollectible: true);
                
                var pluginInfos = new List<PluginLoadInfo>();
                foreach (var dll in dllFiles)
                {
                    try
                    {
                        using var stream = File.OpenRead(dll);
                        var assembly = _currentContext.LoadFromStream(stream);
                        var dependency = assembly.GetTypes().FirstOrDefault(t => typeof(IPluginDependency).IsAssignableFrom(t) && !t.IsInterface);
                        var logic = assembly.GetTypes().FirstOrDefault(t => typeof(ILogicService).IsAssignableFrom(t) && !t.IsInterface);
                        
                        if (logic != null)
                        {
                            var info = new PluginLoadInfo { Assembly = assembly, LogicType = logic };
                            if (dependency != null)
                            {
                                var depInstance = (IPluginDependency)Activator.CreateInstance(dependency)!;
                                info.Name = depInstance.PluginName;
                                info.DependsOn = depInstance.DependsOn;
                            }
                            else
                            {
                                info.Name = Path.GetFileNameWithoutExtension(dll);
                            }
                            pluginInfos.Add(info);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[PluginManager] Skip DLL {Path.GetFileName(dll)}: {ex.Message}");
                    }
                }

                // 위상 정렬 (Topological Sort)
                var sortedPlugins = SortPlugins(pluginInfos);
                if (sortedPlugins.Count > 0)
                {
                    // 첫 번째 정렬된 플러그인을 활성 로직으로 사용
                    var target = sortedPlugins[0];
                    var constructor = target.LogicType.GetConstructors().First();
                    var parameters = constructor.GetParameters()
                        .Select(p => _serviceProvider.GetService(p.ParameterType))
                        .ToArray();

                    var instance = (ILogicService)Activator.CreateInstance(target.LogicType, parameters);
                    _proxy.UpdateLogic(instance);
                    Console.WriteLine($"[PluginManager] Logic plugin hot-reloaded: {target.Name}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PluginManager] Error reloading plugins: {ex.Message}");
            }
        }

        private void UnloadCurrentContext()
        {
            if (_currentContext == null) return;

            var weakRef = new WeakReference(_currentContext);
            _currentContext.Unload();
            _currentContext = null;

            // Wait for unload (max 5s)
            for (int i = 0; i < 50 && weakRef.IsAlive; i++)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                System.Threading.Thread.Sleep(100);
            }

            if (weakRef.IsAlive)
            {
                Console.WriteLine("[PluginManager] Warning: Previous AssemblyLoadContext still alive after Unload.");
            }
        }

        private List<PluginLoadInfo> SortPlugins(List<PluginLoadInfo> plugins)
        {
            var sorted = new List<PluginLoadInfo>();
            var visited = new HashSet<string>();
            var visiting = new HashSet<string>();

            void Visit(PluginLoadInfo plugin)
            {
                if (visited.Contains(plugin.Name)) return;
                if (visiting.Contains(plugin.Name))
                {
                    throw new Exception($"Circular dependency detected: {plugin.Name}");
                }

                visiting.Add(plugin.Name);

                if (plugin.DependsOn != null)
                {
                    foreach (var depName in plugin.DependsOn)
                    {
                        var depPlugin = plugins.FirstOrDefault(p => p.Name == depName);
                        if (depPlugin != null) Visit(depPlugin);
                    }
                }

                visiting.Remove(plugin.Name);
                visited.Add(plugin.Name);
                sorted.Add(plugin);
            }

            foreach (var plugin in plugins)
            {
                try
                {
                    Visit(plugin);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[PluginManager] Dependency Error: {ex.Message}");
                }
            }

            return sorted;
        }

        private class PluginLoadInfo
        {
            public string Name { get; set; } = string.Empty;
            public string[] DependsOn { get; set; } = Array.Empty<string>();
            public Assembly Assembly { get; set; }
            public Type LogicType { get; set; }
        }
    }
}
