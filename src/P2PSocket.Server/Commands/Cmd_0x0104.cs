﻿using P2PSocket.Core.Commands;
using P2PSocket.Core.Models;
using P2PSocket.Core.Utils;
using P2PSocket.Server.Models;
using P2PSocket.Server.Models.Send;
using P2PSocket.Server.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace P2PSocket.Server.Commands
{
    [CommandFlag(Core.P2PCommandType.Login0x0104)]
    public class Cmd_0x0104 : P2PCommand
    {
        readonly P2PTcpClient m_tcpClient;
        BinaryReader m_data { get; }
        public Cmd_0x0104(P2PTcpClient tcpClient, byte[] data)
        {
            m_tcpClient = tcpClient;
            m_data = new BinaryReader(new MemoryStream(data));
        }
        public override bool Excute()
        {
            string macAddress = BinaryUtils.ReadString(m_data);
            string clientName = ClientCenter.Instance.GetClientName(macAddress);
            bool isSuccess = true;
            P2PTcpItem item = new P2PTcpItem();
            item.TcpClient = m_tcpClient;
            if (ClientCenter.Instance.TcpMap.ContainsKey(clientName))
            {
                if (ClientCenter.Instance.TcpMap[clientName].TcpClient.IsDisConnected)
                {
                    ClientCenter.Instance.TcpMap[clientName].TcpClient?.SafeClose();
                    ClientCenter.Instance.TcpMap[clientName] = item;
                }
                else
                {
                    isSuccess = false;
                    Send_0x0101 sendPacket = new Send_0x0101(m_tcpClient, false, $"ClientName:{clientName} 已被使用", clientName);
                    m_tcpClient.Client.Send(sendPacket.PackData());
                    m_tcpClient?.SafeClose();

                    try
                    {
                        ClientCenter.Instance.TcpMap[clientName].TcpClient.Client.Send(new Send_0x0052().PackData());
                    }
                    catch (Exception)
                    {
                        ClientCenter.Instance.TcpMap.Remove(clientName);
                    }
                }
            }
            else
                ClientCenter.Instance.TcpMap.Add(clientName, item);
            if (isSuccess)
            {
                m_tcpClient.ClientName = clientName;
                Send_0x0101 sendPacket = new Send_0x0101(m_tcpClient, true, $"客户端{clientName}认证通过",clientName);
                m_tcpClient.Client.Send(sendPacket.PackData());
            }
            return true;
        }
    }
}
