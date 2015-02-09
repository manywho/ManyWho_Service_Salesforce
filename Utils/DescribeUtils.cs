using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ManyWho.Flow.SDK;
using ManyWho.Flow.SDK.Describe;
using ManyWho.Flow.SDK.Draw.Elements.UI;
using ManyWho.Flow.SDK.Draw.Elements.Type;
using ManyWho.Flow.SDK.Run.Elements.UI;
using ManyWho.Flow.SDK.Run.Elements.Config;
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
