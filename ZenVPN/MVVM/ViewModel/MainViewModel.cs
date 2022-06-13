using ZenVPN.Core;
using ZenVPN.MVVM.Model;
using ZenVPN.Services;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

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
            if (SelectedServer == null)
            {
                ConnectionStatus = "SELECT SERVER";
                return;
            }

            ConnectionStatus = "Connecting...";

            Task.Run(() => _service.Connect(SelectedServer)).ContinueWith(x => SetConnectionStatus());

        });

        DisconnectCommand = new RelayCommand(o =>
        {
            ConnectionStatus = "Disconnecting...";

            Task.Run(() => _service.Disconnect()).ContinueWith(x => SetConnectionStatus());
        });
    }



    private void SetConnectionStatus()
    {
        if (_service.CheckForVPNInterface())
            ConnectionStatus = "Connected";
        else
            ConnectionStatus = "Disconnected";
    }
}
