using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZenVPN.MVVM.Model;

namespace ZenVPN.Utilities;

internal static class IOUtil
{
    public static IEnumerable<ServerModel> GetServers()
    {
        var servers = Directory.GetFiles("./servers")
            .Select(x => new ServerModel
            {
                Name = Path.GetFileName(x).Remove(Path.GetFileName(x).Length - 5, 5),
                Country = GetCountry(Path.GetFileName(x)),
                Ip = GetFileIPAddress(x),
                Ms = NetworkUtil.PingServerIp(GetFileIPAddress(x)),
                ForegroundColor = "White"

            }).ToList();

        return servers;
    }

    public static string GetCountry(string name)
    {
        if (name.Contains("se"))
            return "https://upload.wikimedia.org/wikipedia/en/thumb/4/4c/Flag_of_Sweden.svg/1280px-Flag_of_Sweden.svg.png";
        if (name.Contains("us"))
            return "https://upload.wikimedia.org/wikipedia/en/thumb/a/a4/Flag_of_the_United_States.svg/1200px-Flag_of_the_United_States.svg.png";
        if (name.Contains("de"))
            return "https://upload.wikimedia.org/wikipedia/en/thumb/b/ba/Flag_of_Germany.svg/1920px-Flag_of_Germany.svg.png";

        return "";
    }


    public static string GetFileIPAddress(string file)
    {
        var line = File.ReadLines(file).FirstOrDefault(x => x.Contains("remote"));

        if (line != null)
            return line.Substring(7, 11);

        return "";
    }


}
