using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace KeyCatcher.models
{
    public class WifiCredential : ObservableObject, INotifyPropertyChanged
    {
        [JsonPropertyName("ssid")]
        [ObservableProperty] public string SSID { get; set; }
        [JsonPropertyName("password")]
        [ObservableProperty] public string Password { get; set; }
    }
}
