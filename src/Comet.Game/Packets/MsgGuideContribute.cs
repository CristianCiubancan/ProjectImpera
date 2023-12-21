// //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) FTW! Masters
// Keep the headers and the patterns adopted by the project. If you changed anything in the file just insert
// your name below, but don't remove the names of who worked here before.
// 
// This project is a fork from Comet, a Conquer Online Server Emulator created by Spirited, which can be
// found here: https://gitlab.com/spirited/comet
// 
// Comet - Comet.Game - MsgGuideContribute.cs
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
using Comet.Game.States.Items;
using Comet.Network.Packets;
using Comet.Shared;

#endregion

namespace Comet.Game.Packets
{
    public sealed class MsgGuideContribute : MsgBase<Client>
    {
        public enum RequestType
        {
            ClaimExperience = 1,
            ClaimHeavenBlessing = 2,
            ClaimItemAdd = 3,
            Query = 4
        }

        public MsgGuideContribute()
        {
            Type = PacketType.MsgGuideContribute;
        }

        public RequestType Mode;
        public uint Identity;
        public byte[] Padding = new byte[12];
        public uint Experience;
        public ushort HeavenBlessing;
        public ushort Composing;
        public ushort Test1;
        public ushort Test2;

        public override void Decode(byte[] bytes)
        {
            PacketReader reader = new PacketReader(bytes);
            Length = reader.ReadUInt16();
            Type = (PacketType)reader.ReadUInt16();
            Mode = (RequestType)reader.ReadUInt32();
            Identity = reader.ReadUInt32();
            Padding = reader.ReadBytes(12);
            Experience = reader.ReadUInt32();
            HeavenBlessing = reader.ReadUInt16();
            Composing = reader.ReadUInt16();
            Test1 = reader.ReadUInt16();
            Test2 = reader.ReadUInt16();
        }

        public override byte[] Encode()
        {
            PacketWriter writer = new PacketWriter();
            writer.Write((ushort)Type);
            writer.Write((int)Mode);
            writer.Write(Identity);
            writer.Write(Padding);
            writer.Write(Experience);
            writer.Write(Test1);
            writer.Write(Test2);
            writer.Write(HeavenBlessing);
            writer.Write(Composing);
            return writer.ToArray();
        }

        public override async Task ProcessAsync(Client client)
        {
            Character user = client.Character;

            switch (Mode)
            {
                case RequestType.Query:
                    {
                        Experience = (uint)user.MentorExpTime;
                        Composing = (ushort)user.MentorAddLevexp;
                        HeavenBlessing = (ushort)user.MentorGodTime;
                        await client.SendAsync(this);
                        break;
                    }

                case RequestType.ClaimExperience:
                    {
                        if (user.MentorExpTime > 0)
                        {
                            await user.AwardExperienceAsync(user.CalculateExpBall((int)user.MentorExpTime), true);

                            user.MentorExpTime = 0;
                            await user.SaveTutorAccessAsync();
                        }
                        break;
                    }

                case RequestType.ClaimHeavenBlessing:
                    {
                        if (user.MentorGodTime > 0)
                        {
                            await user.AddBlessingAsync(user.MentorGodTime);
                            user.MentorGodTime = 0;
                            await user.SaveTutorAccessAsync();
                        }
                        
                        break;
                    }

                case RequestType.ClaimItemAdd:
                    {
                        int stoneAmount = (int)(user.MentorAddLevexp / 100);

                        if (!user.UserPackage.IsPackSpare(stoneAmount))
                        {
                            await user.SendAsync(Language.StrYourBagIsFull);
                            return;
                        }

                        for (int i = 0; i < stoneAmount; i++)
                        {
                            if (await user.UserPackage.AwardItemAsync(Item.TYPE_STONE1))
                                user.MentorAddLevexp -= 100;
                        }

                        await user.SaveTutorAccessAsync();
                        break;
                    }

                default:
                    {
                        await client.SendAsync(this);
                        if (client.Character.IsPm())
                        {
                            await client.SendAsync(new MsgTalk(client.Identity, MsgTalk.TalkChannel.Service,
                                $"Missing packet {Type}, Action {Mode}, Length {Length}"));
                        }

                        await Log.WriteLogAsync(LogLevel.Warning,
                            "Missing packet {0}, Action {1}, Length {2}\n{3}",
                            Type, Mode, Length, PacketDump.Hex(Encode()));
                        break;
                    }
            }
        }
    }
}