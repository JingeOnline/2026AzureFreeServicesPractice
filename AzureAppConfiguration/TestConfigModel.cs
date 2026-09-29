using System;
using System.Collections.Generic;
using System.Text;

namespace AzureAppConfiguration
{
    public class TestConfigModel
    {
        public string FileName { get; set; }
        public bool IsEnabled { get; set; }
        public int Size { get; set; }
        public string[] Categories { get; set; }
    }
}
