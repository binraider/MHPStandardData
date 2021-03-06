using System;
using System.Data;
using System.Text;
using System.Collections;
using System.Configuration;
using System.Web;
using System.Xml;


namespace MHPStandardData {


    public class DTReturnClass {
        public DataTable Datatable = null;
        public bool Success = true;
        public string Message = "";
        public string Techmessage = "";

        public DTReturnClass() {

        }

        public DTReturnClass(bool _success) {
            Success = _success;
        }
    }

    [Serializable()]
    public class ReturnClass {
        private bool mSuccess = true;
        private string mMessage = "";
        private string mTechmessage = "";
        private int mIntvar = 0;
        private double mDoublevar = 0;
        private long mLongvar = 0;

        public bool Success {
            get { return mSuccess; }
            set { mSuccess = value; }
        }

        public string Message {
            get { return mMessage; }
            set { mMessage = value; }
        }

        public string Techmessage {
            get { return mTechmessage; }
            set { mTechmessage = value; }
        }

        public int Intvar {
            get { return mIntvar; }
            set { mIntvar = value; }
        }

        public double Doublevar {
            get { return mDoublevar; }
            set { mDoublevar = value; }
        }

        public long Longvar {
            get { return mLongvar; }
            set { mLongvar = value; }
        }

        public ReturnClass() {

        }

        public ReturnClass(bool _success) {
            mSuccess = _success;
        }

        public void AddMessage(string _msg) {
            mMessage += "\r\n" + _msg;
        }

        public void AddTechMessage(string _msg) {
            mTechmessage += "\r\n" + _msg;
        }

        public string GetBothMessages() {
            return mMessage + " " + mTechmessage;
        }


        public string GetMessage(bool admin) {
            string outy = "";
            if (admin) {
                if (mTechmessage.Length == 0) {
                    outy = mMessage;
                } else {
                    outy = mTechmessage;
                }
            } else {
                outy = mMessage;
            }
            return outy;
        }

        public void SetFailureMessage(string message) {
            mSuccess = false;
            mMessage = message;
            mTechmessage = message;
        }


        public void SetFailureMessage(string message, string techmessage) {
            mSuccess = false;
            mMessage = message;
            mTechmessage = techmessage;
        }

        /*
        public void SendNotificationEmail(EmailHelperClass helper) {
            WebEmailClass2 email = new WebEmailClass2(helper.LogFolder, helper.SmtpSSL);
            SimpleEmailVO vo = new SimpleEmailVO();
            string subject = "Error from CMS4 Date:" + DateTime.Now.ToString("yyyy/MM/dd hh:mm") + " Application:" + helper.ApplicationName + " Url:" + helper.Url;
            vo.ToEmail = helper.ToEmail;
            vo.ToName = helper.ToName;
            vo.FromEmail = helper.FromEmail;
            vo.FromName = helper.FromName;
            vo.Subject = subject;
            vo.HtmlBody = subject + " <p>Message:[" + Message + "]</p> <p>Techmessage:[" + Techmessage + "]</p>";
            vo.TextBody = subject + "\r\n\r\n Message:" + Message + " \r\n\r\n Techmessage:[" + Techmessage + "]";
            ReturnClass outcome = email.Send(vo);
        }
        */
    }
}
