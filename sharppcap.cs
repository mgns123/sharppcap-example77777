
using PacketDotNet;
using SharpPcap;

namespace Example1
{
    public class Program
    {
        public static void Main()
        {
            // Получаем список всех доступных сетевых интерфейсов
            var devices = CaptureDeviceList.Instance;

            if (devices.Count < 1)
            {
                Console.WriteLine("Нет доступных сетевых устройств");
                return;
            }

            Console.WriteLine("Доступные сетевые устройства:");
            Console.WriteLine("----------------------------------------------------\n");

            int i = 0;

            foreach (var dev in devices)
            {
                Console.WriteLine("{0}) {1}", i, dev.Description);
                i++;
            }
            Console.Write("Выберите устройство для захвата: ");
            i = int.Parse(Console.ReadLine());

            using var device = devices[i];

            device.OnPacketArrival += new PacketArrivalEventHandler(device_OnPacketArrival);

            // Открывает устройство для захвата
            int readTimeoutMilliseconds = 1000;
            device.Open(mode: DeviceModes.Promiscuous | DeviceModes.DataTransferUdp | DeviceModes.NoCaptureLocal, read_timeout: readTimeoutMilliseconds);

            Console.WriteLine();
            
            // Начинаем захват
            device.StartCapture();

            Console.WriteLine("Нажмите любую клавишу для завершения захвата...");
            Console.ReadKey();

            // Останавливаем захват
            device.StopCapture();

            Console.WriteLine();
            PrintStatistics();
        }

        private static int tcpPacketCount = 0;
        private static int udpPacketCount = 0;
        private static Dictionary<string, int> ipAddresses = new Dictionary<string, int>();


        private static void device_OnPacketArrival(object sender, PacketCapture e)
        {
            var rawPacket = e.GetPacket();
            var packet = Packet.ParsePacket(rawPacket.LinkLayerType, rawPacket.Data);
               
            // Извлекаем ipv4 пакет
            var ipPacket = packet.Extract<IPv4Packet>();

            if (ipPacket != null)
            {
                var ip = ipPacket.SourceAddress.ToString();
                if (!ipAddresses.ContainsKey(ip))
                {
                    ipAddresses[ip] = 0;
                }
                ipAddresses[ip]++;

                var tcpPacket = packet.Extract<TcpPacket>();
                if (tcpPacket != null)
                {
                    tcpPacketCount++;
                }

                var udpPacket = packet.Extract<UdpPacket>();
                if (udpPacket != null)
                {
                    udpPacketCount++;
                }
            }
        }
        private static void PrintStatistics()
        {
            Console.WriteLine($"TCP пакетов: {tcpPacketCount}");
            Console.WriteLine($"UDP пакетов: {udpPacketCount}");

            Console.WriteLine("IP-адреса:");
            foreach (var entry in ipAddresses)
            {
                Console.WriteLine($"{entry.Key}: {entry.Value} пакетов");
            }
        }
    }
}
