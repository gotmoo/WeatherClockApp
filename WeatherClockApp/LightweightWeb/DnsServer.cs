using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace WeatherClockApp.LightweightWeb
{
    /// <summary>
    /// A minimal DNS server for captive portal functionality.
    /// It responds to all A-record queries with the device's own IP address.
    /// </summary>
    public class DnsServer
    {
        private readonly IPAddress _ipAddress;
        private Thread _serverThread;
        private bool _isRunning = false;
        private UdpClient _udpClient;

        public DnsServer(IPAddress ipAddress)
        {
            _ipAddress = ipAddress;
        }

        public void Start()
        {
            if (_isRunning) return;

            _isRunning = true;
            _serverThread = new Thread(ListenLoop);
            _serverThread.Start();
            Debug.WriteLine($"DNS server started, redirecting all queries to {_ipAddress}");
        }

        public void Stop()
        {
            if (!_isRunning) return;

            _isRunning = false;
            _udpClient?.Close();
            _serverThread.Join();
            Debug.WriteLine("DNS server stopped.");
        }

        private void ListenLoop()
        {
            try
            {
                _udpClient = new UdpClient(53);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to bind DNS server to port 53: {ex.Message}");
                return;
            }

            IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
            byte[] receiveBuffer = new byte[512]; // Standard buffer size for DNS

            while (_isRunning)
            {
                try
                {
                    // Correctly receive data using the provided API signature
                    int bytesRead = _udpClient.Receive(receiveBuffer, ref remoteEndPoint);

                    if (bytesRead > 12) // Minimum length for a DNS query header
                    {
                        // Create a new buffer with the exact size of the received data
                        byte[] queryBuffer = new byte[bytesRead];
                        Array.Copy(receiveBuffer, 0, queryBuffer, 0, bytesRead);

                        // Extract the domain name from the query for logging
                        string domainName = ExtractDomainName(queryBuffer, 12, queryBuffer.Length);
                        Debug.WriteLine($"DNS Query from {remoteEndPoint.Address} for: {domainName}");

                        // Craft a response based on the actual query data
                        byte[] response = CraftDnsResponse(queryBuffer);

                        Debug.WriteLine($"DNS Response: Redirecting {domainName} to {_ipAddress}");

                        // Send the response back to the client using the correct overload
                        _udpClient.Send(response, remoteEndPoint);
                    }
                }
                catch (Exception ex)
                {
                    if (_isRunning)
                    {
                        Debug.WriteLine($"Error in DNS listen loop: {ex.Message}");
                    }
                }
            }
        }

        private byte[] CraftDnsResponse(byte[] query)
        {
            byte[] response = new byte[query.Length + 16];

            // Copy transaction ID, flags, etc. from query
            Array.Copy(query, 0, response, 0, query.Length);

            // Set response flags (QR=1 for response, RA=1 for recursion available)
            response[2] = 0x81;
            response[3] = 0x80;

            // Set Answer RRs count to 1
            response[7] = 0x01;

            // Add the answer section
            int offset = query.Length;

            // Name: pointer to the name in the query (offset 12)
            response[offset] = 0xC0;
            response[offset + 1] = 0x0C;

            // Type: A record (1)
            response[offset + 2] = 0x00;
            response[offset + 3] = 0x01;

            // Class: IN (1)
            response[offset + 4] = 0x00;
            response[offset + 5] = 0x01;

            // TTL: 60 seconds
            response[offset + 6] = 0x00;
            response[offset + 7] = 0x00;
            response[offset + 8] = 0x00;
            response[offset + 9] = 0x3C;

            // RDLENGTH: 4 bytes for an IPv4 address
            response[offset + 10] = 0x00;
            response[offset + 11] = 0x04;

            // RDATA: The IP address to return
            byte[] ipBytes = _ipAddress.GetAddressBytes();
            Array.Copy(ipBytes, 0, response, offset + 12, ipBytes.Length);

            return response;
        }

        private string ExtractDomainName(byte[] buffer, int offset, int length)
        {
            var sb = new StringBuilder();
            while (offset < length)
            {
                byte labelLength = buffer[offset++];
                if (labelLength == 0) break; // End of name
                if (sb.Length > 0) sb.Append('.');
                sb.Append(Encoding.UTF8.GetString(buffer, offset, labelLength));
                offset += labelLength;
            }
            return sb.ToString();
        }
    }
}

