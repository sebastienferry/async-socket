using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AsyncSocket.Core.Net
{
    /// <summary>
    /// Supported address protocols
    /// </summary>
    internal enum Protocol
    {
        Tcp
    }
    
    /// <summary>
    /// A class that represents a address of the following form protocol://ip_or_name:port.
    /// </summary>
    internal class Address
    {
        private static Regex _addressExpression = new Regex("(\\w*)://(.+):(\\d+)", RegexOptions.Compiled);

        /// <summary>
        /// Initializes a new instance of the <see cref="OriginalAddress"/> class.
        /// </summary>
        /// <param name="address">string represention of an address. Typically tcp://ip:port</param>
        public Address(string address)
        {
            OriginalAddress = address;

            Match match = _addressExpression.Match(OriginalAddress);

            if (match.Groups.Count < 4)
            {
                throw new ArgumentException("Address must looks like tcp://<ip_or_name>:port");
            }

            // Protocol parsing
            string protocol = match.Groups[1].Value;

            Protocol detectedProtocol;

            if (!Enum.TryParse(protocol, true, out detectedProtocol))
            {
                throw new ArgumentException("Address protocol not supported");
            }

            Protocol = detectedProtocol;

            // IP or name
            string iporname = match.Groups[2].Value;

            IPAddress detectedIpAddress;

            if (!IPAddress.TryParse(iporname, out detectedIpAddress))
            {
                if (iporname.Length > 0)
                {
                    _isNameAddress = true;
                    _dnsName = iporname;
                }
                else
                {
                    throw new ArgumentException("Address ip or name is invalid");
                }
            }
            else
            {
                _ipAddress = detectedIpAddress;
            }

            // Port
            string port = match.Groups[3].Value;

            int detectedPort;

            if (!int.TryParse(port, out detectedPort))
            {
                throw new ArgumentException("Address port is invalid");
            }

            Port = detectedPort;

            if (_isNameAddress)
            {
                EndPoint = new DnsEndPoint(iporname, Port);
            }
            else
            {
                EndPoint = new IPEndPoint(detectedIpAddress, detectedPort);
            }
        }

        /// <summary>
        /// A field that indicates if the address uses name or ip.
        /// </summary>
        private readonly bool _isNameAddress;

        /// <summary>
        /// A field that indicates the name part of the address.
        /// </summary>
        private readonly string _dnsName;

        /// <summary>
        /// A field that indicates if the address uses name or ip.
        /// </summary>
        private IPAddress _ipAddress;
        
        /// <summary>
        /// Gets the string representation used to initialized this address.
        /// </summary>
        public string OriginalAddress { get; private set; }

        /// <summary>
        /// Gets the protocol of this address.
        /// </summary>
        public Protocol Protocol { get; private set; }

        /// <summary>
        /// Gets the port associated to the address.
        /// </summary>
        public int Port { get; private set; }

        /// <summary>
        /// Gets the endpoint associated to the address.
        /// </summary>
        public EndPoint EndPoint { get; private set; }

        /// <summary>
        /// Gets the IP address associated to this address.
        /// If the address uses a DNS name, the IP is resolve using Dns.
        /// </summary>
        public IPAddress IpAddress { get { return _ipAddress = _ipAddress ?? Resolve(_dnsName).Result; } }

        /// <summary>
        /// Resolve a DNS name into an IP address.
        /// </summary>
        /// <param name="dnsName"></param>
        /// <returns>IP address resolved or IPAddress.None</returns>
        public static async Task<IPAddress> Resolve(string dnsName)
        {
            IPAddress[] addresses = await Dns.GetHostAddressesAsync(dnsName);

            return addresses.Any() ? addresses.First() : IPAddress.None;
        }
    }
}
