using ZenVPN.MVVM.Model;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using ZenVPN.Core;
using System.Threading;

namespace ZenVPN.Services;

//interface IVPNService
//{
//    IEnumerable<ServerModel> GetServers();
//    bool CheckForVPNInterface();
//    void Connect(ServerModel sm);
//    void Disconnect();
//    string GetCountry(string name);
//    long PingServerIp(string ip);
//    string SetDisconnectStatus();
//    string SetConnectStatus(ServerModel server, int timer);
//}

//internal class VPNService : ObservableObject, IVPNService
//{
//    CancellationTokenSource tokenSource;
//    Process process;

    



//    public string SetDisconnectStatus()
//    {
//        for (int i = 0; i < 8; i++)
//        {
//            Thread.Sleep(1000);

//            if (!CheckForVPNInterface())
//                return "Disconnected";
//        }

//        return "Something went wrong...";

//    }
//    public string SetConnectStatus(ServerModel server, int timer)
//    {
//        for (int i = 0; i < timer; i++)
//        {
//            Thread.Sleep(1000);

//            if (CheckForVPNInterface())
//                return $"Connected to {server.Name}";
//        }

//        return "Something went wrong. Retrying...";

//    }




//}
