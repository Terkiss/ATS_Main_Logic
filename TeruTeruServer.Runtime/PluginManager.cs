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

        public void UpdateLogic(ILogicService newLogic)
        {
            lock (_lock)
            {
                _currentLogic = newLogic;
            }
        }

        public void ProcessDirectProtocol(byte[] buffer, Socket socket)
        {
            lock (_lock)
            {
                _currentLogic?.ProcessDirectProtocol(buffer, socket);
            }
        }

        public void ProcessJsonProtocol(string json, ProtocolSelect protocolSelect, Socket socket)
        {
            lock (_lock)
            {
                _currentLogic?.ProcessJsonProtocol(json, protocolSelect, socket);
            }
        }
    }

    public class PluginManager
    {
        private readonly string _pluginPath;
        private readonly LogicProxy _proxy;
        private readonly IServiceProvider _serviceProvider;
        private FileSystemWatcher _watcher;

        public PluginManager(string pluginPath, LogicProxy proxy, IServiceProvider serviceProvider)
        {
            _pluginPath = Path.GetFullPath(pluginPath);
            _proxy = proxy;
            _serviceProvider = serviceProvider;

            if (!Directory.Exists(_pluginPath)) Directory.CreateDirectory(_pluginPath);
        }

        public void StartMonitoring()
        {
            ReloadPlugins();

            _watcher = new FileSystemWatcher(_pluginPath, "*.dll");
            _watcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName;
            _watcher.Changed += (s, e) => ReloadPlugins();
            _watcher.Created += (s, e) => ReloadPlugins();
            _watcher.EnableRaisingEvents = true;

            Console.WriteLine($"[PluginManager] Monitoring plugins at: {_pluginPath}");
        }

        public void ReloadPlugins()
        {
            try
            {
                var dllFiles = Directory.GetFiles(_pluginPath, "*.dll");
                var latestDll = dllFiles.FirstOrDefault();

                if (string.IsNullOrEmpty(latestDll)) return;

                Console.WriteLine($"[PluginManager] Loading logic plugin: {Path.GetFileName(latestDll)}...");

                var context = new AssemblyLoadContext("LogicContext", isCollectible: true);
                using var stream = File.OpenRead(latestDll);
                var assembly = context.LoadFromStream(stream);

                var logicType = assembly.GetTypes().FirstOrDefault(t => typeof(ILogicService).IsAssignableFrom(t) && !t.IsInterface);
                if (logicType != null)
                {
                    var constructor = logicType.GetConstructors().First();
                    var parameters = constructor.GetParameters()
                        .Select(p => _serviceProvider.GetService(p.ParameterType))
                        .ToArray();

                    var instance = (ILogicService)Activator.CreateInstance(logicType, parameters);
                    _proxy.UpdateLogic(instance);

                    Console.WriteLine("[PluginManager] Logic plugin hot-reloaded successfully.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PluginManager] Error loading plugin: {ex.Message}");
            }
        }
    }
}
