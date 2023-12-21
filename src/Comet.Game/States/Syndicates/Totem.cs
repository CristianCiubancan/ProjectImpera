// //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) FTW! Masters
// Keep the headers and the patterns adopted by the project. If you changed anything in the file just insert
// your name below, but don't remove the names of who worked here before.
// 
// This project is a fork from Comet, a Conquer Online Server Emulator created by Spirited, which can be
// found here: https://gitlab.com/spirited/comet
// 
// Comet - Comet.Game - Totem.cs
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

using Comet.Game.Database.Models;
using Comet.Game.States.Items;

#endregion

namespace Comet.Game.States.Syndicates
{
    public sealed class Totem
    {
        public Totem(DbItem item, string playerName)
        {
            ItemIdentity = item.Id;
            PlayerIdentity = item.PlayerId;
            PlayerName = playerName;

            ItemType = item.Type;
            SocketOne = (Item.SocketGem) item.Gem1;
            SocketTwo = (Item.SocketGem) item.Gem2;
            Addition = item.Magic3;
        }

        public Totem(Character owner, Item item)
        {
            ItemIdentity = item.Identity;
            PlayerIdentity = owner.Identity;
            PlayerName = owner.Name;

            ItemType = item.Type;
            SocketOne = item.SocketOne;
            SocketTwo = item.SocketTwo;
            Addition = item.Plus;
        }

        public uint ItemIdentity { get; private set; }
        public uint PlayerIdentity { get; private set; }
        public string PlayerName { get; private set; }

        public uint ItemType { get; private set; }
        public byte Quality => (byte) (ItemType % 10);
        public Item.SocketGem SocketOne { get; private set; }
        public Item.SocketGem SocketTwo { get; private set; }
        public byte Addition { get; private set; }

        public void SynchronizeItem(Item item)
        {
            ItemType = item.Type;
            SocketOne = item.SocketOne;
            SocketTwo = item.SocketTwo;
            Addition = item.Plus;
        }

        public int Points
        {
            get
            {
                int result = 0;
                switch (Quality)
                {
                    case 8: result += 1000;
                        break;
                    case 9: result += 16660;
                        break;
                }

                if (SocketTwo > Item.SocketGem.NoSocket)
                    result += 133330;
                else if (SocketOne > Item.SocketGem.NoSocket)
                    result += 33330;

                switch (Addition)
                {
                    case 1:
                        result += 90;
                        break;
                    case 2:
                        result += 490;
                        break;
                    case 3:
                        result += 1350;
                        break;
                    case 4:
                        result += 4070;
                        break;
                    case 5:
                        result += 12340;
                        break;
                    case 6:
                        result += 37030;
                        break;
                    case 7:
                        result += 111110;
                        break;
                    case 8:
                        result += 333330;
                        break;
                    case 9:
                        result += 1000000;
                        break;
                    case 10:
                        result += 1033330;
                        break;
                    case 11:
                        result += 1101230;
                        break;
                    case 12:
                        result += 1212340;
                        break;
                }

                return result;
            }
        }
    }
}