using System;
using System.Collections.Generic;
using ManyWho.Flow.SDK;
using ManyWho.Flow.SDK.Describe;
using ManyWho.Flow.SDK.Draw.Elements.Value;
using ManyWho.Flow.SDK.Draw.Elements.Config;
using ManyWho.Flow.SDK.Run.Elements.Type;

namespace ManyWho.Service.Salesforce
{
    public class DescribeUtils
    {
        public static DescribeValueAPI CreateDescribeValue(String contentType, String developerName, String contentValue, Boolean required)
        {
            DescribeValueAPI describeValue = null;

            describeValue = new DescribeValueAPI();
            describeValue.contentType = contentType;
            describeValue.developerName = developerName;
            describeValue.contentValue = contentValue;
            describeValue.isRequired = required;

            return describeValue;
        }

        public static ServiceValueRequestAPI CreateServiceValue(String contentType, String developerName, ValueElementResponseAPI valueElementResponse)
        {
            ServiceValueRequestAPI serviceValue = null;

            serviceValue = new ServiceValueRequestAPI();
            serviceValue.contentType = contentType;
            serviceValue.developerName = developerName;

            if (valueElementResponse != null)
            {
                serviceValue.valueElementToReferenceId = new ValueElementIdAPI();
                serviceValue.valueElementToReferenceId.id = valueElementResponse.id;
            }

            return serviceValue;
        }

        public static ObjectAPI CreateAttributeObject(String label, String value)
        {
            ObjectAPI attributeObject = null;
            PropertyAPI attributeProperty = null;

            attributeObject = new ObjectAPI();
            attributeObject.externalId = value;
            attributeObject.developerName = ManyWhoConstants.AUTHENTICATION_AUTHENTICATION_ATTRIBUTE_OBJECT_DEVELOPER_NAME;
            attributeObject.properties = new List<PropertyAPI>();

            attributeProperty = new PropertyAPI();
            attributeProperty.developerName = ManyWhoConstants.AUTHENTICATION_ATTRIBUTE_LABEL;
            attributeProperty.contentValue = label;

            attributeObject.properties.Add(attributeProperty);

            attributeProperty = new PropertyAPI();
            attributeProperty.developerName = ManyWhoConstants.AUTHENTICATION_ATTRIBUTE_VALUE;
            attributeProperty.contentValue = value;

            attributeObject.properties.Add(attributeProperty);

            return attributeObject;
        }
    }
}
