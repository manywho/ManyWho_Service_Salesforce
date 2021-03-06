﻿using ManyWho.Flow.SDK;
using ManyWho.Flow.SDK.Run.Elements.Type;
using ManyWho.Service.Salesforce.Singletons;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ManyWho.Service.Salesforce.Utils
{
    public class QueryConstructorUtil
    {
        public String ConstructQuery(ListFilterAPI listFilterAPI, CleanedObjectDataTypeProperties cleanedObjectDataTypeProperties)
        {
            String soql = "";

            if (listFilterAPI != null)
            {
                // We add this defensive code as the comparison type was not checked for "required"
                if (listFilterAPI.comparisonType == null ||
                    listFilterAPI.comparisonType.Trim().Length == 0)
                {
                    // Assume "AND"
                    listFilterAPI.comparisonType = ManyWhoConstants.LIST_FILTER_CONFIG_COMPARISON_TYPE_AND;
                }

                // We're filtering for a unique object
                if (listFilterAPI.id != null &&
                    listFilterAPI.id.Trim().Length > 0)
                {
                    // Despite this being a little silly as we only have one filter, it makes the logic a little easier to manage on the string construction
                    soql += " " + listFilterAPI.comparisonType + " Id = '" + listFilterAPI.id + "'";
                }
                else
                {
                    // Check to see if we have an actual WHERE filter to apply
                    soql = this.GenerateWhereConditions(listFilterAPI.comparisonType, listFilterAPI.where, listFilterAPI.listFilters, cleanedObjectDataTypeProperties) ;

                    if (!string.IsNullOrEmpty(listFilterAPI.search) &&
                        listFilterAPI.searchCriteria != null &&
                        listFilterAPI.searchCriteria.Count > 0)
                    {
                        string searchWithWildcards = listFilterAPI.search.Replace(" ", "%");
                        string searchCriteriaPart = string.Join(" OR ", listFilterAPI.searchCriteria.Select(
                            criteria => " " + criteria.columnName + " LIKE '%" + searchWithWildcards + "%'").ToArray());

                         // we need to do a binary comparison (content comparison) and ( where search)
                        soql = " (" + soql + ") AND (" + searchCriteriaPart + ")";
                    }

                    if (listFilterAPI.orderByPropertyDeveloperName != null &&
                        listFilterAPI.orderByPropertyDeveloperName.Trim().Length > 0)
                    {
                        soql += " ORDER BY " + listFilterAPI.orderByPropertyDeveloperName + " " + listFilterAPI.orderByDirectionType;
                    } else if ((listFilterAPI.orderBy == null || listFilterAPI.orderBy.Count == 0) && string.IsNullOrEmpty(listFilterAPI.orderByPropertyDeveloperName))
                    {
                        soql += " ORDER BY Id";
                        
                        if (string.IsNullOrEmpty(listFilterAPI.orderByDirectionType))
                        {
                            soql += " ASC";
                        }
                        else
                        {
                            soql += " " + listFilterAPI.orderByDirectionType;
                        }
                    }

                    if (listFilterAPI.limit > 0)
                    {
                        if (listFilterAPI.search != null &&
                            listFilterAPI.search.Trim().Length > 0)
                        {
                            // Search does not support offset, we so we need to do a little calculation to manage that limitation
                            // We basically limit by the offset and then need to ignore the records that come before the offset
                            soql += " LIMIT " + (listFilterAPI.limit + 1 + listFilterAPI.offset);
                        }
                        else
                        {
                            // We grab one extra record so we know if there are any more to get
                            soql += " LIMIT " + (listFilterAPI.limit + 1);
                        }
                    }

                    // Search does not support offset
                    if (listFilterAPI.offset > 0 &&
                        (listFilterAPI.search == null ||
                         listFilterAPI.search.Trim().Length == 0))
                    {
                        soql += " OFFSET " + listFilterAPI.offset;
                    }
                }

                soql = soql.Trim();
                if (soql.Length > 0 && soql.StartsWith("ORDER BY") == false && soql.StartsWith("LIMIT") == false &&
                    (soql.StartsWith(listFilterAPI.comparisonType) ||
                     soql.StartsWith("( " + listFilterAPI.comparisonType) ||
                     soql.StartsWith("() AND") == true))
                {
                    soql = " WHERE " + soql;
                    // remove (), this case happens when there is a searchCriteria with no WHERE part in the listFilter
                    soql = soql.Replace("WHERE () AND ", "WHERE ");
                    // This is to get rid of any preceding ANDs and ORs
                    soql = soql.Replace("WHERE " + listFilterAPI.comparisonType, "WHERE ");
                    soql = soql.Replace("() AND ", "");
                    soql = soql.Replace("( OR", "(");
                    soql = soql.Replace("( AND", "(");
                }
            }

            return " " + soql;
        }

        private String GenerateWhereConditions(String comparisonType, List<ListFilterWhereAPI> where, List<ListFilterMinimalAPI> listFilters, CleanedObjectDataTypeProperties cleanedObjectDataTypeProperties)
        {
            String soql = "";

            if (where != null)
            {
                foreach (ListFilterWhereAPI listFilterWhereAPI in where)
                {
                    if (listFilterWhereAPI != null)
                    {
                        soql = soql + this.GenerateCondition(false, listFilterWhereAPI, comparisonType, cleanedObjectDataTypeProperties);
                    }
                }

            }

            if (listFilters != null)
            {
                foreach (ListFilterMinimalAPI listFilter in listFilters)
                {
                    if (listFilter != null)
                    {
                        soql = soql + " " + comparisonType + " (" + GenerateWhereConditions(listFilter.comparisonType, listFilter.where, listFilter.listFilters, cleanedObjectDataTypeProperties) + ")";
                    }
                }
            }

            return soql;
        }

        private String GenerateCondition(Boolean first, ListFilterWhereAPI listFilterWhereAPI, String comparisonType, CleanedObjectDataTypeProperties cleanedObjectDataTypeProperties)
        {
            String soql = "";
            if (first) {
                soql += " " + listFilterWhereAPI.columnName;
            } else {
                soql += " " + comparisonType + " " + listFilterWhereAPI.columnName;
            }

            if (listFilterWhereAPI.criteriaType.Equals(ManyWhoConstants.CONTENT_VALUE_IMPLEMENTATION_CRITERIA_TYPE_EQUAL, StringComparison.InvariantCultureIgnoreCase) == true)
            {
                soql += " =";
            }
            else if (listFilterWhereAPI.criteriaType.Equals(ManyWhoConstants.CONTENT_VALUE_IMPLEMENTATION_CRITERIA_TYPE_GREATER_THAN, StringComparison.InvariantCultureIgnoreCase) == true)
            {
                soql += " >";
            }
            else if (listFilterWhereAPI.criteriaType.Equals(ManyWhoConstants.CONTENT_VALUE_IMPLEMENTATION_CRITERIA_TYPE_GREATER_THAN_OR_EQUAL, StringComparison.InvariantCultureIgnoreCase) == true)
            {
                soql += " >=";
            }
            else if (listFilterWhereAPI.criteriaType.Equals(ManyWhoConstants.CONTENT_VALUE_IMPLEMENTATION_CRITERIA_TYPE_LESS_THAN, StringComparison.InvariantCultureIgnoreCase) == true)
            {
                soql += " <";
            }
            else if (listFilterWhereAPI.criteriaType.Equals(ManyWhoConstants.CONTENT_VALUE_IMPLEMENTATION_CRITERIA_TYPE_LESS_THAN_OR_EQUAL, StringComparison.InvariantCultureIgnoreCase) == true)
            {
                soql += " <=";
            }
            else if (listFilterWhereAPI.criteriaType.Equals(ManyWhoConstants.CONTENT_VALUE_IMPLEMENTATION_CRITERIA_TYPE_NOT_EQUAL, StringComparison.InvariantCultureIgnoreCase) == true)
            {
                soql += " !=";
            }
            else if (listFilterWhereAPI.criteriaType.Equals(ManyWhoConstants.CONTENT_VALUE_IMPLEMENTATION_CRITERIA_TYPE_STARTS_WITH, StringComparison.InvariantCultureIgnoreCase) == true ||
                     listFilterWhereAPI.criteriaType.Equals(ManyWhoConstants.CONTENT_VALUE_IMPLEMENTATION_CRITERIA_TYPE_ENDS_WITH, StringComparison.InvariantCultureIgnoreCase) == true ||
                     listFilterWhereAPI.criteriaType.Equals(ManyWhoConstants.CONTENT_VALUE_IMPLEMENTATION_CRITERIA_TYPE_CONTAINS, StringComparison.InvariantCultureIgnoreCase) == true)
            {
                soql += " LIKE";
            }
            else
            {
                throw new NotImplementedException();
            }

            if (listFilterWhereAPI.criteriaType.Equals(ManyWhoConstants.CONTENT_VALUE_IMPLEMENTATION_CRITERIA_TYPE_STARTS_WITH, StringComparison.InvariantCultureIgnoreCase) == true)
            {
                soql += " '" + listFilterWhereAPI.value + "%'";
            }
            else if (listFilterWhereAPI.criteriaType.Equals(ManyWhoConstants.CONTENT_VALUE_IMPLEMENTATION_CRITERIA_TYPE_ENDS_WITH, StringComparison.InvariantCultureIgnoreCase) == true)
            {
                soql += " '%" + listFilterWhereAPI.value + "'";
            }
            else if (listFilterWhereAPI.criteriaType.Equals(ManyWhoConstants.CONTENT_VALUE_IMPLEMENTATION_CRITERIA_TYPE_CONTAINS, StringComparison.InvariantCultureIgnoreCase) == true)
            {
                soql += " '%" + listFilterWhereAPI.value + "%'";
            }
            else
            {
                Boolean valueAssigned = false;

                if (cleanedObjectDataTypeProperties != null)
                {
                    Boolean isDataType = false;

                    if (cleanedObjectDataTypeProperties.BooleanFields != null &&
                        cleanedObjectDataTypeProperties.BooleanFields.TryGetValue(listFilterWhereAPI.columnName, out isDataType) == true)
                    {
                        Boolean booleanValue = false;
                        Boolean.TryParse(listFilterWhereAPI.value, out booleanValue);

                        soql += " " + booleanValue.ToString().ToLower() + "";

                        valueAssigned = true;
                    }
                    else if (cleanedObjectDataTypeProperties.DateTimeFields != null &&
                             cleanedObjectDataTypeProperties.DateTimeFields.TryGetValue(listFilterWhereAPI.columnName, out isDataType) == true)
                    {
                        DateTime dateTimeValue;
                        DateTime.TryParse(listFilterWhereAPI.value, out dateTimeValue);

                        soql += " " + dateTimeValue.ToString("yyyy-MM-ddThh:mm:ssZ") + "";

                        valueAssigned = true;
                    }
                    else if (cleanedObjectDataTypeProperties.DateFields != null &&
                             cleanedObjectDataTypeProperties.DateFields.TryGetValue(listFilterWhereAPI.columnName, out isDataType) == true)
                    {
                        DateTime dateTimeValue;
                        DateTime.TryParse(listFilterWhereAPI.value, out dateTimeValue);

                        soql += " " + dateTimeValue.ToString("yyyy-MM-dd") + "";

                        valueAssigned = true;
                    }
                    else if (cleanedObjectDataTypeProperties.NumberFields != null &&
                             cleanedObjectDataTypeProperties.NumberFields.TryGetValue(listFilterWhereAPI.columnName, out isDataType) == true)
                    {
                        Double doubleValue;
                        Double.TryParse(listFilterWhereAPI.value, out doubleValue);

                        soql += " " + doubleValue.ToString() + "";

                        valueAssigned = true;
                    }
                    else if (cleanedObjectDataTypeProperties.CurrencyFields != null &&
                             cleanedObjectDataTypeProperties.CurrencyFields.TryGetValue(listFilterWhereAPI.columnName, out isDataType) == true)
                    {
                        Double doubleValue;
                        Double.TryParse(listFilterWhereAPI.value, out doubleValue);

                        soql += " " + doubleValue.ToString() + "";

                        valueAssigned = true;
                    }
                }

                // If the value has not been assigned based on type information, we assign it as a string query
                if (valueAssigned == false)
                {
                    soql += " '" + listFilterWhereAPI.value + "'";
                }

            }

            return soql;
        }
    }
}