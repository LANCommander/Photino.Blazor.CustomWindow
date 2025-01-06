using System.Drawing;
using System.Runtime.InteropServices;
using Monitor = Photino.NET.Monitor;

namespace Photino.Blazor.CustomWindow;

public class ScreenInfo
{
    public Monitor Monitor;
    public Rectangle WebScreen;
    public double Scale;

    public ScreenInfo(Monitor monitor)
    {
        Monitor = monitor;
        Scale = monitor.Scale;

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            Scale = 1;

        WebScreen = ScaleRect(monitor.MonitorArea, 1 / Scale);
    }
    
    private Rectangle ScaleRect(Rectangle rect, double scale)
    { 
        return new Rectangle(rect.X, rect.Y, (int)(rect.Width * scale), (int)(rect.Height * scale));
    }
}