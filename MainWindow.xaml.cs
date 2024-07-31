using Microsoft.Win32;
using System.Diagnostics;
using System.Management;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace Monitoring
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            //Récupération des infos du PC
            GetAllSystemInfos();

            //Timer màj des infos
            DispatcherTimer timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(0.75);
            timer.Tick += timer_Tick;
            timer.Start();
        }

        //Fonction timer
        void timer_Tick(object sender, EventArgs e)
        {
            //Màj infos CPU
            cpu.Content = RefreshCpuInfos();
            //Màj infos RAM
            RefreshRamInfos();
        }

        public void RefreshRamInfos()
        {
            ramTotal.Content = "Total : " + FormatSize(GetTotalPhys());
            ramUsed.Content = "Utilisé : " + FormatSize(GetUsedPhys());
            ramFree.Content = "Disponible : " + FormatSize(GetAvailPhys());
        }

        public string RefreshCpuInfos()
        {
            PerformanceCounter cpuCounter = new PerformanceCounter();
            cpuCounter.CategoryName = "Processor";
            cpuCounter.CounterName = "% Processor Time";
            cpuCounter.InstanceName = "_Total";

            dynamic firstVal = cpuCounter.NextValue(); //valeur toujours à 0
            System.Threading.Thread.Sleep(50);
            dynamic val = cpuCounter.NextValue(); // valeur réelle

            //Rotation de l'aiguille
            RotateTransform rotateTransform = new RotateTransform((val * 2.7f) -90);
            imgAiguille.RenderTransform = rotateTransform;

            decimal roundVal = Convert.ToDecimal(val);
            roundVal = Math.Round(roundVal, 2);

            return roundVal + " %";

        }

        // Travailler avec la mémoire (RAM)
        #region Fonctions spécifiques à la RAM
        [DllImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GlobalMemoryStatusEx(ref MEMORY_INFO mi);

        // Structure de l'info de la mémoire
        [StructLayout(LayoutKind.Sequential)]
        public struct MEMORY_INFO
        {
            public uint dwLength; // Taille structure
            public uint dwMemoryLoad; // Utilisation mémoire
            public ulong ullTotalPhys; // Mémoire physique totale
            public ulong ullAvailPhys; // Mémoire physique dispo
            public ulong ullTotalPageFile;
            public ulong ullAvailPageFile;
            public ulong ullTotalVirtual; // Taille mémoire virtuelle
            public ulong ullAvailVirtual; // Mémoire virtuelle dispo
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

        // Récupération mémoire physique totale dispo
        public static ulong GetAvailPhys()
        {
            MEMORY_INFO mi = GetMemoryStatus();
            return mi.ullAvailPhys;
        }

        // Récupération mémoire utilisée
        public static ulong GetUsedPhys()
        {
            MEMORY_INFO mi = GetMemoryStatus();
            return (mi.ullTotalPhys - mi.ullAvailPhys);
        }

        // Récup la mémoire physique totale
        public static ulong GetTotalPhys()
        {
            MEMORY_INFO mi = GetMemoryStatus();
            return mi.ullTotalPhys;
        }
        #endregion

        public void GetAllSystemInfos()
        {
            SystemInfo si = new SystemInfo();
            osName.Content = si.GetOsInfos("os");
            osArch.Content = si.GetOsInfos("arch");
            procName.Content = si.GetCpuInfos();
            gpuName.Content = si.GetGpuInfos();
        }

        private void infoMsg_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "https://github.com/YvanGui19",
                UseShellExecute = true
            };
            System.Diagnostics.Process.Start(psi);
        }
    }

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
                return processor_name.GetValue("ProcessorNameString").ToString();
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
                    return obj["Name"].ToString();
                }
            }
            return "";
        }

    }
}