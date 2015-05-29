﻿using Codeplex.Data;
using LynLogger.Models.Battling;
using LynLogger.Utilities;
using System;
using System.Text;

namespace LynLogger.Observers
{
    class ApiMapStartNextObserver : IObserver<Fiddler.Session>
    {
        private static readonly string[] Names = new string[] {
            "油", "弹", "钢", "铝", "喷火器", "桶", "开发资材", "改修资财"
        };

        private Action<MapNext> _onMapNext;
        public event Action<MapNext> OnMapNext
        {
            add { _onMapNext += value.MakeWeak(x => _onMapNext -= x); }
            remove { }
        }

        public void OnNext(Fiddler.Session value)
        {
            if(_onMapNext == null) return;

            try {
                var response = value.ResponseBody;
                string json = Encoding.ASCII.GetString(response, 7, response.Length-7);
                dynamic res = DynamicJson.Parse(json);
                if(!res.api_result() || res.api_result != 1) return;

                var data = res.api_data;
                MapNext mapNext = new MapNext() {
                    ZwMapAreaId = (int)data.api_maparea_id,
                    ZwMapSectionId = (int)data.api_mapinfo_no,
                    ZwMapLocId = (int)data.api_no,
                    //ZwMapBossLocId = (int)data.api_bosscell_no,
                    ZwEvent = (MapNext.EventType)((int)data.api_event_id * ((int)data.api_event_kind == 2 ? -1 : 1)),
                    ZwRawData = json
                };
                if(mapNext.ZwEvent == MapNext.EventType.ItemGet) {
                    mapNext.ZwItemGetLost = new MapNext.ItemGetLostInfo() {
                        ZwAmount = (int)data.api_itemget.api_getcount,
                        ZwItemId = (int)data.api_itemget.api_id
                    };
                } else if(mapNext.ZwEvent == MapNext.EventType.ItemLost) {
                    mapNext.ZwItemGetLost = new MapNext.ItemGetLostInfo() {
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

                _onMapNext(mapNext);
            } catch(Exception e) {
                System.Diagnostics.Debugger.Break();
                System.Diagnostics.Trace.TraceError(e.ToString());
            }
        }

        public void OnCompleted() { return; }
        public void OnError(Exception error) { return; }
    }
}