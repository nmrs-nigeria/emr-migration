using System;
using System.Collections.Generic;
using System.Text;

namespace Common
{
    public class CommonResponse
    {
        public string uuid { get; set; }
    }
    public class ArrayResponse
    {
        public List<string> uuids { get; set; }
    }
    public class DataQueryResponseWrapper
    {
        public List<DataQueryResponse> results { get; set; }
        public List<ResponseLink> links { get; set; }
    }

    public class DataQueryResponse
    {
        public string uuid { get; set; }
        public string display { get; set; }
    }
    public class ResponseLink
    {
        public string rel { get; set; }
        public string uri { get; set; }
    }

    //{"results":[{"uuid":"0bff3956-1e61-4cb7-9263-e303c67427ad","display":"AB-07-0110 - FUNMILAYO BELLO","links":[{"rel":"self","uri":"http://localhost:8080/nmrs-migration-tester/ws/rest/v1/patient/0bff3956-1e61-4cb7-9263-e303c67427ad"}]}]}
}
