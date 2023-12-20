namespace Comet.Network.Packets
{
    /// <summary>
    /// Packet types for the Conquer Online game client across all server projects. 
    /// Identifies packets by an unsigned short from offset 2 of every packet sent to
    /// the server.
    /// </summary>
    public enum PacketType : ushort
    {
        MsgRegister = 1001,
        MsgTalk = 1004,
        MsgUserInfo = 1006,
        MsgItemInfo = 1008,
        MsgItem,
        MsgTick = 1012,
        MsgName = 1015,
        MsgWeather,
        MsgFriend = 1019,
        MsgInteract = 1022,
        MsgTeam,
        MsgAllot,
        MsgWeaponSkill,
        MsgTeamMember,
        MsgGemEmbed,
        MsgFuse,
        MsgTeamAward,
        MsgGodExp = 1036,
        MsgPing,
        MsgEnemyList = 1041,
        MsgMonsterTransform,
        MsgTeamRoll,
        MsgLoadMap,
        MsgConnect = 1052,
        MsgConnectEx = 1055,
        MsgTrade,
        MsgSynpOffer = 1058,
        MsgEncryptCode,
        MsgAccount = 1086,
        MsgPCNum = 1100,
        MsgMapItem,
        MsgPackage,
        MsgMagicInfo,
        MsgFlushExp,
        MsgMagicEffect,
        MsgSyndicateAttributeInfo,
        MsgSyndicate,
        MsgItemInfoEx,
        MsgNpcInfoEx,
        MsgMapInfo,
        MsgMessageBoard,
        MsgDice = 1113,
        MsgSyncAction,
        MsgTitle = 1130,
        MsgTaskStatus = 1134,
        MsgTaskDetailInfo,
        MsgFamily = 1312,
        MsgFamilyOccupy,
        MsgNpcInfo = 2030,
        MsgNpc,
        MsgTaskDialog,
        MsgDataArray = 2036,
        MsgTradeBuddy = 2046,
        MsgTradeBuddyInfo,
        MsgEquipLock,
        MsgPigeon = 2050,
        MsgPigeonQuery,
        MsgPeerage = 2064,
        MsgGuide,
        MsgGuideInfo,
        MsgContribute,
        MsgQuiz,
        MsgTotemPoleInfo = 2201,
        MsgWeaponsInfo,
        MsgTotemPole,
        MsgQualifyingInteractive = 2205,
        MsgQualifyingFightersList,
        MsgQualifyingRank,
        MsgQualifyingSeasonRankList,
        MsgQualifyingDetailInfo,
        MsgArenicScore,
        MsgArenicWitness,
        MsgWalk = 10005,
        MsgAction = 10010,
        MsgPlayer = 10014,
        MsgUserAttrib = 10017
    }
}