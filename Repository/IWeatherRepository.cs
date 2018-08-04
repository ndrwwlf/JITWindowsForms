using System;
using System.Collections.Generic;
using WeatherForm.Dto;
using WeatherForm.Model;
using WeatherServiceForm.Model;

namespace WeatherForm.Repository
{
    public interface IWeatherRepository
    {
        List<string> GetDistinctZipCodes();
        List<WeatherKey> GetAllWeatherKeys();
        bool InsertWeatherData(WeatherData weatherData);
        bool GetWeatherDataExistForZipAndDate(string ZipCode, DateTime rDate);
        int GetWeatherDataRowCount();
        int GetWeatherDataRowCountByZip(string ZipCode);
        string GetMostRecentWeatherDataDate();
        List<ReadingsQueryResult> GetReadings(string DateStart);
        int GetExpectedWthExpUsageRowCount(string DateStart);
        int GetActualWthExpUsageRowCount();
        List<WeatherData> GetWeatherDataByZipStartAndEndDate(string ZipCode, DateTime DateStart, DateTime DateEnd);
        bool InsertWthExpUsage(int readingId, decimal value);
    }
}