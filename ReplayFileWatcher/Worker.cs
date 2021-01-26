using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SkylordsRebornAPI.Replay;
using SkylordsRebornAPI.Replay.Data;

namespace ReplayFileWatcher
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly ReplayReader _reader;
        private readonly Config _config;

        public Worker(ILogger<Worker> logger)
        {
            _logger = logger;
            _reader = new();
            _config = new Config();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                var path = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) +
                           @"\BattleForge\replays";
                using (FileSystemWatcher watcher = new FileSystemWatcher())
                {
                    watcher.Path = path;
                    watcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName;
                    watcher.Filter = "autosave.pmv";
                    watcher.Changed += WatcherOnChanged;
                    watcher.EnableRaisingEvents = true;
                }
                _logger.LogInformation($"{path} is now being watched");
                
                while (!stoppingToken.IsCancellationRequested)
                {
                    await Task.Delay(1000, stoppingToken);
                }
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

            if (_config.MoveToThisFolder == String.Empty || !Directory.Exists(_config.MoveToThisFolder))
                File.Move(e.FullPath, e.FullPath.Replace("autosave", newFileName));
            else File.Move(e.FullPath, _config.MoveToThisFolder + @$"\{newFileName}.pmv");

            _logger.LogInformation($"File: {e.FullPath} changed to {newFileName}");
        }
    }

    public struct Config
    {
        public String MoveToThisFolder { get; set; }
        public String NewFileName { get; set; }

        public readonly string GetActualFileName(Replay replay)
        {
            var date = DateTime.Now;
            var fileName = NewFileName;
            fileName = fileName.Replace("<map>", replay.MapPath.Split(@"\\").Last().Split("_").Last());
            //List<String> playerNames = new List<string>();
            string hostPlayerName = "";
            string playerNameList = "";
            foreach (var team in replay.Teams)
            {
                foreach (var player in team.Players)
                {
                    if (player.PlayerId == replay.HostPlayerId)
                        hostPlayerName = player.Name;
                    //playerNames.Add(player.Name);
                    playerNameList += player.Name + "_";
                }
            }

            fileName = fileName.Replace("<PlayerNames>", playerNameList);
            fileName = fileName.Replace("<hostPlayer>", hostPlayerName);
            fileName = fileName.Replace("<date>", date.Day + "-" + date.Month + "-" + date.Year);
            return fileName;
        }
    }
}