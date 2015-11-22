using System.Text.RegularExpressions;
using Microsoft.AspNet.Http.Features;
using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.Mvc.Filters;

namespace ManyWho.Service.Salesforce.Filters
{
    public class ExceptionFilter : IExceptionFilter
    {
        public void OnException(ExceptionContext context)
        {
            SetResultContent(context, context.Exception.Message, 500);
        }

        static void SetResultContent(ExceptionContext context, string content, int statusCode)
        {
            // Set the Reason Phrase, stripping any new lines
            var responseFeature = context.HttpContext.Features.Get<IHttpResponseFeature>();
            if (responseFeature != null)
            {
                responseFeature.ReasonPhrase = Regex.Replace(content, @"\t|\n|\r", " ");
            }

            // Set the response result
            context.Result = new ContentResult()
            {
                Content = content,
                StatusCode = statusCode
            };
        }
    }
}
