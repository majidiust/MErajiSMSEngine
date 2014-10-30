using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;


namespace SMSEngine
{
    class Program
    {
        static Thread m_runnerThread;
        static void Main(string[] args)
        {
            Console.WriteLine("Welcome To Merajipur SMS License Software.....");

            Console.WriteLine("Any Key To Exit ....");
            m_runnerThread = new Thread(new ThreadStart(ThreadHandler));
            m_runnerThread.Start();
            while (true) ;
            Console.WriteLine("See You Later");
        }


        static void SendSMS(string to, string msg)
        {
            SMSService.ArrayOfLong rec = new SMSService.ArrayOfLong();
            byte[] status = null;
            SMSService.SendSoapClient SMSClient = new SMSService.SendSoapClient();



            var username = "merajipur";
            var password = "256401";
            var from = "10000077627719";

            SMSService.ArrayOfString tos = new SMSService.ArrayOfString();
                tos.Add(to);

                SMSClient.SendSms(username, password, tos, from, msg, false, "", ref rec, ref status);

        }

        static bool IsNumber(string str)
        {
            for (int i = 0; i < str.Length; i++)
            {
                if (!(str[i] <= '9' && str[i] >= '0'))
                    return false;
            }
            return true;
        }
        static void ThreadHandler()
        {
            while (true)
            {
                try
                {
                    DatabaseDataContext m_model = new DatabaseDataContext();
                    int num;
                    foreach (SM sm in m_model.SMs)
                    {
                        if (sm.Status == 0)
                        {
                            sm.Status = 1;
                            Console.WriteLine("-------------------------------------------------------");
                            Console.WriteLine("Read New SMS ....");
                            Console.WriteLine("Message : " + sm.Msg);
                            Console.WriteLine("From : " + sm.From);

                            if (IsNumber(sm.Msg))
                            {
                                //Find HW AND Software Code
                                int softwareLength = int.Parse(sm.Msg[0].ToString());
                                string softwareCode = "";
                                string hwCode = "";
                                int softwareCL = 0 ;
                                for (int i = 1; i < sm.Msg.Length; i++)
                                {
                                    if ((i % 2 == 0 && i > 1) && softwareCL < softwareLength)
                                    {
                                        softwareCode += sm.Msg[i];
                                        softwareCL++;
                                    }
                                    else
                                    {
                                        hwCode += sm.Msg[i];
                                    }
                                }

                                Console.WriteLine("Software Code : " + softwareCode);
                                Console.WriteLine("Hardware COde : " + hwCode);

                                //Find ProperSoftware
                                bool find = false;
                                foreach (SMSEngine.SoftwareLisence sl in m_model.SoftwareLisences)
                                {
                                    if (sl.SoftwareCRC == softwareCode)
                                    {
                                        find = true;
                                        int n = m_model.Installations.Count(P => P.SoftwareLisenceID == sl.id);
                                        if (n < sl.num)
                                        {
                                            int installedBefore = m_model.Installations.Count(P => P.MachineKey.Equals(hwCode));
                                            Crc16 f = new Crc16(Crc16Mode.CcittKermit);
                                            softwareCode = f.ComputeChecksum(System.Text.ASCIIEncoding.ASCII.GetBytes(sl.Lisence)).ToString();
                                            Console.WriteLine("Generate New Code .....");
                                            Console.WriteLine("New Software Code : " + softwareCode);
                                            Console.WriteLine("Hardware Code : " + hwCode);
                                            string finalCode = softwareCode.Length.ToString();
                                            int index = 0;
                                            int index2 = 0;
                                            for (int i = 0; i < softwareCode.Length && i < hwCode.Length; i++)
                                            {
                                                finalCode += hwCode[index++];
                                                finalCode += softwareCode[index2++];
                                            }

                                            if (index < hwCode.Length)
                                            {
                                                finalCode += hwCode.Substring(index );
                                            }
                                            else if (index2 < softwareCode.Length)
                                            {
                                                finalCode += softwareCode.Substring(index2);
                                            }

                                            Console.WriteLine("Final Generated Code : " + finalCode);

                                            if (installedBefore == 0)
                                            {
                                                Installation installation = new Installation();
                                                installation.SMSID = sm.ID;
                                                installation.MachineKey = hwCode;
                                                installation.SoftwareLisenceID = sl.id;
                                                installation.Date = DateTime.Now;

                                                m_model.Installations.InsertOnSubmit(installation);
                                            }

                                            SendSMS(sm.From, finalCode);
                                        }
                                        else
                                        {
                                            Console.WriteLine("Installation Capacity is full");
                                            SendSMS(sm.From, "این نرم افزار به تعداد مجاز نصب شده است. برای فعال سازی لطفا با شماره 77627719 تماس بگیرید. با تشکر");
                                        }

                                        break;
                                    }
                                }
                                if (find == false)
                                {
                                    SendSMS(sm.From, "کد وارد شده صحیح نمی باشد");
                                }
                            }
                            else
                            {
                                SendSMS(sm.From, "کد وارد شده صحیح نمی باشد");
                            }

                            m_model.SubmitChanges();
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }

                System.Threading.Thread.Sleep(1000);
            }
        }
    }
}
