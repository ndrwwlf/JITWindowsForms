using Dapper;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WeatherForm.Dto;
using WeatherForm.Model;
using WeatherServiceForm;
using WeatherServiceForm.Model;

namespace WeatherForm.Repository
{
    public class WeatherRepository : IWeatherRepository
    {
        private readonly string _jitWeatherConnectionString;
        private readonly string _jitWebData3ConnectionString;

        public WeatherRepository(AerisJobParams aerisJobParams)
        {
            _jitWeatherConnectionString = aerisJobParams.JitWeatherConnectionString;
            _jitWebData3ConnectionString = aerisJobParams.JitWebData3ConnectionString;
        }

        public List<string> GetDistinctZipCodes()
        {
            List<string> data = new List<string>();


            using (IDbConnection jitWebData3Db = new SqlConnection(_jitWebData3ConnectionString))
            {
                data = jitWebData3Db.Query<string>("SELECT DISTINCT b.Zip FROM Buildings AS b " +
                    "JOIN Accounts AS a ON b.BldID = a.BldID " +
                    "JOIN WthNormalParams AS w ON a.AccID = w.AccID").AsList();
            }
            return data;
        }

        public List<WeatherKey> GetAllWeatherKeys()
        {
            var keys = new List<WeatherKey>();

            using (IDbConnection db = new SqlConnection(_jitWeatherConnectionString))
            {
                return db.Query<WeatherKey>("Select [ZipCode], [RDate] from [WeatherData]").AsList();
            }
        }

        public bool InsertWeatherData(WeatherData weatherData)
        {
            string sql = @"
            INSERT INTO [WeatherData] ([StationId], [ZipCode], [RDate], [HighTmp], [LowTmp], [AvgTmp], [DewPt]) 
            VALUES (@StationId, @ZipCode, @RDate, @HighTmp, @LowTmp, @AvgTmp, @DewPT);
            SELECT CAST(SCOPE_IDENTITY() as int)";

            using (IDbConnection db = new SqlConnection(_jitWeatherConnectionString))
            {
                int rowsAffected = db.Execute(sql, new
                {
                    StationID = weatherData.StationId,
                    ZipCode = weatherData.ZipCode,
                    RDate = weatherData.RDate.ToShortDateString(),
                    HighTmp = weatherData.HighTmp,
                    LowTmp = weatherData.LowTmp,
                    AvgTmp = weatherData.AvgTmp,
                    DewPT = weatherData.DewPt
                });

                return (rowsAffected == 1);
            }
        }

        public bool GetWeatherDataExistForZipAndDate(string zipCode, DateTime rDate)
        {
            using (IDbConnection db = new SqlConnection(_jitWeatherConnectionString))
            {
                //DateTime date = Convert.ToDateTime(rDate.ToShortDateString());
                bool exists = db.ExecuteScalar<bool>("SELECT COUNT(1) FROM WeatherData WHERE ZipCode=@ZipCode AND RDate=@RDate",
                    new { ZipCode = zipCode, RDate = rDate });
                return exists;
            }
        }

        public int GetWeatherDataRowCount()
        {
            string sql = @"SELECT COUNT(ID) FROM [WeatherData] WHERE ZipCode IS NOT NULL";
            using (IDbConnection db = new SqlConnection(_jitWeatherConnectionString))
            {
                return db.ExecuteScalar<int>(sql);
            }
        }

        public int GetWeatherDataRowCountByZip(string ZipCode)
        {
            var sql = @"SELECT COUNT(*) FROM WeatherData WHERE ZipCode = @ZipCode";

            using (IDbConnection db = new SqlConnection(_jitWeatherConnectionString))
            {
                return db.ExecuteScalar<int>(sql, new { ZipCode });
            }
        }

        public string GetMostRecentWeatherDataDate()
        {
            using (IDbConnection db = new SqlConnection(_jitWeatherConnectionString))
            {
                var date = db.Query<DateTime>("SELECT TOP(1) RDate FROM WeatherData ORDER BY RDate DESC").First();
                return date.ToShortDateString();
            }
        }

        public List<ReadingsQueryResult> GetReadings(string DateStart)
        {
            string DateEnd = GetMostRecentWeatherDataDate();
            var data = new List<ReadingsQueryResult>();

            string Sql = @"SELECT r.RdngID, b.Zip, r.DateStart,  r.DateEnd, r.Days, r.UnitID as rUnitID, 
                                  wnp.UnitID as wnpUnitID, wnp.B1, wnp.B2, wnp.B3, wnp.B4, wnp.B5
                        FROM Readings r 
                           JOIN WthNormalParams wnp ON wnp.AccID = r.AccID
                                                    AND wnp.UtilID = r.UtilID
                                                    AND wnp.UnitID = r.UnitID
                        JOIN Accounts a ON a.AccID = r.AccID
                        JOIN Buildings b ON b.BldID = a.BldID
                        WHERE NOT EXISTS 
                            (SELECT weu.RdngID FROM WthExpUsage weu
                             WHERE weu.RdngID = r.RdngID)
                        AND r.DateStart >= @DateStart
                        AND r.DateEnd <= @DateEnd
                        ORDER BY DateStart asc";

            //string Sql = @"select r.RdngID, b.Zip, r.DateStart,  r.DateEnd, r.Days, r.UnitID as rUnitID, 
            //                      wnp.UnitID as wnpUnitID, wnp.B1, wnp.B2, wnp.B3, wnp.B4, wnp.B5
            //            from Readings r 
            //               join WthNormalParams wnp on wnp.AccID = r.AccID
            //                                        and wnp.UtilID = r.UtilID
            //                                        and wnp.UnitID = r.UnitID
            //            join Accounts a on a.AccID = r.AccID
            //            join Buildings b on b.BldID = a.BldID
            //            where  r.DateStart >= @DateStart
            //               and r.DateEnd <= @DateEnd
            //            order by DateStart asc";

            using (IDbConnection db = new SqlConnection(_jitWebData3ConnectionString))
            {
                return db.Query<ReadingsQueryResult>(Sql, new { DateStart, DateEnd }).AsList();
            }
        }

        public int GetExpectedWthExpUsageRowCount(string DateStart)
        {
            string DateEnd = GetMostRecentWeatherDataDate();

            string sql = @"select count(r.RdngID) 
                           from Readings r 
                           join WthNormalParams wnp on wnp.AccID = r.AccID
                                                    and wnp.UtilID = r.UtilID
                                                    and wnp.UnitID = r.UnitID
                        join Accounts a on a.AccID = r.AccID
                        join Buildings b on b.BldID = a.BldID
                        where  r.DateStart >= @DateStart
                           and r.DateEnd <= @DateEnd";

            using (IDbConnection db = new SqlConnection(_jitWebData3ConnectionString))
            {
                return db.ExecuteScalar<int>(sql, new { DateStart, DateEnd });
            }
        }

        public int GetActualWthExpUsageRowCount()
        {
            string sql = @"SELECT COUNT(RdngID) FROM [WthExpUsage]";
            //using (IDbConnection db = new SqlConnection(_jitWeatherConnectionString))
            using (IDbConnection db = new SqlConnection(_jitWebData3ConnectionString))

            {
                return db.ExecuteScalar<int>(sql);
            }
        }

        public List<WeatherData> GetWeatherDataByZipStartAndEndDate(string ZipCode, DateTime DateStart, DateTime DateEnd)
        {
            var data = new List<WeatherData>();

            string Sql = @"SELECT ID, (RTRIM(StationId)) as StationId, (RTRIM(ZipCode)) as ZipCode, RDate, HighTmp, LowTmp, AvgTmp, DewPt FROM WeatherData  
                             WHERE ZipCode = @ZipCode  AND RDATE >= @DateStart AND RDATE <= @DateEnd ORDER BY ID";

            using (IDbConnection db = new SqlConnection(_jitWeatherConnectionString))
            {
                return db.Query<WeatherData>(Sql, new { ZipCode, DateStart, DateEnd }).AsList();
            }
        }

        public bool InsertWthExpUsage(int ReadingId, decimal ExpUsage)
        {
            string sql = @"
            INSERT INTO [WthExpUsage] ([RdngID], [ExpUsage]) 
            VALUES (@ReadingID, @ExpUsage);
            SELECT CAST(SCOPE_IDENTITY() as int)";

            /*
             * this is using a mocked WthExpUsage table in local DB. 
             *
            CREATE TABLE [dbo].[WthExpUsage](
	        [RdngID] [int] NOT NULL,
	        [ExpUsage] [decimal](18, 4) NOT NULL
            ) ON [PRIMARY]
            *
            */

            using (IDbConnection db = new SqlConnection(_jitWebData3ConnectionString))
            //using (IDbConnection db = new SqlConnection(_jitWeatherConnectionString))
            {
                int rowsAffected = db.Execute(sql, new
                {
                    ReadingId,
                    ExpUsage
                });

                return (rowsAffected == 1);
            }
        }
    }
}
