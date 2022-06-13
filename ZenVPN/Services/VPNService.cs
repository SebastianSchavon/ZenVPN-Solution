using ZenVPN.MVVM.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace ZenVPN.Services;

interface IVPNService
{
    IEnumerable<ServerModel> GetServers();
    bool CheckForVPNInterface();
    Task Connect(ServerModel sm);
    Task Disconnect();
    string GetCountry(string name);

}

internal class VPNService : IVPNService
{
    public IEnumerable<ServerModel> GetServers()
    {
        return Directory.GetFiles("./servers")
            .Select(x => new ServerModel { Name = Path.GetFileName(x).Remove(Path.GetFileName(x).Length - 5, 5), Country = GetCountry(Path.GetFileName(x)) });
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
        process.WaitForExitAsync();

        await Task.Delay(TimeSpan.FromSeconds(6));

    }
    public async Task Disconnect()
    {
        Process.Start("taskkill", "/F /IM openvpn.exe").StartInfo.CreateNoWindow = true;

        await Task.Delay(TimeSpan.FromSeconds(6));

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



}
