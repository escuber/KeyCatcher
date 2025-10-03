using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KeyCatcher.models;
using KeyCatcher.services;
using System.Reflection;
using System.Text.Json;

namespace KeyCatcher.ViewModels
{
    public partial class WifiCredsViewModel : ObservableObject
    {
        [ObservableProperty]
        private string primarySSID = "";

        [ObservableProperty]
        private string primaryPassword = "";

        [ObservableProperty]
        private ObservableCollection<WifiCredential> networks = new();

        [ObservableProperty]
        private WifiCredential editingNetwork = new();

        [ObservableProperty]
        private bool isEditing = false;


        bool _isBusy;
        public bool IsBusy
        {
            get => _isBusy;
            set { _isBusy = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        void OnPropertyChanged([CallerMemberName] string name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        public CommHub Hub { get; }
        public readonly KeyCatcherSettingsService _settings;

        public WifiCredsViewModel(KeyCatcherSettingsService settings, CommHub hub )
        {
            Hub = hub;

            _settings = settings;
            SaveCommand = new AsyncRelayCommand(SaveAsync);

        }

        public IAsyncRelayCommand SaveCommand { get; }

        // Raise an event so the view can close itself
        public event EventHandler? SaveSucceeded;

        public async Task SaveAsync()
        {
            _settings.Save();
            SaveSucceeded?.Invoke(this, EventArgs.Empty);
        }

        // Load current settings into the VM

        public void InitFromService(KeyCatcherSettingsService svc)
        {
            PrimarySSID = svc.SSID ?? "";
            PrimaryPassword = svc.Password ?? "";

            Networks.Clear();
            if (svc.creds != null)
            {
                foreach (var n in svc.creds)
                    Networks.Add(n);
            }
        }
        //public void InitFromService(KeyCatcherSettingsService svc)
        //{
        //    //PrimarySSID = svc.SSID ?? "";
        //    //PrimaryPassword = svc.Password ?? "";

        //    //if (string.IsNullOrWhiteSpace(svc.creds))
        //    //    return;

        //    //var opt = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        //    //try
        //    //{
        //    //    // Preferred shape: [{ "ssid": "...", "password": "..." }] or "SSID"/"Password"
        //    //    var list = new List<string>; //null;// JsonSerializer.Deserialize<List<WifiCredential>>(svc.creds, opt) ?? new();
        //    //    foreach (var n in list) Networks.Add(n);
        //    //    return;
        //    //}
        //    //catch { /* fall through */ }

        //    //try
        //    //{
        //    //    // Legacy shape: ["ssid1","ssid2",...]
        //    //    var names = JsonSerializer.Deserialize<List<string>>(svc.creds, opt) ?? new();
        //    //    foreach (var s in names) Networks.Add(new WifiCredential { SSID = s, Password = "" });
        //    //}
        //    //catch
        //    //{
        //    //    // Bad/unknown shape – start empty
        //    //}

        //    //Networks.Clear();
        //    //try
        //    //{
        //    //    var list = string.IsNullOrWhiteSpace(svc.Creds)
        //    //        ? new List<WifiCredential>()
        //    //        : JsonSerializer.Deserialize<List<WifiCredential>>(svc.Creds) ?? new List<WifiCredential>();

        //    //    list.Add(new WifiCredential { SSID = "sssid1", Password = "pass1"});

        //    //    foreach (var n in list)
        //    //        Networks.Add(n);
        //    //}
        //    //catch
        //    //{
        //    //    // If deserialize fails, start fresh
        //    //}
        //}





        // Push VM changes back into the service
        public void ApplyToService(KeyCatcherSettingsService svc)
        {
            svc.SSID = PrimarySSID ?? "";
            svc.Password = PrimaryPassword ?? "";

            var list = Networks?.ToList() ?? new List<WifiCredential>();
            svc.creds = list;


            //// Save with a predictable naming policy (and our loader is case-insensitive anyway)
            //svc.creds = JsonSerializer.Serialize(list, new JsonSerializerOptions
            //{
            //    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            //});

            svc.Save();
        }
        // Commands
        [RelayCommand]
        public void AddNetwork()
        {
            EditingNetwork = new WifiCredential();
            IsEditing = true;
        }

        [RelayCommand]
        public void EditNetwork(WifiCredential network)
        {
            if (network == null) return;
            EditingNetwork = new WifiCredential { SSID = network.SSID, Password = network.Password };
            IsEditing = true;
        }

        [RelayCommand]
        public void SaveNetwork()
        {
            if (string.IsNullOrWhiteSpace(EditingNetwork?.SSID))
            {
                IsEditing = false;
                EditingNetwork = new WifiCredential();
                return;
            }

            var existing = Networks.FirstOrDefault(n => n.SSID == EditingNetwork.SSID);
            if (existing == null)
                Networks.Add(new WifiCredential { SSID = EditingNetwork.SSID, Password = EditingNetwork.Password });
            else
            {
                var idx = Networks.IndexOf(existing);
                Networks[idx] = new WifiCredential { SSID = EditingNetwork.SSID, Password = EditingNetwork.Password };
            }

            IsEditing = false;
            EditingNetwork = new WifiCredential();
        }

        [RelayCommand]
        public async Task SaveAndClose()
        {
            try
            {
                
                IsBusy = true;   // show spinner
                                 //VM.ApplyToService(_settings);


                ApplyToService(_settings);
                _settings.Save();
                if (Hub != null && Hub.IsAnyUp)
                {
                    var payload = _settings.MakeMessage();
                    await Hub.SendAsync(payload);
                }
                //await close();


                //IsEditing = false;

                //if (Hub is not null)
                //{
                //    var payload = _settings.MakeMessage();
                //    //try { 
                //    //await Hub.ap(payload, 20000);
                //    //}
                //    //catch { /* ignore transport error here */ }
                //}
            }
            finally
            {
                IsBusy = false;  // hide spinner
                
                
                
            }
        }

        // Promote a backup to primary
        [RelayCommand]


        //[RelayCommand]
        public void MakePrimary(WifiCredential tapped)
        {
            if (tapped is null) return;

            var oldPrimary = new WifiCredential { SSID = PrimarySSID, Password = PrimaryPassword };

            // Update primary
            PrimarySSID = tapped.SSID ?? "";
            PrimaryPassword = tapped.Password ?? "";

            // Remove the tapped item from backups
            var idx = Networks.IndexOf(tapped);
            if (idx >= 0) Networks.RemoveAt(idx);

            // Put the old primary back into backups if it has a name
            if (!string.IsNullOrWhiteSpace(oldPrimary.SSID))
            {
                // dedupe by SSID
                var dup = Networks.FirstOrDefault(n => string.Equals(n.SSID, oldPrimary.SSID, StringComparison.OrdinalIgnoreCase));
                if (dup != null) Networks.Remove(dup);

                Networks.Insert(0, oldPrimary);

                // cap to 4 backups
                while (Networks.Count > 4)
                    Networks.RemoveAt(Networks.Count - 1);
            }
        }
    }
}