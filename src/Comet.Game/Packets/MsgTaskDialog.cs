// //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) FTW! Masters
// Keep the headers and the patterns adopted by the project. If you changed anything in the file just insert
// your name below, but don't remove the names of who worked here before.
// 
// This project is a fork from Comet, a Conquer Online Server Emulator created by Spirited, which can be
// found here: https://gitlab.com/spirited/comet
// 
// Comet - Comet.Game - MsgTaskDialog.cs
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

using System.Collections.Generic;
using System.Threading.Tasks;
using Comet.Game.Database.Models;
using Comet.Game.States;
using Comet.Game.States.BaseEntities;
using Comet.Game.States.Syndicates;
using Comet.Game.World.Maps;
using Comet.Network.Packets;
using Comet.Shared;

#endregion

namespace Comet.Game.Packets
{
    public sealed class MsgTaskDialog : MsgBase<Client>
    {
        public MsgTaskDialog()
        {
            Type = PacketType.MsgTaskDialog;
            Text = string.Empty;
        }

        public uint TaskIdentity { get; set; }
        public ushort Data { get; set; }
        public byte OptionIndex { get; set; }
        public TaskInteraction InteractionType { get; set; }
        public string Text { get; set; }

        /// <summary>
        ///     Decodes a byte packet into the packet structure defined by this message class.
        ///     Should be invoked to structure data from the client for processing. Decoding
        ///     follows TQ Digital's byte ordering rules for an all-binary protocol.
        /// </summary>
        /// <param name="bytes">Bytes from the packet processor or client socket</param>
        public override void Decode(byte[] bytes)
        {
            var reader = new PacketReader(bytes);
            Length = reader.ReadUInt16();
            Type = (PacketType)reader.ReadUInt16();
            TaskIdentity = reader.ReadUInt32();
            Data = reader.ReadUInt16();
            OptionIndex = reader.ReadByte();
            InteractionType = (TaskInteraction)reader.ReadByte();
            List<string> strings = reader.ReadStrings();
            Text = strings.Count > 0 ? strings[0] : "";
        }

        /// <summary>
        ///     Encodes the packet structure defined by this message class into a byte packet
        ///     that can be sent to the client. Invoked automatically by the client's send
        ///     method. Encodes using byte ordering rules interoperable with the game client.
        /// </summary>
        /// <returns>Returns a byte packet of the encoded packet.</returns>
        public override byte[] Encode()
        {
            var writer = new PacketWriter();
            writer.Write((ushort)Type);
            writer.Write(TaskIdentity);
            writer.Write(Data);
            writer.Write(OptionIndex);
            writer.Write((byte)InteractionType);
            writer.Write(new List<string> { Text });
            return writer.ToArray();
        }

        public override async Task ProcessAsync(Client client)
        {
            Character user = client.Character;

            switch (InteractionType)
            {
                case TaskInteraction.MessageBox:
                    {
                        if (user.MessageBox != null)
                        {
                            if (OptionIndex == 0)
                                await user.MessageBox.OnCancelAsync();
                            else
                                await user.MessageBox.OnAcceptAsync();

                            user.MessageBox = null;
                        }
                        else
                        {
                            uint idTask = user.GetTaskId(OptionIndex);

                            user.ClearTaskId();
                            await GameAction.ExecuteActionAsync(idTask, user,
                                Kernel.RoleManager.GetRole(user.InteractingNpc), user.UserPackage[user.InteractingItem], Text);
                        }

                        break;
                    }

                case TaskInteraction.Answer:
                    {
                        if (OptionIndex == byte.MaxValue)
                        {
                            return;
                        }

                        Role targetRole = Kernel.RoleManager.GetRole(user.InteractingNpc);
                        if (targetRole != null
                            && targetRole.MapIdentity != 5000
                            && targetRole.MapIdentity != user.MapIdentity)
                        {
                            user.CancelInteraction();
                            return;
                        }

                        if (targetRole != null
                            && targetRole.GetDistance(user) > Screen.VIEW_SIZE)
                        {
                            user.CancelInteraction();
                            return;
                        }

                        if (user.InteractingNpc == 0 && user.InteractingItem == 0)
                        {
                            user.CancelInteraction();
                            return;
                        }

                        uint idTask = user.GetTaskId(OptionIndex);
                        DbTask task = Kernel.EventManager.GetTask(idTask);
                        if (task == null)
                        {
                            if (OptionIndex != 0)
                            {
                                user.CancelInteraction();

                                if (user.IsGm() && idTask != 0)
                                    await user.SendAsync($"Could not find InteractionAsnwer for task {idTask}");
                            }

                            return;
                        }

                        user.ClearTaskId();
                        await GameAction.ExecuteActionAsync(await user.TestTaskAsync(task) ? task.IdNext : task.IdNextfail, user,
                            targetRole, user.UserPackage[user.InteractingItem], Text);
                        break;
                    }
                case TaskInteraction.TextInput:
                    {
                        if (TaskIdentity == 31100)
                        {
                            if (user.SyndicateIdentity == 0 ||
                                user.SyndicateRank < SyndicateMember.SyndicateRank.DeputyLeader)
                                return;

                            await user.Syndicate.KickOutMemberAsync(user, Text);
                            await user.Syndicate.SendMembersAsync(0, user);
                            return;
                        }

                        break;
                    }
                default:
                    await Log.WriteLogAsync(LogLevel.Warning, $"MsgTaskDialog: {Type}, {InteractionType} unhandled");
                    break;
            }
        }

        public enum TaskInteraction : byte
        {
            ClientRequest = 0,
            Dialog = 1,
            Option = 2,
            Input = 3,
            Avatar = 4,
            LayNpc = 5,
            MessageBox = 6,
            Finish = 100,
            Answer = 101,
            TextInput = 102,
            UpdateWindow = 112
        }
    }
}