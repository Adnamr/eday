﻿using System;
using System.Linq;
using System.Data.Objects;
using System.Text;
using System.Diagnostics;
using System.Configuration;

namespace EdayRoom.API
{
    public class Debbuging
    {
        private const string debugSeperator =
    "-------------------------------------------------------------------------------";

        public static IQueryable<T> TraceQuery<T>(IQueryable<T> query)
        {

            if (query != null)
            {
                ObjectQuery<T> objectQuery = query as ObjectQuery<T>;
                if (objectQuery != null)
                {
                    StringBuilder queryString = new StringBuilder();
                    queryString.Append(Environment.NewLine)
                        .AppendLine(debugSeperator)
                        .AppendLine("QUERY GENERATED...")
                        .AppendLine(debugSeperator)
                        .AppendLine(objectQuery.ToTraceString())
                        .AppendLine(debugSeperator)
                        .AppendLine(debugSeperator)
                        .AppendLine("PARAMETERS...")
                        .AppendLine(debugSeperator);
                    foreach (ObjectParameter parameter in objectQuery.Parameters)
                    {
                        queryString.Append(String.Format("{0}({1}) \t- {2}", parameter.Name, parameter.ParameterType, parameter.Value)).Append(Environment.NewLine);
                    }
                    queryString.AppendLine(debugSeperator).Append(Environment.NewLine);
                    Console.WriteLine(queryString);
                    Trace.WriteLine(queryString);
                }
            }
            return query;
        }
    }
}