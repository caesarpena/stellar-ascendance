using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using NReco;


namespace webapi.Utilities
{
    public static class ExtensionMethods
    {
        // Deep clone
        public static T DeepClone<T>(T obj)
        {
            var str = JsonConvert.SerializeObject(obj);
            var ret = JsonConvert.DeserializeObject<T>(str);
            return ret;
        }
    }
}
