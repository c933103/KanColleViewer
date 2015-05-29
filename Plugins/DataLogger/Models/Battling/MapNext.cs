﻿using LynLogger.DataStore.Serialization;
using System.Text;

namespace LynLogger.Models.Battling
{
    public class MapNext : AbstractDSSerializable<MapNext>
    {
        [Serialize(0)] internal string ZwRawData;
        public string RawData { get { return ZwRawData; } }

        [Serialize(1)] internal int ZwMapAreaId;
        public int MapAreaId { get { return ZwMapAreaId; } }

        [Serialize(2)] internal int ZwMapSectionId;
        public int MapSectionId { get { return ZwMapSectionId; } }

        [Serialize(3)] internal int ZwMapLocId;
        public int MapLocId { get { return ZwMapLocId; } }

        [Serialize(4)] internal EventType ZwEvent;
        public EventType Event { get { return ZwEvent; } }

        [Serialize(5)] internal ItemGetLostInfo ZwItemGetLost;
        public ItemGetLostInfo ItemGetLost { get { return ZwItemGetLost; } }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(string.Format("{0}-{1}-{2}", MapAreaId, MapSectionId, MapLocId));
            switch(Event) {
                case EventType.Battle:
                    sb.Append("来！战个痛！快！");
                    break;
                case EventType.BossBattle:
                    sb.Append("BOSS战突入！");
                    break;
                case EventType.NightBattle:
                    sb.Append("夜战突入！");
                    break;
                case EventType.NightBossBattle:
                    sb.Append("夜战 BOSS！");
                    break;
                case EventType.ItemGet:
                    sb.AppendFormat("获得 {0} x{1}", ItemGetLost.ItemName, ItemGetLost.Amount);
                    break;
                case EventType.ItemLost:
                    sb.AppendFormat("损失 {0} x{1}", ItemGetLost.ItemName, ItemGetLost.Amount);
                    break;
                case EventType.Nothing:
                    sb.Append("来！战个……没事");
                    break;
                default:
                    sb.Append(RawData);
                    break;
            }
            return sb.ToString();
        }

        public class ItemGetLostInfo : AbstractDSSerializable<ItemGetLostInfo>
        {
            [Serialize(0)] internal int ZwItemId;
            public int ItemId { get { return ZwItemId; } }

            [Serialize(1)] public int ZwAmount;
            public int Amount { get { return ZwAmount; } }

            [Serialize(2)] internal string ZwItemName;
            public string ItemName { get { return ZwItemName; } }
        }
        public enum EventType { ItemGet = 2, ItemLost = 3, Battle = 4, BossBattle = 5, NightBattle = -4, NightBossBattle = -5, Nothing = 6 }
    }
}