using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using SMBLibrary;
using SMBLibrary.Server;
using SMBLibrary.Server.Win32;
using Utilities;

namespace SMBServer
{
    public partial class ServerUI : Form
    {
        public const string SettingsFileName = "Settings.xml";
        private SMBLibrary.Server.SMBServer m_server;
        private SMBLibrary.Server.NameServer m_nameServer;

        public ServerUI()
        {
            InitializeComponent();
        }

        private void ServerUI_Load(object sender, EventArgs e)
        {
            List<IPAddress> localIPs = GetHostIPAddresses();
            KeyValuePairList<string, IPAddress> list = new KeyValuePairList<string, IPAddress>();
            list.Add("Any", IPAddress.Any);
            foreach (IPAddress address in localIPs)
            {
                list.Add(address.ToString(), address);
            }
            comboIPAddress.DataSource = list;
            comboIPAddress.DisplayMember = "Key";
            comboIPAddress.ValueMember = "Value";
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            IPAddress serverAddress = (IPAddress)comboIPAddress.SelectedValue;
            SMBTransportType transportType;
            if (rbtNetBiosOverTCP.Checked)
            {
                transportType = SMBTransportType.NetBiosOverTCP;
            }
            else
            {
                transportType = SMBTransportType.DirectTCPTransport;
            }

            INTLMAuthenticationProvider provider;
            if (chkIntegratedWindowsAuthentication.Checked)
            {
                provider = new Win32UserCollection();
                
            }
            else
            {
                UserCollection users;
                try
                {
                    users = ReadUserSettings();
                }
                catch
                {
                    MessageBox.Show("Cannot read " + SettingsFileName, "Error");
                    return;
                }
                
                provider = new IndependentUserCollection(users);
            }


            List<string> allUsers = provider.ListUsers();
            ShareCollection shares;
            try
            {
                shares = ReadShareSettings(allUsers);
            }
            catch (Exception)
            {
                MessageBox.Show("Cannot read " + SettingsFileName, "Error");
                return;
            }

            m_server = new SMBLibrary.Server.SMBServer(shares, provider, serverAddress, transportType);
            m_server.OnLogEntry += new EventHandler<LogEntry>(Server_OnLogEntry);

            try
            {
                m_server.Start();
                if (transportType == SMBTransportType.NetBiosOverTCP)
                {
                    m_nameServer = new NameServer(serverAddress);
                    m_nameServer.Start();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error");
                return;
            }

            btnStart.Enabled = false;
            btnStop.Enabled = true;
            comboIPAddress.Enabled = false;
            rbtDirectTCPTransport.Enabled = false;
            rbtNetBiosOverTCP.Enabled = false;
            chkIntegratedWindowsAuthentication.Enabled = false;
        }

        private XmlDocument GetSettingsXML()
        {
            string executableDirectory = Path.GetDirectoryName(Application.ExecutablePath) + "\\";
            XmlDocument document = GetXmlDocument(executableDirectory + SettingsFileName);
            return document;
        }

        private UserCollection ReadUserSettings()
        {
            UserCollection users = new UserCollection();
            XmlDocument document = GetSettingsXML();
            XmlNode usersNode = document.SelectSingleNode("Settings/Users");

            foreach (XmlNode userNode in usersNode.ChildNodes)
            {
                string accountName = userNode.Attributes["AccountName"].Value;
                string password = userNode.Attributes["Password"].Value;
                users.Add(accountName, password);
            }
            return users;
        }

        private ShareCollection ReadShareSettings(List<string> allUsers)
        {
            ShareCollection shares = new ShareCollection();
            XmlDocument document = GetSettingsXML();
            XmlNode sharesNode = document.SelectSingleNode("Settings/Shares");

            foreach (XmlNode shareNode in sharesNode.ChildNodes)
            {
                string shareName = shareNode.Attributes["Name"].Value;
                string sharePath = shareNode.Attributes["Path"].Value;

                XmlNode readAccessNode = shareNode.SelectSingleNode("ReadAccess");
                List<string> readAccess = ReadAccessList(readAccessNode, allUsers);
                XmlNode writeAccessNode = shareNode.SelectSingleNode("WriteAccess");
                List<string> writeAccess = ReadAccessList(writeAccessNode, allUsers);
                shares.Add(shareName, readAccess, writeAccess, new DirectoryFileSystem(sharePath));
            }
            return shares;
        }

        private List<string> ReadAccessList(XmlNode node, List<string> allUsers)
        {
            List<string> result = new List<string>();
            if (node != null)
            {
                string accounts = node.Attributes["Accounts"].Value;
                if (accounts == "*")
                {
                    result.AddRange(allUsers);
                }
                else
                {
                    string[] splitted = accounts.Split(',');
                    result.AddRange(splitted);
                }
            }
            return result;
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            m_server.Stop();
            btnStart.Enabled = true;
            btnStop.Enabled = false;
            comboIPAddress.Enabled = true;
            rbtDirectTCPTransport.Enabled = true;
            rbtNetBiosOverTCP.Enabled = true;
            chkIntegratedWindowsAuthentication.Enabled = true;

            if (m_nameServer != null)
            {
                m_nameServer.Stop();
            }
        }

        private static XmlDocument GetXmlDocument(string path)
        {
            XmlDocument doc = new XmlDocument();
            doc.Load(path);
            return doc;
        }

        private static List<IPAddress> GetHostIPAddresses()
        {
            List<IPAddress> result = new List<IPAddress>();
            foreach (NetworkInterface netInterface in NetworkInterface.GetAllNetworkInterfaces())
            {
                IPInterfaceProperties ipProperties = netInterface.GetIPProperties();
                foreach (UnicastIPAddressInformation addressInfo in ipProperties.UnicastAddresses)
                {
                    if (addressInfo.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                    {
                        result.Add(addressInfo.Address);
                    }
                }
            }
            return result;
        }

        private void Server_OnLogEntry(object sender, LogEntry entry)
        {
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss ");
            string message = String.Format("{0} {1} {2}", entry.Severity.ToString().PadRight(12), timestamp, entry.Message);
            System.Diagnostics.Debug.Print(message);
        }
    }
}