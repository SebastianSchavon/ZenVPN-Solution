using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ZenVPN.MVVM.Model;

namespace ZenVPN.Services;

interface IViewModelService
{
    void Connect(ServerModel sm);
    void Disconnect();

}

internal class ViewModelService : IViewModelService
{
    CancellationTokenSource tokenSource;
    Process process;

    public async void Connect(ServerModel sm)
    {
        tokenSource = new CancellationTokenSource();

        process = new Process();
        process.StartInfo.FileName = @"C:\Program Files\OpenVPN\bin\openvpn.exe";
        process.StartInfo.Arguments = $@"--config ./servers/{sm.Name}.ovpn";
        process.StartInfo.Verb = "runas";
        //process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.CreateNoWindow = true;
        process.Start();

        tokenSource.Token.Register(() =>
        {
            process.Kill();
            process.Dispose();

        });

        #pragma warning disable CS4014
        process.WaitForExitAsync(tokenSource.Token);
        #pragma warning restore CS4014

    }
    public void Disconnect()
    {
        //tokenSource.Cancel();
        Process.Start("taskkill", "/F /IM openvpn.exe").StartInfo.CreateNoWindow = false;
        tokenSource.Dispose();
    }


}
