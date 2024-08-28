using Microsoft.Win32;
using System.Diagnostics;
using System.IO;
using System.Management;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace Monitoring.MVVM.View
{
    public partial class InfoView : UserControl
    {
        public InfoView()
        {
            InitializeComponent();

            //Récupération des infos du PC
            GetAllSystemInfos();

            //Récupération infos disques
            GetDrivesInfos();

            //Timer màj des infos
            DispatcherTimer timer = new()
            {
                Interval = TimeSpan.FromSeconds(0.75)
            };
            timer.Tick += Timer_Tick;
            timer.Start();
        }

        //Fonction timer
        void Timer_Tick(object sender, EventArgs e)
        {
            //MÀJ infos CPU
            cpu.Content = RefreshCpuInfos();
            //MÀJ infos RAM
            RefreshRamInfos();
            //MÀJ infos Température
            RefreshTempInfos(this);
            //MÀJ infos Réseaux
            RefreshNetworkInfos(this);
        }

        //Liste des disques
        public void GetDrivesInfos()
        {
            DriveInfo[] allDrives = DriveInfo.GetDrives();
            List<Disque> disques = new List<Disque>();

            foreach (DriveInfo info in allDrives)
            {
                if (info.IsReady == true)
                {
                    Console.WriteLine("Disque " + info.Name + " prêt !");
                }
                disques.Add(new Disque(info.Name, info.DriveFormat, FormatBytes(info.TotalSize), FormatBytes(info.AvailableFreeSpace)));
            }

            listeDisques.ItemsSource = disques;
        }

        private static string FormatBytes(long bytes)
        {
            string[] Suffix = ["B", "KB", "MB", "GB", "TB"];
            int i;
            double dblSByte = bytes;
            for (i = 0; i < Suffix.Length && bytes >= 1024; i++, bytes /= 1024)
            {
                dblSByte = bytes / 1024.0;
            }
            return String.Format("{0:0.##} {1}", dblSByte, Suffix[i]);
        }

        //Activité Réseaux
        public static void RefreshNetworkInfos(InfoView infoView)
        {
            if (!NetworkInterface.GetIsNetworkAvailable())
                return;

            NetworkInterface[] interfaces = NetworkInterface.GetAllNetworkInterfaces();

            foreach (NetworkInterface ni in interfaces)
            {
                //Envoyé
                if (ni.GetIPv4Statistics().BytesSent > 0)
                    infoView.netMont.Content = ni.GetIPv4Statistics().BytesSent / 1000 + " KB";
                //Reçu
                if (ni.GetIPv4Statistics().BytesReceived > 0)
                    infoView.netDes.Content = ni.GetIPv4Statistics().BytesReceived / 1000 + " KB";
            }
        }

        //Actualisation des données de la température
        public static void RefreshTempInfos(InfoView infoView)
        {
            Double temperature = 0;
            String instanceName = "";

            ManagementObjectSearcher searcher = new ManagementObjectSearcher(@"root\WMI", "SELECT * FROM MSAcpi_ThermalZoneTemperature");
            foreach (ManagementObject obj in searcher.Get())
            {
                instanceName = obj["InstanceName"].ToString();
                if (instanceName.Contains("CPUZ_0"))
                {
                    temperature = Convert.ToDouble(obj["CurrentTemperature"].ToString());
                    temperature = (temperature - 2732) / 10.0;
                }
            }
            infoView.temp.Content = temperature + "°C";
        }


        //Actualisation des données de la RAM
        public void RefreshRamInfos()
        {
            ramTotal.Content = "Total : " + FormatSize(GetTotalPhys());
            ramUsed.Content = "Utilisé : " + FormatSize(GetUsedPhys());
            ramFree.Content = "Disponible : " + FormatSize(GetAvailPhys());

            string[] maxVal = FormatSize(GetTotalPhys()).Split(' ');
            barRam.Maximum = float.Parse(maxVal[0]);
            string[] memVal = FormatSize(GetUsedPhys()).Split(' ');
            barRam.Value = float.Parse(memVal[0]); ;
        }
        public string RefreshCpuInfos()
        {
            PerformanceCounter cpuCounter = new PerformanceCounter();
            cpuCounter.CategoryName = "Processor";
            cpuCounter.CounterName = "% Processor Time";
            cpuCounter.InstanceName = "_Total";

            dynamic firstVal = cpuCounter.NextValue(); //valeur toujours à 0
            System.Threading.Thread.Sleep(50);
            dynamic val = cpuCounter.NextValue(); //valeur réelle

            // Créez une instance de RotateTransform
            RotateTransform rotateTransform = imgAiguille.RenderTransform as RotateTransform;
            if (rotateTransform == null)
            {
                rotateTransform = new RotateTransform();
                imgAiguille.RenderTransform = rotateTransform;
                imgAiguille.RenderTransformOrigin = new Point(0.729, 0.736);
            }

            double angle = (val * 2.7f) - 90;

            // Créez une animation pour la propriété Angle de RotateTransform
            DoubleAnimation animation = new DoubleAnimation();
            animation.From = rotateTransform.Angle;
            animation.To = angle;
            animation.Duration = new Duration(TimeSpan.FromMilliseconds(1000)); // Augmentez la durée
            animation.AccelerationRatio = 0.5; // Accélération au début
            animation.DecelerationRatio = 0.5; // Décélération plus lente

            // Utilisez une fonction d'interpolation pour adoucir la transition
            //animation.EasingFunction = new QuinticEase { EasingMode = EasingMode.EaseInOut };

            // Appliquez l'animation à la propriété Angle de RotateTransform
            rotateTransform.BeginAnimation(RotateTransform.AngleProperty, animation);

            //Valeur en %
            decimal roundVal = Convert.ToDecimal(val);
            roundVal = Math.Round(roundVal, 2);
            return roundVal + " %";
        }


        //Travailler avec la mémoire (RAM)
        #region Fonctions spécifiques à la RAM
        [DllImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GlobalMemoryStatusEx(ref MEMORY_INFO mi);

        //Structure de l'info de la mémoire
        [StructLayout(LayoutKind.Sequential)]
        public struct MEMORY_INFO
        {
            public uint dwLength; //Taille structure
            public uint dwMemoryLoad; //Utilisation mémoire
            public ulong ullTotalPhys; //Mémoire physique totale
            public ulong ullAvailPhys; //Mémoire physique dispo
            public ulong ullTotalPageFile;
            public ulong ullAvailPageFile;
            public ulong ullTotalVirtual; //Taille mémoire virtuelle
            public ulong ullAvailVirtual; //Mémoire virtuelle dispo
            public ulong ullAvailExtendedVirtual;
        }

        static string FormatSize(double size)
        {
            double d = (double)size;
            int i = 0;
            while ((d > 1024) && (i < 5))
            {
                d /= 1024;
                i++;
            }
            string[] unit = { "B", "KB", "MB", "GB", "TB" };
            return (string.Format("{0} {1}", Math.Round(d, 2), unit[i]));
        }

        public static MEMORY_INFO GetMemoryStatus()
        {
            MEMORY_INFO mi = new MEMORY_INFO();
            mi.dwLength = (uint)Marshal.SizeOf(mi);
            GlobalMemoryStatusEx(ref mi);
            return mi;
        }

        //Récupération mémoire physique totale dispo
        public static ulong GetAvailPhys()
        {
            MEMORY_INFO mi = GetMemoryStatus();
            return mi.ullAvailPhys;
        }

        //Récupération mémoire utilisée
        public static ulong GetUsedPhys()
        {
            MEMORY_INFO mi = GetMemoryStatus();
            return (mi.ullTotalPhys - mi.ullAvailPhys);
        }

        //Récup la mémoire physique totale
        public static ulong GetTotalPhys()
        {
            MEMORY_INFO mi = GetMemoryStatus();
            return mi.ullTotalPhys;
        }
        #endregion

        /// <summary>
        /// Informations système
        /// </summary>
        public void GetAllSystemInfos()
        {
            SystemInfo si = new SystemInfo();
            osName.Content = si.GetOsInfos("os");
            osArch.Content = si.GetOsInfos("arch");
            procName.Content = si.GetCpuInfos();
            gpuName.Content = si.GetGpuInfos();
        }
    }

    /// <summary>
    ///Informations générales du système 
    /// </summary>
    public class SystemInfo
    {
        //Récupération infos OS
        public string GetOsInfos(string param)
        {
            ManagementObjectSearcher mos = new ManagementObjectSearcher("select * from Win32_OperatingSystem");
            foreach (ManagementObject mo in mos.Get())
            {
                switch (param)
                {
                    case "os":
                        return mo["Caption"].ToString();
                    case "arch":
                        return mo["OSArchitecture"].ToString();
                    case "osv":
                        return mo["CSDVersion"].ToString();
                }
            }
            return "";
        }

        //Récupération infos CPU
        public string GetCpuInfos()
        {
            RegistryKey processor_name = Registry.LocalMachine.OpenSubKey(@"Hardware\Description\System\CentralProcessor\0", RegistryKeyPermissionCheck.ReadSubTree);

            if (processor_name != null)
            {
                return "CPU : " + processor_name.GetValue("ProcessorNameString").ToString();
            }

            return "";
        }

        //Récupération infos GPU
        public string GetGpuInfos()
        {
            using (var searcher = new ManagementObjectSearcher("select * from Win32_VideoController"))
            {
                foreach (ManagementObject obj in searcher.Get())
                {
                    return "GPU : " + obj["Name"].ToString();
                }
            }
            return "";
        }

    }

    /// <summary>
    ///Informations des disques
    /// </summary>
    public class Disque
    {
        private string name;
        private string format;
        private string totalSpace;
        private string freeSpace;

        public Disque(string n, string f, string t, string fs)
        {
            name = n;
            format = f;
            totalSpace = t;
            freeSpace = fs;
        }

        public override string ToString()
        {
            return name + " (" + format + ") " + freeSpace + " libres /" + totalSpace;
        }

    }

}
