using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ScreenshotPage;

public sealed class ViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    public string RegistrationCode
    {
        get;
        set => Set(ref field, value);
    } = "";

    public string Status
    {
        get;
        set => Set(ref field, value);
    } = "Not registered";

    public void Register() => Status = $"Registered: {RegistrationCode}";

    void Set(ref string field, string value, [CallerMemberName] string? name = null)
    {
        if (field == value) return;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
