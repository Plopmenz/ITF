using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.IO;
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

                var getCommand = connection.CreateCommand();
                getCommand.CommandText = "SELECT * FROM autompg";
                List<Autompg> databaseInfo = new List<Autompg>();

                using (var reader = getCommand.ExecuteReader())
                {
                    while (reader.Read())
                        databaseInfo.Add(new Autompg(reader));
                }

                //h values numerical
                float mpgH = CalcH(StandardDeviation(databaseInfo.Select(auto => auto.mpg)), databaseInfo.Count);
                float cylindersH = CalcH(StandardDeviationF(databaseInfo.Select(auto => auto.cylinders)), databaseInfo.Count);
                float displacementH = CalcH(StandardDeviation(databaseInfo.Select(auto => auto.displacement)), databaseInfo.Count);
                float horsepowerH = CalcH(StandardDeviation(databaseInfo.Select(auto => auto.horsepower)), databaseInfo.Count);
                float weightH = CalcH(StandardDeviation(databaseInfo.Select(auto => auto.weight)), databaseInfo.Count);
                float accelerationH = CalcH(StandardDeviationF(databaseInfo.Select(auto => auto.acceleration)), databaseInfo.Count);
                float modelyearH = CalcH(StandardDeviation(databaseInfo.Select(auto => auto.modelyear)), databaseInfo.Count);

                //idf categorical
                Dictionary<int, float> originIDF = GetIDF(databaseInfo.Select(auto => auto.origin));
                Dictionary<string, float> brandIDF = GetIDF(databaseInfo.Select(auto => auto.brand));
                Dictionary<string, float> modelIDF = GetIDF(databaseInfo.Select(auto => auto.model));
                Dictionary<string, float> typeIDF = GetIDF(databaseInfo.Select(auto => auto.type));

                //rfq both
                Dictionary<string, Dictionary<string, int>> rqf = new Dictionary<string, Dictionary<string, int>>();
                StreamReader sReader = new StreamReader("workload.txt");
                string line;
                while ((line = sReader.ReadLine()) != null)
                {
                    if (line.Contains("COUNT"))
                        continue;

                    string[] split = line.Split(" times: ");
                    int count = int.Parse(split[0]);

                    string filter = split[1].Split(" WHERE ")[1];
                    string[] attributeValues = filter.Split(" AND ");
                    foreach (string attributeValue in attributeValues)
                    {
                        string name;
                        string[] values;
                        if (attributeValue.Contains(" = "))
                        {
                            string[] eqSplit = attributeValue.Split(" = ");

                            name = eqSplit[0];
                            values = new string[] { eqSplit[1] }; //remove ''
                        }
                        else if (attributeValue.Contains(" IN "))
                        {
                            string[] inSplit = attributeValue.Split(" IN ");

                            name = inSplit[0];
                            values = inSplit[1].Substring(1, inSplit[1].Length - 2).Split(','); //remove ()
                        }
                        else throw new NotImplementedException();

                        foreach (string value in values)
                        {
                            string v = value.Substring(1, value.Length - 2);
                            if (name == "id")
                                continue;

                            Console.WriteLine($"{name}: {v}");
                            if (rqf.ContainsKey(name))
                            {
                                if (rqf[name].ContainsKey(v))
                                    rqf[name][v] += count;
                                else
                                    rqf[name].Add(v, count);
                            }
                            else
                            {
                                rqf.Add(name, new Dictionary<string, int>());
                                rqf[name].Add(v, count);
                            }
                        }
                    }
                }

                //rqfmax both
                Dictionary<string, int> rqfmax = new Dictionary<string, int>();
                foreach (KeyValuePair<string, Dictionary<string, int>> bigPair in rqf)
                {
                    int highestCount = int.MinValue;
                    foreach (KeyValuePair<string, int> smallPair in bigPair.Value)
                    {
                        if (smallPair.Value <= highestCount)
                            continue;

                        highestCount = smallPair.Value;
                    }
                    rqfmax.Add(bigPair.Key, highestCount);
                }

                //qf both
                Dictionary<string, float> mpgQF = GetQF(rqf["mpg"], rqfmax["mpg"], 1);
                Dictionary<string, float> cylindersQF = GetQF(rqf["cylinders"], rqfmax["cylinders"], 1);
                Dictionary<string, float> displacementQF = GetQF(rqf["displacement"], rqfmax["displacement"], 1);
                Dictionary<string, float> horsepowerQF = GetQF(rqf["horsepower"], rqfmax["horsepower"], 1);
                //Dictionary<string, float> weightQF = GetQF(rqf["weight"], rqfmax["weight"], 1);
                Dictionary<string, float> accelerationQF = GetQF(rqf["acceleration"], rqfmax["acceleration"], 1);
                Dictionary<string, float> modelyearQF = GetQF(rqf["model_year"], rqfmax["model_year"], 1);
                //Dictionary<string, float> originQF = GetQF(rqf["origin"], rqfmax["origin"]);
                Dictionary<string, float> brandQF = GetQF(rqf["brand"], rqfmax["brand"], 0);
                //Dictionary<string, float> modelQF = GetQF(rqf["model"], rqfmax["model"]);
                Dictionary<string, float> typeQF = GetQF(rqf["type"], rqfmax["type"], 0);
            }
        }

        static Dictionary<T, float> GetQF<T>(Dictionary<T, int> rqf, int max, int add)
        {
            Dictionary<T, float> result = new Dictionary<T, float>();
            foreach (KeyValuePair<T, int> pair in rqf)
                result.Add(pair.Key, (pair.Value + add) / (max + add));
            return result;
        }

        static Dictionary<T, float> GetIDF<T>(IEnumerable<T> collection)
        {
            Dictionary<T, int> counter = new Dictionary<T, int>();
            foreach (T t in collection)
            {
                if (counter.ContainsKey(t))
                    counter[t]++;
                else
                    counter.Add(t, 1);
            }

            Dictionary<T, float> result = new Dictionary<T, float>();
            foreach(KeyValuePair<T, int> pair in counter)
                result.Add(pair.Key, (float)Math.Log(collection.Count() / pair.Value));

            return result;
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
