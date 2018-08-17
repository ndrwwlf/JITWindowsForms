﻿using System;
using System.Collections.Generic;
using WeatherForm.Dto;
using WeatherForm.Model;
using WeatherServiceApp.Dao;
using WeatherServiceForm.Model;

namespace WeatherForm.Repository
{
    public interface IWeatherRepository
    {
        List<string> GetDistinctZipCodes();
        bool InsertWeatherData(WeatherData weatherData);
        bool GetWeatherDataExistForZipAndDate(string ZipCode, DateTime rDate);
        int GetWeatherDataRowCount();
        int GetWeatherDataRowCountByZip(string ZipCode);
        DateTime GetMostRecentWeatherDataDate();
        DateTime GetCurrentOldestWeatherDataDate();
        List<ReadingsQueryResult> GetReadings(string DateStart);
        List<ReadingsQueryResult> GetReadingsForExpUsageUpdate(string DateStart, WthNormalParams normalParams);
        int GetExpectedWthExpUsageRowCount(string DateStart);
        int GetActualWthExpUsageRowCount();
        List<WeatherData> GetWeatherDataByZipStartAndEndDate(string ZipCode, DateTime DateStart, DateTime DateEnd);
        bool InsertWthExpUsage(int readingId, decimal value);

        List<WNRdngData> GetAllReadingsFromStoredProcedure();
        bool GetWthNormalParamsExists(WthNormalParams normalParams);
        bool InsertWthNormalParams(WthNormalParams normalParams);
        bool UpdateWthNormalParams(WthNormalParams normalParams);
        bool GetWthExpUsageExists(int RdngID);
        bool UpdateWthExpUsage(int RdngID, decimal value);
    }
}