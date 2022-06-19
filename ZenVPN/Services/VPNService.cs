using ZenVPN.MVVM.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using ZenVPN.Core;
using System.Threading;

namespace ZenVPN.Services;

interface IVPNService
{
    IEnumerable<ServerModel> GetServers();
    bool CheckForVPNInterface();
    Task Connect(ServerModel sm);
    Task Disconnect();
    string GetCountry(string name);
    long PingServerIp(string ip);
    string SetDisconnectStatus();
    string SetConnectStatus(ServerModel server);
}

internal class VPNService : ObservableObject, IVPNService
{
    public IEnumerable<ServerModel> GetServers()
    {
        var servers = Directory.GetFiles("./servers")
            .Select(x => new ServerModel
            {
                Name = Path.GetFileName(x).Remove(Path.GetFileName(x).Length - 5, 5),
                Country = GetCountry(Path.GetFileName(x)),
                Ip = GetFileIPAddress(x),
                Ms = PingServerIp(GetFileIPAddress(x))

            }).ToList();

        return servers;
    }

    public bool CheckForVPNInterface()
    {
        foreach (NetworkInterface Interface in NetworkInterface.GetAllNetworkInterfaces())
        {
            // TAP-Windows Adapter is the OpenVPN driver for windows
            if (Interface.Description.Contains("TAP-Windows Adapter") && Interface.OperationalStatus == OperationalStatus.Up)
                return true;
        }

        return false;
    }

    public async Task Connect(ServerModel sm)
    {
        var process = new Process();
        process.StartInfo.FileName = @"C:\Program Files\OpenVPN\bin\openvpn.exe";
        process.StartInfo.Arguments = $@"--config ./servers/{sm.Name}.ovpn";
        process.StartInfo.Verb = "runas";
        process.StartInfo.CreateNoWindow = true;
        process.Start();
#pragma warning disable CS4014
        process.WaitForExitAsync();
#pragma warning restore CS4014

        

    }
    public async Task Disconnect()
    {
        Process.Start("taskkill", "/F /IM openvpn.exe").StartInfo.CreateNoWindow = true;

    }

    public string SetDisconnectStatus()
    {

        for (int i = 0; i < 20; i++)
        {
            Thread.Sleep(1000);

            if (!CheckForVPNInterface())
                return "Disconnected";
        }

        return "Something went wrong...";

    }
    public string SetConnectStatus(ServerModel server)
    {

        for (int i = 0; i < 20; i++)
        {
            Thread.Sleep(1000);

            if (CheckForVPNInterface())
                return $"Connected to {server.Name}";
        }

        return "Something went wrong...";

    }

    public string GetCountry(string name)
    {
        if (name.Contains("se"))
            return "https://upload.wikimedia.org/wikipedia/en/thumb/4/4c/Flag_of_Sweden.svg/1280px-Flag_of_Sweden.svg.png";
        if (name.Contains("us"))
            return "https://upload.wikimedia.org/wikipedia/en/thumb/a/a4/Flag_of_the_United_States.svg/1200px-Flag_of_the_United_States.svg.png";
        if (name.Contains("de"))
            return "https://upload.wikimedia.org/wikipedia/en/thumb/b/ba/Flag_of_Germany.svg/1920px-Flag_of_Germany.svg.png";

        return "";
    }

    //public PingReply PingServerFile(string file)
    //{

    //    using (var pinger = new Ping())
    //    {
    //        return pinger.Send(GetFileIPAddress(file));
    //    }

    //}
    public long PingServerIp(string ip)
    {
        long rounds = 0;

        using (var pinger = new Ping())
        {
            for (int i = 0; i < 5; i++)
            {
                rounds += pinger.Send(ip).RoundtripTime;
            }
            return rounds / 5;
        }

    }

    public string GetFileIPAddress(string file)
    {
        foreach (var line in File.ReadAllLines(file))
        {
            if (line.Contains("remote"))
            {
                var l = line.Substring(7, 11);
                return l;
            }
        }

        return "";
    }

    
}
