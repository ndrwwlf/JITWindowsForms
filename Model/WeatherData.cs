﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WeatherServiceForm.Model
{
    public class WeatherData
    {
        public int Id { get; set; }
        public string StationId { get; set; }
        public string ZipCode { get; set; }
        public DateTime RDate { get; set; }
        public int? HighTmp { get; set; }
        public int? LowTmp { get; set; }
        public double? AvgTmp { get; set; }
        public double? DewPt { get; set; }
    }
}
