using ZenVPN.Core;

namespace ZenVPN.MVVM.Model;

internal class ServerModel : ObservableObject
{
    public string Country { get; set; }
    public string Name { get; set; }
    public string Ip { get; set; }

    private string _foregroundColor;

    public string ForegroundColor
    {
        get { return _foregroundColor; }
        set 
        { 
            _foregroundColor = value;
            OnPropertyChanged();
        }
    }


    private long _ms;
    public long Ms
    {
        get { return _ms; }
        set 
        { 
            _ms = value;
            OnPropertyChanged();
        }
    }



}
