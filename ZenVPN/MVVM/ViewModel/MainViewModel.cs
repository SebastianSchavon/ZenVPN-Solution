using ZenVPN.Core;
using ZenVPN.MVVM.Model;
using ZenVPN.Services;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Net.NetworkInformation;
using System.Linq;
using ZenVPN.Utilities;
using System.Diagnostics;
using System;

namespace ZenVPN.MVVM.ViewModel;

internal class MainViewModel : ObservableObject
{
    IViewModelService _service;
    CancellationTokenSource tokenSource;
    public RelayCommand MoveWindowCommand { get; set; }
    public RelayCommand ShutdownWindowCommand { get; set; }
    public RelayCommand MinimizeWindowCommand { get; set; }

    public RelayCommand ConnectCommand { get; set; }
    public RelayCommand DisconnectCommand { get; set; }

    public ObservableCollection<ServerModel> Servers { get; set; }

    private string _connectionStatus;

    public string ConnectionStatus
    {
        get { return _connectionStatus; }
        set
        {
            _connectionStatus = value;
            OnPropertyChanged();
        }
    }

    private ServerModel _selectedServer;

    public ServerModel SelectedServer
    {
        get { return _selectedServer; }
        set
        {
            _selectedServer = value;
            OnPropertyChanged();
        }
    }

    private string _currentIP;

    public string CurrentIP
    {
        get { return _currentIP; }
        set
        {
            _currentIP = value;
            OnPropertyChanged();
        }
    }

    private string _dataTransfer;

    public string DataTransfer
    {
        get { return _dataTransfer; }
        set
        {
            _dataTransfer = value;
            OnPropertyChanged();
        }
    }

    public MainViewModel()
    {

    }

    public MainViewModel(IViewModelService service)
    {
        _service = service;

        //_service = new VPNService();

        SetConnectionStatus();

        SetDataTransfer("0kb   0kb");

        Servers = new ObservableCollection<ServerModel>(IOUtil.GetServers());

        MoveWindowCommand = new RelayCommand(o => { Application.Current.MainWindow.DragMove(); });

        ShutdownWindowCommand = new RelayCommand(o => { /*_service.Disconnect();*/ Process.Start("taskkill", "/F /IM openvpn.exe").StartInfo.CreateNoWindow = false; Application.Current.Shutdown(); ; });

        MinimizeWindowCommand = new RelayCommand(o => { Application.Current.MainWindow.WindowState = WindowState.Minimized; });

        Task.Run(() => SetServerPing());

        ConnectCommand = new RelayCommand(o =>
        {
            if (SelectedServer == null)
            {
                ConnectionStatus = "SELECT SERVER";
                return;
            }

            if (ConnectionStatus == $"Connected to {SelectedServer.Name}")
                return;

            ConnectMethod();

        });

        DisconnectCommand = new RelayCommand(o =>
        {

            if (ConnectionStatus == "Disconnected")
                return;

            DisconnectMethod();

        });
    }

    private void ConnectMethod()
    {
        tokenSource = new CancellationTokenSource();

        var t1 = Task.Run(() =>
        {
            int counter = 0;
            while (true)
            {
                _service.Connect(SelectedServer);

                if (SetConnectStatus(5, "Retrying"))
                    return;

                if (counter >= 3 || tokenSource.Token.IsCancellationRequested)
                {
                    tokenSource.Cancel();
                    ConnectionStatus = "Disconnected";

                    return;
                }

                counter++;
            }

        }, tokenSource.Token);

        t1.ContinueWith((antecendent) =>
        {
            SetServerForeground(SelectedServer);
            MonitorBandWidth();

        });

    }

    private void DisconnectMethod()
    {
        var t1 = Task.Run(() =>
        {
            tokenSource.Cancel();
            _service.Disconnect();

        });

        t1.ContinueWith((antecendant) => SetDisconnectStatus()).ContinueWith((antecendant) =>
        {
            SetServerForeground(SelectedServer);
            SetDataTransfer("0kb  0kb");

        });

    }

    private void SetServerForeground(ServerModel sm)
    {
        foreach (var s in Servers)
            s.ForegroundColor = "white";

        if (ConnectionStatus.Contains("Connected to"))
        {
            var server = Servers.FirstOrDefault(x => x.Name == sm.Name);

            if (server != null)
                server.ForegroundColor = "#50ee3a";

        }

        if (ConnectionStatus.Contains("Disconnected"))
        {

            var server = Servers.FirstOrDefault(x => x.Name == sm.Name);

            if (server != null)
                server.ForegroundColor = "white";

        }
    }

    private void SetDataTransfer(string value)
    {
        DataTransfer = value;
    }

    private void SetConnectionStatus()
    {
        if (NetworkUtil.CheckForVPNInterface())
            ConnectionStatus = "Connected";
        else
            ConnectionStatus = "Disconnected";
    }


    public void SetDisconnectStatus()
    {
        ConnectionStatus = "Disconnecting";

        int count = 0;

        for (int i = 0; i < 8; i++)
        {

            if (count >= 4)
            {
                ConnectionStatus = ConnectionStatus.Substring(0, ConnectionStatus.Length - count);
                count = 0;
            }

            Thread.Sleep(1000);

            if (!NetworkUtil.CheckForVPNInterface())
            {
                ConnectionStatus = "Disconnected";
                return;
            }

            count++;

            ConnectionStatus += ".";

        }

        ConnectionStatus = "Something went wrong...";

    }
    public bool SetConnectStatus(int timer, string errorStatus)
    {
        ConnectionStatus = "Connecting";

        int count = 0;

        for (int i = 0; i < timer; i++)
        {

            if (count >= 4)
            {
                ConnectionStatus = ConnectionStatus.Substring(0, ConnectionStatus.Length - count);
                count = 0;
            }

            Thread.Sleep(1000);

            if (NetworkUtil.CheckForVPNInterface())
            {
                ConnectionStatus = $"Connected to {SelectedServer.Name}";
                return true;
            }

            count++;

            ConnectionStatus += ".";

        }

        ConnectionStatus = errorStatus;

        return false;
    }

    private async void SetServerPing()
    {
        while (true)
        {
            Thread.Sleep(3000);

            foreach (var server in Servers)
            {
                server.Ms = NetworkUtil.PingServerIp(server.Ip);
            }
        }
    }

    public async void MonitorBandWidth()
    {
        IPv4InterfaceStatistics statistics;
        long sent;
        long recieved;

        foreach (NetworkInterface Interface in NetworkInterface.GetAllNetworkInterfaces())
        {
            // TAP-Windows Adapter is the OpenVPN driver for windows
            if (Interface.Description.Contains("TAP-Windows Adapter") && Interface.OperationalStatus == OperationalStatus.Up)
            {
                statistics = Interface.GetIPv4Statistics();

                long sentBefore = statistics.BytesSent / 1024;
                long recievedBefore = statistics.BytesReceived / 1024;

                while (ConnectionStatus.Contains("Connected to"))
                {
                    Thread.Sleep(2000);

                    statistics = Interface.GetIPv4Statistics();

                    sent = (statistics.BytesSent / 1024) - sentBefore;
                    recieved = (statistics.BytesReceived / 1024) - recievedBefore;

                    DataTransfer = $"{sent}kb  {recieved}kb";

                }
            }

        }



    }

}
