using System.Collections.Generic;
using UnityEngine.Networking;

namespace Dan
{
    internal static partial class Requests
    {
        /// <summary>
        /// Creates a form section and adds the parameters to it.
        /// </summary>
        /// <returns>IMultipartFormSection object</returns>
        public static IMultipartFormSection Field(string fieldName, string data) => 
            new MultipartFormDataSection(fieldName, data);

        /// <summary>
        /// Creates a form out of the given parameters.
        /// </summary>
        /// <param name="formDataSections"></param>
        /// <returns>A list of IMultipartFormSection objects</returns>
        public static List<IMultipartFormSection> Form(params IMultipartFormSection[] formDataSections) =>
            new List<IMultipartFormSection>(formDataSections);
    }
}