using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KeyCatcher.services;
using KeyCatcher.models;
using KeyCatcher.services;
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

        // Load current settings into the VM
        public void InitFromService(KeyCatcherSettingsService svc)
        {
            PrimarySSID = svc.SSID ?? "";
            PrimaryPassword = svc.Password ?? "";

            Networks.Clear();
            try
            {
                //var list = string.IsNullOrWhiteSpace(svc.Creds)
                //    ? new List<WifiCredential>()
                //    : JsonSerializer.Deserialize<List<WifiCredential>>(svc.Creds) ?? new List<WifiCredential>();

                //foreach (var n in list)
                //    Networks.Add(n);

                PrimarySSID = svc.SSID ?? "";
                PrimaryPassword = svc.Password ?? "";

                Networks.Clear();
                // Just use the List directly
                if (svc.Creds != null)
                {
                    foreach (var n in svc.Creds)
                        Networks.Add(n);
                }
            }
            catch
            {
                // If deserialize fails, start fresh
            }
        }

        // Push VM changes back into the service
        public void ApplyToService(KeyCatcherSettingsService svc)
        {
            svc.SSID = PrimarySSID ?? "";
            svc.Password = PrimaryPassword ?? "";

            var list = Networks?.ToList() ?? new List<WifiCredential>();
            svc.Creds = list;// JsonSerializer.Serialize(list);

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
        public void RemoveNetwork(WifiCredential net)
        {
            if (net == null) return;
            Networks.Remove(net);
        }

        // Promote a backup to primary
        [RelayCommand]
        public void MakePrimary(WifiCredential net)
        {
            if (net == null) return;
            PrimarySSID = net.SSID;
            PrimaryPassword = net.Password;
        }
    }
}