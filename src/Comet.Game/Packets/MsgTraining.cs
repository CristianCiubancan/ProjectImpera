// //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) FTW! Masters
// Keep the headers and the patterns adopted by the project. If you changed anything in the file just insert
// your name below, but don't remove the names of who worked here before.
// 
// This project is a fork from Comet, a Conquer Online Server Emulator created by Spirited, which can be
// found here: https://gitlab.com/spirited/comet
// 
// Comet - Comet.Game - MsgTraining.cs
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

using System;
using System.Threading.Tasks;
using Comet.Game.States;
using Comet.Network.Packets;
using Comet.Shared;

#endregion

namespace Comet.Game.Packets
{
    public class MsgTraining : MsgBase<Client>
    {
        public enum Mode
        {
            RequestTime,
            RequestEnter,
            Unknown2,
            RequestRewardInfo,
            ClaimReward
        }

        public MsgTraining()
        {
            Type = PacketType.MsgTraining;
        }

        public Mode Action { get; set; }
        public ulong TrainingTime { get; set; }

        public override void Decode(byte[] bytes)
        {
            PacketReader reader = new PacketReader(bytes);
            Length = reader.ReadUInt16();
            Type = (PacketType)reader.ReadUInt16();
            Action = (Mode)reader.ReadUInt32();
            TrainingTime = reader.ReadUInt64();
        }

        public override byte[] Encode()
        {
            PacketWriter writer = new PacketWriter();
            writer.Write((ushort)Type);
            writer.Write((uint)Action);
            writer.Write(TrainingTime);
            return writer.ToArray();
        }

        public override async Task ProcessAsync(Client client)
        {
            switch (Action)
            {
                case Mode.RequestTime:
                    {
                        TrainingTime = client.Character.CurrentTrainingMinutes;
                        await client.Character.SendAsync(this);
                        break;
                    }

                case Mode.RequestEnter:
                    {
                        if (!client.Character.IsBlessed || client.Character.CurrentTrainingMinutes == 0)
                        {
                            await client.Character.SendAsync(Language.StrCannotEnterTG);
                            return;
                        }

                        await client.Character.SendAsync(this);

                        if (client.Character.MapIdentity != 601)
                            await client.Character.EnterAutoExerciseAsync();
                        break;
                    }

                case Mode.RequestRewardInfo:
                    {
                        var currData = client.Character.GetCurrentOnlineTGExp();
                        await client.Character.SendAsync(new MsgTrainingInfo
                        {
                            Experience = (int)currData.Experience,
                            Level = currData.Level,
                            TimeRemaining = (ushort)(client.Character.CurrentTrainingTime - Math.Min(client.Character.CurrentOfflineTrainingTime, client.Character.CurrentTrainingTime)),
                            TimeUsed = Math.Min(client.Character.CurrentOfflineTrainingTime, client.Character.CurrentTrainingTime)
                        });
                        break;
                    }

                case Mode.ClaimReward:
                    {
                        await client.Character.LeaveAutoExerciseAsync();
                        break;
                    }

                default:
                    await Log.WriteLogAsync(LogLevel.Warning, $"Unhandled MsgTraining::{Action}");
                    break;
            }
        }
    }
}