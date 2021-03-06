﻿using Dapper;
using Kooboo.IndexedDB.Dynamic;
using Kooboo.IndexedDB.Query;
using KScript;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace Kooboo.Sites.Scripting.Global.RelationalDatabase
{
    public abstract class SqlExecuter<T> where T : IDbConnection
    {
        readonly string _connectionString;

        public abstract char QuotationLeft { get; }
        public abstract char QuotationRight { get; }

        public SqlExecuter(string connectionSring)
        {
            _connectionString = connectionSring;
        }

        internal IDbConnection CreateConnection() => (T)Activator.CreateInstance(typeof(T), _connectionString);

        public abstract RelationalSchema GetSchema(string name);

        public virtual void UpgradeSchema(string name, IEnumerable<RelationalSchema.Item> items)
        {
            var sb = new StringBuilder();

            foreach (var item in items)
            {
                sb.AppendLine($@"ALTER TABLE {WarpField(name)} ADD COLUMN {WarpField(item.Name)} {item.Type.ToString()};");
            }

            using (var connection = CreateConnection())
            {
                connection.Execute(sb.ToString());
            }
        }

        public abstract void CreateTable(string name);

        public virtual void Insert(string name, object data)
        {
            var dic = data as IDictionary<string, object>;
            var columns = string.Join(",", dic.Select(s => $@"{WarpField(s.Key)}"));
            var values = string.Join(",", dic.Select(s => $"@{s.Key}"));
            var sql = $@"INSERT INTO {WarpField(name)}({columns}) VALUES ({values})";

            using (var connection = CreateConnection())
            {
                connection.Execute(sql, new[] { data });
            }
        }

        public virtual void Append(string name, object data, RelationalSchema schema)
        {
            var dic = data as IDictionary<string, object>;
            var removeKeys = new List<string>();

            foreach (var item in dic)
            {
                if (schema.Items.All(a => a.Name != item.Key))
                {
                    removeKeys.Add(item.Key);
                }
            }

            foreach (var item in removeKeys)
            {
                dic.Remove(item);
            }

            var columns = string.Join(",", dic.Select(s => $@"{WarpField(s.Key)}"));
            var values = string.Join(",", dic.Select(s => $"@{s.Key}"));
            var sql = $@"INSERT INTO {WarpField(name)} ({columns}) VALUES ({values})";

            using (var connection = CreateConnection())
            {
                connection.Execute(sql, data);
            }
        }

        public virtual void CreateIndex(string name, string fieldname)
        {
            var sql = $@"CREATE INDEX {fieldname} on {WarpField(name)}({WarpField(fieldname)})";

            using (var connection = CreateConnection())
            {
                connection.Execute(sql);
            }
        }

        public virtual void Delete(string name, string primaryKey, object id)
        {
            var sql = $@"DELETE FROM {WarpField(name)} WHERE {primaryKey} = @Id";

            using (var connection = CreateConnection())
            {
                connection.Execute(sql, new { Id = id });
            }
        }

        public virtual void UpdateData(string name, string primaryKey, object id, object data)
        {
            var dic = data as IDictionary<string, object>;
            var keyValues = string.Join(",", dic.Select(s => $@"{WarpField(s.Key)}=@{s.Key}"));
            dic.Add("__id", id);
            var sql = $@"UPDATE {WarpField(name)} SET {keyValues} WHERE {primaryKey} = @__id";

            using (var connection = CreateConnection())
            {
                connection.Execute(sql, data);
            }
        }

        public abstract RelationModel GetRelation(string name, string relation);

        public virtual object[] QueryData(string name, string where = null, long? limit = null, long? offset = null, string orderBy = null, object @params = null)
        {
            var conditions = QueryPraser.ParseConditoin(where);
            var whereStr = where == null ? string.Empty : $"WHERE {ConditionsToSql(conditions)}";
            var limitStr = limit.HasValue ? $"LIMIT {limit}" : string.Empty;
            var orderByStr = orderBy == null ? string.Empty : $"ORDER BY {orderBy}";
            var offsetStr = offset.HasValue && offset != 0 ? $"OFFSET {offset}" : string.Empty;
            var sql = $@"SELECT * FROM {WarpField(name)} {whereStr} {orderByStr} {limitStr} {offsetStr}";

            using (var connection = CreateConnection())
            {
                return connection.Query<object>(sql, @params).ToArray();
            }
        }

        public virtual int Count(string name, string where = null, long? limit = null, long? offset = null)
        {
            var conditions = QueryPraser.ParseConditoin(where);
            var whereStr = where == null ? string.Empty : $"WHERE {ConditionsToSql(conditions)}";
            var sql = $@"SELECT count(*) FROM {WarpField(name)} {whereStr}";
            int count;

            using (var connection = CreateConnection())
            {
                count = connection.Query<int>(sql).FirstOrDefault();
            }

            if (limit.HasValue && count > limit) count = (int)limit.Value;

            if (offset.HasValue)
            {
                count -= (int)offset.Value;
                if (count < 0) count = 0;
            };

            return count;
        }

        internal string ConditionsToSql(List<ConditionItem> conditions)
        {
            return string.Join(" and ", conditions.Select(s => $@" {WarpField(s.Field)} {ComparerToString(s.Comparer)} {ConventValue(s.Comparer, s.Value)} "));
        }

        internal string WarpField(string field)
        {
            return $"{QuotationLeft}{field}{QuotationRight}";
        }

        static string ComparerToString(Comparer comparer)
        {
            switch (comparer)
            {
                case Comparer.EqualTo:
                    return "=";
                case Comparer.GreaterThan:
                    return ">";
                case Comparer.GreaterThanOrEqual:
                    return ">=";
                case Comparer.LessThan:
                    return "<";
                case Comparer.LessThanOrEqual:
                    return "<=";
                case Comparer.NotEqualTo:
                    return "<>";
                case Comparer.StartWith:
                    return "like";
                case Comparer.Contains:
                    return "like";
                default:
                    throw new NotSupportedException();
            }
        }

        static string ConventValue(Comparer comparer, string value)
        {
            switch (comparer)
            {
                case Comparer.EqualTo:
                case Comparer.NotEqualTo:

                    if (!decimal.TryParse(value, out var _) && !bool.TryParse(value, out var _))
                    {
                        value = $"'{value}'";
                    }

                    break;
                case Comparer.StartWith:
                    value = $"'{value}%'";
                    break;
                case Comparer.Contains:
                    value = $"'%{value}%'";
                    break;
                default:
                    break;
            }

            return value;
        }
    }
}
