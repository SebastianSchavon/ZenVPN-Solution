using ZenVPN.Core;
using ZenVPN.MVVM.Model;
using ZenVPN.Services;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Diagnostics.Tracing.Session;
using Microsoft.Diagnostics.Tracing.Parsers;
using System.Net.NetworkInformation;

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

    private string _bytes;

    public string Bytes
    {
        get { return _bytes; }
        set 
        { 
            _bytes = value;
            OnPropertyChanged();
        }
    }


    public MainViewModel()
    {

        _service = new VPNService();

        SetConnectionStatus();

        Servers = new ObservableCollection<ServerModel>(_service.GetServers());

        MoveWindowCommand = new RelayCommand(o => { Application.Current.MainWindow.DragMove(); });

        ShutdownWindowCommand = new RelayCommand(o => { Application.Current.Shutdown(); });

        MinimizeWindowCommand = new RelayCommand(o => { Application.Current.MainWindow.WindowState = WindowState.Minimized; });

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

            Task.Run(() => _service.Disconnect()).ContinueWith(x => SetDisconnectStatus());
            
        });
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

                while (true)
                {
                    Thread.Sleep(2000);

                    statistics = Interface.GetIPv4Statistics();

                    sent = (statistics.BytesSent / 1024) - sentBefore;
                    recieved = (statistics.BytesReceived / 1024) - recievedBefore;

                    Bytes = $"Sent: {sent}kb / Recieved: {recieved}kb";
                }
            }
                
        }



    }
}
