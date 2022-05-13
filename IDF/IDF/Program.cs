using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IDF
{
    class Program
    {
        static void Main(string[] args)
        {
            using (var connection = new SqliteConnection("Data Source=autompg.db"))
            {
                connection.Open();

                var command = connection.CreateCommand();
                command.CommandText = "SELECT * FROM autompg";
                List<Autompg> databaseInfo = new List<Autompg>();

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                        databaseInfo.Add(new Autompg(reader));
                }

                float mpgH = CalcH(StandardDeviation(databaseInfo.Select(auto => auto.mpg)), databaseInfo.Count);
                float cylindersH = CalcH(StandardDeviationF(databaseInfo.Select(auto => auto.cylinders)), databaseInfo.Count);
                float displacementH = CalcH(StandardDeviation(databaseInfo.Select(auto => auto.displacement)), databaseInfo.Count);
                float horsepowerH = CalcH(StandardDeviation(databaseInfo.Select(auto => auto.horsepower)), databaseInfo.Count);
                float weightH = CalcH(StandardDeviation(databaseInfo.Select(auto => auto.weight)), databaseInfo.Count);
                float accelerationH = CalcH(StandardDeviationF(databaseInfo.Select(auto => auto.acceleration)), databaseInfo.Count);
                float modelyearH = CalcH(StandardDeviation(databaseInfo.Select(auto => auto.modelyear)), databaseInfo.Count);

                Console.WriteLine(mpgH);
            }
        }

        static float CalcH(float SD, int n) => 1.06f * SD * (float)Math.Pow(n, -1/5);

        //danku https://stackoverflow.com/questions/3141692/standard-deviation-of-generic-list
        static float StandardDeviation(IEnumerable<int> values)
        {
            if (!values.Any())
                return 0;

            // Compute the average.     
            var avg = values.Average();

            // Perform the Sum of (value-avg)_2_2.      
            var sum = values.Sum(d => Math.Pow(d - avg, 2));

            // Put it all together.      
            return (float)Math.Sqrt((sum) / (values.Count() - 1));
        }

        static float StandardDeviationF(IEnumerable<float> values)
        {
            if (!values.Any())
                return 0;

            // Compute the average.     
            var avg = values.Average();

            // Perform the Sum of (value-avg)_2_2.      
            var sum = values.Sum(d => Math.Pow(d - avg, 2));

            // Put it all together.      
            return (float)Math.Sqrt((sum) / (values.Count() - 1));
        }
    }

    class Autompg
    {
        //numerical
        public int mpg;
        public float cylinders;
        public int displacement;
        public int horsepower;
        public int weight;
        public float acceleration;
        public int modelyear;

        //categoricals
        public int origin;
        public string brand;
        public string model;
        public string type;

        public Autompg(SqliteDataReader reader)
        {
            mpg = reader.GetInt32(1);
            cylinders = reader.GetFloat(2);
            displacement = reader.GetInt32(3);
            horsepower = reader.GetInt32(4);
            weight = reader.GetInt32(5);
            acceleration = reader.GetFloat(6);
            modelyear = reader.GetInt32(7);
            origin = reader.GetInt32(8);
            brand = reader.GetString(9);
            model = reader.GetString(10);
            type = reader.GetString(11);
        }
    }
}
