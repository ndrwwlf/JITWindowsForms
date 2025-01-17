﻿using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WeatherServiceForm.Dao;
using WeatherServiceForm.Model;
using WeatherServiceForm.Repository;
using WeatherServiceForm.Model;

namespace WeatherServiceForm
{
    class Controller
    {
        //private AerisJobParams _aerisJobParams;
        //private IWeatherRepository _weatherRepository;

        //private int expectedWthExpUsageInserts;
        //private int actualWthExpUsageInserts;

        //private int expectedDailyWeatherDataInserts;
        //private int actualDailyWeatherDataInserts;

        //private int expectedTotalWeatherDataEntries;
        //private int actualTotalWeatherDataEntries;

        //private int expectedHistoricalWeatherDataInserts;
        //private int actualHistoricalWeatherDataInserts;

        //readonly DateTime fromDateStart = new DateTime(2014, 11, 13);

        //public Task Execute(IJobExecutionContext context)
        //{
        //    _aerisJobParams = AerisJobParamsValueOf(context);
        //    _weatherRepository = _weatherRepositoryValueOf(_aerisJobParams);

        //    Log.Information("\nWeather job starting...\n");

        //    GatherWeatherData();

        //    PopulateWthExpUsageTable();

        //    Log.Information($"WeatherData gathered and WthExpUsage calculated for Readings going back to {fromDateStart.ToShortDateString()}");
        //    Log.Information("\nWeather job finished.\n");

        //    return Task.FromResult(0);
        //}

        //private void GatherWeatherData()
        //{
        //    Log.Information("Starting GatherWeatherData()...");

        //    try
        //    {
        //        GatherHistoricalWeatherData();
        //        GatherDailyWeatherData(-1);

        //        actualTotalWeatherDataEntries = _weatherRepository.GetWeatherDataRowCount();
        //    }
        //    catch (Exception ex)
        //    {
        //        Log.Error(ex.Message);
        //        Log.Error(ex.StackTrace);
        //    }

        //    Log.Information("Finished GatherWeatherData()");
        //    Log.Information($"Expected Total WeatherData Entries: {expectedTotalWeatherDataEntries}, Actual: {actualTotalWeatherDataEntries}.\n");

        //}

        //private void GatherDailyWeatherData(int i)
        //{
        //    DateTime targetDate = DateTime.Now.AddDays(i);
        //    List<string> zipCodes = _weatherRepository.GetDistinctZipCodes();

        //    Log.Information($"Starting GatherDailyWeatherData(int {i}) for targetDate: {targetDate.ToShortDateString()} and {zipCodes.Count} zip codes...");

        //    foreach (string zipCode in zipCodes)
        //    {
        //        if (!_weatherRepository.GetWeatherDataExistForZipAndDate(zipCode, targetDate))
        //        {
        //            expectedDailyWeatherDataInserts++;

        //            try
        //            {
        //                WeatherData weatherData = BuildWeatherData(zipCode, targetDate);

        //                bool success = _weatherRepository.InsertWeatherData(weatherData);

        //                if (success)
        //                {
        //                    Log.Information($"Inserted into WeatherData >> StationId: {weatherData.StationId}, Zip Code: {weatherData.ZipCode}, " +
        //                        $"RDate: {weatherData.RDate.ToShortDateString()}, LowTmp: {weatherData.LowTmp}, HighTmp: {weatherData.HighTmp}, " +
        //                        $"AvgTmp: {weatherData.AvgTmp}, DewPt: {weatherData.DewPt}");

        //                    actualDailyWeatherDataInserts++;
        //                    actualHistoricalWeatherDataInserts++;
        //                }
        //                else
        //                {
        //                    Log.Error($"Failed attempt: insert into WeatherData >> StationId: {weatherData.StationId}, Zip Code: {weatherData.ZipCode}, " +
        //                        $"RDate: {weatherData.RDate.ToShortDateString()}, LowTmp: {weatherData.LowTmp}, HighTmp: {weatherData.HighTmp}, " +
        //                        $"AvgTmp: {weatherData.AvgTmp}, DewPt: {weatherData.DewPt}");
        //                }
        //            }
        //            catch (Exception e)
        //            {
        //                Log.Error(e.Message);
        //                Log.Error(e.StackTrace);
        //            }
        //        }
        //    };

        //    Log.Information($"Finished GatherDailyWeatherData(int {i}) for {targetDate}.");
        //    Log.Information($"Expected inserts: {expectedDailyWeatherDataInserts}, Actual inserts: {actualDailyWeatherDataInserts}.\n");

        //    expectedDailyWeatherDataInserts = 0;
        //    actualDailyWeatherDataInserts = 0;
        //}

        //private void GatherHistoricalWeatherData()
        //{
        //    DateTime today = DateTime.Now;

        //    // yyyy, mm, dd
        //    //DateTime fromDate = new DateTime(2015, 01, 01);

        //    int days = (int)fromDateStart.Subtract(today).TotalDays;

        //    int zipCount = _weatherRepository.GetDistinctZipCodes().Count;

        //    expectedTotalWeatherDataEntries = ((days * -1) - 1) * zipCount;
        //    actualTotalWeatherDataEntries = _weatherRepository.GetWeatherDataRowCount();

        //    if (expectedTotalWeatherDataEntries > actualTotalWeatherDataEntries)
        //    {
        //        expectedHistoricalWeatherDataInserts = expectedTotalWeatherDataEntries - actualTotalWeatherDataEntries;

        //        Log.Information($"Starting GatherHistoricalWeatherData(), from {fromDateStart} to yesterday. {days} days.");

        //        for (int i = days; i <= -1; i++)
        //        {
        //            GatherDailyWeatherData(i);
        //        };

        //        Log.Information($"Finished GatherHistoricalWeatherData().");
        //        Log.Information($"Expected inserts: {expectedHistoricalWeatherDataInserts}, Actual inserts: {actualHistoricalWeatherDataInserts}.\n");

        //        expectedHistoricalWeatherDataInserts = 0;
        //        actualHistoricalWeatherDataInserts = 0;
        //    }

        //    expectedTotalWeatherDataEntries += zipCount;
        //}

        //private void PopulateWthExpUsageTable()
        //{
        //    Log.Information("Starting PopulateWthExpUsage()...");

        //    string fromDateStartStr = $"{fromDateStart.Month}-{fromDateStart.Day}-{fromDateStart.Year}";

        //    try
        //    {
        //        List<ReadingsQueryResult> readings = _weatherRepository.GetReadings(fromDateStartStr);

        //        expectedWthExpUsageInserts = readings.Count;

        //        foreach (ReadingsQueryResult result in readings)
        //        {
        //            //Console.WriteLine(result.DateStart + ", " + result.DateEnd + ", " + result.Days + ",  result.B1 = " + result.B1 + ",  result.B2 = " + result.B2 + ", result.B3 = " + result.B3 + ",  result.B4 = " + result.B4);
        //            try
        //            {
        //                List<WeatherData> weatherDataList = _weatherRepository.GetWeatherDataByZipStartAndEndDate(result.Zip, result.DateStart, result.DateEnd);
        //                HeatingCoolingDegreeDays heatingCoolingDegreeDays = HeatingCoolingDegreeDaysValueOf(result, weatherDataList);

        //                DoCalculation(result, heatingCoolingDegreeDays);
        //            }
        //            catch (Exception e)
        //            {
        //                Log.Error(e.Message);
        //                Log.Error(e.StackTrace);
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Log.Error(ex.Message);
        //        Log.Error(ex.StackTrace);
        //    }

        //    int expectedTotalWthExpUsageEntries = _weatherRepository.GetExpectedWthExpUsageRowCount(fromDateStartStr);
        //    int actualTotalWthExpUsageEntries = _weatherRepository.GetActualWthExpUsageRowCount();

        //    Log.Information($"Finished PopulateWthExpUsage(). Expected inserts: {expectedWthExpUsageInserts}, Actual: {actualWthExpUsageInserts}");
        //    Log.Information($"Expected WthExpUsage total entries: {expectedTotalWthExpUsageEntries}, Actual: {actualTotalWthExpUsageEntries}.\n");

        //    expectedWthExpUsageInserts = 0;
        //    actualWthExpUsageInserts = 0;
        //}

        //private void DoCalculation(ReadingsQueryResult result, HeatingCoolingDegreeDays heatingCoolingDegreeDays)
        //{
        //    // Normalized Energy Usage = E = B1(DAYS) + B2(HDDB3) + B4(CDDB5)
        //    double? resultAsDouble = (result.B1 * result.Days) + (result.B2 * heatingCoolingDegreeDays.HDD) + (result.B4 * heatingCoolingDegreeDays.CDD);
        //    decimal resultAsDecimal = decimal.Round(Convert.ToDecimal(resultAsDouble), 4, MidpointRounding.AwayFromZero);

        //    bool success = _weatherRepository.InsertWthExpUsage(result.RdngID, resultAsDecimal);

        //    if (success)
        //    {
        //        Log.Information($"Inserted into WthExpUsage >> RdngID: {result.RdngID} WthExpUsage: {resultAsDecimal} ... B1: {result.B1} B2: {result.B2} " +
        //            $"B3: {result.B3} Hdd: {heatingCoolingDegreeDays.HDD} B4: {result.B4} B5: {result.B5} Cdd: {heatingCoolingDegreeDays.CDD} " +
        //            $"RdngUnitID: {result.RUnitID} WthNormalParamsUnitID: {result.WnpUnitID}");

        //        actualWthExpUsageInserts++;
        //    }
        //    else
        //    {
        //        Log.Error($"FAILED attempt: insert into WthExpUsage >> RdngID: {result.RdngID} WthExpUsage: {resultAsDecimal} ... B1: {result.B1} B2: " +
        //            $"{result.B2} B3: {result.B3} Hdd: {heatingCoolingDegreeDays.HDD} B4: {result.B4} B5: {result.B5} Cdd: {heatingCoolingDegreeDays.CDD} " +
        //            $"RdngUnitID: {result.RUnitID} WthNormalParamsUnitID: {result.WnpUnitID}");
        //    }
        //}

        //private HeatingCoolingDegreeDays HeatingCoolingDegreeDaysValueOf(ReadingsQueryResult result, List<WeatherData> weatherDataList)
        //{
        //    HeatingCoolingDegreeDays hcdd = new HeatingCoolingDegreeDays();
        //    hcdd.CDD = 0.0;
        //    hcdd.HDD = 0.0;

        //    if (result.B3 == 0 && result.B5 == 0)
        //    {
        //        return hcdd;
        //    }

        //    foreach (WeatherData weatherData in weatherDataList)
        //    {

        //        if (result.B5 > 0)
        //        {
        //            if (weatherData.AvgTmp >= result.B5)
        //            {
        //                hcdd.CDD = hcdd.CDD + (weatherData.AvgTmp - result.B5);
        //            }

        //        }
        //        else if (result.B3 > 0)
        //        {
        //            if (weatherData.AvgTmp <= result.B3)
        //            {
        //                hcdd.HDD = hcdd.HDD + (result.B3 - weatherData.AvgTmp);
        //            }
        //        }
        //    }

        //    return hcdd;
        //}

        //private IWeatherRepository _weatherRepositoryValueOf(AerisJobParams aerisJobParams)
        //{
        //    return new WeatherRepository(aerisJobParams);
        //}

        //private AerisJobParams AerisJobParamsValueOf(IJobExecutionContext context)
        //{
        //    var schedulerContext = context.Scheduler.Context;
        //    return (AerisJobParams)schedulerContext.Get("aerisJobParams");
        //}

        //private WeatherData BuildWeatherData(string zipCode, DateTime targetDate)
        //{
        //    AerisResult result = GetAerisResult(zipCode, targetDate);

        //    Response response = result.Response.First();
        //    Summary summary = response.Periods.First().Summary;

        //    Temp temp = summary.Temp;
        //    Dewpt dewpt = summary.Dewpt;

        //    WeatherData weatherData = new WeatherData
        //    {
        //        StationId = response.Id,
        //        ZipCode = zipCode,
        //        RDate = targetDate,
        //        HighTmp = temp.MaxF,
        //        LowTmp = temp.MinF,
        //        AvgTmp = temp.AvgF,
        //        DewPt = dewpt.AvgF
        //    };

        //    return weatherData;
        //}

        //private AerisResult GetAerisResult(string zipCode, DateTime targetDate)
        //{
        //    string fromDate = targetDate.Date.ToString("MM/dd/yyyy");
        //    Log.Information($"Calling Aeris for zip: {zipCode} and date: {fromDate}");

        //    /* 
        //     * example
        //    http://api.aerisapi.com/observations/summary/closest?p=94304&query=maxt:!NULL,maxdewpt:!NULL&from=12/03/2014&to=12/03/2014&fields=id,periods.summary.dateTimeISO,periods.summary.temp.maxF,periods.summary.temp.minF,periods.summary.temp.avgF,periods.summary.dewpt.avgF
        //    */

        //    string rootUrl = $"http://api.aerisapi.com/observations/summary/closest?p={zipCode}&query=maxt:!NULL,maxdewpt:!NULL" +
        //        $"&from={fromDate}&to={fromDate}&fields=id,periods.summary.dateTimeISO,periods.summary.temp.maxF,periods.summary.temp.minF," +
        //        "periods.summary.temp.avgF,periods.summary.dewpt.avgF";

        //    StringBuilder builder = new StringBuilder();
        //    builder.Append(rootUrl);
        //    builder.Append("&client_id=");
        //    builder.Append(_aerisJobParams.AerisClientId);
        //    builder.Append("&client_secret=");
        //    builder.Append(_aerisJobParams.AerisClientSecret);

        //    string url = builder.ToString();
        //    Console.WriteLine("url: {0}", url);

        //    using (WebClient wc = new WebClient())
        //    {
        //        var json = wc.DownloadString(url);
        //        //Log.Information($"JSON: {json}");                                                                  
        //        return JsonConvert.DeserializeObject<AerisResult>(json, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Include });
        //    }
        //}
    //}
}
}
