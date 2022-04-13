namespace Grand.Plugin.Payments.AllBank.Models
{
    public class ResponseModel
    {
        public string AuthCode { get; set; }
        public string Response { get; set; }
        public string HostRefNum { get; set; }
        public string ProcReturnCode { get; set; }
        public string TransId { get; set; }
        public string ErrMsg { get; set; }
        public string ReturnOid { get; set; }
        public string HASHPARAMS { get; set; }
        public string HASHPARAMSVAL { get; set; }
        public string HASH { get; set; }
        public string mdStatus { get; set; }
        public string eci { get; set; }
        public string xid { get; set; }
        public string md { get; set; }
        public string rnd { get; set; }
        public string MdErrorMsg { get; set; }
    }
}
