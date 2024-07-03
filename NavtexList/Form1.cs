
using System;
using DevExpress.SpreadsheetSource;
using DevExpress.Utils;
using DevExpress.XtraEditors.Repository;
using DevExpress.XtraGrid.Views.Grid;
using DevExpress.XtraEditors;

using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Timers;
using System.Runtime.InteropServices;

using MySql.Data.MySqlClient;

namespace NavtexList
{
    
    public partial class Form1 : DevExpress.XtraEditors.XtraForm
    {
        //private static readonly double cycleTime = 180000;  //3분
        private bool firstFlag = true;
        private bool checkFlag = false;
        private int rowCnt = 0;
        private float fontSize = 9f;

        private string strDataBase = "";
        private string strIP = "";
        private string strPort = "";
        private string strID = "";
        private string strPW = "";

        [DllImport("kernel32")]
        private static extern int GetPrivateProfileString(string section, string key, string def, StringBuilder retVal, int size, string filePath);

        public Form1()
        {
            InitializeComponent();

            txtFontSize.Text = fontSize.ToString();
            this.Shown += DevForm_shown;
            
            //dataGridView1.DataSource = table;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct FLASHWINFO
        {
            public UInt16 cbSize;
            public IntPtr hwnd;
            public UInt32 dwFlags;
            public UInt16 uCount;
            public UInt32 dwTimeout;
        }

        private const UInt32 FLASHW_STOP = 0;
        private const UInt32 FLASHW_TRAY = 2;

        [DllImport("user32.dll")]
        static extern Int16 FlashWindowEx(ref FLASHWINFO pwfi);


        public void DevForm_shown(object sender, EventArgs e)
        {
            
            InitGridControl();

            //GridSetting(null, null);

            timer1_Tick(sender, e);
            timer1.Interval = 10000;
            timer1.Start();
            //aTimer.Elapsed += GridSetting;
            //aTimer.Enabled = true;
        }

        public void timer1_Tick(object sender, EventArgs e)
        {
            DataTable dt = GetData();   //이전데이터와 새로 조회했을때 데이터 갯수
            if (!checkFlag && rowCnt < dt.Rows.Count)
            {
                FLASHWINFO fInfo = new FLASHWINFO();

                fInfo.cbSize = (ushort)Marshal.SizeOf(fInfo);
                fInfo.hwnd = this.Handle;
                fInfo.dwFlags = FLASHW_TRAY;
                fInfo.uCount = UInt16.MaxValue;
                fInfo.dwTimeout = 0;
                FlashWindowEx(ref fInfo);
            }
            
            GridSetting();

            //uiView_Main.Columns["순번"].Width = 70;
            uiView_Main.Columns["메세지구분"].Width = 150;
            
            uiView_Main.MoveLast();
        }

        public void GridSetting()
        {
            DataTable dt = GetData();
            this.uiGrid_Main.DataSource = dt;
            rowCnt = dt.Rows.Count;
            if (firstFlag)
            {
                uiView_Main.MoveFirst();
                int i = dt.Rows.Count - 1;
                string id = dt.Rows[i][0].ToString();
                selectBody(id);
            }

            uiView_Main.Appearance.HeaderPanel.Font = new Font("맑은 고딕", 10);
            uiView_Main.Appearance.Row.Font = new Font("맑은 고딕", 10);

            for (int i = 0; i < dt.Columns.Count; i++)
            {
                uiView_Main.Columns[i].AppearanceHeader.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
            }
            firstFlag = false;
            
        }

       
        

        public void InitGridControl()
        {
            GridView gv = this.uiGrid_Main.MainView as GridView;
            gv.OptionsView.ShowGroupPanel = false;
            gv.OptionsBehavior.Editable = true;
            this.uiView_Main.Columns.Clear();
            this.uiView_Main.FocusInvalidRow();
            
        }

        public DataTable GetData()
        {
            DataTable table = new DataTable();
            //table.Columns.Add("순번", typeof(string));
            table.Columns.Add("송신번호", typeof(string));
            table.Columns.Add("주파수", typeof(string));
            table.Columns.Add("메세지구분", typeof(string));
            table.Columns.Add("일자", typeof(string));
            table.Columns.Add("시간", typeof(string));

            // 서버#1로 전송
            //SqlConnection sqlConnection = new SqlConnection();
            DataSet ds = new DataSet();

            //string strDataBase = "Navtex";
            //string strIP = "localhost";
            //string strPort = "3306";
            //string strID = "sa";
            //string strPW = "gcsc8932";
            

            StringBuilder dataBase = new StringBuilder();
            StringBuilder ip = new StringBuilder();
            StringBuilder port = new StringBuilder();
            StringBuilder id = new StringBuilder();
            StringBuilder pw = new StringBuilder();

            GetPrivateProfileString("SETTING", "DATABASE", "(NONE)", dataBase, 32, System.Windows.Forms.Application.StartupPath + @"\config.ini");
            GetPrivateProfileString("SETTING", "IP", "(NONE)", ip, 32, System.Windows.Forms.Application.StartupPath + @"\config.ini");
            GetPrivateProfileString("SETTING", "PORT", "(NONE)", port, 32, System.Windows.Forms.Application.StartupPath + @"\config.ini");
            GetPrivateProfileString("SETTING", "ID", "(NONE)", id, 32, System.Windows.Forms.Application.StartupPath + @"\config.ini");
            GetPrivateProfileString("SETTING", "PW", "(NONE)", pw, 32, System.Windows.Forms.Application.StartupPath + @"\config.ini");

            strDataBase = dataBase.ToString();
            strIP = ip.ToString();
            strPort = port.ToString();
            strID = id.ToString();
            strPW = pw.ToString();

            //서버연결
            string constring = "server=" + strIP + "," + strPort + ";database=" + strDataBase + ";uid=" + strID + ";pwd=" + strPW;
            using (MySqlConnection sqlConnection = new MySqlConnection(constring))
            {
                sqlConnection.Open();
                
                string sql = @" SELECT	IDX AS ID, UTC_CODE, SUBSTRING(IDX,2,1) AS MSG_CODE,
                                        CASE WHEN F_YEAR != ''
											THEN CONCAT(	F_YEAR,'-', 
													  		CASE WHEN LENGTH(F_MONTH) != 2 THEN '0' + F_MONTH ELSE F_MONTH END, '-',
															CASE WHEN LENGTH(F_DAY) != 2 THEN '0' + F_DAY ELSE F_DAY END)
											ELSE ''
										END AS DAY_F
                                        ,CASE WHEN F_TIME != ''
											    THEN CONCAT(	SUBSTRING(F_TIME,1,2),':', 
													  		    SUBSTRING(F_TIME,3,2), ':',
															    SUBSTRING(F_TIME,5,2)   )
											    ELSE ''
										    END AS TIME_F
                                        ,replace(replace(REPLACE(MSG_BODY, CHAR(13), ' '), char(10), ' '), '  ' , '<br />') AS BODY
                                FROM NAV_MAIN
                                ORDER BY REV_DT ";

                MySqlDataAdapter rData = new MySqlDataAdapter(sql, sqlConnection);
                rData.Fill(ds);
            }
            
            //sqlConnection.ConnectionString = constring;
            //sqlConnection.Open();

            //try
            //{
            //    string sql = @"SELECT	ROW_NUMBER() OVER(ORDER BY REV_DT) AS CNT,
            //                                            IDX AS ID,
            //                                            UTC_CODE,
            //                                            SUBSTRING(IDX,2,1) AS MSG_CODE,
            //                                            CASE WHEN F_YEAR != '' 
            //                                                THEN	F_YEAR + '-' +
            //                                                        CASE WHEN LEN(F_MONTH) != 2 THEN '0' + F_MONTH ELSE F_MONTH END + '-' + 
            //                                                        CASE WHEN LEN(F_DAY) != 2 THEN '0' + F_DAY ELSE F_DAY END
            //                                            ELSE '' END AS DAY_F
            //                                            ,CASE WHEN F_TIME != '' 
            //                                                    THEN SUBSTRING(F_TIME,1,2) + ':' + 
            //                                                        SUBSTRING(F_TIME,3,2) + ':' + 
            //                                                        SUBSTRING(F_TIME,5,2) 
            //                                            ELSE '' END AS TIME_F
            //                                            , replace(replace(REPLACE(MSG_BODY, CHAR(13), ' '), char(10), ' '), '  ' , '<br />') AS BODY
            //                                    FROM NAV_MAIN ";
            //    //playCommand.Parameters.AddWithValue("@ORG_BODY", tmp_str);
            //    MySqlDataAdapter rData = new MySqlDataAdapter(sql, sqlConnection);
            //    rData.Fill(ds);
            //}
            //catch (Exception ex)
            //{
            //    sqlConnection.Close();
            //    Console.WriteLine("==============DB Error ==============");
            //    Console.WriteLine(ex.Message);
            //}

            if (ds.Tables[0].Rows.Count > 0)
            {
                for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
                {
                    //string cnt = ds.Tables[0].Rows[i]["CNT"].ToString();
                    string Rid = ds.Tables[0].Rows[i]["ID"].ToString();
                    string utcCode = ds.Tables[0].Rows[i]["UTC_CODE"].ToString();
                    string utcNm = selectUtcCode(utcCode);
                    string msgCd = ds.Tables[0].Rows[i]["MSG_CODE"].ToString();
                    string msgNm = selectMsgCode(msgCd);
                    string day = ds.Tables[0].Rows[i]["DAY_F"].ToString();
                    string time = ds.Tables[0].Rows[i]["TIME_F"].ToString();
                    string body = ds.Tables[0].Rows[i]["BODY"].ToString();
                    table.Rows.Add(Rid, utcNm, msgNm, day, time);
                }
            }
            return table;
        }

        public string selectMsgCode(string msgCd)
        {
            string msgNm = "";

            switch(msgCd)
            {
                case "A":
                    msgNm = "항해 경보";
                    break;
                case "B":
                    msgNm = "기상 경보";
                    break;
                case "C":
                    msgNm = "빙산 정보";
                    break;
                case "D":
                    msgNm = "수색 및 구조 정보";
                    break;
                case "E":
                    msgNm = "기상 예보";
                    break;
                case "F":
                    msgNm = "Pilot 메시지";
                    break;
                case "G":
                    msgNm = "AIS 메시지";
                    break;
                case "H":
                    msgNm = "LORAN-C 메시지";
                    break;
                case "I":
                    msgNm = "미사용";
                    break;
                case "J":
                    msgNm = "SANTNAV 메시지";
                    break;
                case "K":
                    msgNm = "다른 항해 지원 메시지";
                    break;
                case "L":
                    msgNm = "추가 항해 경보";
                    break;
                case "Z":
                    msgNm = "메세지 없음";
                    break;
                default:
                    break;
            }

            return msgNm;
        }

        public string selectUtcCode(string utcCode)
        {
            string name = "";
            switch (utcCode)
            {
                case "1":
                    name = "490kHz";
                    break;
                case "2":
                    name = "518kHz";
                    break;
                case "3":
                    name = "4209,5kHz";
                    break;
                default:
                    name = "not received...";
                    break;
            }
            return name;
        }

        private void RowCellClick(object sender, RowCellClickEventArgs e)
        {
            DataRow dr = this.uiView_Main.GetFocusedDataRow();
        }

        private void uiView_Main_RowClick(object sender, RowClickEventArgs e)
        {
            DataRow dr = this.uiView_Main.GetFocusedDataRow();

        }

        public void selectBody(string rId)
        {
            // 서버#1로 전송
            DataSet ds = new DataSet();

            //string strDataBase = "Navtex";
            //string strIP = "155.155.4.93";
            //string strPort = "1433";
            //string strID = "sa";
            //string strPW = "gcsc8932";

            StringBuilder dataBase = new StringBuilder();
            StringBuilder ip = new StringBuilder();
            StringBuilder port = new StringBuilder();
            StringBuilder id = new StringBuilder();
            StringBuilder pw = new StringBuilder();

            GetPrivateProfileString("SETTING", "DATABASE", "(NONE)", dataBase, 32, System.Windows.Forms.Application.StartupPath + @"\config.ini");
            GetPrivateProfileString("SETTING", "IP", "(NONE)", ip, 32, System.Windows.Forms.Application.StartupPath + @"\config.ini");
            GetPrivateProfileString("SETTING", "PORT", "(NONE)", port, 32, System.Windows.Forms.Application.StartupPath + @"\config.ini");
            GetPrivateProfileString("SETTING", "ID", "(NONE)", id, 32, System.Windows.Forms.Application.StartupPath + @"\config.ini");
            GetPrivateProfileString("SETTING", "PW", "(NONE)", pw, 32, System.Windows.Forms.Application.StartupPath + @"\config.ini");

            strDataBase = dataBase.ToString();
            strIP = ip.ToString();
            strPort = port.ToString();
            strID = id.ToString();
            strPW = pw.ToString();

            string constring = "server=" + strIP + "," + strPort + ";database=" + strDataBase + ";uid=" + strID + ";pwd=" + strPW;
            using (MySqlConnection sqlConnection = new MySqlConnection(constring))
            {
                sqlConnection.Open();

                string sql = @" SELECT	replace(replace(REPLACE(MSG_BODY, CHAR(13), ' '), char(10), ' '), '  ' , '\r\n')  AS BODY 
                                        
                                FROM NAV_MAIN
                                WHERE IDX = '" + rId + "'  ";

                MySqlDataAdapter rData = new MySqlDataAdapter(sql, sqlConnection);
                rData.Fill(ds);

                if (ds.Tables[0].Rows.Count > 0)
                {
                    memEdit.Text = ds.Tables[0].Rows[0][0].ToString();
                }

            }

            //서버연결
            //string constring = "server=" + strIP + "," + strPort + ";database=" + strDataBase + ";uid=" + strID + ";pwd=" + strPW;
            //sqlConnection.ConnectionString = constring;
            //sqlConnection.Open();

            //try
            //{
            //    string sql = @" SELECT	MSG_BODY AS BODY 

            //                    FROM NAV_MAIN
            //                    WHERE IDX = '" + id + "' ";
            //    //playCommand.Parameters.AddWithValue("@ORG_BODY", tmp_str);
            //    SqlDataAdapter rData = new SqlDataAdapter(sql, sqlConnection);
            //    rData.Fill(ds);


            //    if (ds.Tables[0].Rows.Count > 0)
            //    {
            //        memEdit.EditValue = ds.Tables[0].Rows[0][0].ToString();
            //    }
            //}
            //catch (Exception ex)
            //{
            //    Console.WriteLine("==============DB Error ==============");
            //    Console.WriteLine(ex.Message);
            //}
        }

        private void uiView_Main_Click(object sender, EventArgs e)
        {
            DataRow dr = this.uiView_Main.GetFocusedDataRow();
            string idx = dr.ItemArray[0].ToString();
            selectBody(idx);
        }

        private void Form1_Activated(object sender, EventArgs e)
        {
            checkFlag = true;
            FLASHWINFO fInfo = new FLASHWINFO();
            
            fInfo.cbSize = (ushort)Marshal.SizeOf(fInfo);
            fInfo.hwnd = this.Handle;
            fInfo.dwFlags = FLASHW_STOP;
            fInfo.uCount = UInt16.MaxValue;
            fInfo.dwTimeout = 0;

            FlashWindowEx(ref fInfo);
        }

        private void Form1_Leave(object sender, EventArgs e)
        {
        }

        private void Form1_Deactivate(object sender, EventArgs e)
        {
            checkFlag = false;
        }

        private void Form1_Resize(object sender, EventArgs e)
        {
            this.Invalidate();
            
        }

        //글꼴 사이즈 마이너스
        private void pictureEdit2_Click(object sender, EventArgs e)
        {
            fontSize = fontSize - 2f;
            txtFontSize.Text = fontSize.ToString();
            memEdit.Font = new Font("Tahoma", fontSize);
        }

        //글꼴 사이즈 플러스
        private void pictureEdit1_Click(object sender, EventArgs e)
        {
            fontSize = fontSize + 2f;
            txtFontSize.Text = fontSize.ToString();
            memEdit.Font = new Font("Tahoma", fontSize);
        }

        //글꼴 사이즈 조정시에 숫자치고 엔터눌렀을때
        private void txtFontSize_KeyUp(object sender, KeyEventArgs e)
        {
            if(e.KeyCode == Keys.Enter)
            {
                fontSize = Convert.ToInt32(txtFontSize.Text);
                memEdit.Font = new Font("Tahoma", fontSize);
            }
        }

        //글꼴 사이즈 조정후 커서 벗어나면
        private void txtFontSize_Leave(object sender, EventArgs e)
        {
            fontSize = Convert.ToInt32(txtFontSize.Text);
            memEdit.Font = new Font("Tahoma", fontSize);
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void txtFontSize_KeyPress(object sender, KeyPressEventArgs e)
        {
            if(!(char.IsDigit(e.KeyChar) || e.KeyChar == Convert.ToChar(Keys.Back)))
            {
                e.Handled = true;
            }
        }

        private void memEdit_EditValueChanged(object sender, EventArgs e)
        {

        }
    }

}
