// //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) FTW! Masters
// Keep the headers and the patterns adopted by the project. If you changed anything in the file just insert
// your name below, but don't remove the names of who worked here before.
// 
// This project is a fork from Comet, a Conquer Online Server Emulator created by Spirited, which can be
// found here: https://gitlab.com/spirited/comet
// 
// Comet - Comet.Game - MsgGuide.cs
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
using Comet.Game.Database.Models;
using Comet.Game.States;
using Comet.Game.States.Guide;
using Comet.Network.Packets;

#endregion

namespace Comet.Game.Packets
{
    public sealed class MsgGuide : MsgBase<Client>
    {
        public enum Request
        {
            InviteApprentice = 1,
            RequestMentor = 2,
            LeaveMentor = 3,
            ExpellApprentice = 4,
            AcceptRequestApprentice = 8,
            AcceptRequestMentor = 9,
            DumpApprentice = 18,
            DumpMentor = 19
        }

        public MsgGuide()
        {
            Type = PacketType.MsgGuide;
        }

        public Request Action;
        public uint Identity;
        public uint Param;
        public uint Param2;
        public bool Online;
        public string Name;

        public override void Decode(byte[] bytes)
        {
            PacketReader reader = new PacketReader(bytes);
            Length = reader.ReadUInt16();
            Type = (PacketType)reader.ReadUInt16();
            Action = (Request)reader.ReadUInt32();
            Identity = reader.ReadUInt32();
            Param = reader.ReadUInt32();
            Param2 = reader.ReadUInt32();
            Online = reader.ReadBoolean();
            Name = reader.ReadString(reader.ReadByte());
        }

        public override byte[] Encode()
        {
            PacketWriter writer = new PacketWriter();
            writer.Write((ushort)Type);
            writer.Write((uint)Action);
            writer.Write(Identity);
            writer.Write(Param);
            writer.Write(Param2);
            writer.Write(Online);
            writer.Write(Name);
            return writer.ToArray();
        }

        public override async Task ProcessAsync(Client client)
        {
            Character user = client.Character;

            switch (Action)
            {
                case Request.InviteApprentice:
                    {
                        Character target = Kernel.RoleManager.GetUser(Param);
                        if (target == null)
                            return;

                        if (user.Level < target.Level || user.Metempsychosis < target.Metempsychosis)
                        {
                            await user.SendAsync(Language.StrGuideStudentHighLevel);
                            return;
                        }

                        int deltaLevel = user.Level - target.Level;
                        if (target.Metempsychosis == 0)
                        {
                            if (deltaLevel > 30)
                            {
                                await user.SendAsync(Language.StrGuideStudentHighLevel);
                                return;
                            }
                        }
                        else if (target.Metempsychosis == 1)
                        {
                            if (deltaLevel > 20)
                            {
                                await user.SendAsync(Language.StrGuideStudentHighLevel);
                                return;
                            }
                        }
                        else
                        {
                            if (deltaLevel < 10)
                            {
                                await user.SendAsync(Language.StrGuideStudentHighLevel);
                                return;
                            }
                        }

                        DbTutorType type = Kernel.RoleManager.GetTutorType(user.Level);
                        if (type == null || user.ApprenticeCount >= type.StudentNum)
                        {
                            await user.SendAsync(Language.StrGuideTooManyStudents);
                            return;
                        }

                        target.SetRequest(RequestType.Guide, user.Identity);

                        await target.SendAsync(new MsgGuide
                        {
                            Identity = user.Identity,
                            Param = user.Identity,
                            Param2 = (uint)user.BattlePower,
                            Action = Request.AcceptRequestApprentice,
                            Online = true,
                            Name = user.Name
                        });
                        await target.SendRelationAsync(user);

                        await user.SendAsync(Language.StrGuideSendTutor);
                        break;
                    }

                case Request.RequestMentor:
                    {
                        Character target = Kernel.RoleManager.GetUser(Param);
                        if (target == null)
                            return;

                        if (target.Level < user.Level || target.Metempsychosis < user.Metempsychosis)
                        {
                            await user.SendAsync(Language.StrGuideStudentHighLevel1);
                            return;
                        }

                        int deltaLevel = target.Level - user.Level;
                        if (target.Metempsychosis == 0)
                        {
                            if (deltaLevel < 30)
                            {
                                await user.SendAsync(Language.StrGuideStudentHighLevel1);
                                return;
                            }
                        }
                        else if (target.Metempsychosis == 1)
                        {
                            if (deltaLevel > 20)
                            {
                                await user.SendAsync(Language.StrGuideStudentHighLevel1);
                                return;
                            }
                        }
                        else
                        {
                            if (deltaLevel < 10)
                            {
                                await user.SendAsync(Language.StrGuideStudentHighLevel1);
                                return;
                            }
                        }

                        DbTutorType type = Kernel.RoleManager.GetTutorType(target.Level);
                        if (type == null || target.ApprenticeCount >= type.StudentNum)
                        {
                            await user.SendAsync(Language.StrGuideTooManyStudents1);
                            return;
                        }

                        target.SetRequest(RequestType.Guide, user.Identity);

                        await target.SendAsync(new MsgGuide
                        {
                            Identity = user.Identity,
                            Param = user.Identity,
                            Param2 = (uint)user.BattlePower,
                            Action = Request.AcceptRequestApprentice,
                            Online = true,
                            Name = user.Name
                        });
                        await target.SendRelationAsync(user);

                        await user.SendAsync(Language.StrGuideSendTutor);
                        break;
                    }

                case Request.LeaveMentor:
                    {
                        if (user.Guide == null)
                            return;

                        if (user.Guide.Betrayed)
                            return;

                        if (!await user.SpendMoneyAsync(Tutor.STUDENT_BETRAYAL_VALUE, true))
                            return;

                        await user.Guide.BetrayAsync();

                        var guide = user.Guide.Guide;
                        if (guide != null)
                        {
                            await guide.SendAsync(string.Format(Language.StrGuideBetrayTutor, user.Name));
                        }

                        await user.Guide.SendTutorAsync();
                        break;
                    }

                case Request.DumpApprentice:
                    {
                        if (!user.IsApprentice(Identity))
                            return;

                        var tutor = user.GetStudent(Identity);
                        if (tutor == null)
                            return;

                        if (tutor.Betrayed)
                            return; // already dumped :]

                        await tutor.BetrayAsync();

                        var student = tutor.Student;
                        if (student != null)
                        {
                            await student.SendAsync(string.Format(Language.StrGuideBetray, tutor.GuideName));
                        }

                        await tutor.SendStudentAsync();
                        break;
                    }

                case Request.AcceptRequestApprentice:
                    {
                        Character target = Kernel.RoleManager.GetUser(Identity);
                        if (target == null)
                            return;

                        if (Param2 == 0)
                        {
                            await target.SendAsync(Language.StrGuideDeclined);
                            return;
                        }

                        if (user.QueryRequest(RequestType.Guide) == Identity)
                        {
                            user.PopRequest(RequestType.Guide);
                            await Character.CreateTutorRelationAsync(user, target);
                            return;
                        }

                        break;
                    }

                case Request.AcceptRequestMentor:
                    {
                        if (Param2 == 0)
                        {
                            await user.SendAsync(Language.StrGuideDeclined);
                            return;
                        }

                        Character target = Kernel.RoleManager.GetUser(Identity);
                        if (target == null)
                            return;

                        if (user.QueryRequest(RequestType.Guide) == Identity)
                        {
                            user.PopRequest(RequestType.Guide);
                            await Character.CreateTutorRelationAsync(target, user);
                            return;
                        }

                        break;
                    }

                default:
                    if (user.IsPm())
                        await user.SendAsync($"Unhandled MsgGuide:{Action}", MsgTalk.TalkChannel.Talk);
                    break;
            }
        }
    }
}