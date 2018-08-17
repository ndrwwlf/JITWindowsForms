﻿using Accord.Math;
using Accord.Math.Optimization.Losses;
using Accord.Statistics;
using Accord.Statistics.Analysis;
using Accord.Statistics.Models.Regression.Linear;
using Accord.Statistics.Testing;
using MathNet.Numerics;
using Quartz;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using WeatherForm.Dto;
using WeatherForm.Model;
using WeatherForm.Repository;
using WeatherServiceApp.Dao;
using WeatherServiceApp.Model;
using WeatherServiceForm;
using WeatherServiceForm.Model;

namespace JitTopshelf.Scheduled
{
    public class WNRdngData01RegressionJob
    {
        private AerisJobParams _aerisJobParams;
        private IWeatherRepository _weatherRepository;

        private DateTime _fromDateStart = new DateTime(2015, 01, 15);

        public void Execute(AerisJobParams aerisJobParams)
        {
            _aerisJobParams = aerisJobParams;
            _weatherRepository = _weatherRepositoryValueOf(_aerisJobParams);
            _fromDateStart = _weatherRepository.GetCurrentOldestWeatherDataDate();

            Log.Information("Starting WNRdngData01RegressionJob (Manual Start)...");
            var regressionWatch = System.Diagnostics.Stopwatch.StartNew();

            PopulateWthNormalParams();

            regressionWatch.Stop();
            var t = regressionWatch.Elapsed;
            Log.Information("Finished WNRdngData01RegressionJob (Manual Start). Time elapsed: " + t.ToString() + "\n\n");
        }

        private void PopulateWthNormalParams()
        {
            Log.Information("Starting PopulateWthNormalParams()... ");

            List<WNRdngData> allWNRdngData = _weatherRepository.GetAllReadingsFromStoredProcedure();

            List<WthNormalParams> newNormalParamsList = new List<WthNormalParams>();

            var wNRdngDataGroups = allWNRdngData.GroupBy(s => new { s.AccID, s.UtilID, s.UnitID });

            int updateCount = 0;
            int insertCount = 0;
            int failCount = 0;

            foreach (var wNRdngGroup in wNRdngDataGroups)
            {
                List<WNRdngData> wNRdngList = wNRdngGroup.OrderBy(s => s.MoID).ToList();

                WNRdngData lastRead = wNRdngList.LastOrDefault();

                WthNormalParams normalParams = new WthNormalParams()
                {
                    AccID = lastRead.AccID,
                    UtilID = lastRead.UtilID,
                    UnitID = lastRead.UnitID,
                    WstID = lastRead.WstID,
                    ZipW = lastRead.Zip,
                    EndDate = lastRead.DateEnd,
                    EMoID = lastRead.EMoID,
                    MoCt = lastRead.MoCt
                };

                bool normalParamsExists = _weatherRepository.GetWthNormalParamsExists(normalParams);

                try
                {
                    List<BalancePointPair> allBalancePointStatsFromYear = CalculateOneYearOfDegreeDaysForAllBalancePoints(wNRdngList);

                    int daysInYear = allBalancePointStatsFromYear.FirstOrDefault().DaysInYear;
                    normalParams.DaysInYear = daysInYear;

                    if (allBalancePointStatsFromYear.Count == 0)
                    {
                        if (normalParamsExists)
                        {
                            bool success = UpdateOrInsertWthNormalParams(normalParams);
                            if (success)
                            {
                                newNormalParamsList.Add(normalParams);
                                updateCount++;
                            }
                            else
                            {
                                failCount++;
                            }
                        }
                        else
                        {
                            bool success = UpdateOrInsertWthNormalParams(normalParams);
                            if (success)
                            {
                                newNormalParamsList.Add(normalParams);
                                insertCount++;
                            }
                            else
                            {
                                failCount++;
                            }
                        }
                        continue;
                    }

                    AccordResult accord = CalculateLinearRegression(allBalancePointStatsFromYear, normalParams);

                    if (accord.FTestFailed)
                    {
                        Log.Warning($"Best Regression Model's F-Test failed... " +
                            $"AccID/UtilID/UnitID: {normalParams.AccID}/{normalParams.UtilID}/{normalParams.UnitID}");
                    }

                    normalParams.B1 = decimal.Round(Convert.ToDecimal(accord.Intercept), 9, MidpointRounding.AwayFromZero);

                    if (accord.IsSimpleSingleRegression == true && accord.HeatingBP > 0)
                    {
                        normalParams.B2 = decimal.Round(Convert.ToDecimal(accord.B2), 9, MidpointRounding.AwayFromZero);
                        normalParams.B3 = accord.HeatingBP;
                    }
                    else if (accord.IsSimpleSingleRegression == true && accord.CoolingBP > 0)
                    {
                        normalParams.B4 = decimal.Round(Convert.ToDecimal(accord.B4), 9, MidpointRounding.AwayFromZero);
                        normalParams.B5 = accord.CoolingBP;
                    }
                    else if (accord.IsMultipleLinearRegression == true)
                    {
                        normalParams.B2 = decimal.Round(Convert.ToDecimal(accord.B2), 9, MidpointRounding.AwayFromZero);
                        normalParams.B3 = accord.HeatingBP;
                        normalParams.B4 = decimal.Round(Convert.ToDecimal(accord.B4), 9, MidpointRounding.AwayFromZero);
                        normalParams.B5 = accord.CoolingBP;
                    }

                    if (!Double.IsNaN(accord.R2Accord) && !Double.IsInfinity(accord.R2Accord))
                    {
                        normalParams.R2 = decimal.Round(Convert.ToDecimal(accord.R2Accord), 9, MidpointRounding.AwayFromZero);
                    }

                    if (normalParamsExists)
                    {
                        bool success = _weatherRepository.UpdateWthNormalParams(normalParams);

                        if (success)
                        {
                            newNormalParamsList.Add(normalParams);
                            updateCount++;
                        }
                        else
                        {
                            failCount++;
                        }
                    }
                    else
                    {
                        bool success = _weatherRepository.InsertWthNormalParams(normalParams);

                        if (success)
                        {
                            newNormalParamsList.Add(normalParams);
                            insertCount++;
                        }
                        else
                        {
                            failCount++;
                        }
                    }
                }
                catch (BadWNRdngDataException bdex)
                {
                    failCount++;
                    Log.Warning(bdex.Message);
                }
                catch (Exception e)
                {
                    failCount++;
                    Log.Debug($"AccID/UtilID/UnitID: {normalParams.AccID}/{normalParams.UtilID}/{normalParams.UnitID} >> {e.Message} {e.StackTrace}");
                }
            }

            if (failCount == 0)
            {
                Log.Information($"Finished PopulateWthNormalParams(). Rows Updated: {updateCount}. Rows Inserted: {insertCount}. Failures: {failCount}");
            }
            else
            {
                Log.Warning($"Finished PopulateWthNormalParams(). Rows Updated: {updateCount}. Rows Inserted: {insertCount}. Failures: {failCount}");
            }

            UpdateWthExpUsage(newNormalParamsList);
        }

        private List<BalancePointPair> CalculateOneYearOfDegreeDaysForAllBalancePoints(List<WNRdngData> wNRdngData)
        {
            List<BalancePointPair> allBalancePointPairs = new List<BalancePointPair>();

            DateTime _yearOfReadsDateStart = wNRdngData.First().DateStart;
            DateTime _yearOfReadsDateEnd = wNRdngData.Last().DateEnd;
            int _readingsCount = wNRdngData.First().MoID;
            int daysInYear = 0;

            bool badData = false;

            foreach (WNRdngData reading in wNRdngData)
            {
                if (reading.DateStart == DateTime.MinValue || reading.DateEnd == DateTime.MinValue)
                {
                    badData = true;
                    Log.Debug($"MoID: {reading.MoID} >> DateStart and/or DateEnd is null for " +
                        $"AccID/UtilID/UnitID: {reading.AccID}/{reading.UtilID}/{reading.UnitID}");
                }
                else
                {
                    var t = reading.DateEnd.Subtract(reading.DateStart).Days;
                    daysInYear += t;
                }
            }

            if (badData)
            {
                throw new BadWNRdngDataException($"Bad/null WNRdngData for AccID/UtilID/UnitID: " +
                    $"{wNRdngData.First().AccID}/{wNRdngData.First().UtilID}/{wNRdngData.First().UnitID}.");
            }

            if (badData)
            {
                throw new Exception($"Bad/null WNRdngData for AccID/UtilID/UnitID: {wNRdngData.First().AccID}/{wNRdngData.First().UtilID}/{wNRdngData.First().UnitID}.");
            }

            foreach (WNRdngData reading in wNRdngData)
            {
                int daysInReading = reading.DateEnd.Subtract(reading.DateStart).Days;

                HeatingCoolingDegreeDays hcdd = new HeatingCoolingDegreeDays
                {
                    CDD = 0.0,
                    HDD = 0.0
                };

                List<WeatherData> weatherDataList = _weatherRepository.GetWeatherDataByZipStartAndEndDate(reading.Zip, reading.DateStart, reading.DateEnd);

                if (weatherDataList.Count != daysInReading)
                {
                    Log.Warning($"WeatherData.Count != daysInReading: {weatherDataList.Count} != {daysInReading} where AccID/UtilID/UnitID: " +
                        $"{reading.AccID}/{reading.UtilID}/{reading.UnitID} and Zip: {reading.Zip}, MoID: {reading.MoID}");
                }

                int rangeMin = 45;
                int rangeMax = 75;
                int range = rangeMax - rangeMin + 1;

                List<int[]> comboList = new List<int[]>();

                for (int i = 0; i < range; i++)
                {
                    int[] hdsOnly = new int[2] { rangeMin + i, 0 };
                    int[] cdsOnly = new int[2] { 0, rangeMin + i };

                    comboList.Add(hdsOnly);
                    comboList.Add(cdsOnly);

                    int k = range - 1 - i;
                    while (k >= 0)
                    {
                        int[] both = new int[2] { rangeMin + i, rangeMin + i + k };
                        k--;

                        comboList.Add(both);
                    }
                }

                comboList.Add(new int[] { 0, 0 });

                foreach (int[] combo in comboList)
                {

                    BalancePointPair bpPair = new BalancePointPair
                    {
                        CoolingBalancePoint = combo[1],
                        HeatingBalancePoint = combo[0]
                    };

                    hcdd = HeatingCoolingDegreeDaysValueOf(bpPair, weatherDataList);

                    bpPair.CoolingDegreeDays = hcdd.CDD;
                    bpPair.HeatingDegreeDays = hcdd.HDD;
                    bpPair.ActualUsage = reading.Units;
                    bpPair.ZipCode = reading.Zip;
                    bpPair.DaysInReading = daysInReading;
                    bpPair.DaysInYear = daysInYear;

                    allBalancePointPairs.Add(bpPair);
                }
            }

            return allBalancePointPairs;
        }

        private HeatingCoolingDegreeDays HeatingCoolingDegreeDaysValueOf(BalancePointPair balancePointPair, List<WeatherData> weatherDataList)
        {
            HeatingCoolingDegreeDays hcdd = new HeatingCoolingDegreeDays
            {
                CDD = 0.0,
                HDD = 0.0,
            };

            foreach (WeatherData weatherData in weatherDataList)
            {
                if (!weatherData.AvgTmp.HasValue)
                {
                    throw new Exception("WeatherData.AvgTmp is null for " + weatherData.ZipCode + " on " + weatherData.RDate);
                }
                else if (balancePointPair.CoolingBalancePoint > 0 && weatherData.AvgTmp >= balancePointPair.CoolingBalancePoint)
                {
                    hcdd.CDD += (weatherData.AvgTmp.Value - balancePointPair.CoolingBalancePoint);
                }
                else if (balancePointPair.HeatingBalancePoint > 0 && weatherData.AvgTmp < balancePointPair.HeatingBalancePoint)
                {
                    hcdd.HDD += (balancePointPair.HeatingBalancePoint - weatherData.AvgTmp.Value);
                }
            }

            return hcdd;
        }

        private AccordResult CalculateLinearRegression(List<BalancePointPair> allBalancePointPairs, WthNormalParams normalParamsKey)
        {
            var allBalancePointGroups = allBalancePointPairs.GroupBy(s => new { s.CoolingBalancePoint, s.HeatingBalancePoint });

            List<AccordResult> accordResults = new List<AccordResult>();

            foreach (var group in allBalancePointGroups)
            {
                try
                {
                    List<BalancePointPair> IdenticalBalancePointPairsFromAllReadings = group.ToList();
                    BalancePointPair _pointPair = IdenticalBalancePointPairsFromAllReadings.First();
                    int readingsCount = IdenticalBalancePointPairsFromAllReadings.Count;

                    double[] fullYData = new double[readingsCount];
                    double[] fullYDataDailyAvg = new double[readingsCount];

                    double[][] hcddMatrix = new double[readingsCount][];

                    double[][] hcddMatrixNonDaily = new double[readingsCount][];

                    foreach (BalancePointPair balancePointPair in IdenticalBalancePointPairsFromAllReadings)
                    {
                        fullYData[IdenticalBalancePointPairsFromAllReadings.IndexOf(balancePointPair)] = (balancePointPair.ActualUsage);

                        fullYDataDailyAvg[IdenticalBalancePointPairsFromAllReadings.IndexOf(balancePointPair)]
                            = (balancePointPair.ActualUsage / balancePointPair.DaysInReading);

                        hcddMatrix[IdenticalBalancePointPairsFromAllReadings.IndexOf(balancePointPair)] = new double[]
                            {
                            (balancePointPair.HeatingDegreeDays / balancePointPair.DaysInReading),
                            (balancePointPair.CoolingDegreeDays / balancePointPair.DaysInReading)
                            };
                    }

                    double[] avgHddsForEachReadingInYear = new double[readingsCount];
                    double[] avgCddsForEachReadingInYear = new double[readingsCount];

                    for (int i = 0; i < readingsCount; i++)
                    {
                        avgHddsForEachReadingInYear[i] = hcddMatrix[i][0];
                        avgCddsForEachReadingInYear[i] = hcddMatrix[i][1];
                    }

                    double[] modelParams = new double[3];
                    modelParams[0] = 0;
                    modelParams[1] = 0;
                    modelParams[2] = 0;

                    if (_pointPair.HeatingBalancePoint == 0 && _pointPair.CoolingBalancePoint == 0)
                    {
                        double[] onesVector = new double[readingsCount];

                        for (int i = 0; i < readingsCount; i++)
                        {
                            onesVector[i] = 1;
                        }

                        modelParams[0] = Fit.LineThroughOrigin(onesVector, fullYDataDailyAvg);

                        OrdinaryLeastSquares ols = new OrdinaryLeastSquares()
                        {
                            UseIntercept = false
                        };

                        double r2 = MathNet.Numerics.GoodnessOfFit.CoefficientOfDetermination(
                            onesVector.Select(x => x * modelParams[0]), fullYDataDailyAvg);

                        AccordResult accordResult = new AccordResult()
                        {
                            IsSimpleSingleRegression = true,
                            HeatingBP = _pointPair.HeatingBalancePoint,
                            CoolingBP = _pointPair.CoolingBalancePoint,
                            Intercept = modelParams[0],
                            R2Accord = r2,
                        };

                        accordResults.Add(accordResult);
                    }
                    else if (_pointPair.CoolingBalancePoint != 0 && _pointPair.HeatingBalancePoint != 0)
                    {

                        try
                        {
                            MultipleLinearRegressionAnalysis mlra = new MultipleLinearRegressionAnalysis(intercept: true);
                            mlra.Learn(hcddMatrix, fullYDataDailyAvg);
                            var regressionAccord = mlra.Regression;

                            double[] predicted = regressionAccord.Transform(hcddMatrix);

                            double r2Accord = new RSquaredLoss(numberOfInputs: 2, expected: fullYDataDailyAvg) { Adjust = false }.Loss(predicted);

                            double r2Coeff = regressionAccord.CoefficientOfDetermination(hcddMatrix, fullYDataDailyAvg, adjust: false);

                            bool FTestFailed = !mlra.FTest.Significant;

                            AccordResult accordResult = new AccordResult()
                            {
                                IsMultipleLinearRegression = true,
                                HeatingBP = _pointPair.HeatingBalancePoint,
                                CoolingBP = _pointPair.CoolingBalancePoint,
                                Intercept = regressionAccord.Intercept,
                                B2 = regressionAccord.Weights[0],
                                B4 = regressionAccord.Weights[1],
                                R2Accord = r2Accord,
                                FTestFailed = FTestFailed

                            };

                            if (mlra.Coefficients.All(x => x.TTest.Significant))
                            {
                                accordResults.Add(accordResult);
                            }
                        }
                        catch (Exception e)
                        {
                            Log.Debug($"Multiple Linear Regression Analysis exceptions are common when certain heating/cooling BPs and/or insignificant" +
                                $"actual usage(Units from a Reading) provide insufficient data. " +
                                $"AccID/UtilID/UnitID: {normalParamsKey.AccID}/{normalParamsKey.UtilID}/{normalParamsKey.UnitID} >> {e.Message} {e.StackTrace}");
                        }
                    }
                    else if (_pointPair.HeatingBalancePoint > 0)
                    {
                        OrdinaryLeastSquares ols = new OrdinaryLeastSquares()
                        {
                            UseIntercept = true
                        };

                        SimpleLinearRegression regressionAccord = ols.Learn(avgHddsForEachReadingInYear, fullYDataDailyAvg);

                        double[] predictedAccord = regressionAccord.Transform(avgHddsForEachReadingInYear);

                        double r2Accord = new RSquaredLoss(1, fullYDataDailyAvg).Loss(predictedAccord);

                        int degreesOfFreedom = normalParamsKey.MoCt - 2;
                        double ssx = Math.Sqrt((avgHddsForEachReadingInYear.Subtract(avgHddsForEachReadingInYear.Mean())).Pow(2).Sum());
                        double s = Math.Sqrt(((fullYDataDailyAvg.Subtract(predictedAccord).Pow(2)).Sum()) / degreesOfFreedom);

                        double error = regressionAccord.GetStandardError(avgHddsForEachReadingInYear, fullYDataDailyAvg);

                        double seSubB = s / ssx;

                        double hypothesizedValue = 0;

                        TTest tTest = new TTest(
                            estimatedValue: regressionAccord.Slope, standardError: seSubB, degreesOfFreedom: degreesOfFreedom,
                            hypothesizedValue: hypothesizedValue, alternate: OneSampleHypothesis.ValueIsDifferentFromHypothesis
                            );

                        AccordResult accordResult = new AccordResult()
                        {
                            IsSimpleSingleRegression = true,
                            HeatingBP = _pointPair.HeatingBalancePoint,
                            Intercept = regressionAccord.Intercept,
                            B2 = regressionAccord.Slope,
                            R2Accord = r2Accord
                        };

                        if (tTest.Significant)
                        {
                            accordResults.Add(accordResult);
                        }
                    }
                    else if (_pointPair.CoolingBalancePoint > 0)
                    {
                        OrdinaryLeastSquares ols = new OrdinaryLeastSquares()
                        {
                            UseIntercept = true
                        };

                        SimpleLinearRegression regressionAccord = ols.Learn(avgCddsForEachReadingInYear, fullYDataDailyAvg);

                        double[] predictedAccord = regressionAccord.Transform(avgCddsForEachReadingInYear);
                        double rAccord = new RSquaredLoss(1, fullYDataDailyAvg).Loss(predictedAccord);

                        int degreesOfFreedom = normalParamsKey.MoCt - 2;
                        double ssx = Math.Sqrt(avgCddsForEachReadingInYear.Subtract(avgCddsForEachReadingInYear.Mean()).Pow(2).Sum());
                        double s = Math.Sqrt(((fullYDataDailyAvg.Subtract(predictedAccord).Pow(2)).Sum()) / degreesOfFreedom);

                        double seSubB = s / ssx;
                        double hypothesizedValue = 0;

                        double myT = seSubB / regressionAccord.Slope;

                        TTest tTest = new TTest(
                            estimatedValue: regressionAccord.Slope, standardError: seSubB, degreesOfFreedom: degreesOfFreedom,
                            hypothesizedValue: hypothesizedValue, alternate: OneSampleHypothesis.ValueIsDifferentFromHypothesis
                            );

                        AccordResult accordResult = new AccordResult()
                        {
                            IsSimpleSingleRegression = true,
                            CoolingBP = _pointPair.CoolingBalancePoint,
                            Intercept = regressionAccord.Intercept,
                            B4 = regressionAccord.Slope,
                            R2Accord = rAccord
                        };

                        if (tTest.Significant)
                        {
                            accordResults.Add(accordResult);
                        }
                    };
                }
                catch (Exception e)
                {
                    Log.Debug($"AccID/UtilID/UnitID: {normalParamsKey.AccID}/{normalParamsKey.UtilID}/{normalParamsKey.UnitID} >> {e.Message} {e.StackTrace}");
                }
            }

            AccordResult accordWinner = accordResults
                .Where(s => s.Intercept >= 0)
                .OrderByDescending(s => s.R2Accord).ToList().FirstOrDefault();

            return accordWinner;
        }

        private bool UpdateOrInsertWthNormalParams(WthNormalParams normalParams)
        {
            bool success;
            bool normalParamsExists = _weatherRepository.GetWthNormalParamsExists(normalParams);

            if (normalParamsExists)
            {
                success = _weatherRepository.UpdateWthNormalParams(normalParams);

                if (success)
                {
                    Log.Information($"Updated WthNormalParams where AccID/UtilID/UnitID: {normalParams.AccID}/{normalParams.UtilID}/{normalParams.UnitID} >> " +
                        $"B1: {normalParams.B1} B2: {normalParams.B2} B3: {normalParams.B3} B4: {normalParams.B4} B5: {normalParams.B5} R2: {normalParams.R2}.");
                }
                else
                {
                    Log.Error($"Failed to Update WthNormalParams where AccID/UtilID/UnitID: {normalParams.AccID}/{normalParams.UtilID}/{normalParams.UnitID} >> " +
                        $"B1: {normalParams.B1} B2: {normalParams.B2} B3: {normalParams.B3} B4: {normalParams.B4} B5: {normalParams.B5} R2: {normalParams.R2}.");
                }
            }
            else
            {
                success = _weatherRepository.InsertWthNormalParams(normalParams);

                if (success)
                {
                    Log.Information($"Inserted WthNormalParams new AccID/UtilID/UnitID: {normalParams.AccID}/{normalParams.UtilID}/{normalParams.UnitID} >> " +
                        $"B1: {normalParams.B1} B2: {normalParams.B2} B3: {normalParams.B3} B4: {normalParams.B4} B5: {normalParams.B5} R2: {normalParams.R2}.");
                }
                else
                {
                    Log.Error($"Failed to Insert into WthNormalParams new AccID/UtilID/UnitID: {normalParams.AccID}/{normalParams.UtilID}/{normalParams.UnitID} >> " +
                        $"B1: {normalParams.B1} B2: {normalParams.B2} B3: {normalParams.B3} B4: {normalParams.B4} B5: {normalParams.B5} R2: {normalParams.R2}.");
                }
            }

            return success;
        }

        private void UpdateWthExpUsage(List<WthNormalParams> newNormalParamsList)
        {
            Log.Information("Starting UpdateWthExpUsage()...");

            string fromDateStartStr = $"{_fromDateStart.Month}-{_fromDateStart.Day}-{_fromDateStart.Year}";

            int updateCount = 0;
            int insertCount = 0;
            int failCount = 0;

            foreach (WthNormalParams normalParams in newNormalParamsList)
            {
                try
                {
                    List<ReadingsQueryResult> readings = _weatherRepository.GetReadingsForExpUsageUpdate(fromDateStartStr, normalParams);

                    foreach (ReadingsQueryResult result in readings)
                    {
                        try
                        {
                            if (result.DateStart == DateTime.MinValue || result.DateEnd == DateTime.MinValue)
                            {
                                throw new Exception($"DateStart and/or DateEnd is null.");
                            }

                            List<WeatherData> weatherDataList = _weatherRepository.GetWeatherDataByZipStartAndEndDate(result.Zip, result.DateStart, result.DateEnd);

                            if (weatherDataList.Count != result.Days)
                            {
                                throw new Exception($"WeatherDataList.Count != reading.Days; WeatherDataList.Count = {weatherDataList.Count}, " +
                                $"reading.Days = {result.Days}.");
                            }

                            BalancePointPair balancePointPair = new BalancePointPair()
                            {
                                HeatingBalancePoint = normalParams.B3,
                                CoolingBalancePoint = normalParams.B5
                            };

                            HeatingCoolingDegreeDays heatingCoolingDegreeDays = HeatingCoolingDegreeDaysValueOf(balancePointPair, weatherDataList);

                            bool[] successCommaAlreadyExisted = DoCalculation(result, heatingCoolingDegreeDays);

                            if (successCommaAlreadyExisted[0])
                            {
                                if (successCommaAlreadyExisted[1])
                                {
                                    updateCount++;
                                }
                                else
                                {
                                    insertCount++;
                                }
                            }
                            else
                            {
                                failCount++;
                            }
                        }
                        catch (Exception e)
                        {
                            failCount++;
                            Log.Debug($"Cannot calculate ExpUsage for RdngID: {result.RdngID} >> {e.Message}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Error($"{normalParams.AccID}/{normalParams.UtilID}/{normalParams.UnitID} {ex.Message}");
                    Log.Debug($"{ex.StackTrace}");
                }
            }

            if (failCount == 0)
            {
                Log.Information($"Finished UpdateWthExpUsage() on Readings going back to {_fromDateStart.ToShortDateString()}. " +
                    $"Inserts: {insertCount}, Updates: {updateCount}, Failures: {failCount}.");
            }
            else
            {
                Log.Warning($"Finished UpdateWthExpUsage() on Readings goin back to {_fromDateStart.ToShortDateString()}. " +
                    $"Inserts: {insertCount}, Updates: {updateCount}, Failures: {failCount}.");
            }
        }

        private bool[] DoCalculation(ReadingsQueryResult result, HeatingCoolingDegreeDays heatingCoolingDegreeDays)
        {
            double resultAsDouble = (result.B1 * result.Days) + (result.B2 * heatingCoolingDegreeDays.HDD) + (result.B4 * heatingCoolingDegreeDays.CDD);
            decimal resultAsDecimal = decimal.Round(Convert.ToDecimal(resultAsDouble), 4, MidpointRounding.AwayFromZero);

            bool existed = _weatherRepository.GetWthExpUsageExists(result.RdngID);
            bool success;

            if (existed)
            {
                success = _weatherRepository.UpdateWthExpUsage(result.RdngID, resultAsDecimal);
            }
            else
            {
                success = _weatherRepository.InsertWthExpUsage(result.RdngID, resultAsDecimal);
            }

            if (existed && success)
            {
                Log.Debug($"Update WthExpUsage >> RdngID: {result.RdngID} WthExpUsage: {resultAsDecimal} ... B1: {result.B1} B2: {result.B2} " +
                    $"B3: {result.B3} Hdd: {heatingCoolingDegreeDays.HDD} B4: {result.B4} B5: {result.B5} Cdd: {heatingCoolingDegreeDays.CDD} " +
                    $"AccID/UtilID/UnitID: {result.AccID}/{result.UtilID}/{result.UnitID}.");
            }
            else if (existed && !success)
            {
                Log.Error($"FAILED attempt: Update WthExpUsage >> RdngID: {result.RdngID} WthExpUsage: {resultAsDecimal} ... B1: {result.B1} B2: " +
                    $"{result.B2} B3: {result.B3} Hdd: {heatingCoolingDegreeDays.HDD} B4: {result.B4} B5: {result.B5} Cdd: {heatingCoolingDegreeDays.CDD} " +
                    $"AccID/UtilID/UnitID: {result.AccID}/{result.UtilID}/{result.UnitID}.");
            }
            else if (!existed && success)
            {
                Log.Debug($"Inserted into WthExpUsage >> RdngID: {result.RdngID} WthExpUsage: {resultAsDecimal} ... B1: {result.B1} B2: {result.B2} " +
                    $"B3: {result.B3} Hdd: {heatingCoolingDegreeDays.HDD} B4: {result.B4} B5: {result.B5} Cdd: {heatingCoolingDegreeDays.CDD} " +
                    $"AccID/UtilID/UnitID: {result.AccID}/{result.UtilID}/{result.UnitID}.");
            }
            else if (!existed && !success)
            {
                Log.Error($"FAILED attempt: Insert into WthExpUsage >> RdngID: {result.RdngID} WthExpUsage: {resultAsDecimal} ... B1: {result.B1} B2: " +
                    $"{result.B2} B3: {result.B3} Hdd: {heatingCoolingDegreeDays.HDD} B4: {result.B4} B5: {result.B5} Cdd: {heatingCoolingDegreeDays.CDD} " +
                    $"AccID/UtilID/UnitID: {result.AccID}/{result.UtilID}/{result.UnitID}.");
            }

            return new bool[] { success, existed };
        }

        private bool AreDoublesAllNotInfinityNorNaN(double[] doubles)
        {
            foreach (double d in doubles)
            {
                if (Double.IsNaN(d) || Double.IsInfinity(d))
                {
                    return false;
                }
            }
            return true;
        }

        private IWeatherRepository _weatherRepositoryValueOf(AerisJobParams aerisJobParams)
        {
            return new WeatherRepository(aerisJobParams);
        }

        private AerisJobParams AerisJobParamsValueOf(IJobExecutionContext context)
        {
            var schedulerContext = context.Scheduler.Context;
            return (AerisJobParams)schedulerContext.Get("aerisJobParams");
        }
    }
}