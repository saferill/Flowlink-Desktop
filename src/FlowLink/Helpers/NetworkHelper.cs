using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace FlowLink.Helpers;

public static class NetworkHelper
{
    public static List<IPAddressInfo> GetAllValidAddresses()
    {
        var addresses = new List<IPAddressInfo>();
        
        foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
        {
            bool isTailscale = ni.Name.Contains("Tailscale", StringComparison.OrdinalIgnoreCase) ||
                               ni.Description.Contains("Tailscale", StringComparison.OrdinalIgnoreCase);

            if ((ni.NetworkInterfaceType is NetworkInterfaceType.Wireless80211 or NetworkInterfaceType.Ethernet or NetworkInterfaceType.Tunnel || isTailscale) &&
                ni.OperationalStatus is OperationalStatus.Up)
            {
                var gateway = ni.GetIPProperties().GatewayAddresses
                    .FirstOrDefault(g => g.Address.AddressFamily is AddressFamily.InterNetwork)?.Address;

                foreach (UnicastIPAddressInformation ip in ni.GetIPProperties().UnicastAddresses)
                {
                    if (ip.Address.AddressFamily is AddressFamily.InterNetwork && 
                        !IPAddress.IsLoopback(ip.Address))
                    {
                        var bytes = ip.Address.GetAddressBytes();
                        bool isTailscaleIp = bytes[0] == 100 && (bytes[1] >= 64 && bytes[1] <= 127);

                        if (gateway != null || isTailscale || isTailscaleIp)
                        {
                            addresses.Add(new IPAddressInfo(
                                Address: ip.Address,
                                SubnetMask: ip.IPv4Mask,
                                Gateway: gateway
                            ));
                        }
                    }
                }
            }
        }
        
        return addresses;
    }

    public record IPAddressInfo(IPAddress Address, IPAddress SubnetMask, IPAddress? Gateway);
}
