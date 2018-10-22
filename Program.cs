using System;
using System.Reflection;
using System.ServiceProcess;

namespace ServiceManager2
{
    public enum ServiceAction
    {
        Install,
        Uninstall,
        Start,
        Run
    }

    public class ServiceConfiguration
    {
        public string Executable { get; set; }

        public string DisplayName { get; internal set; }

        public string ServiceName { get; internal set; }

        public ServiceAccount Account { get; internal set; }

        public string UserName { get; internal set; }

        public string Password { get; internal set; }

        public ServiceAction Action { get; set; } = ServiceAction.Start;

        public string LogPath { get; set; }

        internal string GenerateServiceBinPath()
        {
            var bin = Assembly.GetExecutingAssembly().Location;

            var command = $"{bin} -exe \\\"{Executable}\\\"";

            if (!string.IsNullOrWhiteSpace(LogPath))
            {
                command += $" -log \\\"{LogPath}\\\"";
            }

            return command;
        }

        internal static ServiceConfiguration Parse(string[] args)
        {
            if (args.Length == 0 || args.Length % 2 != 0)
            {
                return null;
            }

            var config = new ServiceConfiguration();

            for (var i = 0; i < args.Length; i += 2)
            {
                var val = args[i + 1];

                switch (args[i])
                {
                    case "-exe":
                        config.Executable = val;
                        break;
                    case "-dn":
                        config.DisplayName = val;
                        break;
                    case "-sn":
                        config.ServiceName = val;
                        break;
                    case "-ac":
                        config.Account = (ServiceAccount)Enum.Parse(typeof(ServiceAccount), val);
                        break;
                    case "-u":
                        config.UserName = val;
                        break;
                    case "-p":
                        config.Password = val;
                        break;
                    case "-action":
                        config.Action = (ServiceAction)Enum.Parse(typeof(ServiceAction), val);
                        break;
                    case "-log":
                        config.LogPath = val;
                        break;
                }
            }

            return config;
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            var config = ServiceConfiguration.Parse(args);

            if (config == null)
            {
                ShowHelp();
                return;
            }

            switch (config.Action)
            {
                case ServiceAction.Install:
                    InstallService(config);
                    break;
                case ServiceAction.Uninstall:
                    UninstallService(config);
                    break;

                case ServiceAction.Run:
                    RunService(config);
                    break;

                case ServiceAction.Start:
                default:
                    StartService(config);
                    break;
            }
        }

        private static void RunService(ServiceConfiguration config)
        {
            var service = new ProcessHostService(config);

            service.Start();
        }

        private static void StartService(ServiceConfiguration config)
        {
            ServiceBase.Run(new ProcessHostService(config));
        }

        private static void ShowHelp()
        {
            Console.WriteLine("Help...");
        }

        private static void InstallService(ServiceConfiguration config)
        {
            ServiceManager.Install(config);
        }

        private static void UninstallService(ServiceConfiguration config)
        {
            ServiceManager.Uninstall(config);
        }
    }
}
