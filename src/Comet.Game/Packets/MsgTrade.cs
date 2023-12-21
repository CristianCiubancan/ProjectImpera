// //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) FTW! Masters
// Keep the headers and the patterns adopted by the project. If you changed anything in the file just insert
// your name below, but don't remove the names of who worked here before.
// 
// This project is a fork from Comet, a Conquer Online Server Emulator created by Spirited, which can be
// found here: https://gitlab.com/spirited/comet
// 
// Comet - Comet.Game - MsgTrade.cs
// Description:
// 
// Creator: FELIPEVIEIRAVENDRAMI [FELIPE VIEIRA VENDRAMINI]
// 
// Developed by:
// Felipe Vieira Vendramini <felipevendramini@live.com>
// 
// Programming today is a race between software engineers striving to build bigger and better
// idiot-proof programs, and the Universe trying to produce bigger and better idiots.
// So far, the Universe is winning.
// //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

#region References

using System.Threading.Tasks;
using Comet.Game.States;
using Comet.Game.World.Maps;
using Comet.Network.Packets;

#endregion

namespace Comet.Game.Packets
{
    public sealed class MsgTrade : MsgBase<Client>
    {
        public enum TradeAction
        {
            Apply = 1,
            Quit,
            Open,
            Success,
            Fail,
            AddItem,
            AddMoney,
            ShowMoney,
            Accept = 10,
            AddItemFail,
            ShowConquerPoints,
            AddConquerPoints,
            Timeout = 17
        }

        public MsgTrade()
        {
            Type = PacketType.MsgTrade;
        }

        public uint Data { get; set; }
        public TradeAction Action { get; set; }

        public override void Decode(byte[] bytes)
        {
            PacketReader reader = new PacketReader(bytes);
            Length = reader.ReadUInt16();
            Type = (PacketType) reader.ReadUInt16();
            Data = reader.ReadUInt32();
            Action = (TradeAction) reader.ReadUInt32();
        }

        public override byte[] Encode()
        {
            PacketWriter writer = new PacketWriter();
            writer.Write((ushort) Type);
            writer.Write(Data);
            writer.Write((uint) Action);
            return writer.ToArray();
        }

        public override async Task ProcessAsync(Client client)
        {
            Character user = client.Character;
            Character target = null;

            switch (Action)
            {
                case TradeAction.Apply:
                    if (Data == 0)
                        return;

                    target = Kernel.RoleManager.GetUser(Data);
                    if (target == null || target.MapIdentity != user.MapIdentity ||
                        target.GetDistance(user) > Screen.VIEW_SIZE)
                    {
                        await user.SendAsync(Language.StrTargetNotInRange);
                        return;
                    }

                    if (user.Trade != null)
                    {
                        await user.SendAsync(Language.StrTradeYouAlreadyTrade);
                        return;
                    }

                    if (target.Trade != null)
                    {
                        await user.SendAsync(Language.StrTradeTargetAlreadyTrade);
                        return;
                    }

                    if (target.QueryRequest(RequestType.Trade) == user.Identity)
                    {
                        target.PopRequest(RequestType.Trade);
                        user.Trade = target.Trade = new Trade(target, user);
                        await user.SendAsync(new MsgTrade { Action = TradeAction.Open, Data = target.Identity });
                        await target.SendAsync(new MsgTrade { Action = TradeAction.Open, Data = user.Identity });
                        return;
                    }

                    Data = user.Identity;
                    await target.SendAsync(this);
                    await target.SendRelationAsync(user);
                    user.SetRequest(RequestType.Trade, target.Identity);
                    await user.SendAsync(Language.StrTradeRequestSent);
                    break;

                case TradeAction.Quit:
                    if (user.Trade != null)
                        await user.Trade.SendCloseAsync();
                    break;

                case TradeAction.AddItem:
                    if (user.Trade != null)
                        await user.Trade.AddItemAsync(Data, user);
                    break;

                case TradeAction.AddMoney:
                    if (user.Trade != null)
                        await user.Trade.AddMoneyAsync(Data, user);
                    break;

                case TradeAction.Accept:
                    if (user.Trade != null)
                        await user.Trade.AcceptAsync(user.Identity);
                    break;

                case TradeAction.AddConquerPoints:
                    if (user.Trade != null)
                        await user.Trade.AddEmoneyAsync(Data, user);
                    break;
            }
        }
    }
}