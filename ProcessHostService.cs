using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Threading.Tasks;

namespace ServiceManager2
{
    partial class ProcessHostService : ServiceBase
    {
        private readonly ServiceConfiguration config;

        public ProcessHostService(ServiceConfiguration config)
        {
            InitializeComponent();

            this.config = config;
        }

        private Process process;

        protected override void OnStart(string[] args)
        {
            if (string.IsNullOrWhiteSpace(config.Executable))
            {
                throw new ArgumentException("Executable is not defined");
            }

            var commandLine = config.Executable.Split(new[] { ' ' }, 2);

            var exe = commandLine[0];
            var arguments = "";

            if (commandLine.Length > 1)
            {
                arguments = commandLine[1];
            }

            process = ServiceManager.RunProcess(exe, arguments);

            if (process != null)
            {
                StartLogging();
            }

            var exited = process?.WaitForExit(10 * 1000) ?? true;

            if (exited)
            {
                base.Stop();
            }
        }

        private void StartLogging()
        {
            if (!string.IsNullOrWhiteSpace(config.LogPath))
            {
                if ("console".Equals(config.LogPath, StringComparison.InvariantCultureIgnoreCase))
                {
                    StartReaderThread(new Tuple<StreamReader, TextWriter>(process.StandardOutput, Console.Out));
                }
                else
                {
                    RedirectOutputToFile(process, config.LogPath);
                }
            }
        }

        protected override void OnStop()
        {

        }

        public void Start()
        {
            OnStart(new string[0]);

            process?.WaitForExit();
        }

        private static void StartReaderThread(Tuple<StreamReader, TextWriter> args)
        {
            new Task(
                obj => ReadPipe((Tuple<StreamReader, TextWriter>)obj),
                args
            ).Start();
        }

        private void RedirectOutputToFile(Process process, string logPath)
        {
            var fileStream = new FileStream(logPath, FileMode.OpenOrCreate, FileAccess.Write);
            var writer = new StreamWriter(fileStream) { AutoFlush = true };

            StartReaderThread(new Tuple<StreamReader, TextWriter>(process.StandardOutput, writer));
        }

        private static void ReadPipe(Tuple<StreamReader, TextWriter> tuple)
        {
            while (true)
            {
                if (tuple.Item1.EndOfStream)
                {
                    return;
                }

                var str = tuple.Item1.Read();

                tuple.Item2.Write((char)str);
            }
        }
    }

    internal static class ServiceManager
    {
        public static void Install(ServiceConfiguration config)
        {
            RunProcess("sc", $"create {config.ServiceName} displayname= \"{config.DisplayName}\" binpath= \"{config.GenerateServiceBinPath()}\" start= auto").WaitForExit();

            var service = ServiceController.GetServices().FirstOrDefault(s => s.ServiceName == config.ServiceName);

            if (service != null && service.Status != ServiceControllerStatus.Running)
            {
                service.Start();
            }
        }

        public static void Uninstall(ServiceConfiguration config)
        {
            var service = ServiceController.GetServices().FirstOrDefault(s => s.ServiceName == config.ServiceName);

            if (service != null &&
                service.CanStop &&
                service.Status != ServiceControllerStatus.Stopped &&
                service.Status != ServiceControllerStatus.StopPending)
            {
                service.Stop();
            }

            RunProcess("sc", $"delete {config.ServiceName}").WaitForExit();
        }

        internal static Process RunProcess(string exe, string command)
        {
            return Process.Start(new ProcessStartInfo
            {
                FileName = exe,
                Arguments = command,
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true
            });
        }
    }
}
