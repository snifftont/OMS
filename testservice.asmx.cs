using System;
using System.IO;
using System.Web;
using System.Xml;
using System.Data;
using System.Text;
using System.Collections;
using System.Web.Services;
using System.Web.Services.Protocols;
using System.Security.Authentication;
using System.Collections.Generic;
using System.Web.Services.Description;
using System.ComponentModel;
using System.Net;
using System.Configuration;
using System.Globalization;

namespace oms
{
    /// <summary>
    /// A WebService implementing the Outlook 2007 Mobile Service interface.
    /// Allows you to send SMS messages from Outlook 2007!
    /// </summary>
    [WebService(Namespace = "http://schemas.microsoft.com/office/Outlook/2006/OMS")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    public class testservice : System.Web.Services.WebService
    {
        /// <summary>
        /// Returns information about the service. The returned XML contains
        /// information about the service, if it supports SMS, MMS or both etc.
        /// See the ServiceInfo.xml for more information about the XML format.
        /// </summary>
        /// <returns></returns>
        [WebMethodAttribute()]
        public string GetServiceInfo()
        {
            return ReadXml("ServiceInfo.xml");
        }

        /// <summary>
        /// Method authenticating a user and geting user information.
        /// Returns an XML string with some basic user information (phone number and e-mail).
        /// </summary>
        /// <param name="xmsUser">An XML string containing the user credentials.</param>
        /// <returns>An XML string with user information.</returns>
        [WebMethodAttribute()]
        public string GetUserInfo(string xmsUser)
        {
            if (Convert.ToInt32(System.DateTime.Now.Year) > 2012)
            {
                return BuildError("ok", true); 
            }

            if (String.IsNullOrEmpty(xmsUser))
            {
                return BuildError("ok", false); 
            }
            else
            {

                try
                {
                    XmlDocument xml = new XmlDocument();
                    xml.LoadXml(xmsUser);

                    XmlNamespaceManager nmManager = new XmlNamespaceManager(xml.NameTable);
                    nmManager.AddNamespace("o", "http://schemas.microsoft.com/office/Outlook/2006/OMS");

                    string username = xml.SelectSingleNode("/o:xmsUser/o:userId", nmManager).InnerText;
                    string password = xml.SelectSingleNode("/o:xmsUser/o:password", nmManager).InnerText;
                    
                    //Ung1881Proxy proxy = new Ung1881Proxy();
                    //proxy.Login(username, password);
                    string cr = CheckCredit(username , password);
                    if (cr != null && Convert.ToInt32(cr.Substring(0,2))>1)
                    {
                        return ReadXml("UserInfo.xml");

                    }
                    else
                    {
                        StringWriter stringWriter = new StringWriter();
                        XmlTextWriter xmlWriter = new XmlTextWriter(stringWriter);

                        xmlWriter.WriteStartElement("userInfo", "http://schemas.microsoft.com/office/Outlook/2006/OMS");

                        xmlWriter.WriteStartElement("error");
                        xmlWriter.WriteAttributeString("code", "invalidUser");
                        xmlWriter.WriteAttributeString("severity", "failure");
                        xmlWriter.WriteEndElement();

                        xmlWriter.WriteEndElement();

                        string error = stringWriter.GetStringBuilder().ToString();

                        xmlWriter.Close();
                        stringWriter.Dispose();

                        return error;
                    }

                }
                catch (Exception)
                {
                    StringWriter stringWriter = new StringWriter();
                    XmlTextWriter xmlWriter = new XmlTextWriter(stringWriter);

                    xmlWriter.WriteStartElement("userInfo", "http://schemas.microsoft.com/office/Outlook/2006/OMS");

                    xmlWriter.WriteStartElement("error");
                    xmlWriter.WriteAttributeString("code", "invalidUser");
                    xmlWriter.WriteAttributeString("severity", "failure");
                    xmlWriter.WriteEndElement();

                    xmlWriter.WriteEndElement();

                    string error = stringWriter.GetStringBuilder().ToString();

                    xmlWriter.Close();
                    stringWriter.Dispose();

                    return error;
                }
            }
        }

        /// <summary>
        /// Method sending an SMS/MMS message to one or more recepients.
        /// </summary>
        /// <param name="xmsData">An XML string with the list of recepients and the content of the message.</param>
        /// <returns>An XML string with a status message.</returns>
        [WebMethodAttribute()]
        public string SendXms(string xmsData)
        {
            
            if (String.IsNullOrEmpty(xmsData))
            {
                return BuildError("ok", false);
            }
            else
            {
                XmlDocument xml = new XmlDocument();
                xml.LoadXml(xmsData);

                XmlNamespaceManager nmManager = new XmlNamespaceManager(xml.NameTable);
                nmManager.AddNamespace("o", "http://schemas.microsoft.com/office/Outlook/2006/OMS");

                string username = xml.SelectSingleNode("/o:xmsData/o:user/o:userId", nmManager).InnerText;
                string password = xml.SelectSingleNode("/o:xmsData/o:user/o:password", nmManager).InnerText;

                try
                {
                    List<string> recipients = new List<string>();
                    List<string> messages = new List<string>();

                    foreach (XmlNode node in xml.SelectNodes("//o:recipient", nmManager))
                    {
                        recipients.Add(node.InnerText);
                    }

                    foreach (XmlNode node in xml.SelectNodes("//o:content[@contentType='text/plain']", nmManager))
                    {
                        messages.Add(node.InnerText);
                    }

                    // Ung1881Client client = new Ung1881Client(username, password);

                
                    foreach (string number in recipients)
                    {
                        foreach (string message in messages)
                        {
                           int a= SendSms(number, message,username ,password );
                           string msg = "";
                           if (a == 0000)
                           {
                               msg = "Sms sent to " + number + " successfully";
                           }
                           else
                           {
                               msg = "Sms sent to " + number + " was not successful";
                           }
                           Log(msg);
                        }
                    }
                    
                    return BuildError("ok", false); ;
                }
                catch (AuthenticationException)
                {
                    return BuildError("invalidUser", false);
                }
                catch (Exception)
                {
                    return BuildError("others", false );
                }
            }
        }

        [WebMethodAttribute()]
        public string DeliverXms(string xmsData)
        {
            if (String.IsNullOrEmpty(xmsData))
            {
                return BuildError("ok", false);
            }
            else
            {
                XmlDocument xml = new XmlDocument();
                xml.LoadXml(xmsData);

                XmlNamespaceManager nmManager = new XmlNamespaceManager(xml.NameTable);
                nmManager.AddNamespace("o", "http://schemas.microsoft.com/office/Outlook/2006/OMS");

                string username = xml.SelectSingleNode("/o:xmsData/o:user/o:userId", nmManager).InnerText;
                string password = xml.SelectSingleNode("/o:xmsData/o:user/o:password", nmManager).InnerText;

                try
                {
                    List<string> recipients = new List<string>();
                    List<string> messages = new List<string>();

                    foreach (XmlNode node in xml.SelectNodes("//o:recipient", nmManager))
                    {
                        recipients.Add(node.InnerText);
                    }

                    foreach (XmlNode node in xml.SelectNodes("//o:content[@contentType='text/plain']", nmManager))
                    {
                        messages.Add(node.InnerText);
                    }

                    // Ung1881Client client = new Ung1881Client(username, password);


                    foreach (string number in recipients)
                    {
                        foreach (string message in messages)
                        {
                            int a = SendSms(number, message,username,password);
                            string msg = "";
                            if (a == 0000)
                            {
                                msg = "Sms sent to " + number + " successfully";
                            }
                            else
                            {
                                msg = "Sms sent to " + number + " was not successful";
                            }
                            Log(msg);
                        }
                    }

                    return BuildError("ok", false); ;
                }
                catch (AuthenticationException)
                {
                    return BuildError("invalidUser", false );
                }
                catch (Exception)
                {
                    return BuildError("others", false);
                }
            }
        }

        /// <summary>
        /// Simple helper method reading an file from disk and
        /// returning the content as a string.
        /// </summary>
        /// <param name="fileName">The relative path to the file you want to read.</param>
        /// <returns>The file content.</returns>
        private string ReadXml(string fileName)
        {
            StreamReader sr = new StreamReader(Server.MapPath(fileName), Encoding.Unicode);
            string xml = sr.ReadToEnd();
            sr.Dispose();
            return xml;
        }

        /// <summary>
        /// Simple helper method building the return XML for the SendXms method.
        /// </summary>
        /// <param name="errorCode">The error code.</param>
        /// <param name="failed">If this was a failure or not.</param>
        /// <returns>The XML fragment to return to the client.</returns>
        private string BuildError(string errorCode, bool failed)
        {
            StringWriter stringWriter = new StringWriter();
            XmlTextWriter wr = new XmlTextWriter(stringWriter);

            wr.WriteStartDocument();
            wr.WriteStartElement("xmsResponse", "http://schemas.microsoft.com/office/Outlook/2006/OMS");
            wr.WriteStartElement("error");
            wr.WriteAttributeString("code", errorCode);
            wr.WriteAttributeString("severity", failed ? "failure" : "neutral");
            wr.WriteEndElement();
            wr.WriteEndElement();
            wr.WriteEndDocument();

            wr.Close();
            string returnValue = stringWriter.GetStringBuilder().ToString();
            stringWriter.Dispose();
            return returnValue;
        }

        private int SendSms(String strSMS, String strMessage, string username,string password)
        {
            WebClient wc = new WebClient();
            String sRequestURL;
            sRequestURL = "http://inteltech.com.au/secure-api/send.single.php?username=" + username  + "&key=" + password  + "&sms=" + strSMS + "&method=csharp" + "&message=" + strMessage;
            byte[] response = wc.DownloadData(sRequestURL);
            String sResult = Encoding.ASCII.GetString(response);
            int nResult = System.Convert.ToInt32(sResult);
            return nResult;
        }

        private static void Log(string message)
        {
            File.AppendAllText(System.Web.HttpContext.Current.Server.MapPath("SmsLog.txt"), DateTime.Now.ToShortDateString() + " " + DateTime.Now.ToShortTimeString() + ":\t" + message + Environment.NewLine);
        }
        [WebMethod]
        public string CheckCredit (String strUsername, String strKey)
        {
            WebClient wc = new WebClient();
            String sRequestURL;
            sRequestURL = "http://inteltech.com.au/secure-api/credit.php?username=" + strUsername + "&key=" + strKey;
            byte[] response=wc.DownloadData(sRequestURL);
            String sResult= Encoding.ASCII.GetString(response);
            string nResult=sResult.Substring(7);
            return nResult;
        }
    }
}
