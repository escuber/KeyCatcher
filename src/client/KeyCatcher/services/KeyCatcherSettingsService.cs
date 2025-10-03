using CommunityToolkit.Mvvm.ComponentModel;
using KeyCatcher.models;
using System.Text;
using System.Text.Json.Serialization;

namespace KeyCatcher.services
{
    public sealed class DeviceConfigDto

    {
        [JsonPropertyName("ssid")] public string? Ssid { get; set; }
        [JsonPropertyName("pss")] public string? Password { get; set; }
        [JsonPropertyName("in")] public string? In { get; set; }
        [JsonPropertyName("out")] public string? Out { get; set; }
        [JsonPropertyName("ap")] public bool Ap { get; set; }
        //[JsonPropertyName("creds")] public List<string>? Creds { get; set; }
        [JsonPropertyName("creds")]
        public List<WifiCredential>? Creds { get; set; }
        [JsonPropertyName("bflag")] public string? Bflag { get; set; }
    }
    public partial class KeyCatcherSettingsService : ObservableObject, INotifyPropertyChanged
    {
        //    public event PropertyChangedEventHandler PropertyChanged;


        public KeyCatcherSettingsService()
        {
            Load();
        }
        [ObservableProperty] public string sSID;
        //public string SSID
        //{
        //    get => ssid;
        //    set { ssid = value; OnPropertyChanged(); }
        //}

        [ObservableProperty] public string password;
        //public string Password
        //{
        //    get => password;
        //    set { password = value; OnPropertyChanged(); }
        //}

        [ObservableProperty] public bool apMode;
        //public bool APMode
        //{
        //    get => apMode;
        //    set { apMode = value; OnPropertyChanged(); }
        //}
        [ObservableProperty] public string inputType;
        //public string InputType
        //{
        //    get => inputType;
        //    set { inputType = value; OnPropertyChanged(); }
        //}
        [ObservableProperty] public string outputType;
        //public string OutputType
        //{
        //    get => outputType;
        //    set { outputType = value; OnPropertyChanged(); }
        //}


        public List<WifiCredential> creds { get; set; } = new List<WifiCredential>();
        //public string OutputType
        //{
        //    get => outputType;
        //    set { outputType = value; OnPropertyChanged(); }
        //}



        public void ApplyDeviceJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return;

            DeviceConfigDto? dto;
            try
            {
                dto = JsonSerializer.Deserialize<DeviceConfigDto>(
                    json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                );
            }
            catch
            {
                return;
            }
            if (dto is null) return;

            // Only update if not blank/empty/null
            if (!string.IsNullOrWhiteSpace(dto.Ssid)) SSID = dto.Ssid!;
            if (!string.IsNullOrWhiteSpace(dto.Password)) Password = dto.Password!;
            if (!string.IsNullOrWhiteSpace(dto.In)) InputType = dto.In!;
            if (!string.IsNullOrWhiteSpace(dto.Out)) OutputType = dto.Out!;
            ApMode = dto.Ap;

            if (dto.Creds != null && dto.Creds.Any())
            {
                creds.Clear();
                foreach (var item in dto.Creds)
                {
                    creds.Add(item);


                    //creds.Add(new WifiCredential
                    //{
                    //    SSID = item.Split(':')[0],
                    //    Password = item.Split(':').Length > 1 ? item.Split(':')[1] : ""
                    //});
                }
            }

            Save();
        }
        public void Load()
        {
            SSID = Preferences.Get("wifi_ssid", "");
            Password = Preferences.Get("wifi_password", "");
            ApMode = Preferences.Get("ap_mode", "true") == "true";
            InputType = Preferences.Get("inputType", "WIFI");
            OutputType = Preferences.Get("outputType", "USBHID");
            string credsJson = Preferences.Get("Creds", "[]");
            creds = JsonSerializer.Deserialize<List<WifiCredential>>(credsJson) ?? new List<WifiCredential>();


        }

        public void Save()
        {
            Preferences.Set("wifi_ssid", SSID ?? "");
            Preferences.Set("wifi_password", Password ?? "");
            Preferences.Set("ap_mode", ApMode ? "true" : "false");
            Preferences.Set("inputType", InputType ?? "WIFI");
            Preferences.Set("outputType", OutputType ?? "USBHID");
            Preferences.Set("Creds", JsonSerializer.Serialize(creds ?? new List<WifiCredential>()));

        }
        public List<string> InputSources { get; set; } = new() { "WIFI", "BLE" };
        public List<string> OutputTypes { get; set; } = new() { "BLEHID", "USBHID" };
        // protected void OnPropertyChanged([CallerMemberName] string prop = "") =>
        //   PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));

        public string MakeMessage()
        {

            string credsJson = JsonSerializer.Serialize(creds);
            var builder = new StringBuilder();


            Debug.WriteLine(JsonSerializer.Serialize(creds));


            builder.Append($"<setup>\n");
            builder.Append($"ssid:{SSID ?? "DADNET"}\n");
            builder.Append($"password:{Password ?? "4c4c4c4c"}\n");
            builder.Append($"input_source:{InputType ?? "WIFI"}\n");
            builder.Append($"output_source:{OutputType ?? "USBHID"} \n");
            builder.Append($"ap_mode:{(ApMode ? "true" : "false")}\n");
            builder.Append($"creds:{credsJson}\n");
            builder.Append($"<endsetup>");
            return builder.ToString();
            //return "";
        }
        public void SendUpdatedConfig()
        {
            var config = new
            {
                wifi_ssid = SSID,
                wifi_password = Password,
                ap_mode = ApMode ? "true" : "false",
                inputType = InputType,
                outputType = OutputType,
                creds = creds
            };
            var json = JsonSerializer.Serialize(config);
            var bytes = System.Text.Encoding.UTF8.GetBytes(json);
            // Send the configuration to the device via the appropriate method
            // This is a placeholder for actual sending logic
            // e.g., via Bluetooth, USB, etc.
        }

        // protected void OnPropertyChanged([CallerMemberName] string prop = "") =>
        //      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        // }

    }
}