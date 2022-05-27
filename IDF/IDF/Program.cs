using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

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
                Dictionary<string, float> H = new Dictionary<string, float>();
                H.Add("mpg", CalcH(StandardDeviation(databaseInfo.Select(auto => auto.mpg)), databaseInfo.Count));
                H.Add("cylinders", CalcH(StandardDeviationF(databaseInfo.Select(auto => auto.cylinders)), databaseInfo.Count));
                H.Add("displacement", CalcH(StandardDeviation(databaseInfo.Select(auto => auto.displacement)), databaseInfo.Count));
                H.Add("horsepower", CalcH(StandardDeviation(databaseInfo.Select(auto => auto.horsepower)), databaseInfo.Count));
                H.Add("weight", CalcH(StandardDeviation(databaseInfo.Select(auto => auto.weight)), databaseInfo.Count));
                H.Add("acceleration", CalcH(StandardDeviationF(databaseInfo.Select(auto => auto.acceleration)), databaseInfo.Count));
                H.Add("model_year", CalcH(StandardDeviation(databaseInfo.Select(auto => auto.modelyear)), databaseInfo.Count));

                //idf categorical
                Dictionary<string, Dictionary<string, float>> IDF = new Dictionary<string, Dictionary<string, float>>();
                IDF.Add("origin", GetIDF(databaseInfo.Select(auto => auto.origin)));
                IDF.Add("brand", GetIDF(databaseInfo.Select(auto => auto.brand)));
                IDF.Add("model", GetIDF(databaseInfo.Select(auto => auto.model)));
                IDF.Add("type", GetIDF(databaseInfo.Select(auto => auto.type)));

                //rfq both
                Dictionary<string, Dictionary<string, int>> rqf = new Dictionary<string, Dictionary<string, int>>();
                Dictionary<string, Dictionary<(string, string), int>> jacr = new Dictionary<string, Dictionary<(string, string), int>>();
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
                            foreach (string value1 in values)
                            {
                                string v1 = value1.Substring(1, value1.Length - 2);
                                foreach (string value2 in values)
                                {
                                    string v2 = value2.Substring(1, value2.Length - 2);
                                    if (v1.CompareTo(v2) != -1)
                                        continue;

                                    if (!jacr.ContainsKey(name))
                                        jacr.Add(name, new Dictionary<(string, string), int>());

                                    if (!jacr[name].ContainsKey((v1, v2)))
                                        jacr[name].Add((v1, v2), 0);

                                    jacr[name][(v1, v2)] += count;
                                }
                            }
                        }
                        else throw new NotImplementedException();

                        foreach (string value in values)
                        {
                            string v = value.Substring(1, value.Length - 2);
                            if (name == "id")
                                continue;

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

                //jac
                Dictionary<string, Dictionary<(string, string), float>> jac = new Dictionary<string, Dictionary<(string, string), float>>();
                foreach (KeyValuePair<string, Dictionary<(string, string), int>> jacrPair in jacr)
                {
                    jac.Add(jacrPair.Key, new Dictionary<(string, string), float>());
                    foreach (KeyValuePair<(string, string), int> jacrAttributePair in jacrPair.Value)
                    {
                        int val1 = rqf[jacrPair.Key][jacrAttributePair.Key.Item1];
                        int val2 = rqf[jacrPair.Key][jacrAttributePair.Key.Item2];
                        jac[jacrPair.Key].Add(jacrAttributePair.Key, jacrAttributePair.Value /
                            (float)(val1 + val2 - jacrAttributePair.Value));
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
                Dictionary<string, Dictionary<string, float>> QF = new Dictionary<string, Dictionary<string, float>>();
                QF.Add("mpg", GetQF(rqf["mpg"], rqfmax["mpg"], 1));
                QF.Add("cylinders", GetQF(rqf["cylinders"], rqfmax["cylinders"], 1));
                QF.Add("displacement", GetQF(rqf["displacement"], rqfmax["displacement"], 1));
                QF.Add("horsepower", GetQF(rqf["horsepower"], rqfmax["horsepower"], 1));
                //weight is not queried
                QF.Add("acceleration", GetQF(rqf["acceleration"], rqfmax["acceleration"], 1));
                QF.Add("model_year", GetQF(rqf["model_year"], rqfmax["model_year"], 1));
                //origin is not queried
                QF.Add("brand", GetQF(rqf["brand"], rqfmax["brand"], 0));
                //model is not queried
                QF.Add("type", GetQF(rqf["type"], rqfmax["type"], 0));

                var insertCommand = connection.CreateCommand();
                StringBuilder commandBuilder = new StringBuilder(); 
                foreach (KeyValuePair<string, float> hPair in H)
                    commandBuilder.AppendLine($"INSERT INTO H_Value (attribute, h) VALUES ('{hPair.Key}', '{hPair.Value}');");

                foreach (KeyValuePair<string, Dictionary<string, float>> idfPair in IDF)
                    foreach (KeyValuePair<string, float> idfAttributePair in idfPair.Value)
                        commandBuilder.AppendLine($"INSERT INTO IDF (attribute, value, idf) VALUES ('{idfPair.Key}', '{idfAttributePair.Key}', '{idfAttributePair.Value}');");

                foreach (KeyValuePair<string, Dictionary<string, float>> qfPair in QF)
                    foreach (KeyValuePair<string, float> qfAttributePair in qfPair.Value)
                        commandBuilder.AppendLine($"INSERT INTO QF (attribute, value, qf) VALUES ('{qfPair.Key}', '{qfAttributePair.Key}', '{qfAttributePair.Value}');");

                foreach (KeyValuePair<string, Dictionary<(string, string), float>> jacPair in jac)
                    foreach (KeyValuePair<(string, string), float> jacAttributePair in jacPair.Value)
                        commandBuilder.AppendLine($"INSERT INTO Jac (attribute, value1, value2, jac) VALUES ('{jacPair.Key}', '{jacAttributePair.Key.Item1}', '{jacAttributePair.Key.Item2}', '{jacAttributePair.Value}');");

                insertCommand.CommandText = commandBuilder.ToString();
                int code = insertCommand.ExecuteNonQuery();
                Console.WriteLine(code);
            }
        }

        static Dictionary<T, float> GetQF<T>(Dictionary<T, int> rqf, int max, int add)
        {
            Dictionary<T, float> result = new Dictionary<T, float>();
            foreach (KeyValuePair<T, int> pair in rqf)
                result.Add(pair.Key, (pair.Value + add) / (float)(max + add));
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
                result.Add(pair.Key, (float)Math.Log(collection.Count() / (float)pair.Value));

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
        public string origin;
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
            origin = reader.GetInt32(8).ToString(); //origin acts more as a string
            brand = reader.GetString(9);
            model = reader.GetString(10);
            type = reader.GetString(11);
        }
    }
}
