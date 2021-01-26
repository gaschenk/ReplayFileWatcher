using System;
using System.Linq;
using SkylordsRebornAPI.Replay.Data;

namespace ReplayFileWatcher
{
    public class Config
    {
        public string MoveToThisFolder { get; set; }
        public string NewFileName { get; set; }

        public string GetActualFileName(Replay replay)
        {
            var date = DateTime.Now;
            var fileName = NewFileName;
            fileName = fileName.Replace("<map>", replay.MapPath.Split(@"\\").Last().Split("_").Last());
            //List<String> playerNames = new List<string>();
            var hostPlayerName = "";
            var playerNameList = "";
            foreach (var team in replay.Teams)
            foreach (var player in team.Players)
            {
                if (player.PlayerId == replay.HostPlayerId)
                    hostPlayerName = player.Name;
                //playerNames.Add(player.Name);
                playerNameList += player.Name + "_";
            }

            fileName = fileName.Replace("<PlayerNames>", playerNameList);
            fileName = fileName.Replace("<hostPlayer>", hostPlayerName);
            fileName = fileName.Replace("<date>", date.Day + "-" + date.Month + "-" + date.Year);
            return fileName;
        }
    }
}