using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace KeyCatcher_acc.models
{
    public class WifiCredential
    {
        [JsonPropertyName("ssid")]
        public string SSID { get; set; }
        [JsonPropertyName("password")]
        public string Password { get; set; }
    }
}
