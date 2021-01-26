using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SkylordsRebornAPI.Replay;

namespace ReplayFileWatcher
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly ReplayReader _reader;
        private Config _config;
        private readonly IConfigurationRoot _configBuilder;

        public Worker(ILogger<Worker> logger)
        {
            _logger = logger;
            _reader = new ReplayReader();
            if (!File.Exists("config.json"))
            {
                File.Create("config.json");
                File.WriteAllText("config.json", "{ \"NewFileName\":\"<date> <map>\",\"MoveToThisFolder\":\"\"}");
            }
            _configBuilder = new ConfigurationBuilder().AddJsonFile("config.json").Build();
            _config = _configBuilder.Get<Config>();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                using var replayWatcher = new FileSystemWatcher();
                var path = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) +
                           @"\BattleForge\replays";
                replayWatcher.Path = path;
                replayWatcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.CreationTime;
                replayWatcher.Filter = "autosave.pmv";
                replayWatcher.Changed += WatcherOnChanged;
                replayWatcher.EnableRaisingEvents = true;

                _logger.LogInformation($"{path} is now being watched");


                using var configWatcher = new FileSystemWatcher();
                configWatcher.Path = AppContext.BaseDirectory;
                configWatcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName;
                configWatcher.Filter = "config.json";
                configWatcher.Changed += (_, _) =>
                {
                    _configBuilder.Reload();
                    _config = _configBuilder.Get<Config>();
                };
                configWatcher.EnableRaisingEvents = true;

                _logger.LogInformation("Config file is now being watched");

                while (!stoppingToken.IsCancellationRequested) await Task.Delay(1000, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex.ToString());
            }
        }

        private void WatcherOnChanged(object sender, FileSystemEventArgs e)
        {
            _logger.LogInformation("Starting Process @ {time}", DateTimeOffset.Now);
            var replay = _reader.ReadReplay(e.FullPath);
            var newFileName = _config.GetActualFileName(replay);

            if (_config.MoveToThisFolder == string.Empty || !Directory.Exists(_config.MoveToThisFolder))
                File.Move(e.FullPath, e.FullPath.Replace("autosave", newFileName));
            else File.Move(e.FullPath, _config.MoveToThisFolder + @$"\{newFileName}.pmv");

            _logger.LogInformation($"File: {e.FullPath} changed to {newFileName}.pmv");
        }
    }
}