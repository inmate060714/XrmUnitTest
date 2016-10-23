﻿using Microsoft.Xrm.Sdk.Query;
using System.Collections.Generic;
using System.Linq;

namespace DLaB.Xrm
{
    public partial class Extensions
    {
        #region ConditionExpression

        private static bool EqualsCondition(this ConditionExpression c1, ConditionExpression c2)
        {
            return (c1 == c2) || (c1 != null && c2 != null &&
                c1.AttributeName == c2.AttributeName &&
                c1.Operator == c2.Operator &&
                c1.Values.SequenceEqual(c2.Values));
        }

        #endregion ConditionExpression

        #region FilterExpression

        /// <summary>
        /// Determines whether the specified entity name has condition.
        /// </summary>
        /// <param name="filter">The filter.</param>
        /// <param name="entityName">Name of the entity.</param>
        /// <param name="columnNameAndValuePairs">The column name and value pairs.</param>
        /// <returns></returns>
        public static bool HasCondition(this FilterExpression filter, params object[] columnNameAndValuePairs)
        {
            var tmp = new FilterExpression();

            // match all conditions one at a time.
            return tmp.WhereEqual(columnNameAndValuePairs).Conditions.All(filter.HasCondition);
        }

        /// <summary>
        /// Determines whether the current filter has the given condition.
        /// </summary>
        /// <param name="filter">The filter.</param>
        /// <param name="condition">The condition.</param>
        /// <returns></returns>
        public static bool HasCondition(this FilterExpression filter, ConditionExpression condition)
        {
            return filter.Conditions.Any(c => c.EqualsCondition(condition)) ||
                filter.Filters.Any(f => f.HasCondition(condition));
        }

        #endregion FilterExpression

        #region LinkEntity

        /// <summary>
        /// Returns all Filters that are filtering on the given entity logical name
        /// </summary>
        /// <param name="link"></param>
        /// <param name="logicalName"></param>
        /// <returns></returns>
        public static IEnumerable<FilterExpression> GetEntityFilters(this LinkEntity link, string logicalName)
        {
            if (link.LinkToEntityName == logicalName)
            {
                yield return link.LinkCriteria;
            }

            foreach (var childLink in link.LinkEntities)
            {
                foreach (var filter in childLink.GetEntityFilters(logicalName))
                {
                    yield return filter;
                }
            }
        }

        /// <summary>
        /// Determines whether the current entity link has all the conditions generated by the columnNameAndValuePairs.
        /// </summary>
        /// <param name="link">The link.</param>
        /// <param name="columnNameAndValuePairs">The column name and value pairs.</param>
        /// <returns></returns>
        public static bool HasCondition(this LinkEntity link, params object[] columnNameAndValuePairs)
        {
            var tmp = new FilterExpression();

            // match all conditions one at a time.
            return tmp.WhereEqual(columnNameAndValuePairs).Conditions.All(link.HasCondition);
        }

        /// <summary>
        /// Determines whether current LinkEntity has the given condition.
        /// </summary>
        /// <param name="link">The link.</param>
        /// <param name="condition">The condition.</param>
        /// <returns></returns>
        public static bool HasCondition(this LinkEntity link, ConditionExpression condition)
        {
            return link.LinkCriteria.HasCondition(condition) || link.LinkEntities.Any(l => l.HasCondition(condition));
        }

        /// <summary>
        /// Determines whether the specified logical name has entity.
        /// </summary>
        /// <param name="link">The link.</param>
        /// <param name="logicalName">Name of the logical.</param>
        /// <returns></returns>
        public static bool HasEntity(this LinkEntity link, string logicalName)
        {
            return link.LinkToEntityName == logicalName || link.LinkEntities.Any(l => l.HasEntity(logicalName));
        }

        #endregion LinkEntity

        #region QueryExpression

        /// <summary>
        /// Returns all Filters that are filtering on the given entity logical name
        /// </summary>
        /// <param name="qe"></param>
        /// <param name="logicalName"></param>
        /// <returns></returns>
        public static IEnumerable<FilterExpression> GetEntityFilters(this QueryExpression qe, string logicalName)
        {
            if(qe.EntityName == logicalName){
                yield return qe.Criteria;
            }

            foreach (var link in qe.LinkEntities)
            {
                foreach (var filter in link.GetEntityFilters(logicalName))
                {
                    yield return filter;
                }
            }
        }

        /// <summary>
        /// Determines whether the current QueryExpression has all conditions defined by the columnNameAndValuePairs.
        /// </summary>
        /// <param name="qe">The qe.</param>
        /// <param name="columnNameAndValuePairs">The column name and value pairs.</param>
        /// <returns></returns>
        public static bool HasCondition(this QueryExpression qe, params object[] columnNameAndValuePairs)
        {
            var tmp = new FilterExpression();

            // match all conditions one at a time.
            return tmp.WhereEqual(columnNameAndValuePairs).Conditions.All(qe.HasCondition);
        }

        /// <summary>
        /// Determines whether the current QueryExpression has the given condition.
        /// </summary>
        /// <param name="qe">The qe.</param>
        /// <param name="condition">The condition.</param>
        /// <returns></returns>
        public static bool HasCondition(this QueryExpression qe, ConditionExpression condition)
        {
            return qe.Criteria.HasCondition(condition) ||
                  qe.LinkEntities.Any(l => l.HasCondition(condition));
        }

        /// <summary>
        /// Determines whether the specified logical name has entity.
        /// </summary>
        /// <param name="qe">The qe.</param>
        /// <param name="logicalName">Name of the logical.</param>
        /// <returns></returns>
        public static bool HasEntity(this QueryExpression qe, string logicalName)
        {
            return qe.EntityName == logicalName || qe.LinkEntities.Any(l => l.HasEntity(logicalName));
        }

        #endregion QueryExpression   
    }
}
