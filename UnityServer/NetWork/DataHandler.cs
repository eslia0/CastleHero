﻿using System;
using System.Threading;
using System.Net.Sockets;
using System.Collections.Generic;

public class DataHandler
{
	public Queue<TcpPacket> receiveMsgs;
	public Queue<TcpPacket> sendMsgs;
	public Database database;

	public Dictionary<Socket, string> LoginUser;

	object receiveLock;
	object sendLock;

	TcpPacket tcpPacket;
    
	byte[] msg = new byte[1024];

    RecvNotifier recvNotifier;
    public delegate ServerPacketId RecvNotifier(byte[] data);
	private Dictionary<int, RecvNotifier> m_notifier = new Dictionary<int, RecvNotifier>();

    public DataHandler(Queue<TcpPacket> receiveQueue, Queue<TcpPacket> sendQueue, object newReceiveLock, object newSendLock)
    {
        database = new Database();
        receiveMsgs = receiveQueue;
        sendMsgs = sendQueue;
        receiveLock = newReceiveLock;
        sendLock = newSendLock;
        LoginUser = new Dictionary<Socket, string>();

        m_notifier.Add((int)ClientPacketId.Create, CreateAccount);
        m_notifier.Add((int)ClientPacketId.Delete, DeleteAccount);
        m_notifier.Add((int)ClientPacketId.Login, Login);
        m_notifier.Add((int)ClientPacketId.Logout, Logout);
        m_notifier.Add((int)ClientPacketId.GameClose, GameClose);
        m_notifier.Add((int)ClientPacketId.HeroDataRequest, HeroDataRequest);
        m_notifier.Add((int)ClientPacketId.ItemDataRequest, ItemDataRequest);
        m_notifier.Add((int)ClientPacketId.SkillDataRequest, SkillDataRequest);
        m_notifier.Add((int)ClientPacketId.UnitDataRequest, UnitDataRequest);
        m_notifier.Add((int)ClientPacketId.BuildingDataRequest, BuildingDataRequest);
        m_notifier.Add((int)ClientPacketId.UpgradeDataRequest, UpgradeDataRequest);
        m_notifier.Add((int)ClientPacketId.ResourceDataRequest, ResourceDataRequest);
        m_notifier.Add((int)ClientPacketId.StateDataRequest, StateDataRequest);
        m_notifier.Add((int)ClientPacketId.Build, BuildBuilding);
        m_notifier.Add((int)ClientPacketId.BuildCancel, BuildCancel);
        m_notifier.Add((int)ClientPacketId.BuildDataRequest, BuildDataRequest);

        Thread handleThread = new Thread(new ThreadStart(DataHandle));
        handleThread.Start();
    }
    
	public void DataHandle ()
	{
        while (true)
        {
            if (receiveMsgs.Count != 0)
            {
                //패킷을 Dequeue 한다 패킷 : 메시지 타입 + 메시지 내용
                lock (receiveLock)
                {
                    tcpPacket = receiveMsgs.Dequeue();
                }

                //타입과 내용을 분리한다
                byte Id = tcpPacket.msg[0];
                msg = new byte[tcpPacket.msg.Length - 1];
                Array.Copy(tcpPacket.msg, 1, msg, 0, msg.Length);

                HeaderData headerData = new HeaderData();

                //Dictionary에 등록된 델리게이트형 메소드에서 msg를 반환받는다.
                if (m_notifier.TryGetValue(Id, out recvNotifier))
                {
                    ServerPacketId packetId = recvNotifier(msg);
                    //send 할 id를 반환받음
                    if (packetId != ServerPacketId.None)
                    {
                        tcpPacket = new TcpPacket(msg, tcpPacket.client);
                        sendMsgs.Enqueue(tcpPacket);
                    }
                }
                else
                {
                    Console.WriteLine("DataHandler::TryGetValue 에러 " + Id);
                    headerData.Id = (byte)ServerPacketId.None;                    
                }                
            }
        }
	}

	public ServerPacketId CreateAccount (byte[] data)
	{
		Console.WriteLine (tcpPacket.client.RemoteEndPoint.ToString() + " 가입요청");

		AccountPacket accountPacket = new AccountPacket (data);
		AccountData accountData = accountPacket.GetData ();

		Console.WriteLine ("아이디 : " + accountData.Id + "패스워드 : " + accountData.password);

        try
        {
            if (database.AddAccountData (accountData.Id, accountData.password))
			{
                msg[0] = (byte) UnityServer.Result.Success;
                Console.WriteLine("가입 성공");
			}
			else
			{
                msg[0] = (byte)UnityServer.Result.Fail;
                Console.WriteLine("가입 실패");
            }
        }
		catch
		{
			Console.WriteLine ("DataHandler::AddPlayerData 에러");
            Console.WriteLine("가입 실패");
            msg[0] = (byte)UnityServer.Result.Fail;
        }

        Array.Resize(ref msg, 1);
        msg = CreateResultPacket(msg, ServerPacketId.CreateResult);

        return ServerPacketId.CreateResult;
	}

	public ServerPacketId DeleteAccount (byte[] data)
	{
		Console.WriteLine (tcpPacket.client.RemoteEndPoint.ToString() + " 탈퇴요청");

		AccountPacket accountPacket = new AccountPacket (data);
		AccountData accountData = accountPacket.GetData ();

		Console.WriteLine ("아이디 : " + accountData.Id + "패스워드 : " + accountData.Id);

		try
		{
			if (database.DeleteAccountData (accountData.Id, accountData.password))
			{
                msg[0] = (byte)UnityServer.Result.Success;
                Console.WriteLine("탈퇴 성공");
            }
			else
			{
                msg[0] = (byte)UnityServer.Result.Fail;
                Console.WriteLine("탈퇴 실패");
            }
		}
		catch
		{
			Console.WriteLine ("DataHandler::RemovePlayerData 에러");
            Console.WriteLine("탈퇴 실패");
            msg[0] = (byte)UnityServer.Result.Fail;
        }

        Array.Resize(ref msg, 1);
        msg = CreateResultPacket(msg, ServerPacketId.DeleteResult);

        return ServerPacketId.DeleteResult;
	}

	public ServerPacketId Login (byte[] data)
	{
		Console.WriteLine (tcpPacket.client.RemoteEndPoint.ToString() + " 로그인요청");

		AccountPacket accountPacket = new AccountPacket (data);
		AccountData accountData = accountPacket.GetData ();

		Console.WriteLine ("아이디 : " + accountData.Id + "비밀번호 : " + accountData.password);

        try
        {
            if (database.AccountData.Contains(accountData.Id))
            {
                if (((LoginData)database.AccountData[accountData.Id]).PW == accountData.password)
                {
                    if (!LoginUser.ContainsValue(accountData.Id))
                    {
                        msg[0] = (byte)UnityServer.Result.Success;
                        Console.WriteLine("로그인 성공");
                        LoginUser.Add(tcpPacket.client, accountData.Id);
                    }
                    else
                    {
                    if (CompareIP(GetSocket(accountData.Id).RemoteEndPoint.ToString(), tcpPacket.client.RemoteEndPoint.ToString()))
                        {
                            LoginUser.Remove(GetSocket(accountData.Id));
                        }

                        Console.WriteLine("현재 접속중인 아이디입니다.");
                        msg[0] = (byte)UnityServer.Result.Fail;
                    }
                }
                else
                {
                    Console.WriteLine("패스워드가 맞지 않습니다.");
                    msg[0] = (byte)UnityServer.Result.Fail;
                }
            }
            else
            {
                Console.WriteLine("존재하지 않는 아이디입니다.");
                msg[0] = (byte)UnityServer.Result.Fail;
            }
        }
        catch
        {
            Console.WriteLine("DataHandler::PlayerData.Contains 에러");
            msg[0] = (byte)UnityServer.Result.Fail;
        }

        Array.Resize(ref msg, 1);

        msg = CreateResultPacket(msg, ServerPacketId.LoginResult);

        return ServerPacketId.LoginResult;
	}

	public ServerPacketId Logout (byte[] data)
	{
		Console.WriteLine (tcpPacket.client.RemoteEndPoint.ToString() + " 로그아웃요청");

        string id = LoginUser[tcpPacket.client];

        msg = new byte[1];

		try
		{
			if (LoginUser.ContainsValue (id))
			{
				LoginUser.Remove(tcpPacket.client);
				Console.WriteLine(id + "로그아웃");
                msg[0] = (byte)UnityServer.Result.Success;
            }
			else
			{
				Console.WriteLine ("로그인되어있지 않은 아이디입니다. : " + id);
                msg[0] = (byte)UnityServer.Result.Fail;
            }
		}
		catch
		{
			Console.WriteLine ("DataHandler::PlayerData.Contains 에러");
            msg[0] = (byte)UnityServer.Result.Fail;
        }

        Array.Resize(ref msg, 1);

        msg = CreateResultPacket(msg, ServerPacketId.LoginResult);

        return ServerPacketId.None;
	}

	public ServerPacketId GameClose (byte[] data)
	{
        Console.WriteLine("게임종료");

        try
		{
            Console.WriteLine(tcpPacket.client.RemoteEndPoint.ToString() + "가 접속을 종료했습니다.");
            LoginUser.Remove(tcpPacket.client);
            tcpPacket.client.Close();
		}
		catch
		{
			Console.WriteLine ("DataHandler::LoginUser.Close 에러");
		}

		return ServerPacketId.None;
	}

    public ServerPacketId BuildBuilding(byte[] data)
    {
        string Id = LoginUser[tcpPacket.client];

        BuildPacket buildPacket = new BuildPacket(data);
        Build buildData = buildPacket.GetData();
        DateTime time = DateTime.Now;
        database.GetAccountData(Id).Build(buildData.Id, time);

        Console.WriteLine(buildData.Id + " , " + time.ToString());

        return ServerPacketId.None;
    }

    public ServerPacketId BuildCancel(byte[] data)
    {
        string Id = LoginUser[tcpPacket.client];

        database.GetAccountData(Id).BuildCancel();

        return ServerPacketId.None;
    }

    public ServerPacketId HeroDataRequest(byte[] data)
    {
        string Id = LoginUser[tcpPacket.client];
        int heroId = database.GetAccountData(Id).HeroId;
        int level = database.GetAccountData(Id).HeroLevel;

        HeroData heroData = new HeroData(heroId, level);
        HeroDataPacket heroDataPacket = new HeroDataPacket(heroData);

        msg = CreatePacket(heroDataPacket, ServerPacketId.HeroData);

        return ServerPacketId.HeroData;
    }

    public ServerPacketId ItemDataRequest(byte[] data)
    {
        string Id = LoginUser[tcpPacket.client];
        int[] equipment = database.GetAccountData(Id).Equipment;
        int[] inventory = database.GetAccountData(Id).InventoryId;
        int[] inventoryNum = database.GetAccountData(Id).InventoryNum;

        ItemData itemData = new ItemData(equipment, inventory, inventoryNum);
        ItemDataPacket itemDataPacket = new ItemDataPacket(itemData);

        msg = CreatePacket(itemDataPacket, ServerPacketId.ItemData);

        return ServerPacketId.ItemData;
    }

    public ServerPacketId SkillDataRequest(byte[] data)
    {
        string Id = LoginUser[tcpPacket.client];
        int[] skill = database.GetAccountData(Id).Skill;

        SkillData skillData = new SkillData(skill);
        SkillDataPacket skillDataPacket = new SkillDataPacket(skillData);

        msg = CreatePacket(skillDataPacket, ServerPacketId.SkillData);

        return ServerPacketId.SkillData;
    }

    public ServerPacketId UnitDataRequest(byte[] data)
    {
        string Id = LoginUser[tcpPacket.client];
        int unitKind = database.GetAccountData(Id).UnitKind;
        int createUnitKind = database.GetAccountData(Id).CreateUnitKind;
        int attackUnitKind = database.GetAccountData(Id).AttackUnitKind;

        Unit[] unit = database.GetAccountData(Id).Unit;
        Unit[] createUnit = database.GetAccountData(Id).CreateUnit;
        Unit[] attackUnit = database.GetAccountData(Id).AttackUnit;
        
        UnitData[] unitData = new UnitData[3];
        unitData[0] = new UnitData(unitKind, unit);
        unitData[1] = new UnitData(createUnitKind, createUnit);
        unitData[2] = new UnitData(attackUnitKind, attackUnit);

        UnitDataPacket unitDataPacket = new UnitDataPacket(unitData);

        msg = CreatePacket(unitDataPacket, ServerPacketId.UnitData);

        return ServerPacketId.UnitData;
    }

    public ServerPacketId BuildingDataRequest(byte[] data)
    {
        string Id = LoginUser[tcpPacket.client];
        int[] building = database.GetAccountData(Id).Skill;

        BuildingData buildingData = new BuildingData(building);
        BuildingDataPacket buildingDataPacket = new BuildingDataPacket(buildingData);

        msg = CreatePacket(buildingDataPacket, ServerPacketId.BuildingData);

        return ServerPacketId.SkillData;
    }

    public ServerPacketId UpgradeDataRequest(byte[] data)
    {
        string Id = LoginUser[tcpPacket.client];
        int[] upgrade = database.GetAccountData(Id).Upgrade;

        UpgradeData upgradeData = new UpgradeData(upgrade);
        UpgradeDataPacket upgradeDataPacket = new UpgradeDataPacket(upgradeData);

        msg = CreatePacket(upgradeDataPacket, ServerPacketId.UpgradeData);

        return ServerPacketId.UpgradeData;
    }

    public ServerPacketId ResourceDataRequest(byte[] data)
    {
        string Id = LoginUser[tcpPacket.client];
        int resource = database.GetAccountData(Id).Resource;

        ResourceData resourceData = new ResourceData(resource);
        ResourceDataPacket resourceDataPacket = new ResourceDataPacket(resourceData);

        msg = CreatePacket(resourceDataPacket, ServerPacketId.ResourceData);

        return ServerPacketId.ResourceData;
    }

    public ServerPacketId StateDataRequest(byte[] data)
    {
        string Id = LoginUser[tcpPacket.client];
        byte heroState = (byte)database.GetAccountData(Id).HState;
        byte CastleState = (byte)database.GetAccountData(Id).CState;

        StateData stateData = new StateData();
        StateDataPacket stateDataPacket = new StateDataPacket(stateData);

        msg = CreatePacket(stateDataPacket, ServerPacketId.StateData);

        return ServerPacketId.StateData;
    }

    public ServerPacketId BuildDataRequest(byte[] data)
    {
        string Id = LoginUser[tcpPacket.client];
        
        byte buildBuilding = (byte)database.GetAccountData(Id).BuildBuilding;
        DateTime time;

        if (buildBuilding != 0)
        {
            time = database.GetAccountData(Id).BuildTime;
        }
        else
        {
            time = DateTime.Now;
        }

        BuildData buildData = new BuildData(buildBuilding, time);
        BuildDataPacket buildDataPacket = new BuildDataPacket(buildData);

        msg = CreatePacket(buildDataPacket, ServerPacketId.BuildData);

        return ServerPacketId.BuildData;
    }

    public void BuildChecker()
    {
        while (true)
        {
            Thread.Sleep(1800000);

            foreach (KeyValuePair<Socket, string> client in LoginUser)
            {
                if(database.GetAccountData(client.Value).BuildTime < DateTime.Now)
                {
                    
                }
            }
        }        
    }

    byte[] CreateHeader<T>(IPacket<T> data, ServerPacketId Id)
    {
        byte[] msg = data.GetPacketData();

        HeaderData headerData = new HeaderData();
        HeaderSerializer headerSerializer = new HeaderSerializer();

        headerData.Id = (byte)Id;
        headerData.length = (short)msg.Length;

        headerSerializer.Serialize(headerData);
        byte[] header = headerSerializer.GetSerializedData();

        return header;
    }

    byte[] CreatePacket<T>(IPacket<T> data, ServerPacketId Id)
    {
        byte[] msg = data.GetPacketData();
        byte[] header = CreateHeader(data, Id);
        byte[] packet = CombineByte(header, msg);

        return packet;
    }

    byte[] CreateResultPacket(byte[] msg, ServerPacketId Id)
    {
        HeaderData headerData = new HeaderData();
        HeaderSerializer HeaderSerializer = new HeaderSerializer();

        headerData.Id = (byte)Id;
        headerData.length = (short)msg.Length;
        HeaderSerializer.Serialize(headerData);
        msg = CombineByte(HeaderSerializer.GetSerializedData(), msg);
        return msg;
    }

    bool CompareIP(string ip1, string ip2)
    {
        if(ip1.Substring(0, ip1.IndexOf(":")) == ip2.Substring(0, ip2.IndexOf(":")))
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    public Socket GetSocket(string Id)
    {
        foreach (KeyValuePair<Socket, string> client in LoginUser)
        {
            if (client.Value == Id)
            {
                return client.Key;
            }
        }

        return null;
    }

    public static byte[] CombineByte (byte[] array1, byte[] array2)
	{
		byte[] array3 = new byte[array1.Length + array2.Length];
		Array.Copy (array1, 0, array3, 0, array1.Length);
		Array.Copy (array2, 0, array3, array1.Length, array2.Length);
		return array3;
	}

	public static byte[] CombineByte (byte[] array1, byte[] array2, byte[] array3)
	{
		byte[] array4 = CombineByte (CombineByte (array1, array2), array3);;
		return array4;
	}
}

[Serializable]
public class TcpClient
{
	public Socket client;
	public string Id;

	public TcpClient (Socket newClient)
	{
		client = newClient;
		Id = "";
	}
}

public class HeaderData
{
    // 헤더 == [2바이트 - 패킷길이][1바이트 - ID]
    public short length; // 패킷의 길이
    public byte Id; // 패킷 ID
}