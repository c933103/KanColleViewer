﻿using Grabacr07.KanColleWrapper.Models;
using Grabacr07.KanColleWrapper.Models.Raw;
using LynLogger.DataStore.LogBook;
using LynLogger.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LynLogger.Observers
{
    class ApiCreateShipObserver : IObserver<SvData<kcsapi_kdock[]>>, IObserver<SvData<kcsapi_createship>>
    {
        private Action<CreateShipLog> _onShipCreate;
        public event Action<CreateShipLog> OnShipCreate
        {
            add { _onShipCreate += value.MakeWeak(x => _onShipCreate -= x); }
            remove { }
        }

        private int waitDockId = -1;
        private int fuel;
        private int ammo;
        private int steel;
        private int bauxite;
        private int mat;
        
        public void OnNext(SvData<kcsapi_createship> value)
        {
            var data = value.Request;
            waitDockId = int.Parse(data["api_kdock_id"]);
            fuel = int.Parse(data["api_item1"]);
            ammo = int.Parse(data["api_item2"]);
            steel = int.Parse(data["api_item3"]);
            bauxite = int.Parse(data["api_item4"]);
            mat = int.Parse(data["api_item5"]);
        }

        public void OnNext(SvData<kcsapi_kdock[]> value)
        {
            if(waitDockId < 0) return;
            if (_onShipCreate != null) {
                _onShipCreate(new CreateShipLog(fuel, ammo, steel, bauxite, mat, waitDockId, value.Data[waitDockId].api_created_ship_id));
            }
            waitDockId = -1;
        }

        public void OnCompleted() { return; }
        public void OnError(Exception error) { return; }
    }
}
