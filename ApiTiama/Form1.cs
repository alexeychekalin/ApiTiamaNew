using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Windows.Forms;
using System.Xml;
using ApiTiama.ServiceReference3;
using System.Data.SqlClient;
using ApiTiama.Properties;
using WindowsFormsApp1;
using System.ComponentModel;
using System.Xml.Linq;
using System.Data;
using System.Threading;

//
namespace ApiTiama
{
    public partial class Form1 : Form
    {
        int In_1, In_2, In_3, In_4, In_5, In_6, In_7, In_8;
        DateTime DT = new DateTime();

        XmlDocument _getMolds = new XmlDocument();

        public struct EJ
        {
            public string mold;
            public string reason;
        }

        List<EJ> _ejected = new List<EJ>();

        Dictionary<string, string> translate = new Dictionary<string, string>();
        public Form1()
        {
            InitializeComponent();
            translate.Add("Rejects", "Сброшено всего");
            translate.Add("Autoreject", "Автосброс");
            translate.Add("Defects", "Сброшено с дефектом");
            translate.Add("Inspected", "Проинспектированно");

            #region НАСТРОЙКА и ЗАПУСК СЧИТЫВАНИЯ-ЗАПИСИ EJECTED MOLDS

            backgroundWorker1.DoWork += BackgroundWorker1_DoWork;
            backgroundWorker1.RunWorkerCompleted += BackgroundWorker1_RunWorkerCompleted;
            timer1.Interval = 60000; // частота обновлениЯ
            timer1.Start();
            timer1.Tick += (o, e) => backgroundWorker1.RunWorkerAsync();
            #endregion

        }

        private void BackgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            var sql = "";
            var conn = DbWalker.GetConnection(Resources.Server, Resources.User, Resources.Password, Resources.secure, "CPS" + Resources.Cech);
            try
            {
                conn.Open();

                if (_getMolds.GetElementsByTagName("Inspected")[0] == null)
                {
                    toolStripStatusLabel1.Text = "Машина на отдыхе, вернула пустой ответ";
                    richTextBox2.Text += DateTime.Now + " - " + "Ответ COUNT пустой"+Environment.NewLine;
                    return;
                }

                #region Записываем -1 в таблицу
                sql = "UPDATE [Line_3_001_CES_1] SET ";

                for (int i = 0; i < 100; i++)
                {
                    sql += " M" + i + " = -1 ,";
                }

                sql += " Time = @time WHERE id = 1";

                var command = new SqlCommand(sql, conn);
                command.Parameters.AddWithValue("@time", DateTime.Now);

                command.ExecuteNonQuery();

                toolStripStatusLabel1.Text = "В таблицу записаны -1";
                #endregion

                #region Обновляем установленные формы

                var mcf = _getMolds.GetElementsByTagName("Mold");

                sql = "UPDATE [Line_3_001_CES_1] SET ";

                if(mcf.Count != 0)
                {
                    foreach (XmlNode tag in mcf)
                    {
                        sql += " M" + tag.Attributes.GetNamedItem("id").InnerText + " = 0 ,";
                    }

                    sql = sql.Remove(sql.Length - 1); // удаляем последнюю запятую
                    sql += " WHERE id = 1";

                    command = new SqlCommand(sql, conn);
                    command.ExecuteNonQuery();

                    toolStripStatusLabel1.Text = "Обновлены данные по установленным формам";
                }

                #endregion

            }
            catch (Exception ex)
            {
                richTextBox2.Text += DateTime.Now + " - " + ex.Message + Environment.NewLine;
                richTextBox2.Text += DateTime.Now + " - " + sql + Environment.NewLine;
            }

            #region Обновляем сдув
            if (e.Result == null) toolStripStatusLabel1.Text = "На сдуве нет форм, ответ пуст"; return;
            /*
            if (e.Result == null)
            {
                toolStripStatusLabel1.Text = "Обновляю сдув из файла, ответ пуст";
                var preRead = File.ReadAllLines("buffer.txt").ToList();

                // Запишем в таблицу FALSE
                foreach (var item in preRead)
                {
                    var sqlLocal = "INSERT INTO [Line_3_001_Report_CES_1] (Time, Operation, Num_Mould) VALUES (@p1, @p2, @p3) ";
                    var commandLocal = new SqlCommand(sqlLocal, conn);
                    commandLocal.Parameters.AddWithValue("@p1", DateTime.Now);
                    commandLocal.Parameters.AddWithValue("@p2", 0);
                    commandLocal.Parameters.AddWithValue("@p3", item);
                    commandLocal.ExecuteNonQuery();
                }

                // очистим файл
                File.WriteAllText("buffer.txt", string.Empty);
                return;
            } 
            */
            var ejected = (List<EJ>)e.Result;

            if (ejected.Count == 0) toolStripStatusLabel1.Text = "На сдуве нет форм, ответ пуст"; return;
            /*
            if (t.Count == 0) 
            {
                toolStripStatusLabel1.Text = "Обновляю сдув из файла, ответ пуст";
                var preRead = File.ReadAllLines("buffer.txt").ToList();

                // Запишем в таблицу FALSE
                foreach (var item in preRead)
                {
                    var sqlLocal = "INSERT INTO [Line_3_001_Report_CES_1] (Time, Operation, Num_Mould) VALUES (@p1, @p2, @p3) ";
                    var commandLocal = new SqlCommand(sqlLocal, conn);
                    commandLocal.Parameters.AddWithValue("@p1", DateTime.Now);
                    commandLocal.Parameters.AddWithValue("@p2", 0);
                    commandLocal.Parameters.AddWithValue("@p3", item);
                    commandLocal.ExecuteNonQuery();
                }

                // очистим файл
                File.WriteAllText("buffer.txt", string.Empty);
                return;
            }
            */



            try
            {
                toolStripStatusLabel1.Text = "Обновляю сдув, c машины поступили данные. Форм в ответе:" + ejected.Count;

                var id1 = "UPDATE [Line_3_001_CES_1] SET ";
                var id2 = "UPDATE [Line_3_001_CES_1] SET ";
                var id3 = "UPDATE [Line_3_001_CES_1] SET ";
                var id4 = "UPDATE [Line_3_001_CES_1] SET ";
                   

                ejected.ForEach(x =>
                {
                    var sqlLocal = "INSERT INTO [Line_3_001_Report_CES_1] (Time, Operation, Num_Mould) VALUES (@p1, @p2, @p3) ";
                    var commandLocal = new SqlCommand(sqlLocal, conn);
                    commandLocal.Parameters.AddWithValue("@p1", DateTime.Now);
                    commandLocal.Parameters.AddWithValue("@p2", 1);
                    commandLocal.Parameters.AddWithValue("@p3", x.mold);
                    commandLocal.ExecuteNonQuery();

                    id1 += " M" + x.mold + " = 1 , ";
                    id2 += " M" + x.mold + " = " + x.reason + " , ";
                    id3 += " M" + x.mold + " = -1 , ";
                    id4 += " M" + x.mold + " = -1 , ";
                });

                try
                {

                    var command = new SqlCommand(id1.Remove(id1.Length - 2) + " WHERE Id = 1 ", conn);
                    command.ExecuteNonQuery();

                    command = new SqlCommand(id2.Remove(id2.Length - 2) + " WHERE Id = 2 ", conn);
                    command.ExecuteNonQuery();

                    command = new SqlCommand(id3.Remove(id3.Length - 2) + " WHERE Id = 3 ", conn);
                    command.ExecuteNonQuery();

                    command = new SqlCommand(id4.Remove(id4.Length - 2) + " WHERE Id = 4 ", conn);
                    command.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("ОШИБКА! Запись в БД данных о ПС в авт.режиме: " + ex.Message);
                }

                /*
                 * toolStripStatusLabel1.Text = "Обновляю сдув из файла, на сдуве есть данные";
                // read file to get previous update 
                var preRead = File.ReadAllLines("buffer.txt").ToList();
                // Сравним файл с ответом и выберем отсутствующие в ответе формы
                var result = preRead.Where(x => !t.Any(n => n.mold == x)).ToList();
                // Сравним файл с ответом и выберем существующие в ответе формы
                var result2 = t.Where(x => !preRead.Any(n => n == x.mold)).ToList();
                
                if(result.Count != 0)
                {
                    // Запишем в таблицу FALSE
                    foreach (var item in result)
                    {
                        var sqlLocal = "INSERT INTO [Line_3_001_Report_CES_1] (Time, Operation, Num_Mould) VALUES (@p1, @p2, @p3) ";
                        var commandLocal = new SqlCommand(sqlLocal, conn);
                        commandLocal.Parameters.AddWithValue("@p1", DateTime.Now);
                        commandLocal.Parameters.AddWithValue("@p2", 0);
                        commandLocal.Parameters.AddWithValue("@p3", item);
                        commandLocal.ExecuteNonQuery();
                    }
                }

                // очистим файл
                File.WriteAllText("buffer.txt", string.Empty);

                if (result2.Count != 0)
                {
                    foreach (var item in result2)
                    {
                        var sqlLocal = "INSERT INTO [Line_3_001_Report_CES_1] (Time, Operation, Num_Mould, Reason) VALUES (@p1, @p2, @p3, @p4) ";
                        var commandLocal = new SqlCommand(sqlLocal, conn);
                        commandLocal.Parameters.AddWithValue("@p1", DateTime.Now);
                        commandLocal.Parameters.AddWithValue("@p2", 1);
                        commandLocal.Parameters.AddWithValue("@p3", item.mold);
                        commandLocal.Parameters.AddWithValue("@p4", item.reason);
                        commandLocal.ExecuteNonQuery();

                        sql += " M" + item.mold + " = 1 ,";
                    }

                    sql = sql.Remove(sql.Length - 1); // удаляем последнюю запятую
                    sql += " WHERE id = 1";

                    var command = new SqlCommand(sql, conn);
                    command.ExecuteNonQuery();

                    toolStripStatusLabel1.Text = "Обновлены данные по сдуву";
                }
                // обновим файл
                t.ForEach(x => File.AppendAllText("buffer.txt", x.mold + Environment.NewLine));
                */
            }
            catch (Exception ex)
            {
                richTextBox2.Text += DateTime.Now + " - " + ex.Message + Environment.NewLine;
                richTextBox2.Text += DateTime.Now + " - " + sql + Environment.NewLine;
                //MessageBox.Show(@"BackgroundWorker1: v " + ex.Message);
            }
            finally
            {
                conn.Close();
            }
            #endregion

        }

        private void BackgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            var m = new ServiceTM11SoapClient();
            XmlDocument docXML = new XmlDocument(); // XML-документ
            var ans = m.EjectedMolds().InnerXml;
            ans = "<xml>" + ans + "</xml>";
            docXML.LoadXml(ans); // загрузить XML
            
           // docXML.Load("ej.xml");

            if (docXML.GetElementsByTagName("xml")[0].ChildNodes.Count == 0)
            {
                return;
            }
            else
            {
                var ej = new List<EJ>();
                // разбор xml и передача в Woker
                foreach(XmlNode node in docXML.GetElementsByTagName("xml")[0].ChildNodes)
                {
                    ej.Add(new EJ { mold = node.Attributes.GetNamedItem("nb").Value, reason = node.Attributes.GetNamedItem("reason").Value });
                }

                e.Result = ej;
            }
        }
        // структура для хранения данных о сбросах
        public struct data
        {
           public string mould, deffect, count, sensorId, id;
        }

        public struct sbros
        {
            public int cej, cpa, ces, mould;
        }

        //Словарь для 1 канала (калибр)
        Dictionary<string, string> dict1chanel = new Dictionary<string, string>()
        {
            { "1", "Калибр"},
            { "2", "Деффект 2"},
            { "4", "Деффект 3"},
            { "5", "Деффект 4"},
        };

        // Словарь для видов посечек
        Dictionary<int, string> dict27chanel = new Dictionary<int, string>()
        {
            { 1, "024\\1"},
            { 2, "024\\3"},
            { 3, "024\\5"},
            { 4, "024\\6"},
            { 5, "024\\14"},
            { -1, "-"},
            //{ 0, "-"},
        };

        // словарь для заполнения полей CEJ, CPA, CES в запросе SQL
        Dictionary<string, int> sqlDic = new Dictionary<string, int>()
        {
            {"Rejects", 1},
            {"Inspected", 2},
            {"Autoreject", 3},
            {"Defects", 4},
        };

        // МАССИВ ДЛЯ ХРАНЕНИЯ ДАННЫХ ЗАПРОСА, ИНИЦИАЛИЗИРУЕМ 0
        int[] sqlValues = Enumerable.Repeat(0, 5).ToArray();

        

        private void button4_Click(object sender, EventArgs e)
        {
            richTextBox1.Clear();
            var m = new ServiceTM11SoapClient();
            richTextBox1.Text = m.Counts().InnerXml;
        }

        //Запрос форм на принудительном сдуве
        private void button1_Click(object sender, EventArgs e)
        {
            var m = new ServiceTM11SoapClient();

            var _url = "http://192.168.1.224/WSTM11/Service.asmx";
            var _action = "http://www.tiama-inspection.com/EjectedMolds";

            XmlDocument soapEnvelopeXml = CreateSoapEnvelope("1.xml");
            HttpWebRequest webRequest = CreateWebRequest(_url, _action);
            InsertSoapEnvelopeIntoWebRequest(soapEnvelopeXml, webRequest);

            // begin async call to web request.
            IAsyncResult asyncResult = webRequest.BeginGetResponse(null, null);

            // suspend this thread until call is complete. You might want to
            // do something usefull here like update your UI.
            asyncResult.AsyncWaitHandle.WaitOne();

            // get the response from the completed web request.
            string soapResult;
            using (WebResponse webResponse = webRequest.EndGetResponse(asyncResult))
            {
                using (StreamReader rd = new StreamReader(webResponse.GetResponseStream()))
                {
                    soapResult = rd.ReadToEnd();
                }
                richTextBox2.Text = soapResult;
            }
        }

        private static HttpWebRequest CreateWebRequest(string url, string action)
        {
            HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(url);
            webRequest.Headers.Add("SOAPAction", action);
            webRequest.ContentType = "text/xml; charset=utf-8";
            webRequest.Accept = "text/xml";
            webRequest.Method = "POST";
            return webRequest;
        }

        private static XmlDocument CreateSoapEnvelope(string name)
        {
            XmlDocument soapEnvelopeDocument = new XmlDocument();
            soapEnvelopeDocument.Load(name);
            return soapEnvelopeDocument;
        }

        private static void InsertSoapEnvelopeIntoWebRequest(XmlDocument soapEnvelopeXml, HttpWebRequest webRequest)
        {
            using (Stream stream = webRequest.GetRequestStream())
            {
                soapEnvelopeXml.Save(stream);
            }
        }


        //Постановка и снятие с принудительного сдува
        private void button2_Click(object sender, EventArgs e)
        {
            var m = new ServiceTM11SoapClient();

            var _url = "http://192.168.1.224/WSTM11/Service.asmx";
            var _action = "http://www.tiama-inspection.com/AddEjectedMolds";

            XmlDocument soapEnvelopeXml = CreateSoapEnvelope("2.xml");
            HttpWebRequest webRequest = CreateWebRequest(_url, _action);
            InsertSoapEnvelopeIntoWebRequest(soapEnvelopeXml, webRequest);

            // begin async call to web request.
            IAsyncResult asyncResult = webRequest.BeginGetResponse(null, null);

            // suspend this thread until call is complete. You might want to
            // do something usefull here like update your UI.
            asyncResult.AsyncWaitHandle.WaitOne();

            // get the response from the completed web request.
            string soapResult;
            using (WebResponse webResponse = webRequest.EndGetResponse(asyncResult))
            {
                using (StreamReader rd = new StreamReader(webResponse.GetResponseStream()))
                {
                    soapResult = rd.ReadToEnd();
                }
                richTextBox2.Text = soapResult;
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            /*
             Обработка поставновки на сдув:
                1) var sended = GetDataForEject со 2 параметром 0 - Получаем из БД данные по формам, которые необходимо поставить на сдув
                2) CreateAddEjectedMoldsXml (sended) - формируем XML
                3) SendEjectToMashine - отправляем сформированный XML на машину и ждем ответа
                4) var getted = GetEjectedFromM1 - получаем данные какие формы стоят на сдуве
                5) UpdateInDB(sended,getted, TABLE, 0) - обновляем данные в таблицах и заверщаем работу
             
             */
            var sended = GetDataForEject("[CPS2].[dbo].[Line_3_001_CES]", 0);
            if(sended.Count() > 0)
            {
                if(CreateAddEjectedMoldsXml(sended)) SendEjectToMashine("192.168.1.223");
                ejectlog.Text += "************** ОЖИДАЮ 10 СЕКУНД **************************" + Environment.NewLine;
                Thread.Sleep(10000);
                ejectlog.Text += "^^^^^^^^^^^^^^^ ПРОДОЛЖАЕМ ^^^^^^^^^^^^^^^" + Environment.NewLine;
                var getted = GetEjectedFromM1();
                UpdateInDB(sended, getted, "[CPS2].[dbo].[Line_3_001_CES]", 0);

            } 

        }

        #region ОБРАБОТКА ПОСТАНОВКИ/СНЯТИЯ ПС
        /*GetDataForEject
            Формирует массив форм для поставновки/снятия со сдува
            Входные параметры:
                    - string TABLE - таблица в которой осуществлять поиск
                    - int VALUE - значения для поиска ( 0 и 1 ) - постановка или снятие со сдува
            Выходные парамерты
                    - List<EJ> toEject - список форм с указанием причины сдува
         */
        private List<EJ> GetDataForEject(string table, int value)
        {
            ejectlog.Text += "----> Начинаю ВЫБОРКУ форм и кодов принудительного сдува" + Environment.NewLine;
            var conn = DbWalker.GetConnection(Resources.Server, Resources.User, Resources.Password, Resources.secure, "CPS" + Resources.Cech);
            var toEject = new List<EJ>();
            try
            {
                conn.Open();

                var sql = "select * from " + table + " where  Id in (3,4)";
                var command = new SqlCommand(sql, conn);

                DataTable dt = new DataTable();
                SqlDataAdapter da = new SqlDataAdapter(command);
                da.Fill(dt);

                toEject = dt.Rows[0].ItemArray.ToList().Select((val, ind) => new { Index = ind, Value = val }).Where(x => x.Index > 1 && Convert.ToInt32(x.Value) == value).Select(p => new EJ { mold = (p.Index - 2).ToString(), reason = dt.Rows[1].ItemArray[p.Index].ToString() }).ToList();

                if (toEject.Count > 0)
                {
                    ejectlog.Text += "<---- Формы НАЙДЕНЫ, отправляю на сборку XML" + Environment.NewLine;
                }

                ejectlog.Text += "<---- Формы не найдены, ЗАВЕРШАЮ работу" + Environment.NewLine;
            }
            catch (Exception ex)
            {
                ejectlog.Text += "<---- ОШИБКА выборки форм" + Environment.NewLine;
            }
            return toEject;
        }

        /*CreateAddEjectedMoldsXml
            Создает в папке XML-файл ejload.xml для дальнейшей передачи на машину
            Входные параметры:
                    - List<EJ> add - список форм с указанием причины сдува
            Выходные парамерты
                    - true - XML-файл сформирован и готов к загруке
                    - false - произошла ошибка, XML-файл НЕ готов к загрузке
         */
        private bool CreateAddEjectedMoldsXml(List<EJ> add)
        {
            ejectlog.Text += "----> Начинаю формировать XML для загрузки" + Environment.NewLine;
            try
            {
                string fileLoc = "addejectedmolds.xml";
                // вычищаем документ на всякий случай
                XDocument xDocument = XDocument.Load(fileLoc);
                xDocument.Descendants("mold").ToList().Remove();
                xDocument.Save(fileLoc);
                // -->

                XmlDocument doc = new XmlDocument();
                doc.Load(fileLoc);
                var node = doc.SelectSingleNode("//Root");

                add.ForEach(x => {
                    // создаем элемент и присоединяем
                    XmlElement elem;
                    elem = doc.CreateElement("mold");
                    elem.SetAttribute("nb", x.mold);
                    elem.SetAttribute("reason", x.reason);
                    node.AppendChild(elem);
                    // -->
                });

                //сохраняем
                doc.Save("ejload.xml");
                // --
                ejectlog.Text += "<---- XML СФОРМИРОВАН. Содержит - " + add.Count() + "  форм " + Environment.NewLine;
            }
            catch(Exception ex)
            {
                ejectlog.Text += "<---- Ошибка формирования XML" + Environment.NewLine;
                MessageBox.Show("ОШИБКА создания XML для постановки на принудительный сдув: " + ex.Message);
                return false;
            }

            return true;
        }

        /* SendEjectToMashine
            Формирует из XML SOAP-запрос и отправляет на машину (на основе XML-файла addejectedmolds.xml) TODO: XML хранить в памяти
            Входные параметры:
                    - string ip - ip-адрес машины на которую надо отправить запрос
            Выходные парамерты
                    - TODO: доработать после получения ответа
         */
        private void SendEjectToMashine(string ip)
        {
            ejectlog.Text += "----> Начинаю отправлять запрос на ПС" + Environment.NewLine;
            var _url = "http://"+ip+"/WSTM11/Service.asmx";
            var _action = "http://www.tiama-inspection.com/AddEjectedMolds";

            XmlDocument soapEnvelopeXml = CreateSoapEnvelope("ejload.xml");
            HttpWebRequest webRequest = CreateWebRequest(_url, _action);
            InsertSoapEnvelopeIntoWebRequest(soapEnvelopeXml, webRequest);

            IAsyncResult asyncResult = webRequest.BeginGetResponse(null, null);

            asyncResult.AsyncWaitHandle.WaitOne();
            ejectlog.Text += "<---- Запрос отправлен" + Environment.NewLine;

            string soapResult;
            using (WebResponse webResponse = webRequest.EndGetResponse(asyncResult))
            {
                using (StreamReader rd = new StreamReader(webResponse.GetResponseStream()))
                {
                    soapResult = rd.ReadToEnd();
                }
                ejectlog.Text += "----> Ответ получен:" + Environment.NewLine;
                ejectlog.Text += "****************************************" + Environment.NewLine;
                ejectlog.Text += soapResult + Environment.NewLine;
                ejectlog.Text += "****************************************" + Environment.NewLine;
                ejectlog.Text += "<---- Конец ответа" + Environment.NewLine;
            }
            
        }

        /* GetEjectedFromM1
            Получение ответа от машины по формам на сдуве
            Входные параметры:
                    
            Выходные парамерты
                    - List<EJ> -  список форм с указанием причины сдува, в т.ч. с 0 количеством
         */
        private List<EJ> GetEjectedFromM1()
        {
            ejectlog.Text += "----> Начинаю получать данные по формам на ПС" + Environment.NewLine;
            var m = new ServiceTM11SoapClient();
            XmlDocument docXML = new XmlDocument(); // XML-документ
            var ans = m.EjectedMolds().InnerXml;
            ans = "<xml>" + ans + "</xml>";
            docXML.LoadXml(ans); // загрузить XML

            // docXML.Load("ej.xml");

            var ej = new List<EJ>();

            if (docXML.GetElementsByTagName("xml")[0].ChildNodes.Count == 0)
            {
                ejectlog.Text += "<---- Данные получены, ответ пустой" + Environment.NewLine;
                return ej;
            }
            else
            {
                // разбор xml и передача в Woker
                foreach (XmlNode node in docXML.GetElementsByTagName("xml")[0].ChildNodes)
                {
                    ej.Add(new EJ { mold = node.Attributes.GetNamedItem("nb").Value, reason = node.Attributes.GetNamedItem("reason").Value });
                }
                ejectlog.Text += "<---- Данные получены, форм в ответе - " + ej.Count() + Environment.NewLine;
                return ej;
            }
        }

        /* UpdateInDB
            Получение ответа от машины по формам на сдуве
            Входные параметры:
                    - List<EJ> SENDED - список форм с указанием причины сдува ОТПРАВЛЕННЫХ на машину (полученных из БД, сформированных в xml и отправленных через SOAP)
                    - List<EJ> GETTED - список форм с указанием причины сдува ПОЛУЧЕННЫХ с машины (результат выполнения функции GetEjectedFromM1)
                    - string TABLE - таблица в которой обновляются данные
                    - string ACTION - обработка постановки или снятия с ПС (1 - снятие, 0 - постановка) 
            Выходные парамерты

         */
        private void UpdateInDB(List<EJ> sended, List<EJ> getted, string table, int action )
        {
            string whatToDo = action == 1 ? " СНЯТИЕ " : " ПОСТАНОВКА ";
            ejectlog.Text += "----> " + whatToDo + " Начинаю обновлять данные в БД"  + Environment.NewLine;
            var id1 = "UPDATE " + table + " SET ";
            var id2 = "UPDATE " + table + " SET ";
            var id3 = "UPDATE " + table + " SET ";
            var id4 = "UPDATE " + table + " SET ";
            var notSet = sended.Except(getted);
            if(notSet.Count() == 0)
            {
                ejectlog.Text += "       Все формы были поставлены на " + whatToDo + ", формирую и отправляю запрос" + Environment.NewLine;
                sended.ForEach(x =>
                {
                    if (action == 0) id1 += " M" + x.mold + " = 1 , ";
                    else id1 += " M" + x.mold + " = 0 , ";
                    if (action == 0) id2 += " M" + x.mold + " = " + x.reason + " , ";
                    else id2 += " M" + x.mold + " = -1 , ";
                    id3 += " M" + x.mold + " = -1 , ";
                    if (action == 0) id4 += " M" + x.mold + " = -1 , ";
                });
            }
            else
            {
                var setted = sended.Except(notSet).ToList();
                ejectlog.Text += "       На " + whatToDo + " было поставлено - " + setted.Count() + " форм, формирую и отправляю запрос" + Environment.NewLine;
                // получаем формы, которые встали на сдув и прописываем
                setted.ForEach(x =>
                {
                    if (action == 0) id1 += " M" + x.mold + " = 1 , ";
                    else id1 += " M" + x.mold + " = 0 , ";
                    if (action == 0) id2 += " M" + x.mold + " = " + x.reason + " , ";
                    else id2 += " M" + x.mold + " = -1 , ";
                    id3 += " M" + x.mold + " = -1 , ";
                    if (action == 0) id4 += " M" + x.mold + " = -1 , ";
                });
            }

            var conn = DbWalker.GetConnection(Resources.Server, Resources.User, Resources.Password, Resources.secure, "CPS" + Resources.Cech);
            try
            {
                conn.Open();

                var command = new SqlCommand(id1.Remove(id1.Length - 2) + " WHERE Id = 1 ", conn);
                command.ExecuteNonQuery();

                command = new SqlCommand(id2.Remove(id2.Length - 2) + " WHERE Id = 2 ", conn);
                command.ExecuteNonQuery();
                
                command = new SqlCommand(id3.Remove(id3.Length - 2) + " WHERE Id = 3 ", conn);
                command.ExecuteNonQuery();
                
                if(action == 0)
                {
                    command = new SqlCommand(id4.Remove(id4.Length - 2) + " WHERE Id = 4 ", conn);
                    command.ExecuteNonQuery();
                }

                ejectlog.Text += "<---- БД обновлена, ЗАВЕРШАЮ работу" + Environment.NewLine;
            }
            catch (Exception ex)
            {
                ejectlog.Text += "<---- ОШИБКА выборки форм" + Environment.NewLine;
            }
        }

        #endregion

        private void button5_Click(object sender, EventArgs e)
        {
            read_TG();
            request();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            this.Text = "М4 - 2" + Resources.LineControl+ " - резервная  v1.5";
           
            t_scan.Enabled= true;
            t_60.Enabled= true; 
            
        }

        private void t_scan_Tick(object sender, EventArgs e)
        {
            DT = DateTime.Now;
            if ((DT.Minute == 0 & DT.Second == 0) || (DT.Minute == 5 & DT.Second == 0) || (DT.Minute == 10 & DT.Second == 0) || (DT.Minute == 15 & DT.Second == 0) ||
               (DT.Minute == 20 & DT.Second == 0) || (DT.Minute == 25 & DT.Second == 0) || (DT.Minute == 30 & DT.Second == 0) || (DT.Minute == 35 & DT.Second == 0) ||
               (DT.Minute == 40 & DT.Second == 0) || (DT.Minute == 45 & DT.Second == 0) || (DT.Minute == 50 & DT.Second == 0) || (DT.Minute == 55 & DT.Second == 0))
            {
                read_TG();
                request();
            }
        }

        private void t_60_Tick(object sender, EventArgs e)
        {
            DT = DateTime.Now;
            if (DT.Minute == 01)
            {
                zip_table();
            }
        }

        #region Читаем данные о местах установки датчиков и их типы браков
        private void read_TG()
        {
            var sql = @"select 
                Input_1, Input_2, Input_3, Input_4, Input_5, Input_6, Input_7, Input_8 
                from CPS" + Resources.Cech + ".[dbo].[Table_TG2]" +
                " where Line = 4 ";
            var conn = DbWalker.GetConnection(Resources.Server, Resources.User, Resources.Password, Resources.secure, "CPS" + Resources.Cech);
            try
            {
                conn.Open();
            }
            catch (Exception ex)
            {
                MessageBox.Show(@"Конструктор" + ex.Message);
            }
            var command = new SqlCommand(sql, conn);
            SqlDataReader reader = command.ExecuteReader();
            reader.Read();
            //In_1 = (int)reader["Input_1"];
            In_2 = (int)reader["Input_2"];
            In_3 = (int)reader["Input_3"];
            In_4 = (int)reader["Input_4"];
            In_5 = (int)reader["Input_5"];
            In_6 = (int)reader["Input_6"];
            In_7 = (int)reader["Input_7"];
            //In_8 = (int)reader["Input_8"];
        }
        #endregion


        #region Собираем данные каждые 5 минут
        private void request()
        {
            var down = new List<data>();
            var downSbros = new List<sbros>();
            var m = new ServiceTM11SoapClient();
            dataGridView1.Rows.Clear();
            richTextBox1.Clear();

            //Словарь для определения соответствия входу и браку
            var counters = new Dictionary<string, Int32>()
            {
                { "2", In_2},
                { "3", In_3},
                { "4", In_4},
                { "5", In_5},
                { "6", In_6},
                { "7", In_7}
            };

            XmlDocument docXML = new XmlDocument(); // XML-документ
            //docXML.Load("M4-error.xml"); // загрузить XML
            docXML.LoadXml(m.Counts().InnerXml); // загрузить XML
            _getMolds = docXML;
            if (docXML.GetElementsByTagName("Inspected")[0] == null)
            {
                //MessageBox.Show("Пришел пустой ответ, время: " + DateTime.Now);
                return;
            }

            richTextBox1.Text = "Всего происпектированно: " + docXML.GetElementsByTagName("Inspected")[0].InnerText;
            richTextBox1.Text = richTextBox1.Text + Environment.NewLine + "Всего Сброшено: " + docXML.GetElementsByTagName("Rejects")[0].InnerText;
            richTextBox1.Text = richTextBox1.Text + Environment.NewLine + "Всего с дефектом: " + docXML.GetElementsByTagName("Defects")[0].InnerText;
            richTextBox1.Text = richTextBox1.Text + Environment.NewLine + "Автосброс: " + docXML.GetElementsByTagName("Autoreject")[0].InnerText;

            var mcf = docXML.GetElementsByTagName("Mold");
            richTextBox1.Text = richTextBox1.Text + Environment.NewLine + "--- ФОРМ В ОТВЕТЕ: " + mcf.Count + " ---";

            foreach (XmlNode tag in mcf)
            {
                richTextBox1.Text = richTextBox1.Text + Environment.NewLine + @"------------------------------ MOLD ID = " + tag.Attributes.GetNamedItem("id").InnerText + @" ------------------------------";

                // название формокомплекта для SQL
                sqlValues[0] = Convert.ToInt32(tag.Attributes.GetNamedItem("id").InnerText);

                var checker = true;

                foreach (XmlNode el in tag)
                {
                    if (el.Name == "Sensor")
                    {
                        richTextBox1.Text = richTextBox1.Text + Environment.NewLine + @"----------- SENSOR ID = " + el.Attributes.GetNamedItem("id").InnerText + @" -------------";
                        richTextBox1.Text = richTextBox1.Text + Environment.NewLine + "Сброшено: " + el["Rejects"].InnerText;
                        richTextBox1.Text = richTextBox1.Text + Environment.NewLine + "С дефектом: " + el["Defects"].InnerText;

                        // Обработка забракованных бутылок
                        if (el.InnerXml.Contains("Counter"))
                        {
                            foreach (XmlNode cnt in el)
                            {
                                if (cnt.Name == "Counter")
                                {
                                    richTextBox1.Text = richTextBox1.Text + Environment.NewLine + "Counter id = " + cnt.Attributes.GetNamedItem("id").InnerText + @": Nb = " + cnt.Attributes.GetNamedItem("Nb").InnerText;

                                    // Обработка 1 канала
                                    if (el.Attributes.GetNamedItem("id").InnerText == "40")
                                    {
                                        if (cnt.Attributes.GetNamedItem("id").InnerText == "1" || cnt.Attributes.GetNamedItem("id").InnerText == "2" || cnt.Attributes.GetNamedItem("id").InnerText == "4" || cnt.Attributes.GetNamedItem("id").InnerText == "5")
                                        {
                                            checker = false;
                                            down.Add(new data { sensorId = "40", deffect = dict1chanel[cnt.Attributes.GetNamedItem("id").InnerText], count = cnt.Attributes.GetNamedItem("Nb").InnerText, mould = tag.Attributes.GetNamedItem("id").InnerText, id = cnt.Attributes.GetNamedItem("id").InnerText });
                                        }
                                    }

                                    // Обработка 2 канала
                                    if (el.Attributes.GetNamedItem("id").InnerText == "16")
                                    {
                                        checker = false;
                                        down.Add(new data { sensorId = "16", deffect = "Считыватель", count = cnt.Attributes.GetNamedItem("Nb").InnerText, mould = tag.Attributes.GetNamedItem("id").InnerText, id = "none" });
                                    }

                                    // Обработка 3 канала
                                    if (el.Attributes.GetNamedItem("id").InnerText == "42")
                                    {
                                        if (cnt.Attributes.GetNamedItem("id").InnerText == "2" || cnt.Attributes.GetNamedItem("id").InnerText == "3" || cnt.Attributes.GetNamedItem("id").InnerText == "4" || cnt.Attributes.GetNamedItem("id").InnerText == "5" || cnt.Attributes.GetNamedItem("id").InnerText == "6" || cnt.Attributes.GetNamedItem("id").InnerText == "7")
                                        {
                                            checker = false;
                                            down.Add(new data { sensorId = "42", deffect = dict27chanel[counters[cnt.Attributes.GetNamedItem("id").InnerText]], count = cnt.Attributes.GetNamedItem("Nb").InnerText, mould = tag.Attributes.GetNamedItem("id").InnerText, id = cnt.Attributes.GetNamedItem("id").InnerText });
                                        }
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        richTextBox1.Text = richTextBox1.Text + Environment.NewLine + translate[el.Name] + @": " + el.InnerText;
                        sqlValues[sqlDic[el.Name]] = Convert.ToInt32(el.InnerText);
                    }
                    // создаем запрос
                }
                downSbros.Add(new sbros { cej = sqlValues[1], cpa = sqlValues[2], ces = sqlValues[3], mould = Convert.ToInt32(sqlValues[0]) });

                if (checker)
                {
                    down.Add(new data { sensorId = "42", deffect = "--", count = "0", mould = tag.Attributes.GetNamedItem("id").InnerText, id = "42" });
                    down.Add(new data { sensorId = "16", deffect = "--", count = "0", mould = tag.Attributes.GetNamedItem("id").InnerText, id = "16" });
                    down.Add(new data { sensorId = "40", deffect = "--", count = "0", mould = tag.Attributes.GetNamedItem("id").InnerText, id = "40" });
                }

            }

            // вывод в DatagridView
            down.ForEach(x =>
            {
                if (x.count != "0")
                    dataGridView1.Rows.Add(x.mould, x.deffect, x.count, x.sensorId);
            });

            var sqlInstance = "";

            var unique = down.Select(x => x.mould).Distinct().ToList();

            var conn = DbWalker.GetConnection(Resources.Server, Resources.User, Resources.Password, Resources.secure, "CPS" + Resources.Cech);
            try
            {
                conn.Open();
            }
            catch (Exception ex)
            {
                MessageBox.Show(@"Конструктор" + ex.Message);
            }

            unique.ForEach(x => {
                var d213 = down.Where(q => q.sensorId == "40").Where(q => q.id == "1").Where(q => q.mould == x).FirstOrDefault().count ?? "0";
                var d414 = down.Where(q => q.sensorId == "40").Where(q => q.id == "4").Where(q => q.mould == x).FirstOrDefault().count ?? "0";
                var d024_1 = down.Where(q => q.sensorId == "42").Where(q => q.deffect == dict27chanel[1]).Where(q => q.mould == x).Sum(q => Convert.ToInt32(q.count));
                var d024_3 = down.Where(q => q.sensorId == "42").Where(q => q.deffect == dict27chanel[2]).Where(q => q.mould == x).Sum(q => Convert.ToInt32(q.count));
                var d024_5 = down.Where(q => q.sensorId == "42").Where(q => q.deffect == dict27chanel[3]).Where(q => q.mould == x).Sum(q => Convert.ToInt32(q.count));
                var d024_6 = down.Where(q => q.sensorId == "42").Where(q => q.deffect == dict27chanel[4]).Where(q => q.mould == x).Sum(q => Convert.ToInt32(q.count));
                var d024_14 = down.Where(q => q.sensorId == "42").Where(q => q.deffect == dict27chanel[5]).Where(q => q.mould == x).Sum(q => Convert.ToInt32(q.count));
                var sb = downSbros.Where(q => q.mould == Convert.ToInt32(x)).FirstOrDefault();

                sqlInstance = "INSERT INTO [CPS2].[dbo].[Line_32_temp] " +
                            "(Number_Mould, " +
                            "Deffect_213, " +
                            "Deffect_414, " +
                            "Deffect_219, " +
                            "Deffect_220, " +
                            "Deffect_024_1, " +
                            "Deffect_024_3, " +
                            "Deffect_024_5, " +
                            "Deffect_024_6, " +
                            "Deffect_024_14, " +
                            "CEJ, " +
                            "CPA, " +
                            "CES) " +
                            "VALUES (" + x + "," + d213 + "," + d414 + "," + "0, " + "0, " + d024_1 + "," + d024_3 + "," + d024_5 + "," + d024_6 + "," + d024_14 + "," +
                            sb.cej + "," + sb.cpa + "," + sb.ces + ")";

                var command = new SqlCommand(sqlInstance, conn);
                try
                {
                    //command.Parameters.AddWithValue("DT", DT);
                    command.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(@"Запись временных данных: " + ex.Message);
                }
            });
            conn.Close();
        }
        #endregion

        #region Группируем данные за 1 час
        private void zip_table()
        {
            //Суммируем показатели
            var sql = @"select 
                    Number_Mould,
                    SUM(Deffect_213),
                    SUM(Deffect_414),
                    SUM(Deffect_219),
                    SUM(Deffect_220),
                    SUM(Deffect_024_1),
                    SUM(Deffect_024_3),
                    SUM(Deffect_024_5),
                    SUM(Deffect_024_6),
                    SUM(Deffect_024_14),
                    SUM(CEJ),
                    SUM(CPA),
                    SUM(CES)
                from [CPS2].[dbo].[Line_32_temp] 
                GROUP BY Number_mould
                    ";
            var conn = DbWalker.GetConnection(Resources.Server, Resources.User, Resources.Password, Resources.secure, "CPS" + Resources.Cech);
            try
            {
                conn.Open();

                var command = new SqlCommand(sql, conn);
                SqlDataReader reader = command.ExecuteReader();
                // reader.Read();
                var sqlinsert = "";

                while (reader.Read())
                {
                    var  DT = DateTime.Now;
                    sqlinsert += "INSERT INTO [CPS2].[dbo].[Line_32_count] " +
                                "(Time, " +
                                "Number_Mould, " +
                                "Deffect_213, " +
                                "Deffect_414, " +
                                "Deffect_219, " +
                                "Deffect_220, " +
                                "Deffect_024_1, " +
                                "Deffect_024_3, " +
                                "Deffect_024_5, " +
                                "Deffect_024_6, " +
                                "Deffect_024_14, " +
                                "CEJ, " +
                                "CPA, " +
                                "CES) " +
                                "VALUES ( '" + DT.ToString("yyyy-MM-ddTHH:00:00.000") + "'," + reader.GetValue(0) + "," + reader.GetValue(1) + "," + reader.GetValue(2) + ","
                                + reader.GetValue(3) + "," + reader.GetValue(4) + "," + reader.GetValue(5) + "," + reader.GetValue(6) + "," + reader.GetValue(7) + "," + reader.GetValue(8) + "," +
                                reader.GetValue(9) + "," + reader.GetValue(10) + "," + reader.GetValue(11) + "," + reader.GetValue(12) + ")";
                }
                reader.Close();
                command = new SqlCommand(sqlinsert, conn);
                command.Parameters.AddWithValue("DT", DT);
                command.ExecuteNonQuery();

                 //Чистим временную таблицу
                  sql = "TRUNCATE TABLE [CPS2].[dbo].[Line_32_temp]";
                  new SqlCommand(sql, conn).ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                MessageBox.Show(@"ZIP_TABLE: v " + ex.Message);
            }
           
        }
        #endregion
    }
}
