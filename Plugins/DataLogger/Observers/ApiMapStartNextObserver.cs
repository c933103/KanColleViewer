﻿using Codeplex.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LynLogger.Observers
{
    class ApiMapStartNextObserver : IObserver<Fiddler.Session>
    {
        private static readonly string[] Names = new string[] {
            "油", "弹", "钢", "铝", "喷火器", "桶", "开发资材", "改修资财"
        };

        public event Action<Models.Battling.MapNext> OnMapNext;

        public void OnCompleted()
        {
            return;
        }

        public void OnError(Exception error)
        {
            return;
        }

        public void OnNext(Fiddler.Session value)
        {
            if(OnMapNext == null) return;

            try {
                var response = value.ResponseBody;
                string json = Encoding.ASCII.GetString(response, 7, response.Length-7);
                dynamic res = DynamicJson.Parse(json);
                if(!res.api_result() || res.api_result != 1) return;

                var data = res.api_data;
                Models.Battling.MapNext mapNext = new Models.Battling.MapNext() {
                    ZwMapAreaId = (int)data.api_maparea_id,
                    ZwMapSectionId = (int)data.api_mapinfo_no,
                    ZwMapLocId = (int)data.api_no,
                    ZwMapBossLocId = (int)data.api_bosscell_no,
                    ZwEvent = (Models.Battling.MapNext.EventType)((int)data.api_event_id * ((int)data.api_event_kind == 2 ? -1 : 1)),
                    ZwRawData = json
                };
                if(mapNext.ZwEvent == Models.Battling.MapNext.EventType.ItemGet) {
                    mapNext.ZwItemGetLost = new Models.Battling.MapNext.ItemGetLostInfo() {
                        ZwAmount = (int)data.api_itemget.api_getcount,
                        ZwItemId = (int)data.api_itemget.api_id
                    };
                } else if(mapNext.ZwEvent == Models.Battling.MapNext.EventType.ItemLost) {
                    mapNext.ZwItemGetLost = new Models.Battling.MapNext.ItemGetLostInfo() {
                        ZwAmount = (int)data.api_happening.api_count,
                        ZwItemId = (int)data.api_happening.api_mst_id
                    };
                }
                if(mapNext.ZwItemGetLost != null) {
                    if(mapNext.ZwItemGetLost.ItemId <= Names.Length) {
                        mapNext.ZwItemGetLost.ZwItemName = Names[mapNext.ZwItemGetLost.ItemId - 1];
                    } else {
                        var item = Grabacr07.KanColleWrapper.KanColleClient.Current.Master.UseItems[mapNext.ZwItemGetLost.ItemId];
                        if(item != null) {
                            mapNext.ZwItemGetLost.ZwItemName = item.Name;
                        } else {
                            mapNext.ZwItemGetLost.ZwItemName = mapNext.ZwItemGetLost.ItemId.ToString();
                        }
                    }
                }

                OnMapNext(mapNext);
            } catch(Exception e) {
                System.Diagnostics.Debugger.Break();
                System.Diagnostics.Trace.TraceError(e.ToString());
            }
        }
    }
}
