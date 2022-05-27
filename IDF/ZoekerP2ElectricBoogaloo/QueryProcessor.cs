using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Diagnostics.CodeAnalysis;

namespace ZoekerP2ElectricBoogaloo
{
    class QueryProcessor
    {
        List<Autompg> databaseInfo = new List<Autompg>();
        Dictionary<string, float> h = new Dictionary<string, float>();
        Dictionary<string, Dictionary<object, float>> idf = new Dictionary<string, Dictionary<object, float>>();
        Dictionary<string, Dictionary<object, float>> qf = new Dictionary<string, Dictionary<object, float>>();
        Dictionary<string, Dictionary<(string, string), float>> jac = new Dictionary<string, Dictionary<(string, string), float>>();
        private bool isCategorical(string s) => s == "origin" || s == "brand" || s == "model" || s == "type";
        private string[] allAttributes = new string[] { "mpg", "cylinders", "displacement", "horsepower", "weight", "acceleration", "model_year", "origin", "brand", "model", "type" };

        public QueryProcessor()
        {
            using (var connection = new SqliteConnection("Data Source=database.db"))
            {
                connection.Open();

                var getCommand = connection.CreateCommand();
                getCommand.CommandText = "SELECT * FROM autompg";
                using (var reader = getCommand.ExecuteReader())
                {
                    while (reader.Read())
                        databaseInfo.Add(new Autompg(reader));
                }

                var hCommand = connection.CreateCommand();
                hCommand.CommandText = "SELECT * FROM H_Value";
                using (var reader = hCommand.ExecuteReader())
                {
                    while (reader.Read())
                        h.Add(reader.GetString(0), GetFloat(reader, 1));
                }

                var idfCommand = connection.CreateCommand();
                idfCommand.CommandText = "SELECT * FROM IDF";
                using (var reader = idfCommand.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string attribute = reader.GetString(0);
                        if (!idf.ContainsKey(attribute))
                            idf.Add(attribute, new Dictionary<object, float>());

                        if (isCategorical(attribute))
                            idf[attribute].Add(reader.GetString(1), GetFloat(reader, 2));
                        else
                            throw new NotImplementedException();
                    }
                }

                var qfCommand = connection.CreateCommand();
                qfCommand.CommandText = "SELECT * FROM QF";
                using (var reader = qfCommand.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string attribute = reader.GetString(0);
                        if (!qf.ContainsKey(attribute))
                            qf.Add(attribute, new Dictionary<object, float>());

                        if (isCategorical(attribute))
                            qf[attribute].Add(reader.GetString(1), GetFloat(reader, 2));
                        else
                            qf[attribute].Add(GetFloat(reader, 1), GetFloat(reader, 2));
                    }
                }

                var jacCommand = connection.CreateCommand();
                jacCommand.CommandText = "SELECT * FROM Jac";
                using (var reader = jacCommand.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string attribute = reader.GetString(0);
                        if (!jac.ContainsKey(attribute))
                            jac.Add(attribute, new Dictionary<(string, string), float>());

                        if (isCategorical(attribute))
                        {
                            jac[attribute].Add((reader.GetString(1), reader.GetString(2)), GetFloat(reader, 3));
                            ;
                        }
                        else
                            throw new NotImplementedException();
                    }
                }
            }
        }

        //expected query form: k = 6, brand = 'volkswagen';
        public string Process(string query)
        {
            string[] commaSplits = query.Substring(0, query.Length - 1).Split(", "); //remove ; and split all the vars
            Dictionary<string, object> attributeValueTarget = new Dictionary<string, object>(); //the variable name and the requested value
            int k = 10;
            foreach (string commaSplit in commaSplits)
            {
                string[] eqSplit = commaSplit.Split(" = ");
                string attributeName = eqSplit[0];
                string attributeValue = eqSplit[1];
                if (attributeName == "k")
                {
                    k = int.Parse(attributeValue);
                    continue;
                }

                if (isCategorical(attributeName)) attributeValueTarget.Add(attributeName, attributeValue.Substring(1, attributeValue.Length - 2)); //remove ''
                else attributeValueTarget.Add(attributeName, float.Parse(attributeValue));
            }

            ScoredAuto[] scores = new ScoredAuto[databaseInfo.Count];
            IEnumerable<string> otherAttributes = allAttributes.Where(s => !attributeValueTarget.ContainsKey(s));
            int i = 0;
            foreach (Autompg auto in databaseInfo)
            {
                float score = 0;
                float tiebreaker = 0;
                foreach (KeyValuePair<string, object> target in attributeValueTarget)
                {
                    if (target.Value is string s)
                    {
                        string val = (string)auto.attributes[target.Key];
                        if (val == s)
                            score += idf[target.Key][target.Value];
                        else 
                        {
                            if (!jac.ContainsKey(target.Key)) continue;

                            (string, string) ord = Ordered(val, s);
                            if (jac[target.Key].ContainsKey(ord))
                                score += idf[target.Key][target.Value] * jac[target.Key][ord];
                        }
                    }
                    else if (target.Value is float f)
                    {
                        float val = (float)auto.attributes[target.Key];
                        float idfval = (float)Math.Log(databaseInfo.Count / (databaseInfo.Sum(autoInfo => NumIdf((float)autoInfo.attributes[target.Key], val, target.Key))));
                        score += NumIdf(f, val, target.Key) * idfval;
                    }
                }
                foreach (string att in otherAttributes)
                {
                    if (!qf.ContainsKey(att)) continue;

                    object val = auto.attributes[att];
                    tiebreaker += qf[att].ContainsKey(val) ? qf[att][val] : 0;
                }
                scores[i++] = new ScoredAuto(score, tiebreaker, auto);
            }
            Array.Sort(scores);

            StringBuilder sb = new StringBuilder();
            for (int j = 0; j < k; j++)
                sb.AppendLine(scores[j].score + ": " + scores[j].auto.ToString());
            return sb.ToString();
        }

        private float Squared(float f) => f * f;

        private (string, string) Ordered(string s1, string s2) => s1.CompareTo(s2) <= 0 ? (s1, s2) : (s2, s1);

        private float NumIdf(float f1, float f2, string attribute) => (float)Math.Exp(-0.5 * Squared((f1 - f2) / h[attribute]));

        public static float GetFloat(SqliteDataReader reader, int index) => float.Parse(reader.GetString(index));
    }

    class ScoredAuto : IComparable<ScoredAuto>
    {
        public float score;
        public float tiebreaker;
        public Autompg auto;

        public ScoredAuto(float _score, float _tiebreaker, Autompg _auto)
        {
            score = _score;
            tiebreaker = _tiebreaker;
            auto = _auto;
        }

        public int CompareTo(ScoredAuto other)
        {
            if (score != other.score)
                return other.score.CompareTo(score); //reverse sort, higest first

            return other.tiebreaker.CompareTo(tiebreaker);
        }
    }

    class Autompg
    {
        public Dictionary<string, object> attributes = new Dictionary<string, object>(); //fuck reflection all my homies use dictionary

        public Autompg(SqliteDataReader reader)
        {
            attributes.Add("mpg", reader.GetFloat(1));
            attributes.Add("cylinders", reader.GetFloat(2));
            attributes.Add("displacement", reader.GetFloat(3));
            attributes.Add("horsepower", reader.GetFloat(4));
            attributes.Add("weight", reader.GetFloat(5));
            attributes.Add("acceleration", reader.GetFloat(6));
            attributes.Add("model_year", reader.GetFloat(7));
            attributes.Add("origin", reader.GetString(8));
            attributes.Add("brand", reader.GetString(9));
            attributes.Add("model", reader.GetString(10));
            attributes.Add("type", reader.GetString(11));
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            foreach (KeyValuePair<string, object> attribute in attributes)
                sb.Append($"{attribute.Key}: {attribute.Value} ");
            sb.AppendLine();
            return sb.ToString();
        }
    }
}
