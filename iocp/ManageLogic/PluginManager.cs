using TeruTeruServer.ServerEngineSDK.Interfaces;
using System;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;
using System.Linq;
using TeruTeruServer.ServerEngineSDK.Interfaces;
using TeruTeruServer.ServerEngineSDK.Enums;
using System.Net.Sockets;

namespace TeruTeruServer.ManageLogic
{
    /// <summary>
    /// 메인 서버 엔진에서 플러그인 로직을 대리하는 프록시 클래스입니다.
    /// 핫로딩 시 내부 인스턴스만 교체되어 끊김 없는 서비스를 제공합니다.
    /// </summary>
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

    /// <summary>
    /// 외부 DLL 플러그인을 동적으로 로드하고 관리하는 매니저입니다.
    /// </summary>
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
            // 초기 로드
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
                // 실전에서는 파일 잠금 문제를 피하기 위해 임시 복사본을 만들어 로드하는 것이 좋습니다.
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
                    // 생성자 주입 (DI 컨테이너의 서비스들을 플러그인에 전달)
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
