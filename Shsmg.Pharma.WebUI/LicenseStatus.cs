using System.ComponentModel;
namespace Shsmg.Pharma.WebUI;

public class LicenseStatus : INotifyPropertyChanged
{
    public bool IsValid
    {
        get;
        set
        {
            if (field != value)
            {
                field = value;
                OnPropertyChanged(nameof(IsValid));
            }
        }
    }

    public string Message
    {
        get;
        set
        {
            if (field != value)
            {
                field = value;
                OnPropertyChanged(nameof(Message));
            }
        }
    } = string.Empty;

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}