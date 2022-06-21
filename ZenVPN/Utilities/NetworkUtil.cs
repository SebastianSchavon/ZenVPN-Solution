using System.Net.NetworkInformation;

namespace ZenVPN.Utilities;

internal static class NetworkUtil
{
    public static bool CheckForVPNInterface()
    {
        foreach (NetworkInterface Interface in NetworkInterface.GetAllNetworkInterfaces())
        {
            // TAP-Windows Adapter is the OpenVPN driver for windows
            if (Interface.Description.Contains("TAP-Windows Adapter") && Interface.OperationalStatus == OperationalStatus.Up)
                return true;
        }

        return false;
    }

    public static long PingServerIp(string ip)
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
}
