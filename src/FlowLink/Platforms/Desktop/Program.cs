using FlowLink;
using Uno.UI.Hosting;

internal class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        // Velopack: handle install/uninstall/update hooks
        Velopack.VelopackApp.Build().Run();

        var host = UnoPlatformHostBuilder.Create()
            .App(() => new App())
            .UseX11()
            .UseLinuxFrameBuffer()
            .UseMacOS()
            .UseWin32()
            .Build();

        host.Run();
    }
}
