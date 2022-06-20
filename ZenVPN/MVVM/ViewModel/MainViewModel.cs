using ZenVPN.Core;
using ZenVPN.MVVM.Model;
using ZenVPN.Services;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Net.NetworkInformation;
using System.Diagnostics;
using System.Windows.Threading;
using System;

namespace ZenVPN.MVVM.ViewModel;

internal class MainViewModel : ObservableObject
{
    IVPNService _service;

    public RelayCommand MoveWindowCommand { get; set; }
    public RelayCommand ShutdownWindowCommand { get; set; }
    public RelayCommand MinimizeWindowCommand { get; set; }

    public RelayCommand ConnectCommand { get; set; }
    public RelayCommand DisconnectCommand { get; set; }

    public ObservableCollection<ServerModel> Servers { get; set; }

    //private ObservableCollection<ServerModel> _servers;

    //public ObservableCollection<ServerModel> Servers
    //{
    //    get { return _servers; }
    //    set 
    //    { 
    //        _servers = value;
    //        OnPropertyChanged();
    //    }
    //}


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

        _service = new VPNService();

        SetConnectionStatus();

        SetDataTransfer("0kb   0kb");

        Servers = new ObservableCollection<ServerModel>(_service.GetServers());

        MoveWindowCommand = new RelayCommand(o => { Application.Current.MainWindow.DragMove(); });

        ShutdownWindowCommand = new RelayCommand(o => { _service.Disconnect(); Application.Current.Shutdown(); ; });

        MinimizeWindowCommand = new RelayCommand(o => { Application.Current.MainWindow.WindowState = WindowState.Minimized; });

        Task.Run(() => SetServerPing());

        ConnectCommand = new RelayCommand(o =>
        {
            if (_service.CheckForVPNInterface())
                return;

            if (SelectedServer == null)
            {
                ConnectionStatus = "SELECT SERVER";
                return;
            }

            ConnectionStatus = "Connecting...";

            Task.Run(() => _service.Connect(SelectedServer)).ContinueWith(x => SetConnectStatus()).ContinueWith(x => MonitorBandWidth());


        });

        DisconnectCommand = new RelayCommand(o =>
        {

            if (!_service.CheckForVPNInterface())
                return;

            ConnectionStatus = "Disconnecting...";

            Task.Run(() => _service.Disconnect()).ContinueWith(x => SetDisconnectStatus()).ContinueWith(x => SetDataTransfer("0kb  0kb"));

        });
    }

    private void SetDataTransfer(string value)
    {
        DataTransfer = value;
    }

    private void SetConnectionStatus()
    {
        if (_service.CheckForVPNInterface())
            ConnectionStatus = "Connected";
        else
            ConnectionStatus = "Disconnected";
    }

    private void SetConnectStatus()
    {
        ConnectionStatus = _service.SetConnectStatus(SelectedServer);
    }

    private void SetDisconnectStatus()
    {
        ConnectionStatus = _service.SetDisconnectStatus();
    }

    private async void SetServerPing()
    {
        while (true)
        {
            Thread.Sleep(3000);
            foreach(var server in Servers)
            {
                server.Ms = _service.PingServerIp(server.Ip);
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
