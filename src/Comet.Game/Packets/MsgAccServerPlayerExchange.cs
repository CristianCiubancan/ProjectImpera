using Comet.Game.Internal;
using Comet.Game.States;
using Comet.Network.Packets.Internal;
using Comet.Shared;

namespace Comet.Game.Packets
{
    public sealed class MsgAccServerPlayerExchange : MsgAccServerPlayerExchange<AccountServer>
    {

        public static PlayerData CreatePlayerData(Character player)
        {
            return new PlayerData
            {
                Identity = player.Identity,
                AccountIdentity = player.Client.Identity,
                Name = player.Name,

                Level = player.Level,
                Metempsychosis = player.Metempsychosis,
                Profession = player.Profession,
                PreviousProfession = player.PreviousProfession,
                FirstProfession = player.FirstProfession,

                Money = player.Silvers,
                ConquerPoints = player.ConquerPoints,
                ConquerPointsMono = 0,

                Donation = player.NobilityDonation,

                SyndicateIdentity = player.SyndicateIdentity,
                SyndicatePosition = (ushort)player.SyndicateRank,

                FamilyIdentity = player.FamilyIdentity,
                FamilyPosition = (ushort)player.FamilyPosition,

                Force = player.Strength,
                Speed = player.Agility,
                Health = player.Vitality,
                Soul = player.Spirit,
                AdditionPoints = player.AttributePoints,

                LastLogin = UnixTimestamp.Timestamp(player.LastLogin),
                LastLogout = UnixTimestamp.Timestamp(player.LastLogout),
                TotalOnlineTime = player.TotalOnlineTime,

                AthletePoints = (int)player.QualifierPoints,
                AthleteHistoryWins = (int)player.QualifierHistoryWins,
                AthleteHistoryLoses = (int)player.QualifierHistoryLoses,
                HonorPoints = (int)player.HonorPoints,

                RedRoses = player.FlowerRed,
                WhiteRoses = player.FlowerWhite,
                Orchids = player.FlowerOrchid,
                Tulips = player.FlowerTulip
            };
        }
    }
}
