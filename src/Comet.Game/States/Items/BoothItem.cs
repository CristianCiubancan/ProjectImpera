// //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) FTW! Masters
// Keep the headers and the patterns adopted by the project. If you changed anything in the file just insert
// your name below, but don't remove the names of who worked here before.
// 
// This project is a fork from Comet, a Conquer Online Server Emulator created by Spirited, which can be
// found here: https://gitlab.com/spirited/comet
// 
// Comet - Comet.Game - Booth Item.cs
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

namespace Comet.Game.States.Items
{
    public class BoothItem
    {
        public Item Item { get; private set; }
        public uint Identity => Item?.Identity ?? 0;
        public uint Value { get; private set; }
        public bool IsSilver { get; private set; }

        public bool Create(Item item, uint dwMoney, bool bSilver)
        {
            Item = item;
            Value = dwMoney;
            IsSilver = bSilver;

            return Value > 0;
        }
    }
}