﻿using Grabacr07.KanColleWrapper.Models;
using Grabacr07.KanColleWrapper.Models.Raw;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LynLogger.Observers
{
    class ApiPortObserver : IObserver<SvData<kcsapi_port>>
    {
        public void OnCompleted()
        {
            return;
        }

        public void OnError(Exception error)
        {
            return;
        }

        public void OnNext(SvData<kcsapi_port> value)
        {
            try {
                if(!value.IsSuccess) return;
                Models.DataStore.SwitchMember(value.Data.api_basic.api_member_id);

                var ds = Models.DataStore.Instance.RwShips;

                //Update ship data with API raw data.
                var shipIds = ds.Keys.ToList();
                foreach(var ship in value.Data.api_ship) {
                    if(!ds.ContainsKey(ship.api_id)) {
                        ds[ship.api_id] = new Models.Ship(ship.api_id, ship.api_ship_id);
                    }
                    ds[ship.api_id].Update(ship);
                    shipIds.Remove(ship.api_id);
                }

                //Remove ships that no longer exists from ds.
                foreach(var id in shipIds) {
                    ds[id] = null;
                    Models.DataStore.Instance.RaiseShipDataChange(id);
                    ds.Remove(id);
                }

                //Update basic info.
                Models.DataStore.Instance.BasicInfo.Update(value.Data.api_material, true);
                Models.DataStore.Instance.BasicInfo.Update(value.Data.api_basic);
            } catch(Exception e) {
                System.Diagnostics.Debugger.Break();
                System.Diagnostics.Trace.TraceError(e.ToString());
            }
        }
    }
}