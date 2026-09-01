using System;
using System.Threading.Tasks;
using FlowLink.Data.Models;

namespace FlowLink.Data.Contracts;

public interface IScreenMirrorService
{
    Task<bool> StartScrcpy(PairedDevice device, string? customArgs = null, string? iconPath = null);

    void LaunchAppByPackage(string package);

    IntPtr CurrentScrcpyHwnd { get; }
    event Action<IntPtr>? OnScrcpyWindowFound;
    event Action? OnScrcpyWindowClosed;
}
