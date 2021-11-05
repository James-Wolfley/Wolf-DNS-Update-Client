using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace Wolf_DDClient
{
    class Program
    {

        static HttpClient client = new();
        static Timer timer = null;
        static string currentIp = "";
        static string domain = "";

        static void Main(string[] args)
        {
            Console.WriteLine("Setting up HttpClient");
            string basePath = Directory.GetCurrentDirectory();
            if (!File.Exists($"{basePath}\\config.xml"))
            {
                Console.WriteLine("First time settup required. Please enter your username and press enter");
                string username = Console.ReadLine().Trim();
                Console.WriteLine("Please enter your password and press enter.");
                string password = Console.ReadLine().Trim();
                Console.WriteLine("Please enter your domain to be updated and press enter.");
                string address = Console.ReadLine().Trim();
                CreateConfig(username, password, address);
            }

            XmlDocument doc = new();
            doc.Load(basePath + "\\config.xml");

            string user = doc.SelectSingleNode("/config/username").InnerText;
            string pwd = doc.SelectSingleNode("/config/password").InnerText;
            domain = doc.SelectSingleNode("/config/address").InnerText;
            SetupClient(user, pwd);
            timer = new Timer(UpdateDNS, null, 0, 300000);
            Console.ReadLine();
        }

        private static void UpdateDNS(object state)
        {
            string newIp = GetPublicIP().Result;
            if (newIp != currentIp)
            {
                currentIp = newIp;
                var result = client.GetAsync(new Uri($"https://domains.google.com/nic/update?hostname={domain}&myip={currentIp}"));
                Console.WriteLine($"We got a new IP and have attempted to update DNS records with a result status code of {result.Result.StatusCode}. The new IP is {newIp}");
            }
            else
            {
                Console.WriteLine("The IP has not changed.");
            }
        }

        static async Task<string> GetPublicIP()
        {
            var result = await client.GetAsync(new Uri("http://checkip.amazonaws.com/"));

            string newIp = await result.Content.ReadAsStringAsync();
            newIp = newIp.Trim();
            return newIp;
        }

        static bool SetupClient(string username, string password)
        {
            string credentials = $"{username}:{password}";

            string base64AuthString = Convert.ToBase64String(Encoding.UTF8.GetBytes(credentials));

            client.DefaultRequestHeaders.Add("Authorization", $"Basic {base64AuthString}");

            return client != null;
        }

        static void CreateConfig(string username, string password, string address)
        {
            XmlDocument doc = new();
            doc.CreateXmlDeclaration("1.0", "UTF-8", null);
            XmlDeclaration xmlDeclaration = doc.CreateXmlDeclaration("1.0", "UTF-8", null);

            XmlElement root = doc.DocumentElement;
            doc.InsertBefore(xmlDeclaration, root);

            XmlElement config = doc.CreateElement("config");
            doc.AppendChild(config);

            XmlElement usernameElement = doc.CreateElement("username");
            XmlElement passwordElement = doc.CreateElement("password");
            XmlElement addressElement = doc.CreateElement("address");

            config.AppendChild(usernameElement);
            config.AppendChild(passwordElement);
            config.AppendChild(addressElement);

            XmlText usernameText = doc.CreateTextNode(username);
            XmlText passwordText = doc.CreateTextNode(password);
            XmlText addressText = doc.CreateTextNode(address);

            usernameElement.AppendChild(usernameText);
            passwordElement.AppendChild(passwordText);
            addressElement.AppendChild(addressText);

            doc.Save(Directory.GetCurrentDirectory() + "//config.xml");
        }

    }
}
