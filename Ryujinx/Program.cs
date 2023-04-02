using Gtk;
using Ryujinx.Common;
using Ryujinx.Common.Configuration;
using Ryujinx.Common.GraphicsDriver;
using Ryujinx.Common.Logging;
using Ryujinx.Common.SystemInfo;
using Ryujinx.Common.SystemInterop;
using Ryujinx.Modules;
using Ryujinx.SDL2.Common;
using Ryujinx.Ui;
using Ryujinx.Ui.Common;
using Ryujinx.Ui.Common.Configuration;
using Ryujinx.Ui.Common.Helper;
using Ryujinx.Ui.Widgets;
using SixLabors.ImageSharp.Formats.Jpeg;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Ryujinx
{
    partial class Program
    {
        public static double WindowScaleFactor { get; private set; }

        public static string Version { get; private set; }

        public static string ConfigurationPath { get; set; }

        public static string CommandLineProfile { get; set; }

        private const string X11LibraryName = "libX11";

        [LibraryImport(X11LibraryName)]
        private static partial int XInitThreads();

        [LibraryImport("user32.dll", SetLastError = true)]
        public static partial int MessageBoxA(IntPtr hWnd, [MarshalAs(UnmanagedType.LPStr)] string text, [MarshalAs(UnmanagedType.LPStr)] string caption, uint type);

        [LibraryImport("libc", SetLastError = true)]
        private static partial int setenv([MarshalAs(UnmanagedType.LPStr)] string name, [MarshalAs(UnmanagedType.LPStr)] string value, int overwrite);

        [LibraryImport("libc")]
        private static partial IntPtr getenv([MarshalAs(UnmanagedType.LPStr)] string name);

        private const uint MB_ICONWARNING = 0x30;

        static Program()
        {
            if (OperatingSystem.IsLinux())
            {
                NativeLibrary.SetDllImportResolver(typeof(Program).Assembly, (name, assembly, path) =>
                {
                    if (name != X11LibraryName)
                    {
                        return IntPtr.Zero;
                    }

                    if (!NativeLibrary.TryLoad("libX11.so.6", assembly, path, out IntPtr result))
                    {
                        if (!NativeLibrary.TryLoad("libX11.so", assembly, path, out result))
                        {
                            return IntPtr.Zero;
                        }
                    }

                    return result;
                });
            }
        }

        static void Main(string[] args)
        {
            Version = ReleaseInformation.GetVersion();

            if (OperatingSystem.IsWindows() && !OperatingSystem.IsWindowsVersionAtLeast(10, 0, 17134))
            {
                MessageBoxA(IntPtr.Zero, "你正在运行一个过时的Windows 版本。\n\n从20226月1日起，Ryujinx将只支持Windows 10 1809及更新版本。\n", $"Ryujinx {Version}", MB_ICONWARNING);
            }

            // Parse arguments
            CommandLineState.ParseArguments(args);

            // Hook unhandled exception and process exit events.
            GLib.ExceptionManager.UnhandledException   += (GLib.UnhandledExceptionArgs e)                => ProcessUnhandledException(e.ExceptionObject as Exception, e.IsTerminating);
            AppDomain.CurrentDomain.UnhandledException += (object sender, UnhandledExceptionEventArgs e) => ProcessUnhandledException(e.ExceptionObject as Exception, e.IsTerminating);
            AppDomain.CurrentDomain.ProcessExit        += (object sender, EventArgs e)                   => Exit();

            // Make process DPI aware for proper window sizing on high-res screens.
            ForceDpiAware.Windows();
            WindowScaleFactor = ForceDpiAware.GetWindowScaleFactor();

            // Delete backup files after updating.
            Task.Run(Updater.CleanupUpdate);

            // NOTE: GTK3 doesn't init X11 in a multi threaded way.
            // This ends up causing race condition and abort of XCB when a context is created by SPB (even if SPB do call XInitThreads).
            if (OperatingSystem.IsLinux())
            {
                XInitThreads();
                Environment.SetEnvironmentVariable("GDK_BACKEND", "x11");
                setenv("GDK_BACKEND", "x11", 1);
            }

            if (OperatingSystem.IsMacOS())
            {
                string baseDirectory = Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory);
                string resourcesDataDir;

                if (Path.GetFileName(baseDirectory) == "MacOS")
                {
                    resourcesDataDir = Path.Combine(Directory.GetParent(baseDirectory).FullName, "Resources");
                }
                else
                {
                    resourcesDataDir = baseDirectory;
                }

                void SetEnvironmentVariableNoCaching(string key, string value)
                {
                    int res = setenv(key, value, 1);
                    Debug.Assert(res != -1);
                }

                // On macOS, GTK3 needs XDG_DATA_DIRS to be set, otherwise it will try searching for "gschemas.compiled" in system directories.
                SetEnvironmentVariableNoCaching("XDG_DATA_DIRS", Path.Combine(resourcesDataDir, "share"));

                // On macOS, GTK3 needs GDK_PIXBUF_MODULE_FILE to be set, otherwise it will try searching for "loaders.cache" in system directories.
                SetEnvironmentVariableNoCaching("GDK_PIXBUF_MODULE_FILE", Path.Combine(resourcesDataDir, "lib", "gdk-pixbuf-2.0", "2.10.0", "loaders.cache"));

                SetEnvironmentVariableNoCaching("GTK_IM_MODULE_FILE", Path.Combine(resourcesDataDir, "lib", "gtk-3.0", "3.0.0", "immodules.cache"));
            }

            string systemPath = Environment.GetEnvironmentVariable("Path", EnvironmentVariableTarget.Machine);
            Environment.SetEnvironmentVariable("Path", $"{Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "bin")};{systemPath}");

            // Setup base data directory.
            AppDataManager.Initialize(CommandLineState.BaseDirPathArg);

            // Initialize the configuration.
            ConfigurationState.Initialize();

            // Initialize the logger system.
            LoggerModule.Initialize();

            // Initialize Discord integration.
            DiscordIntegrationModule.Initialize();

            // Initialize SDL2 driver
            SDL2Driver.MainThreadDispatcher = action =>
            {
                Application.Invoke(delegate
                {
                    action();
                });
            };

            // Sets ImageSharp Jpeg Encoder Quality.
            SixLabors.ImageSharp.Configuration.Default.ImageFormatsManager.SetEncoder(JpegFormat.Instance, new JpegEncoder()
            {
                Quality = 100
            });

            string localConfigurationPath   = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config.json");
            string appDataConfigurationPath = Path.Combine(AppDataManager.BaseDirPath,            "Config.json");

            // Now load the configuration as the other subsystems are now registered
            ConfigurationPath = File.Exists(localConfigurationPath)
                ? localConfigurationPath
                : File.Exists(appDataConfigurationPath)
                    ? appDataConfigurationPath
                    : null;

            bool showVulkanPrompt = false;

            if (ConfigurationPath == null)
            {
                // No configuration, we load the default values and save it to disk
                ConfigurationPath = appDataConfigurationPath;

                ConfigurationState.Instance.LoadDefault();
                ConfigurationState.Instance.ToFileFormat().SaveConfig(ConfigurationPath);

                showVulkanPrompt = true;
            }
            else
            {
                if (ConfigurationFileFormat.TryLoad(ConfigurationPath, out ConfigurationFileFormat configurationFileFormat))
                {
                    ConfigurationLoadResult result = ConfigurationState.Instance.Load(configurationFileFormat, ConfigurationPath);

                    if ((result & ConfigurationLoadResult.MigratedFromPreVulkan) != 0)
                    {
                        showVulkanPrompt = true;
                    }
                }
                else
                {
                    ConfigurationState.Instance.LoadDefault();

                    showVulkanPrompt = true;

                    Logger.Warning?.PrintMsg(LogClass.Application, $"无法加载配置！加载默认配置。\n无法加载位于 {ConfigurationPath} 的配置");
                }
            }

            // Check if graphics backend was overridden
            if (CommandLineState.OverrideGraphicsBackend != null)
            {
                if (CommandLineState.OverrideGraphicsBackend.ToLower() == "opengl")
                {
                    ConfigurationState.Instance.Graphics.GraphicsBackend.Value = GraphicsBackend.OpenGl;
                    showVulkanPrompt = false;
                }
                else if (CommandLineState.OverrideGraphicsBackend.ToLower() == "vulkan")
                {
                    ConfigurationState.Instance.Graphics.GraphicsBackend.Value = GraphicsBackend.Vulkan;
                    showVulkanPrompt = false;
                }
            }

            // Check if docked mode was overriden.
            if (CommandLineState.OverrideDockedMode.HasValue)
            {
                ConfigurationState.Instance.System.EnableDockedMode.Value = CommandLineState.OverrideDockedMode.Value;
            }

            // Logging system information.
            PrintSystemInfo();

            // Enable OGL multithreading on the driver, when available.
            BackendThreading threadingMode = ConfigurationState.Instance.Graphics.BackendThreading;
            DriverUtilities.ToggleOGLThreading(threadingMode == BackendThreading.Off);

            // Initialize Gtk.
            Application.Init();

            // Check if keys exists.
            bool hasSystemProdKeys = File.Exists(Path.Combine(AppDataManager.KeysDirPath, "prod.keys"));
            bool hasCommonProdKeys = AppDataManager.Mode == AppDataManager.LaunchMode.UserProfile && File.Exists(Path.Combine(AppDataManager.KeysDirPathUser, "prod.keys"));
            if (!hasSystemProdKeys && !hasCommonProdKeys)
            {
                UserErrorDialog.CreateUserErrorDialog(UserError.NoKeys);
            }

            // Show the main window UI.
            MainWindow mainWindow = new MainWindow();
            mainWindow.Show();

            if (CommandLineState.LaunchPathArg != null)
            {
                mainWindow.RunApplication(CommandLineState.LaunchPathArg, CommandLineState.StartFullscreenArg);
            }

            if (ConfigurationState.Instance.CheckUpdatesOnStart.Value && Updater.CanUpdate(false))
            {
                Updater.BeginParse(mainWindow, false).ContinueWith(task =>
                {
                    Logger.Error?.Print(LogClass.Application, $"更新器错误: {task.Exception}");
                }, TaskContinuationOptions.OnlyOnFaulted);
            }

            if (showVulkanPrompt)
            {
                var buttonTexts = new Dictionary<int, string>()
                {
                    { 0, "是的 (Vulkan)" },
                    { 1, "不用 (OpenGL)" }
                };

                ResponseType response = GtkDialog.CreateCustomDialog(
                    "Ryujinx - 默认图像后端",
                    "使用Vulkan作为默认的图形后端?",
                    "Ryujinx现已支持VULKAN API。 " +
                    "Vulkan极大地提升了着色器编译性能, " +
                    "解决了一些图形渲染卡顿;但是, 这还是个新的功能, " +
                    "你可能会遇到OpenGL没有的渲染错误。\n\n" +
                    "注意：在1.1.200及以上版本，当你第一次启动游戏时你也将会丢失任何已存在的着色器缓存， " +
                    "因为Vulkan需要更改着色器缓存，使其与以前的版本不兼容.\n\n" +
                    "是否要将Vulkan设置为默认的图形后端？ " +
                    "您可以随时在设置窗口中更改此设置。",
                    buttonTexts,
                    MessageType.Question);

                ConfigurationState.Instance.Graphics.GraphicsBackend.Value = response == 0
                    ? GraphicsBackend.Vulkan
                    : GraphicsBackend.OpenGl;

                ConfigurationState.Instance.ToFileFormat().SaveConfig(ConfigurationPath);
            }

            Application.Run();
        }

        private static void PrintSystemInfo()
        {
            Logger.Notice.Print(LogClass.Application, $"Ryujinx版本: {Version}");
            SystemInfo.Gather().Print();

            var enabledLogs = Logger.GetEnabledLevels();
            Logger.Notice.Print(LogClass.Application, $"Logs Enabled: {(enabledLogs.Count == 0 ? "<None>" : string.Join(", ", enabledLogs))}");

            if (AppDataManager.Mode == AppDataManager.LaunchMode.Custom)
            {
                Logger.Notice.Print(LogClass.Application, $"启动模式: 自定义路径 {AppDataManager.BaseDirPath}");
            }
            else
            {
                Logger.Notice.Print(LogClass.Application, $"启动模式: {AppDataManager.Mode}");
            }
        }

        private static void ProcessUnhandledException(Exception ex, bool isTerminating)
        {
            string message = $"未处理的异常捕获: {ex}";

            Logger.Error?.PrintMsg(LogClass.Application, message);

            if (Logger.Error == null)
            {
                Logger.Notice.PrintMsg(LogClass.Application, message);
            }

            if (isTerminating)
            {
                Exit();
            }
        }

        public static void Exit()
        {
            DiscordIntegrationModule.Exit();

            Logger.Shutdown();
        }
    }
}