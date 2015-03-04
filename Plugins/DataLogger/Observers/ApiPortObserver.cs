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
                var ds = Models.DataStore.Instance;

                ds.UpdateShips(value.Data.api_ship);
                ds.BasicInfo.Update(value.Data.api_material, true);
                ds.BasicInfo.Update(value.Data.api_basic);
            } catch(Exception e) {
                System.Diagnostics.Debugger.Break();
                System.Diagnostics.Trace.TraceError(e.ToString());
            }
        }
    }
}
