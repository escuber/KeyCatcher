using CommunityToolkit.Mvvm.ComponentModel;
using System.Text.Json.Serialization;

namespace KeyCatcher.models
{
    public class WifiCredential : ObservableObject, INotifyPropertyChanged
    {
        [JsonPropertyName("ssid")]
        public string SSID { get; set; }
        [JsonPropertyName("password")]
        public string Password { get; set; }
    }
}
