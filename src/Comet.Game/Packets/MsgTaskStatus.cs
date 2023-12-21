// //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) FTW! Masters
// Keep the headers and the patterns adopted by the project. If you changed anything in the file just insert
// your name below, but don't remove the names of who worked here before.
// 
// This project is a fork from Comet, a Conquer Online Server Emulator created by Spirited, which can be
// found here: https://gitlab.com/spirited/comet
// 
// Comet - Comet.Game - MsgTaskStatus.cs
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
using Comet.Game.States;
using Comet.Network.Packets;

#endregion

namespace Comet.Game.Packets
{
    public sealed class MsgTaskStatus : MsgBase<Client>
    {
        public TaskStatusMode Mode { get; set; }
        public ushort Amount { get; set; }
        public List<TaskItemStruct> Tasks = new List<TaskItemStruct>();

        public override void Decode(byte[] bytes)
        {
            PacketReader reader = new PacketReader(bytes);
            Length = reader.ReadUInt16();
            Type = (PacketType)reader.ReadUInt16();
            Mode = (TaskStatusMode) reader.ReadUInt16();
            Amount = reader.ReadUInt16();
            for (int i = 0; i < Amount; i++)
            {
                TaskItemStruct item = new TaskItemStruct
                {
                    Identity = reader.ReadInt32(),
                    Status = (TaskItemStatus) reader.ReadInt32()
                };
                Tasks.Add(item);
            }
        }

        public override byte[] Encode()
        {
            PacketWriter writer = new PacketWriter();
            writer.Write((ushort)PacketType.MsgTaskStatus);
            writer.Write((ushort) Mode);
            writer.Write(Amount = (ushort) Tasks.Count);
            foreach (var task in Tasks)
            {
                writer.Write(task.Identity);
                writer.Write((int) task.Status);
                // writer.Write(task.Unknown);
            }
            return writer.ToArray();
        }

        public override async Task ProcessAsync(Client client)
        {
            foreach (var item in Tasks)
            {
                item.Status = TaskItemStatus.Available;
            }
            await client.SendAsync(this);
        }

        public class TaskItemStruct
        {
            public int Identity { get; set; }
            public TaskItemStatus Status { get; set; }
            public int Unknown { get; set; }
        }

        public enum TaskItemStatus : byte
        {
            Accepted = 0,
            Done = 1,
            Available = 2,
            Event = 3,
            Daily = 4,
            AcceptedWithoutTrace = 5,
            Quitted = 6,
            None = 255,
        }

        public enum TaskStatusMode : byte
        {
            None = 0,
            Add = 1,
            Remove = 2,
            Update = 3,
            Finish = 4,
            Quit = 8,
        }
    }
}