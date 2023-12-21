// //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) FTW! Masters
// Keep the headers and the patterns adopted by the project. If you changed anything in the file just insert
// your name below, but don't remove the names of who worked here before.
// 
// This project is a fork from Comet, a Conquer Online Server Emulator created by Spirited, which can be
// found here: https://gitlab.com/spirited/comet
// 
// Comet - Comet.Game - DbNpc.cs
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

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Comet.Game.Database.Models
{
    [Table("cq_npc")]
    public class DbNpc
    {
        [Key] [Column("id")] public virtual uint Id { get; set; }
        [Column("ownerid")] public virtual uint Ownerid { get; set; }
        [Column("playerid")] public virtual uint Playerid { get; set; }
        [Column("name")] public virtual string Name { get; set; }
        [Column("type")] public virtual ushort Type { get; set; }
        [Column("lookface")] public virtual ushort Lookface { get; set; }
        [Column("idxserver")] public virtual int Idxserver { get; set; }
        [Column("mapid")] public virtual uint Mapid { get; set; }
        [Column("cellx")] public virtual ushort Cellx { get; set; }
        [Column("celly")] public virtual ushort Celly { get; set; }
        [Column("task0")] public virtual uint Task0 { get; set; }
        [Column("task1")] public virtual uint Task1 { get; set; }
        [Column("task2")] public virtual uint Task2 { get; set; }
        [Column("task3")] public virtual uint Task3 { get; set; }
        [Column("task4")] public virtual uint Task4 { get; set; }
        [Column("task5")] public virtual uint Task5 { get; set; }
        [Column("task6")] public virtual uint Task6 { get; set; }
        [Column("task7")] public virtual uint Task7 { get; set; }
        [Column("data0")] public virtual int Data0 { get; set; }
        [Column("data1")] public virtual int Data1 { get; set; }
        [Column("data2")] public virtual int Data2 { get; set; }
        [Column("data3")] public virtual int Data3 { get; set; }
        [Column("datastr")] public virtual string Datastr { get; set; }
        [Column("linkid")] public virtual uint Linkid { get; set; }
        [Column("life")] public virtual ushort Life { get; set; }
        [Column("maxlife")] public virtual ushort Maxlife { get; set; }
        [Column("base")] public virtual uint Base { get; set; }
        [Column("sort")] public virtual ushort Sort { get; set; }
        [Column("itemid")] public virtual uint Itemid { get; set; }
        [Column("defence")] public virtual ushort Defence { get; set; }
        [Column("magic_def")] public virtual ushort MagicDef { get; set; }
    }
}