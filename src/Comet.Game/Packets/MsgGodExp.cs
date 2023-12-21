// //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) FTW! Masters
// Keep the headers and the patterns adopted by the project. If you changed anything in the file just insert
// your name below, but don't remove the names of who worked here before.
// 
// This project is a fork from Comet, a Conquer Online Server Emulator created by Spirited, which can be
// found here: https://gitlab.com/spirited/comet
// 
// Comet - Comet.Game - MsgGodExp.cs
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
using Comet.Network.Packets;

#endregion

namespace Comet.Game.Packets
{
    public sealed class MsgGodExp : MsgBase<Client>
    {
        public enum MsgGodExpAction
        {
            Query,
            ClaimOnlineTraining,
            ClaimHuntTraining
        }

        public MsgGodExpAction Action;
        public int GodTimeExp;
        public int HuntExp;

        public override void Decode(byte[] bytes)
        {
            var reader = new PacketReader(bytes);
            Length = reader.ReadUInt16();
            Type = (PacketType)reader.ReadUInt16();
            GodTimeExp = reader.ReadInt32();
            HuntExp = reader.ReadInt32();
        }

        public override byte[] Encode()
        {
            PacketWriter writer = new PacketWriter();
            writer.Write((ushort)PacketType.MsgGodExp);
            writer.Write((uint)Action);
            writer.Write(GodTimeExp);
            writer.Write(HuntExp);
            return writer.ToArray();
        }

        public override async Task ProcessAsync(Client client)
        {
            Character user = client.Character;

            switch (Action)
            {
                case MsgGodExpAction.Query:
                    {
                        GodTimeExp = (int)user.GodTimeExp;
                        HuntExp = (int)user.OnlineTrainingExp;
                        await client.SendAsync(this);
                        break;
                    }

                case MsgGodExpAction.ClaimHuntTraining:
                    {
                        if (user.OnlineTrainingExp > 0)
                        {
                            await user.AwardExperienceAsync(user.CalculateExpBall((int)user.OnlineTrainingExp), true);
                            user.OnlineTrainingExp = 0;
                            await user.SaveAsync();

                            await client.SendAsync(new MsgGodExp
                            {
                                Action = MsgGodExpAction.Query,
                                GodTimeExp = (int)user.GodTimeExp,
                                HuntExp = (int)user.OnlineTrainingExp
                            });
                        }

                        break;
                    }

                case MsgGodExpAction.ClaimOnlineTraining:
                    {
                        if (user.GodTimeExp > 0)
                        {
                            await user.AwardExperienceAsync(user.CalculateExpBall((int)user.GodTimeExp), true);
                            user.GodTimeExp = 0;
                            await user.SaveAsync();

                            await client.SendAsync(new MsgGodExp
                            {
                                Action = MsgGodExpAction.Query,
                                GodTimeExp = (int)user.GodTimeExp,
                                HuntExp = (int)user.OnlineTrainingExp
                            });
                        }

                        break;
                    }
            }
        }
    }
}