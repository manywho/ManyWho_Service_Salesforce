using ManyWho.Flow.SDK;
using ManyWho.Flow.SDK.Run.Elements.Type;
using ManyWho.Service.Salesforce.Singletons;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

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
                    soql = soql + this.GenerateWhereConditions(listFilterAPI.comparisonType, listFilterAPI.where, listFilterAPI.listFilters, cleanedObjectDataTypeProperties);

                    if (!string.IsNullOrEmpty(listFilterAPI.search) &&
                        listFilterAPI.searchCriteria != null &&
                        listFilterAPI.searchCriteria.Count > 0)
                    {
                        soql += " " + listFilterAPI.comparisonType + "(";
                        soql += string.Join(" OR ", listFilterAPI.searchCriteria.Select(criteria => " " + criteria.columnName + " = '" + listFilterAPI.search + "'").ToArray());
                        soql += ")";
                    }

                    if (listFilterAPI.orderByPropertyDeveloperName != null &&
                        listFilterAPI.orderByPropertyDeveloperName.Trim().Length > 0)
                    {
                        soql += " ORDER BY " + listFilterAPI.orderByPropertyDeveloperName + " " + listFilterAPI.orderByDirectionType;
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

                if (soql.Trim().Length > 0 &&
                    soql.IndexOf(" " + listFilterAPI.comparisonType) == 0)
                {
                    // Add the where clause if we have anything
                    soql = " WHERE" + soql;

                    // This is to get rid of any preceding ANDs and ORs
                    soql = soql.Replace("WHERE " + listFilterAPI.comparisonType, "WHERE");
                    soql = soql.Replace("( OR", "(");
                    soql = soql.Replace("( AND", "(");
                }
            }

            return soql;
        }

        private String GenerateWhereConditions(String comparisonType, List<ListFilterWhereAPI> where, List<ListFilterMinimalAPI> listFilters, CleanedObjectDataTypeProperties cleanedObjectDataTypeProperties)
        {
            String soql = "";

            foreach (ListFilterWhereAPI listFilterWhereAPI in where)
            {
                if(listFilterWhereAPI != null)
                {
                    soql = soql + this.GenerateCondition(false, listFilterWhereAPI, comparisonType, cleanedObjectDataTypeProperties);
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

        private Boolean haveWhereFilterToApply(ListFilterAPI listFilterAPI)
        {
            // exist where condition
            if (listFilterAPI.where != null && listFilterAPI.where.Count > 0)
            {
                return true;
            }

            // exist a filter with a where condition
            if (listFilterAPI.listFilters != null && listFilterAPI.listFilters.Count> 0)
            {
                if (listFilterAPI.listFilters.First().where != null && listFilterAPI.listFilters.First().where.Count > 0)
                {
                    return true;
                }
            }

            return false;
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