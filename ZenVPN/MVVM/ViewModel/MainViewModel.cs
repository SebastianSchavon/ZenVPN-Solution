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

namespace ZenVPN.MVVM.ViewModel;

internal class MainViewModel : ObservableObject
{
    IViewModelService _service;

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
        ConnectionStatus = "Connecting";

        Task.Run(() => _service.Connect(SelectedServer)).ContinueWith(x => SetConnectStatus(8, "Retrying")).ContinueWith(x =>
        {
            if (ConnectionStatus == $"Connected to {SelectedServer.Name}")
            {
                SetServerForeground(SelectedServer);
                MonitorBandWidth();
            }

            if (ConnectionStatus == "Retrying")
            {
                Task.Run(() => {
                    _service.Disconnect();

                    Thread.Sleep(1000);

                    _service.Connect(SelectedServer);

                }).ContinueWith(x => SetConnectStatus(8, "Something went wrong. Retrying")).ContinueWith(x =>
                {
                    if (ConnectionStatus == $"Connected to {SelectedServer.Name}")
                    {
                        SetServerForeground(SelectedServer);
                        MonitorBandWidth();
                    }

                    if (ConnectionStatus == "Something went wrong. Retrying")
                    {
                        Task.Run(() => {
                            _service.Disconnect();

                            Thread.Sleep(1000);

                            _service.Connect(SelectedServer);

                        }).ContinueWith(x => SetConnectStatus(8, "Disconnected")).ContinueWith(x =>
                        {
                            if (ConnectionStatus == $"Connected to {SelectedServer.Name}")
                            {
                                SetServerForeground(SelectedServer);
                                MonitorBandWidth();
                            }


                        });
                    }

                });
            }

        });
    }

    private void DisconnectMethod()
    {
        ConnectionStatus = "Disconnecting";

        Task.Run(() => _service.Disconnect())
        .ContinueWith(x => SetDisconnectStatus()).ContinueWith(x =>
        {
            if (ConnectionStatus == "Disconnected")
            {
                SetServerForeground(SelectedServer);

                Thread.Sleep(3000);

                SetDataTransfer("0kb  0kb");
            }
        });
    }

    private void SetServerForeground(ServerModel sm)
    {
        if(ConnectionStatus.Contains("Connected to"))
        {
            var server = Servers.FirstOrDefault(x => x.Name == sm.Name);

            if(server != null)
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
        int count = 0;

        for (int i = 0; i < 8; i++)
        {
            count++;
            ConnectionStatus += ".";

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
                
        }

        ConnectionStatus = "Something went wrong...";

    }
    public void SetConnectStatus(int timer, string errorStatus)
    {
        int count = 0;

        for (int i = 0; i < timer; i++)
        {
            count++;
            ConnectionStatus += ".";

            if (count >= 4)
            {
                ConnectionStatus = ConnectionStatus.Substring(0, ConnectionStatus.Length - count);
                count = 0;
            }

            Thread.Sleep(1000);

            if (NetworkUtil.CheckForVPNInterface())
            {
                ConnectionStatus = $"Connected to {SelectedServer.Name}";
                return;
            }
                
        }

        ConnectionStatus = errorStatus;

    }

    private async void SetServerPing()
    {
        while (true)
        {
            Thread.Sleep(3000);

            foreach(var server in Servers)
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
