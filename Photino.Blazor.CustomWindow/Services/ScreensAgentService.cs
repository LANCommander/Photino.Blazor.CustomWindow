using Microsoft.AspNetCore.Components.Web;
using System.Drawing;
using System.Runtime.InteropServices;
using Monitor = Photino.NET.Monitor;

namespace Photino.Blazor.CustomWindow.Services;

public class ScreensAgentService(PhotinoBlazorApp photinoBlazorApp)
{
    private PhotinoBlazorApp PhotinoBlazorApp { get; set; } = photinoBlazorApp;
    
    private List<ScreenInfo> _allScreens = new();
    private ScreenInfo _currentScreen { get; set; }

    public bool Inited => _allScreens.Any();

    private Rectangle ScaleRect(Rectangle rect, double scale)
    { 
        return new Rectangle(rect.X, rect.Y, (int)(rect.Width * scale), (int)(rect.Height * scale));
    }

    public Point GetOSPointerPosition(MouseEventArgs e)
    {
        var pointerScreenPos = new Point((int)e.ScreenX, (int)e.ScreenY);

        if (_currentScreen == null || !_currentScreen.WebScreen.Contains(pointerScreenPos))
        {
            var screen = _allScreens.First(s => s.WebScreen.Contains(pointerScreenPos));
            
            _currentScreen = screen;
        }
        
        return new()
        {
            X = (int)(_currentScreen.Monitor.MonitorArea.X + _currentScreen.Scale * (pointerScreenPos.X - _currentScreen.WebScreen.X)),
            Y = (int)(_currentScreen.Monitor.MonitorArea.Y + _currentScreen.Scale * (pointerScreenPos.Y - _currentScreen.WebScreen.Y)),
        };
    }

    public double GetPointerScreenScale(MouseEventArgs e)
    {
        var pointerScreenPos = new Point((int)e.ScreenX, (int)e.ScreenY);
        var screen = _allScreens.First(s => s.WebScreen.Contains(pointerScreenPos));
        return screen.Scale;
    }

    public void InitializeIfNeed()
    {
        if (!Inited)
            UpdateScreensInfo();
    }

    public void UpdateScreensInfo()
    {
        // init monitors and primary monitor
        var monitors = PhotinoBlazorApp.MainWindow.Monitors.ToArray();
        var primaryMonitor = monitors.Single(m => m.MonitorArea.Location.IsEmpty);

        // simple calculation if there is single monitor or no specific scale factors
        if (monitors.Length == 1 || monitors.All(m => m.Scale == 1))
        {
            _allScreens = monitors.Select(m => new ScreenInfo(m)).ToList();

            return;
        }

        // determine monitors positioning direction
        var isHorizontalDirection =
            monitors.Any(m1 => monitors.Except([m1]).Any(m2 => m2.MonitorArea.Left >= m1.MonitorArea.Width)) ||
            monitors.Any(m1 => m1.MonitorArea.Left <= -m1.MonitorArea.Width);
        var isVerticalDirection =
            monitors.Any(m1 => monitors.Except([m1]).Any(m2 => m2.MonitorArea.Top >= m1.MonitorArea.Height)) ||
            monitors.Any(m1 => m1.MonitorArea.Top <= -m1.MonitorArea.Height);

        if (!(isHorizontalDirection ^ isVerticalDirection))
        {
            throw new Exception("Only one-direction monitors positioning supported for different scale factors");
        }
        else
        {
            // add primary monitor to dictionary
            var primaryScreen = new ScreenInfo(primaryMonitor);
            
            _allScreens = new() { primaryScreen };

            Rectangle lastWebScreen;

            // horizontal direction calculation
            if (isHorizontalDirection)
            {
                var screensOrderedByX = _allScreens.OrderBy(s => s.Monitor.MonitorArea.X).ToList();
                var primaryMonitorIndex = screensOrderedByX.FindIndex(s => s.Monitor.Equals(primaryMonitor));

                lastWebScreen = primaryScreen.WebScreen;

                foreach (var screen in screensOrderedByX)
                {
                    screen.WebScreen.X = lastWebScreen.Right;
                    screen.WebScreen.Y = (int)(screen.Monitor.MonitorArea.Y / primaryMonitor.Scale) +
                                         (screen.Monitor.MonitorArea.Y > 0 ? 0 : primaryScreen.WebScreen.Bottom - screen.WebScreen.Height);
                    
                    lastWebScreen = screen.WebScreen;
                }

                lastWebScreen = primaryScreen.WebScreen;

                foreach (var screen in screensOrderedByX)
                {
                    screen.WebScreen.X = lastWebScreen.Left - screen.WebScreen.Width;
                    screen.WebScreen.Y = (int)(screen.Monitor.MonitorArea.Y / primaryMonitor.Scale) +
                                  (screen.Monitor.MonitorArea.Y > 0 ? 0 : primaryScreen.WebScreen.Bottom - screen.WebScreen.Height);
                    
                    lastWebScreen = screen.WebScreen;
                }
            }

            // vertical direction calculation
            if (isVerticalDirection)
            {
                var screensOrderedByY = _allScreens.OrderBy(s => s.Monitor.MonitorArea.Y).ToList();
                var primaryMonitorIndex = screensOrderedByY.FindIndex(s => s.Monitor.Equals(primaryMonitor));

                lastWebScreen = primaryScreen.WebScreen;

                foreach (var screen in screensOrderedByY)
                {
                    screen.WebScreen.Y = lastWebScreen.Bottom;
                    screen.WebScreen.X = (int)(screen.Monitor.MonitorArea.X / primaryMonitor.Scale) +
                                  (screen.Monitor.MonitorArea.X > 0 ? 0 : primaryScreen.WebScreen.Right - screen.WebScreen.Width);
                    
                    lastWebScreen = screen.WebScreen;
                }

                lastWebScreen = primaryScreen.WebScreen;

                foreach (var screen in screensOrderedByY)
                {
                    screen.WebScreen.Y = lastWebScreen.Top - screen.WebScreen.Height;
                    screen.WebScreen.X = (int)(screen.Monitor.MonitorArea.X / primaryMonitor.Scale) +
                                  (screen.Monitor.MonitorArea.X > 0 ? 0 : primaryScreen.WebScreen.Right - screen.WebScreen.Width);
                    
                    lastWebScreen = screen.WebScreen;
                }
            }
        }
    }
}
