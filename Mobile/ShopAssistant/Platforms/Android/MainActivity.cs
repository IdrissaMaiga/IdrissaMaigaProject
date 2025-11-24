using Android.App;
using Android.Content.PM;
using Android.OS;
using System;
using System.IO;
using System.Text;

namespace ShopAssistant
    {
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
        {
        protected override void OnCreate(Bundle? savedInstanceState)
            {
            // Suppress Mono verbose logging - set before base.OnCreate
            // NOTE: Many messages like "[monodroid-assembly]" and "Loaded assembly:" are written
            // directly to logcat by the native Mono/Android runtime BEFORE .NET initializes.
            // These CANNOT be filtered through .NET code. They appear in Visual Studio Output
            // because VS captures logcat. Filter them in VS Output window or use logcat filters.
            System.Environment.SetEnvironmentVariable("MONO_LOG_LEVEL", "error");
            
            // Filter .NET console output to hide verbose framework messages
            // This will filter messages that come through Console.Out/Error, but NOT native logcat messages
            FilterConsoleOutput();
            
            base.OnCreate(savedInstanceState);
            }

        private void FilterConsoleOutput()
            {
#if DEBUG
            // Filter .NET-level verbose messages (native Mono messages will still appear)
            var originalOut = Console.Out;
            var filteredWriter = new FilteredTextWriter(originalOut);
            Console.SetOut(filteredWriter);
            
            var originalError = Console.Error;
            var filteredErrorWriter = new FilteredTextWriter(originalError);
            Console.SetError(filteredErrorWriter);
#endif
            }

        // Custom TextWriter that filters out verbose framework messages
        private class FilteredTextWriter : TextWriter
            {
            private readonly TextWriter _baseWriter;
            private static readonly string[] _suppressedPatterns = new[]
                {
                // Mono/Android Runtime Messages
                "[monodroid-assembly]",
                "Loaded assembly:",
                "open_from_bundles:",
                "the assembly might have been uploaded",
                "Thread started:",
                
                // Android System Messages
                "[EGL_emulation]",
                "[HWUI]",
                "[AppCompatDelegate]",
                "[ashmem]",
                "[DesktopModeFlags]",
                "[CompatChangeReporter]",
                "[MaterialButton]",
                "[ProfileInstaller]",
                "[ResourcesManager]",
                "[nativeloader]",
                "[TabLayout]",
                "[ImeTracker]",
                "[InsetsController]",
                "[WindowOnBackDispatcher]",
                "[RemoteInputConnectionImpl]",
                "[AutofillManager]",
                "[PerfettoTrigger]",
                "[MESA]",
                "[vulkan]",
                "[libc] Access denied",
                "AssetManager2",
                "ApplicationLoaders",
                "hiddenapi:",
                "Compiler allocated",
                "Image decoding logging dropped!",
                "Unknown dataspace",
                
                // WebView/Chromium Messages
                "[chromium]",
                "[cr_",
                "[WebViewFactory]",
                "[VideoCapabilities]",
                "[CameraManagerGlobal]",
                
                // UI/Graphics Messages
                "[RippleDrawable]",
                "[FrameTracker]",
                "[InteractionJankMonitor]",
                "Davey!",
                
                // GC Messages
                "Explicit concurrent mark compact GC freed",
                "NativeAlloc concurrent mark compact GC freed",
                "Waiting for a blocking GC",
                "WaitForGcToComplete blocked"
                };

            public FilteredTextWriter(TextWriter baseWriter)
                {
                _baseWriter = baseWriter;
                }

            public override Encoding Encoding => _baseWriter.Encoding;

            public override void Write(char value)
                {
                _baseWriter.Write(value);
                }

            public override void Write(string? value)
                {
                if (value != null && ShouldSuppress(value))
                    return;
                _baseWriter.Write(value);
                }

            public override void WriteLine(string? value)
                {
                if (value != null && ShouldSuppress(value))
                    return;
                _baseWriter.WriteLine(value);
                }

            private static bool ShouldSuppress(string message)
                {
                foreach (var pattern in _suppressedPatterns)
                    {
                    if (message.Contains(pattern, StringComparison.OrdinalIgnoreCase))
                        return true;
                    }
                return false;
                }
            }
        }
    }
