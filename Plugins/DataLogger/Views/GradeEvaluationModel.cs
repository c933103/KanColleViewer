﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LynLogger.Models;

namespace LynLogger.Views
{
    class GradeEvaluationModel : NotificationSourceObject
    {
        public double Score { get { return DataStore.Instance.BasicInfo.Score; } }

        public string Grade
        {
            get
            {
                var s = Score;
                if(s < 5) return "很休闲";
                if(s < 12) return "正常";
                if(s < 15) return "甘地";
                return "超神";
            }
        }

        public GradeEvaluationModel()
        {
            DataStore.OnDataStoreCreate += (_, ds) => {
                ds.BasicInfoChanged += () => {
                    RaisePropertyChanged(o => Grade, o => Score);
                };
            };
        }
    }
}
